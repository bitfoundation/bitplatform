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

        // A wide table has to scroll somewhere. Scrolling the <table> itself (display: block) is
        // the shortcut most Markdown stylesheets take, and it costs the element its table
        // semantics - a screen reader stops announcing rows and columns. Scrolling a wrapper
        // instead keeps them, and the wrapper is focusable so the overflow can be reached with
        // the keyboard alone.
        b.OpenElement(0, "div");
        b.AddAttribute(1, "class", "bit-mdv-table-wrapper");
        b.AddAttribute(2, "role", "region");
        b.AddAttribute(3, "aria-label", r.Texts.Table);
        b.AddAttribute(4, "tabindex", "0");

        b.OpenElement(5, "table");

        b.OpenElement(6, "thead");
        b.OpenElement(7, "tr");
        for (int c = 0; c < table.Header.Count; c++)
        {
            b.OpenElement(8, "th");
            b.AddAttribute(9, "scope", "col");
            AddAlignment(b, table, c);
            r.WriteNodes(b, table.Header[c]);
            b.CloseElement();
        }
        b.CloseElement();
        b.CloseElement();

        b.OpenElement(11, "tbody");
        foreach (var row in table.Rows)
        {
            b.OpenElement(12, "tr");
            for (int c = 0; c < row.Count; c++)
            {
                b.OpenElement(13, "td");
                AddAlignment(b, table, c);
                r.WriteNodes(b, row[c]);
                b.CloseElement();
            }
            b.CloseElement();
        }
        b.CloseElement();

        b.CloseElement();
        b.CloseElement();
    }

    // Column alignment is applied with a class rather than an inline style attribute, so the
    // rendered document still aligns under a strict Content-Security-Policy that does not
    // allow 'unsafe-inline' for styles.
    private static void AddAlignment(RenderTreeBuilder b, BitMarkdownTableNode table, int col)
    {
        if (col >= table.Alignments.Count) return;
        string? align = table.Alignments[col] switch
        {
            BitMarkdownColumnAlignment.Left => "bit-mdv-align-left",
            BitMarkdownColumnAlignment.Center => "bit-mdv-align-center",
            BitMarkdownColumnAlignment.Right => "bit-mdv-align-right",
            _ => null
        };
        if (align is not null)
            b.AddAttribute(10, "class", align);
    }
}
