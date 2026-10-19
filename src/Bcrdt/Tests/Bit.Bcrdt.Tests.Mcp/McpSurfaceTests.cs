using System.Text.Json;
using System.Text.RegularExpressions;
using Bit.Bcrdt.Demo.Server.Services;
using ModelContextProtocol.Client;

namespace Bit.Bcrdt.Tests.Mcp;

/// <summary>
/// The server as a client sees it before calling anything: the handshake, the published tools, the
/// annotations that decide whether a client may run a tool without asking a person, and the schemas
/// it validates arguments and answers against. None of this is visible from the C# side - an
/// attribute left off compiles perfectly and only goes missing on the wire.
/// </summary>
[TestClass]
public partial class McpSurfaceTests
{
    /// <summary>
    /// The published set, spelled out. A tool added without being added here fails on purpose: a new
    /// tool needs its own coverage, and a renamed one breaks every client that had learned the old name -
    /// including bitplatform.dev/mcp, which proxies these names as they are, the README's MCP section and
    /// the demo's MCP page, which list them.
    /// </summary>
    internal static readonly string[] ExpectedTools =
    [
        "GetBcrdtApi",
        "GetBcrdtGuideSection",
        "GetBcrdtSetupGuide",
        "GetBcrdtSourceFile",
        "InspectBcrdtMerge",
        "InspectBcrdtUpdate",
        "SearchBcrdt",
    ];

    /// <summary>The tools that answer with a DTO, and therefore have to publish an output schema.</summary>
    private static readonly string[] _structuredTools = ["InspectBcrdtMerge", "InspectBcrdtUpdate", "SearchBcrdt"];

    /// <summary>The tools whose key is optional, since leaving it out answers with the index (or a sample).</summary>
    private static readonly (string Tool, string Argument)[] _keyedTools =
    [
        ("GetBcrdtApi", "typeName"),
        ("GetBcrdtGuideSection", "heading"),
        ("GetBcrdtSetupGuide", "hostingModel"),
        ("GetBcrdtSourceFile", "path"),
        ("InspectBcrdtMerge", "operations"),
        ("InspectBcrdtUpdate", "payload"),
    ];

    private static IList<McpClientTool> _tools = [];

    [ClassInitialize]
    public static async Task ListToolsAsync(TestContext context)
    {
        _tools = await McpTestHost.Client.ListToolsAsync(cancellationToken: context.CancellationToken);
    }

    [TestMethod]
    public void The_server_introduces_itself_as_the_library_at_its_own_version()
    {
        var info = McpTestHost.Client.ServerInfo;

        Assert.AreEqual("bit-bcrdt", info.Name);
        Assert.AreEqual("Bit.Bcrdt", info.Title);
        Assert.AreEqual(BcrdtServerInstructions.BcrdtVersion, info.Version);
        Assert.AreEqual("https://github.com/bitfoundation/bitplatform/tree/develop/src/Bcrdt", info.WebsiteUrl);
    }

    [TestMethod]
    public void The_version_is_the_release_version_the_packages_ship_as()
    {
        // The build already refuses a stale copy (see the ReleaseVersionLiteral item of the demo server's
        // .csproj); this says the same thing where a failing test run will be read.
        var props = FindRepositoryFile("Bit.Build.props");
        if (props is null) Assert.Inconclusive("Bit.Build.props is not next to this build.");

        var release = ReleaseVersionRegex().Match(File.ReadAllText(props)).Groups["version"].Value;

        Assert.IsFalse(string.IsNullOrEmpty(release), "Bit.Build.props declares no ReleaseVersion.");
        Assert.AreEqual(BcrdtServerInstructions.BcrdtVersion, release, "BcrdtServerInstructions.BcrdtVersion is not the ReleaseVersion of Bit.Build.props.");
    }

    [TestMethod]
    public void The_handshake_advertises_tools_resources_prompts_and_completions()
    {
        var capabilities = McpTestHost.Client.ServerCapabilities;

        Assert.IsNotNull(capabilities.Tools);
        Assert.IsNotNull(capabilities.Resources);
        Assert.IsNotNull(capabilities.Prompts);
        Assert.IsNotNull(capabilities.Completions);
    }

    [TestMethod]
    public void The_instructions_name_the_version_and_only_tools_that_exist()
    {
        var instructions = McpTestHost.Client.ServerInstructions;

        Assert.AreEqual(BcrdtServerInstructions.Text, instructions);
        Assert.Contains(BcrdtServerInstructions.BcrdtVersion, instructions!);

        var mentioned = ToolNames.MentionedIn(instructions!).ToArray();

        Assert.IsNotEmpty(mentioned);
        foreach (var tool in mentioned) CollectionAssert.Contains(ExpectedTools, tool, $"The instructions mention '{tool}', which is not a tool.");
    }

    [TestMethod]
    public void The_instructions_carry_the_rules_that_fail_silently()
    {
        var instructions = McpTestHost.Client.ServerInstructions!;

        // The wiring mistakes that compile and then fail at runtime, and the modelling mistake that quietly
        // throws one person's typing away - each costs an agent a debugging session it cannot see coming.
        Assert.Contains("both projects", instructions);
        Assert.Contains("absolute hub URL", instructions);
        Assert.Contains("AddBitBcrdtHub", instructions);
        Assert.Contains("UseWebSockets", instructions);
        Assert.Contains("one value wins", instructions);
        Assert.Contains("not thread-safe", instructions);
    }

