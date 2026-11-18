namespace Bit.Bink;

/// <summary>Measured text dimensions returned by the browser.</summary>
internal sealed record BinkTextMetrics
{
    public double Width { get; init; }
    public int Lines { get; init; } = 1;
}
