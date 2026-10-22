using System.Text;

namespace Bit.BlazorUI;

/// <summary>
/// Parses Markdown into a document tree.
/// </summary>
/// <remarks>
/// <para>
/// A CommonMark subset with the GitHub-flavoured extensions that models actually emit: tables, task
/// lists, strikethrough, autolinks, footnotes, and mathematics. It is not a conformance-complete
/// CommonMark implementation, and it does not try to be: the priority is parsing a half-written
/// response forty times a second without allocating a new tree each time.
/// </para>
/// <para>
/// Every block records whether the source ended inside it, which is what makes streaming look
/// settled rather than jittery. See <see cref="BitAiIncrementalMarkdownParser"/> for the streaming entry
/// point.
/// </para>
/// <para>
/// Link reference definitions and footnote definitions may follow the text that uses them, so a
/// document that contains any is parsed twice: once to collect the definitions and once to resolve
/// the references. Documents without definitions, which is nearly all of them, are parsed once.
/// </para>
/// </remarks>
public static class BitAiMarkdownParser
{
    /// <summary>
    /// Parses Markdown into a document.
    /// </summary>
    /// <param name="markdown">The source.</param>
    /// <param name="options">Which extensions to enable. Defaults to <see cref="BitAiMarkdownOptions.Default"/>.</param>
    /// <param name="isComplete">
    /// <see langword="false"/> when more source is expected, which marks the trailing block open.
    /// </param>
    /// <returns>The document.</returns>
    public static BitAiMarkdownDocument Parse(string? markdown, BitAiMarkdownOptions? options = null, bool isComplete = true)
    {
        if (string.IsNullOrEmpty(markdown)) return new BitAiMarkdownDocument();
        return Parse(markdown, options ?? BitAiMarkdownOptions.Default, isComplete, null, null, out _);
    }

    /// <summary>
    /// Parses Markdown and reports where each top-level block starts, which is how
    /// <see cref="BitAiIncrementalMarkdownParser"/> learns where it can safely cut.
    /// </summary>
    /// <param name="markdown">The source.</param>
    /// <param name="options">Which extensions to enable.</param>
    /// <param name="isComplete"><see langword="false"/> when more source is expected.</param>
    /// <param name="blockStarts">
    /// Receives, for each top-level block in order, the offset in <paramref name="markdown"/> of the
    /// line it starts on. May be <see langword="null"/>.
    /// </param>
    /// <param name="usedHeadingIds">
    /// Heading identifiers already taken by text before this source, so the ones assigned here do
    /// not collide with them.
    /// </param>
    /// <param name="sawDefinitions">
    /// Set when the source contains a link reference or footnote definition, which means text
    /// anywhere in the document may depend on text anywhere else.
    /// </param>
    /// <returns>The document.</returns>
    internal static BitAiMarkdownDocument Parse(
        string markdown,
        BitAiMarkdownOptions options,
        bool isComplete,
        List<int>? blockStarts,
        IEnumerable<string>? usedHeadingIds,
        out bool sawDefinitions)
    {
        var document = new BitAiMarkdownDocument();
        sawDefinitions = false;
        if (markdown.Length == 0) return document;

        var lines = SplitLines(markdown);
        var context = new BitAiParseContext(options);
        if (usedHeadingIds is not null) context.HeadingIds.UnionWith(usedHeadingIds);
        ParseBlocks(lines, 0, lines.Count, document.Blocks, context, isComplete, 0, blockStarts);

        if (context.SawDefinitions)
        {
            // A definition may follow the text that cites it, so the first pass could only collect.
            // The second one knows every definition from the start.
            sawDefinitions = true;
            var resolving = new BitAiParseContext(options)
            {
                LinkDefinitions = context.LinkDefinitions,
                FootnoteDefinitions = context.FootnoteDefinitions,
                ResolvesReferences = true,
            };
            if (usedHeadingIds is not null) resolving.HeadingIds.UnionWith(usedHeadingIds);

            document = new BitAiMarkdownDocument();
            blockStarts?.Clear();
            ParseBlocks(lines, 0, lines.Count, document.Blocks, resolving, isComplete, 0, blockStarts);
            ResolveFootnotes(document, resolving);
        }

        if (blockStarts is not null) ConvertLinesToOffsets(markdown, blockStarts);
        return document;
    }

    // Convert line indices to offsets. Both lists ascend, so one pass over the text does it.
    private static void ConvertLinesToOffsets(string markdown, List<int> blockStarts)
    {
        var line = 0;
        var offset = 0;
        for (var i = 0; i < blockStarts.Count; i++)
        {
            for (; line < blockStarts[i]; line++) offset = markdown.IndexOf('\n', offset) + 1;
            blockStarts[i] = offset;
        }
    }

    internal static List<string> SplitLines(string text)
    {
        var lines = new List<string>(Math.Max(4, text.Length / 40));
        var start = 0;

        for (var i = 0; i < text.Length; i++)
        {
            if (text[i] != '\n') continue;
            var end = i > start && text[i - 1] == '\r' ? i - 1 : i;
            lines.Add(text[start..end]);
            start = i + 1;
        }

        lines.Add(text[start..]);
        return lines;
    }

