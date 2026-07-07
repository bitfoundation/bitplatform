using System.Text.RegularExpressions;

namespace Bit.BlazorUI;

/// <summary>
/// Pure, side-effect-free implementations of every <see cref="BitMarkdownEditorCommand"/>.
/// Each method takes the current text plus the selection range and returns the
/// new text and where the selection should land. Keeping these pure makes the
/// editing behaviour fully unit-testable without a browser.
/// </summary>
public static partial class BitMarkdownEditorCommands
{
    /// <summary>
    /// Applies <paramref name="command"/> to <paramref name="text"/> for the given selection.
    /// </summary>
    /// <param name="command">The command to run.</param>
    /// <param name="text">Full editor text (LF line endings).</param>
    /// <param name="start">Selection start index.</param>
    /// <param name="end">Selection end index.</param>
    /// <param name="indentUnit">String inserted for one indent level (e.g. two spaces or a tab).</param>
    public static BitMarkdownEditorEditResult Apply(BitMarkdownEditorCommand command, string text, int start, int end, string indentUnit = "  ")
    {
        text ??= string.Empty;
        start = Math.Clamp(start, 0, text.Length);
        end = Math.Clamp(end, 0, text.Length);
        if (end < start)
        {
            (start, end) = (end, start);
        }

        return command switch
        {
            BitMarkdownEditorCommand.Bold => ToggleWrap(text, start, end, "**", "bold text"),
            BitMarkdownEditorCommand.Italic => ToggleWrap(text, start, end, "*", "italic text"),
            BitMarkdownEditorCommand.Strikethrough => ToggleWrap(text, start, end, "~~", "strikethrough"),
            BitMarkdownEditorCommand.InlineCode => ToggleWrap(text, start, end, "`", "code"),
            BitMarkdownEditorCommand.Heading1 => Heading(text, start, end, 1),
            BitMarkdownEditorCommand.Heading2 => Heading(text, start, end, 2),
            BitMarkdownEditorCommand.Heading3 => Heading(text, start, end, 3),
            BitMarkdownEditorCommand.Quote => LinePrefixToggle(text, start, end, "> ", QuotePrefix()),
            BitMarkdownEditorCommand.UnorderedList => UnorderedList(text, start, end),
            BitMarkdownEditorCommand.OrderedList => OrderedList(text, start, end),
            BitMarkdownEditorCommand.TaskList => TaskList(text, start, end),
            BitMarkdownEditorCommand.CodeBlock => CodeBlock(text, start, end),
            BitMarkdownEditorCommand.Link => LinkOrImage(text, start, end, isImage: false),
            BitMarkdownEditorCommand.Image => LinkOrImage(text, start, end, isImage: true),
            BitMarkdownEditorCommand.Table => Table(text, start, end),
            BitMarkdownEditorCommand.HorizontalRule => HorizontalRule(text, start, end),
            BitMarkdownEditorCommand.Indent => Indent(text, start, end, indentUnit),
            BitMarkdownEditorCommand.Outdent => Outdent(text, start, end, indentUnit),
            BitMarkdownEditorCommand.NewLine => NewLine(text, start, end),
            _ => BitMarkdownEditorEditResult.NotHandled(text, start, end)
        };
    }

    // ---- inline wrapping (bold / italic / strikethrough / code) -------------

    private static BitMarkdownEditorEditResult ToggleWrap(string text, int start, int end, string marker, string placeholder)
    {
        int ml = marker.Length;
        string selected = text[start..end];

        // Already wrapped on the outside? -> unwrap.
        if (start >= ml && end + ml <= text.Length &&
            text.Substring(start - ml, ml) == marker &&
            text.Substring(end, ml) == marker &&
            (marker is not "*" || (IsItalicDelimiter(text, start - ml) && IsItalicDelimiter(text, end))))
        {
            string unwrapped = text[..(start - ml)] + selected + text[(end + ml)..];
            return new BitMarkdownEditorEditResult(true, unwrapped, start - ml, end - ml);
        }

        // Markers captured inside the selection? -> unwrap.
        if (selected.Length >= 2 * ml && selected.StartsWith(marker, StringComparison.Ordinal) && selected.EndsWith(marker, StringComparison.Ordinal) &&
            (marker is not "*" || (IsItalicDelimiter(selected, 0) && IsItalicDelimiter(selected, selected.Length - 1))))
        {
            string inner = selected[ml..^ml];
            return new BitMarkdownEditorEditResult(true, text[..start] + inner + text[end..], start, start + inner.Length);
        }

        if (start == end)
        {
            string insert = marker + placeholder + marker;
            return new BitMarkdownEditorEditResult(true, text[..start] + insert + text[end..], start + ml, start + ml + placeholder.Length);
        }

        string wrapped = marker + selected + marker;
        return new BitMarkdownEditorEditResult(true, text[..start] + wrapped + text[end..], start + ml, start + ml + selected.Length);
    }

