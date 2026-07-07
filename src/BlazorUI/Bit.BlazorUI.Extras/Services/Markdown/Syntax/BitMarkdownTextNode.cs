namespace Bit.BlazorUI;

/// <summary>Plain literal text.</summary>
public sealed class BitMarkdownTextNode : BitMarkdownNode
{
    public string Text { get; set; } = string.Empty;
    public BitMarkdownTextNode() { }
    public BitMarkdownTextNode(string text) => Text = text;
}
