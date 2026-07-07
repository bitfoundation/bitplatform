using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.BlazorUI;

/// <summary>
/// Renders a node into the Blazor render tree. Plugins provide renderers for the
/// node types they introduce.
/// </summary>
public abstract class BitMarkdownNodeRenderer
{
    /// <summary>True if this renderer can render <paramref name="node"/>.</summary>
    public abstract bool Accept(BitMarkdownNode node);

    /// <summary>Writes <paramref name="node"/> to the render tree.</summary>
    public abstract void Write(BitMarkdownRenderer renderer, RenderTreeBuilder builder, BitMarkdownNode node);
}
