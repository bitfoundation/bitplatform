namespace Bit.BlazorUI;

/// <summary>
/// The texts of the BitMarkdownEditor UI. All strings default to English;
/// override individual properties to localize the editor.
/// </summary>
public class BitMarkdownEditorTexts
{
    public string ToolbarAriaLabel { get; set; } = "Markdown formatting";
    public string EditorAriaLabel { get; set; } = "Markdown editor";
    public string PreviewAriaLabel { get; set; } = "Markdown preview";

    // Toolbar button titles. Keyed by the item names used in
    // BitMarkdownEditorToolbar.Default so custom toolbars reusing those names are
    // localized automatically; unknown names fall back to the item's own Title.
    public string ToolbarUndo { get; set; } = "Undo";
    public string ToolbarRedo { get; set; } = "Redo";
    public string ToolbarBold { get; set; } = "Bold";
    public string ToolbarItalic { get; set; } = "Italic";
    public string ToolbarStrikethrough { get; set; } = "Strikethrough";
    public string ToolbarHeading { get; set; } = "Heading";
    public string ToolbarHeading1 { get; set; } = "Heading 1";
    public string ToolbarHeading2 { get; set; } = "Heading 2";
    public string ToolbarHeading3 { get; set; } = "Heading 3";
    public string ToolbarHeading4 { get; set; } = "Heading 4";
    public string ToolbarHeading5 { get; set; } = "Heading 5";
    public string ToolbarHeading6 { get; set; } = "Heading 6";
    public string ToolbarQuote { get; set; } = "Blockquote";
    public string ToolbarUnorderedList { get; set; } = "Bullet list";
    public string ToolbarOrderedList { get; set; } = "Numbered list";
    public string ToolbarTaskList { get; set; } = "Task list";
    public string ToolbarLink { get; set; } = "Link";
    public string ToolbarImage { get; set; } = "Image";
    public string ToolbarInlineCode { get; set; } = "Inline code";
    public string ToolbarCodeBlock { get; set; } = "Code block";
    public string ToolbarTable { get; set; } = "Table";
    public string ToolbarHorizontalRule { get; set; } = "Horizontal rule";
    public string ToolbarTableTools { get; set; } = "Table tools";
    public string ToolbarTableInsertRowAbove { get; set; } = "Insert row above";
    public string ToolbarTableInsertRowBelow { get; set; } = "Insert row below";
    public string ToolbarTableDeleteRow { get; set; } = "Delete row";
    public string ToolbarTableInsertColumnBefore { get; set; } = "Insert column before";
    public string ToolbarTableInsertColumnAfter { get; set; } = "Insert column after";
    public string ToolbarTableDeleteColumn { get; set; } = "Delete column";
    public string ToolbarTableAlignLeft { get; set; } = "Align column left";
    public string ToolbarTableAlignCenter { get; set; } = "Align column center";
    public string ToolbarTableAlignRight { get; set; } = "Align column right";
    public string ToolbarSuperscript { get; set; } = "Superscript";
    public string ToolbarSubscript { get; set; } = "Subscript";
    public string ToolbarClearFormatting { get; set; } = "Clear formatting";
    public string ToolbarFind { get; set; } = "Find & replace";
    public string ToolbarTogglePreview { get; set; } = "Toggle preview mode";
    public string ToolbarToggleFullScreen { get; set; } = "Toggle full-screen";
    public string ToolbarHelp { get; set; } = "Keyboard shortcuts";

    public string PreviewEmptyText { get; set; } = "Nothing to preview yet.";

    /// <summary>
    /// The word shown inside the placeholder that stands in for an image while it uploads.
    /// </summary>
    public string UploadingText { get; set; } = "uploading";

    // Full format templates ({0} = the count) so localized strings control the
    // word order rather than concatenating fixed English fragments in the markup.
    public string WordsFormat { get; set; } = "{0} words";
    public string CharsFormat { get; set; } = "{0} chars";

    /// <summary>
    /// The character counter while a MaxLength is set ({0} = the count, {1} = the limit).
    /// </summary>
    public string CharsWithMaxFormat { get; set; } = "{0} / {1} chars";

    public string ReadingTimeFormat { get; set; } = "{0} min read";

    /// <summary>
    /// The caret position readout of the status bar ({0} = the line, {1} = the column).
    /// </summary>
    public string CursorPositionFormat { get; set; } = "Ln {0}, Col {1}";

    /// <summary>
    /// The selection size readout of the status bar ({0} = how many characters are selected).
    /// </summary>
    public string SelectedFormat { get; set; } = "{0} selected";

    public string ModeEdit { get; set; } = "Edit";
    public string ModeSplit { get; set; } = "Split";
    public string ModePreview { get; set; } = "Preview";

    public string FindReplaceTitle { get; set; } = "Find and replace";
    public string FindPlaceholder { get; set; } = "Find";
    public string ReplacePlaceholder { get; set; } = "Replace with";
    public string ReplaceButton { get; set; } = "Replace";
    public string ReplaceAllButton { get; set; } = "All";

