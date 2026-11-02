using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;

namespace Clipline.Spike;

internal abstract class GhostWindow : Window
{
    protected IntPtr Handle;

    protected GhostWindow()
    {
        WindowStyle = WindowStyle.None;
        AllowsTransparency = true;
        ShowInTaskbar = false;
        ShowActivated = false;
        Topmost = true;
        ResizeMode = ResizeMode.NoResize;
        Focusable = false;
        Background = Brushes.Transparent;
        SizeToContent = SizeToContent.Manual;
        // 点击穿透 = 窗口不参与命中测试；可交互的子类反过来要参与命中，
        // 但不参与激活，靠下面的 WM_MOUSEACTIVATE → MA_NOACTIVATE 保证"可点不可抢焦点"。
        IsHitTestVisible = !ClickThrough;
    }

    /// <summary>默认纯装饰（点击穿透）；需要交互的子类改成 false。</summary>
    protected virtual bool ClickThrough => true;

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        Handle = ((HwndSource?)PresentationSource.FromVisual(this))?.Handle ?? IntPtr.Zero;
        if (Handle == IntPtr.Zero)
        {
            return;
        }

        if (ClickThrough)
        {
            Native.MakeClickThrough(Handle);
        }
        else
        {
            Native.MakeNonActivating(Handle);
        }

        // 可交互的装饰窗也绝不能因为一次点击抢走焦点，否则 R1/零打扰原则就破了。
        ((HwndSource)PresentationSource.FromVisual(this)!).AddHook((hwnd, msg, wParam, lParam, ref handled) =>
        {
            if (msg == Native.WM_MOUSEACTIVATE)
            {
                handled = true;
                return new IntPtr(Native.MA_NOACTIVATE);
            }

            return IntPtr.Zero;
        });
    }
}

internal sealed class AnchorLine : GhostWindow
{
    public const double CordWidthDip = 34;
    public const double CordHeightDip = 50;

    /// <summary>锚点横向位置：工作区宽度的 2/3 处（用户指定，不居中）。</summary>
    public const double AnchorRatio = 2.0 / 3.0;

    // 用户提供的"拉绳"素材，viewBox 0 0 1024 1024。直接喂给 WPF 的 path mini-language，
    // 不引 SVG 依赖：这段路径本身已经是纯矢量数据。
    private const string PullCordData =
        "M544 388.5V32c0-17.7-14.3-32-32-32s-32 14.3-32 32v356.5c0 42.2-21.2 81.3-55.9 105.2-86.6 59.4-147.7 195.6-147.7 294.7 0 130.1 105.5 235.6 235.6 235.6s235.6-105.5 235.6-235.6c0-99-61.1-235.2-147.7-294.7-34.7-23.8-55.9-63-55.9-105.2z m89.3 521.2C600.9 942.1 557.8 960 512 960s-88.9-17.9-121.3-50.3c-32.4-32.4-50.3-75.5-50.3-121.3 0-52.8 23-121.8 58.5-175.8 17.9-27.3 38.3-49.5 58.8-64.2 18.8-13.5 37.1-20.4 54.4-20.4s35.5 6.8 54.4 20.4c20.5 14.7 40.8 36.9 58.8 64.2 35.5 54 58.5 123 58.5 175.8-0.2 45.8-18 88.9-50.5 121.3z";

    public static readonly Geometry CordGeometry = Geometry.Parse(PullCordData);

    private static readonly Brush NormalFill = Freeze(new SolidColorBrush(Color.FromArgb(0xF0, 0xC9, 0x9C, 0x66)));
    private static readonly Brush HoverFill = Freeze(new SolidColorBrush(Color.FromArgb(0xFF, 0xE8, 0xC7, 0x8C)));
    private static readonly Brush NormalStroke = Freeze(new SolidColorBrush(Color.FromArgb(0xF0, 0x5A, 0x3C, 0x1E)));
    private static readonly Brush HoverStroke = Freeze(new SolidColorBrush(Color.FromArgb(0xFF, 0x8A, 0x5F, 0x2E)));

    private readonly System.Windows.Shapes.Path _cord;

    public AnchorLine()
    {
        Width = CordWidthDip;
        Height = CordHeightDip;
        _cord = new System.Windows.Shapes.Path
        {
            Data = CordGeometry,
            Stretch = Stretch.Uniform,
            Fill = NormalFill,
            Stroke = NormalStroke,
            StrokeThickness = 1.1,
        };
        Content = _cord;
        Cursor = System.Windows.Input.Cursors.Hand;
    }

