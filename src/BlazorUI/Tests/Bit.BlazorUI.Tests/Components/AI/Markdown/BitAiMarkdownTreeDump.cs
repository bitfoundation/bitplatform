using System.Collections.Generic;
using System.Text;

namespace Bit.BlazorUI.Tests.Components.AI.Markdown;

/// <summary>Renders a parsed tree to a canonical string so two parses can be compared exactly.</summary>
internal static class BitAiMarkdownTreeDump
{
    public static string Of(BitAiMarkdownDocument document)
    {
        var builder = new StringBuilder();
        foreach (var block in document.Blocks) Block(block, builder);
        return builder.ToString();
    }

    private static void Block(BitAiMarkdownBlock block, StringBuilder b)
    {
        b.Append('{').Append(block.GetType().Name["BitAi".Length..]).Append(block.IsOpen ? " open" : string.Empty);
        switch (block)
        {
            case BitAiParagraphBlock p: Inlines(p.Inlines, b); break;
            case BitAiHeadingBlock h: b.Append(' ').Append(h.Level).Append(' ').Append(h.Id); Inlines(h.Inlines, b); break;
            case BitAiCodeBlock c: b.Append(' ').Append(c.Language).Append(" [").Append(c.Code).Append(']'); break;
            case BitAiMathBlock m: b.Append(" [").Append(m.Expression).Append(']'); break;
            case BitAiHtmlBlock html: b.Append(" [").Append(html.Html).Append(']'); break;
            case BitAiQuoteBlock q: foreach (var child in q.Blocks) Block(child, b); break;
            case BitAiListBlock l:
                b.Append(l.IsOrdered ? " ol" : " ul").Append(l.IsLoose ? " loose" : string.Empty);
                foreach (var item in l.Items) Block(item, b);
                break;
            case BitAiListItemBlock item:
                b.Append(' ').Append(item.IsChecked);
                foreach (var child in item.Blocks) Block(child, b);
                break;
            case BitAiTableBlock t:
                foreach (var cell in t.Header) Inlines(cell, b);
                foreach (var row in t.Rows) foreach (var cell in row) Inlines(cell, b);
                break;
        }

        b.Append('}');
    }

    private static void Inlines(IEnumerable<BitAiMarkdownInline> inlines, StringBuilder b)
    {
        b.Append('(');
        foreach (var inline in inlines)
        {
            switch (inline)
            {
                case BitAiTextInline t: b.Append('"').Append(t.Text).Append('"'); break;
                case BitAiEmphasisInline e: b.Append(e.IsStrong ? "strong" : "em"); Inlines(e.Inlines, b); break;
                case BitAiStrikethroughInline s: b.Append("del"); Inlines(s.Inlines, b); break;
                case BitAiLinkInline l: b.Append("a[").Append(l.Url).Append(']'); Inlines(l.Inlines, b); break;
                case BitAiCodeInline c: b.Append("code[").Append(c.Code).Append(']'); break;
                case BitAiMathInline m: b.Append("math[").Append(m.Expression).Append(']'); break;
                default: b.Append(inline.GetType().Name["BitAi".Length..]); break;
            }
        }

        b.Append(')');
    }
}
