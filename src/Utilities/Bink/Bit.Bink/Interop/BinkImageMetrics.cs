namespace Bit.Bink;

/// <summary>Natural size of a loaded image.</summary>
internal sealed record BinkImageMetrics
{
    public int Width { get; init; }
    public int Height { get; init; }
}
