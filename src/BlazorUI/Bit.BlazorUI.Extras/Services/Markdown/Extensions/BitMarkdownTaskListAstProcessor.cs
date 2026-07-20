using System.Text.RegularExpressions;

namespace Bit.BlazorUI;

/// <summary>
/// Rewrites list items beginning with <c>[ ]</c> / <c>[x]</c> into a
/// <see cref="BitMarkdownTaskCheckboxNode"/> followed by the remaining text.
/// </summary>
public sealed partial class BitMarkdownTaskListAstProcessor : BitMarkdownAstProcessor
{
    [GeneratedRegex(@"^\[([ xX])\](?:\s+(.*))?$")]
    private static partial Regex TaskMarker();

    public override void Process(BitMarkdownDocumentNode document, BitMarkdownPipeline pipeline)
    {
        foreach (var list in BitMarkdownAstHelper.Descendants(document).OfType<BitMarkdownListNode>())
        {
            foreach (var item in list.Items)
            {
                // Detect task markers from the raw (pre-inline) item source so that
                // escaped literals like "\[ \]" are not misread as real checkboxes.
                // Only the first logical source line is considered, otherwise a
                // multi-line item (with continuation lines) fails the anchored regex.
                if (item.Source is null) continue;
                int newline = item.Source.IndexOfAny(['\n', '\r']);
                var firstLine = newline >= 0 ? item.Source[..newline] : item.Source;
                var raw = TaskMarker().Match(firstLine);
                if (!raw.Success) continue;

                if (item.Children.FirstOrDefault() is not BitMarkdownParagraphNode para) continue;
                if (para.Inlines.FirstOrDefault() is not BitMarkdownTextNode text) continue;

                var m = TaskMarker().Match(text.Text);
                if (!m.Success) continue;

                text.Text = m.Groups[2].Value;
                para.Inlines.Insert(0, new BitMarkdownTaskCheckboxNode { Checked = raw.Groups[1].Value is "x" or "X" });
            }
        }
    }
}
