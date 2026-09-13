using Microsoft.VisualStudio.TestTools.UnitTesting;
using ModelContextProtocol.Client;
using Bit.BlazorUI.Tests.Mcp.Infrastructure;

namespace Bit.BlazorUI.Tests.Mcp;

/// <summary>
/// What a client is handed before it calls anything: the seven tools, their annotations, their
/// descriptions, and the instructions the server writes into the model's context at
/// <c>initialize</c>.
/// <para>
/// This is the part of the server that is paid for on every request of every session, so it is the
/// part worth pinning. A tool that appears without an annotation is a tool a client will stop to
/// ask a person about; an eighth tool that appears without anyone deciding it should is the cost
/// this server's design was spent avoiding.
/// </para>
/// </summary>
[TestClass]
public class ToolSurfaceTests : McpTestBase
{
    private async Task<IList<McpClientTool>> ToolsAsync() => await OncePerFixtureAsync<IList<McpClientTool>>(async () => await Mcp.ListToolsAsync(cancellationToken: Ct));

    [TestMethod]
    public async Task Publishes_exactly_the_seven_tools()
    {
        var tools = await ToolsAsync();

        CollectionAssert.AreEquivalent(ToolNames, tools.Select(t => t.Name).ToArray(),
            "The tool set is the design of this server, not an accident. Adding one costs every session's context; removing one breaks the calls the answers name.");
    }

    [TestMethod]
    public async Task Every_tool_is_annotated_read_only_and_closed_world()
    {
        using var scope = Assert.Scope();

        foreach (var tool in await ToolsAsync())
        {
            var annotations = tool.ProtocolTool.Annotations;

            Assert.IsNotNull(annotations, $"{tool.Name} has no annotations.");
            Assert.IsNotEmpty(annotations.Title ?? string.Empty, $"{tool.Name} has no title.");

            // Read-only is what lets a client run the tool without stopping to ask a person, which
            // is the difference between an agent that consults the documentation and one that
            // guesses rather than interrupt.
            Assert.AreEqual(true, annotations.ReadOnlyHint, $"{tool.Name} is not marked read-only.");
            Assert.AreEqual(true, annotations.IdempotentHint, $"{tool.Name} is not marked idempotent.");
            Assert.AreEqual(false, annotations.DestructiveHint, $"{tool.Name} is not marked non-destructive.");

            // Closed world says the answers come from this build rather than from the web, so a
            // disagreement with a search result is this library's version of the truth.
            Assert.AreEqual(false, annotations.OpenWorldHint, $"{tool.Name} is not marked closed-world.");
        }
    }

    [TestMethod]
    public async Task Every_tool_and_every_argument_carries_a_description()
    {
        using var scope = Assert.Scope();

        foreach (var tool in await ToolsAsync())
        {
            Assert.IsGreaterThan(120, (tool.Description ?? string.Empty).Length,
                $"{tool.Name}'s description is too short to tell a model when to reach for it over its neighbour.");

            var schema = tool.ProtocolTool.InputSchema;

            if (schema.TryGetProperty("properties", out var properties) is false) continue;

            foreach (var argument in properties.EnumerateObject())
            {
                Assert.IsTrue(argument.Value.TryGetProperty("description", out var description) && description.GetString()?.Length > 20,
                    $"{tool.Name}'s '{argument.Name}' argument has no usable description.");
            }
        }
    }

    [TestMethod]
    public async Task No_tool_publishes_an_output_schema()
    {
        using var scope = Assert.Scope();

        foreach (var tool in await ToolsAsync())
        {
            // A declared output schema makes the SDK answer with the payload twice - once as
            // structuredContent and once as text, because the protocol asks a server to keep
            // answering clients that cannot read a schema - and the schemas themselves are paid for
            // in tools/list on every session. These tools answer in Markdown, once.
            Assert.IsNull(tool.ProtocolTool.OutputSchema, $"{tool.Name} publishes an output schema.");
        }
    }

    [TestMethod]
    public async Task Instructions_name_the_first_tool_and_the_standing_rules()
    {
        var instructions = Mcp.ServerInstructions ?? string.Empty;

        using var scope = Assert.Scope();

        Assert.IsNotEmpty(instructions, "The server sends no instructions, which is the one thing it gets to say before it is asked anything.");
        StringAssert.Contains(instructions, "SearchBitBlazorUI", "The instructions do not say which tool to call first.");
        StringAssert.Contains(instructions, "AddBitBlazorUIServices", "The instructions do not carry the registration that fails without a build error.");
        StringAssert.Contains(instructions, "IsEnabled", "The instructions do not carry the disabled-state rule.");
        StringAssert.Contains(instructions, "--bit-*", "The instructions do not carry the styling rule.");

        // The counts are interpolated from the catalogs rather than written down, so a hand-typed
        // number cannot go stale in the one message nothing can be checked against.
        Assert.DoesNotContain("110 components", instructions, "The instructions hard-code a component count.");
    }

    [TestMethod]
    public void Server_identifies_itself_with_the_shipped_version()
    {
        var info = Mcp.ServerInfo;

        using var scope = Assert.Scope();

        Assert.AreEqual("bit-blazorui", info.Name);
        Assert.IsNotEmpty(info.Version ?? string.Empty, "The server publishes no version.");
        StringAssert.Contains(Mcp.ServerInstructions ?? string.Empty, info.Version!,
            "The instructions quote a different version from the one the server reports.");
    }
}