    private static void ParseBlocks(
        List<string> lines,
        int start,
        int end,
        List<BitAiMarkdownBlock> output,
        BitAiParseContext context,
        bool isComplete,
        int depth,
        List<int>? blockStarts = null)
    {
        var options = context.Options;
        var index = start;

        while (index < end)
        {
            var line = lines[index];

            if (IsBlank(line))
            {
                index++;
                continue;
            }

            // Every pass from here adds at most one block, starting on this line. A definition adds
            // none, and takes its start back out so the two lists stay aligned.
            var count = output.Count;
            blockStarts?.Add(index);

            ParseBlock(lines, ref index, end, output, context, isComplete, depth, options);

            if (blockStarts is not null && output.Count == count) blockStarts.RemoveAt(blockStarts.Count - 1);
        }
    }

    private static void ParseBlock(
        List<string> lines,
        ref int index,
        int end,
        List<BitAiMarkdownBlock> output,
        BitAiParseContext context,
        bool isComplete,
        int depth,
        BitAiMarkdownOptions options)
    {
        var line = lines[index];

        if (IndentOf(line) >= 4 && TryIndentedCode(lines, ref index, end, output, isComplete)) return;
        if (TryFencedCode(lines, ref index, end, output, isComplete)) return;
        if (TryThematicBreak(line, ref index, output)) return;
        if (TryHeading(line, ref index, output, context)) return;
        if (options.Math && TryMathBlock(lines, ref index, end, output, isComplete)) return;
        if (depth < options.MaxNestingDepth && TryQuote(lines, ref index, end, output, context, isComplete, depth)) return;
        if (depth < options.MaxNestingDepth && TryList(lines, ref index, end, output, context, isComplete, depth)) return;
        if (options.Tables && TryTable(lines, ref index, end, output, context, isComplete)) return;
        if (options.AllowRawHtml && TryHtmlBlock(lines, ref index, end, output)) return;
        if (TryDefinition(lines, ref index, end, context)) return;
        ParseParagraph(lines, ref index, end, output, context, isComplete);
    }

    private static bool IsBlank(string line)
    {
        foreach (var c in line)
        {
            if (!char.IsWhiteSpace(c)) return false;
        }

        return true;
    }

    private static int NextNonBlank(List<string> lines, int index, int end)
    {
        while (index < end && IsBlank(lines[index])) index++;
        return index;
    }

    private static int IndentOf(string line)
    {
        var indent = 0;
        foreach (var c in line)
        {
            if (c == ' ') indent++;
            else if (c == '\t') indent += 4;
            else break;
        }

        return indent;
    }

    private static bool TryThematicBreak(string line, ref int index, List<BitAiMarkdownBlock> output)
    {
        if (!IsThematicBreak(line)) return false;

        output.Add(new BitAiThematicBreakBlock());
        index++;
        return true;
    }

    private static bool IsThematicBreak(string line)
    {
        var trimmed = line.Trim();
        if (trimmed.Length < 3) return false;

        var marker = trimmed[0];
        if (marker is not ('-' or '*' or '_')) return false;

        var count = 0;
        foreach (var c in trimmed)
        {
            if (c == marker) count++;
            else if (c is not (' ' or '\t')) return false;
        }

        return count >= 3;
    }

    private static bool TryHeading(string line, ref int index, List<BitAiMarkdownBlock> output, BitAiParseContext context)
    {
        var trimmed = line.TrimStart();
        if (trimmed.Length == 0 || trimmed[0] != '#') return false;

        var level = 0;
        while (level < trimmed.Length && trimmed[level] == '#') level++;
        if (level > 6) return false;
        if (level < trimmed.Length && trimmed[level] is not (' ' or '\t')) return false;

        var text = StripClosingSequence(trimmed[level..].Trim());
        output.Add(CreateHeading(level, text, context));
        index++;
        return true;
    }

    private static BitAiHeadingBlock CreateHeading(int level, string text, BitAiParseContext context)
    {
        var heading = new BitAiHeadingBlock(level);
        heading.Inlines.AddRange(BitAiInlineParser.Parse(text, context.Options, isComplete: true, context));
        if (context.Options.HeadingIds) heading.Id = context.ClaimHeadingId(text);
        return heading;
    }

    // The closing run of '#' only counts when a space separates it from the text, or "## Using C#"
    // would lose its last character.
    private static string StripClosingSequence(string text)
    {
        var closing = text.Length;
        while (closing > 0 && text[closing - 1] == '#') closing--;

        if (closing == 0) return string.Empty;
        if (closing == text.Length || text[closing - 1] is not (' ' or '\t')) return text;
        return text[..closing].TrimEnd();
    }

    internal static string Slug(string text)
    {
        var builder = new StringBuilder(text.Length);
        foreach (var c in text)
        {
            if (char.IsLetterOrDigit(c)) builder.Append(char.ToLowerInvariant(c));
            else if (c is ' ' or '-' or '_' && builder.Length > 0 && builder[^1] != '-') builder.Append('-');
        }

        return builder.ToString().Trim('-');
    }

