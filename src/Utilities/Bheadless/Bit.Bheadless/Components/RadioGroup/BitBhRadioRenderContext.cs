namespace Bit.Bheadless;

/// <summary>Render-fragment context exposed by <see cref="BitBhRadio"/>.</summary>
public sealed record BitBhRadioRenderContext
{
    /// <summary>Whether this radio is checked.</summary>
    public required bool IsChecked { get; init; }

    /// <summary>Whether this radio (or the group) is disabled.</summary>
    public required bool Disabled { get; init; }
}
