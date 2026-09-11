namespace Bit.BlazorUI;

/// <summary>
/// Helpers for editing task markers in a Markdown source, so a ticked box can be written back into
/// the document it came from.
/// </summary>
/// <remarks>
/// The boxes are found by parsing, never by scanning the source for something that looks like a
/// marker. A second scanner would have to agree with the parser about every rule that decides
/// whether a <c>- [ ]</c> is a task - what a code fence is, how far a list item's content is
/// indented, whether a block quote's contents count - and wherever the two disagreed, the box a
/// reader ticked and the marker that got rewritten would be different ones. Parsing instead means
/// the boxes counted here are exactly the boxes drawn, by construction.
/// </remarks>
public static class BitMarkdownTaskList
{
    // The flavors that decide which "- [ ]" markers exist. Task lists need their own extension,
    // and a marker may sit anywhere the block parsers can reach one, so the whole advanced set is
    // used rather than a lone UseTaskLists() that would miss a task inside a container.
    private static readonly Lazy<BitMarkdownPipeline> _pipeline
        = new(() => new BitMarkdownPipelineBuilder().UseAdvanced().Build());

    /// <summary>
    /// Returns <paramref name="markdown"/> with the task marker at <paramref name="index"/> - the
    /// index the viewer reports, counted from 0 in reading order - set to
    /// <paramref name="isChecked"/>. The source is returned unchanged when there is no such marker.
    /// </summary>
    /// <remarks>
    /// The source is parsed to find the marker, with the flavors <see cref="BitMarkdownPipelines"/>
    /// calls advanced. A document rendered through a pipeline of your own should be toggled through
    /// the overload taking it, since it is the pipeline that decides which markers are tasks at all.
    /// </remarks>
    public static string Toggle(string? markdown, int index, bool isChecked)
        => Toggle(markdown, index, isChecked, null);

    /// <summary>
    /// Returns <paramref name="markdown"/> with the task marker at <paramref name="index"/> set to
    /// <paramref name="isChecked"/>, finding it with <paramref name="pipeline"/> - the one the
    /// document is rendered with, so the markers counted here are the boxes the reader sees.
    /// </summary>
    public static string Toggle(string? markdown, int index, bool isChecked, BitMarkdownPipeline? pipeline)
    {
        if (string.IsNullOrEmpty(markdown) || index < 0) return markdown ?? string.Empty;

        foreach (var checkbox in Checkboxes(markdown, pipeline))
        {
            if (checkbox.Index == index) return Toggle(markdown, checkbox, isChecked);
        }

        return markdown;
    }

    /// <summary>
    /// Returns <paramref name="markdown"/> with the marker <paramref name="checkbox"/> was parsed
    /// from set to <paramref name="isChecked"/>. This is the exact overload: the box carries the
    /// line it came from, so nothing has to be searched for and nothing can be mismatched. The
    /// source is returned unchanged when the box was not parsed from it.
    /// </summary>
    public static string Toggle(string? markdown, BitMarkdownTaskCheckboxNode checkbox, bool isChecked)
    {
        ArgumentNullException.ThrowIfNull(checkbox);

        if (string.IsNullOrEmpty(markdown) || checkbox.SourceLine < 0) return markdown ?? string.Empty;
        if (TryGetLine(markdown, checkbox.SourceLine, out int start, out int end) is false) return markdown;

        // The marker opens the item's content, and only indentation and the list marker itself
        // come before it, so the first box on the line is the one to rewrite.
        int box = IndexOfBox(markdown, start, end);
        if (box < 0) return markdown;

        return string.Concat(markdown.AsSpan(0, box), isChecked ? "x" : " ", markdown.AsSpan(box + 1));
    }

    /// <summary>
    /// Counts the task markers in <paramref name="markdown"/> - the boxes a viewer showing it draws.
    /// </summary>
    public static int Count(string? markdown) => Count(markdown, null);

    /// <summary>
    /// Counts the task markers <paramref name="pipeline"/> finds in <paramref name="markdown"/> -
    /// the boxes a viewer rendering it through that pipeline draws.
    /// </summary>
    public static int Count(string? markdown, BitMarkdownPipeline? pipeline)
    {
        if (string.IsNullOrEmpty(markdown)) return 0;

        int count = 0;
        foreach (var _ in Checkboxes(markdown, pipeline)) count++;
        return count;
    }

    private static IEnumerable<BitMarkdownTaskCheckboxNode> Checkboxes(string markdown, BitMarkdownPipeline? pipeline)
        => BitMarkdownAstHelper
            .Descendants((pipeline ?? _pipeline.Value).Parse(markdown))
            .OfType<BitMarkdownTaskCheckboxNode>();

    // The index of the "[" ... "]" box's state character within the given line, or -1 when the
    // line holds none.
    private static int IndexOfBox(string markdown, int start, int end)
    {
        for (int i = start; i + 2 < end; i++)
        {
            if (markdown[i] != '[') continue;
            if (markdown[i + 1] is not (' ' or 'x' or 'X')) continue;
            if (markdown[i + 2] != ']') continue;
            return i + 1;
        }
        return -1;
    }

    // The bounds of the given line's content within the source, excluding its terminator. The line
    // boundaries are the parser's own - "\n", "\r" and "\r\n" - so the index a node reports means
    // the same line here as it did there. Working in the original string, rather than splitting and
    // re-joining it, is also what keeps every line ending exactly as the author wrote it.
    private static bool TryGetLine(string markdown, int line, out int start, out int end)
    {
        start = 0;
        int current = 0;

        for (int i = 0; i < markdown.Length; i++)
        {
            char ch = markdown[i];
            if (ch is not ('\n' or '\r')) continue;

            if (current == line)
            {
                end = i;
                return true;
            }

            current++;
            // "\r\n" is a single line boundary.
            if (ch == '\r' && i + 1 < markdown.Length && markdown[i + 1] == '\n') i++;
            start = i + 1;
        }

        end = markdown.Length;
        return current == line;
    }
}
