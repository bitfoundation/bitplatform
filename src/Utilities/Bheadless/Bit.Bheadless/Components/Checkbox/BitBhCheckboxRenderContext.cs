namespace Bit.Bheadless;

/// <summary>Render-fragment context exposed by <see cref="BitBhCheckbox"/> (and cascaded to <see cref="BitBhCheckboxIndicator"/>).</summary>
public sealed record BitBhCheckboxRenderContext
{
    /// <summary>Whether the box is checked.</summary>
    public required bool IsChecked { get; init; }

    /// <summary>Whether the box shows the mixed state.</summary>
    public required bool IsIndeterminate { get; init; }

    /// <summary>Whether the box is disabled.</summary>
    public required bool Disabled { get; init; }
}
