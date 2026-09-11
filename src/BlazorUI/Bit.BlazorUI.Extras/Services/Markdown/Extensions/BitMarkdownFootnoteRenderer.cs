using System.Globalization;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.BlazorUI;

/// <summary>
/// Renders footnote references, the footnotes section and its entries, using the markup
/// GitHub emits so the same stylesheet rules apply.
/// </summary>
public sealed class BitMarkdownFootnoteRenderer : BitMarkdownNodeRenderer
{
    public override bool Accept(BitMarkdownNode node) => node is
        BitMarkdownFootnoteReferenceNode or BitMarkdownFootnotesNode or BitMarkdownFootnoteDefinitionNode;

    // Fixed literal sequence numbers (see BitMarkdownCoreRenderer for the rationale).
    public override void Write(BitMarkdownRenderer r, RenderTreeBuilder b, BitMarkdownNode node)
    {
        switch (node)
        {
            case BitMarkdownFootnoteReferenceNode reference:
                string referenceScope = Scope(reference.IdScope);
                b.OpenElement(0, "sup");
                b.AddAttribute(1, "class", "footnote-ref");
                b.OpenElement(2, "a");
                b.AddAttribute(3, "href", $"#{referenceScope}fn-{reference.Number}");
                b.AddAttribute(4, "id", FootnoteRefId(reference));
                b.AddAttribute(5, "aria-describedby", $"{referenceScope}footnotes");
                b.AddContent(6, reference.Number.ToString());
                b.CloseElement();
                b.CloseElement();
                break;

            case BitMarkdownFootnotesNode footnotes:
                b.OpenElement(7, "section");
                b.AddAttribute(8, "class", "footnotes");
                b.AddAttribute(9, "id", $"{Scope(footnotes.IdScope)}footnotes");
                b.AddAttribute(10, "aria-label", r.Texts.Footnotes);
                b.OpenElement(11, "hr");
                b.CloseElement();
                b.OpenElement(12, "ol");
                r.WriteNodes(b, footnotes.Children);
                b.CloseElement();
                b.CloseElement();
                break;

            case BitMarkdownFootnoteDefinitionNode definition:
                b.OpenElement(13, "li");
                b.AddAttribute(14, "id", $"{Scope(definition.IdScope)}fn-{definition.Number}");
                b.AddAttribute(15, "class", "footnote-item");
                r.WriteNodes(b, definition.Children);
                // One back-link per reference, so a note cited several times can return to
                // each of them.
                for (int i = 1; i <= definition.ReferenceCount; i++)
                {
                    b.AddContent(16, " ");
                    b.OpenElement(17, "a");
                    b.AddAttribute(18, "href", $"#{Scope(definition.IdScope)}fnref-{definition.Number}{(i > 1 ? "-" + i : string.Empty)}");
                    b.AddAttribute(19, "class", "footnote-backref");
                    b.AddAttribute(20, "aria-label", definition.ReferenceCount > 1
                        ? string.Format(CultureInfo.CurrentCulture, r.Texts.FootnoteBackReferenceOccurrence, definition.Number, i)
                        : string.Format(CultureInfo.CurrentCulture, r.Texts.FootnoteBackReference, definition.Number));
                    b.AddContent(21, i > 1 ? $"↩︎{i}" : "↩︎");
                    b.CloseElement();
                }
                b.CloseElement();
                break;
        }
    }

    private static string FootnoteRefId(BitMarkdownFootnoteReferenceNode reference)
        => reference.Occurrence > 1
            ? $"{Scope(reference.IdScope)}fnref-{reference.Number}-{reference.Occurrence}"
            : $"{Scope(reference.IdScope)}fnref-{reference.Number}";

    // Every id the section emits carries the host viewer's own prefix, so two viewers on one
    // page keep their links pointing at their own notes instead of at each other's.
    private static string Scope(string? idScope)
        => string.IsNullOrEmpty(idScope) ? string.Empty : idScope + "-";
}
