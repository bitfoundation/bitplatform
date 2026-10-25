using System.Text.RegularExpressions;
using Bit.Blex.Demo.Server.Dtos;
using Bit.Blex.Demo.Server.Services;

namespace Bit.Blex.Tests.Mcp;

/// <summary>
/// The joins between the catalogs - the places where one tool hands a caller a value another tool has to
/// accept. A heading renamed in the README, a docs page removed, a tool renamed: each leaves every
/// individual tool working and the path between them broken. These tests walk those paths.
/// </summary>
[TestClass]
public partial class McpCatalogConsistencyTests
{
    private static readonly string[] _apologies =
    [
        "No documentation page has the slug",
        "has no section called",
        "has no public type called",
        "has no diagnostic",
        "No source file at",
        "is not a known hosting model",
        "could not be rendered",
    ];

    [TestMethod]
    public async Task Every_call_a_search_hit_suggests_is_one_this_server_answers()
    {
        string[] queries =
        [
            "store", "state", "action", "effect", "computed", "selector", "persist", "history", "devtools",
            "middleware", "entity", "testing", "error", "maui", "partial", "cancel",
        ];

        var suggestions = new SortedSet<string>(StringComparer.Ordinal);

        foreach (var query in queries)
        {
            var result = await McpCall.StructuredAsync<BlexSearchResultDto>("SearchBlex", new() { ["query"] = query, ["limit"] = 20 });

            foreach (var hit in result.Hits) suggestions.Add(hit.Tool);
        }

        Assert.IsTrue(suggestions.Count > 50, $"Only {suggestions.Count} distinct follow-up calls were suggested; the sample is too small to say much.");

        foreach (var suggestion in suggestions)
        {
            var call = ToolCallRegex().Match(suggestion);

            Assert.IsTrue(call.Success, $"'{suggestion}' is not a tool call a caller could make verbatim.");

            var tool = call.Groups["tool"].Value;
            CollectionAssert.Contains(McpToolSurfaceTests.ExpectedTools, tool, $"'{suggestion}' names a tool this server does not expose.");

            var arguments = new Dictionary<string, object?>();
            if (call.Groups["argument"].Success) arguments[call.Groups["argument"].Value] = call.Groups["value"].Value;

            var answer = await McpCall.TextAsync(tool, arguments.Count == 0 ? null : arguments);

            foreach (var apology in _apologies)
            {
                Assert.IsFalse(answer.Contains(apology, StringComparison.Ordinal),
                    $"'{suggestion}' was suggested by a search hit, and answers with: {answer[..Math.Min(200, answer.Length)]}");
            }
        }
    }

    [TestMethod]
    public async Task The_documentation_pages_are_the_same_set_however_a_client_asks_for_them()
    {
        var index = await McpCall.TextAsync("GetBlexDocsPage");

        var listed = SlugRegex().Matches(index).Select(match => match.Groups["slug"].Value).ToArray();

        var resources = (await McpTestHost.Client.ListResourcesAsync())
                        .Where(resource => resource.Uri.StartsWith("blex://docs/", StringComparison.Ordinal))
                        .Select(resource => resource.Uri["blex://docs/".Length..])
                        .ToArray();

        var completions = (await McpTestHost.Client.CompleteAsync(
            new ModelContextProtocol.Protocol.ResourceTemplateReference { Uri = "blex://docs/{slug}" }, "slug", "")).Completion.Values;

        CollectionAssert.AreEquivalent(listed, resources);
        CollectionAssert.AreEquivalent(listed, completions.ToArray());
    }

    [TestMethod]
    public async Task Every_page_in_the_site_map_is_served_and_named_in_the_tool_description()
    {
        var description = (await McpTestHost.Client.ListToolsAsync()).Single(tool => tool.Name == "GetBlexDocsPage").Description!;

        foreach (var page in DocsPageRenderer.Pages)
        {
            StringAssert.Contains(description, $"'{page.Slug}'", $"'{page.Slug}' is a documentation page the tool's description never names.");
        }

        foreach (var quoted in QuotedSlugRegex().Matches(description).Select(match => match.Groups["slug"].Value))
        {
            Assert.IsNotNull(DocsPageRenderer.FindPage(quoted), $"The tool's description advertises the slug '{quoted}', which no page has.");
        }
    }

    [TestMethod]
    public async Task Every_source_file_the_setup_guides_quote_is_one_the_server_can_hand_out()
    {
        var paths = BlexSourceCatalog.SourceFiles.Select(file => file.Path).ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var model in new[] { "wasm", "standalone-wasm" })
        {
            var guide = await McpCall.TextAsync("GetBlexSetupGuide", new() { ["hostingModel"] = model });

            var quoted = QuotedPathRegex().Matches(guide).Select(match => match.Groups["path"].Value).ToArray();

            // A guide built from the catalog that suddenly quotes nothing has stopped finding its project.
            Assert.IsTrue(quoted.Length >= 4, $"The '{model}' guide quotes only {quoted.Length} files from the source catalog.");

            foreach (var path in quoted)
            {
                Assert.IsTrue(paths.Contains(path), $"The '{model}' guide quotes '{path}', which the source index does not list.");
            }
        }
    }

    [TestMethod]
    public async Task The_store_the_setup_guides_point_to_is_one_the_generator_accepts()
    {
        // The guides send the reader to it as the store that exercises everything; it has to be one.
        var source = await McpCall.TextAsync("GetBlexSourceFile", new() { ["path"] = "Demo/Client/Stores/CartStore.cs" });

        var result = await McpCall.StructuredAsync<BlexStoreInspectionDto>("InspectBlexStore", new() { ["source"] = source });

        Assert.IsTrue(result.Stores.Length > 0 && result.Stores.All(store => store.Generated),
            $"The generator rejects the demo's own store: {string.Join("; ", result.Diagnostics.Select(d => $"{d.Id} {d.Message}"))}");
    }

    // A suggested call, exactly as a hit spells it: GetBlexGuideSection(heading: "Persistence").
    [GeneratedRegex("""^(?<tool>\w+)\((?:(?<argument>\w+): "(?<value>[^"]*)")?\)$""")]
    private static partial Regex ToolCallRegex();

    [GeneratedRegex(@"^### `(?<path>[^`]+)`\r?$", RegexOptions.Multiline)]
    private static partial Regex QuotedPathRegex();

    [GeneratedRegex(@"^- `(?<slug>[^`]+)` - \*\*", RegexOptions.Multiline)]
    private static partial Regex SlugRegex();

    // Only a quoted word that continues a list - introduced by ':' or continued by ',' - is a slug.
    [GeneratedRegex(@"(?<=[:,] )'(?<slug>[a-z][a-z-]+)'")]
    private static partial Regex QuotedSlugRegex();
}
