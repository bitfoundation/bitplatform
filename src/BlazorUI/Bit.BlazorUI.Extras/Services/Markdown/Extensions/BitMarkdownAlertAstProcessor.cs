namespace Bit.BlazorUI;

/// <summary>
/// Turns a block quote whose first line is <c>[!NOTE]</c> (or <c>[!TIP]</c>,
/// <c>[!IMPORTANT]</c>, <c>[!WARNING]</c>, <c>[!CAUTION]</c>) into a
/// <see cref="BitMarkdownAlertNode"/>. The marker must be alone on that line, exactly as
/// GitHub requires; anything else stays an ordinary block quote.
/// </summary>
public sealed class BitMarkdownAlertAstProcessor : BitMarkdownAstProcessor
{
    // After the core reference resolver, whose text merging is what leaves the marker as a
    // single text node to match against.
    public override int Order => 20;

    public override void Process(BitMarkdownDocumentNode document, BitMarkdownPipeline pipeline)
    {
        BitMarkdownAstHelper.VisitChildLists(document, list =>
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] is not BitMarkdownBlockquoteNode quote) continue;
                if (TryConvert(quote) is { } alert) list[i] = alert;
            }
        });
    }

    private static BitMarkdownAlertNode? TryConvert(BitMarkdownBlockquoteNode quote)
    {
        if (quote.Children.FirstOrDefault() is not BitMarkdownParagraphNode paragraph) return null;
        if (paragraph.Inlines.FirstOrDefault() is not BitMarkdownTextNode marker) return null;
        if (TryParseKind(marker.Text, out var kind) is false) return null;

        // The marker owns its whole line: the very next inline must be the line break that
        // ends it, or the paragraph must consist of nothing else.
        bool hasBreak = paragraph.Inlines.Count > 1 && paragraph.Inlines[1] is BitMarkdownLineBreakNode;
        if (paragraph.Inlines.Count > 1 && hasBreak is false) return null;

        paragraph.Inlines.RemoveAt(0);
        if (hasBreak) paragraph.Inlines.RemoveAt(0);

        var alert = new BitMarkdownAlertNode { Kind = kind };
        // A "> [!NOTE]" with nothing after it leaves an empty paragraph behind.
        alert.Children.AddRange(paragraph.Inlines.Count == 0 ? quote.Children.Skip(1) : quote.Children);
        return alert;
    }

    private static bool TryParseKind(string text, out BitMarkdownAlertKind kind)
    {
        kind = default;

        var span = text.AsSpan().Trim();
        if (span.Length < 4 || span[0] != '[' || span[1] != '!' || span[^1] != ']') return false;

        var name = span[2..^1];
        if (name.Equals("NOTE", StringComparison.OrdinalIgnoreCase)) kind = BitMarkdownAlertKind.Note;
        else if (name.Equals("TIP", StringComparison.OrdinalIgnoreCase)) kind = BitMarkdownAlertKind.Tip;
        else if (name.Equals("IMPORTANT", StringComparison.OrdinalIgnoreCase)) kind = BitMarkdownAlertKind.Important;
        else if (name.Equals("WARNING", StringComparison.OrdinalIgnoreCase)) kind = BitMarkdownAlertKind.Warning;
        else if (name.Equals("CAUTION", StringComparison.OrdinalIgnoreCase)) kind = BitMarkdownAlertKind.Caution;
        else return false;

        return true;
    }
}
