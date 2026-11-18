namespace Bit.Bheadless;

/// <summary>Render-fragment context exposed by <see cref="BitBhListboxButton{TValue}"/>.</summary>
public sealed record BitBhListboxButtonRenderContext<TValue>
{
    /// <summary>Whether the options panel is open.</summary>
    public required bool IsOpen { get; init; }

    /// <summary>Whether the listbox is disabled.</summary>
    public required bool Disabled { get; init; }

    /// <summary>The selected value (single-select).</summary>
    public required TValue? Value { get; init; }

    /// <summary>The selected values (multi-select).</summary>
    public required IReadOnlyCollection<TValue> Values { get; init; }

    /// <summary>The label(s) of the selected option(s), comma-separated, or null when nothing is selected.</summary>
    public string? Text { get; init; }
}
