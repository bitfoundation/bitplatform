using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.BlazorUI;

/// <summary>
/// Renders an expanded abbreviation as <c>&lt;abbr title="..."&gt;</c>, and a definition as
/// nothing at all.
/// </summary>
public sealed class BitMarkdownAbbreviationRenderer : BitMarkdownNodeRenderer
{
    public override bool Accept(BitMarkdownNode node) => node is
        BitMarkdownAbbreviationNode or BitMarkdownAbbreviationDefinitionNode;

    // Fixed literal sequence numbers (see BitMarkdownCoreRenderer for the rationale).
    public override void Write(BitMarkdownRenderer r, RenderTreeBuilder b, BitMarkdownNode node)
    {
        if (node is not BitMarkdownAbbreviationNode abbreviation)
        {
            // A definition declares a term; it is not part of the rendered document. The AST
            // processor normally removes it before this point.
            return;
        }

        b.OpenElement(0, "abbr");
        b.AddAttribute(1, "title", abbreviation.Title);
        b.AddContent(2, abbreviation.Text);
        b.CloseElement();
    }
}
