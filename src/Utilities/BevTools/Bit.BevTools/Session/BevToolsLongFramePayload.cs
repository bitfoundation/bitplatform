namespace Bit.BevTools;

/// <summary>A long animation frame (or long task) as the page hub reports it.</summary>
public sealed class BevToolsLongFramePayload
{
    public double AgeMs { get; set; }

    public double DurationMs { get; set; }

    public double BlockingMs { get; set; }

    public BevToolsFrameScript[]? Scripts { get; set; }
}
