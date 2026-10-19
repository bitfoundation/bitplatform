using System.Text.Json;
using ModelContextProtocol.Client;

namespace Bit.Blex.Tests.Mcp;

/// <summary>
/// The tools as a client sees them before calling one: the set, the annotations that decide whether a
/// client may run a tool without asking a person, and the schemas it validates arguments and answers
/// against. None of this is visible from the C# side - an attribute left off compiles perfectly and only
/// goes missing on the wire.
/// </summary>
[TestClass]
public class McpToolSurfaceTests
{
    /// <summary>The published set, spelled out. A tool added or renamed without being added here fails on purpose.</summary>
    internal static readonly string[] ExpectedTools =
    [
        "GetBlexApi",
        "GetBlexDiagnostics",
        "GetBlexDocsPage",
        "GetBlexGuideSection",
        "GetBlexSetupGuide",
        "GetBlexSourceFile",
        "InspectBlexStore",
        "SearchBlex",
    ];

    /// <summary>The tools that answer with a DTO, and therefore have to publish an output schema - and only those.</summary>
    private static readonly string[] _structuredTools = ["InspectBlexStore", "SearchBlex"];

    /// <summary>The reference tools, and the key each one answers by. Leaving the key out asks for the index.</summary>
    internal static readonly (string Tool, string Argument)[] KeyedTools =
    [
        ("GetBlexApi", "typeName"),
        ("GetBlexDiagnostics", "code"),
        ("GetBlexDocsPage", "slug"),
        ("GetBlexGuideSection", "heading"),
        ("GetBlexSourceFile", "path"),
    ];

    private static IList<McpClientTool> _tools = [];

    [ClassInitialize]
    public static async Task ListToolsAsync(TestContext context)
    {
        _tools = await McpTestHost.Client.ListToolsAsync(cancellationToken: context.CancellationToken);
    }

    [TestMethod]
    public void Every_expected_tool_is_published_and_nothing_else_is()
    {
        CollectionAssert.AreEquivalent(ExpectedTools, _tools.Select(tool => tool.Name).ToArray());
    }

    [TestMethod]
    public void Every_tool_carries_a_title_and_a_description_worth_reading()
    {
        foreach (var tool in _tools)
        {
            Assert.IsFalse(string.IsNullOrWhiteSpace(tool.Title), $"'{tool.Name}' has no title, so a client lists it by its method name.");
            Assert.IsTrue(tool.Description?.Length > 80,
                $"'{tool.Name}' has a description of {tool.Description?.Length ?? 0} characters - too short to choose it by.");
        }
    }

    [TestMethod]
    public void Every_tool_is_annotated_read_only_and_closed_world()
    {
        foreach (var tool in _tools)
        {
            var annotations = tool.ProtocolTool.Annotations;

            Assert.IsNotNull(annotations, $"'{tool.Name}' carries no annotations, so a client has to assume it might write.");
            Assert.AreEqual(true, annotations.ReadOnlyHint, $"'{tool.Name}' is not marked read-only; a client will stop and ask before running it.");
            Assert.AreEqual(false, annotations.OpenWorldHint, $"'{tool.Name}' is not marked closed-world.");
            Assert.IsNull(annotations.DestructiveHint, $"'{tool.Name}' states a destructive hint, which is meaningless on a read-only tool.");
            Assert.IsNull(annotations.IdempotentHint, $"'{tool.Name}' states an idempotent hint, which is meaningless on a read-only tool.");
        }
    }

    [TestMethod]
    public void Tools_that_answer_with_an_object_publish_an_output_schema()
    {
        foreach (var tool in _tools)
        {
            var structured = _structuredTools.Contains(tool.Name);

            Assert.AreEqual(structured, tool.ReturnJsonSchema is not null,
                structured
                    ? $"'{tool.Name}' answers with a DTO but publishes no output schema, so a client gets JSON it cannot validate."
                    : $"'{tool.Name}' answers with prose but publishes an output schema.");
        }
    }

    [TestMethod]
    public void Every_argument_is_described()
    {
        foreach (var tool in _tools)
        {
            if (tool.JsonSchema.TryGetProperty("properties", out var properties) is false) continue;

            foreach (var argument in properties.EnumerateObject())
            {
                Assert.IsTrue(argument.Value.TryGetProperty("description", out var description) &&
                              string.IsNullOrWhiteSpace(description.GetString()) is false,
                    $"'{tool.Name}' takes an undescribed argument '{argument.Name}', which a model then has to guess at.");
            }
        }
    }

