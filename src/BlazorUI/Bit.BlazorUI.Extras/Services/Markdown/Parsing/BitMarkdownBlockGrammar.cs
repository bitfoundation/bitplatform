using System.Text.RegularExpressions;

namespace Bit.BlazorUI;

/// <summary>Shared regular expressions for the core block grammar.</summary>
internal static partial class BitMarkdownBlockGrammar
{
    [GeneratedRegex(@"^ {0,3}(?:([-*_])[ \t]*)(?:\1[ \t]*){2,}$")]
    public static partial Regex ThematicBreak();

    // The optional closing run of '#'s is stripped in the parser (only when preceded
    // by whitespace), so the content group here captures the full text after the
    // opening '#'s and is not responsible for removing trailing hashes.
    [GeneratedRegex(@"^ {0,3}(#{1,6})(?:[ \t]+(.*?))?[ \t]*$")]
    public static partial Regex AtxHeading();

    // Backtick fences may not contain backticks in their info string, but tilde
    // fences may; handle the two fence types with separate alternatives.
    [GeneratedRegex(@"^ {0,3}(?<fence>`{3,})[ \t]*(?<info>[^`\n]*)$|^ {0,3}(?<fence>~{3,})[ \t]*(?<info>[^\n]*)$")]
    public static partial Regex Fence();

    [GeneratedRegex(@"^ {0,3}(`{3,}|~{3,})[ \t]*$")]
    public static partial Regex FenceClose();

    // The whitespace + content after the marker is optional so that a marker-only
    // line (e.g. "-" or "1.") is recognised as an empty list item per CommonMark.
    [GeneratedRegex(@"^ {0,3}([-+*])(?:([ \t]+)(.*))?$")]
    public static partial Regex Bullet();

    // The digits are matched as [0-9] rather than \d on purpose: CommonMark defines an
    // ordered list marker as ASCII digits only, while .NET's \d also matches every other
    // Unicode decimal digit (Arabic-Indic "۱", Devanagari "१", fullwidth "１", ...). Those
    // must stay paragraph text, and letting them match here also fed a non-ASCII number
    // to int.Parse in the list parser, which threw a FormatException.
    [GeneratedRegex(@"^ {0,3}([0-9]{1,9})([.)])(?:([ \t]+)(.*))?$")]
    public static partial Regex Ordered();

    [GeneratedRegex(@"^ {0,3}(=+|-+)\s*$")]
    public static partial Regex Setext();
}
