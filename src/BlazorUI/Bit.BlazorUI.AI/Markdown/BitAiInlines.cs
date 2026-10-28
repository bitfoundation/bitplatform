namespace Bit.BlazorUI;

/// <summary>Literal text.</summary>
/// <param name="text">The text.</param>
public sealed class BitAiTextInline(string text) : BitAiMarkdownInline
{
    /// <summary>The text.</summary>
    public string Text { get; internal set; } = text;

    /// <inheritdoc />
    public override string ToString() => Text;
}

/// <summary>Emphasised text.</summary>
/// <param name="isStrong"><see langword="true"/> for strong emphasis.</param>
public sealed class BitAiEmphasisInline(bool isStrong) : BitAiMarkdownInline
{
    /// <summary><see langword="true"/> for strong emphasis, <see langword="false"/> for ordinary.</summary>
    public bool IsStrong { get; } = isStrong;

    /// <summary>The emphasised content.</summary>
    public List<BitAiMarkdownInline> Inlines { get; } = [];
}

/// <summary>Struck-through text.</summary>
public sealed class BitAiStrikethroughInline : BitAiMarkdownInline
{
    /// <summary>The struck content.</summary>
    public List<BitAiMarkdownInline> Inlines { get; } = [];
}

/// <summary>Inline code.</summary>
/// <param name="code">The code.</param>
public sealed class BitAiCodeInline(string code) : BitAiMarkdownInline
{
    /// <summary>The code.</summary>
    public string Code { get; } = code;
}

/// <summary>A hyperlink.</summary>
/// <param name="url">The destination.</param>
/// <param name="title">The title attribute, when present.</param>
public sealed class BitAiLinkInline(string url, string? title = null) : BitAiMarkdownInline
{
    /// <summary>The destination.</summary>
    public string Url { get; } = url;

    /// <summary>The title attribute, when present.</summary>
    public string? Title { get; } = title;

    /// <summary>The link text.</summary>
    public List<BitAiMarkdownInline> Inlines { get; } = [];

    /// <summary><see langword="true"/> when the source was a bare URL rather than bracketed.</summary>
    public bool IsAutoLink { get; internal set; }
}

/// <summary>An image.</summary>
/// <param name="url">The image source.</param>
/// <param name="alt">The alternative text.</param>
/// <param name="title">The title attribute, when present.</param>
public sealed class BitAiImageInline(string url, string alt, string? title = null) : BitAiMarkdownInline
{
    /// <summary>The image source.</summary>
    public string Url { get; } = url;

    /// <summary>The alternative text.</summary>
    public string Alt { get; } = alt;

    /// <summary>The title attribute, when present.</summary>
    public string? Title { get; } = title;
}

/// <summary>A line break inside a paragraph.</summary>
/// <param name="isHard"><see langword="true"/> when the source forced the break.</param>
public sealed class BitAiLineBreakInline(bool isHard) : BitAiMarkdownInline
{
    /// <summary><see langword="true"/> when the source forced the break with a backslash or two spaces.</summary>
    public bool IsHard { get; } = isHard;
}

/// <summary>An inline mathematical expression.</summary>
/// <param name="expression">The expression source, without delimiters.</param>
public sealed class BitAiMathInline(string expression) : BitAiMarkdownInline
{
    /// <summary>The expression source, without delimiters.</summary>
    public string Expression { get; } = expression;
}

/// <summary>
/// A numbered citation marker, produced from a source reference in the text.
/// </summary>
/// <param name="number">The marker number.</param>
/// <param name="citationId">The citation this marker points at, when known.</param>
/// <remarks>
/// Emitted for bracketed digit runs such as <c>[3]</c> when citation parsing is enabled, so the
/// renderer can turn them into interactive source chips instead of literal brackets.
/// </remarks>
public sealed class BitAiCitationMarkerInline(int number, string? citationId = null) : BitAiMarkdownInline
{
    /// <summary>The marker number.</summary>
    public int Number { get; } = number;

    /// <summary>The citation this marker points at, when known.</summary>
    public string? CitationId { get; internal set; } = citationId;
}

/// <summary>A reference to a footnote, written <c>[^label]</c>.</summary>
/// <param name="label">The label, without the caret.</param>
public sealed class BitAiFootnoteReferenceInline(string label) : BitAiMarkdownInline
{
    /// <summary>The label, without the caret.</summary>
    public string Label { get; } = label;

    /// <summary>The footnote's number, assigned in the order footnotes are first cited.</summary>
    public int Number { get; internal set; }

    /// <summary>
    /// Which of the footnote's references this is, from one, so a renderer can give each reference
    /// its own anchor and the footnote a back-link to each.
    /// </summary>
    public int Ordinal { get; internal set; }
}

/// <summary>Raw inline HTML found in the source.</summary>
/// <param name="html">The raw markup.</param>
/// <remarks>
/// An element closed later in the same text is one inline from its opening tag to its closing one, content included
/// (<c>&lt;kbd&gt;Ctrl&lt;/kbd&gt;</c>), so the markup of each inline stands on its own; a tag the text does not close, a
/// closing tag nothing opened, a comment and a declaration are one each.
/// </remarks>
public sealed class BitAiRawHtmlInline(string html) : BitAiMarkdownInline
{
    /// <summary>The raw markup.</summary>
    public string Html { get; } = html;
}
