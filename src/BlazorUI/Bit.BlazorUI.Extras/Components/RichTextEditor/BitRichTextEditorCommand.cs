namespace Bit.BlazorUI;

/// <summary>
/// The commands <see cref="BitRichTextEditor.ExecuteCommandAsync(BitRichTextEditorCommand, string?)"/> runs: the ones the
/// editor's toolbar and shortcuts use. Each is also accepted by name by the string overload (<c>"bold"</c>,
/// <c>"formatBlock"</c>, ...).
/// </summary>
public enum BitRichTextEditorCommand
{
    /// <summary>Toggles bold on the selection, or for the text typed next at a caret.</summary>
    Bold,

    /// <summary>Toggles italic on the selection, or for the text typed next at a caret.</summary>
    Italic,

    /// <summary>Toggles underline on the selection, or for the text typed next at a caret.</summary>
    Underline,

    /// <summary>Toggles strikethrough on the selection, or for the text typed next at a caret.</summary>
    StrikeThrough,

    /// <summary>Toggles subscript on the selection (and superscript off), or for the text typed next at a caret.</summary>
    Subscript,

    /// <summary>Toggles superscript on the selection (and subscript off), or for the text typed next at a caret.</summary>
    Superscript,

    /// <summary>Toggles inline code on the selection, or for the text typed next at a caret.</summary>
    InlineCode,

    /// <summary>Clears the inline formatting of the selection: marks, colors and fonts. Links stay.</summary>
    RemoveFormat,

    /// <summary>
    /// Gives the lines the selection touches a block format: the value is the tag (<c>p</c>, <c>h1</c>-<c>h6</c>,
    /// <c>pre</c>, or <c>blockquote</c>, which quotes them, or takes the quote off where they all are quoted).
    /// </summary>
    FormatBlock,

    /// <summary>Turns the lines the selection touches into a numbered list, or back into paragraphs.</summary>
    InsertOrderedList,

    /// <summary>Turns the lines the selection touches into a bulleted list, or back into paragraphs.</summary>
    InsertUnorderedList,

    /// <summary>Turns the lines the selection touches into a checklist, or back into paragraphs.</summary>
    InsertTaskList,

    /// <summary>Nests the list items the selection touches under the item before them; indents any other line a step.</summary>
    Indent,

    /// <summary>Brings the list items the selection touches out a level; takes a step of indentation off any other line.</summary>
    Outdent,

    /// <summary>Aligns the lines the selection touches to the left.</summary>
    JustifyLeft,

    /// <summary>Centers the lines the selection touches.</summary>
    JustifyCenter,

    /// <summary>Aligns the lines the selection touches to the right.</summary>
    JustifyRight,

    /// <summary>Justifies the lines the selection touches.</summary>
    JustifyFull,

    /// <summary>Paints the selection with the text color given as the value (any CSS color).</summary>
    ForeColor,

    /// <summary>Highlights the selection with the color given as the value (any CSS color).</summary>
    BackColor,

    /// <summary>Sets the font family given as the value on the selection.</summary>
    FontName,

    /// <summary>Sets the font size given as the value (a CSS length, <c>18px</c>) on the selection.</summary>
    FontSize,

    /// <summary>
    /// Links the selection to the address given as the value, or inserts the address as a link at a caret. An address
    /// the editor's policy does not allow is refused through <see cref="BitRichTextEditor.OnError"/>.
    /// </summary>
    CreateLink,

    /// <summary>Removes every link the selection touches, or the one the caret is in.</summary>
    Unlink,

    /// <summary>Inserts a horizontal rule at the caret.</summary>
    InsertHorizontalRule,

    /// <summary>Undoes the last step of the editor's own history.</summary>
    Undo,

    /// <summary>Redoes the last undone step.</summary>
    Redo,
}
