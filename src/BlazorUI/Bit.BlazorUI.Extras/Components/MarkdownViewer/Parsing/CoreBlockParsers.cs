using System.Text;
using System.Text.RegularExpressions;

namespace Bit.BlazorUI;

/// <summary>Shared regular expressions for the core block grammar.</summary>
internal static partial class BlockGrammar
{
    [GeneratedRegex(@"^ {0,3}(?:([-*_])\s*)(?:\1\s*){2,}$")]
    public static partial Regex ThematicBreak();

    [GeneratedRegex(@"^ {0,3}(#{1,6})(?:\s+(.*?))?\s*#*\s*$")]
    public static partial Regex AtxHeading();

    [GeneratedRegex(@"^ {0,3}(`{3,}|~{3,})\s*([^`]*)$")]
    public static partial Regex Fence();

    [GeneratedRegex(@"^ {0,3}(`{3,}|~{3,})\s*$")]
    public static partial Regex FenceClose();

    [GeneratedRegex(@"^ {0,3}([-+*])(\s+)(.*)$")]
    public static partial Regex Bullet();

    [GeneratedRegex(@"^ {0,3}(\d{1,9})([.)])(\s+)(.*)$")]
    public static partial Regex Ordered();

    [GeneratedRegex(@"^ {0,3}(=+|-+)\s*$")]
    public static partial Regex Setext();
}

/// <summary>Parses fenced code blocks (<c>```</c> / <c>~~~</c>).</summary>
public sealed class FencedCodeBlockParser : BlockParser
{
    public override int Order => 10;

    public override bool CanInterruptParagraph(BlockProcessor state, int lineIndex)
        => BlockGrammar.Fence().IsMatch(state.Lines[lineIndex]);

    public override bool TryParse(BlockProcessor state, List<MarkdownNode> output)
    {
        var lines = state.Lines;
        var fence = BlockGrammar.Fence().Match(lines[state.Line]);
        if (!fence.Success) return false;

        string marker = fence.Groups[1].Value;
        char fenceChar = marker[0];
        int fenceLen = marker.Length;
        string info = fence.Groups[2].Value.Trim();
        int indent = BlockProcessor.GetIndent(lines[state.Line]);

        var sb = new StringBuilder();
        int i = state.Line + 1;
        while (i < lines.Count)
        {
            string l = lines[i];
            var close = BlockGrammar.FenceClose().Match(l);
            if (close.Success && close.Groups[1].Value[0] == fenceChar
                && close.Groups[1].Value.Length >= fenceLen)
            {
                i++;
                break;
            }
            sb.AppendLine(BlockProcessor.StripIndent(l, indent));
            i++;
        }

        output.Add(new CodeBlockNode
        {
            Info = string.IsNullOrEmpty(info) ? null : info,
            Content = BlockProcessor.TrimTrailingNewline(sb.ToString())
        });
        state.Line = i;
        return true;
    }
}

/// <summary>Parses ATX headings (<c># ... ######</c>).</summary>
public sealed class AtxHeadingParser : BlockParser
{
    public override int Order => 20;

    public override bool CanInterruptParagraph(BlockProcessor state, int lineIndex)
        => BlockGrammar.AtxHeading().IsMatch(state.Lines[lineIndex]);

    public override bool TryParse(BlockProcessor state, List<MarkdownNode> output)
    {
        var m = BlockGrammar.AtxHeading().Match(state.Lines[state.Line]);
        if (!m.Success) return false;

        var heading = new HeadingNode { Level = m.Groups[1].Value.Length };
        string content = m.Groups[2].Success ? m.Groups[2].Value.Trim() : string.Empty;
        if (content.Length > 0)
            heading.Inlines.AddRange(state.ParseInlines(content));
        output.Add(heading);
        state.Line++;
        return true;
    }
}

/// <summary>Parses thematic breaks / horizontal rules.</summary>
public sealed class ThematicBreakParser : BlockParser
{
    public override int Order => 30;

