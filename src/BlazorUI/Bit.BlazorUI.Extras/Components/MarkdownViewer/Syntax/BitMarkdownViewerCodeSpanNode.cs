namespace Bit.BlazorUI;

/// <summary>Inline code span.</summary>
public sealed class BitMarkdownViewerCodeSpanNode : BitMarkdownViewerMarkdownNode
{
    public string Content { get; init; } = string.Empty;
}
