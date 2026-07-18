namespace Bit.BlazorUI;

/// <summary>Inline code span.</summary>
public sealed class BitMarkdownCodeSpanNode : BitMarkdownNode
{
    public string Content { get; init; } = string.Empty;
}
