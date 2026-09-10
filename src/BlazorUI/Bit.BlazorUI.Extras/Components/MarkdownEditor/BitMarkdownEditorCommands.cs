using System.Text;
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
        => Apply(command, text, start, end, new BitMarkdownEditorCommandOptions { IndentUnit = indentUnit });

    /// <summary>
    /// Applies <paramref name="command"/> to <paramref name="text"/> for the given selection.
    /// </summary>
    /// <param name="command">The command to run.</param>
    /// <param name="text">Full editor text (LF line endings).</param>
    /// <param name="start">Selection start index.</param>
    /// <param name="end">Selection end index.</param>
    /// <param name="options">Everything the commands need beside the text and the selection.</param>
    public static BitMarkdownEditorEditResult Apply(BitMarkdownEditorCommand command, string text, int start, int end, BitMarkdownEditorCommandOptions options)
    {
        text ??= string.Empty;
        options ??= BitMarkdownEditorCommandOptions.Default;
        // An empty indent unit would make Indent a no-op that still reported a change.
        var indentUnit = string.IsNullOrEmpty(options.IndentUnit) ? "  " : options.IndentUnit;
        start = Math.Clamp(start, 0, text.Length);
        end = Math.Clamp(end, 0, text.Length);
        if (end < start)
        {
            (start, end) = (end, start);
        }

        return command switch
        {
            BitMarkdownEditorCommand.Bold => ToggleWrap(text, start, end, options.BoldMarker, "bold text"),
            BitMarkdownEditorCommand.Italic => ToggleWrap(text, start, end, options.ItalicMarker, "italic text"),
            BitMarkdownEditorCommand.Strikethrough => ToggleWrap(text, start, end, "~~", "strikethrough"),
            BitMarkdownEditorCommand.InlineCode => ToggleWrap(text, start, end, "`", "code"),
            BitMarkdownEditorCommand.Heading1 => Heading(text, start, end, 1),
            BitMarkdownEditorCommand.Heading2 => Heading(text, start, end, 2),
            BitMarkdownEditorCommand.Heading3 => Heading(text, start, end, 3),
            BitMarkdownEditorCommand.Heading4 => Heading(text, start, end, 4),
            BitMarkdownEditorCommand.Heading5 => Heading(text, start, end, 5),
            BitMarkdownEditorCommand.Heading6 => Heading(text, start, end, 6),
            BitMarkdownEditorCommand.Superscript => ToggleWrap(text, start, end, "^", "sup"),
            BitMarkdownEditorCommand.Subscript => ToggleWrap(text, start, end, "~", "sub"),
            BitMarkdownEditorCommand.ClearFormatting => ClearFormatting(text, start, end),
            BitMarkdownEditorCommand.Quote => LinePrefixToggle(text, start, end, "> ", QuotePrefix()),
            BitMarkdownEditorCommand.UnorderedList => UnorderedList(text, start, end, options.Bullet),
            BitMarkdownEditorCommand.OrderedList => OrderedList(text, start, end),
            BitMarkdownEditorCommand.TaskList => TaskList(text, start, end, options.Bullet),
            BitMarkdownEditorCommand.CodeBlock => CodeBlock(text, start, end),
            BitMarkdownEditorCommand.Link => LinkOrImage(text, start, end, isImage: false),
            BitMarkdownEditorCommand.Image => LinkOrImage(text, start, end, isImage: true),
            BitMarkdownEditorCommand.Table => Table(text, start, end, options.TableColumns, options.TableRows),
            BitMarkdownEditorCommand.HorizontalRule => HorizontalRule(text, start, end),
            BitMarkdownEditorCommand.Indent => Indent(text, start, end, indentUnit),
            BitMarkdownEditorCommand.Outdent => Outdent(text, start, end, indentUnit),
            BitMarkdownEditorCommand.NewLine => NewLine(text, start, end),
            BitMarkdownEditorCommand.MoveLineUp => MoveLines(text, start, end, up: true),
            BitMarkdownEditorCommand.MoveLineDown => MoveLines(text, start, end, up: false),
            BitMarkdownEditorCommand.DuplicateLine => DuplicateLines(text, start, end),
            BitMarkdownEditorCommand.DeleteLine => DeleteLines(text, start, end),
            BitMarkdownEditorCommand.TableInsertRowAbove => TableInsertRow(text, start, end, below: false),
            BitMarkdownEditorCommand.TableInsertRowBelow => TableInsertRow(text, start, end, below: true),
            BitMarkdownEditorCommand.TableDeleteRow => TableDeleteRow(text, start, end),
            BitMarkdownEditorCommand.TableInsertColumnBefore => TableInsertColumn(text, start, end, after: false),
            BitMarkdownEditorCommand.TableInsertColumnAfter => TableInsertColumn(text, start, end, after: true),
            BitMarkdownEditorCommand.TableDeleteColumn => TableDeleteColumn(text, start, end),
            BitMarkdownEditorCommand.TableAlignLeft => TableAlign(text, start, end, 'l'),
            BitMarkdownEditorCommand.TableAlignCenter => TableAlign(text, start, end, 'c'),
            BitMarkdownEditorCommand.TableAlignRight => TableAlign(text, start, end, 'r'),
            BitMarkdownEditorCommand.TableNextCell => TableMoveCell(text, start, end, forward: true),
            BitMarkdownEditorCommand.TablePreviousCell => TableMoveCell(text, start, end, forward: false),
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
            IsWholeMarkerDelimiter(text, start - ml, marker) && IsWholeMarkerDelimiter(text, end, marker))
        {
            string unwrapped = text[..(start - ml)] + selected + text[(end + ml)..];
            return new BitMarkdownEditorEditResult(true, unwrapped, start - ml, end - ml);
        }

        // Markers captured inside the selection? -> unwrap.
        if (selected.Length >= 2 * ml && selected.StartsWith(marker, StringComparison.Ordinal) && selected.EndsWith(marker, StringComparison.Ordinal) &&
            IsWholeMarkerDelimiter(selected, 0, marker) && IsWholeMarkerDelimiter(selected, selected.Length - 1, marker))
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
    /// A lone marker char that is part of a longer run of the same character belongs to
    /// the multi-char marker (a single '*' inside '**' is bold; a single '~' inside '~~'
    /// is strikethrough). The char at <paramref name="index"/> counts as a single-char
    /// delimiter only when its contiguous run has an odd length (the unpaired char is the
    /// single marker).
    /// </summary>
    private static bool IsSingleCharDelimiter(string text, int index, char c)
    {
        int s = index;
        while (s > 0 && text[s - 1] == c) s--;

        int e = index;
        while (e < text.Length - 1 && text[e + 1] == c) e++;

        return (e - s + 1) % 2 == 1;
    }

    /// <summary>
    /// True when the marker occurrence at <paramref name="index"/> is a self-contained
    /// delimiter rather than part of a longer run of the same character. Only single-char
    /// markers that also form a double marker are ambiguous ('*' vs '**', '~' vs '~~');
    /// every other marker is always a whole delimiter.
    /// </summary>
    private static bool IsWholeMarkerDelimiter(string text, int index, string marker)
        => marker is not ("*" or "~" or "_") || IsSingleCharDelimiter(text, index, marker[0]);

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

    private static BitMarkdownEditorEditResult UnorderedList(string text, int start, int end, string bullet)
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
                    lines[i] = bullet + " " + lines[i];
                }
            }
        });
    }

    private static BitMarkdownEditorEditResult TaskList(string text, int start, int end, string bullet)
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
                    lines[i] = bullet + " [ ] " + lines[i];
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

    // ---- clear formatting ---------------------------------------------------

    private static BitMarkdownEditorEditResult ClearFormatting(string text, int start, int end)
    {
        // With no selection TransformBlock already widens to the whole current line, so a
        // single click on an empty selection still does something useful - and the caret
        // survives it instead of the line ending up selected.
        return TransformBlock(text, start, end, lines =>
        {
            for (int i = 0; i < lines.Count; i++)
            {
                lines[i] = StripInlineMarkers(BlockPrefix().Replace(lines[i], string.Empty));
            }
        });
    }

    // Removes paired inline emphasis/code markers, keeping the inner text intact.
    private static string StripInlineMarkers(string line)
    {
        // Links and images collapse to their label first, so the emphasis inside the label
        // is stripped by the passes below instead of being left behind with the url.
        line = LinkMarker().Replace(line, "$1");
        // Both spellings are stripped whatever the editor is configured to write, since the
        // text being cleaned was not necessarily written by this editor.
        line = BoldMarker().Replace(line, "$1");
        line = UnderscoreBoldMarker().Replace(line, "$1");
        line = StrikeMarker().Replace(line, "$1");
        line = ItalicMarker().Replace(line, "$1");
        line = UnderscoreItalicMarker().Replace(line, "$1");
        line = InlineCodeMarker().Replace(line, "$1");
        return line;
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

        // A selected URL is the target, not the label: keep it and put the caret on the
        // label placeholder instead, which is what the user still has to type.
        if (UrlLike().IsMatch(selected))
        {
            string label = isImage ? "alt" : "text";
            string fromUrl = $"{bang}[{label}]({selected})";
            int labelStart = start + bang.Length + 1;
            return new BitMarkdownEditorEditResult(true, text[..start] + fromUrl + text[end..], labelStart, labelStart + label.Length);
        }

        // Use the selection as the label and drop the caret on the url placeholder.
        string built = $"{bang}[{selected}](url)";
        int urlStart = start + bang.Length + 1 + selected.Length + 2; // after "](".
        return new BitMarkdownEditorEditResult(true, text[..start] + built + text[end..], urlStart, urlStart + 3);
    }

    // ---- table --------------------------------------------------------------

    private static BitMarkdownEditorEditResult Table(string text, int start, int end, int columns, int rows)
    {
        columns = Math.Max(1, columns);
        rows = Math.Max(0, rows);

        string prefix = LeadingBlankLinePrefix(text, start);

        // Each column is as wide as its header, so the source stays aligned in the textarea.
        string[] headers = [.. Enumerable.Range(1, columns).Select(i => $"Column {i}")];

        var sb = new StringBuilder(prefix);
        AppendRow(sb, headers, headers);
        AppendRow(sb, [.. headers.Select(h => new string('-', h.Length))], headers);
        for (int r = 0; r < rows; r++)
        {
            AppendRow(sb, [.. headers.Select(_ => "Cell")], headers);
        }

        string insert = sb.ToString();
        int sel = start + prefix.Length + 2; // start of "Column 1"
        return new BitMarkdownEditorEditResult(true, text[..start] + insert + text[end..], sel, sel + headers[0].Length);
    }

    private static void AppendRow(StringBuilder sb, string[] cells, string[] headers)
    {
        sb.Append('|');
        for (int i = 0; i < cells.Length; i++)
        {
            sb.Append(' ').Append(cells[i].PadRight(headers[i].Length)).Append(" |");
        }
        sb.Append('\n');
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
        // Inside a table there is no indenting to do, and Tab is how the cells are walked
        // through - which is what the key does in every editor that knows about tables.
        if (start == end && ReadTable(text, start) is not null) return TableMoveCell(text, start, end, forward: true);

        // A caret inside a list item or a quote indents the whole line: Tab is how a nested
        // list is reached from the keyboard, and pushing spaces into the middle of the text
        // instead would make nesting impossible without first selecting the line.
        if (start == end && IsIndentableLine(text, start) is false)
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
        if (start == end && ReadTable(text, start) is not null) return TableMoveCell(text, start, end, forward: false);

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

    /// <summary>
    /// True when the line the caret sits on carries a block marker that indenting is meant
    /// to nest (a bullet, a numbered item, a task or a quote).
    /// </summary>
    private static bool IsIndentableLine(string text, int pos)
    {
        string line = text[LineStartIndex(text, pos)..LineEndIndex(text, pos)];

        return TaskItem().IsMatch(line) ||
               UnorderedItem().IsMatch(line) ||
               OrderedItem().IsMatch(line) ||
               QuoteItem().IsMatch(line);
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
            string inserted = text[..start] + insertion + text[end..];

            // Renumber the ordered items that follow the newly inserted line so the
            // sequence stays consecutive. Only text after the caret changes, so the
            // caret index is unaffected.
            inserted = RenumberOrderedFrom(inserted, caret, ordered.Groups[1].Value.Length);
            return new BitMarkdownEditorEditResult(true, inserted, caret, caret);
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

    // ---- whole-line operations (move / duplicate / delete) ------------------

    /// <summary>
    /// Swaps the block of lines touched by the selection with the line above or below it,
    /// carrying the selection along so the caret keeps pointing at the same characters.
    /// </summary>
    private static BitMarkdownEditorEditResult MoveLines(string text, int start, int end, bool up)
    {
        (int blockStart, int blockEnd) = BlockBounds(text, start, end);
        string block = text[blockStart..blockEnd];

        if (up)
        {
            // Already the first line of the document: nothing above to swap with.
            if (blockStart == 0) return BitMarkdownEditorEditResult.NotHandled(text, start, end);

            int prevStart = LineStartIndex(text, blockStart - 1);
            string previous = text[prevStart..(blockStart - 1)];
            string moved = text[..prevStart] + block + "\n" + previous + text[blockEnd..];
            int delta = prevStart - blockStart;
            return new(true, moved, start + delta, end + delta);
        }

        // Already the last line of the document: nothing below to swap with.
        if (blockEnd >= text.Length) return BitMarkdownEditorEditResult.NotHandled(text, start, end);

        int nextStart = blockEnd + 1;
        int nextEnd = LineEndIndex(text, nextStart);
        string next = text[nextStart..nextEnd];
        string result = text[..blockStart] + next + "\n" + block + text[nextEnd..];
        int offset = next.Length + 1;
        return new(true, result, start + offset, end + offset);
    }

    /// <summary>
    /// Copies the block of lines touched by the selection right below itself and moves the
    /// selection onto the copy, so repeated invocations keep duplicating the newest copy.
    /// </summary>
    private static BitMarkdownEditorEditResult DuplicateLines(string text, int start, int end)
    {
        (int blockStart, int blockEnd) = BlockBounds(text, start, end);
        string block = text[blockStart..blockEnd];

        string duplicated = text[..blockEnd] + "\n" + block + text[blockEnd..];
        int offset = block.Length + 1;
        return new(true, duplicated, start + offset, end + offset);
    }

    /// <summary>
    /// Removes the block of lines touched by the selection together with one of the newlines
    /// around it, so no blank line is left behind.
    /// </summary>
    private static BitMarkdownEditorEditResult DeleteLines(string text, int start, int end)
    {
        (int blockStart, int blockEnd) = BlockBounds(text, start, end);

        int removeStart = blockStart;
        int removeEnd = blockEnd;
        if (removeEnd < text.Length)
        {
            // Eat the trailing newline so the following line moves up.
            removeEnd++;
        }
        else if (removeStart > 0)
        {
            // Last line of the document: eat the preceding newline instead.
            removeStart--;
        }

        string deleted = text[..removeStart] + text[removeEnd..];
        int caret = Math.Min(removeStart, deleted.Length);
        return new(true, deleted, caret, caret);
    }

    // ---- shared helpers -----------------------------------------------------

    /// <summary>
    /// The start and end index of the run of whole lines the selection touches. A selection
    /// ending exactly on a line break does not pull the next (unselected) line into the block.
    /// </summary>
    private static (int Start, int End) BlockBounds(string text, int start, int end)
    {
        int effEnd = end;
        if (effEnd > start && effEnd > 0 && text[effEnd - 1] == '\n')
        {
            effEnd--;
        }

        return (LineStartIndex(text, start), LineEndIndex(text, effEnd));
    }

    /// <summary>
    /// Runs <paramref name="transform"/> over every full line touched by the selection.
    /// </summary>
    private static BitMarkdownEditorEditResult TransformBlock(string text, int start, int end, Action<List<string>> transform)
    {
        (int blockStart, int blockEnd) = BlockBounds(text, start, end);

        string block = text[blockStart..blockEnd];
        List<string> original = [.. block.Split('\n')];
        List<string> lines = [.. original];
        transform(lines);
        string rebuilt = string.Join('\n', lines);

        string newText = text[..blockStart] + rebuilt + text[blockEnd..];

        // A caret stays a caret. Toggling a heading or outdenting from a bare caret used to
        // leave the whole line selected, throwing away where the user was about to type.
        if (start == end)
        {
            int caret = CaretAfterTransform(original, lines, blockStart, start);
            return new BitMarkdownEditorEditResult(true, newText, caret, caret);
        }

        return new BitMarkdownEditorEditResult(true, newText, blockStart, blockStart + rebuilt.Length);
    }

    /// <summary>
    /// Where a caret lands after the line it sits on was rewritten. The rewrite is read as the
    /// span between the parts of the line that did not change, so a caret in front of it stays
    /// put and one behind it travels by exactly what the line grew or shrank.
    /// </summary>
    private static int CaretAfterTransform(List<string> original, List<string> lines, int blockStart, int caret)
    {
        int oldOffset = blockStart;
        int newOffset = blockStart;

        for (int i = 0; i < original.Count; i++)
        {
            string oldLine = original[i];
            string newLine = i < lines.Count ? lines[i] : string.Empty;
            int lineEnd = oldOffset + oldLine.Length;

            if (caret <= lineEnd || i == original.Count - 1)
            {
                return newOffset + CaretInLine(oldLine, newLine, Math.Clamp(caret - oldOffset, 0, oldLine.Length));
            }

            oldOffset = lineEnd + 1;
            newOffset += newLine.Length + 1;
        }

        return caret;
    }

    /// <summary>
    /// Maps a caret offset from a line to its rewritten self, by the unchanged text on either
    /// side of the edit.
    /// </summary>
    private static int CaretInLine(string oldLine, string newLine, int caret)
    {
        int max = Math.Min(oldLine.Length, newLine.Length);

        int prefix = 0;
        while (prefix < max && oldLine[prefix] == newLine[prefix]) prefix++;

        int suffix = 0;
        while (suffix < max - prefix && oldLine[oldLine.Length - suffix - 1] == newLine[newLine.Length - suffix - 1]) suffix++;

        // Behind the edit: moved by what the line gained or lost. Tested first, so a caret at
        // the very start of a line that gained a marker travels with the text rather than
        // being left stranded in front of the new "# ".
        if (caret >= oldLine.Length - suffix) return caret + (newLine.Length - oldLine.Length);

        // In front of it: unmoved.
        if (caret <= prefix) return caret;

        // Inside the rewritten span, where there is nothing to anchor to: keep it as close to
        // where it was as the new span allows.
        return Math.Min(caret, newLine.Length - suffix);
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

    /// <summary>
    /// Renumbers the run of ordered-list items at <paramref name="indentLen"/> indentation
    /// that immediately follows the line containing <paramref name="caret"/>. Only text after
    /// the caret line is rewritten, so the caret index stays valid.
    /// </summary>
    private static string RenumberOrderedFrom(string text, int caret, int indentLen)
    {
        int lineStart = LineStartIndex(text, caret);
        int lineEnd = LineEndIndex(text, caret);
        Match current = OrderedItem().Match(text[lineStart..lineEnd]);
        if (current.Success is false || int.TryParse(current.Groups[2].Value, out int number) is false)
        {
            return text;
        }

        var sb = new StringBuilder(text[..lineEnd]);
        int idx = lineEnd;
        while (idx < text.Length && text[idx] == '\n')
        {
            int nextStart = idx + 1;
            int nextEnd = LineEndIndex(text, nextStart);
            string lineText = text[nextStart..nextEnd];
            Match m = OrderedItem().Match(lineText);
            if (m.Success is false || m.Groups[1].Value.Length != indentLen)
            {
                break;
            }

            number++;
            sb.Append('\n').Append(m.Groups[1].Value).Append(number).Append(m.Groups[3].Value).Append(' ').Append(lineText[m.Length..]);
            idx = nextEnd;
        }

        sb.Append(text[idx..]);
        return sb.ToString();
    }

    /// <summary>
    /// Determines which formatting commands are "active" for the given selection, so the
    /// toolbar can reflect the caret's context (e.g. highlight Bold inside <c>**bold**</c>).
    /// Pure and side-effect free.
    /// </summary>
    public static IReadOnlyCollection<BitMarkdownEditorCommand> DetectActiveFormats(string text, int start, int end)
        => DetectActiveFormats(text, start, end, BitMarkdownEditorCommandOptions.Default);

    /// <summary>
    /// Determines which formatting commands are "active" for the given selection, reading the
    /// emphasis spelling the editor is configured to write. Pure and side effect free.
    /// </summary>
    public static IReadOnlyCollection<BitMarkdownEditorCommand> DetectActiveFormats(string text, int start, int end, BitMarkdownEditorCommandOptions options)
    {
        options ??= BitMarkdownEditorCommandOptions.Default;

        var set = new HashSet<BitMarkdownEditorCommand>();
        if (string.IsNullOrEmpty(text)) return set;

        start = Math.Clamp(start, 0, text.Length);
        end = Math.Clamp(end, 0, text.Length);
        if (end < start)
        {
            (start, end) = (end, start);
        }

        // Block-level formats come from the line containing the selection start.
        string line = text[LineStartIndex(text, start)..LineEndIndex(text, start)];
        Match heading = HeadingPrefix().Match(line);
        if (heading.Success)
        {
            set.Add(heading.Groups[1].Value.Length switch
            {
                1 => BitMarkdownEditorCommand.Heading1,
                2 => BitMarkdownEditorCommand.Heading2,
                3 => BitMarkdownEditorCommand.Heading3,
                4 => BitMarkdownEditorCommand.Heading4,
                5 => BitMarkdownEditorCommand.Heading5,
                _ => BitMarkdownEditorCommand.Heading6
            });
        }
        if (QuoteItem().IsMatch(line)) set.Add(BitMarkdownEditorCommand.Quote);
        if (TaskItem().IsMatch(line)) set.Add(BitMarkdownEditorCommand.TaskList);
        else if (UnorderedItem().IsMatch(line)) set.Add(BitMarkdownEditorCommand.UnorderedList);
        if (OrderedItem().IsMatch(line)) set.Add(BitMarkdownEditorCommand.OrderedList);

        // Inline formats.
        if (IsWrapped(text, start, end, options.BoldMarker)) set.Add(BitMarkdownEditorCommand.Bold);
        if (IsWrapped(text, start, end, "~~")) set.Add(BitMarkdownEditorCommand.Strikethrough);
        if (IsWrapped(text, start, end, "`")) set.Add(BitMarkdownEditorCommand.InlineCode);
        if (IsWrapped(text, start, end, "^")) set.Add(BitMarkdownEditorCommand.Superscript);
        if (IsWrapped(text, start, end, "~") && IsWrapped(text, start, end, "~~") is false)
        {
            set.Add(BitMarkdownEditorCommand.Subscript);
        }
        // A caret is inside emphasis when an odd number of markers precede it on the line; a
        // selection has to carry the delimiters itself. Bold's doubled marker counts even
        // either way, so a caret in **bold** never reports italic alongside it.
        char italic = options.ItalicMarker[0];
        if (start == end)
        {
            if (IsWrapped(text, start, end, options.ItalicMarker)) set.Add(BitMarkdownEditorCommand.Italic);
        }
        else if (text[start..end] is { Length: >= 2 } sel &&
                 sel[0] == italic && sel[^1] == italic &&
                 IsSingleCharDelimiter(sel, 0, italic) && IsSingleCharDelimiter(sel, sel.Length - 1, italic))
        {
            set.Add(BitMarkdownEditorCommand.Italic);
        }

        return set;
    }

    private static bool IsWrapped(string text, int start, int end, string marker)
    {
        int ml = marker.Length;
        if (start >= ml && end + ml <= text.Length &&
            text.Substring(start - ml, ml) == marker && text.Substring(end, ml) == marker)
        {
            return true;
        }

        string selected = text[start..end];
        if (selected.Length >= 2 * ml && selected.StartsWith(marker, StringComparison.Ordinal) && selected.EndsWith(marker, StringComparison.Ordinal))
        {
            return true;
        }

        // Caret with no selection: inside a span when an odd number of markers precede it
        // on the line and a closing marker follows.
        if (start == end)
        {
            string before = text[LineStartIndex(text, start)..start];
            string after = text[start..LineEndIndex(text, start)];
            return CountOccurrences(before, marker) % 2 == 1 && after.Contains(marker, StringComparison.Ordinal);
        }

        return false;
    }

    private static int CountOccurrences(string haystack, string needle)
    {
        int count = 0, i = 0;
        while ((i = haystack.IndexOf(needle, i, StringComparison.Ordinal)) >= 0)
        {
            count++;
            i += needle.Length;
        }
        return count;
    }

    [GeneratedRegex(@"^(#{1,6}) ")]
    private static partial Regex HeadingPrefix();

    // Leading block markers stripped by Clear formatting.
    [GeneratedRegex(@"^(\s*)(#{1,6} |> |[-*+] (\[[ xX]\] )?|\d+[.)] )+")]
    private static partial Regex BlockPrefix();

    [GeneratedRegex(@"\*\*(.+?)\*\*")]
    private static partial Regex BoldMarker();

    [GeneratedRegex(@"~~(.+?)~~")]
    private static partial Regex StrikeMarker();

    [GeneratedRegex(@"(?<!\*)\*(?!\*)(.+?)(?<!\*)\*(?!\*)")]
    private static partial Regex ItalicMarker();

    [GeneratedRegex(@"__(.+?)__")]
    private static partial Regex UnderscoreBoldMarker();

    [GeneratedRegex(@"(?<!_)_(?!_)(.+?)(?<!_)_(?!_)")]
    private static partial Regex UnderscoreItalicMarker();

    [GeneratedRegex(@"`(.+?)`")]
    private static partial Regex InlineCodeMarker();

    // An inline link or image: group 1 is the label kept by Clear formatting.
    [GeneratedRegex(@"!?\[([^\]]*)\]\([^)\s]*(?:\s+""[^""]*"")?\)")]
    private static partial Regex LinkMarker();

    // A selection that is nothing but an absolute url (or a mail/anchor/relative target).
    [GeneratedRegex(@"^(?:https?://|mailto:|tel:|ftp://|/|\./|\.\./|#)\S+$", RegexOptions.IgnoreCase)]
    private static partial Regex UrlLike();

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
