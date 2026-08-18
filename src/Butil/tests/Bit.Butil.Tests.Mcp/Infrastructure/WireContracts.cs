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
    /// <summary>Every tool the server is expected to advertise, and the arguments each one takes.</summary>
    public static readonly IReadOnlyDictionary<string, string[]> Tools = new Dictionary<string, string[]>(StringComparer.Ordinal)
    {
        ["GetButilOverview"] = [],
        ["SearchButil"] = ["query", "limit"],
        ["GetButilSetupGuide"] = ["hostingModel"],
        ["GetButilApiList"] = [],
        ["GetButilApiDetails"] = ["typeName"],
        ["InspectButilApi"] = ["name"],
        ["PlanButilFeature"] = ["apis"],
        ["GetButilBrowserSupport"] = [],
        ["GetButilDocsList"] = [],
        ["GetButilDocsPage"] = ["slug"],
        ["GetButilGuideSections"] = [],
        ["GetButilGuideSection"] = ["heading"],
        ["GetButilSourceFiles"] = [],
        ["GetButilSourceFile"] = ["path"],
    };

    /// <summary>
    /// The tools declared with UseStructuredContent: the ones that publish an output schema and put
    /// the object itself in structuredContent, so a client does not have to re-parse prose.
    /// </summary>
    public static readonly string[] StructuredTools =
    [
        "SearchButil", "GetButilApiList", "GetButilApiDetails", "InspectButilApi", "PlanButilFeature",
        "GetButilBrowserSupport", "GetButilDocsList", "GetButilGuideSections", "GetButilSourceFiles"
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
