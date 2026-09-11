namespace Bit.BlazorUI;

/// <summary>Subscript text (<c>H~2~O</c>), rendered as <c>&lt;sub&gt;</c>.</summary>
public sealed class BitMarkdownSubscriptNode : BitMarkdownNode
{
    public List<BitMarkdownNode> Children { get; } = new();
    public override IList<BitMarkdownNode> ChildNodes => Children;
}

/// <summary>Superscript text (<c>x^2^</c>), rendered as <c>&lt;sup&gt;</c>.</summary>
public sealed class BitMarkdownSuperscriptNode : BitMarkdownNode
{
    public List<BitMarkdownNode> Children { get; } = new();
    public override IList<BitMarkdownNode> ChildNodes => Children;
}

/// <summary>Inserted text (<c>++added++</c>), rendered as <c>&lt;ins&gt;</c>.</summary>
public sealed class BitMarkdownInsertedNode : BitMarkdownNode
{
    public List<BitMarkdownNode> Children { get; } = new();
    public override IList<BitMarkdownNode> ChildNodes => Children;
}

/// <summary>Highlighted text (<c>==important==</c>), rendered as <c>&lt;mark&gt;</c>.</summary>
public sealed class BitMarkdownMarkedNode : BitMarkdownNode
{
    public List<BitMarkdownNode> Children { get; } = new();
    public override IList<BitMarkdownNode> ChildNodes => Children;
}
