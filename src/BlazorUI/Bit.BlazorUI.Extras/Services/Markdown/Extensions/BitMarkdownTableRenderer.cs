using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.BlazorUI;

/// <summary>Renders <see cref="BitMarkdownTableNode"/> as an HTML table with column alignment.</summary>
public sealed class BitMarkdownTableRenderer : BitMarkdownNodeRenderer
{
    public override bool Accept(BitMarkdownNode node) => node is BitMarkdownTableNode;

    public override void Write(BitMarkdownRenderer r, RenderTreeBuilder b, BitMarkdownNode node)
    {
        // Fixed literal sequence numbers (see BitMarkdownCoreRenderer for the rationale).
        var table = (BitMarkdownTableNode)node;
        b.OpenElement(0, "table");

        b.OpenElement(1, "thead");
        b.OpenElement(2, "tr");
        for (int c = 0; c < table.Header.Count; c++)
        {
            b.OpenElement(3, "th");
            b.AddAttribute(4, "scope", "col");
            AddAlignment(b, table, c);
            r.WriteNodes(b, table.Header[c]);
            b.CloseElement();
        }
        b.CloseElement();
        b.CloseElement();

        b.OpenElement(5, "tbody");
        foreach (var row in table.Rows)
        {
            b.OpenElement(6, "tr");
            for (int c = 0; c < row.Count; c++)
            {
                b.OpenElement(7, "td");
                AddAlignment(b, table, c);
                r.WriteNodes(b, row[c]);
                b.CloseElement();
            }
            b.CloseElement();
        }
        b.CloseElement();

        b.CloseElement();
    }

    private static void AddAlignment(RenderTreeBuilder b, BitMarkdownTableNode table, int col)
    {
        if (col >= table.Alignments.Count) return;
        string? align = table.Alignments[col] switch
        {
            BitMarkdownColumnAlignment.Left => "left",
            BitMarkdownColumnAlignment.Center => "center",
            BitMarkdownColumnAlignment.Right => "right",
            _ => null
        };
        if (align is not null)
            b.AddAttribute(8, "style", $"text-align:{align}");
    }
}
