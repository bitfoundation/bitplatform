using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.BlazorUI;

/// <summary>
/// Renders a node into the Blazor render tree. Plugins provide renderers for the
/// node types they introduce.
/// </summary>
public abstract class BitMarkdownViewerNodeRenderer
{
    /// <summary>True if this renderer can render <paramref name="node"/>.</summary>
    public abstract bool Accept(BitMarkdownViewerMarkdownNode node);

    /// <summary>Writes <paramref name="node"/> to the render tree.</summary>
    public abstract void Write(BitMarkdownViewerMarkdownRenderer renderer, RenderTreeBuilder builder, BitMarkdownViewerMarkdownNode node);
}