    [TestMethod]
    public void Every_expected_tool_is_published_and_nothing_else_is()
    {
        CollectionAssert.AreEquivalent(ExpectedTools, _tools.Select(tool => tool.Name).ToArray());
    }

    [TestMethod]
    public void The_readme_documents_exactly_the_published_tools()
    {
        // The README's MCP section is what a person reads before connecting; a tool it names that the server
        // does not publish sends them looking for nothing.
        var section = BcrdtSourceCatalog.GetGuideSection("MCP server")!;

        CollectionAssert.AreEquivalent(ExpectedTools, ToolNames.MentionedIn(section).ToArray());
    }

    [TestMethod]
    public void Every_tool_name_carries_the_library_name()
    {
        // bitplatform.dev/mcp merges the tools of every bit library into one list, where a name is the
        // only address a client has and the first server to claim one wins. "Bcrdt" in every name is
        // what keeps these from colliding with SearchBalette, GetBitBlazorUIComponent and the rest.
        foreach (var tool in _tools) Assert.Contains("Bcrdt", tool.Name, $"'{tool.Name}' would be ambiguous next to the other libraries' tools.");
    }

    [TestMethod]
    public void Every_tool_carries_a_title_and_a_description_worth_reading()
    {
        foreach (var tool in _tools)
        {
            Assert.IsFalse(string.IsNullOrWhiteSpace(tool.Title), $"'{tool.Name}' has no title.");
            Assert.IsGreaterThan(80, tool.Description.Length, $"'{tool.Name}' has a description too short to choose it by.");
        }
    }

    [TestMethod]
    public void Every_tool_is_read_only_and_closed_world()
    {
        foreach (var tool in _tools)
        {
            var annotations = tool.ProtocolTool.Annotations;

            Assert.IsNotNull(annotations, $"'{tool.Name}' has no annotations.");
            Assert.AreEqual(true, annotations.ReadOnlyHint, $"'{tool.Name}' is not marked read-only; a client will stop and ask before running it.");
            Assert.AreEqual(false, annotations.OpenWorldHint, $"'{tool.Name}' is not marked closed-world.");
        }
    }

    [TestMethod]
    public void Only_the_machine_actionable_tools_publish_an_output_schema()
    {
        foreach (var tool in _tools)
        {
            var structured = _structuredTools.Contains(tool.Name);

            Assert.AreEqual(structured, tool.ReturnJsonSchema is not null,
                structured ? $"'{tool.Name}' answers with a DTO but publishes no output schema." : $"'{tool.Name}' answers in prose but publishes an output schema.");
        }
    }

    [TestMethod]
    public void The_key_of_every_reference_tool_is_optional()
    {
        foreach (var (name, argument) in _keyedTools)
        {
            var tool = _tools.Single(tool => tool.Name == name);

            Assert.IsTrue(tool.JsonSchema.GetProperty("properties").TryGetProperty(argument, out _), $"'{name}' does not take '{argument}'.");

            if (tool.JsonSchema.TryGetProperty("required", out var required))
            {
                Assert.IsFalse(required.EnumerateArray().Any(value => value.GetString() == argument),
                    $"'{name}' requires '{argument}', so it can no longer answer with the index (or a sample).");
            }
        }
    }

    [TestMethod]
    public void The_closed_sets_are_in_the_schema()
    {
        // [AllowedValues] puts the values where a client validates against them and offers them as
        // completions; described in prose only, a model passes whatever it guesses.
        AssertEnum("GetBcrdtSetupGuide", "hostingModel", BcrdtSetupGuide.HostingModels);
        AssertEnum("InspectBcrdtUpdate", "readAs", BcrdtUpdateInspector.ReadAs);
    }

    [TestMethod]
    public void The_search_takes_the_query_and_nothing_else_is_required_anywhere()
    {
        foreach (var tool in _tools)
        {
            var required = tool.JsonSchema.TryGetProperty("required", out var list) ? list.EnumerateArray().Select(value => value.GetString()).ToArray() : [];

            CollectionAssert.AreEquivalent(tool.Name == "SearchBcrdt" ? new[] { "query" } : [], required, tool.Name);
        }
    }

    [TestMethod]
    public void The_tool_list_stays_cheap_to_send_with_every_request()
    {
        // Every tool's name, description and schemas ride along with every request an agent makes.
        var size = _tools.Sum(tool => tool.Name.Length + tool.Description.Length
                                      + JsonSerializer.Serialize(tool.JsonSchema).Length
                                      + (tool.ReturnJsonSchema is null ? 0 : JsonSerializer.Serialize(tool.ReturnJsonSchema).Length));

        Assert.IsLessThan(16_000, size, $"The tool list costs {size} characters per request.");
    }

    private static void AssertEnum(string toolName, string argument, string[] expected)
    {
        var tool = _tools.Single(tool => tool.Name == toolName);

        var values = tool.JsonSchema.GetProperty("properties").GetProperty(argument).GetProperty("enum")
                         .EnumerateArray().Where(value => value.ValueKind == JsonValueKind.String).Select(value => value.GetString()).ToArray();

        CollectionAssert.AreEquivalent(expected, values, $"{toolName}.{argument}");
    }

    internal static string? FindRepositoryFile(params string[] parts)
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            var candidate = Path.Combine([directory.FullName, .. parts]);
            if (File.Exists(candidate)) return candidate;
        }

        return null;
    }

    [GeneratedRegex(@"<ReleaseVersion>(?<version>[^<]+)</ReleaseVersion>")]
    private static partial Regex ReleaseVersionRegex();
}
