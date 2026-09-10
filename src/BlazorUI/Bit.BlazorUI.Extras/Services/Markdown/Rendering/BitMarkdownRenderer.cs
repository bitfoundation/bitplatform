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

    /// <summary>Creates a renderer that writes its own words in English.</summary>
    public BitMarkdownRenderer(IReadOnlyList<BitMarkdownNodeRenderer> renderers)
        : this(renderers, BitMarkdownTexts.Default)
    {
    }

    /// <summary>Creates a renderer that writes its own words from <paramref name="texts"/>.</summary>
    public BitMarkdownRenderer(IReadOnlyList<BitMarkdownNodeRenderer> renderers, BitMarkdownTexts texts)
    {
        _renderers = renderers;
        Texts = texts ?? BitMarkdownTexts.Default;
    }

    /// <summary>
    /// The words the renderers write themselves - alert titles, footnote back-links, the accessible
    /// names of the regions and controls the markup adds. A renderer reads them from here rather
    /// than hard-coding English.
    /// </summary>
    public BitMarkdownTexts Texts { get; }

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