    public override bool CanInterruptParagraph(BlockProcessor state, int lineIndex)
        => BlockGrammar.ThematicBreak().IsMatch(state.Lines[lineIndex]);

    public override bool TryParse(BlockProcessor state, List<MarkdownNode> output)
    {
        if (!BlockGrammar.ThematicBreak().IsMatch(state.Lines[state.Line])) return false;
        output.Add(new ThematicBreakNode());
        state.Line++;
        return true;
    }
}

/// <summary>Parses block quotes (<c>&gt; ...</c>) with lazy continuation.</summary>
public sealed class BlockquoteParser : BlockParser
{
    public override int Order => 40;

    public override bool CanInterruptParagraph(BlockProcessor state, int lineIndex)
        => IsQuote(state.Lines[lineIndex]);

    public override bool TryParse(BlockProcessor state, List<MarkdownNode> output)
    {
        var lines = state.Lines;
        if (!IsQuote(lines[state.Line])) return false;

        var inner = new List<string>();
        int i = state.Line;
        while (i < lines.Count)
        {
            string l = lines[i];
            if (IsQuote(l)) { inner.Add(StripMarker(l)); i++; }
            else if (!BlockProcessor.IsBlank(l) && !state.StartsBlock(i)) { inner.Add(l); i++; }
            else break;
        }

        var quote = new BlockquoteNode();
        quote.Children.AddRange(state.ParseBlocks(inner));
        output.Add(quote);
        state.Line = i;
        return true;
    }

    internal static bool IsQuote(string line)
    {
        // A blockquote marker may be preceded by at most 3 spaces; 4+ spaces
        // make it an indented code block instead.
        int spaces = 0;
        while (spaces < line.Length && line[spaces] == ' ') spaces++;
        return spaces <= 3 && spaces < line.Length && line[spaces] == '>';
    }

    private static string StripMarker(string line)
    {
        string t = line.TrimStart(' ')[1..];
        if (t.StartsWith(' ')) t = t[1..];
        return t;
    }
}

/// <summary>Parses indented (4-space) code blocks.</summary>
public sealed class IndentedCodeBlockParser : BlockParser
{
    public override int Order => 50;

    public override bool TryParse(BlockProcessor state, List<MarkdownNode> output)
    {
        var lines = state.Lines;
        if (BlockProcessor.GetIndent(lines[state.Line]) < 4) return false;

        var sb = new StringBuilder();
        int i = state.Line;
        int lastNonBlank = state.Line;
        while (i < lines.Count)
        {
            string l = lines[i];
            if (BlockProcessor.IsBlank(l)) { sb.AppendLine(string.Empty); i++; continue; }
            if (BlockProcessor.GetIndent(l) < 4) break;
            sb.AppendLine(BlockProcessor.StripIndent(l, 4));
            lastNonBlank = i;
            i++;
        }

        output.Add(new CodeBlockNode
        {
            Content = BlockProcessor.TrimTrailingNewline(sb.ToString()).TrimEnd('\n')
        });
        state.Line = lastNonBlank + 1;
        return true;
    }
}

/// <summary>Parses ordered and unordered lists, including nesting and looseness.</summary>
public sealed class ListParser : BlockParser
{
    public override int Order => 60;

    public override bool CanInterruptParagraph(BlockProcessor state, int lineIndex)
    {
        var line = state.Lines[lineIndex];
        if (BlockGrammar.Bullet().IsMatch(line)) return true;
        // An ordered list may only interrupt a paragraph when it starts with "1".
        var m = BlockGrammar.Ordered().Match(line);
        return m.Success && m.Groups[1].Value == "1";
    }

