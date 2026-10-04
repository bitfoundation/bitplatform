using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.BlazorUI;

/// <summary>
/// Hands the node types a viewer has a template for to that template instead of to the renderer
/// that would otherwise draw them, so a host can put its own component in the middle of a rendered
/// document - a highlighter around a code block, a lightbox around an image, a router-aware anchor
/// around a link - without writing a renderer or a pipeline of its own.
/// </summary>
/// <remarks>
/// This renderer belongs to one viewer rather than to a pipeline: the templates are that viewer's
/// parameters, and it reads them at the moment it draws, so a template supplied on a later render
/// takes effect without anything being rebuilt.
/// </remarks>
internal sealed class BitMarkdownViewerTemplateRenderer : BitMarkdownNodeRenderer
{
    private readonly BitMarkdownViewer _viewer;

    public BitMarkdownViewerTemplateRenderer(BitMarkdownViewer viewer) => _viewer = viewer;

    public override bool Accept(BitMarkdownNode node) => node switch
    {
        BitMarkdownCodeBlockNode => _viewer.CodeBlockTemplate is not null,
        BitMarkdownImageNode => _viewer.ImageTemplate is not null,
        BitMarkdownLinkNode => _viewer.LinkTemplate is not null,
        _ => false
    };

    // Fixed literal sequence numbers (see BitMarkdownCoreRenderer for the rationale).
    //
    // Each template's output sits in a display: contents wrapper the stylesheet stops at, so the rules
    // written for the renderers' own markup - and the reset under them - never reach a component the
    // host put into the document. The wrapper is a span where the node is inline content, since a div
    // may not sit in a paragraph.
    public override void Write(BitMarkdownRenderer renderer, RenderTreeBuilder builder, BitMarkdownNode node)
    {
        switch (node)
        {
            case BitMarkdownCodeBlockNode code:
                builder.OpenElement(0, "div");
                builder.AddAttribute(1, "class", TemplateClass);
                builder.AddContent(2, _viewer.CodeBlockTemplate, code);
                builder.CloseElement();
                break;

            case BitMarkdownImageNode image:
                builder.OpenElement(3, "span");
                builder.AddAttribute(4, "class", TemplateClass);
                builder.AddContent(5, _viewer.ImageTemplate, image);
                builder.CloseElement();
                break;

            case BitMarkdownLinkNode link:
                builder.OpenElement(6, "span");
                builder.AddAttribute(7, "class", TemplateClass);
                builder.AddContent(8, _viewer.LinkTemplate, link);
                builder.CloseElement();
                break;
        }
    }

    internal const string TemplateClass = "bit-mdv-tpl";
}
