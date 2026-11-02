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
    public string? FilePath { get; init; }
    public required string Label { get; set; }
    public bool IsMonospace { get; init; }

    public string Display => Text ?? string.Empty;

    public string Preview => Kind switch
    {
        SpikeKind.Image => $"image {System.IO.Path.GetFileName(FilePath)}",
        _ => $"text len={Display.Length} \"{Display.Replace("\n", "\\n").Trim()}\"",
    };
}
