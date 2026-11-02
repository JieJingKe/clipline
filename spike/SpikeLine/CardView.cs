using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;

namespace Clipline.Spike;

internal sealed class CardView : Grid
{
    // 用户提供的晾衣夹素材，viewBox 0 0 1024 1024，两段 path 都是 EvenOdd 才能挖出内框/铆钉。
    private const string ClothespinData =
        "F0 M604.16 600.746667H417.564444V349.297778c0-26.168889 21.617778-47.786667 47.786667-47.786667H557.511111c26.168889 0 47.786667 21.617778 47.786667 47.786667v251.448889z m-169.528889-17.066667h152.462222V349.297778c0-17.066667-13.653333-30.72-30.72-30.72h-92.16c-17.066667 0-30.72 13.653333-30.72 30.72v234.382222z " +
        "M827.164444 723.626667H193.422222c-18.204444 0-32.995556-14.791111-32.995555-32.995556v-20.48c0-13.653333 9.102222-25.031111 21.617777-28.444444l242.346667-58.026667h172.942222l232.106667 55.751111c18.204444 4.551111 31.857778 21.617778 31.857778 39.822222v10.24c0 19.342222-14.791111 34.133333-34.133334 34.133334zM426.666667 600.746667l-241.208889 58.026666c-5.688889 1.137778-9.102222 5.688889-9.102222 11.377778v20.48c0 9.102222 6.826667 15.928889 15.928888 15.928889h634.88c9.102222 0 17.066667-7.964444 17.066667-17.066667v-10.24c0-11.377778-7.964444-21.617778-19.342222-23.893333l-230.968889-55.751111H426.666667zM608.711111 457.386667h-13.653333v-17.066667h13.653333c3.413333 0 6.826667-2.275556 6.826667-5.688889 0-2.275556-3.413333-5.688889-6.826667-5.688889h-13.653333v-17.066666h13.653333c12.515556 0 22.755556 10.24 22.755556 21.617777s-10.24 23.893333-22.755556 23.893334zM425.528889 457.386667h-10.24c-12.515556 0-22.755556-10.24-22.755556-21.617778s10.24-21.617778 22.755556-21.617778h10.24v17.066667h-10.24c-3.413333 0-6.826667 2.275556-6.826667 5.688889 0 2.275556 3.413333 5.688889 6.826667 5.688889h10.24v14.791111z";

    public static readonly Geometry ClothespinGeometry = Geometry.Parse(ClothespinData);

    public const double PinWidth = 34;
    public const double PinHeight = 21.5;

    private readonly SpikeItem _item;
    private readonly LineWindow _owner;
    private readonly RotateTransform _swing = new();
    private TextBlock? _textContent;
    private static readonly DependencyProperty AngleProperty = DependencyProperty.Register(
        nameof(Angle),
        typeof(double),
        typeof(CardView),
        new PropertyMetadata(0.0, OnAngleChanged));

    private Point _pressPoint;
    private bool _dragged;
    private bool _doubleClickPending;
    private DragDropEffects _lastDragEffect;

    public CardView(SpikeItem item, LineWindow owner)
    {
        _item = item;
        _owner = owner;

        Width = LineWindow.CardWidth;
        Height = LineWindow.CardHeight;
        RenderTransform = _swing;

        Children.Add(KraftPaper.BuildCard(BuildContent()));
        Children.Add(BuildClothespin());

        MouseLeftButtonDown += OnLeftButtonDown;
        MouseLeftButtonUp += OnLeftButtonUp;
        MouseMove += OnMouseMove;
        PreviewMouseRightButtonDown += OnRightButtonDown;

        ContextMenu = BuildContextMenu();
    }

    public double Angle
    {
        get => (double)GetValue(AngleProperty);
        set => SetValue(AngleProperty, value);
    }

    public SpikeItem Item => _item;

