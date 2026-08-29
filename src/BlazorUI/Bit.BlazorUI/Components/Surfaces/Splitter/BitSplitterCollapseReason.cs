namespace Bit.BlazorUI;

/// <summary>
/// What made the first panel of a BitSplitter collapse or expand.
/// </summary>
public enum BitSplitterCollapseReason
{
    /// <summary>
    /// The gutter was pressed, or moved by the Enter key or Ctrl with an arrow key.
    /// </summary>
    Gutter,

    /// <summary>
    /// The gutter was dragged close enough to the start of the splitter for the panel to snap shut.
    /// </summary>
    Drag,

    /// <summary>
    /// The Collapse, Expand or ToggleCollapse method of the splitter was called.
    /// </summary>
    Method,

    /// <summary>
    /// The position the splitter had remembered under its PersistKey was restored.
    /// </summary>
    Restore
}
