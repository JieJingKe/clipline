using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace Clipline.Spike;

/// <summary>
/// 托盘菜单"使用小 tips"弹出的说明面板。卡片上不再挂提示卡，用法集中在这里，用户想看时自己点开。
/// 沿用 GhostWindow 的非激活窗口：弹出时不会把焦点从用户正在用的应用抢走。
/// </summary>
internal sealed class TipsPanel : GhostWindow
{
    private const double MarginDip = 16;
    private const int AutoHideMs = 12000;

    private static readonly string[] TipKeys =
    [
        "hint.open",
        "hint.scroll",
        "hint.click",
        "hint.doubleClick",
        "hint.dragImage",
    ];

    private readonly TextBlock _title;
    private readonly StackPanel _lines = new();
    private readonly Border _shell;
    private readonly DispatcherTimer _hideTimer = new() { Interval = TimeSpan.FromMilliseconds(AutoHideMs) };

    public TipsPanel()
    {
        Width = 360;
        _title = new TextBlock
        {
            FontSize = 14,
            FontWeight = FontWeights.SemiBold,
            Foreground = Brushes.White,
        };

        var stack = new StackPanel();
        stack.Children.Add(_title);
        stack.Children.Add(_lines);
        _shell = new Border
        {
            CornerRadius = new CornerRadius(10),
            Background = new SolidColorBrush(Color.FromArgb(0xE6, 0x1F, 0x22, 0x26)),
            BorderBrush = new SolidColorBrush(Color.FromArgb(0x40, 0xFF, 0xFF, 0xFF)),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(16, 12, 16, 12),
            Child = stack,
        };
        Content = _shell;

        MouseLeftButtonUp += (_, _) =>
        {
            Log.Write("G6 tips dismissed by click");
            HideTips();
        };

        _hideTimer.Tick += (_, _) =>
        {
            Log.Write("G6 tips auto-hidden");
            HideTips();
        };

        RebuildText();
    }

    /// <summary>点一下面板就收起，所以它必须参与命中测试；但仍走 GhostWindow 的"可点不可激活"。</summary>
    protected override bool ClickThrough => false;

    /// <summary>托盘菜单每次点击都走这里：已显示则收起，否则贴右下角弹出。</summary>
    public void Toggle()
    {
        if (IsVisible)
        {
            HideTips();
            return;
        }

        PlaceAboveTray();
        Show();
        _hideTimer.Stop();
        _hideTimer.Start();
        Log.Write($"G6 tips shown at ({Left:F0},{Top:F0},{Width:F0}x{Height:F0})");
    }

    public void RefreshLanguage() => RebuildText();

    private void HideTips()
    {
        _hideTimer.Stop();
        if (IsVisible)
        {
            Hide();
        }
    }

    /// <summary>贴着主屏工作区右下角（任务栏之上）。不触碰任务栏本身，也不占焦点。</summary>
    private void PlaceAboveTray()
    {
        var area = SystemParameters.WorkArea;
        Left = area.Right - Width - MarginDip;
        Top = area.Bottom - Height - MarginDip;
    }

    private void RebuildText()
    {
        _title.Text = Lang.T("tips.title");
        _lines.Children.Clear();
        foreach (var key in TipKeys)
        {
            _lines.Children.Add(new TextBlock
            {
                Text = "· " + Lang.T(key),
                FontSize = 13,
                Foreground = new SolidColorBrush(Color.FromRgb(0xE8, 0xEC, 0xEF)),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 6, 0, 0),
            });
        }

        FitHeight();
    }

    /// <summary>英文比中文多折一两行，高度按内容量出来，不在底部留一大片空白。</summary>
    private void FitHeight()
    {
        _shell.Measure(new Size(Width, double.PositiveInfinity));
        Height = Math.Ceiling(_shell.DesiredSize.Height);
        if (IsVisible)
        {
            PlaceAboveTray();
        }
    }
}
