using System.Text.RegularExpressions;

namespace Bit.BlazeDb.Tests.Mcp;

/// <summary>
/// Finds the tool names inside a piece of the server's own prose - the instructions, the overview,
/// a prompt - so a test can hold that prose to the tools that actually exist.
/// </summary>
internal static partial class ToolNames
{
    public static IEnumerable<string> MentionedIn(string text)
    {
        return ToolNameRegex().Matches(text).Select(match => match.Groups["name"].Value).Distinct(StringComparer.Ordinal);
    }

    // Every tool on this server is named after what it does to BlazeDb, which is what makes them
    // findable in prose without also matching the type names the same prose is full of. A Get or an
    // Inspect tool always names its subject after "BlazeDb" - which is what keeps the EF Core
    // provider's own GetBlazeDb() extension, all over the guide, from reading as a tool.
    [GeneratedRegex(@"\b(?<name>SearchBlazeDb|(Get|Inspect)BlazeDb[A-Z][A-Za-z]*)\b")]
    private static partial Regex ToolNameRegex();
}
