using System.Text.RegularExpressions;

namespace Bit.BlazorUI;

/// <summary>
/// Helpers for editing task markers in a Markdown source, so a ticked box can be written back into
/// the document it came from.
/// </summary>
public static partial class BitMarkdownTaskList
{
    // A list marker followed by a task box. The three parts are captured separately so the box can
    // be rewritten without touching the indentation or the marker.
    [GeneratedRegex(@"^([ \t]*(?:[-+*]|[0-9]{1,9}[.)])[ \t]+\[)([ xX])(\].*)$", RegexOptions.None, matchTimeoutMilliseconds: 1000)]
    private static partial Regex TaskLine();

    // The fence that opens or closes a code block, whose contents are not Markdown and whose
    // "- [ ]" lines are therefore not tasks.
    [GeneratedRegex(@"^ {0,3}(`{3,}|~{3,})", RegexOptions.None, matchTimeoutMilliseconds: 1000)]
    private static partial Regex CodeFence();

    /// <summary>
    /// Returns <paramref name="markdown"/> with the task marker at <paramref name="index"/> - the
    /// index the viewer reports, counted from 0 in reading order - set to
    /// <paramref name="isChecked"/>. The source is returned unchanged when there is no such marker.
    /// </summary>
    /// <remarks>
    /// Markers inside a fenced code block are skipped, so a document that documents the task syntax
    /// still counts the same boxes the renderer drew. Indentation is not treated as code, because
    /// an indented line under a list item is a nested task and those are the common case; a task
    /// marker inside an <em>indented</em> code block is the one shape this counts and the renderer
    /// does not.
    /// </remarks>
    public static string Toggle(string? markdown, int index, bool isChecked)
    {
        if (string.IsNullOrEmpty(markdown) || index < 0) return markdown ?? string.Empty;

        var lines = markdown.Split('\n');
        string? fence = null;
        int found = 0;

        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i];
            string body = line.EndsWith('\r') ? line[..^1] : line;

            var fenceMatch = CodeFence().Match(body);
            if (fenceMatch.Success)
            {
                string marker = fenceMatch.Groups[1].Value;
                if (fence is null) fence = marker;
                else if (marker[0] == fence[0] && marker.Length >= fence.Length) fence = null;
                continue;
            }
            if (fence is not null) continue;

            var match = TaskLine().Match(body);
            if (match.Success is false) continue;

            if (found++ != index) continue;

            string rewritten = match.Groups[1].Value + (isChecked ? "x" : " ") + match.Groups[3].Value;
            lines[i] = line.EndsWith('\r') ? rewritten + "\r" : rewritten;
            return string.Join("\n", lines);
        }

        return markdown;
    }

    /// <summary>
    /// Counts the task markers in <paramref name="markdown"/>, using the same rules
    /// <see cref="Toggle"/> counts them with.
    /// </summary>
    public static int Count(string? markdown)
    {
        if (string.IsNullOrEmpty(markdown)) return 0;

        int count = 0;
        string? fence = null;
        foreach (var raw in markdown.Split('\n'))
        {
            string body = raw.EndsWith('\r') ? raw[..^1] : raw;

            var fenceMatch = CodeFence().Match(body);
            if (fenceMatch.Success)
            {
                string marker = fenceMatch.Groups[1].Value;
                if (fence is null) fence = marker;
                else if (marker[0] == fence[0] && marker.Length >= fence.Length) fence = null;
                continue;
            }
            if (fence is not null) continue;
            if (TaskLine().IsMatch(body)) count++;
        }

        return count;
    }
}
