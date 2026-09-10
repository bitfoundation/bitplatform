namespace Bit.BlazorUI;

/// <summary>
/// Parses the metadata block a document may open with: <c>---</c> (YAML) or <c>+++</c> (TOML) on
/// the very first line, the metadata, then a matching closing fence.
/// </summary>
public sealed class BitMarkdownFrontMatterBlockParser : BitMarkdownBlockParser
{
    // Ahead of every other parser: the opening fence is also a valid thematic break, and the
    // metadata under it is also a valid setext heading.
    public override int Order => -100;

    public override bool TryParse(BitMarkdownBlockProcessor state, List<BitMarkdownNode> output)
    {
        // Front matter is only front matter at the very top of the document, never inside a
        // list item, a block quote, or after any other content.
        if (state.Line != 0 || state.IsTopLevel is false) return false;

        var lines = state.Lines;
        string fence = ReadFence(lines[0]);
        if (fence.Length == 0) return false;

        int end = -1;
        for (int i = 1; i < lines.Count; i++)
        {
            if (IsClosingFence(lines[i], fence[0])) { end = i; break; }
        }

        // An unterminated block is not front matter; leaving it unmatched keeps the document
        // rendering exactly as it did before the extension was enabled.
        if (end < 0) return false;

        output.Add(new BitMarkdownFrontMatterNode
        {
            Fence = fence,
            Text = string.Join("\n", lines.Take(end).Skip(1))
        });
        state.Line = end + 1;
        return true;
    }

    // The opening fence must be a run of at least three '-' or '+' alone on the line.
    private static string ReadFence(string line)
    {
        string trimmed = line.TrimEnd(' ', '\t');
        if (trimmed.Length < 3) return string.Empty;

        char c = trimmed[0];
        if (c is not ('-' or '+')) return string.Empty;

        foreach (char ch in trimmed)
        {
            if (ch != c) return string.Empty;
        }
        return trimmed;
    }

    // YAML also allows "..." to end the document, which is what a generator emits when the
    // metadata is followed by more YAML documents.
    private static bool IsClosingFence(string line, char fenceChar)
    {
        string trimmed = line.TrimEnd(' ', '\t');
        if (trimmed.Length < 3) return false;

        char c = trimmed[0];
        if (c != fenceChar && !(fenceChar == '-' && c == '.')) return false;

        foreach (char ch in trimmed)
        {
            if (ch != c) return false;
        }
        return true;
    }
}
