namespace Bit.BlazorUI;

/// <summary>
/// The metadata block a document may open with: a run of lines fenced by <c>---</c> (YAML) or
/// <c>+++</c> (TOML). It carries information about the document rather than content of it, so it
/// renders nothing; the raw text is kept here for the host to read and parse with whatever
/// serializer it already uses.
/// </summary>
/// <remarks>
/// Without the front matter extension enabled, a leading <c>---</c> is ordinary Markdown: the
/// first line becomes a thematic break and the metadata lines become a setext heading, which is
/// how a real <c>.md</c> file from a static-site generator renders as garbage. Enabling the
/// extension is what makes those files render as their authors meant them to.
/// </remarks>
public sealed class BitMarkdownFrontMatterNode : BitMarkdownNode
{
    /// <summary>The fence that opened the block, either <c>---</c> or <c>+++</c>.</summary>
    public string Fence { get; init; } = "---";

    /// <summary>The raw text between the fences, with the line endings normalized to <c>\n</c>.</summary>
    public string Text { get; init; } = string.Empty;

    /// <summary>True when the block was fenced with <c>+++</c>, i.e. TOML rather than YAML.</summary>
    public bool IsToml => Fence.Length > 0 && Fence[0] == '+';

    /// <summary>
    /// Returns the document's front matter block, or <c>null</c> when it has none. Only a block at
    /// the very top of the document is front matter, so this reads the first child rather than
    /// searching the tree.
    /// </summary>
    public static BitMarkdownFrontMatterNode? Find(BitMarkdownDocumentNode document)
    {
        ArgumentNullException.ThrowIfNull(document);
        return document.Children.Count > 0 ? document.Children[0] as BitMarkdownFrontMatterNode : null;
    }
}