    /// <summary>绳头现在可悬停/可点击：鼠标放上去或点一下都拉起绳子。</summary>
    protected override bool ClickThrough => false;

    public event Action? PullRequested;

    public void PlaceAt(Native.MONITORINFOEX monitor, double scale, double leftDip)
    {
        Left = leftDip;
        Top = monitor.rcMonitor.Top / scale;
    }

    public void PlaceOnPrimaryMonitor(LineWindow rope)
    {
        var monitor = rope.PrimaryMonitor;
        var scale = rope.Scale;
        var centerPhysical = monitor.rcWork.Left + ((double)monitor.rcWork.Width * AnchorRatio);
        PlaceAt(monitor, scale, (centerPhysical - (CordWidthDip * scale / 2)) / scale);
        var (triggerLeft, triggerWidth) = rope.TriggerZoneDip;
        Log.Write($"G4 anchor placed physical=({Left * scale:F0},{Top * scale:F0},{CordWidthDip * scale:F0}x{CordHeightDip * scale:F0}) scale={scale:F2} ratio={AnchorRatio:F2} interactive=True triggerZone=({triggerLeft * scale:F0}..{(triggerLeft + triggerWidth) * scale:F0})");
    }

    protected override void OnMouseEnter(System.Windows.Input.MouseEventArgs e)
    {
        base.OnMouseEnter(e);
        _cord.Fill = HoverFill;
        _cord.Stroke = HoverStroke;
        PullRequested?.Invoke();
    }

    protected override void OnMouseLeave(System.Windows.Input.MouseEventArgs e)
    {
        base.OnMouseLeave(e);
        _cord.Fill = NormalFill;
        _cord.Stroke = NormalStroke;
    }

    protected override void OnMouseLeftButtonUp(System.Windows.Input.MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonUp(e);
        PullRequested?.Invoke();
    }

    private static SolidColorBrush Freeze(SolidColorBrush brush)
    {
        brush.Freeze();
        return brush;
    }

    public void SetVisible(bool visible)
    {
        if (visible == IsVisible)
        {
            return;
        }

        Log.Write($"G4 anchor visible={visible}");
        if (visible)
        {
            Show();
        }
        else
        {
            Hide();
        }
    }
}

internal sealed class OnboardingTip : GhostWindow
{
    private readonly TextBlock _text;

    public OnboardingTip()
    {
        Width = 300;
        // 英文文案比中文长三成，固定 40 高会切掉第二行，所以这里留出行高并允许折行。
        Height = 52;
        _text = new TextBlock
        {
            Text = Lang.T("tip.onboarding"),
            FontSize = 13,
            Foreground = Brushes.White,
            TextWrapping = TextWrapping.Wrap,
            TextAlignment = TextAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
        Content = new Border
        {
            CornerRadius = new CornerRadius(9),
            Background = new SolidColorBrush(Color.FromArgb(0xCC, 0x1f, 0x22, 0x26)),
            BorderBrush = new SolidColorBrush(Color.FromArgb(0x66, 0xff, 0xff, 0xff)),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(10, 4, 10, 4),
            Child = _text,
        };
    }

    public void RefreshLanguage() => _text.Text = Lang.T("tip.onboarding");

    public void PlaceBelowAnchor(AnchorLine anchor)
    {
        Left = anchor.Left + (anchor.Width - Width) / 2;
        Top = anchor.Top + anchor.Height + 2;
    }

    public void Dismiss()
    {
        if (IsVisible)
        {
            Hide();
            Log.Write("G4 onboarding tip dismissed");
        }
    }
}

internal static class OnboardingState
{
    private static string FlagPath => System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "CliplineSpike",
        "onboarded.flag");

    private static bool? _needsTip;

    public static bool NeedsTip => _needsTip ??= ComputeNeedsTip();

    private static bool ComputeNeedsTip()
    {
        var exists = System.IO.File.Exists(FlagPath);
        Log.Write($"G4 onboarding flag exists={exists} path={FlagPath}");
        return !exists;
    }

    public static void MarkOnboarded()
    {
        try
        {
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(FlagPath)!);
            System.IO.File.WriteAllText(FlagPath, DateTime.Now.ToString("O"));
            _needsTip = false;
            Log.Write("G4 onboarded flag written，气泡不再出现");
        }
        catch (Exception error)
        {
            Log.Write($"G4 onboarding flag write failed: {error.Message}");
        }
    }
}
