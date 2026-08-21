using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ModelContextProtocol.Client;
using Bit.Butil.Tests.Mcp.Infrastructure;

namespace Bit.Butil.Tests.Mcp;

/// <summary>
/// The shape of the tool surface: which tools exist, what each one is called, what it says about
/// itself, and what a client is told it may do without asking a person first.
/// <para>
/// A tool is invoked from a description and a schema, never from its source. Everything asserted
/// here is what an agent reads before it decides which tool to call and whether it needs
/// permission - so a missing annotation or an undeclared parameter is a functional defect, not a
/// documentation one.
/// </para>
/// </summary>
[TestClass]
public class ToolSurfaceTests : McpTestBase
{
    private IList<McpClientTool> _tools = null!;

    /// <summary>
    /// Listed once for the fixture: every test below reads the same advertised surface, and what it
    /// reads off each tool is data the client already holds rather than another call.
    /// </summary>
    [TestInitialize]
    public async Task ListTools()
    {
        _tools = await OncePerFixtureAsync(async () => await Mcp.ListToolsAsync(cancellationToken: Ct));
    }

    [TestMethod]
    public void Server_advertises_exactly_the_expected_tools()
    {
        var advertised = _tools.Select(tool => tool.Name).OrderBy(name => name, StringComparer.Ordinal).ToArray();
        var expected = ButilMcp.Tools.Keys.OrderBy(name => name, StringComparer.Ordinal).ToArray();

        // Both directions on purpose. A missing tool breaks a client that calls it; an unexpected
        // one is a tool nobody wrote a description, an annotation or a test for.
        Assert.AreSequenceEqual(expected, advertised,
            "The advertised tool names have changed. Renaming or removing one breaks every client that already holds the old name.");
    }

    [TestMethod]
    public void Every_tool_carries_a_title_and_a_description()
    {
        using (Assert.Scope())
        {
            foreach (var tool in _tools)
            {
                Assert.IsFalse(string.IsNullOrEmpty(tool.Title), $"{tool.Name} has no title for a person to read in a tool picker.");

                // The description is the only thing an agent has to choose with. These are written
                // to be read - a one-liner would defeat the point of the server.
                Assert.IsFalse(string.IsNullOrEmpty(tool.Description), $"{tool.Name} has no description.");
                Assert.IsGreaterThan(80, tool.Description!.Length,
                    $"{tool.Name}'s description is too thin for an agent to choose it from: '{tool.Description}'.");
            }
        }
    }

    [TestMethod]
    public void Every_tool_is_annotated_read_only_and_closed_world()
    {
        using (Assert.Scope())
        {
            foreach (var tool in _tools)
            {
                var annotations = tool.ProtocolTool.Annotations;

                Assert.IsNotNull(annotations, $"{tool.Name} carries no annotations.");

                // A client told a tool is read-only can run it without stopping to ask a person -
                // the difference between an agent that consults the documentation and one that
                // guesses rather than interrupt.
                Assert.IsTrue(annotations!.ReadOnlyHint, $"{tool.Name} must be annotated read-only.");
                Assert.IsTrue(annotations.IdempotentHint, $"{tool.Name} must be annotated idempotent.");
                Assert.IsFalse(annotations.DestructiveHint, $"{tool.Name} must be annotated non-destructive.");

                // OpenWorld = false says the answers come from this build rather than from the web,
                // so a disagreement with a search result is this library's version of the truth.
                Assert.IsFalse(annotations.OpenWorldHint, $"{tool.Name} must be annotated closed-world.");
            }
        }
    }

    [TestMethod]
    public void Every_tool_declares_exactly_the_parameters_it_takes()
    {
        using (Assert.Scope())
        {
            foreach (var tool in _tools)
            {
                var schema = tool.JsonSchema;

                Assert.AreEqual(JsonValueKind.Object, schema.ValueKind, $"{tool.Name} has no input schema object.");
                Assert.AreEqual("object", schema.GetProperty("type").GetString());

                string[] declared = schema.TryGetProperty("properties", out var properties)
                    ? [.. properties.EnumerateObject().Select(property => property.Name)]
                    : [];

                CollectionAssert.AreEquivalent(ButilMcp.Tools[tool.Name], declared,
                    $"{tool.Name} does not declare the parameters the suite expects.");
            }
        }
    }

