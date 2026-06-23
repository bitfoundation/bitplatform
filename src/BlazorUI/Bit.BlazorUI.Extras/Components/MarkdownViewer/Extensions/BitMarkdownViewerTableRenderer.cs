using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.BlazorUI;

/// <summary>Renders <see cref="BitMarkdownViewerTableNode"/> as an HTML table with column alignment.</summary>
public sealed class BitMarkdownViewerTableRenderer : BitMarkdownViewerNodeRenderer
{
    public override bool Accept(BitMarkdownViewerMarkdownNode node) => node is BitMarkdownViewerTableNode;

    public override void Write(BitMarkdownViewerMarkdownRenderer r, RenderTreeBuilder b, BitMarkdownViewerMarkdownNode node)
    {
        var table = (BitMarkdownViewerTableNode)node;
        b.OpenElement(r.NextSeq(), "table");

        b.OpenElement(r.NextSeq(), "thead");
        b.OpenElement(r.NextSeq(), "tr");
        for (int c = 0; c < table.Header.Count; c++)
        {
            b.OpenElement(r.NextSeq(), "th");
            AddAlignment(r, b, table, c);
            r.WriteNodes(b, table.Header[c]);
            b.CloseElement();
        }
        b.CloseElement();
        b.CloseElement();

        b.OpenElement(r.NextSeq(), "tbody");
        foreach (var row in table.Rows)
        {
            b.OpenElement(r.NextSeq(), "tr");
            for (int c = 0; c < row.Count; c++)
            {
                b.OpenElement(r.NextSeq(), "td");
                AddAlignment(r, b, table, c);
                r.WriteNodes(b, row[c]);
                b.CloseElement();
            }
            b.CloseElement();
        }
        b.CloseElement();

        b.CloseElement();
    }

    private static void AddAlignment(BitMarkdownViewerMarkdownRenderer r, RenderTreeBuilder b, BitMarkdownViewerTableNode table, int col)
    {
        if (col >= table.Alignments.Count) return;
        string? align = table.Alignments[col] switch
        {
            BitMarkdownViewerColumnAlignment.Left => "left",
            BitMarkdownViewerColumnAlignment.Center => "center",
            BitMarkdownViewerColumnAlignment.Right => "right",
            _ => null
        };
        if (align is not null)
            b.AddAttribute(r.NextSeq(), "style", $"text-align:{align}");
    }
}