    public override bool TryParse(BlockProcessor state, List<MarkdownNode> output)
    {
        var lines = state.Lines;
        string first = lines[state.Line];
        bool ordered = BlockGrammar.Ordered().IsMatch(first);
        if (!ordered && !BlockGrammar.Bullet().IsMatch(first)) return false;

        int startNum = ordered ? int.Parse(BlockGrammar.Ordered().Match(first).Groups[1].Value) : 1;
        var list = new ListNode { Ordered = ordered, Start = startNum };
        int i = state.Line;
        bool loose = false;

        while (i < lines.Count)
        {
            string line = lines[i];
            Match m = ordered ? BlockGrammar.Ordered().Match(line) : BlockGrammar.Bullet().Match(line);
            if (!m.Success) break;

            int markerIndent;
            string firstContent;
            if (ordered)
            {
                markerIndent = BlockProcessor.GetIndent(line)
                    + m.Groups[1].Value.Length + 1 + m.Groups[3].Value.Length;
                firstContent = m.Groups[4].Value;
            }
            else
            {
                markerIndent = BlockProcessor.GetIndent(line) + 1 + m.Groups[2].Value.Length;
                firstContent = m.Groups[3].Value;
            }

            var itemLines = new List<string> { firstContent };
            i++;

            bool itemHadBlank = false;
            while (i < lines.Count)
            {
                string l = lines[i];
                if (BlockProcessor.IsBlank(l))
                {
                    int j = i + 1;
                    while (j < lines.Count && BlockProcessor.IsBlank(lines[j])) j++;
                    if (j < lines.Count && BlockProcessor.GetIndent(lines[j]) >= markerIndent)
                    {
                        itemLines.Add(string.Empty);
                        itemHadBlank = true;
                        i++;
                        continue;
                    }
                    if (j < lines.Count && IsSameMarker(lines[j], ordered)) loose = true;
                    break;
                }

                if (BlockProcessor.GetIndent(l) >= markerIndent)
                {
                    itemLines.Add(BlockProcessor.StripIndent(l, markerIndent));
                    i++;
                    continue;
                }

                if (IsSameMarker(l, ordered)
                    || BlockGrammar.Bullet().IsMatch(l) || BlockGrammar.Ordered().IsMatch(l))
                    break;

                if (!state.StartsBlock(i))
                {
                    itemLines.Add(l.TrimStart());
                    i++;
                    continue;
                }
                break;
            }

            var item = new ListItemNode();
            item.Children.AddRange(state.ParseBlocks(itemLines));
            if (itemHadBlank) loose = true;
            list.Items.Add(item);
        }

        list.Tight = !loose;
        output.Add(list);
        state.Line = i;
        return true;
    }

    private static bool IsSameMarker(string line, bool ordered)
        => ordered ? BlockGrammar.Ordered().IsMatch(line) : BlockGrammar.Bullet().IsMatch(line);
}

/// <summary>The fallback parser: gathers a paragraph and detects setext headings.</summary>
public sealed class ParagraphParser : BlockParser
{
    public override int Order => 1000;

    public override bool TryParse(BlockProcessor state, List<MarkdownNode> output)
    {
        var lines = state.Lines;
        var buffer = new List<string>();
        int i = state.Line;

        while (i < lines.Count)
        {
            string l = lines[i];
            if (BlockProcessor.IsBlank(l)) break;

            if (buffer.Count > 0)
            {
                var setext = BlockGrammar.Setext().Match(l);
                if (setext.Success && !BlockGrammar.ThematicBreak().IsMatch(l))
                {
                    int level = setext.Groups[1].Value[0] == '=' ? 1 : 2;
                    var heading = new HeadingNode { Level = level };
                    heading.Inlines.AddRange(state.ParseInlines(string.Join('\n', buffer).Trim()));
                    output.Add(heading);
                    state.Line = i + 1;
                    return true;
                }

                if (state.StartsBlock(i)) break;
            }

            // Keep trailing spaces so two-space hard breaks survive.
            buffer.Add(l.TrimStart());
            i++;
        }

        if (buffer.Count > 0)
        {
            var para = new ParagraphNode();
            para.Inlines.AddRange(state.ParseInlines(string.Join('\n', buffer)));
            output.Add(para);
        }
        state.Line = i;
        return true;
    }
}
