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
    /// Toggles superscript on the current selection.
    /// </summary>
    Superscript,

    /// <summary>
    /// Toggles subscript on the current selection.
    /// </summary>
    Subscript,

    /// <summary>
    /// Removes inline and block markdown formatting from the selected lines.
    /// </summary>
    ClearFormatting
}
