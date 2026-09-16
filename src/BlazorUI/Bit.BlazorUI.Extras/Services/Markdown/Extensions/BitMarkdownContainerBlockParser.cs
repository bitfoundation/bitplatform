namespace Bit.BlazorUI;

/// <summary>
/// Parses custom containers: a fence of three or more colons followed by a name and an optional
/// title, up to a closing fence of colons carrying no info of its own.
/// </summary>
/// <remarks>
/// Containers nest: an inner fence that opens (it carries a name) is balanced against its own
/// closing fence, so <c>:::tip</c> inside <c>:::note</c> ends where it should rather than closing
/// the outer one. That is also why the name is required to open one: an info-less fence is always a
/// closer, and a stray one would otherwise claim every line after it.
/// </remarks>
public sealed class BitMarkdownContainerBlockParser : BitMarkdownBlockParser
{
    // Between the block quote (40) and the indented code block (50): a container fence sits at the
    // margin, so nothing indented can be mistaken for one.
    public override int Order => 45;

    public override bool CanInterruptParagraph(BitMarkdownBlockProcessor state, int lineIndex)
        => Opens(state.Lines[lineIndex]);

    public override bool TryParse(BitMarkdownBlockProcessor state, List<BitMarkdownNode> output)
    {
        var lines = state.Lines;
        if (ReadFence(lines[state.Line], out string name, out string? title) == 0) return false;
        // Only a fence carrying a name opens a container. An info-less ":::" is a closer, and
        // reading a stray or unmatched one as an opening fence would hand it every line that
        // follows - the rest of a block quote, or the rest of the document.
        if (name.Length == 0) return false;

        var inner = new List<string>();
        int depth = 1;
        int i = state.Line + 1;
        // A code fence's content is not Markdown, so a ":::" written inside one is sample text
        // rather than this container's closer.
        string? codeFence = null;
        for (; i < lines.Count; i++)
        {
            string line = lines[i];

            if (ReadCodeFence(line, out string codeMarker, out bool codeHasInfo))
            {
                if (codeFence is null) codeFence = codeMarker;
                else if (codeHasInfo is false && codeMarker[0] == codeFence[0]
                         && codeMarker.Length >= codeFence.Length) codeFence = null;
                inner.Add(line);
                continue;
            }

            if (codeFence is null)
            {
                int fence = ReadFence(line, out string innerName, out _);
                if (fence > 0 && innerName.Length > 0)
                {
                    // A nested opening fence: keep it in the content and balance it.
                    depth++;
                }
                else if (fence > 0 && innerName.Length == 0)
                {
                    // Any fence carrying no info closes one level, whatever its length. Balancing by
                    // nesting rather than by fence length is what lets "::::note" hold ":::tip" - the
                    // pattern a longer outer fence exists for - close in the right order.
                    depth--;
                    if (depth == 0) { i++; break; }
                }
            }

            inner.Add(line);
        }

        var container = new BitMarkdownContainerNode { Name = name, Title = title };
        // The content is every line after the opening fence, kept in order, so it maps one-to-one
        // onto the document's own lines.
        container.Children.AddRange(state.ParseBlocks(inner, state.Line + 1));
        output.Add(container);
        state.Line = i;
        return true;
    }

    /// <summary>True when <paramref name="line"/> is a fence that opens a container.</summary>
    private static bool Opens(string line)
        => ReadFence(line, out string name, out _) > 0 && name.Length > 0;

    /// <summary>
    /// Reads the <c>```</c> / <c>~~~</c> run opening or closing a fenced code block, and whether it
    /// carries an info string - which only an opening fence may.
    /// </summary>
    private static bool ReadCodeFence(string line, out string marker, out bool hasInfo)
    {
        marker = string.Empty;
        hasInfo = false;

        int indent = 0;
        while (indent < line.Length && line[indent] == ' ') indent++;
        if (indent > 3 || indent >= line.Length) return false;

        char c = line[indent];
        if (c is not ('`' or '~')) return false;

        int i = indent;
        while (i < line.Length && line[i] == c) i++;
        if (i - indent < 3) return false;

        marker = line[indent..i];
        hasInfo = line[i..].Trim().Length > 0;
        return true;
    }

    /// <summary>
    /// Returns the length of the colon fence opening <paramref name="line"/> (0 when it is not a
    /// fence), along with the name and title of its info string.
    /// </summary>
    private static int ReadFence(string line, out string name, out string? title)
    {
        name = string.Empty;
        title = null;

        int indent = 0;
        while (indent < line.Length && line[indent] == ' ') indent++;
        // Four spaces of indentation makes it an indented code block, not a fence.
        if (indent > 3) return 0;

        int colons = 0;
        int i = indent;
        while (i < line.Length && line[i] == ':') { colons++; i++; }
        if (colons < 3) return 0;

        string info = line[i..].Trim();
        if (info.Length == 0) return colons;

        // A fence's info is "name [title]"; the name is what the class is built from, so it is
        // slugified rather than taken as written.
        int space = info.IndexOfAny([' ', '\t']);
        string rawName = space < 0 ? info : info[..space];
        name = Slugify(rawName);
        if (name.Length == 0) return 0;

        if (space >= 0)
        {
            string rest = info[(space + 1)..].Trim();
            if (rest.Length > 0) title = rest;
        }

        return colons;
    }

    private static string Slugify(string text)
    {
        var sb = new System.Text.StringBuilder(text.Length);
        foreach (char c in text.ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(c)) sb.Append(c);
            else if (c is '-' or '_' && sb.Length > 0) sb.Append('-');
        }
        return sb.ToString().TrimEnd('-');
    }
}
