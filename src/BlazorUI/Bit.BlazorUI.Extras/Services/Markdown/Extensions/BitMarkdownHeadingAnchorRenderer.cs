using System.Globalization;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.BlazorUI;

/// <summary>
/// Renders a <see cref="BitMarkdownHeadingAnchorNode"/> as a permalink. The glyph itself is
/// hidden from assistive technology and the link is named after its heading instead, so a screen
/// reader announces "Permalink to Installation" rather than the character.
/// </summary>
public sealed class BitMarkdownHeadingAnchorRenderer : BitMarkdownNodeRenderer
{
    public override bool Accept(BitMarkdownNode node) => node is BitMarkdownHeadingAnchorNode;

    // Fixed literal sequence numbers (see BitMarkdownCoreRenderer for the rationale).
    public override void Write(BitMarkdownRenderer r, RenderTreeBuilder b, BitMarkdownNode node)
    {
        var anchor = (BitMarkdownHeadingAnchorNode)node;

        b.OpenElement(0, "a");
        b.AddAttribute(1, "class", "bit-mdv-anchor");
        b.AddAttribute(2, "href", "#" + anchor.Id);
        b.AddAttribute(3, "aria-label", string.IsNullOrEmpty(anchor.HeadingText)
            ? r.Texts.PermalinkToSection
            : string.Format(CultureInfo.CurrentCulture, r.Texts.PermalinkTo, anchor.HeadingText));
        b.OpenElement(4, "span");
        b.AddAttribute(5, "aria-hidden", "true");
        b.AddContent(6, "#");
        b.CloseElement();
        b.CloseElement();
    }
}
