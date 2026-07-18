using System.Text;

namespace Bit.BlazorUI;

/// <summary>Shared utilities used by the core inline parsers.</summary>
public static class BitMarkdownInlineHelpers
{
    public static int CountRun(string s, int start, char c)
    {
        int j = start;
        while (j < s.Length && s[j] == c) j++;
        return j - start;
    }

    public static bool IsAsciiPunctuation(char c) =>
        (c >= '!' && c <= '/') || (c >= ':' && c <= '@') ||
        (c >= '[' && c <= '`') || (c >= '{' && c <= '~');

    public static bool IsPunctuation(char c) =>
        IsAsciiPunctuation(c) || char.IsPunctuation(c) || char.IsSymbol(c);

    /// <summary>Produces the plain-text form of inline nodes (used for image alt text).</summary>
    public static string PlainText(IEnumerable<BitMarkdownNode> nodes)
    {
        var sb = new StringBuilder();
        Append(nodes, sb);
        return sb.ToString();

        static void Append(IEnumerable<BitMarkdownNode> ns, StringBuilder sb)
        {
            foreach (var node in ns)
            {
                switch (node)
                {
                    case BitMarkdownTextNode t: sb.Append(t.Text); break;
                    case BitMarkdownCodeSpanNode c: sb.Append(c.Content); break;
                    case BitMarkdownImageNode im: sb.Append(im.Alt); break;
                    case BitMarkdownLineBreakNode: sb.Append(' '); break;
                    default:
                        if (node.ChildNodes is { } children) Append(children, sb);
                        break;
                }
            }
        }
    }
}
