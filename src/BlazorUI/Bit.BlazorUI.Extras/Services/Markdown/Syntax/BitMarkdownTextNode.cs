namespace Bit.BlazorUI;

/// <summary>Plain literal text.</summary>
public sealed class BitMarkdownViewerTextNode : BitMarkdownViewerMarkdownNode
{
    public string Text { get; set; } = string.Empty;
    public BitMarkdownViewerTextNode() { }
    public BitMarkdownViewerTextNode(string text) => Text = text;
}
