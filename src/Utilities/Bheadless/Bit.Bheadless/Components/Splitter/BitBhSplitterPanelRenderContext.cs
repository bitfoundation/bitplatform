namespace Bit.Bheadless;

/// <summary>Render-fragment context exposed by <see cref="BitBhSplitterPanel"/>.</summary>
public sealed record BitBhSplitterPanelRenderContext
{
    /// <summary>The panel's size in percent.</summary>
    public required double Size { get; init; }

    /// <summary>Whether the panel is collapsed.</summary>
    public required bool IsCollapsed { get; init; }
}
