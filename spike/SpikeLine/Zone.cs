namespace Clipline.Spike;

/// <summary>
/// 顶边几何的唯一来源。分成两件事：
/// **触发区**（多大范围的顶边悬停会拉起绳子）与**绳子跨度**（绳子本体多宽）。
/// 触发区恒定是工作区中间的 1/3，不随卡片数量变化；绳子跨度随卡片变多而变长，但最长到屏幕装得下为止。
/// </summary>
internal static class Zone
{
    public const int EdgeThresholdPx = 6;
    public const double MaxWidthDip = 900;
    public const double TriggerRatioOfWorkArea = 1.0 / 3.0;
    public const double RopeRatioOfWorkArea = 0.4;

    public static (double LeftDip, double WidthDip) Trigger(Native.MONITORINFOEX monitor, double scale)
    {
        var workWidthDip = (double)monitor.rcWork.Width / scale;
        var widthDip = Math.Min(workWidthDip * TriggerRatioOfWorkArea, MaxWidthDip);
        return (monitor.rcWork.Left / scale + (workWidthDip - widthDip) / 2, widthDip);
    }

    /// <summary>绳子本体：居中，至少容纳看得见的卡片（minWidthDip），最宽不超过工作区。</summary>
    public static (double LeftDip, double WidthDip) Span(Native.MONITORINFOEX monitor, double scale, double minWidthDip)
    {
        var workWidthDip = WorkWidthDip(monitor, scale);
        var natural = Math.Min(workWidthDip * RopeRatioOfWorkArea, MaxWidthDip);
        var widthDip = Math.Clamp(Math.Max(natural, minWidthDip), 120, workWidthDip - 24);
        return (monitor.rcWork.Left / scale + (workWidthDip - widthDip) / 2, widthDip);
    }

    /// <summary>工作区宽度（DIP）。绳宽的上限、从而"看得见的张数"都由它算出来。</summary>
    public static double WorkWidthDip(Native.MONITORINFOEX monitor, double scale) => (double)monitor.rcWork.Width / scale;

    public static bool Contains(Native.POINT cursor, Native.MONITORINFOEX monitor, double scale)
    {
        var (leftDip, widthDip) = Trigger(monitor, scale);
        var left = leftDip * scale;
        return cursor.Y <= monitor.rcWork.Top + EdgeThresholdPx
            && cursor.X >= left
            && cursor.X <= left + widthDip * scale;
    }
}