    public void StartSwing(TimeSpan delay)
    {
        _swing.CenterX = Width / 2;
        _swing.CenterY = 0;

        var animation = new DoubleAnimationUsingKeyFrames { FillBehavior = FillBehavior.Stop, BeginTime = delay };
        animation.KeyFrames.Add(new LinearDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.Zero)));
        animation.KeyFrames.Add(new EasingDoubleKeyFrame(-2.4, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(160)))
        { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } });
        animation.KeyFrames.Add(new EasingDoubleKeyFrame(1.5, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(380)))
        { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut } });
        animation.KeyFrames.Add(new EasingDoubleKeyFrame(-0.8, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(620)))
        { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut } });
        animation.KeyFrames.Add(new LinearDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(900))));

        BeginAnimation(AngleProperty, animation);
    }

    private static void OnAngleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((CardView)d)._swing.Angle = (double)e.NewValue;

    /// <summary>金属银晾衣夹：夹口正好咬住绳线，只让夹子下缘 1/4 高度压在便签上，再多就糊住纸面。</summary>
    public static UIElement BuildClothespin() => new System.Windows.Shapes.Path
    {
        Data = ClothespinGeometry,
        Stretch = Stretch.Uniform,
        Width = PinWidth,
        Height = PinHeight,
        Fill = Metal,
        Stroke = new SolidColorBrush(Color.FromArgb(0xB0, 0x6E, 0x76, 0x7E)),
        StrokeThickness = 0.8,
        HorizontalAlignment = HorizontalAlignment.Center,
        VerticalAlignment = VerticalAlignment.Top,
        Margin = new Thickness(0, -PinHeight * 0.75, 0, 0),
    };

    private static readonly Brush Metal = BuildMetal();

    private static Brush BuildMetal()
    {
        var brush = new LinearGradientBrush { StartPoint = new Point(0, 0), EndPoint = new Point(0.35, 1) };
        brush.GradientStops.Add(new GradientStop(Color.FromArgb(0xF8, 0xFF, 0xFF, 0xFF), 0));
        brush.GradientStops.Add(new GradientStop(Color.FromArgb(0xF8, 0xF2, 0xF5, 0xF8), 0.18));
        brush.GradientStops.Add(new GradientStop(Color.FromRgb(0xD6, 0xDD, 0xE4), 0.45));
        brush.GradientStops.Add(new GradientStop(Color.FromRgb(0xB9, 0xC2, 0xCC), 0.72));
        brush.GradientStops.Add(new GradientStop(Color.FromRgb(0xED, 0xF1, 0xF5), 1));
        brush.Freeze();
        return brush;
    }

    private UIElement BuildContent()
    {
        if (_item.Kind == SpikeKind.Image && _item.FilePath is not null)
        {
            return new Image
            {
                Source = Delivery.LoadBitmap(_item.FilePath, 420),
                Stretch = Stretch.Uniform,
            };
        }

        _textContent = new TextBlock
        {
            Text = _item.Display,
            TextWrapping = TextWrapping.Wrap,
            TextTrimming = TextTrimming.CharacterEllipsis,
            MaxHeight = LineWindow.CardHeight - 24,
            FontSize = _item.IsMonospace ? 12 : 13,
            FontFamily = new FontFamily(_item.IsMonospace ? "Consolas" : "Segoe UI"),
            Foreground = KraftPaper.Ink,
            VerticalAlignment = VerticalAlignment.Top,
        };
        return _textContent;
    }

    /// <summary>语言开关拨动后重画文案。图片卡没有文本节点，右键菜单照样要换语言。</summary>
    public void RefreshLanguage()
    {
        if (_textContent is not null)
        {
            _textContent.Text = _item.Display;
        }

        ContextMenu = BuildContextMenu();
    }

    private ContextMenu BuildContextMenu()
    {
        var menu = new ContextMenu();
        menu.Items.Add(MenuEntry(Lang.T("ctx.copy"), () => Delivery.Copy(_item)));
        menu.Items.Add(MenuEntry(Lang.T("ctx.remove"), () => _owner.Remove(_item)));
        menu.Opened += (_, _) =>
            Log.Write($"context menu opened item={_item.Label} focus now={Native.DescribeWindow(Native.GetForegroundWindow())}");
        return menu;
    }

    private static MenuItem MenuEntry(string header, Action action)
    {
        var entry = new MenuItem { Header = header };
        entry.Click += (_, _) => action();
        return entry;
    }

    private void OnLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _pressPoint = e.GetPosition(null);
        _dragged = false;
        _doubleClickPending = e.ClickCount >= 2;
        CaptureMouse();
    }

    private void OnLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        ReleaseMouseCapture();
        if (_dragged)
        {
            return;
        }

        var focusBefore = Native.GetForegroundWindow();
        if (_doubleClickPending)
        {
            Delivery.PasteIntoForeground(_item);
        }
        else
        {
            Delivery.Copy(_item);
        }

        var focusAfter = Native.GetForegroundWindow();
        Log.Write(
            $"G1 focus check item={_item.Label} before=0x{focusBefore.ToInt64():x} after=0x{focusAfter.ToInt64():x} unchanged={focusBefore == focusAfter}");
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        if (_dragged || e.LeftButton != MouseButtonState.Pressed)
        {
            return;
        }

        var delta = e.GetPosition(null) - _pressPoint;
        var threshold = Math.Max(
            SystemParameters.MinimumHorizontalDragDistance,
            SystemParameters.MinimumVerticalDragDistance);
        if (Math.Abs(delta.X) < threshold && Math.Abs(delta.Y) < threshold)
        {
            return;
        }

        _dragged = true;
        ReleaseMouseCapture();

        var data = Delivery.BuildDataObject(_item);
        Log.Write($"G3 drag start item={_item.Label} formats={string.Join(",", data.GetFormats())}");

        _owner.DragInProgress = true;
        try
        {
            _lastDragEffect = DragDrop.DoDragDrop(this, data, DragDropEffects.Copy | DragDropEffects.Move);
        }
        catch (Exception error)
        {
            _lastDragEffect = DragDropEffects.None;
            Log.Write($"G3 drag threw {error.GetType().Name}: {error.Message}");
        }
        finally
        {
            _owner.DragInProgress = false;
        }

        Log.Write($"G3 drag end item={_item.Label} effect={_lastDragEffect}");
    }

    private void OnRightButtonDown(object sender, MouseButtonEventArgs e)
        => Log.Write($"right press item={_item.Label} focus={Native.DescribeWindow(Native.GetForegroundWindow())}");
}

