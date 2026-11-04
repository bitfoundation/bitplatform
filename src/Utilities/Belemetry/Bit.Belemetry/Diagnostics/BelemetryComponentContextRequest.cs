namespace Bit.Belemetry;

/// <summary>Which components a capture involves.</summary>
public sealed class BelemetryComponentContextRequest
{
    /// <summary>The component types (full names) the timeline associates with the capture, most relevant first.</summary>
    public IReadOnlyList<string> ComponentTypes { get; init; } = [];

    /// <summary>The current route template, when known.</summary>
    public string? Route { get; init; }

    /// <summary>The most components to describe.</summary>
    public int MaxComponents { get; init; } = 20;
}
