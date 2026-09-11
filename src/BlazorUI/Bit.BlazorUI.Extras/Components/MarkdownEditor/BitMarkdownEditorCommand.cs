namespace Bit.BlazorUI;

/// <summary>
/// The set of built-in editing commands the BitMarkdownEditor toolbar and keyboard shortcuts can invoke.
/// These are pure text transformations executed in C# so the markdown logic stays in one place.
/// </summary>
public enum BitMarkdownEditorCommand
{
    /// <summary>
    /// Toggles bold formatting on the current selection.
    /// </summary>
    Bold,

    /// <summary>
    /// Toggles italic formatting on the current selection.
    /// </summary>
    Italic,

    /// <summary>
    /// Toggles strikethrough formatting on the current selection.
    /// </summary>
    Strikethrough,

    /// <summary>
    /// Toggles inline code formatting on the current selection.
    /// </summary>
    InlineCode,

    /// <summary>
    /// Toggles a level 1 heading on the selected lines.
    /// </summary>
    Heading1,

    /// <summary>
    /// Toggles a level 2 heading on the selected lines.
    /// </summary>
    Heading2,

    /// <summary>
    /// Toggles a level 3 heading on the selected lines.
    /// </summary>
    Heading3,

    /// <summary>
    /// Toggles a level 4 heading on the selected lines.
    /// </summary>
    Heading4,

    /// <summary>
    /// Toggles a level 5 heading on the selected lines.
    /// </summary>
    Heading5,

    /// <summary>
    /// Toggles a level 6 heading on the selected lines.
    /// </summary>
    Heading6,

    /// <summary>
    /// Toggles a blockquote on the selected lines.
    /// </summary>
    Quote,

    /// <summary>
    /// Wraps the current selection in a fenced code block.
    /// </summary>
    CodeBlock,

    /// <summary>
    /// Inserts a link or turns the current selection into a link.
    /// </summary>
    Link,

    /// <summary>
    /// Inserts an image or turns the current selection into an image.
    /// </summary>
    Image,

    /// <summary>
    /// Toggles an unordered (bullet) list on the selected lines.
    /// </summary>
    UnorderedList,

    /// <summary>
    /// Toggles an ordered (numbered) list on the selected lines.
    /// </summary>
    OrderedList,

    /// <summary>
    /// Toggles a task (checkbox) list on the selected lines.
    /// </summary>
    TaskList,

    /// <summary>
    /// Inserts a table template at the caret position.
    /// </summary>
    Table,

    /// <summary>
    /// Inserts a horizontal rule at the caret position.
    /// </summary>
    HorizontalRule,

    /// <summary>
    /// Increases the indentation of the selected lines (Tab).
    /// </summary>
    Indent,

    /// <summary>
    /// Decreases the indentation of the selected lines (Shift+Tab).
    /// </summary>
    Outdent,

    /// <summary>
    /// Smart newline that continues lists and quotes (Enter).
    /// </summary>
    NewLine,

    /// <summary>
    /// Toggles superscript (<c>^text^</c>) on the current selection. The syntax is an extension:
    /// none of the <see cref="BitMarkdownPipelines"/> renders it, so the built-in preview shows
    /// it verbatim unless the markdown is rendered elsewhere by a parser that supports it.
    /// </summary>
    Superscript,

    /// <summary>
    /// Toggles subscript (<c>~text~</c>) on the current selection. The syntax is an extension:
    /// none of the <see cref="BitMarkdownPipelines"/> renders it, so the built-in preview shows
    /// it verbatim unless the markdown is rendered elsewhere by a parser that supports it.
    /// </summary>
    Subscript,

    /// <summary>
    /// Removes inline and block markdown formatting from the selected lines.
    /// </summary>
    ClearFormatting,

    /// <summary>
    /// Swaps the selected lines with the line above them.
    /// </summary>
    MoveLineUp,

    /// <summary>
    /// Swaps the selected lines with the line below them.
    /// </summary>
    MoveLineDown,

    /// <summary>
    /// Duplicates the selected lines right below themselves.
    /// </summary>
    DuplicateLine,

    /// <summary>
    /// Deletes the selected lines entirely.
    /// </summary>
    DeleteLine,

    /// <summary>
    /// Inserts an empty row above the table row the caret is in. Above the header row means
    /// the first body row, since a GFM table cannot start without its header. Does nothing
    /// when the caret is not inside a table.
    /// </summary>
    TableInsertRowAbove,

    /// <summary>
    /// Inserts an empty row below the table row the caret is in.
    /// Does nothing when the caret is not inside a table.
    /// </summary>
    TableInsertRowBelow,

    /// <summary>
    /// Deletes the table row the caret is in. The header row is kept, since the table is
    /// defined by it. Does nothing when the caret is not inside a table.
    /// </summary>
    TableDeleteRow,

    /// <summary>
    /// Inserts an empty column before the one the caret is in, and moves the caret into its
    /// header cell. Does nothing when the caret is not inside a table.
    /// </summary>
    TableInsertColumnBefore,

    /// <summary>
    /// Inserts an empty column after the one the caret is in, and moves the caret into its
    /// header cell. Does nothing when the caret is not inside a table.
    /// </summary>
    TableInsertColumnAfter,

    /// <summary>
    /// Deletes the table column the caret is in. The last remaining column is kept.
    /// Does nothing when the caret is not inside a table.
    /// </summary>
    TableDeleteColumn,

    /// <summary>
    /// Left-aligns the table column the caret is in (<c>:---</c>).
    /// Does nothing when the caret is not inside a table.
    /// </summary>
    TableAlignLeft,

    /// <summary>
    /// Centers the table column the caret is in (<c>:---:</c>).
    /// Does nothing when the caret is not inside a table.
    /// </summary>
    TableAlignCenter,

    /// <summary>
    /// Right-aligns the table column the caret is in (<c>---:</c>).
    /// Does nothing when the caret is not inside a table.
    /// </summary>
    TableAlignRight,

    /// <summary>
    /// Selects the next cell of the table the caret is in, adding a row when it was already in
    /// the last one. This is what <see cref="Indent"/> does inside a table, so Tab walks the
    /// grid. Does nothing when the caret is not inside a table.
    /// </summary>
    TableNextCell,

    /// <summary>
    /// Selects the previous cell of the table the caret is in. This is what <see cref="Outdent"/>
    /// does inside a table, so Shift+Tab walks back. Does nothing at the first cell, or when the
    /// caret is not inside a table.
    /// </summary>
    TablePreviousCell
}
