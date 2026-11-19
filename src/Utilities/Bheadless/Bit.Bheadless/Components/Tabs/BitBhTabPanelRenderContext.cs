namespace Bit.Bheadless;

/// <summary>Render-fragment context exposed by <see cref="BitBhTabPanel"/>.</summary>
public sealed record BitBhTabPanelRenderContext
{
    /// <summary>Whether this panel is the one shown.</summary>
    public required bool IsSelected { get; init; }
}
