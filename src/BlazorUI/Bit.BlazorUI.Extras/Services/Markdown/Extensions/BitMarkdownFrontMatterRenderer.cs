using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.BlazorUI;

/// <summary>
/// Renders a <see cref="BitMarkdownFrontMatterNode"/> as nothing at all. The block describes the
/// document instead of belonging to it, so it is read from the AST rather than displayed.
/// </summary>
public sealed class BitMarkdownFrontMatterRenderer : BitMarkdownNodeRenderer
{
    public override bool Accept(BitMarkdownNode node) => node is BitMarkdownFrontMatterNode;

    public override void Write(BitMarkdownRenderer renderer, RenderTreeBuilder builder, BitMarkdownNode node)
    {
    }
}
