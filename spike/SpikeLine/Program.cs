using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;

namespace Clipline.Spike;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        if (args.Contains("--copy-text"))
        {
            return RunClipboardHelper(args);
        }

        if (args.Contains("--paste-probe"))
        {
            return RunPasteProbe();
        }

        if (args.Contains("--render-preview"))
        {
            return RunRenderPreview(GetValue(args, "--render-preview"));
        }

        if (args.Contains("--paste-gate"))
        {
            return RunPasteGate();
        }

        using var mutex = new Mutex(true, @"Local\CliplineSpike", out var isFirst);
        if (!isFirst)
        {
            Log.Write("另一个实例已在运行，退出（单实例守卫生效）");
            return 1;
        }

        Log.Reset();

        var anchorEnabled = !args.Contains("--no-anchor");
        var app = new Application();
        var window = new LineWindow(SpikeSamples.Build());
        var zone = new HotZone(window);
        var anchor = new AnchorLine();
        var tip = new OnboardingTip();

        window.Show();
        var tray = new TrayIcon(window.WindowHandle) { AnchorEnabled = anchorEnabled };
        tray.ToggleRequested += window.ToggleManually;
        tray.AnchorToggleRequested += () => window.ToggleAnchor();
        tray.QuitRequested += () =>
        {
            window.Close();
            app.Shutdown();
        };
        app.Exit += (_, _) => tray.Dispose();

        anchor.PlaceOnPrimaryMonitor(window);
        anchor.SetVisible(anchorEnabled);

        if (anchorEnabled && OnboardingState.NeedsTip)
        {
            ShowTip(tip, anchor);
        }

        window.OpenChanged += open =>
        {
            if (open)
            {
                tip.Dismiss();
            }

            anchor.SetVisible(anchorEnabled && !open);
            SetTrayTip(tray, window, open);
        };

        Lang.Changed += () =>
        {
            window.RefreshLanguage();
            tip.RefreshLanguage();
            if (tip.IsVisible)
            {
                tip.PlaceBelowAnchor(anchor);
            }

            SetTrayTip(tray, window, window.IsOpen);
        };

        // 锚点/绳头位置只跟工作区走，与卡片数量无关，所以不需要随绳子宽度重排。
        anchor.PullRequested += () =>
        {
            Log.Write("G4 cord pulled（悬停/点击绳头）");
            window.Reveal(manual: true);
        };

        window.AnchorToggleRequested += () =>
        {
            anchorEnabled = !anchorEnabled;
            tray.AnchorEnabled = anchorEnabled;
            anchor.PlaceOnPrimaryMonitor(window);
            anchor.SetVisible(anchorEnabled && !window.IsOpen);
            Log.Write($"G6 下拉绳显示={anchorEnabled}（Ctrl+Alt+L 与托盘菜单共用此开关）");
            if (anchorEnabled && OnboardingState.NeedsTip)
            {
                ShowTip(tip, anchor);
            }
            else
            {
                tip.Dismiss();
            }
        };

        window.QuitRequested += () =>
        {
            // 只关绳子窗口不够：锚点细线和引导气泡仍算"打开的窗口"，进程会留着不退出。
            app.Shutdown();
        };

        zone.Start();
        Log.Write("ready：指针停在屏幕顶边中部 → 绳子垂下；外部复制会自动挂上新卡片");
        Log.Write("Ctrl+Alt+T 开关绳子，Ctrl+Alt+L 开关锚点细线，Ctrl+Alt+Shift+Q 退出；托盘图标左键开关、右键菜单可退出");

        if (args.Contains("--selftest"))
        {
            RunSelfTest(app, window, anchor);
        }

        app.Run(window);
        return 0;
    }

    private static void ShowTip(OnboardingTip tip, AnchorLine anchor)
    {
        tip.Show();
        tip.PlaceBelowAnchor(anchor);
        Log.Write($"G4 onboarding tip shown at ({tip.Left:F0},{tip.Top:F0},{tip.Width:F0}x{tip.Height:F0})");
    }

    private static void SetTrayTip(TrayIcon tray, LineWindow window, bool open) => tray.SetTip(
        open ? Lang.T("tray.tip.open") : Lang.Format("tray.tip.closed", window.CardCount));

    private static int RunClipboardHelper(string[] args)
    {
        var text = GetValue(args, "--copy-text") ?? "selftest 普通复制：这条应该挂上绳子";
        var withIgnore = args.Contains("--with-ignore");
        var historyOptOut = args.Contains("--history-opt-out");
        var exitCode = ClipboardProducer.WriteText(text, withIgnore, historyOptOut);
        Log.Write($"helper pid={Environment.ProcessId} exit={exitCode} ignore={withIgnore} optOut={historyOptOut} text=\"{text}\"");
        return exitCode;
    }

    private static string? GetValue(string[] args, string flag)
    {
        var index = Array.IndexOf(args, flag);
        return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
    }

    private static string ProbeResultPath =>
        System.IO.Path.Combine(System.IO.Path.GetTempPath(), "clipline-paste-probe.txt");

    /// <summary>
    /// 独立进程里的普通可激活窗口：只有真正被别的进程注入 Ctrl+V 才算 G3-a 通过，
    /// 所以这里不写日志内容，只回写长度与哈希，避免把剪贴板原文留在临时文件里。
    /// </summary>
    private static int RunPasteProbe()
    {
        try
        {
            System.IO.File.Delete(ProbeResultPath);
        }
        catch (Exception)
        {
        }

        var app = new Application();
        var box = new System.Windows.Controls.TextBox { AcceptsReturn = true, FontSize = 14 };
        var window = new Window
        {
            Title = "CliplinePasteProbe",
            Content = box,
            Width = 460,
            Height = 180,
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
            Topmost = true,
        };

        var probeHwnd = IntPtr.Zero;
        window.SourceInitialized += (_, _) => probeHwnd = new WindowInteropHelper(window).Handle;

        var acquired = false;
        var started = Environment.TickCount64;
        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(120) };
        timer.Tick += (_, _) =>
        {
            if (!acquired && probeHwnd != IntPtr.Zero)
            {
                // 后台进程被 Windows 的前台锁挡住，Activate() 不够，这三下都是测试脚手架；
                // 产品路径不需要它：用户双击卡片时目标应用本来就是前台。
                Native.BringWindowToTop(probeHwnd);
                var setOk = Native.SetForegroundWindow(probeHwnd);
                if (!setOk)
                {
                    Native.SwitchToThisWindow(probeHwnd, true);
                }

                acquired = Native.GetForegroundWindow() == probeHwnd;
                Log.Write($"paste probe: hwnd=0x{probeHwnd.ToInt64():x} setForeground={setOk} acquired={acquired}");
                if (acquired)
                {
                    _ = box.Focus();
                    Keyboard.Focus(box);
                }
            }

            if (box.Text.Length > 0)
            {
                var text = box.Text;
                System.IO.File.WriteAllText(ProbeResultPath, $"ok len={text.Length} hash={System.HashCode.Combine(text)}");
                Log.Write($"paste probe received len={text.Length}");
                timer.Stop();
                app.Shutdown();
            }
            else if (Environment.TickCount64 - started > 12000)
            {
                System.IO.File.WriteAllText(ProbeResultPath, "timeout");
                Log.Write("paste probe timed out with empty box");
                timer.Stop();
                app.Shutdown();
            }
        };
        timer.Start();

        window.Show();
        box.Focus();
        app.Run(window);
        return 0;
    }

    /// <summary>
    /// 离屏把"收起态"和"展开态"画成一张 PNG，用来看真实 DPI 下的观感，
    /// 不必把窗口顶到屏幕上再靠人眼确认。背景从左到右由白到深灰，一次看清华白两种桌面。
    /// </summary>
    private static int RunRenderPreview(string? outputPath)
    {
        _ = new Application();
        var target = outputPath ?? System.IO.Path.Combine(System.IO.Path.GetTempPath(), "clipline-preview.png");
        const double sceneWidth = 780;
        const double sceneHeight = 300;

        var background = new System.Windows.Media.LinearGradientBrush { StartPoint = new Point(0, 0), EndPoint = new Point(1, 0) };
        background.GradientStops.Add(new GradientStop(Color.FromRgb(0xFF, 0xFF, 0xFF), 0));
        background.GradientStops.Add(new GradientStop(Color.FromRgb(0xF2, 0xF3, 0xF5), 0.35));
        background.GradientStops.Add(new GradientStop(Color.FromRgb(0x3A, 0x3E, 0x44), 0.7));
        background.GradientStops.Add(new GradientStop(Color.FromRgb(0x1F, 0x22, 0x26), 1));
        background.Freeze();

        var canvas = new System.Windows.Controls.Canvas { Background = background, Width = sceneWidth, Height = sceneHeight };

        void Place(System.Windows.UIElement element, double left, double top)
        {
            System.Windows.Controls.Canvas.SetLeft(element, left);
            System.Windows.Controls.Canvas.SetTop(element, top);
            canvas.Children.Add(element);
        }

        void AddLine(double top) => Place(new System.Windows.Shapes.Rectangle
        {
            Width = sceneWidth - 40,
            Height = 3,
            RadiusX = 1.5,
            RadiusY = 1.5,
            Fill = new SolidColorBrush(Color.FromArgb(0xD9, 0xFF, 0xFF, 0xFF)),
        }, 20, top);

        void AddCard(double left, double top, string text, bool mono) => Place(new System.Windows.Controls.Border
        {
            Width = LineWindow.CardWidth,
            Height = LineWindow.CardHeight,
            Child = new System.Windows.Controls.Grid
            {
                Children =
                {
                    KraftPaper.BuildCard(new System.Windows.Controls.TextBlock
                    {
                        Text = text,
                        Width = LineWindow.CardWidth - 18,
                        TextWrapping = TextWrapping.Wrap,
                        TextTrimming = TextTrimming.CharacterEllipsis,
                        MaxHeight = LineWindow.CardHeight - 24,
                        FontSize = mono ? 12 : 13,
                        FontFamily = new FontFamily(mono ? "Consolas" : "Segoe UI"),
                        Foreground = KraftPaper.Ink,
                    }),
                    CardView.BuildClothespin(),
                },
            },
        }, left, top);

        // 收起态：白线 + 工作区 2/3 处的拉绳锚点
        AddLine(18);
        Place(new System.Windows.Shapes.Path
        {
            Data = AnchorLine.CordGeometry,
            Stretch = Stretch.Uniform,
            Width = AnchorLine.CordWidthDip,
            Height = AnchorLine.CordHeightDip,
            Fill = new SolidColorBrush(Color.FromArgb(0xF0, 0xC9, 0x9C, 0x66)),
            Stroke = new SolidColorBrush(Color.FromArgb(0xF0, 0x5A, 0x3C, 0x1E)),
            StrokeThickness = 1.1,
        }, 20 + ((sceneWidth - 40) * AnchorLine.AnchorRatio) - (AnchorLine.CordWidthDip / 2), 16);

        // 展开态：白线 + 三张牛皮纸便签（图文同宽，最新的一张挂最右），这里直接画真实首屏的三句提示
        AddLine(118);
        AddCard(60, 120, Lang.T("hint.click"), false);
        AddCard(280, 120, Lang.T("hint.doubleClick"), false);
        AddCard(500, 120, Lang.T("hint.dragImage"), false);

        canvas.Measure(new Size(sceneWidth, sceneHeight));
        canvas.Arrange(new Rect(0, 0, sceneWidth, sceneHeight));
        canvas.UpdateLayout();

        const double scale = 2.0;
        var bitmap = new System.Windows.Media.Imaging.RenderTargetBitmap(
            (int)(sceneWidth * scale),
            (int)(sceneHeight * scale),
            96 * scale,
            96 * scale,
            System.Windows.Media.PixelFormats.Pbgra32);
        bitmap.Render(canvas);

        var encoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
        encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(bitmap));
        using (var stream = System.IO.File.Create(target))
        {
            encoder.Save(stream);
        }

        Log.Write($"render-preview → {target}");
        return 0;
    }

    private static bool ProbeIsForeground()
    {
        var title = new System.Text.StringBuilder(64);
        var hwnd = Native.GetForegroundWindow();
        if (hwnd == IntPtr.Zero)
        {
            return false;
        }

        Native.GetWindowText(hwnd, title, title.Capacity);
        return title.ToString().Contains("CliplinePasteProbe");
    }

    /// <summary>
    /// G3-a 半自动门禁：用户点一下探针窗口把前台交出去（Windows 前台锁不允许后台进程自己抢），
    /// 之后注入、比对全自动化。产品路径不需要这一步——用户双击卡片时目标应用本来就是前台。
    /// </summary>
    private static int RunPasteGate()
    {
        try
        {
            System.IO.File.Delete(ProbeResultPath);
        }
        catch (Exception)
        {
        }

        var app = new Application();
        var target = new SpikeItem
        {
            Kind = SpikeKind.Text,
            Label = "GATE",
            Text = $"clipline paste gate {Environment.TickCount64 % 100000}",
        };
        var expected = $"ok len={target.Text.Length} hash={System.HashCode.Combine(target.Text)}";
        var started = Environment.TickCount64;
        var injected = false;

        SpawnHelper("--paste-probe");
        Log.Write($"paste-gate: 请在 90 秒内点一下探针窗口的输入框，之后自动注入 Ctrl+V（目标 {target.Text.Length} 字）");

        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(150) };
        timer.Tick += (_, _) =>
        {
            if (!injected)
            {
                if (!ProbeIsForeground())
                {
                    if (Environment.TickCount64 - started > 90000)
                    {
                        Log.Write("paste-gate RESULT BLOCKED 探针始终没有成为前台（用户未点击或前台锁未解除）");
                        timer.Stop();
                        app.Shutdown();
                    }

                    return;
                }

                injected = true;
                Delivery.PasteIntoForeground(target);
                return;
            }

            if (!System.IO.File.Exists(ProbeResultPath))
            {
                return;
            }

            var actual = System.IO.File.ReadAllText(ProbeResultPath);
            Log.Write($"paste-gate RESULT {(actual == expected ? "PASS" : "FAIL")} probe={actual} expected={expected}");
            timer.Stop();
            app.Shutdown();
        };
        timer.Start();
        app.Run();
        return 0;
    }

    private static void SpawnHelper(string arguments)
    {
        var executable = Environment.ProcessPath ?? "CliplineSpike.exe";
        Process.Start(new ProcessStartInfo(executable, arguments) { UseShellExecute = false, CreateNoWindow = true });
        Log.Write($"selftest: spawned helper `{arguments}`");
    }

    private static void RunSelfTest(Application app, LineWindow window, AnchorLine anchor)
    {
        var watcher = window.Watcher;
        var stage = 0;
        var deadline = 0L;
        var lruIndex = 0;
        var clipboardUsable = true;
        var pasteSizeOk = true;
        Func<bool>? waiting = null;

        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
        timer.Tick += (_, _) =>
        {
            if (waiting is not null && Environment.TickCount64 < deadline && !waiting())
            {
                return;
            }

            waiting = null;
            switch (stage)
            {
                case 0:
                    Log.Write("selftest: reveal");
                    window.Reveal(manual: true);
                    Wait(600);
                    break;
                case 1:
                    Log.Write($"selftest: state open={window.IsOpen} bounds={window.OpenedBoundsPhysical} anchorVisible={anchor.IsVisible} anchorWidth={anchor.Width:F0} ropeWidth={window.Width:F0}");
                    window.Retract();
                    Wait(600);
                    break;
                case 2:
                    var probe = Native.OpenClipboard(IntPtr.Zero);
                    var probeError = Marshal.GetLastWin32Error();
                    if (probe)
                    {
                        Native.CloseClipboard();
                    }

                    clipboardUsable = probe;
                    Log.Write($"selftest: clipboard probe openable={probe} err={probeError}; cards={window.CardCount} captures={watcher.Captures}");
                    SpawnHelper("--copy-text \"selftest 普通复制 12345\"");
                    WaitFor(8000, () => watcher.Captures >= 1);
                    break;
                case 3:
                    Log.Write($"selftest: ①外部复制入绳 cards={window.CardCount} labels={window.CardLabels} captures={watcher.Captures}");
                    SpawnHelper("--copy-text \"selftest 敏感复制 67890\" --with-ignore");
                    WaitFor(8000, () => watcher.Rejections >= 1);
                    break;
                case 4:
                    Log.Write($"selftest: ②隐私闸门 rejections={watcher.Rejections} last={watcher.LastRejection} cards={window.CardCount}");
                    if (window.LatestItem is null)
                    {
                        Log.Write("selftest: FATAL no captured item to self-copy");
                    }
                    else
                    {
                        Delivery.Copy(window.LatestItem);
                    }

                    WaitFor(5000, () => watcher.SelfWrites >= 1);
                    break;
                case 5:
                    Log.Write($"selftest: ③自写抑制 selfWrites={watcher.SelfWrites} staleSkips={watcher.StaleSkips} cards={window.CardCount}");
                    SpawnHelper("--copy-text \"selftest 普通复制 12345\"");
                    WaitFor(8000, () => watcher.Duplicates >= 1);
                    break;
                case 6 when lruIndex < 9:
                    // 一次只派一个写者，且绝不在 UI 线程 Sleep：绳子收起时批量复制是真实场景，
                    // 阻塞 UI 会让排队的通知塌成一次读取（上一轮实测 9 写 1 获）。
                    SpawnHelper($"--copy-text \"selftest LRU 第 {lruIndex} 条\"");
                    lruIndex++;
                    deadline = Environment.TickCount64 + 450;
                    return;
                case 6:
                    Log.Write($"selftest: ④重复跳过 duplicates={watcher.Duplicates} captures={watcher.Captures}");
                    WaitFor(15000, () => window.Evictions >= 1);
                    break;
                case 7:
                    Log.Write($"selftest: ⑤LRU 挤旧 evictions={window.Evictions} cards={window.CardCount}");
                    SpawnHelper("--copy-text \"selftest 历史关闭 13579\" --history-opt-out");
                    WaitFor(8000, () => watcher.Rejections >= 2);
                    break;
                case 8:
                    // 直贴门禁不能全自动：Windows 前台锁不允许后台进程抢前台，
                    // 注入 Ctrl+V 会打进用户此刻正在用的窗口。这里只核对结构体尺寸。
                    var inputSize = Marshal.SizeOf<Native.INPUT>();
                    Log.Write($"selftest: ⑥直贴改为半自动（--paste-gate）sizeofINPUT={inputSize} {(inputSize == 40 ? "ok" : "WRONG，SendInput 会被拒")}");
                    if (inputSize != 40)
                    {
                        pasteSizeOk = false;
                    }

                    Wait(150);
                    break;
                default:
                    var pass = watcher.Captures >= 10
                        && watcher.Rejections >= 2
                        && watcher.SelfWrites >= 1
                        && watcher.Duplicates >= 1
                        && window.Evictions >= 1
                        && window.CardCount == watcher.MaxItems
                        && pasteSizeOk;
                    var verdict = !clipboardUsable
                        ? "BLOCKED（本机剪贴板被其他进程锁死，非代码问题：重启或注销后重跑）"
                        : pass ? "PASS" : "FAIL";
                    Log.Write(
                        $"selftest RESULT {verdict} clipboardUsable={clipboardUsable} captures={watcher.Captures} rejections={watcher.Rejections} lastRejection={watcher.LastRejection} selfWrites={watcher.SelfWrites} duplicates={watcher.Duplicates} staleSkips={watcher.StaleSkips} evictions={window.Evictions} cards={window.CardCount} pasteGate=manual(--paste-gate)");
                    timer.Stop();
                    app.Shutdown();
                    break;
            }

            return;

            void Wait(int ms)
            {
                stage++;
                deadline = Environment.TickCount64 + ms;
            }

            void WaitFor(int ms, Func<bool> condition)
            {
                stage++;
                waiting = condition;
                deadline = Environment.TickCount64 + ms;
            }
        };
        timer.Start();
    }
}
