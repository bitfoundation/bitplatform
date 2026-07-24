namespace Bit.BlazorUI;

/// <summary>A GitHub task-list checkbox at the start of a list item.</summary>
public sealed class BitMarkdownTaskCheckboxNode : BitMarkdownNode
{
    public bool Checked { get; init; }
}
