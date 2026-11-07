namespace Bit.Belemetry;

/// <summary>
/// What is known about one component a capture involves: its type, how the timeline associates it with the failure,
/// and - from an <see cref="IBelemetryComponentContextProvider"/> - its place in the tree and its render activity.
/// Never parameter values or fields.
/// </summary>
public sealed class BelemetryComponentInfo
{
    /// <summary>The component type (full name): the stable identity across sessions and releases.</summary>
    public required string Type { get; init; }

    /// <summary>
    /// An instance key valid within one capture only (<c>c1</c>, <c>c2</c>, ...): never the renderer's component id,
    /// so it neither identifies a session nor grows a backend's cardinality.
    /// </summary>
    public string? InstanceKey { get; init; }

    /// <summary>The component types from the root down to this component's parent, when a provider knows them.</summary>
    public IReadOnlyList<string>? Ancestors { get; init; }

    /// <summary>How many times it rendered, when measured.</summary>
    public int? RenderCount { get; init; }

    /// <summary>The total render time, when measured.</summary>
    public double? RenderMilliseconds { get; init; }

    /// <summary>How many timeline events name it.</summary>
    public int Events { get; init; }

    /// <summary>How many failure events name it.</summary>
    public int Failures { get; init; }

    /// <summary>
    /// How it is associated with the capture: <c>framework</c> (an event the framework attributed to it),
    /// <c>trace</c> (the failure happened in its event handler's trace), <c>stack</c> (inferred from the exception's
    /// first application frame), or <c>provider</c>.
    /// </summary>
    public string Association { get; init; } = "framework";

    /// <summary>Where the facts came from: <c>timeline</c>, or the provider's name.</summary>
    public string Source { get; init; } = "timeline";
}
