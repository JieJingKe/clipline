using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace Clipline.Spike;

internal sealed class LineWindow : Window
{
    public const double CardHeight = 132;

    /// <summary>图文同宽：以前文字卡 320、图片卡 200，一排挂下来高低错落很难看。</summary>
    public const double CardWidth = 200;

    private const double Gap = 20;
    private const double SidePadding = 32;
    private const double LineY = 18;
    private const double Step = CardWidth + Gap;

    /// <summary>卡片挂出绳外时淡出的距离（DIP），到边就不是灰着戳你，而是干脆点不动。</summary>
    private const double EdgeFade = 90;

    private const int ToggleHotkeyId = 1;
    private const int QuitHotkeyId = 2;
    private const int AnchorHotkeyId = 3;
    private const int ManualGraceMs = 2500;

    private readonly Canvas _canvas = new();
    private readonly List<CardView> _cards = [];
    private readonly ClipboardWatcher _watcher = new();

    private IntPtr _hwnd;
    private double _scale = 1.0;
    private double _ignoreRetractUntil;
    private double _scroll;
    private Native.MONITORINFOEX _monitor;
    private bool _open;
    private bool _suppressLogged;

    public LineWindow(IEnumerable<SpikeItem> items)
    {
        WindowStyle = WindowStyle.None;
        AllowsTransparency = true;
        ShowInTaskbar = false;
        ShowActivated = false;
        Topmost = true;
        ResizeMode = ResizeMode.NoResize;
        Focusable = false;
        Title = "CliplineSpike";
        Background = Brushes.Transparent;
        Content = _canvas;

        foreach (var item in items)
        {
            _cards.Add(new CardView(item, this));
        }

        _watcher.Captured += Hang;
        PreviewMouseWheel += OnWheel;
        Log.Write($"window constructed; items={_cards.Count}");
    }

    public bool IsOpen => _open;

    public IntPtr WindowHandle => _hwnd;

    public ClipboardWatcher Watcher => _watcher;

    public int CardCount => _cards.Count;

    /// <summary>当前绳宽装得下几张。超出的卡片挂到绳外，靠滚轮沿绳查看。</summary>
    public int VisibleCount { get; private set; } = 1;

    public int Evictions { get; private set; }

    public SpikeItem? LatestItem { get; private set; }

    public string CardLabels => string.Join(",", _cards.Select(card => card.Item.Label));

    public bool DragInProgress { get; set; }

    public bool InGrace => Environment.TickCount64 < _ignoreRetractUntil;

    public Native.RECT OpenedBoundsPhysical => new()
    {
        Left = (int)Math.Round(Left * _scale),
        Top = (int)Math.Round(TargetTop() * _scale),
        Right = (int)Math.Round((Left + Width) * _scale),
        Bottom = (int)Math.Round((TargetTop() + Height) * _scale),
    };

    public bool CursorInTriggerZone(Native.POINT cursor) => Zone.Contains(cursor, MonitorNearestTo(cursor), _scale);

    /// <summary>触发区恒为工作区中间 1/3，日志里打出来便于核对锚点位置。</summary>
    public (double LeftDip, double WidthDip) TriggerZoneDip => Zone.Trigger(_monitor, _scale);

    public double Scale => _scale;

    public Native.MONITORINFOEX PrimaryMonitor => MonitorNearestTo(new Native.POINT { X = 0, Y = 0 });

    public event Action<bool>? OpenChanged;

    public event Action? AnchorToggleRequested;

    public event Action? QuitRequested;

