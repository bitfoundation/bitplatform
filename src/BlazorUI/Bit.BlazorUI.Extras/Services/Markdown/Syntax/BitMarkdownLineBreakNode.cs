namespace Bit.BlazorUI;

/// <summary>A line break. Hard breaks render as <c>&lt;br /&gt;</c>.</summary>
public sealed class BitMarkdownLineBreakNode : BitMarkdownNode
{
    public bool Hard { get; init; }
}
