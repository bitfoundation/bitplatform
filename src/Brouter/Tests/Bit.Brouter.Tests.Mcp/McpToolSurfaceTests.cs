using System.Text.Json;
using ModelContextProtocol.Client;

namespace Bit.Brouter.Tests.Mcp;

/// <summary>
/// The tools as a client sees them before calling one: the set, the annotations that decide whether
/// a client may run a tool without asking a person, and the schemas it validates arguments and
/// answers against. None of this is visible from the C# side - an attribute left off compiles
/// perfectly and only goes missing on the wire.
/// </summary>
[TestClass]
public class McpToolSurfaceTests
{
    /// <summary>
    /// The published set, spelled out. A tool added without being added here fails this test on
    /// purpose: a new tool needs its own coverage below, and a renamed one breaks every client that
    /// had learned the old name.
    /// </summary>
    internal static readonly string[] ExpectedTools =
    [
        "AnalyzeBrouterRouteTable",
        "GetBrouterApiDetails",
        "GetBrouterApiList",
        "GetBrouterDocsList",
        "GetBrouterDocsPage",
        "GetBrouterGuideSection",
        "GetBrouterGuideSections",
        "GetBrouterOverview",
        "GetBrouterRouteConstraints",
        "GetBrouterSetupGuide",
        "GetBrouterSourceFile",
        "GetBrouterSourceFiles",
        "GetBrouterTypedRoutes",
        "InspectBrouterRouteTemplate",
        "SearchBrouter",
    ];

    /// <summary>The tools that answer with a DTO, and therefore have to publish an output schema.</summary>
    private static readonly string[] _structuredTools =
    [
        "AnalyzeBrouterRouteTable",
        "GetBrouterApiDetails",
        "GetBrouterApiList",
        "GetBrouterDocsList",
        "GetBrouterGuideSections",
        "GetBrouterRouteConstraints",
        "GetBrouterSourceFiles",
        "GetBrouterTypedRoutes",
        "InspectBrouterRouteTemplate",
        "SearchBrouter",
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

            // The description is what a model picks a tool by; a one-liner is not enough to tell
            // fifteen documentation tools apart.
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

            // Both are defined only for tools that modify something; stating them on a reader says nothing.
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
    public void Arguments_that_take_a_key_are_declared_required()
    {
        (string Tool, string Argument)[] expected =
        [
            ("SearchBrouter", "query"),
            ("GetBrouterSetupGuide", "renderMode"),
            ("GetBrouterGuideSection", "heading"),
            ("GetBrouterApiDetails", "typeName"),
            ("GetBrouterSourceFile", "path"),
            ("InspectBrouterRouteTemplate", "template"),
            ("AnalyzeBrouterRouteTable", "templates"),
        ];

        foreach (var (name, argument) in expected)
        {
            var tool = _tools.Single(t => t.Name == name);
            var required = tool.JsonSchema.GetProperty("required").EnumerateArray().Select(value => value.GetString()).ToArray();

            CollectionAssert.Contains(required, argument, $"'{name}.{argument}' is not declared required.");
        }
    }

    [TestMethod]
    public void The_optional_arguments_are_the_ones_with_a_sensible_default()
    {
        // Both have an answer without them: the docs overview, and one page of results.
        var docsPage = _tools.Single(tool => tool.Name == "GetBrouterDocsPage");
        Assert.IsFalse(docsPage.JsonSchema.TryGetProperty("required", out _), "GetBrouterDocsPage should answer without a slug.");

        var search = _tools.Single(tool => tool.Name == "SearchBrouter");
        var searchRequired = search.JsonSchema.GetProperty("required").EnumerateArray().Select(value => value.GetString()).ToArray();
        CollectionAssert.DoesNotContain(searchRequired, "limit");
    }

    [TestMethod]
    public void The_render_mode_argument_is_a_closed_set_in_the_schema()
    {
        // Declared with [AllowedValues] rather than merely described, so a client offers the four
        // modes as a choice and a model cannot pass a fifth past them.
        var tool = _tools.Single(t => t.Name == "GetBrouterSetupGuide");
        var values = tool.JsonSchema.GetProperty("properties").GetProperty("renderMode").GetProperty("enum")
                         .EnumerateArray().Select(value => value.GetString()).ToArray();

        CollectionAssert.AreEquivalent(new[] { "server", "wasm", "auto", "standalone-wasm" }, values);
    }

    [TestMethod]
    public void The_search_limit_is_an_integer_with_the_documented_default()
    {
        var limit = _tools.Single(tool => tool.Name == "SearchBrouter")
                          .JsonSchema.GetProperty("properties").GetProperty("limit");

        Assert.AreEqual("integer", limit.GetProperty("type").GetString());
        Assert.AreEqual(12, limit.GetProperty("default").GetInt32());
    }

    [TestMethod]
    public void Every_argument_that_takes_a_key_shows_an_example_of_one()
    {
        // A key into a closed set is unguessable from outside, so the description is where a model
        // learns what one looks like before it goes and calls the listing tool.
        string[] keys = ["slug", "heading", "typeName", "path", "renderMode"];

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
    public void Tool_names_are_the_declared_ones_rather_than_camel_cased_method_names()
    {
        foreach (var tool in _tools)
        {
            Assert.AreNotEqual(JsonNamingPolicy.CamelCase.ConvertName(tool.Name), tool.Name,
                $"'{tool.Name}' is not the explicitly declared name it should be.");
        }
    }
}