    /// <summary>
    /// A lone '*' that is part of a '**' run belongs to bold, not italic. The star at
    /// <paramref name="index"/> counts as an italic delimiter only when its contiguous
    /// run of stars has an odd length (the unpaired star is the italic marker).
    /// </summary>
    private static bool IsItalicDelimiter(string text, int index)
    {
        int s = index;
        while (s > 0 && text[s - 1] == '*') s--;

        int e = index;
        while (e < text.Length - 1 && text[e + 1] == '*') e++;

        return (e - s + 1) % 2 == 1;
    }

    // ---- headings -----------------------------------------------------------

    private static BitMarkdownEditorEditResult Heading(string text, int start, int end, int level)
    {
        return TransformBlock(text, start, end, lines =>
        {
            string hashes = new('#', level);
            for (int i = 0; i < lines.Count; i++)
            {
                Match m = HeadingPrefix().Match(lines[i]);
                string rest = m.Success ? lines[i][m.Length..] : lines[i];
                int existing = m.Success ? m.Groups[1].Value.Length : 0;
                lines[i] = existing == level ? rest : $"{hashes} {rest}";
            }
        });
    }

    // ---- blockquote ---------------------------------------------------------

    private static BitMarkdownEditorEditResult LinePrefixToggle(string text, int start, int end, string prefix, Regex detect)
    {
        return TransformBlock(text, start, end, lines =>
        {
            bool allPrefixed = lines.Where(l => l.Length > 0).All(detect.IsMatch);
            for (int i = 0; i < lines.Count; i++)
            {
                if (allPrefixed)
                {
                    Match m = detect.Match(lines[i]);
                    if (m.Success)
                    {
                        lines[i] = lines[i][m.Length..];
                    }
                }
                else
                {
                    lines[i] = prefix + lines[i];
                }
            }
        });
    }

    // ---- lists --------------------------------------------------------------

    private static BitMarkdownEditorEditResult UnorderedList(string text, int start, int end)
    {
        return TransformBlock(text, start, end, lines =>
        {
            bool allListed = lines.Where(l => l.Trim().Length > 0).All(UnorderedItem().IsMatch);
            for (int i = 0; i < lines.Count; i++)
            {
                if (allListed)
                {
                    Match m = UnorderedItem().Match(lines[i]);
                    if (m.Success)
                    {
                        lines[i] = m.Groups[1].Value + lines[i][m.Length..];
                    }
                }
                else if (lines[i].Trim().Length > 0)
                {
                    lines[i] = "- " + lines[i];
                }
            }
        });
    }

    private static BitMarkdownEditorEditResult TaskList(string text, int start, int end)
    {
        return TransformBlock(text, start, end, lines =>
        {
            bool allTasks = lines.Where(l => l.Trim().Length > 0).All(TaskItem().IsMatch);
            for (int i = 0; i < lines.Count; i++)
            {
                if (allTasks)
                {
                    Match m = TaskItem().Match(lines[i]);
                    if (m.Success)
                    {
                        lines[i] = m.Groups[1].Value + lines[i][m.Length..];
                    }
                }
                else if (lines[i].Trim().Length > 0)
                {
                    lines[i] = "- [ ] " + lines[i];
                }
            }
        });
    }

    private static BitMarkdownEditorEditResult OrderedList(string text, int start, int end)
    {
        return TransformBlock(text, start, end, lines =>
        {
            bool allOrdered = lines.Where(l => l.Trim().Length > 0).All(OrderedItem().IsMatch);
            int n = 1;
            for (int i = 0; i < lines.Count; i++)
            {
                if (allOrdered)
                {
                    Match m = OrderedItem().Match(lines[i]);
                    if (m.Success)
                    {
                        lines[i] = m.Groups[1].Value + lines[i][m.Length..];
                    }
                }
                else if (lines[i].Trim().Length > 0)
                {
                    lines[i] = $"{n++}. {lines[i]}";
                }
            }
        });
    }

    // ---- fenced code block --------------------------------------------------

    private static BitMarkdownEditorEditResult CodeBlock(string text, int start, int end)
    {
        string prefix = LeadingBlankLinePrefix(text, start);
        // Keep the closing fence on its own line when text follows the insertion point.
        string suffix = end < text.Length && text[end] != '\n' ? "\n" : string.Empty;

        string selected = text[start..end];
        if (start == end)
        {
            string insert = prefix + "```\n\n```" + suffix;
            // caret on the empty middle line
            int caret = start + prefix.Length + 4;
            return new BitMarkdownEditorEditResult(true, text[..start] + insert + text[end..], caret, caret);
        }

        string body = selected.TrimEnd('\n');
        string fenced = prefix + $"```\n{body}\n```" + suffix;
        int selStart = start + prefix.Length + 4;
        return new BitMarkdownEditorEditResult(true, text[..start] + fenced + text[end..], selStart, selStart + body.Length);
    }

