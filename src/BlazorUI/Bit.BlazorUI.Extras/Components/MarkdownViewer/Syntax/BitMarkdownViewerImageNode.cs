namespace Bit.BlazorUI;

/// <summary>An image.</summary>
public sealed class BitMarkdownViewerImageNode : BitMarkdownViewerMarkdownNode
{
    public string Url { get; init; } = string.Empty;
    public string? Title { get; init; }
    public string Alt { get; init; } = string.Empty;
}
