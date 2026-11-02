using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Clipline.Spike;

internal enum SpikeKind
{
    Text,
    Image,
}

internal sealed class SpikeItem
{
    public required SpikeKind Kind { get; init; }
    public string? Text { get; init; }

    /// <summary>文案键。非空时文本跟着语言开关走，用于首次使用的那几张提示卡。</summary>
    public string? TextKey { get; init; }

    public string? FilePath { get; init; }
    public required string Label { get; set; }
    public bool IsMonospace { get; init; }

    public string Display => TextKey is null ? Text ?? string.Empty : Lang.T(TextKey);

    public string Preview => Kind switch
    {
        SpikeKind.Image => $"image {System.IO.Path.GetFileName(FilePath)}",
        _ => $"text len={Display.Length} \"{Display.Replace("\n", "\\n").Trim()}\"",
    };
}

internal static class SpikeSamples
{
    public static IReadOnlyList<SpikeItem> Build()
    {
        var image = WriteSamplePng();
        return
        [
            // 滚动提示挂最左：它讲的是"绳两头之外还有"，正好指着这一排被挤出绳外的方向。
            new SpikeItem
            {
                Kind = SpikeKind.Text,
                Label = "T-1",
                TextKey = "hint.scroll",
            },
            new SpikeItem
            {
                Kind = SpikeKind.Text,
                Label = "T-2",
                TextKey = "hint.click",
            },
            new SpikeItem
            {
                Kind = SpikeKind.Text,
                Label = "T-3",
                TextKey = "hint.doubleClick",
            },
            new SpikeItem
            {
                Kind = SpikeKind.Text,
                Label = "T-4",
                TextKey = "hint.dragImage",
            },
            // 图片卡挂最右：既守住"最新在右"的方向，也让"拖动图片"这条提示紧挨着能拖的那张卡。
            new SpikeItem
            {
                Kind = SpikeKind.Image,
                Label = "I-1",
                FilePath = image,
            },
        ];
    }

    private static string WriteSamplePng()
    {
        const int width = 360;
        const int height = 220;

        var visual = new DrawingVisual();
        using (var dc = visual.RenderOpen())
        {
            var background = new LinearGradientBrush(
                Color.FromRgb(0x2b, 0x5f, 0x8f),
                Color.FromRgb(0x7a, 0xc2, 0x9a),
                45);
            dc.DrawRectangle(background, null, new Rect(0, 0, width, height));
            dc.DrawEllipse(
                new SolidColorBrush(Color.FromArgb(0x66, 0xff, 0xff, 0xff)),
                null,
                new Point(width * 0.72, height * 0.3),
                42,
                42);

            var typeface = new Typeface("Segoe UI");
            var text = new FormattedText(
                "sample screenshot",
                System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                typeface,
                22,
                Brushes.White,
                1.0);
            dc.DrawText(text, new Point(24, height - 52));
        }

        var target = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
        target.Render(visual);
        target.Freeze();

        var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "clipline-spike-sample.png");
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(target));
        using var stream = System.IO.File.Create(path);
        encoder.Save(stream);

        return path;
    }
}