    // ---- links & images -----------------------------------------------------

    private static BitMarkdownEditorEditResult LinkOrImage(string text, int start, int end, bool isImage)
    {
        string bang = isImage ? "!" : string.Empty;
        string selected = text[start..end];
        if (start == end)
        {
            string label = isImage ? "alt" : "text";
            string insert = $"{bang}[{label}](url)";
            int selStart = start + bang.Length + 1; // inside the [..]
            return new BitMarkdownEditorEditResult(true, text[..start] + insert + text[end..], selStart, selStart + label.Length);
        }

        // Use the selection as the label and drop the caret on the url placeholder.
        string built = $"{bang}[{selected}](url)";
        int urlStart = start + bang.Length + 1 + selected.Length + 2; // after "](".
        return new BitMarkdownEditorEditResult(true, text[..start] + built + text[end..], urlStart, urlStart + 3);
    }

    // ---- table --------------------------------------------------------------

    private static BitMarkdownEditorEditResult Table(string text, int start, int end)
    {
        string prefix = LeadingBlankLinePrefix(text, start);
        const string template =
            "| Column 1 | Column 2 |\n" +
            "| -------- | -------- |\n" +
            "| Cell     | Cell     |\n";
        string insert = prefix + template;
        int sel = start + prefix.Length + 2; // start of "Column 1"
        return new BitMarkdownEditorEditResult(true, text[..start] + insert + text[end..], sel, sel + "Column 1".Length);
    }

    // ---- horizontal rule ----------------------------------------------------

    private static BitMarkdownEditorEditResult HorizontalRule(string text, int start, int end)
    {
        string prefix = LeadingBlankLinePrefix(text, start);
        string insert = prefix + "---\n";
        int caret = start + insert.Length;
        return new BitMarkdownEditorEditResult(true, text[..start] + insert + text[end..], caret, caret);
    }

    // ---- indentation --------------------------------------------------------

    private static BitMarkdownEditorEditResult Indent(string text, int start, int end, string indentUnit)
    {
        if (start == end)
        {
            string ins = indentUnit;
            return new BitMarkdownEditorEditResult(true, text[..start] + ins + text[end..], start + ins.Length, start + ins.Length);
        }

        return TransformBlock(text, start, end, lines =>
        {
            for (int i = 0; i < lines.Count; i++)
            {
                lines[i] = indentUnit + lines[i];
            }
        });
    }

    private static BitMarkdownEditorEditResult Outdent(string text, int start, int end, string indentUnit)
    {
        return TransformBlock(text, start, end, lines =>
        {
            for (int i = 0; i < lines.Count; i++)
            {
                lines[i] = RemoveOneIndent(lines[i], indentUnit);
            }
        });
    }

    private static string RemoveOneIndent(string line, string indentUnit)
    {
        if (line.StartsWith('\t'))
        {
            return line[1..];
        }

        int spaces = 0;
        int max = indentUnit.Length == 0 ? 2 : indentUnit.Length;
        while (spaces < max && spaces < line.Length && line[spaces] == ' ')
        {
            spaces++;
        }

        return line[spaces..];
    }

    // ---- smart newline (list / quote continuation) --------------------------