    [TestMethod]
    public void Every_parameter_is_typed_and_every_optional_one_states_its_default()
    {
        // An argument an agent has to guess the shape of is an argument it gets wrong - and an
        // optional one it cannot see the default of is worse, because omitting it is then a guess
        // too. SearchButil's `limit` is the case: a client has to know it gets twelve hits.
        using (Assert.Scope())
        {
            foreach (var tool in _tools)
            {
                if (tool.JsonSchema.TryGetProperty("properties", out var properties) is false) continue;

                var required = tool.JsonSchema.TryGetProperty("required", out var element)
                    ? element.EnumerateArray().Select(value => value.GetString()).ToHashSet(StringComparer.Ordinal)
                    : [];

                foreach (var property in properties.EnumerateObject())
                {
                    Assert.IsTrue(property.Value.TryGetProperty("type", out _),
                        $"{tool.Name}.{property.Name} declares no type, so a client cannot know what to send.");

                    if (required.Contains(property.Name)) continue;

                    Assert.IsTrue(property.Value.TryGetProperty("default", out _),
                        $"{tool.Name}.{property.Name} is optional but publishes no default, so a client cannot tell what omitting it does.");
                }
            }
        }
    }

    [TestMethod]
    public void No_tool_publishes_an_output_schema()
    {
        // An output schema is not free and it is not what it looks like: declaring one makes the SDK
        // send the answer twice, as structuredContent and as the identical JSON in the text block
        // the protocol wants there anyway. The three data tools carried both, plus 2,800 characters
        // of schema in every tools/list. The JSON a client parses is the same either way.
        using (Assert.Scope())
        {
            foreach (var tool in _tools)
            {
                Assert.IsFalse(tool.ReturnJsonSchema.HasValue,
                    $"{tool.Name} publishes an output schema, so every one of its answers is now sent twice.");
            }
        }
    }

    [TestMethod]
    public async Task A_data_tool_answers_with_its_json_once()
    {
        // The other half of the same promise, from the wire rather than from the declaration.
        foreach (var tool in ButilMcp.DataTools)
        {
            var result = await CallRawAsync(tool, tool switch
            {
                "SearchButil" => new { query = "clipboard" },
                "PlanButilFeature" => (object)new { apis = "Clipboard" },
                _ => new { typeName = "Clipboard" }
            });

            using (Assert.Scope())
            {
                Assert.IsNull(result.StructuredContent,
                    $"{tool} answered with structuredContent as well as text - the same JSON, paid for twice.");

                Assert.StartsWith("{", Text(result), $"{tool} answers with data, so its text block is the JSON of it.");
            }
        }
    }

    [TestMethod]
    public void Required_arguments_are_marked_required()
    {
        using (Assert.Scope())
        {
            foreach (var tool in _tools)
            {
                string?[] required = tool.JsonSchema.TryGetProperty("required", out var element)
                    ? [.. element.EnumerateArray().Select(value => value.GetString())]
                    : [];

                switch (tool.Name)
                {
                    // The argument of each of these IS the question: a call without it is not a
                    // broader call, it is a call that cannot be answered.
                    case "SearchButil":
                        Assert.Contains("query", required);
                        Assert.DoesNotContain("limit", required, "limit has a default and must stay optional.");
                        break;

                    case "GetButilSetupGuide": Assert.Contains("hostingModel", required); break;
                    case "PlanButilFeature": Assert.Contains("apis", required); break;

                    // The retrieval tools are the other half of the fold that removed the listing
                    // tools: calling one with nothing is how a client asks what it can return.
                    // Marking the argument required would put those four listings back out of reach
                    // and leave nothing in their place.
                    default:
                        Assert.Contains(tool.Name, ButilMcp.ListingTools,
                            $"{tool.Name} requires none of its arguments but is not one of the tools that lists on an empty call.");
                        Assert.IsEmpty(required,
                            $"{tool.Name} lists what it can return when called with no argument, so its argument cannot be required.");
                        break;
                }
            }
        }
    }

    [TestMethod]
    public void The_tool_list_itself_fits_in_a_context_window()
    {
        // tools/list is put in front of the model on every session this server is connected to, so
        // its total size is a standing cost - and the reason the surface is seven tools rather than
        // the fourteen it started as. Richly described, they should be a couple of thousand tokens.
        var size = _tools.Sum(tool => tool.Name.Length
                                    + (tool.Title?.Length ?? 0)
                                    + (tool.Description?.Length ?? 0)
                                    + tool.JsonSchema.GetRawText().Length
                                    + (tool.ReturnJsonSchema?.GetRawText().Length ?? 0));

        Assert.IsLessThan(12_000, size,
            $"The advertised tool surface is {size} characters, which every session pays for up front.");
    }

}
