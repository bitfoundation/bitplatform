namespace Bit.BevTools;

/// <summary>One script a long animation frame ran: what invoked it, where it is written, how long it took.</summary>
public sealed class BevToolsFrameScript
{
    public string? Invoker { get; set; }

    public string? Type { get; set; }

    public string? Source { get; set; }

    public string? Fn { get; set; }

    public double DurationMs { get; set; }
}
