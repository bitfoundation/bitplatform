using System.Text;
using System.Text.RegularExpressions;

namespace Bit.Butil.Tests.Mcp.Infrastructure;

/// <summary>
/// A follow-up call a search hit names, e.g. <c>GetButilDocsPage(slug: "clipboard")</c>.
/// <para>
/// Every hit the search returns carries one, and the whole design rests on it being callable
/// verbatim: an agent is told to make that call next, and a hit that names a call which does not
/// resolve sends it somewhere there is nothing. Parsing them back into real calls is how the suite
/// proves the promise instead of assuming it.
/// </para>
/// </summary>
public sealed partial record ToolCallReference(string Tool, string Argument, string Value)
{
    public static ToolCallReference? Parse(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;

        var match = CallRegex().Match(text.Trim());

        return match.Success
            ? new ToolCallReference(match.Groups["tool"].Value, match.Groups["argument"].Value, match.Groups["value"].Value)
            : null;
    }

    public Dictionary<string, object?> Arguments => new(StringComparer.Ordinal) { [Argument] = Value };

    // Greedy on the value so a heading containing a quote still parses: the call always ends with
    // the same two characters.
    [GeneratedRegex("""^(?<tool>\w+)\((?<argument>\w+):\s*"(?<value>.*)"\)$""")]
    private static partial Regex CallRegex();
}
