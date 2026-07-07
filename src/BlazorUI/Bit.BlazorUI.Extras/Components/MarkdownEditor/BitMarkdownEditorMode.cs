namespace Bit.BlazorUI;

/// <summary>
/// Controls which panes the BitMarkdownEditor displays.
/// </summary>
public enum BitMarkdownEditorMode
{
    /// <summary>
    /// Only the markdown text area is shown.
    /// </summary>
    Edit,

    /// <summary>
    /// Editor and rendered preview are shown side by side.
    /// </summary>
    Split,

    /// <summary>
    /// Only the rendered preview is shown.
    /// </summary>
    Preview
}