    public void Reveal(bool manual = false)
    {
        if (_open)
        {
            return;
        }

        if (!Native.GetCursorPos(out var cursor))
        {
            return;
        }

        _monitor = MonitorNearestTo(cursor);
        Layout();
        _open = true;
        if (OnboardingState.NeedsTip)
        {
            OnboardingState.MarkOnboarded();
        }

        OpenChanged?.Invoke(true);
        if (manual)
        {
            _ignoreRetractUntil = Environment.TickCount64 + ManualGraceMs;
        }

        Log.Write(
            $"G2 reveal manual={manual} {_monitor} scale={_scale:F2} window=({Left:F0},{Top:F0},{Width:F0}x{Height:F0})");

        AnimateTop(HiddenTop(), TargetTop(), 420, new ElasticEase { Oscillations = 1, Springiness = 4, EasingMode = EasingMode.EaseOut });
        for (var index = 0; index < _cards.Count; index++)
        {
            _cards[index].StartSwing(TimeSpan.FromMilliseconds(index * 45));
        }
    }

    public void Retract()
    {
        if (!_open || DragInProgress)
        {
            return;
        }

        _open = false;
        _scroll = 0;
        OpenChanged?.Invoke(false);
        Log.Write($"G2 retract window=({Left:F0},{Top:F0})");
        AnimateTop(TargetTop(), HiddenTop(), 200, new CubicEase { EasingMode = EasingMode.EaseIn });
    }

    public void ToggleManually()
    {
        if (_open)
        {
            Retract();
        }
        else
        {
            Reveal(manual: true);
        }
    }

    /// <summary>顶边绳头（锚点）开关。热键和托盘菜单共用这一条路径。</summary>
    public void ToggleAnchor() => AnchorToggleRequested?.Invoke();

    /// <summary>卡片宽度固定，所以换语言只改文字，不需要重新量绳子。</summary>
    public void RefreshLanguage()
    {
        foreach (var card in _cards)
        {
            card.RefreshLanguage();
        }
    }

    public void Remove(SpikeItem item)
    {
        var card = _cards.FirstOrDefault(candidate => ReferenceEquals(candidate.Item, item));
        if (card is null)
        {
            return;
        }

        _cards.Remove(card);
        _canvas.Children.Remove(card);
        Log.Write($"removed item={item.Label}");

        var wasOpen = _open;
        Layout();
        if (wasOpen)
        {
            BeginAnimation(TopProperty, null);
            Top = TargetTop();
        }
    }

    /// <summary>
    /// 新条目挂到绳子最右边（最新在右，符合"往右长"的直觉）。绳宽装不下时先让它悬在左端窗外，
    /// 用滚轮拉回来；只有超出 <see cref="ClipboardWatcher.MaxItems"/> 才真的把最老的挤下绳。
    /// </summary>
    public void Hang(SpikeItem item)
    {
        var card = new CardView(item, this);
        _cards.Add(card);
        LatestItem = item;
        while (_cards.Count > _watcher.MaxItems)
        {
            var oldest = _cards[0];
            _cards.RemoveAt(0);
            _canvas.Children.Remove(oldest);
            Evictions++;
            Log.Write($"R7 LRU evicted {oldest.Item.Label} ({oldest.Item.Preview})");
        }

        var wasOpen = _open;
        Layout();
        if (wasOpen)
        {
            BeginAnimation(TopProperty, null);
            Top = TargetTop();
            card.StartSwing(TimeSpan.FromMilliseconds(120));
        }
    }

