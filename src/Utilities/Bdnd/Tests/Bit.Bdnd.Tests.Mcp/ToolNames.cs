using System.Reflection;
using System.Text.RegularExpressions;

namespace Bit.Bdnd.Tests.Mcp;

/// <summary>
/// Finds the tool names inside a piece of the server's own prose - the instructions, the overview,
/// a prompt - so a test can hold that prose to the tools that actually exist.
/// </summary>
internal static partial class ToolNames
{
    /// <summary>
    /// The names of the library's own public members. Its API shares the tools' naming - <c>GetBdndInterop</c> - so
    /// what the prose says about the library is told apart by name, however it is written: <c>Services.GetBdndInterop()</c>
    /// or <c>GetBdndInterop()</c> alone. A tool may not take one of these names, or the prose could name it unchecked.
    /// </summary>
    public static readonly IReadOnlySet<string> LibraryMembers = typeof(BitBdnd).Assembly.GetExportedTypes()
        .SelectMany(type => type.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
        .Select(member => member.Name)
        .ToHashSet(StringComparer.Ordinal);

    public static IEnumerable<string> MentionedIn(string text)
    {
        return ToolNameRegex().Matches(text)
                              .Select(match => match.Groups["name"].Value)
                              .Where(name => LibraryMembers.Contains(name) is false)
                              .Distinct(StringComparer.Ordinal);
    }

    // Every tool on this server is named after what it does to Bdnd, which is what makes them
    // findable in prose without also matching the type names the same prose is full of.
    [GeneratedRegex(@"\b(?<name>(Get|Search|Inspect)Bdnd[A-Za-z]*)\b")]
    private static partial Regex ToolNameRegex();
}