    // Four columns of indentation open a code block, as in CommonMark. It cannot interrupt a
    // paragraph, which ParseParagraph guarantees by continuing through indented lines.
    private static bool TryIndentedCode(List<string> lines, ref int index, int end, List<BitAiMarkdownBlock> output, bool isComplete)
    {
        var cursor = index;
        var lastContent = index;

        while (cursor < end)
        {
            var line = lines[cursor];
            if (IsBlank(line))
            {
                cursor++;
                continue;
            }

            if (IndentOf(line) < 4) break;
            cursor++;
            lastContent = cursor;
        }

        var body = new StringBuilder();
        for (var i = index; i < lastContent; i++)
        {
            if (i > index) body.Append('\n');
            body.Append(StripIndent(lines[i], 4));
        }

        output.Add(new BitAiCodeBlock
        {
            IsFenced = false,
            Code = body.ToString(),
            IsOpen = !isComplete && cursor >= end,
        });

        // Trailing blank lines belong to whatever follows.
        index = lastContent;
        return true;
    }

    private static bool TryFencedCode(
        List<string> lines,
        ref int index,
        int end,
        List<BitAiMarkdownBlock> output,
        bool isComplete)
    {
        var line = lines[index];
        var trimmed = line.TrimStart();
        if (trimmed.Length < 3) return false;

        var fence = trimmed[0];
        if (fence is not ('`' or '~')) return false;

        var fenceLength = 0;
        while (fenceLength < trimmed.Length && trimmed[fenceLength] == fence) fenceLength++;
        if (fenceLength < 3) return false;

        var info = trimmed[fenceLength..].Trim();
        if (fence == '`' && info.Contains('`')) return false;

        var space = info.IndexOf(' ');
        var block = new BitAiCodeBlock
        {
            IsFenced = true,
            Language = info.Length == 0 ? null : (space < 0 ? info : info[..space]),
            Meta = space < 0 ? null : info[(space + 1)..].Trim(),
        };

        var body = new StringBuilder();
        var cursor = index + 1;
        var closed = false;

        while (cursor < end)
        {
            // A closing fence may carry trailing whitespace, as CommonMark allows.
            var candidate = lines[cursor].Trim();
            if (candidate.Length >= fenceLength && candidate.All(c => c == fence))
            {
                closed = true;
                cursor++;
                break;
            }

            // The last line of a block still being written that is the closing fence as far as it got ("``" of "```")
            // is the fence arriving, not code: drawn as code it would show its backticks for a frame and then vanish
            // into the fence. It is held back until it either completes the fence or turns into something else.
            if (!isComplete && cursor == end - 1 && IsFencePrefix(candidate, fence, fenceLength))
            {
                cursor++;
                break;
            }

            if (body.Length > 0) body.Append('\n');
            body.Append(lines[cursor]);
            cursor++;
        }

        block.Code = body.ToString();
        block.IsOpen = !closed && !isComplete;
        output.Add(block);
        index = cursor;
        return true;
    }

    // Whether a line, trimmed as a closing fence is, could still become the closing fence: one to fewer than the fence's
    // length of its character and nothing else. The indentation it may carry is the closing fence's own, which is read
    // off the trimmed line exactly as above, so a line held back here is one the next character could close the block on.
    private static bool IsFencePrefix(string candidate, char fence, int fenceLength)
    {
        if (candidate.Length == 0 || candidate.Length >= fenceLength) return false;

        foreach (var c in candidate)
        {
            if (c != fence) return false;
        }

        return true;
    }

    private static bool TryMathBlock(
        List<string> lines,
        ref int index,
        int end,
        List<BitAiMarkdownBlock> output,
        bool isComplete)
    {
        var trimmed = lines[index].Trim();
        if (!trimmed.StartsWith("$$", StringComparison.Ordinal)) return false;

        var body = new StringBuilder();
        var closed = false;

        if (trimmed.Length >= 4 && trimmed.EndsWith("$$", StringComparison.Ordinal))
        {
            body.Append(trimmed[2..^2]);
            closed = true;
            index++;
        }
        else
        {
            body.Append(trimmed[2..]);
            var cursor = index + 1;
            while (cursor < end)
            {
                var candidate = lines[cursor].Trim();
                if (candidate.EndsWith("$$", StringComparison.Ordinal))
                {
                    if (candidate.Length > 2)
                    {
                        if (body.Length > 0) body.Append('\n');
                        body.Append(candidate[..^2]);
                    }

                    closed = true;
                    cursor++;
                    break;
                }

                if (body.Length > 0) body.Append('\n');
                body.Append(lines[cursor]);
                cursor++;
            }

            index = cursor;
        }

        output.Add(new BitAiMathBlock(body.ToString().Trim()) { IsOpen = !closed && !isComplete });
        return true;
    }

