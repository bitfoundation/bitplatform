using System.Text.RegularExpressions;

namespace Bit.Butil.Tests.Mcp.Infrastructure;

/// <summary>
/// The Butil MCP server's public inventory, written down.
/// <para>
/// The tool names, the resource names and the prompt names are the identifiers a client stores: a
/// configured agent, a pinned resource in someone's editor, a prompt in a menu. Renaming one is a
/// breaking change for every client that already holds it, and nothing in a reflection-driven
/// server would notice. Pinning them here is the point - this list failing is exactly the signal
/// it exists to give.
/// </para>
/// </summary>
public static class ButilMcp
{
    /// <summary>
    /// Every tool the server is expected to advertise, and the arguments each one takes.
    /// <para>
    /// Seven, and the number is asserted as much as the names are. Each listing this server used to
    /// advertise as a tool of its own is now what its retrieval tool answers when called with no
    /// argument, and the single-API inspection is what PlanButilFeature answers when passed one
    /// name - so a fifteenth entry appearing here is the signal that the surface has started
    /// growing back, which is the whole reason this list is written down by hand.
    /// </para>
    /// </summary>
    public static readonly IReadOnlyDictionary<string, string[]> Tools = new Dictionary<string, string[]>(StringComparer.Ordinal)
    {
        ["SearchButil"] = ["query", "limit"],
        ["GetButilSetupGuide"] = ["hostingModel"],
        ["GetButilApiDetails"] = ["typeName"],
        ["PlanButilFeature"] = ["apis"],
        ["GetButilDocsPage"] = ["slug"],
        ["GetButilGuideSection"] = ["heading"],
        ["GetButilSourceFile"] = ["path"],
    };

    /// <summary>
    /// The tools whose only argument is optional, because calling them with nothing is a request
    /// for the list of what they can return. This is the fold that removed four listing tools, and
    /// it only holds while the argument stays out of the schema's "required".
    /// </summary>
    public static readonly string[] ListingTools =
    [
        "GetButilApiDetails", "GetButilDocsPage", "GetButilGuideSection", "GetButilSourceFile"
    ];

    /// <summary>
    /// The tools declared with UseStructuredContent: the ones that publish an output schema and put
    /// the object itself in structuredContent, so a client does not have to re-parse prose.
    /// </summary>
    public static readonly string[] StructuredTools =
    [
        "SearchButil", "GetButilApiDetails", "PlanButilFeature"
    ];

    /// <summary>The fixed-URI resources, by name.</summary>
    public static readonly IReadOnlyDictionary<string, string> Resources = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["butil-guide"] = "butil://guide",
        ["butil-api"] = "butil://api",
        ["butil-support"] = "butil://support",
    };

    /// <summary>The templated resources, by name, with the argument each template captures.</summary>
    public static readonly IReadOnlyDictionary<string, (string UriTemplate, string Argument)> ResourceTemplates =
        new Dictionary<string, (string, string)>(StringComparer.Ordinal)
        {
            ["butil-guide-section"] = ("butil://guide/{heading}", "heading"),
            ["butil-api-type"] = ("butil://api/{typeName}", "typeName"),
            ["butil-source"] = ("butil://source/{path}", "path"),
            ["butil-docs-page"] = ("butil://docs/{slug}", "slug"),
        };

    /// <summary>The prompts, by name, with the arguments each one declares.</summary>
    public static readonly IReadOnlyDictionary<string, string[]> Prompts = new Dictionary<string, string[]>(StringComparer.Ordinal)
    {
        ["add-butil-to-app"] = ["hostingModel"],
        ["implement-butil-feature"] = ["feature"],
        ["replace-jsinterop-with-butil"] = [],
        ["debug-butil-issue"] = ["symptom"],
    };

    /// <summary>The hosting models GetButilSetupGuide answers for.</summary>
    public static readonly string[] HostingModels = ["wasm", "web-app", "server", "hybrid"];

    /// <summary>
    /// The cap every document-shaped answer is held to, from DocsPageRenderer.MaxDocumentLength.
    /// It is a wire-visible promise about what one answer may cost a client's context window, so it
    /// is asserted from the outside rather than read off the server's own constant.
    /// </summary>
    public const int MaxDocumentLength = 40_000;

    /// <summary>The suffix a truncated answer ends with.</summary>
    public const string TruncationMarker = "[truncated - the full text is longer than";
}

/// <summary>
/// One row of the index <c>GetButilDocsPage</c> answers with when it is called with no slug - which
/// is also the whole of <c>butil://support</c>, the page listing and the browser-support matrix
/// having been folded into one table.
/// <para>
/// Parsed out of Markdown rather than deserialized, because that is the form the answer takes: a
/// listing is read and then one value from it is passed back, so it ships as a table an agent reads
/// instead of as a DTO with a tool description to advertise it. The suite reads it the same way,
/// which also holds the table's columns to a shape.
/// </para>
/// </summary>
public sealed partial record DocsIndexRow(string Group, string Slug, string Title, string Summary, string[] Services, string Engines, string[] Requires)
{
    /// <summary>Every row of the index, with the group heading each one sat under.</summary>
    public static DocsIndexRow[] ParseAll(string markdown)
    {
        var rows = new List<DocsIndexRow>();
        var group = string.Empty;

        foreach (var line in markdown.Split('\n').Select(line => line.TrimEnd('\r')))
        {
            if (line.StartsWith("## ", StringComparison.Ordinal))
            {
                group = line[3..].Trim();
                continue;
            }

            var match = RowRegex().Match(line);
            if (match.Success is false) continue;

            // Split on the pipes rather than on the pattern: the row is six cells, and a cell that
            // went missing should read as a short row here rather than as a row that did not match.
            var cells = line.Trim().Trim('|').Split('|').Select(cell => cell.Trim()).ToArray();
            if (cells.Length != 6) continue;

            rows.Add(new DocsIndexRow(group, match.Groups["slug"].Value, cells[1], cells[2], Cell(cells[3]), cells[4], Cell(cells[5])));
        }

        return [.. rows];
    }

    /// <summary>A list cell: comma-separated, or "-" when the row has none of that thing.</summary>
    private static string[] Cell(string text)
        => text is "-" or "" ? [] : [.. text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)];

    [GeneratedRegex(@"^\|\s*`(?<slug>[^`]+)`\s*\|")]
    private static partial Regex RowRegex();
}

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