/// <summary>
/// 牛皮纸便签配色。卡片本体和离屏预览共用同一套画刷，避免两处各写一份而走样。
/// 半透明玻璃底在浅色桌面上完全看不清白字，所以底色改成不透明牛皮纸、字改成墨色。
/// </summary>
internal static class KraftPaper
{
    public static readonly Brush Background = CreateBackground();
    public static readonly Brush Edge = Freeze(new SolidColorBrush(Color.FromArgb(0xB4, 0xC9, 0xAE, 0x86)));
    public static readonly Brush Ink = Freeze(new SolidColorBrush(Color.FromRgb(0x33, 0x22, 0x11)));

    public static Border BuildCard(UIElement content) => new()
    {
        CornerRadius = new CornerRadius(8),
        BorderThickness = new Thickness(1),
        BorderBrush = Edge,
        Background = Background,
        Effect = new DropShadowEffect
        {
            BlurRadius = 14,
            ShadowDepth = 2,
            Opacity = 0.28,
            Color = Color.FromRgb(0x4A, 0x3B, 0x2A),
        },
        Padding = new Thickness(8),
        Child = content,
    };

    private static Brush CreateBackground()
    {
        var brush = new LinearGradientBrush { StartPoint = new Point(0, 0), EndPoint = new Point(0.25, 1) };
        brush.GradientStops.Add(new GradientStop(Color.FromArgb(0xFA, 0xFD, 0xF7, 0xEA), 0));
        brush.GradientStops.Add(new GradientStop(Color.FromArgb(0xFA, 0xF8, 0xEF, 0xDA), 0.55));
        brush.GradientStops.Add(new GradientStop(Color.FromArgb(0xF7, 0xF1, 0xE2, 0xC4), 1));
        brush.Freeze();
        return brush;
    }

    private static SolidColorBrush Freeze(SolidColorBrush brush)
    {
        brush.Freeze();
        return brush;
    }
}
