namespace Bit.Bheadless;

/// <summary>Render-fragment context exposed by <see cref="BitBhTab"/>.</summary>
public sealed record BitBhTabRenderContext
{
    /// <summary>Whether this tab is selected.</summary>
    public required bool IsSelected { get; init; }

    /// <summary>Whether this tab (or the group) is disabled.</summary>
    public required bool Disabled { get; init; }
}
