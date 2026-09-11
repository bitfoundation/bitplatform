using System.Text;

namespace Bit.BlazorUI;

/// <summary>
/// Shared helpers for the pieces of the grammar that deal with link labels: inline links
/// and images, reference links, link reference definitions and footnotes.
/// </summary>
public static class BitMarkdownLinkHelpers
{
    /// <summary>The CommonMark ceiling on the length of a link label.</summary>
    public const int MaxLabelLength = 999;

    /// <summary>
    /// True when <paramref name="url"/> is a destination that leaves the page's own origin, which
    /// is what decides the <c>target</c> and <c>rel</c> a rendered link carries.
    /// </summary>
    /// <remarks>
    /// A protocol-relative destination (<c>//host/path</c>) counts: the sanitizer allows it, and a
    /// browser resolves it to another host exactly as an <c>https://</c> one would. Reading it as
    /// internal is what would leave a <c>[spam](//evil.com)</c> without the <c>nofollow ugc</c> a
    /// user-generated-content policy exists to put on it.
    /// </remarks>
    public static bool IsExternalUrl(string? url)
        => string.IsNullOrEmpty(url) is false
           && (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
               || url.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
               || url.StartsWith("//", StringComparison.Ordinal));

    /// <summary>
    /// Returns the index of the <c>]</c> that closes the label opened at
    /// <paramref name="openBracket"/>, or -1 when there is none. Backslash escapes are
    /// skipped, nested brackets are balanced, and code spans are stepped over so a
    /// <c>]</c> inside backticks does not end the label early.
    /// </summary>
    public static int FindLabelEnd(string s, int openBracket)
    {
        int depth = 0;
        int i = openBracket;
        while (i < s.Length)
        {
            char c = s[i];
            if (c == '\\') { i += 2; continue; }
            if (c == '`')
            {
                i = SkipCodeSpan(s, i);
                continue;
            }
            if (c == '[') depth++;
            else if (c == ']')
            {
                depth--;
                if (depth == 0) return i;
            }
            i++;
        }
        return -1;
    }

    /// <summary>
    /// Given the index of a backtick, returns the index just past a matching closing
    /// code-span run. If no closing run of equal length exists, the backticks are literal
    /// text and the index just past the opening run is returned.
    /// </summary>
    public static int SkipCodeSpan(string s, int i)
    {
        int start = i;
        int openLen = 0;
        while (i < s.Length && s[i] == '`') { i++; openLen++; }

        int j = i;
        while (j < s.Length)
        {
            if (s[j] == '`')
            {
                int closeLen = 0;
                while (j < s.Length && s[j] == '`') { j++; closeLen++; }
                if (closeLen == openLen) return j;
            }
            else j++;
        }
        return start + openLen;
    }

    /// <summary>
    /// Normalizes a link label the way CommonMark matches them: the surrounding whitespace
    /// is trimmed, internal whitespace runs collapse to a single space, and the result is
    /// case-folded. <c>[Foo   Bar]</c> and <c>[foo bar]</c> therefore name the same definition.
    /// </summary>
    public static string NormalizeLabel(string label)
    {
        var sb = new StringBuilder(label.Length);
        bool pendingSpace = false;
        foreach (char c in label)
        {
            if (char.IsWhiteSpace(c))
            {
                pendingSpace = sb.Length > 0;
                continue;
            }
            if (pendingSpace)
            {
                sb.Append(' ');
                pendingSpace = false;
            }
            sb.Append(c);
        }
        return sb.ToString().ToLowerInvariant();
    }
}
