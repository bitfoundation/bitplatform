using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.BlazorUI;

/// <summary>
/// Walks an AST and dispatches each node to a matching <see cref="BitMarkdownNodeRenderer"/>.
/// Renderers are probed in reverse registration order, so the last renderer registered for a
/// node type wins, allowing pipeline extensions to override the core renderers.
/// </summary>
public sealed class BitMarkdownRenderer
{
    private readonly IReadOnlyList<BitMarkdownNodeRenderer> _renderers;

    public BitMarkdownRenderer(IReadOnlyList<BitMarkdownNodeRenderer> renderers) => _renderers = renderers;

    /// <summary>Renders a sequence of nodes.</summary>
    public void WriteNodes(RenderTreeBuilder builder, IEnumerable<BitMarkdownNode> nodes)
    {
        foreach (var node in nodes)
            WriteNode(builder, node);
    }

    /// <summary>Renders a single node using the matching renderer (last registered wins).</summary>
    public void WriteNode(RenderTreeBuilder builder, BitMarkdownNode node)
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
            "Register a BitMarkdownNodeRenderer for it via the pipeline builder.");
    }
}
