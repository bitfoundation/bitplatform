using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.BlazorUI;

/// <summary>Strikethrough text (GFM), rendered as <c>&lt;del&gt;</c>.</summary>
public sealed class StrikethroughNode : MarkdownNode
{
    public List<MarkdownNode> Children { get; } = new();
    public override IList<MarkdownNode> ChildNodes => Children;
}

/// <summary>Delimiter processor for <c>~~</c> strikethrough runs.</summary>
public sealed class StrikethroughDelimiterProcessor : DelimiterProcessor
{
    public override char[] Characters => new[] { '~' };
    public override int MinRunLength => 2;

    public override (bool canOpen, bool canClose) GetFlanking(
        char c, bool leftFlanking, bool rightFlanking, char prev, char next)
        => (leftFlanking, rightFlanking);

    public override int TryCreate(char c, int openLength, int closeLength,
        List<MarkdownNode> children, out MarkdownNode? node)
    {
        // GFM strikethrough requires runs of two on both sides.
        if (openLength < 2 || closeLength < 2)
        {
            node = null;
            return 0;
        }
        var strike = new StrikethroughNode();
        strike.Children.AddRange(children);
        node = strike;
        return 2;
    }
}

/// <summary>Renders <see cref="StrikethroughNode"/>.</summary>
public sealed class StrikethroughRenderer : NodeRenderer
{
    public override bool Accept(MarkdownNode node) => node is StrikethroughNode;

    public override void Write(MarkdownRenderer r, RenderTreeBuilder b, MarkdownNode node)
    {
        b.OpenElement(r.NextSeq(), "del");
        r.WriteNodes(b, ((StrikethroughNode)node).Children);
        b.CloseElement();
    }
}

/// <summary>Enables <c>~~strikethrough~~</c> (GFM).</summary>
public sealed class StrikethroughExtension : IBitMarkdownExtension
{
    public void Setup(BitMarkdownPipelineBuilder builder)
    {
        builder.DelimiterProcessors.Add(new StrikethroughDelimiterProcessor());
        builder.Renderers.Add(new StrikethroughRenderer());
    }
}
