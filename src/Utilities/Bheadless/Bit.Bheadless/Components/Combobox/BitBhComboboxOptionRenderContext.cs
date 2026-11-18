namespace Bit.Bheadless;

/// <summary>Render-fragment context exposed by <see cref="BitBhComboboxOption{TValue}"/>.</summary>
public sealed record BitBhComboboxOptionRenderContext<TValue>
{
    /// <summary>The option's value.</summary>
    public required TValue Value { get; init; }

    /// <summary>Whether this option is selected.</summary>
    public required bool IsSelected { get; init; }

    /// <summary>Whether this option is highlighted.</summary>
    public required bool IsActive { get; init; }

    /// <summary>Whether this option is disabled.</summary>
    public required bool Disabled { get; init; }
}
