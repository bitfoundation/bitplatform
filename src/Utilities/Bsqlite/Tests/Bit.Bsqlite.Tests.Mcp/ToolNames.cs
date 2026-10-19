using System.Text.RegularExpressions;

namespace Bit.Bsqlite.Tests.Mcp;

/// <summary>
/// Finds the tool names inside a piece of the server's own prose - the instructions, a prompt, a
/// setup guide - so a test can hold that prose to the tools that actually exist.
/// </summary>
internal static partial class ToolNames
{
    public static IEnumerable<string> MentionedIn(string text)
    {
        return ToolNameRegex().Matches(text).Select(match => match.Groups["name"].Value).Distinct(StringComparer.Ordinal);
    }

    // Every tool on this server is named after what it does to Bsqlite - get, search, plan, check - which is
    // what makes them findable in prose without also matching the Bsqlite... type names the same prose is full of.
    [GeneratedRegex(@"\b(?<name>(Get|Search|Plan|Check)Bsqlite[A-Za-z]*)\b")]
    private static partial Regex ToolNameRegex();
}
