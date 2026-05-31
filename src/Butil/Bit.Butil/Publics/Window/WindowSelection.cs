namespace Bit.Butil;

/// <summary>
/// Represents the current text selection inside the window. Mirrors the most useful
/// fields of <see href="https://developer.mozilla.org/en-US/docs/Web/API/Selection">Selection</see>.
/// </summary>
public class WindowSelection
{
    /// <summary>
    /// The full selected text, equivalent to <c>Selection.toString()</c>.
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// True when the anchor and focus are at the same position.
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Selection/isCollapsed"/>
    /// </summary>
    public bool IsCollapsed { get; set; }

    /// <summary>
    /// The number of contiguous ranges in the selection (typically 0 or 1).
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Selection/rangeCount"/>
    /// </summary>
    public int RangeCount { get; set; }

    /// <summary>
    /// The selection type: <c>None</c>, <c>Caret</c>, or <c>Range</c>.
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Selection/type"/>
    /// </summary>
    public string? Type { get; set; }

    /// <summary>
    /// Offset of the anchor (selection start) inside its node.
    /// </summary>
    public int AnchorOffset { get; set; }

    /// <summary>
    /// Offset of the focus (selection end) inside its node.
    /// </summary>
    public int FocusOffset { get; set; }
}