    private static bool TryQuote(
        List<string> lines,
        ref int index,
        int end,
        List<BitAiMarkdownBlock> output,
        BitAiParseContext context,
        bool isComplete,
        int depth)
    {
        if (!IsQuoteLine(lines[index])) return false;

        var inner = new List<string>();
        var cursor = index;

        while (cursor < end && (IsQuoteLine(lines[cursor]) || (inner.Count > 0 && IsLazyQuoteLine(lines, cursor, end, context))))
        {
            inner.Add(StripQuoteMarker(lines[cursor]));
            cursor++;
        }

        var quote = new BitAiQuoteBlock();
        ParseBlocks(inner, 0, inner.Count, quote.Blocks, context, isComplete || cursor < end, depth + 1);
        quote.IsOpen = !isComplete && cursor >= end;
        output.Add(quote);
        index = cursor;
        return true;
    }

    // Only paragraph text continues a quote lazily; a heading, fence, or list after it starts anew.
    private static bool IsLazyQuoteLine(List<string> lines, int index, int end, BitAiParseContext context) =>
        !IsBlank(lines[index]) && !StartsNewBlock(lines, index, end, context);

    private static bool IsQuoteLine(string line)
    {
        var trimmed = line.TrimStart();
        return trimmed.StartsWith('>');
    }

    private static string StripQuoteMarker(string line)
    {
        var trimmed = line.TrimStart();
        if (!trimmed.StartsWith('>')) return line;
        trimmed = trimmed[1..];
        return trimmed.StartsWith(' ') ? trimmed[1..] : trimmed;
    }

    private readonly record struct ListMarker(bool IsOrdered, int Start, int ContentIndent, char Bullet)
    {
        // Changing the bullet character, or an ordered list's "." for ")", starts a new list.
        public bool Continues(ListMarker first) => IsOrdered == first.IsOrdered && Bullet == first.Bullet;
    }

    private static ListMarker? TryReadListMarker(string line)
    {
        var indent = IndentOf(line);
        var trimmed = line.TrimStart();
        if (trimmed.Length == 0) return null;

        if (trimmed[0] is '-' or '*' or '+')
        {
            if (trimmed.Length < 2 || trimmed[1] is not (' ' or '\t')) return null;
            return new ListMarker(false, 1, indent + 2, trimmed[0]);
        }

        var digits = 0;
        while (digits < trimmed.Length && char.IsAsciiDigit(trimmed[digits])) digits++;
        if (digits == 0 || digits > 9 || digits >= trimmed.Length) return null;
        if (trimmed[digits] is not ('.' or ')')) return null;
        if (digits + 1 >= trimmed.Length || trimmed[digits + 1] is not (' ' or '\t')) return null;

        return new ListMarker(true, int.Parse(trimmed[..digits]), indent + digits + 2, trimmed[digits]);
    }

    private static bool TryList(
        List<string> lines,
        ref int index,
        int end,
        List<BitAiMarkdownBlock> output,
        BitAiParseContext context,
        bool isComplete,
        int depth)
    {
        if (TryReadListMarker(lines[index]) is not { } first) return false;

        var list = new BitAiListBlock { IsOrdered = first.IsOrdered, Start = first.Start };
        var cursor = index;
        var sawBlank = false;

        while (cursor < end)
        {
            if (IsBlank(lines[cursor]))
            {
                // A blank line continues the list only when another item follows. Anything else
                // the previous item refused ends the list, and the blank line is left for the block
                // after it rather than being swallowed.
                var next = NextNonBlank(lines, cursor, end);
                if (next < end && TryReadListMarker(lines[next]) is { } following && following.Continues(first))
                {
                    sawBlank = true;
                    cursor = next;
                    continue;
                }

                break;
            }

            if (IsThematicBreak(lines[cursor])) break;
            if (TryReadListMarker(lines[cursor]) is not { } marker || !marker.Continues(first)) break;

            var itemStart = cursor;
            cursor++;
            while (cursor < end)
            {
                if (IsBlank(lines[cursor]))
                {
                    var next = NextNonBlank(lines, cursor, end);
                    if (next < end && IndentOf(lines[next]) >= marker.ContentIndent)
                    {
                        cursor = next;
                        continue;
                    }

                    break;
                }

                if (IndentOf(lines[cursor]) >= marker.ContentIndent)
                {
                    cursor++;
                    continue;
                }

                // A line short of the content indent continues the item only as lazy paragraph
                // text. One that starts any block, a rule or a table or display math included,
                // ends the item instead of being pulled into it.
                if (TryReadListMarker(lines[cursor]) is not null) break;
                if (StartsNewBlock(lines, cursor, end, context)) break;
                cursor++;
            }

            // Only the item the source ends inside can still grow.
            list.Items.Add(BuildItem(lines, itemStart, cursor, marker, context, isComplete || cursor < end, depth));
        }

        list.IsLoose = sawBlank;
        list.IsOpen = !isComplete && cursor >= end;
        output.Add(list);
        index = Math.Max(cursor, index + 1);
        return true;
    }

