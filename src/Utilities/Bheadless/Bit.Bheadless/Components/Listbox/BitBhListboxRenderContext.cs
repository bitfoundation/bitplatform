namespace Bit.Bheadless;

/// <summary>Render-fragment context exposed by the listbox root (legacy).</summary>
public sealed record BitBhListboxRenderContext<TValue>
{
    /// <summary>Whether the listbox panel is open.</summary>
    public required bool IsOpen { get; init; }

    /// <summary>The selected value (single-select).</summary>
    public required TValue? Value { get; init; }

    /// <summary>The selected values (multi-select).</summary>
    public required IReadOnlyCollection<TValue> Values { get; init; }

    /// <summary>Closes the listbox panel.</summary>
    public required Action Close { get; init; }
}
