using System.Text.RegularExpressions;

namespace Bit.BlazorUI;

/// <summary>Parses ordered and unordered lists, including nesting and looseness.</summary>
public sealed class BitMarkdownListParser : BitMarkdownBlockParser
{
    public override int Order => 60;

    public override bool CanInterruptParagraph(BitMarkdownBlockProcessor state, int lineIndex)
    {
        var line = state.Lines[lineIndex];
        // An empty list item (marker with no content) cannot interrupt a paragraph.
        var bullet = BitMarkdownBlockGrammar.Bullet().Match(line);
        if (bullet.Success && bullet.Groups[3].Value.Length > 0) return true;
        // An ordered list may only interrupt a paragraph when it starts with "1".
        var m = BitMarkdownBlockGrammar.Ordered().Match(line);
        return m.Success && m.Groups[1].Value == "1" && m.Groups[4].Value.Length > 0;
    }

    public override bool TryParse(BitMarkdownBlockProcessor state, List<BitMarkdownNode> output)
    {
        var lines = state.Lines;
        string first = lines[state.Line];
        bool ordered = BitMarkdownBlockGrammar.Ordered().IsMatch(first);
        if (!ordered && !BitMarkdownBlockGrammar.Bullet().IsMatch(first)) return false;

        // Track the specific marker character so that a change of marker
        // (e.g. "- a" followed by "* b", or "1." followed by "1)") starts a
        // new list, as required by CommonMark.
        int startNum;
        char markerChar;
        if (ordered)
        {
            var fm = BitMarkdownBlockGrammar.Ordered().Match(first);
            startNum = int.Parse(fm.Groups[1].Value);
            markerChar = fm.Groups[2].Value[0];
        }
        else
        {
            startNum = 1;
            markerChar = BitMarkdownBlockGrammar.Bullet().Match(first).Groups[1].Value[0];
        }
        var list = new BitMarkdownListNode { Ordered = ordered, Start = startNum };
        int i = state.Line;
        bool loose = false;

        while (i < lines.Count)
        {
            string line = lines[i];
            Match m = ordered ? BitMarkdownBlockGrammar.Ordered().Match(line) : BitMarkdownBlockGrammar.Bullet().Match(line);
            if (!m.Success) break;
            // A different marker character begins a new list.
            char curMarker = ordered ? m.Groups[2].Value[0] : m.Groups[1].Value[0];
            if (curMarker != markerChar) break;

            int markerIndent;
            string firstContent;
            if (ordered)
            {
                // A marker-only item has no following whitespace; its content column
                // still starts one space past the marker.
                int baseIndent = BitMarkdownBlockProcessor.GetIndent(line) + m.Groups[1].Value.Length + 1;
                int afterMarker = m.Groups[3].Value.Length;
                firstContent = m.Groups[4].Value;
                (markerIndent, firstContent) = ResolveContentIndent(baseIndent, afterMarker, firstContent);
            }
            else
            {
                int baseIndent = BitMarkdownBlockProcessor.GetIndent(line) + 1;
                int afterMarker = m.Groups[2].Value.Length;
                firstContent = m.Groups[3].Value;
                (markerIndent, firstContent) = ResolveContentIndent(baseIndent, afterMarker, firstContent);
            }

            var itemLines = new List<string> { firstContent };
            i++;

            bool itemHadBlank = false;
            while (i < lines.Count)
            {
                string l = lines[i];
                if (BitMarkdownBlockProcessor.IsBlank(l))
                {
                    int j = i + 1;
                    while (j < lines.Count && BitMarkdownBlockProcessor.IsBlank(lines[j])) j++;
                    if (j < lines.Count && BitMarkdownBlockProcessor.GetIndent(lines[j]) >= markerIndent)
                    {
                        itemLines.Add(string.Empty);
                        itemHadBlank = true;
                        i++;
                        continue;
                    }
                    if (j < lines.Count && IsSameMarker(lines[j], ordered, markerChar))
                    {
                        // A same-marker item after blank separator(s) makes the list
                        // loose; advance past the blank separator so the outer loop
                        // resumes at the next marker instead of stopping on the blank
                        // line (which would split this into two separate lists).
                        loose = true;
                        i = j;
                    }
                    break;
                }

                if (BitMarkdownBlockProcessor.GetIndent(l) >= markerIndent)
                {
                    itemLines.Add(BitMarkdownBlockProcessor.StripIndent(l, markerIndent));
                    i++;
                    continue;
                }

                if (IsSameMarker(l, ordered, markerChar)
                    || BitMarkdownBlockGrammar.Bullet().IsMatch(l) || BitMarkdownBlockGrammar.Ordered().IsMatch(l))
                    break;

                if (!state.StartsBlock(i))
                {
                    itemLines.Add(l.TrimStart());
                    i++;
                    continue;
                }
                break;
            }

            var item = new BitMarkdownListItemNode { Source = firstContent };
            item.Children.AddRange(state.ParseBlocks(itemLines));
            if (itemHadBlank) loose = true;
            list.Items.Add(item);
        }

        list.Tight = !loose;
        output.Add(list);
        state.Line = i;
        return true;
    }

    private static bool IsSameMarker(string line, bool ordered, char markerChar)
    {
        var m = ordered ? BitMarkdownBlockGrammar.Ordered().Match(line) : BitMarkdownBlockGrammar.Bullet().Match(line);
        if (!m.Success) return false;
        char c = ordered ? m.Groups[2].Value[0] : m.Groups[1].Value[0];
        return c == markerChar;
    }

    // Computes the content indentation column for a list item from the number of
    // spaces following the marker. Per CommonMark, 1-4 spaces are consumed as
    // indentation; 5+ spaces place the content one column past the marker and keep
    // the surplus spaces as item content (an indented code block within the item).
    // The 5+ rule is driven purely by the spaces after the marker: it applies even
    // when the first line has no content, so a blank first line still yields the
    // correct (marker + 1) content indent instead of an inflated one.
    private static (int markerIndent, string firstContent) ResolveContentIndent(
        int baseIndent, int afterMarker, string firstContent)
    {
        if (afterMarker >= 5)
            return (baseIndent + 1, new string(' ', afterMarker - 1) + firstContent);

        return (baseIndent + Math.Max(1, afterMarker), firstContent);
    }
}