    private static BitAiListItemBlock BuildItem(
        List<string> lines,
        int start,
        int end,
        ListMarker marker,
        BitAiParseContext context,
        bool isComplete,
        int depth)
    {
        var item = new BitAiListItemBlock();
        var body = new List<string>(end - start);

        for (var i = start; i < end; i++)
        {
            var line = lines[i];
            if (i == start)
            {
                // The marker's content starts after the first space or tab, so "-\titem" keeps "item".
                var trimmed = line.TrimStart();
                var offset = trimmed.IndexOfAny([' ', '\t']);
                line = offset < 0 ? string.Empty : trimmed[(offset + 1)..];

                if (context.Options.TaskLists && line.Length >= 3 && line[0] == '[' && line[2] == ']')
                {
                    var state = char.ToLowerInvariant(line[1]);
                    if (state is ' ' or 'x')
                    {
                        item.IsChecked = state == 'x';
                        line = line[3..].TrimStart();
                    }
                }
            }
            else
            {
                line = StripIndent(line, marker.ContentIndent);
            }

            body.Add(line);
        }

        ParseBlocks(body, 0, body.Count, item.Blocks, context, isComplete, depth + 1);
        return item;
    }

    // Removes up to the given number of columns of indentation, counting a tab as IndentOf does, so
    // a tab is never cut in half and the characters after it are never eaten.
    private static string StripIndent(string line, int columns)
    {
        var column = 0;
        var i = 0;

        while (i < line.Length && column < columns)
        {
            if (line[i] == ' ') column++;
            else if (line[i] == '\t')
            {
                if (column + 4 > columns) return new string(' ', column + 4 - columns) + line[(i + 1)..];
                column += 4;
            }
            else break;

            i++;
        }

        return line[i..];
    }

    private static bool TryTable(
        List<string> lines,
        ref int index,
        int end,
        List<BitAiMarkdownBlock> output,
        BitAiParseContext context,
        bool isComplete)
    {
        if (index + 1 >= end) return false;
        if (!lines[index].Contains('|')) return false;
        if (ParseAlignments(lines[index + 1]) is not { } alignments) return false;

        var header = SplitRow(lines[index]);
        if (header.Count == 0) return false;

        var options = context.Options;
        var table = new BitAiTableBlock();
        table.Alignments.AddRange(alignments);
        foreach (var cell in header) table.Header.Add(BitAiInlineParser.Parse(cell, options, isComplete: true, context));

        while (table.Alignments.Count < table.Header.Count) table.Alignments.Add(BitAiColumnAlignment.None);

        var cursor = index + 2;
        while (cursor < end && !IsBlank(lines[cursor]) && lines[cursor].Contains('|'))
        {
            var cells = SplitRow(lines[cursor]);
            var row = new List<List<BitAiMarkdownInline>>(cells.Count);
            var lastLine = cursor == end - 1;
            foreach (var cell in cells) row.Add(BitAiInlineParser.Parse(cell, options, isComplete || !lastLine, context));
            table.Rows.Add(row);
            cursor++;
        }

        table.IsOpen = !isComplete && cursor >= end;
        output.Add(table);
        index = cursor;
        return true;
    }

    private static List<BitAiColumnAlignment>? ParseAlignments(string line)
    {
        if (!line.Contains('-') || !line.Contains('|')) return null;

        var alignments = new List<BitAiColumnAlignment>();
        foreach (var raw in SplitRow(line))
        {
            var cell = raw.Trim();
            if (cell.Length == 0) return null;

            var left = cell[0] == ':';
            var right = cell[^1] == ':';
            var body = cell.Trim(':');
            if (body.Length == 0 || body.Any(c => c != '-')) return null;

            alignments.Add((left, right) switch
            {
                (true, true) => BitAiColumnAlignment.Center,
                (true, false) => BitAiColumnAlignment.Left,
                (false, true) => BitAiColumnAlignment.Right,
                _ => BitAiColumnAlignment.None,
            });
        }

        return alignments.Count > 0 ? alignments : null;
    }

    private static List<string> SplitRow(string line)
    {
        var trimmed = line.Trim();
        if (trimmed.StartsWith('|')) trimmed = trimmed[1..];
        if (trimmed.EndsWith('|') && !trimmed.EndsWith("\\|", StringComparison.Ordinal)) trimmed = trimmed[..^1];

        var cells = new List<string>();
        var builder = new StringBuilder();

        for (var i = 0; i < trimmed.Length; i++)
        {
            var c = trimmed[i];
            if (c == '\\' && i + 1 < trimmed.Length && trimmed[i + 1] == '|')
            {
                builder.Append('|');
                i++;
                continue;
            }

            if (c == '|')
            {
                cells.Add(builder.ToString().Trim());
                builder.Clear();
                continue;
            }

            builder.Append(c);
        }

        cells.Add(builder.ToString().Trim());
        return cells;
    }

    private static bool TryHtmlBlock(List<string> lines, ref int index, int end, List<BitAiMarkdownBlock> output)
    {
        if (!IsHtmlBlockStart(lines[index].TrimStart(), interruptingParagraph: false)) return false;

        var body = new StringBuilder();
        var cursor = index;
        while (cursor < end && !IsBlank(lines[cursor]))
        {
            if (body.Length > 0) body.Append('\n');
            body.Append(lines[cursor]);
            cursor++;
        }

        output.Add(new BitAiHtmlBlock(body.ToString()));
        index = cursor;
        return true;
    }

