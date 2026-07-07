namespace Bit.BlazorUI;

/// <summary>A fenced or indented code block, rendered verbatim.</summary>
public sealed class BitMarkdownViewerCodeBlockNode : BitMarkdownViewerMarkdownNode
{
    /// <summary>The info string of a fenced block (e.g. the language), or null.</summary>
    public string? Info { get; init; }
    public string Content { get; init; } = string.Empty;
}
