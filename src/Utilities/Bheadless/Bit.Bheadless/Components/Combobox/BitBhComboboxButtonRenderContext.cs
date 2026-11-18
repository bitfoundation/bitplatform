namespace Bit.Bheadless;

/// <summary>Render-fragment context exposed by <see cref="BitBhComboboxButton{TValue}"/>.</summary>
public sealed record BitBhComboboxButtonRenderContext<TValue>
{
    /// <summary>Whether the options panel is open.</summary>
    public required bool IsOpen { get; init; }

    /// <summary>Whether the combobox is disabled.</summary>
    public required bool Disabled { get; init; }
}
