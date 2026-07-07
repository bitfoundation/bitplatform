namespace Bit.BlazorUI;

/// <summary>
/// The texts of the BitMarkdownEditor UI. All strings default to English;
/// override individual properties to localize the editor.
/// </summary>
public class BitMarkdownEditorTexts
{
    public string ToolbarAriaLabel { get; set; } = "Markdown formatting";

    public string PreviewEmptyText { get; set; } = "Nothing to preview yet.";

    // Full format templates ({0} = the count) so localized strings control the
    // word order rather than concatenating fixed English fragments in the markup.
    public string WordsFormat { get; set; } = "{0} words";
    public string CharsFormat { get; set; } = "{0} chars";

    public string ModeEdit { get; set; } = "Edit";
    public string ModeSplit { get; set; } = "Split";
    public string ModePreview { get; set; } = "Preview";

    public string KeyboardShortcutsTitle { get; set; } = "Keyboard shortcuts";
    public string CloseAriaLabel { get; set; } = "Close";
    public string ShortcutBold { get; set; } = "Bold";
    public string ShortcutItalic { get; set; } = "Italic";
    public string ShortcutStrikethrough { get; set; } = "Strikethrough";
    public string ShortcutLink { get; set; } = "Link";
    public string ShortcutUndo { get; set; } = "Undo";
    public string ShortcutRedo { get; set; } = "Redo";
    public string ShortcutIndentOutdent { get; set; } = "Indent / Outdent";
    public string ShortcutContinueList { get; set; } = "Continue list";

    public string GetModeLabel(BitMarkdownEditorMode mode) => mode switch
    {
        BitMarkdownEditorMode.Edit => ModeEdit,
        BitMarkdownEditorMode.Split => ModeSplit,
        BitMarkdownEditorMode.Preview => ModePreview,
        _ => mode.ToString()
    };
}