    // Tags that may start an HTML block in the middle of a paragraph, from CommonMark's list of
    // block-level names. A span or a bold tag after a line of text continues that text instead.
    private static readonly HashSet<string> BlockLevelTags = new(StringComparer.OrdinalIgnoreCase)
    {
        "address", "article", "aside", "blockquote", "body", "center", "details", "dialog", "dir", "div",
        "dl", "dd", "dt", "fieldset", "figcaption", "figure", "footer", "form", "frame", "frameset",
        "h1", "h2", "h3", "h4", "h5", "h6", "head", "header", "hr", "html", "iframe", "legend", "li",
        "main", "menu", "nav", "ol", "p", "pre", "script", "section", "style", "summary", "table",
        "tbody", "td", "tfoot", "th", "thead", "title", "tr", "ul",
    };

    // A line opens an HTML block when it starts with something that is actually a tag: a comment,
    // a declaration, or a name made of letters. A bare "<" or "<3" is text.
    private static bool IsHtmlBlockStart(string trimmed, bool interruptingParagraph)
    {
        if (trimmed.Length < 3 || trimmed[0] != '<') return false;
        if (trimmed.StartsWith("<!--", StringComparison.Ordinal)) return true;
        if (trimmed[1] == '!' && !interruptingParagraph) return char.IsAsciiLetter(trimmed[2]) || trimmed[2] == '[';

        var i = 1;
        if (trimmed[i] == '/') i++;
        var nameStart = i;
        if (i >= trimmed.Length || !char.IsAsciiLetter(trimmed[i])) return false;
        while (i < trimmed.Length && (char.IsAsciiLetterOrDigit(trimmed[i]) || trimmed[i] == '-')) i++;
        if (i < trimmed.Length && trimmed[i] is not (' ' or '\t' or '>' or '/')) return false;

        return !interruptingParagraph || BlockLevelTags.Contains(trimmed[nameStart..i]);
    }

    /// <summary>
    /// Reads a link reference definition (<c>[label]: url "title"</c>) or a footnote definition
    /// (<c>[^label]: text</c>) and records it on the context without adding a block.
    /// </summary>
    private static bool TryDefinition(List<string> lines, ref int index, int end, BitAiParseContext context)
    {
        var line = lines[index];
        if (IndentOf(line) >= 4) return false;

        var trimmed = line.TrimStart();
        if (trimmed.Length < 4 || trimmed[0] != '[') return false;

        var close = trimmed.IndexOf(']', 1);
        if (close < 0 || close > 1000 || close + 1 >= trimmed.Length || trimmed[close + 1] != ':') return false;

        var label = trimmed.AsSpan(1, close - 1);
        if (label.IsWhiteSpace() || label.Contains('[')) return false;

        if (label[0] == '^')
        {
            if (!context.Options.Footnotes || label.Length < 2 || label[1..].ContainsAny(" \t")) return false;
            ReadFootnoteDefinition(lines, ref index, end, context, label[1..].ToString(), trimmed[(close + 2)..]);
            return true;
        }

        if (!TryReadLinkDefinitionTail(trimmed.AsSpan(close + 2), out var url, out var title)) return false;

        (context.LinkDefinitions ??= new(StringComparer.Ordinal)).TryAdd(BitAiParseContext.NormalizeLabel(label), new BitAiLinkReferenceDefinition(url, title));
        index++;
        return true;
    }

    // The part after "[label]:": whitespace, a destination (angle-bracketed or a run of non-space
    // characters), and an optional quoted title, all on the one line.
    private static bool TryReadLinkDefinitionTail(ReadOnlySpan<char> tail, out string url, out string? title)
    {
        url = string.Empty;
        title = null;

        tail = tail.Trim();
        if (tail.Length == 0) return false;

        int destinationEnd;
        if (tail[0] == '<')
        {
            destinationEnd = tail.IndexOf('>');
            if (destinationEnd < 0 || tail[1..destinationEnd].Contains('<')) return false;
            url = tail[1..destinationEnd].ToString();
            destinationEnd++;
        }
        else
        {
            destinationEnd = 0;
            while (destinationEnd < tail.Length && !char.IsWhiteSpace(tail[destinationEnd])) destinationEnd++;
            url = tail[..destinationEnd].ToString();
        }

        var rest = tail[destinationEnd..];
        if (rest.Length == 0) return true;
        if (!char.IsWhiteSpace(rest[0])) return false;

        rest = rest.Trim();
        if (rest.Length < 2) return false;

        var closer = rest[0] switch { '"' => '"', '\'' => '\'', '(' => ')', _ => '\0' };
        if (closer == '\0' || rest[^1] != closer) return false;

        title = rest[1..^1].ToString();
        return true;
    }

