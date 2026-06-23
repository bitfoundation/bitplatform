using System.Text.RegularExpressions;

namespace Bit.BlazorUI;

/// <summary>Shared regular expressions for the core block grammar.</summary>
internal static partial class BitMarkdownViewerBlockGrammar
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
