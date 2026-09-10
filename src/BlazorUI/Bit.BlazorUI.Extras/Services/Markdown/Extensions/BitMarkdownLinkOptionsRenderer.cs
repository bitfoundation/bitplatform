using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.BlazorUI;

/// <summary>
/// Renders links with the <c>target</c> and <c>rel</c> the host chose, overriding the core
/// renderer's defaults (a new tab and <c>noopener noreferrer</c> for anything external).
/// </summary>
public sealed class BitMarkdownLinkOptionsRenderer : BitMarkdownNodeRenderer
{
    /// <summary>Creates a renderer applying the supplied policy.</summary>
    public BitMarkdownLinkOptionsRenderer(
        BitMarkdownLinkTarget externalTarget,
        string? externalRel,
        BitMarkdownLinkTarget internalTarget,
        string? internalRel)
    {
        ExternalTarget = externalTarget;
        ExternalRel = externalRel;
        InternalTarget = internalTarget;
        InternalRel = internalRel;
    }

    /// <summary>Where a link to another origin opens.</summary>
    public BitMarkdownLinkTarget ExternalTarget { get; }

    /// <summary>The <c>rel</c> given to a link to another origin, or <c>null</c> for none.</summary>
    public string? ExternalRel { get; }

    /// <summary>Where a link within the document or the site opens.</summary>
    public BitMarkdownLinkTarget InternalTarget { get; }

    /// <summary>The <c>rel</c> given to a link within the document or the site, or <c>null</c> for none.</summary>
    public string? InternalRel { get; }

    public override bool Accept(BitMarkdownNode node) => node is BitMarkdownLinkNode;

    // Fixed literal sequence numbers (see BitMarkdownCoreRenderer for the rationale).
    public override void Write(BitMarkdownRenderer r, RenderTreeBuilder b, BitMarkdownNode node)
    {
        var link = (BitMarkdownLinkNode)node;
        bool external = IsExternal(link.Url);

        b.OpenElement(0, "a");
        if (string.IsNullOrEmpty(link.Url) is false)
        {
            b.AddAttribute(1, "href", link.Url);

            var target = external ? ExternalTarget : InternalTarget;
            if (target != BitMarkdownLinkTarget.Self)
                b.AddAttribute(2, "target", "_" + target.ToString().ToLowerInvariant());

            var rel = external ? ExternalRel : InternalRel;
            if (string.IsNullOrWhiteSpace(rel) is false)
                b.AddAttribute(3, "rel", rel);
        }
        if (string.IsNullOrEmpty(link.Title) is false)
            b.AddAttribute(4, "title", link.Title);

        r.WriteNodes(b, link.Children);
        b.CloseElement();
    }

    private static bool IsExternal(string url) =>
        url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
        url.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
}