    // A footnote's text is the rest of its line and every following line indented four columns,
    // plus lazy paragraph continuation lines, as GitHub reads them.
    private static void ReadFootnoteDefinition(List<string> lines, ref int index, int end, BitAiParseContext context, string label, string firstLine)
    {
        var body = new List<string> { firstLine.Trim() };
        var cursor = index + 1;

        while (cursor < end)
        {
            var line = lines[cursor];
            if (IsBlank(line))
            {
                var next = NextNonBlank(lines, cursor, end);
                if (next >= end || IndentOf(lines[next]) < 4) break;
                for (; cursor < next; cursor++) body.Add(string.Empty);
                continue;
            }

            if (IndentOf(line) >= 4) body.Add(StripIndent(line, 4));
            else if (!IsBlank(body[^1]) && !StartsNewBlock(lines, cursor, end, context) && !LooksLikeDefinition(line)) body.Add(line);
            else break;

            cursor++;
        }

        (context.FootnoteDefinitions ??= new(StringComparer.Ordinal)).TryAdd(BitAiParseContext.NormalizeLabel(label), body);
        index = cursor;
    }

    private static bool LooksLikeDefinition(string line)
    {
        var trimmed = line.TrimStart();
        if (trimmed.Length < 4 || trimmed[0] != '[') return false;
        var close = trimmed.IndexOf(']', 1);
        return close > 1 && close + 1 < trimmed.Length && trimmed[close + 1] == ':';
    }

    /// <summary>
    /// Numbers footnote references in the order they are first cited, lists the footnotes they
    /// cite on the document, and parses each footnote's text.
    /// </summary>
    private static void ResolveFootnotes(BitAiMarkdownDocument document, BitAiParseContext context)
    {
        if (context.FootnoteDefinitions is null) return;

        var byLabel = new Dictionary<string, BitAiFootnoteBlock>(StringComparer.Ordinal);
        VisitBlocks(document.Blocks);

        // A footnote may cite another, so the list can grow while it is being walked.
        for (var i = 0; i < document.Footnotes.Count; i++)
        {
            var footnote = document.Footnotes[i];
            var body = context.FootnoteDefinitions[BitAiParseContext.NormalizeLabel(footnote.Label)];
            ParseBlocks(body, 0, body.Count, footnote.Blocks, context, isComplete: true, depth: 1);
            VisitBlocks(footnote.Blocks);
        }

        void VisitBlocks(List<BitAiMarkdownBlock> blocks)
        {
            foreach (var block in blocks)
            {
                switch (block)
                {
                    case BitAiParagraphBlock p: VisitInlines(p.Inlines); break;
                    case BitAiHeadingBlock h: VisitInlines(h.Inlines); break;
                    case BitAiQuoteBlock q: VisitBlocks(q.Blocks); break;
                    case BitAiListBlock l: foreach (var item in l.Items) VisitBlocks(item.Blocks); break;
                    case BitAiTableBlock t:
                        foreach (var cell in t.Header) VisitInlines(cell);
                        foreach (var row in t.Rows) foreach (var cell in row) VisitInlines(cell);
                        break;
                }
            }
        }

        void VisitInlines(List<BitAiMarkdownInline> inlines)
        {
            foreach (var inline in inlines)
            {
                switch (inline)
                {
                    case BitAiFootnoteReferenceInline reference:
                        var key = BitAiParseContext.NormalizeLabel(reference.Label);
                        if (!byLabel.TryGetValue(key, out var footnote))
                        {
                            footnote = new BitAiFootnoteBlock(reference.Label, document.Footnotes.Count + 1);
                            byLabel[key] = footnote;
                            document.Footnotes.Add(footnote);
                        }

                        reference.Number = footnote.Number;
                        reference.Ordinal = ++footnote.ReferenceCount;
                        break;
                    case BitAiEmphasisInline e: VisitInlines(e.Inlines); break;
                    case BitAiStrikethroughInline s: VisitInlines(s.Inlines); break;
                    case BitAiLinkInline l: VisitInlines(l.Inlines); break;
                }
            }
        }
    }

    private static void ParseParagraph(
        List<string> lines,
        ref int index,
        int end,
        List<BitAiMarkdownBlock> output,
        BitAiParseContext context,
        bool isComplete)
    {
        var options = context.Options;
        var builder = new StringBuilder();
        var cursor = index;
        var level = 0;

        while (cursor < end)
        {
            var line = lines[cursor];
            if (IsBlank(line)) break;

            if (cursor > index)
            {
                var setext = line.Trim();
                if (setext.Length > 0 && setext.All(c => c == '=')) { level = 1; cursor++; break; }
                if (setext.Length > 0 && setext.All(c => c == '-')) { level = 2; cursor++; break; }
                if (StartsNewBlock(lines, cursor, end, context)) break;
            }

            // A line's leading indentation is not content, as CommonMark reads paragraphs; its
            // trailing spaces are, since two of them make a hard break.
            if (builder.Length > 0) builder.Append('\n');
            builder.Append(line.AsSpan().TrimStart(" \t"));
            cursor++;
        }

        var text = builder.ToString();
        index = Math.Max(cursor, index + 1);
        if (text.Trim().Length == 0) return;

        var open = !isComplete && cursor >= end;

        if (level > 0)
        {
            output.Add(CreateHeading(level, text, context));
            return;
        }

        var paragraph = new BitAiParagraphBlock { IsOpen = open };
        paragraph.Inlines.AddRange(BitAiInlineParser.Parse(text, options, !open || !options.HideDanglingDelimiters, context));
        output.Add(paragraph);
    }