    /// <summary>
    /// The accessible name of the "All" button, which needs to say what it replaces:
    /// its visible label is too short to stand on its own out of context.
    /// </summary>
    public string ReplaceAllAriaLabel { get; set; } = "Replace all";
    public string FindNextAriaLabel { get; set; } = "Next match";
    public string FindPreviousAriaLabel { get; set; } = "Previous match";
    public string MatchCaseAriaLabel { get; set; } = "Match case";

    /// <summary>
    /// The match counter of the find panel ({0} = the current match, {1} = the total).
    /// </summary>
    public string MatchesFormat { get; set; } = "{0} of {1}";

    public string NoMatchesText { get; set; } = "No results";

    public string KeyboardShortcutsTitle { get; set; } = "Keyboard shortcuts";
    public string CloseAriaLabel { get; set; } = "Close";
    public string ShortcutBold { get; set; } = "Bold";
    public string ShortcutItalic { get; set; } = "Italic";
    public string ShortcutStrikethrough { get; set; } = "Strikethrough";
    public string ShortcutInlineCode { get; set; } = "Inline code";
    public string ShortcutCodeBlock { get; set; } = "Code block";
    public string ShortcutLink { get; set; } = "Link";
    public string ShortcutHeadings { get; set; } = "Heading 1 - 6";
    public string ShortcutQuote { get; set; } = "Blockquote";
    public string ShortcutOrderedList { get; set; } = "Numbered list";
    public string ShortcutUnorderedList { get; set; } = "Bullet list";
    public string ShortcutTaskList { get; set; } = "Task list";
    public string ShortcutUndo { get; set; } = "Undo";
    public string ShortcutRedo { get; set; } = "Redo";
    public string ShortcutIndentOutdent { get; set; } = "Indent / Outdent";
    public string ShortcutTableCells { get; set; } = "Next / previous table cell";
    public string ShortcutContinueList { get; set; } = "Continue list";
    public string ShortcutFind { get; set; } = "Find & replace";
    public string ShortcutMoveLine { get; set; } = "Move line up / down";
    public string ShortcutDuplicateLine { get; set; } = "Duplicate line";
    public string ShortcutDeleteLine { get; set; } = "Delete line";
    public string ShortcutTogglePreview { get; set; } = "Cycle edit / split / preview";
    public string ShortcutFullScreen { get; set; } = "Toggle full-screen";
    public string ShortcutSubmit { get; set; } = "Submit";
    public string ShortcutEscapeTab { get; set; } = "Move focus out of the editor";

    public string GetToolbarTitle(string name, string fallback) => name switch
    {
        "undo" => ToolbarUndo,
        "redo" => ToolbarRedo,
        "bold" => ToolbarBold,
        "italic" => ToolbarItalic,
        "strikethrough" => ToolbarStrikethrough,
        "heading" => ToolbarHeading,
        "h1" => ToolbarHeading1,
        "h2" => ToolbarHeading2,
        "h3" => ToolbarHeading3,
        "h4" => ToolbarHeading4,
        "h5" => ToolbarHeading5,
        "h6" => ToolbarHeading6,
        "quote" => ToolbarQuote,
        "ul" => ToolbarUnorderedList,
        "ol" => ToolbarOrderedList,
        "task" => ToolbarTaskList,
        "link" => ToolbarLink,
        "image" => ToolbarImage,
        "code" => ToolbarInlineCode,
        "codeblock" => ToolbarCodeBlock,
        "table" => ToolbarTable,
        "hr" => ToolbarHorizontalRule,
        "tabletools" => ToolbarTableTools,
        "trowabove" => ToolbarTableInsertRowAbove,
        "trowbelow" => ToolbarTableInsertRowBelow,
        "trowdelete" => ToolbarTableDeleteRow,
        "tcolbefore" => ToolbarTableInsertColumnBefore,
        "tcolafter" => ToolbarTableInsertColumnAfter,
        "tcoldelete" => ToolbarTableDeleteColumn,
        "talignleft" => ToolbarTableAlignLeft,
        "taligncenter" => ToolbarTableAlignCenter,
        "talignright" => ToolbarTableAlignRight,
        "sup" => ToolbarSuperscript,
        "sub" => ToolbarSubscript,
        "clear" => ToolbarClearFormatting,
        "find" => ToolbarFind,
        "preview" => ToolbarTogglePreview,
        "fullscreen" => ToolbarToggleFullScreen,
        "help" => ToolbarHelp,
        _ => fallback
    };

    public string GetModeLabel(BitMarkdownEditorMode mode) => mode switch
    {
        BitMarkdownEditorMode.Edit => ModeEdit,
        BitMarkdownEditorMode.Split => ModeSplit,
        BitMarkdownEditorMode.Preview => ModePreview,
        _ => mode.ToString()
    };
}
