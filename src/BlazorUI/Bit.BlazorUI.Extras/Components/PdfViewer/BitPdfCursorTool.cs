namespace Bit.BlazorUI;

/// <summary>What dragging on the document surface does.</summary>
public enum BitPdfCursorTool
{
    /// <summary>Dragging selects text (the default).</summary>
    Select,

    /// <summary>Dragging pans the document, as the hand tool of a desktop viewer does.</summary>
    Pan,
}