    private static bool StartsNewBlock(List<string> lines, int index, int end, BitAiParseContext context)
    {
        var options = context.Options;
        var line = lines[index];
        var trimmed = line.TrimStart();
        if (trimmed.Length == 0) return true;
        if (trimmed[0] == '#' && TryHeadingLevel(trimmed) > 0) return true;
        if (trimmed.StartsWith("```", StringComparison.Ordinal) || trimmed.StartsWith("~~~", StringComparison.Ordinal)) return true;
        if (trimmed.StartsWith('>')) return true;
        if (TryReadListMarker(line) is not null) return true;
        if (IsThematicBreak(line)) return true;
        if (options.Math && IsDisplayMathLine(trimmed)) return true;
        if (options.Tables && index + 1 < end && line.Contains('|') && ParseAlignments(lines[index + 1]) is not null) return true;
        if (options.AllowRawHtml && IsHtmlBlockStart(trimmed, interruptingParagraph: true)) return true;
        return false;
    }

    // A line that is only "$$", or a whole "$$...$$" expression, is display math even straight after
    // text. Anything looser would turn a paragraph line that merely starts with "$$" into a math block.
    private static bool IsDisplayMathLine(string trimmed)
    {
        var line = trimmed.TrimEnd();
        return line == "$$"
            || (line.Length >= 4 && line.StartsWith("$$", StringComparison.Ordinal) && line.EndsWith("$$", StringComparison.Ordinal));
    }

    private static int TryHeadingLevel(string trimmed)
    {
        var level = 0;
        while (level < trimmed.Length && trimmed[level] == '#') level++;
        if (level is 0 or > 6) return 0;
        return level == trimmed.Length || trimmed[level] is ' ' or '\t' ? level : 0;
    }
}

/// <summary>A link reference definition's destination and title.</summary>
internal readonly record struct BitAiLinkReferenceDefinition(string Url, string? Title);

/// <summary>
/// What one parse of a document shares between its blocks: the options, the heading identifiers
/// already handed out, and the definitions that references resolve against.
/// </summary>
internal sealed class BitAiParseContext(BitAiMarkdownOptions options)
{
    /// <summary>Which extensions to enable.</summary>
    public BitAiMarkdownOptions Options { get; } = options;

    /// <summary>Every heading identifier assigned so far, so no two headings share one.</summary>
    public HashSet<string> HeadingIds { get; } = new(StringComparer.Ordinal);

    /// <summary>Link reference definitions by normalised label, or <see langword="null"/> when there are none.</summary>
    public Dictionary<string, BitAiLinkReferenceDefinition>? LinkDefinitions { get; set; }

    /// <summary>Footnote definitions by normalised label, as the lines of their text.</summary>
    public Dictionary<string, List<string>>? FootnoteDefinitions { get; set; }

    /// <summary>
    /// Whether the definitions are complete, so a reference can be resolved rather than left as
    /// text. False on the first pass over a document, which only collects them.
    /// </summary>
    public bool ResolvesReferences { get; init; }

    /// <summary>Whether any definition was found.</summary>
    public bool SawDefinitions => LinkDefinitions is not null || FootnoteDefinitions is not null;

    /// <summary>Finds the link a reference label points at.</summary>
    public bool TryGetLink(ReadOnlySpan<char> label, out BitAiLinkReferenceDefinition definition)
    {
        definition = default;
        return ResolvesReferences && LinkDefinitions is not null && LinkDefinitions.TryGetValue(NormalizeLabel(label), out definition);
    }

    /// <summary>Whether a footnote with this label is defined.</summary>
    public bool HasFootnote(ReadOnlySpan<char> label) =>
        ResolvesReferences && FootnoteDefinitions is not null && FootnoteDefinitions.ContainsKey(NormalizeLabel(label));

    /// <summary>
    /// Gives a heading an identifier derived from its text: prefixed so it cannot collide with
    /// the host page's own ids, and suffixed when the same text appears twice.
    /// </summary>
    public string ClaimHeadingId(string text)
    {
        var slug = BitAiMarkdownParser.Slug(text);
        var id = "bit-ai-h-" + (slug.Length == 0 ? "section" : slug);
        if (HeadingIds.Add(id)) return id;

        for (var n = 2; ; n++)
        {
            var candidate = $"{id}-{n}";
            if (HeadingIds.Add(candidate)) return candidate;
        }
    }

    /// <summary>Case-folds a label and collapses its whitespace, as CommonMark matches labels.</summary>
    public static string NormalizeLabel(ReadOnlySpan<char> label)
    {
        var builder = new StringBuilder(label.Length);
        var pendingSpace = false;

        foreach (var c in label.Trim())
        {
            if (char.IsWhiteSpace(c))
            {
                pendingSpace = true;
                continue;
            }

            if (pendingSpace && builder.Length > 0) builder.Append(' ');
            pendingSpace = false;
            builder.Append(char.ToUpperInvariant(c));
        }

        return builder.ToString();
    }
}
