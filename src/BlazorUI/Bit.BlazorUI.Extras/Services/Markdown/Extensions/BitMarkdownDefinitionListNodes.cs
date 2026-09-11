namespace Bit.BlazorUI;

/// <summary>
/// A definition list: terms with the definitions that belong to them, rendered as
/// <c>&lt;dl&gt;</c>. Its children alternate <see cref="BitMarkdownDefinitionTermNode"/> and
/// <see cref="BitMarkdownDefinitionDescriptionNode"/>, in the order they were written.
/// </summary>
public sealed class BitMarkdownDefinitionListNode : BitMarkdownNode
{
    public List<BitMarkdownNode> Children { get; } = new();
    public override IList<BitMarkdownNode> ChildNodes => Children;
}

/// <summary>A term being defined, rendered as <c>&lt;dt&gt;</c>.</summary>
public sealed class BitMarkdownDefinitionTermNode : BitMarkdownNode
{
    public List<BitMarkdownNode> Inlines { get; } = new();
    public override IList<BitMarkdownNode> ChildNodes => Inlines;
}

/// <summary>One definition of the term above it, rendered as <c>&lt;dd&gt;</c>.</summary>
public sealed class BitMarkdownDefinitionDescriptionNode : BitMarkdownNode
{
    public List<BitMarkdownNode> Children { get; } = new();
    public override IList<BitMarkdownNode> ChildNodes => Children;
}
