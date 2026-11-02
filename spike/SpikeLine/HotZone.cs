using System.Windows.Threading;

namespace Clipline.Spike;

internal sealed class HotZone
{
    private const int PollMs = 32;
    private const int StayInsideMarginPx = 12;

    private readonly LineWindow _window;
    private readonly DispatcherTimer _timer;

    public HotZone(LineWindow window)
    {
        _window = window;
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(PollMs) };
        _timer.Tick += (_, _) => Poll();
    }

    public void Start()
    {
        _timer.Start();
        Log.Write($"G2 polling started every {PollMs}ms; trigger zone = 顶边中间恒定 1/3 宽（与卡片数量无关，上限 900 DIP）；绳头可悬停/点击拉起");
    }

    private void Poll()
    {
        if (_window.DragInProgress || !Native.GetCursorPos(out var cursor))
        {
            return;
        }

        if (!_window.IsOpen)
        {
            if (_window.CursorInTriggerZone(cursor) && !_window.ShouldSuppress(cursor))
            {
                _window.Reveal();
            }

            return;
        }

        var bounds = _window.OpenedBoundsPhysical;
        var stillOnRope =
            cursor.X >= bounds.Left - StayInsideMarginPx &&
            cursor.X <= bounds.Right + StayInsideMarginPx &&
            cursor.Y <= bounds.Bottom + StayInsideMarginPx;
        if (!stillOnRope && !_window.InGrace)
        {
            _window.Retract();
        }
    }
}