    private static BitMarkdownEditorEditResult NewLine(string text, int start, int end)
    {
        int lineStart = LineStartIndex(text, start);
        int lineEnd = LineEndIndex(text, start);
        string fullLine = text[lineStart..lineEnd];

        Match task = TaskItem().Match(fullLine);
        Match unordered = UnorderedItem().Match(fullLine);
        Match ordered = OrderedItem().Match(fullLine);
        Match quote = QuoteItem().Match(fullLine);

        // Continue task list
        if (task.Success)
        {
            string content = fullLine[task.Length..];
            if (content.Trim().Length == 0)
            {
                return ClearLine(text, lineStart, lineEnd);
            }

            string insertion = "\n" + task.Groups[1].Value + "- [ ] ";
            int caret = start + insertion.Length;
            return new BitMarkdownEditorEditResult(true, text[..start] + insertion + text[end..], caret, caret);
        }

        // Continue unordered list
        if (unordered.Success)
        {
            string content = fullLine[unordered.Length..];
            if (content.Trim().Length == 0)
            {
                return ClearLine(text, lineStart, lineEnd);
            }

            string insertion = "\n" + unordered.Groups[1].Value + unordered.Groups[2].Value + " ";
            int caret = start + insertion.Length;
            return new BitMarkdownEditorEditResult(true, text[..start] + insertion + text[end..], caret, caret);
        }

        // Continue ordered list (increment the number)
        if (ordered.Success)
        {
            string content = fullLine[ordered.Length..];
            if (content.Trim().Length == 0)
            {
                return ClearLine(text, lineStart, lineEnd);
            }

            int number = int.TryParse(ordered.Groups[2].Value, out int parsed) ? parsed + 1 : 1;
            string insertion = "\n" + ordered.Groups[1].Value + number + ordered.Groups[3].Value + " ";
            int caret = start + insertion.Length;
            return new BitMarkdownEditorEditResult(true, text[..start] + insertion + text[end..], caret, caret);
        }

        // Continue blockquote
        if (quote.Success)
        {
            string content = fullLine[quote.Length..];
            if (content.Trim().Length == 0)
            {
                return ClearLine(text, lineStart, lineEnd);
            }

            string insertion = "\n" + quote.Groups[1].Value + "> ";
            int caret = start + insertion.Length;
            return new BitMarkdownEditorEditResult(true, text[..start] + insertion + text[end..], caret, caret);
        }

        // Default: plain newline, preserving the current line's leading whitespace.
        string indent = LeadingWhitespace().Match(fullLine).Value;
        string plain = "\n" + indent;
        int pos = start + plain.Length;
        return new BitMarkdownEditorEditResult(true, text[..start] + plain + text[end..], pos, pos);
    }

    private static BitMarkdownEditorEditResult ClearLine(string text, int lineStart, int lineEnd) =>
        new(true, text[..lineStart] + text[lineEnd..], lineStart, lineStart);

    // ---- shared helpers -----------------------------------------------------

    /// <summary>
    /// Runs <paramref name="transform"/> over every full line touched by the selection.
    /// </summary>
    private static BitMarkdownEditorEditResult TransformBlock(string text, int start, int end, Action<List<string>> transform)
    {
        int blockStart = LineStartIndex(text, start);
        int effEnd = end;
        if (effEnd > start && effEnd > 0 && text[effEnd - 1] == '\n')
        {
            effEnd--;
        }

        int blockEnd = LineEndIndex(text, effEnd);

        string block = text[blockStart..blockEnd];
        List<string> lines = [.. block.Split('\n')];
        transform(lines);
        string rebuilt = string.Join('\n', lines);

        string newText = text[..blockStart] + rebuilt + text[blockEnd..];
        return new BitMarkdownEditorEditResult(true, newText, blockStart, blockStart + rebuilt.Length);
    }

    private static int LineStartIndex(string text, int p)
    {
        if (p <= 0) return 0;

        int idx = text.LastIndexOf('\n', p - 1);
        return idx + 1;
    }

    private static int LineEndIndex(string text, int p)
    {
        int idx = text.IndexOf('\n', Math.Min(p, text.Length));
        return idx < 0 ? text.Length : idx;
    }

    /// <summary>
    /// Block elements like tables and thematic breaks must be separated from the
    /// preceding paragraph by a blank line ("---" right below a text line would be
    /// parsed as a setext heading). Returns the newlines needed before insertion.
    /// </summary>
    private static string LeadingBlankLinePrefix(string text, int pos)
    {
        if (pos == 0) return string.Empty;

        // Mid-line: end the current line and add a blank one.
        if (text[pos - 1] != '\n') return "\n\n";

        // At the start of a line right below a non-empty one: add a blank line.
        if (pos >= 2 && text[pos - 2] != '\n') return "\n";

        return string.Empty;
    }

    [GeneratedRegex(@"^(#{1,6}) ")]
    private static partial Regex HeadingPrefix();

    [GeneratedRegex(@"^> ")]
    private static partial Regex QuotePrefix();

    // group 1 = leading whitespace (mirrors the Enter key handler's QUOTE_LINE)
    [GeneratedRegex(@"^(\s*)> ")]
    private static partial Regex QuoteItem();

    // group 1 = leading whitespace, group 2 = bullet char
    [GeneratedRegex(@"^(\s*)([-*+]) (?!\[[ xX]\])")]
    private static partial Regex UnorderedItem();

    // group 1 = leading whitespace
    [GeneratedRegex(@"^(\s*)[-*+] \[[ xX]\] ")]
    private static partial Regex TaskItem();

    // group 1 = leading whitespace, group 2 = number, group 3 = delimiter (. or ))
    [GeneratedRegex(@"^(\s*)(\d+)([.)]) ")]
    private static partial Regex OrderedItem();

    [GeneratedRegex(@"^[ \t]*")]
    private static partial Regex LeadingWhitespace();
}
