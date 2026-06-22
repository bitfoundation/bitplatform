using Bit.BlazorUI.Markdown.Syntax;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.BlazorUI.Markdown.Rendering;

/// <summary>
/// Renders a node into the Blazor render tree. Plugins provide renderers for the
/// node types they introduce.
/// </summary>
public abstract class NodeRenderer
{
    /// <summary>True if this renderer can render <paramref name="node"/>.</summary>
    public abstract bool Accept(MarkdownNode node);

    /// <summary>Writes <paramref name="node"/> to the render tree.</summary>
    public abstract void Write(MarkdownRenderer renderer, RenderTreeBuilder builder, MarkdownNode node);
}

/// <summary>
/// Walks an AST and dispatches each node to the first matching <see cref="NodeRenderer"/>.
/// A fresh instance is used per render pass because it maintains render-tree sequence state.
/// </summary>
public sealed class MarkdownRenderer
{
    private readonly IReadOnlyList<NodeRenderer> _renderers;
    private int _seq;

    public MarkdownRenderer(IReadOnlyList<NodeRenderer> renderers) => _renderers = renderers;

    /// <summary>Returns the next monotonic sequence number for the render tree.</summary>
    public int NextSeq() => _seq++;

    /// <summary>Renders a sequence of nodes.</summary>
    public void WriteNodes(RenderTreeBuilder builder, IEnumerable<MarkdownNode> nodes)
    {
        foreach (var node in nodes)
            WriteNode(builder, node);
    }

    /// <summary>Renders a single node using the matching renderer (last registered wins).</summary>
    public void WriteNode(RenderTreeBuilder builder, MarkdownNode node)
    {
        for (int i = _renderers.Count - 1; i >= 0; i--)
        {
            if (_renderers[i].Accept(node))
            {
                _renderers[i].Write(this, builder, node);
                return;
            }
        }

        throw new InvalidOperationException(
            $"No renderer registered for node type '{node.GetType().Name}'. " +
            "Register a NodeRenderer for it via the pipeline builder.");
    }
}
