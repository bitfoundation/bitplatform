using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.BlazorUI;

/// <summary>
/// Walks an AST and dispatches each node to a matching <see cref="BitMarkdownViewerNodeRenderer"/>.
/// Renderers are probed in reverse registration order, so the last renderer registered for a
/// node type wins, allowing pipeline extensions to override the core renderers.
/// </summary>
public sealed class BitMarkdownViewerMarkdownRenderer
{
    private readonly IReadOnlyList<BitMarkdownViewerNodeRenderer> _renderers;

    public BitMarkdownViewerMarkdownRenderer(IReadOnlyList<BitMarkdownViewerNodeRenderer> renderers) => _renderers = renderers;

    /// <summary>Renders a sequence of nodes.</summary>
    public void WriteNodes(RenderTreeBuilder builder, IEnumerable<BitMarkdownViewerMarkdownNode> nodes)
    {
        foreach (var node in nodes)
            WriteNode(builder, node);
    }

    /// <summary>Renders a single node using the matching renderer (last registered wins).</summary>
    public void WriteNode(RenderTreeBuilder builder, BitMarkdownViewerMarkdownNode node)
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
            "Register a BitMarkdownViewerNodeRenderer for it via the pipeline builder.");
    }
}
