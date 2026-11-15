namespace Bit.BlazorUI;

/// <summary>
/// Which Markdown extensions to enable and how to treat untrusted markup.
/// </summary>
/// <remarks>
/// The defaults are what an AI chat surface wants: GitHub-flavoured extensions on, raw HTML off.
/// Model output is untrusted, so rendering embedded markup is opt-in rather than opt-out.
/// </remarks>
public sealed record BitAiMarkdownOptions
{
    /// <summary>The default options.</summary>
    public static readonly BitAiMarkdownOptions Default = new();

    /// <summary>Parse pipe tables.</summary>
    public bool Tables { get; init; } = true;

    /// <summary>Parse strikethrough written with double tildes.</summary>
    public bool Strikethrough { get; init; } = true;

    /// <summary>Parse task-list checkboxes in list items.</summary>
    public bool TaskLists { get; init; } = true;

    /// <summary>Turn bare URLs into links.</summary>
    public bool AutoLinks { get; init; } = true;

    /// <summary>Parse dollar-delimited mathematics.</summary>
    public bool Math { get; init; } = true;

    /// <summary>
    /// Turn bracketed digit runs such as <c>[3]</c> into citation markers.
    /// </summary>
    public bool CitationMarkers { get; init; } = true;

    /// <summary>
    /// Keep raw HTML as markup instead of escaping it.
    /// </summary>
    /// <remarks>
    /// Leave this off unless the Markdown comes from a source you trust as much as your own code.
    /// A model can be talked into emitting a script tag.
    /// </remarks>
    public bool AllowRawHtml { get; init; }

    /// <summary>Give headings anchor identifiers derived from their text.</summary>
    /// <remarks>
    /// The parser names a heading <c>bit-ai-h-&lt;slug&gt;</c>, numbered <c>-2</c>, <c>-3</c>, ... when the same text
    /// repeats in a document, so the ids cannot clobber the host page's own anchors (<see cref="BitAiHeadingBlock.Id"/>).
    /// <see cref="BitAiMarkdownView"/> renders each one after its own id (<c>&lt;view id&gt;-bit-ai-h-&lt;slug&gt;</c>), so the
    /// same heading in two messages of a transcript does not give the page two elements of one id.
    /// </remarks>
    public bool HeadingIds { get; init; } = true;

    /// <summary>
    /// Hide a trailing unmatched emphasis or code delimiter while the text is still streaming.
    /// </summary>
    /// <remarks>
    /// Without this, a response containing bold text flashes a literal asterisk before the closing
    /// one arrives. With it, the delimiter stays invisible until it either closes or the stream ends.
    /// A heading still being written hides them as a paragraph does, and with <see cref="Tables"/>
    /// on, the lines starting with a pipe that end the text so far - the header row of a table whose
    /// delimiter row has not arrived yet - are held back the same way, rather than shown as text and
    /// then taken back into the table.
    /// </remarks>
    public bool HideDanglingDelimiters { get; init; } = true;

    /// <summary>
    /// How deeply blocks and inline spans may nest before further markers are read as literal text.
    /// </summary>
    /// <remarks>
    /// Bounds both the parser's recursion and the depth of the tree a renderer has to walk. A line
    /// of ten thousand <c>&gt;</c> characters, or as many nested brackets, must not take down the
    /// process. The default of 32 is far deeper than any document a person or a model writes.
    /// </remarks>
    public int MaxNestingDepth { get; init; } = 32;

    /// <summary>
    /// Treat a single newline inside a paragraph as a line break, as chat clients often do.
    /// </summary>
    /// <remarks>
    /// CommonMark renders a soft break as a space. Models fine-tuned on chat transcripts tend to
    /// write one line per sentence and mean the line breaks, so a host may prefer to keep them.
    /// </remarks>
    public bool SoftBreaksAsHardBreaks { get; init; }

    /// <summary>
    /// Parse footnotes: <c>[^1]</c> in the text with a matching <c>[^1]: note</c> definition.
    /// </summary>
    /// <remarks>
    /// Referenced footnotes are collected on <see cref="BitAiMarkdownDocument.Footnotes"/> in the order
    /// they are first cited, numbered from one, and are not left in the block flow.
    /// </remarks>
    public bool Footnotes { get; init; } = true;
}
