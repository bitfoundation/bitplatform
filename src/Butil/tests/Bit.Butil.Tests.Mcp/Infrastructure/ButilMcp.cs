using System.Text;
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
    /// name - so an eighth entry appearing here is the signal that the surface has started
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
    /// The tools that answer with data rather than with a document - their text block is the JSON of
    /// an object, and the suite deserializes it.
    /// <para>
    /// None of them declares UseStructuredContent, and that is asserted rather than assumed: the SDK
    /// answers such a tool with the object in structuredContent AND the same JSON, byte for byte, in
    /// a text block that the protocol wants there regardless. Every search, reference and plan was
    /// therefore paid for twice, on top of the output schemas sitting in every tools/list. Turning it
    /// off costs a client nothing - the JSON it parses is the same JSON - so a schema reappearing
    /// here is a doubling of the answer, which is what this list exists to catch.
    /// </para>
    /// </summary>
    public static readonly string[] DataTools =
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
