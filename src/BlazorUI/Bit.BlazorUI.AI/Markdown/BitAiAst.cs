namespace Bit.BlazorUI;

/// <summary>The base of every node in a parsed Markdown document.</summary>
public abstract class BitAiMarkdownNode;

/// <summary>The base of every block-level node.</summary>
public abstract class BitAiMarkdownBlock : BitAiMarkdownNode
{
    /// <summary>
    /// <see langword="true"/> when the block may still grow because the source ended inside it.
    /// </summary>
    /// <remarks>
    /// Renderers use this to defer work that would flicker: an open fenced block is not highlighted,
    /// an open table is not measured. It is the single most important flag for streaming quality.
    /// </remarks>
    public bool IsOpen { get; internal set; }
}

/// <summary>The base of every inline node.</summary>
public abstract class BitAiMarkdownInline : BitAiMarkdownNode;

/// <summary>A parsed document: an ordered list of blocks.</summary>
public sealed class BitAiMarkdownDocument : BitAiMarkdownBlock
{
    /// <summary>The blocks, in order.</summary>
    public List<BitAiMarkdownBlock> Blocks { get; } = [];

    /// <summary>
    /// The footnotes the text cites, in the order they are first cited and numbered from one.
    /// </summary>
    /// <remarks>
    /// A definition nobody cites is dropped, as GitHub does. Renderers list these after the blocks,
    /// each with a link back to the reference that cited it.
    /// </remarks>
    public List<BitAiFootnoteBlock> Footnotes { get; } = [];

    /// <summary>An empty document.</summary>
    public static BitAiMarkdownDocument Empty { get; } = new();
}

/// <summary>A run of text terminated by a blank line or another block.</summary>
public sealed class BitAiParagraphBlock : BitAiMarkdownBlock
{
    /// <summary>The inline content.</summary>
    public List<BitAiMarkdownInline> Inlines { get; } = [];
}

/// <summary>A heading.</summary>
/// <param name="level">The heading level, 1 through 6.</param>
public sealed class BitAiHeadingBlock(int level) : BitAiMarkdownBlock
{
    /// <summary>The heading level, 1 through 6.</summary>
    public int Level { get; } = Math.Clamp(level, 1, 6);

    /// <summary>The inline content.</summary>
    public List<BitAiMarkdownInline> Inlines { get; } = [];

    /// <summary>
    /// A slug derived from the text (<c>bit-ai-h-&lt;slug&gt;</c>), unique within the document, used as an anchor id.
    /// <see cref="BitAiMarkdownView"/> renders it after its own id, so two views never share one.
    /// </summary>
    public string? Id { get; internal set; }
}

/// <summary>A code block, fenced or indented.</summary>
public sealed class BitAiCodeBlock : BitAiMarkdownBlock
{
    /// <summary>The language identifier from the info string, when there was one.</summary>
    public string? Language { get; internal set; }

    /// <summary>The rest of the info string after the language.</summary>
    public string? Meta { get; internal set; }

    /// <summary>The source, without the fences.</summary>
    public string Code { get; internal set; } = string.Empty;

    /// <summary><see langword="true"/> for a fenced block, <see langword="false"/> for an indented one.</summary>
    public bool IsFenced { get; internal set; }
}

/// <summary>A bullet or numbered list.</summary>
public sealed class BitAiListBlock : BitAiMarkdownBlock
{
    /// <summary><see langword="true"/> for a numbered list.</summary>
    public bool IsOrdered { get; internal set; }

    /// <summary>The first number of an ordered list.</summary>
    public int Start { get; internal set; } = 1;

    /// <summary>
    /// <see langword="true"/> when items are separated by blank lines and should be spaced apart.
    /// </summary>
    public bool IsLoose { get; internal set; }

    /// <summary>The items, in order.</summary>
    public List<BitAiListItemBlock> Items { get; } = [];
}

/// <summary>One item of a list.</summary>
public sealed class BitAiListItemBlock : BitAiMarkdownBlock
{
    /// <summary>
    /// The checkbox state of a task-list item, or <see langword="null"/> when the item has no checkbox.
    /// </summary>
    public bool? IsChecked { get; internal set; }

    /// <summary>The item content, which may contain nested blocks.</summary>
    public List<BitAiMarkdownBlock> Blocks { get; } = [];
}

/// <summary>A block quote.</summary>
public sealed class BitAiQuoteBlock : BitAiMarkdownBlock
{
    /// <summary>The quoted content.</summary>
    public List<BitAiMarkdownBlock> Blocks { get; } = [];
}

/// <summary>How a table column is aligned.</summary>
public enum BitAiColumnAlignment
{
    /// <summary>No alignment specified.</summary>
    None = 0,

    /// <summary>Left aligned.</summary>
    Left = 1,

    /// <summary>Centre aligned.</summary>
    Center = 2,

    /// <summary>Right aligned.</summary>
    Right = 3,
}

/// <summary>A pipe table.</summary>
public sealed class BitAiTableBlock : BitAiMarkdownBlock
{
    /// <summary>The header cells.</summary>
    public List<List<BitAiMarkdownInline>> Header { get; } = [];

    /// <summary>The body rows.</summary>
    public List<List<List<BitAiMarkdownInline>>> Rows { get; } = [];

    /// <summary>The alignment of each column.</summary>
    public List<BitAiColumnAlignment> Alignments { get; } = [];
}

/// <summary>A horizontal rule.</summary>
public sealed class BitAiThematicBreakBlock : BitAiMarkdownBlock;

/// <summary>A footnote the text cites, listed on <see cref="BitAiMarkdownDocument.Footnotes"/>.</summary>
/// <param name="label">The label the source used, without the caret.</param>
/// <param name="number">The footnote's number, which is its position in the list.</param>
public sealed class BitAiFootnoteBlock(string label, int number) : BitAiMarkdownBlock
{
    /// <summary>The label the source used, without the caret.</summary>
    public string Label { get; } = label;

    /// <summary>The footnote's number, which is its position in the list.</summary>
    public int Number { get; } = number;

    /// <summary>How many references cite this footnote, so each can get its own back-link.</summary>
    public int ReferenceCount { get; internal set; }

    /// <summary>The footnote content, which may contain several blocks.</summary>
    public List<BitAiMarkdownBlock> Blocks { get; } = [];
}

/// <summary>A displayed mathematical expression.</summary>
/// <param name="expression">The expression source, without delimiters.</param>
public sealed class BitAiMathBlock(string expression) : BitAiMarkdownBlock
{
    /// <summary>The expression source, without delimiters.</summary>
    public string Expression { get; internal set; } = expression;
}

/// <summary>Raw HTML found in the source.</summary>
/// <param name="html">The raw markup.</param>
/// <remarks>
/// Never rendered as markup unless the host opts in. Model output is untrusted input, and treating
/// it as HTML by default is how a chat UI grows a cross-site scripting hole.
/// </remarks>
public sealed class BitAiHtmlBlock(string html) : BitAiMarkdownBlock
{
    /// <summary>The raw markup.</summary>
    public string Html { get; internal set; } = html;
}
