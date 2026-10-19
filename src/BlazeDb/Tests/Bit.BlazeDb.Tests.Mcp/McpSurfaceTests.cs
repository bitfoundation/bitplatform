using System.Text.Json;
using Bit.BlazeDb.Demo.Server.Services;
using ModelContextProtocol.Client;

namespace Bit.BlazeDb.Tests.Mcp;

/// <summary>
/// The server as a client sees it before calling anything: the handshake, the published tools, the
/// annotations that decide whether a client may run a tool without asking a person, and the schemas
/// it validates arguments and answers against. None of this is visible from the C# side - an
/// attribute left off compiles perfectly and only goes missing on the wire.
/// </summary>
[TestClass]
public class McpSurfaceTests
{
    /// <summary>
    /// The published set, spelled out. A tool added without being added here fails on purpose: a new
    /// tool needs its own coverage, and a renamed one breaks every client that had learned the old name -
    /// including bitplatform.dev/mcp, which proxies these names as they are.
    /// </summary>
    internal static readonly string[] ExpectedTools =
    [
        "GetBlazeDbApi",
        "GetBlazeDbDiagnostics",
        "GetBlazeDbGuideSection",
        "GetBlazeDbSetupGuide",
        "GetBlazeDbSourceFile",
        "InspectBlazeDbTable",
        "SearchBlazeDb",
    ];

    /// <summary>The tools that answer with a DTO, and therefore have to publish an output schema.</summary>
    private static readonly string[] _structuredTools = ["InspectBlazeDbTable", "SearchBlazeDb"];

    /// <summary>The reference tools, and the key each one takes - optional, since leaving it out answers with the index.</summary>
    private static readonly (string Tool, string Argument)[] _keyedTools =
    [
        ("GetBlazeDbApi", "typeName"),
        ("GetBlazeDbDiagnostics", "code"),
        ("GetBlazeDbGuideSection", "heading"),
        ("GetBlazeDbSourceFile", "path"),
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

        Assert.AreEqual("bit-blazedb", info.Name);
        Assert.AreEqual("Bit.BlazeDb", info.Title);
        Assert.AreEqual(BlazeDbServerInstructions.BlazeDbVersion, info.Version);
        Assert.AreNotEqual("unknown", info.Version);
        Assert.DoesNotContain("+", info.Version, "The build metadata should have been stripped from the version.");
    }

    [TestMethod]
    public void The_version_is_the_library_package_version()
    {
        // The package version is stamped into the assembly by Bit.Build.props; the MCP version must be
        // that, not the demo host's, since the answers are about the library.
        var informational = typeof(BlazeDbDatabase).Assembly
            .GetCustomAttributes(typeof(System.Reflection.AssemblyInformationalVersionAttribute), false)
            .OfType<System.Reflection.AssemblyInformationalVersionAttribute>()
            .Single().InformationalVersion;

        Assert.StartsWith(BlazeDbServerInstructions.BlazeDbVersion, informational);
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

        Assert.AreEqual(BlazeDbServerInstructions.Text, instructions);
        Assert.Contains(BlazeDbServerInstructions.BlazeDbVersion, instructions!);

        var mentioned = ToolNames.MentionedIn(instructions!).ToArray();

        Assert.IsNotEmpty(mentioned);
        foreach (var tool in mentioned) CollectionAssert.Contains(ExpectedTools, tool, $"The instructions mention '{tool}', which is not a tool.");
    }

    [TestMethod]
    public void Every_expected_tool_is_published_and_nothing_else_is()
    {
        CollectionAssert.AreEquivalent(ExpectedTools, _tools.Select(tool => tool.Name).ToArray());
    }

    [TestMethod]
    public void Every_tool_name_carries_the_library_name()
    {
        // bitplatform.dev/mcp merges the tools of every bit library into one list, where a name is the
        // only address a client has and the first server to claim one wins. "BlazeDb" in every name is
        // what keeps these from colliding with SearchBmotion, GetBitBlazorUIComponent and the rest.
        foreach (var tool in _tools) Assert.Contains("BlazeDb", tool.Name, $"'{tool.Name}' would be ambiguous next to the other libraries' tools.");
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
                    $"'{name}' requires '{argument}', so it can no longer answer with the index.");
            }
        }
    }

    [TestMethod]
    public void The_hosting_model_is_a_closed_set_in_the_schema()
    {
        var tool = _tools.Single(tool => tool.Name == "GetBlazeDbSetupGuide");

        var values = tool.JsonSchema.GetProperty("properties").GetProperty("hostingModel").GetProperty("enum")
                         .EnumerateArray().Select(value => value.GetString()).ToArray();

        CollectionAssert.AreEquivalent(BlazeDbSetupGuide.HostingModels, values);
    }

    [TestMethod]
    public void The_tool_list_stays_cheap_to_send_with_every_request()
    {
        // Every tool's name, description and schemas ride along with every request an agent makes.
        var size = _tools.Sum(tool => tool.Name.Length + tool.Description.Length
                                      + JsonSerializer.Serialize(tool.JsonSchema).Length
                                      + (tool.ReturnJsonSchema is null ? 0 : JsonSerializer.Serialize(tool.ReturnJsonSchema).Length));

        Assert.IsLessThan(24_000, size, $"The tool list costs {size} characters per request.");
    }
}
