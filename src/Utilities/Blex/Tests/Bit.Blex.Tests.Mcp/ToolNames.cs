using System.Text.RegularExpressions;

namespace Bit.Blex.Tests.Mcp;

/// <summary>
/// Finds the tool names inside a piece of the server's own prose - the instructions, a prompt - so a
/// test can hold that prose to the tools that actually exist.
/// </summary>
internal static partial class ToolNames
{
    public static IEnumerable<string> MentionedIn(string text)
    {
        return ToolNameRegex().Matches(text).Select(match => match.Groups["name"].Value).Distinct(StringComparer.Ordinal);
    }

    // Every tool on this server is named after what it does to Blex, which is what makes them findable in
    // prose without also matching the type names (BlexStoreBase, BlexOptions) the same prose is full of.
    [GeneratedRegex(@"\b(?<name>(Get|Search|Inspect)Blex[A-Za-z]*)\b")]
    private static partial Regex ToolNameRegex();
}
