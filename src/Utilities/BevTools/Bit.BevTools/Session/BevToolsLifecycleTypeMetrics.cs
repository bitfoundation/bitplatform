namespace Bit.BevTools;

/// <summary>Per-type lifecycle numbers observed through the framework's meters (.NET 10+): SetParametersAsync durations.</summary>
public sealed class BevToolsLifecycleTypeMetrics
{
    /// <summary>The full type name, namespace included, as the framework reports it; the same as <see cref="BevToolsComponentRecord.TypeName"/>.</summary>
    public required string TypeName { get; init; }

    /// <summary>The type name without its namespace, for display.</summary>
    public required string DisplayName { get; init; }

    public int ParameterUpdates;

    public double ParameterUpdateMs;

    public double MaxParameterUpdateMs;

    public int EventHandlers;

    public double EventHandlerMs;
}
