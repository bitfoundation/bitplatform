using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.BlazorUI;

/// <summary>Renders a definition list as <c>&lt;dl&gt;</c> / <c>&lt;dt&gt;</c> / <c>&lt;dd&gt;</c>.</summary>
public sealed class BitMarkdownDefinitionListRenderer : BitMarkdownNodeRenderer
{
    public override bool Accept(BitMarkdownNode node) => node is
        BitMarkdownDefinitionListNode or BitMarkdownDefinitionTermNode or BitMarkdownDefinitionDescriptionNode;

    // Fixed literal sequence numbers (see BitMarkdownCoreRenderer for the rationale).
    public override void Write(BitMarkdownRenderer r, RenderTreeBuilder b, BitMarkdownNode node)
    {
        switch (node)
        {
            case BitMarkdownDefinitionListNode list:
                b.OpenElement(0, "dl");
                r.WriteNodes(b, list.Children);
                b.CloseElement();
                break;

            case BitMarkdownDefinitionTermNode term:
                b.OpenElement(1, "dt");
                if (r.AutoDirection)
                    b.AddAttribute(3, "dir", "auto");
                r.WriteNodes(b, term.Inlines);
                b.CloseElement();
                break;

            case BitMarkdownDefinitionDescriptionNode description:
                b.OpenElement(2, "dd");
                // A one-paragraph definition writes its text straight into the <dd>, so the <dd> is what
                // takes the direction of it under automatic direction.
                if (r.AutoDirection)
                    b.AddAttribute(4, "dir", "auto");
                // A definition of a single paragraph reads as part of the list rather than as a
                // block of its own, exactly like a tight list item.
                if (description.Children.Count == 1 && description.Children[0] is BitMarkdownParagraphNode only)
                {
                    r.WriteNodes(b, only.Inlines);
                }
                else
                {
                    r.WriteNodes(b, description.Children);
                }
                b.CloseElement();
                break;
        }
    }
}