    public bool ShouldSuppress(Native.POINT cursor)
    {
        var foreground = Native.GetForegroundWindow();
        if (foreground == IntPtr.Zero || foreground == _hwnd || !Native.IsWindowVisible(foreground))
        {
            return false;
        }

        var description = Native.DescribeWindow(foreground);
        if (description.Contains("[explorer]", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (description.Contains("[mstsc]", StringComparison.OrdinalIgnoreCase))
        {
            LogSuppression($"remote session foreground={description}");
            return true;
        }

        if (!Native.GetWindowRect(foreground, out var rect))
        {
            return false;
        }

        var monitor = MonitorNearestTo(cursor);
        var coversWholeMonitor =
            rect.Left <= monitor.rcMonitor.Left &&
            rect.Top <= monitor.rcMonitor.Top &&
            rect.Right >= monitor.rcMonitor.Right &&
            rect.Bottom >= monitor.rcMonitor.Bottom;
        if (coversWholeMonitor)
        {
            LogSuppression($"fullscreen foreground={description}");
            return true;
        }

        _suppressLogged = false;
        return false;
    }

    private void LogSuppression(string reason)
    {
        if (_suppressLogged)
        {
            return;
        }

        _suppressLogged = true;
        Log.Write($"G2 suppressed reveal: {reason}");
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        var source = (HwndSource?)PresentationSource.FromVisual(this);
        _hwnd = source?.Handle ?? IntPtr.Zero;
        if (_hwnd == IntPtr.Zero)
        {
            Log.Write("FATAL no hwnd");
            return;
        }

        Native.MakeNonActivating(_hwnd);
        source!.AddHook(WndProc);
        _scale = Native.GetDpiForWindow(_hwnd) / 96.0;
        _watcher.Attach(_hwnd);

        var registeredToggle = Native.RegisterHotKey(_hwnd, ToggleHotkeyId, Native.MOD_CONTROL | Native.MOD_ALT, 0x54);
        var registeredQuit = Native.RegisterHotKey(_hwnd, QuitHotkeyId, Native.MOD_CONTROL | Native.MOD_ALT | Native.MOD_SHIFT, 0x51);
        var registeredAnchor = Native.RegisterHotKey(_hwnd, AnchorHotkeyId, Native.MOD_CONTROL | Native.MOD_ALT, 0x4C);
        Log.Write($"G1 hwnd=0x{_hwnd.ToInt64():x} scale={_scale:F2} hotkey toggle={registeredToggle} quit={registeredQuit} anchor={registeredAnchor}");

        ParkOffscreen();
    }

    protected override void OnClosed(EventArgs e)
    {
        _watcher.Detach(_hwnd);
        base.OnClosed(e);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == Native.WM_MOUSEACTIVATE)
        {
            handled = true;
            return new IntPtr(Native.MA_NOACTIVATE);
        }

        if (msg == Native.WM_CLIPBOARDUPDATE)
        {
            _watcher.OnClipboardUpdate();
            return IntPtr.Zero;
        }

        if (msg == Native.WM_HOTKEY)
        {
            switch (wParam.ToInt32())
            {
                case ToggleHotkeyId:
                    Log.Write("hotkey Ctrl+Alt+T");
                    ToggleManually();
                    return IntPtr.Zero;
                case QuitHotkeyId:
                    Log.Write("hotkey Ctrl+Alt+Shift+Q → quit");
                    Close();
                    QuitRequested?.Invoke();
                    return IntPtr.Zero;
                case AnchorHotkeyId:
                    Log.Write("hotkey Ctrl+Alt+L → 切换锚点");
                    ToggleAnchor();
                    return IntPtr.Zero;
            }
        }

        return IntPtr.Zero;
    }

    private void Layout()
    {
        // 绳宽只长到"屏幕装得下"为止，而不是长到装下所有卡片：以前的写法把 12 张的宽度
        // 塞给 Zone.Span，却被工作区上限夹回来，于是最新（最右）的几张被排在窗外看不见了。
        var capacity = CapacityDip();
        var shown = Math.Min(Math.Max(_cards.Count, 1), capacity);
        var (zoneLeft, zoneWidth) = Zone.Span(_monitor, _scale, SidePadding * 2 + shown * CardWidth + (shown - 1) * Gap);
        Width = zoneWidth;
        Height = LineY + CardHeight + 30;
        Left = zoneLeft;
        VisibleCount = Math.Max(1, (int)((Width - SidePadding * 2 + Gap) / Step));
        _scroll = Math.Clamp(_scroll, 0, Math.Max(0, _cards.Count - VisibleCount) * Step);

        _canvas.Children.Clear();
        var line = new Rectangle
        {
            Height = 3,
            Width = Math.Max(0, Width - SidePadding * 2 + 16),
            RadiusX = 1.5,
            RadiusY = 1.5,
            Fill = new SolidColorBrush(Color.FromArgb(0xD9, 0xFF, 0xFF, 0xFF)),
        };
        Canvas.SetLeft(line, SidePadding - 8);
        Canvas.SetTop(line, LineY - 6);
        _canvas.Children.Add(line);

        PlaceCards();
        Top = _open ? TargetTop() : HiddenTop();
    }

    /// <summary>
    /// 最新一张固定挂在"满绳时最后一格"的位置，旧的往左排出去；超出绳宽的那几张悬在窗外，
    /// 由 <see cref="OnWheel"/> 沿绳拉回来。不满绳时整排居中，保持原来的观感。
    /// </summary>
    private void PlaceCards()
    {
        var overflowing = _cards.Count > VisibleCount;
        var rowWidth = VisibleCount * CardWidth + (VisibleCount - 1) * Gap;
        var lastSlot = (Width - rowWidth) / 2 + (VisibleCount - 1) * Step;
        var x = SidePadding + Math.Max(0, (Width - (SidePadding * 2 + _cards.Count * CardWidth + (_cards.Count - 1) * Gap)) / 2);

        for (var index = 0; index < _cards.Count; index++)
        {
            if (overflowing)
            {
                x = lastSlot - (_cards.Count - 1 - index) * Step + _scroll;
            }

            var card = _cards[index];
            Canvas.SetLeft(card, x);
            Canvas.SetTop(card, LineY);
            var centre = x + CardWidth / 2;
            card.Opacity = Math.Clamp(Math.Min(centre, Width - centre) / EdgeFade, 0, 1);
            // 淡到看不见的卡片不该继续吃点击：否则用户点的是空气，绳子上却毫无反应。
            card.IsHitTestVisible = card.Opacity > 0.5;
            if (!overflowing)
            {
                x += Step;
            }

            _canvas.Children.Add(card);
        }
    }

    /// <summary>滚轮沿绳滑动：上滚看旧的，下滚回到最新。和蓝本 Tendedero 的手势一致。</summary>
    private void OnWheel(object sender, MouseWheelEventArgs e)
    {
        var most = Math.Max(0, _cards.Count - VisibleCount) * Step;
        if (!_open || most <= 0)
        {
            return;
        }

        var next = Math.Clamp(_scroll + e.Delta / 120.0 * Step / 2, 0, most);
        if (Math.Abs(next - _scroll) < 0.5)
        {
            return;
        }

        _scroll = next;
        e.Handled = true;
        PlaceCards();
        Log.Write($"R7 scroll={_scroll:F0}/{most:F0} cards={_cards.Count} visible={VisibleCount}");
    }

    /// <summary>屏幕最宽能挂几张：绳宽封顶在工作区之内，所以张数由工作区减去留白算出。</summary>
    private int CapacityDip() =>
        Math.Max(1, (int)((Zone.WorkWidthDip(_monitor, _scale) - 24 - SidePadding * 2 + Gap) / Step));

    private double TargetTop() => _monitor.rcMonitor.Top / _scale;

    private double HiddenTop() => TargetTop() - Height - 1;

    private void ParkOffscreen()
    {
        _monitor = MonitorNearestTo(new Native.POINT { X = 0, Y = 0 });
        Layout();
        Log.Write($"G1 parked offscreen {_monitor} window=({Left:F0},{Top:F0},{Width:F0}x{Height:F0})");
    }

    private Native.MONITORINFOEX MonitorNearestTo(Native.POINT point)
    {
        var handle = Native.MonitorFromPoint(point, Native.MONITOR_DEFAULTTONEAREST);
        var info = new Native.MONITORINFOEX { cbSize = System.Runtime.InteropServices.Marshal.SizeOf<Native.MONITORINFOEX>() };
        Native.GetMonitorInfo(handle, ref info);
        return info;
    }

    private void AnimateTop(double from, double to, int durationMs, EasingFunctionBase easing)
    {
        var animation = new DoubleAnimation(from, to, TimeSpan.FromMilliseconds(durationMs))
        {
            EasingFunction = easing,
            FillBehavior = FillBehavior.HoldEnd,
        };
        BeginAnimation(TopProperty, animation);
    }
}
