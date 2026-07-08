namespace Bit.BlazorUI;

/// <summary>
/// Describes how a <see cref="BitMarkdownEditorToolbarItem"/> behaves when clicked.
/// </summary>
public enum BitMarkdownEditorToolbarItemType
{
    /// <summary>
    /// Runs the associated <see cref="BitMarkdownEditorCommand"/> against the text.
    /// </summary>
    Command,

    /// <summary>
    /// Reverts the editor to the previous state in the undo history.
    /// </summary>
    Undo,

    /// <summary>
    /// Re-applies the most recently undone change.
    /// </summary>
    Redo,

    /// <summary>
    /// A non-interactive vertical divider in the toolbar.
    /// </summary>
    Separator,

    /// <summary>
    /// Cycles the editor display mode (edit / split / preview).
    /// </summary>
    TogglePreview,

    /// <summary>
    /// Toggles the full-screen mode of the editor.
    /// </summary>
    ToggleFullScreen,

    /// <summary>
    /// Toggles the keyboard-shortcut help panel.
    /// </summary>
    Help,

    /// <summary>
    /// Toggles the find &amp; replace panel.
    /// </summary>
    Find,

    /// <summary>
    /// Invokes a user-supplied callback.
    /// </summary>
    Custom,

    /// <summary>
    /// A button that reveals a menu of child items (e.g. a heading picker).
    /// </summary>
    Dropdown
}
