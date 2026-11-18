namespace Bit.BevTools;

/// <summary>One execution of a component's render fragment (BuildRenderTree). Diffing time is tracked per batch, not per component.</summary>
public sealed class BevToolsRenderSample
{
    public required long Sequence { get; init; }

    public required DateTimeOffset At { get; init; }

    public required long StartTicks { get; init; }

    public double DurationMs { get; set; }

    public long BatchId { get; init; }

    public BevToolsRenderCause Cause { get; init; }

    public string? CauseDetail { get; init; }

    /// <summary>Timeline event id of this render.</summary>
    public long EventId { get; set; }

    /// <summary>Timeline event that triggered the render (UI event, navigation, parent render) when known.</summary>
    public long? TriggerEventId { get; init; }

    /// <summary>Component fields that changed since the previous render, when state capture is enabled for the component.</summary>
    public IReadOnlyList<string>? ChangedState { get; set; }

    /// <summary>
    /// Parameters and cascading values whose observable value changed since the previous render ("Name: before → after"),
    /// when state capture is enabled for the component. Empty when they were compared and nothing changed; null when
    /// they were not compared (capture started with this render).
    /// </summary>
    public IReadOnlyList<string>? ChangedParameters { get; set; }

    /// <summary>True when parameter values were compared with the previous render's (an empty list means "compared, nothing changed").</summary>
    public bool ParametersCompared => ChangedParameters is not null;

    /// <summary>
    /// A re-render the parent forced by re-applying parameters whose observable values were identical to the previous
    /// render. Blazor cannot skip it on its own (it only short-circuits for primitive-typed parameters); the component
    /// can, with <c>ShouldRender</c>, or the parent can stop recreating equivalent values. Only decided for renders
    /// where the parameters were compared.
    /// </summary>
    public bool IsWastedRender => Cause == BevToolsRenderCause.ParentRender && ChangedParameters is { Count: 0 };

    // Samples are the most numerous objects BevTools keeps (one per render, up to MaxRendersPerComponent per instance):
    // whether a render failed and what it allocated share one int, so measuring allocations does not make every sample
    // bigger. Bit 31: failed; bits 0-30: allocated bytes + 1, 0 when not measured, saturated at about 2 GB.
    private int _packed;

    public bool Failed
    {
        get => _packed < 0;
        set => _packed = value ? _packed | int.MinValue : _packed & int.MaxValue;
    }

    /// <summary>
    /// Bytes the render fragment (BuildRenderTree) allocated on its thread: strings it formatted, closures and lambdas it
    /// created, collections it built. Its children render separately and count for themselves. Null when the runtime does
    /// not count allocations per thread (<see cref="BevToolsAllocations.IsMeasurable"/>); at most <see cref="int.MaxValue"/> - 1.
    /// </summary>
    public long? AllocatedBytes
    {
        get => (_packed & int.MaxValue) is var stored and > 0 ? stored - 1L : null;
        // Clamped before the one is added: long.MaxValue + 1 would wrap to a negative count and read as nothing allocated.
        set => _packed = (_packed & int.MinValue) | (value is { } bytes ? (int)Math.Clamp(bytes, 0, int.MaxValue - 1) + 1 : 0);
    }
}
