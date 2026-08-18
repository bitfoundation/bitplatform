using System.Text.Json;
using NUnit.Framework;
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
[TestFixture]
[Parallelizable(ParallelScope.Self)]
public class ToolSurfaceTests : McpTestBase
{
    private IList<McpClientTool> _tools = null!;

    [OneTimeSetUp]
    public async Task ListTools()
    {
        _tools = await Mcp.ListToolsAsync(cancellationToken: Ct);
    }

    [Test]
    public void Server_advertises_exactly_the_expected_tools()
    {
        var advertised = _tools.Select(tool => tool.Name).OrderBy(name => name, StringComparer.Ordinal).ToArray();
        var expected = ButilMcp.Tools.Keys.OrderBy(name => name, StringComparer.Ordinal).ToArray();

        // Both directions on purpose. A missing tool breaks a client that calls it; an unexpected
        // one is a tool nobody wrote a description, an annotation or a test for.
        Assert.That(advertised, Is.EqualTo(expected).AsCollection,
            "The advertised tool names have changed. Renaming or removing one breaks every client that already holds the old name.");
    }

    [Test]
    public void Every_tool_carries_a_title_and_a_description()
    {
        Assert.Multiple(() =>
        {
            foreach (var tool in _tools)
            {
                Assert.That(tool.Title, Is.Not.Null.And.Not.Empty, $"{tool.Name} has no title for a person to read in a tool picker.");

                // The description is the only thing an agent has to choose with. These are written
                // to be read - a one-liner would defeat the point of the server.
                Assert.That(tool.Description, Is.Not.Null.And.Not.Empty, $"{tool.Name} has no description.");
                Assert.That(tool.Description!.Length, Is.GreaterThan(80),
                    $"{tool.Name}'s description is too thin for an agent to choose it from: '{tool.Description}'.");
            }
        });
    }

    [Test]
    public void Every_tool_is_annotated_read_only_and_closed_world()
    {
        Assert.Multiple(() =>
        {
            foreach (var tool in _tools)
            {
                var annotations = tool.ProtocolTool.Annotations;

                Assert.That(annotations, Is.Not.Null, $"{tool.Name} carries no annotations.");

                // A client told a tool is read-only can run it without stopping to ask a person -
                // the difference between an agent that consults the documentation and one that
                // guesses rather than interrupt.
                Assert.That(annotations!.ReadOnlyHint, Is.True, $"{tool.Name} must be annotated read-only.");
                Assert.That(annotations.IdempotentHint, Is.True, $"{tool.Name} must be annotated idempotent.");
                Assert.That(annotations.DestructiveHint, Is.False, $"{tool.Name} must be annotated non-destructive.");

                // OpenWorld = false says the answers come from this build rather than from the web,
                // so a disagreement with a search result is this library's version of the truth.
                Assert.That(annotations.OpenWorldHint, Is.False, $"{tool.Name} must be annotated closed-world.");
            }
        });
    }

    [Test]
    public void Every_tool_declares_exactly_the_parameters_it_takes()
    {
        Assert.Multiple(() =>
        {
            foreach (var tool in _tools)
            {
                var schema = tool.JsonSchema;

                Assert.That(schema.ValueKind, Is.EqualTo(JsonValueKind.Object), $"{tool.Name} has no input schema object.");
                Assert.That(schema.GetProperty("type").GetString(), Is.EqualTo("object"));

                string[] declared = schema.TryGetProperty("properties", out var properties)
                    ? [.. properties.EnumerateObject().Select(property => property.Name)]
                    : [];

                Assert.That(declared, Is.EquivalentTo(ButilMcp.Tools[tool.Name]),
                    $"{tool.Name} does not declare the parameters the suite expects.");
            }
        });
    }

    [Test]
    public void Every_parameter_is_typed_and_every_optional_one_states_its_default()
    {
        // An argument an agent has to guess the shape of is an argument it gets wrong - and an
        // optional one it cannot see the default of is worse, because omitting it is then a guess
        // too. SearchButil's `limit` is the case: a client has to know it gets twelve hits.
        Assert.Multiple(() =>
        {
            foreach (var tool in _tools)
            {
                if (tool.JsonSchema.TryGetProperty("properties", out var properties) is false) continue;

                var required = tool.JsonSchema.TryGetProperty("required", out var element)
                    ? element.EnumerateArray().Select(value => value.GetString()).ToHashSet(StringComparer.Ordinal)
                    : [];

                foreach (var property in properties.EnumerateObject())
                {
                    Assert.That(property.Value.TryGetProperty("type", out _), Is.True,
                        $"{tool.Name}.{property.Name} declares no type, so a client cannot know what to send.");

                    if (required.Contains(property.Name)) continue;

                    Assert.That(property.Value.TryGetProperty("default", out _), Is.True,
                        $"{tool.Name}.{property.Name} is optional but publishes no default, so a client cannot tell what omitting it does.");
                }
            }
        });
    }

    [Test]
    public void Tools_that_answer_with_data_publish_an_output_schema()
    {
        Assert.Multiple(() =>
        {
            foreach (var tool in _tools)
            {
                var structured = ButilMcp.StructuredTools.Contains(tool.Name, StringComparer.Ordinal);

                Assert.That(tool.ReturnJsonSchema.HasValue, Is.EqualTo(structured),
                    structured
                        ? $"{tool.Name} is declared with UseStructuredContent, so it must publish an output schema."
                        : $"{tool.Name} answers with prose and should not publish an output schema.");
            }
        });
    }

    [Test]
    public void Required_arguments_are_marked_required()
    {
        Assert.Multiple(() =>
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
                        Assert.That(required, Does.Contain("query"));
                        Assert.That(required, Does.Not.Contain("limit"), "limit has a default and must stay optional.");
                        break;

                    case "GetButilSetupGuide": Assert.That(required, Does.Contain("hostingModel")); break;
                    case "GetButilApiDetails": Assert.That(required, Does.Contain("typeName")); break;
                    case "InspectButilApi": Assert.That(required, Does.Contain("name")); break;
                    case "PlanButilFeature": Assert.That(required, Does.Contain("apis")); break;
                    case "GetButilDocsPage": Assert.That(required, Does.Contain("slug")); break;
                    case "GetButilGuideSection": Assert.That(required, Does.Contain("heading")); break;
                    case "GetButilSourceFile": Assert.That(required, Does.Contain("path")); break;

                    default:
                        Assert.That(required, Is.Empty, $"{tool.Name} takes no arguments, so nothing can be required.");
                        break;
                }
            }
        });
    }

    [Test]
    public void The_tool_list_itself_fits_in_a_context_window()
    {
        // tools/list is put in front of the model on every session this server is connected to, so
        // its total size is a standing cost. Fourteen richly described tools should be a few
        // thousand tokens, not tens of thousands.
        var size = _tools.Sum(tool => tool.Name.Length
                                    + (tool.Title?.Length ?? 0)
                                    + (tool.Description?.Length ?? 0)
                                    + tool.JsonSchema.GetRawText().Length
                                    + (tool.ReturnJsonSchema?.GetRawText().Length ?? 0));

        Assert.That(size, Is.LessThan(80_000),
            $"The advertised tool surface is {size} characters, which every session pays for up front.");
    }

}