    [TestMethod]
    public void Arguments_that_carry_the_request_are_declared_required()
    {
        (string Tool, string Argument)[] expected =
        [
            ("SearchBlex", "query"),
            ("GetBlexSetupGuide", "hostingModel"),
            ("InspectBlexStore", "source"),
        ];

        foreach (var (name, argument) in expected)
        {
            var tool = _tools.Single(t => t.Name == name);
            var required = tool.JsonSchema.GetProperty("required").EnumerateArray().Select(value => value.GetString()).ToArray();

            CollectionAssert.Contains(required, argument, $"'{name}.{argument}' is not declared required.");
        }

        // The options beside them have defaults; a caller who leaves them out gets the documented behavior.
        var searchRequired = _tools.Single(tool => tool.Name == "SearchBlex").JsonSchema.GetProperty("required").EnumerateArray().Select(v => v.GetString()).ToArray();
        CollectionAssert.DoesNotContain(searchRequired, "limit");

        var inspectRequired = _tools.Single(tool => tool.Name == "InspectBlexStore").JsonSchema.GetProperty("required").EnumerateArray().Select(v => v.GetString()).ToArray();
        CollectionAssert.DoesNotContain(inspectRequired, "includeGeneratedCode");
    }

    [TestMethod]
    public void A_reference_tools_key_is_optional_because_leaving_it_out_asks_for_the_index()
    {
        foreach (var (name, argument) in KeyedTools)
        {
            var tool = _tools.Single(t => t.Name == name);

            Assert.IsFalse(tool.JsonSchema.TryGetProperty("required", out _),
                $"'{name}' declares an argument required, so a caller cannot ask it for the index of what there is.");

            Assert.IsTrue(tool.JsonSchema.GetProperty("properties").TryGetProperty(argument, out _),
                $"'{name}' does not take the '{argument}' key it is supposed to answer by.");

            StringAssert.Contains(tool.Description!, "Omit", $"'{name}' never says what leaving its key out answers with.");
        }
    }

    [TestMethod]
    public void The_hosting_model_argument_is_a_closed_set_in_the_schema()
    {
        var tool = _tools.Single(t => t.Name == "GetBlexSetupGuide");
        var values = tool.JsonSchema.GetProperty("properties").GetProperty("hostingModel").GetProperty("enum")
                         .EnumerateArray().Select(value => value.GetString()).ToArray();

        CollectionAssert.AreEquivalent(Bit.Blex.Demo.Server.Services.BlexSetupGuide.HostingModels, values);
    }

    [TestMethod]
    public void The_search_limit_is_an_integer_with_the_documented_default()
    {
        var limit = _tools.Single(tool => tool.Name == "SearchBlex").JsonSchema.GetProperty("properties").GetProperty("limit");

        Assert.AreEqual("integer", limit.GetProperty("type").GetString());
        Assert.AreEqual(12, limit.GetProperty("default").GetInt32());
    }

    [TestMethod]
    public void Every_argument_that_takes_a_key_shows_an_example_of_one()
    {
        string[] keys = ["slug", "heading", "typeName", "path", "code", "hostingModel"];

        foreach (var tool in _tools)
        {
            if (tool.JsonSchema.TryGetProperty("properties", out var properties) is false) continue;

            foreach (var argument in properties.EnumerateObject())
            {
                if (keys.Contains(argument.Name) is false) continue;

                var description = argument.Value.GetProperty("description").GetString()!;

                Assert.IsTrue(description.Contains("e.g.", StringComparison.Ordinal) || description.Contains('\''),
                    $"'{tool.Name}.{argument.Name}' names no example value.");
            }
        }
    }

    [TestMethod]
    public void The_whole_tool_surface_stays_small_enough_to_carry_in_every_request()
    {
        // Names, descriptions and schemas are re-sent with every request a client makes, for as long as
        // the session lasts. A budget, so that growing it is a decision rather than a drift.
        var characters = _tools.Sum(tool => tool.Name.Length + (tool.Title?.Length ?? 0) + (tool.Description?.Length ?? 0)
                                            + JsonSerializer.Serialize(tool.JsonSchema).Length
                                            + (tool.ReturnJsonSchema is null ? 0 : JsonSerializer.Serialize(tool.ReturnJsonSchema).Length));

        Assert.IsTrue(characters < 16_000, $"The published tool surface is {characters} characters, which every request now carries.");
    }
}
