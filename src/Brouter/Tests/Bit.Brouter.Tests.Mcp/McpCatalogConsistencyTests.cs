using System.Text.RegularExpressions;
using Bit.Brouter.Demo.Client;
using Bit.Brouter.Demo.Server.Dtos;

namespace Bit.Brouter.Tests.Mcp;

/// <summary>
/// The joins between the catalogs - the places where one tool hands a caller a value that another
/// tool has to accept.
/// <para>
/// Each catalog is separately correct and separately tested; what breaks in practice is the seam
/// between two of them, because nothing in the type system connects a search hit's suggested call
/// to the tool that would serve it, or a listed slug to the page behind it. A heading renamed in
/// the README, a docs page removed, a tool renamed: each leaves every individual tool working and
/// the path between them broken. These tests walk those paths.
/// </para>
/// </summary>
[TestClass]
public partial class McpCatalogConsistencyTests
{
    /// <summary>The answers that mean "the key you were handed does not resolve".</summary>
    private static readonly string[] _apologies =
    [
        "No documentation page has the slug",
        "has no section called",
        "has no public type called",
        "No source file at",
        "is not a known render mode",
        "could not be rendered",
    ];

    [TestMethod]
    public async Task Every_call_a_search_hit_suggests_is_one_this_server_answers()
    {
        // A hit's whole job is to name the follow-up call. Every distinct suggestion across a broad
        // set of queries is parsed and made, and its answer has to be the material - not an apology
        // about a key that no longer exists.
        string[] queries =
        [
            "guard", "loader", "navigation", "route template", "constraint", "outlet", "query",
            "transition", "keep alive", "typed routes", "discovery", "scroll", "error", "history",
        ];

        var suggestions = new SortedSet<string>(StringComparer.Ordinal);

        foreach (var query in queries)
        {
            var result = await McpCall.StructuredAsync<BrouterSearchResultDto>("SearchBrouter", new() { ["query"] = query, ["limit"] = 20 });

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
        // Three separate code paths enumerate them: the tool's own index, the resource listing in
        // Program.cs and the completion table. A page added to one and not the others is invisible
        // in the rest.
        var index = await McpCall.TextAsync("GetBrouterDocsPage");

        var listed = SlugRegex().Matches(index).Select(match => match.Groups["slug"].Value).ToArray();

        var resources = (await McpTestHost.Client.ListResourcesAsync())
                        .Where(resource => resource.Uri.StartsWith("brouter://docs/", StringComparison.Ordinal))
                        .Select(resource => resource.Uri["brouter://docs/".Length..])
                        .ToArray();

        var completions = (await McpTestHost.Client.CompleteAsync(
            new ModelContextProtocol.Protocol.ResourceTemplateReference { Uri = "brouter://docs/{slug}" }, "slug", "")).Completion.Values;

        CollectionAssert.AreEquivalent(listed, resources);
        CollectionAssert.AreEquivalent(listed, completions.ToArray());
    }

    [TestMethod]
    public async Task Every_slug_the_docs_tool_advertises_in_its_own_description_is_a_page()
    {
        // The description spells the slugs out so a model can pick one without spending a call on the
        // index first - which only pays if they are all real, and if the ones worth naming are named.
        var description = (await McpTestHost.Client.ListToolsAsync())
                          .Single(tool => tool.Name == "GetBrouterDocsPage").Description!;

        var known = DocsCatalog.AllPages.Select(page => page.Slug.Length == 0 ? "overview" : page.Slug).ToArray();

        foreach (var slug in known)
        {
            StringAssert.Contains(description, slug, $"'{slug}' is a documentation page the tool's description never names.");
        }

        // And nothing it names is a page that has since been renamed away.
        foreach (var quoted in QuotedSlugRegex().Matches(description).Select(match => match.Groups["slug"].Value))
        {
            CollectionAssert.Contains(known, quoted, $"The tool's description advertises the slug '{quoted}', which no page has.");
        }
    }

    [TestMethod]
    public async Task Every_public_type_the_index_names_has_a_reference_behind_it()
    {
        var index = await McpCall.TextAsync("GetBrouterApi");

        var listed = IndexEntryRegex().Matches(index)
                                      .Select(match => (Name: match.Groups["name"].Value, Kind: match.Groups["kind"].Value))
                                      .ToArray();

        Assert.IsTrue(listed.Length > 30, $"Only {listed.Length} types were found in the API index.");

        foreach (var (name, kind) in listed)
        {
            var reference = await McpCall.TextAsync("GetBrouterApi", new() { ["typeName"] = name });

            StringAssert.StartsWith(reference, $"# {name} ({kind})",
                $"'{name}' is listed as a public type but its reference does not answer under that name and kind.");
        }
    }

    [TestMethod]
    public async Task Every_constraint_the_server_documents_parses_in_a_real_template()
    {
        // The catalog is hand-written; the parser is the router's. A constraint documented with a
        // token the parser does not accept would send an agent to a template that throws on render.
        var table = await McpCall.TextAsync("GetBrouterRouteConstraints");

        foreach (var constraint in ConstraintCatalog.All)
        {
            StringAssert.Contains(table, $"`{{value:{constraint.Token}}}`", $"'{constraint.Token}' is not in the documented table.");

            var analysis = await McpCall.StructuredAsync<BrouterRouteAnalysisDto>(
                "InspectBrouterRouteTemplates", new() { ["templates"] = $"/c/{constraint.Token.Split('(')[0]}/{{value:{constraint.Token}}}" });

            var inspection = analysis.Routes.Single();

            Assert.IsTrue(inspection.IsValid, $"The documented constraint '{constraint.Token}' does not parse: {inspection.Error}");
        }
    }

    [TestMethod]
    public async Task Every_constraint_the_server_documents_is_one_this_site_actually_demonstrates()
    {
        // The documented table and the site's constraint-tester routes come from one catalog, and
        // that is the whole claim: a constraint cannot be documented here without the running site
        // demonstrating it. What has to keep holding is that the route table still reads it.
        var table = await McpCall.TextAsync("GetBrouterRouteConstraints");
        var routeTable = await McpCall.TextAsync("GetBrouterSourceFile", new() { ["path"] = "Demo/Client/AppRouter.razor" });

        StringAssert.Contains(routeTable, "ConstraintCatalog.All",
            "The demo no longer generates its constraint-tester routes from the catalog the tool answers from.");
        StringAssert.Contains(routeTable, "/c/{c.Kind}/",
            "The demo's constraint-tester route no longer has the shape the catalog's entries are built for.");

        foreach (var constraint in ConstraintCatalog.All)
        {
            StringAssert.Contains(table, $"| `{{value:{constraint.Token}}}` | {constraint.Category} |",
                $"'{constraint.Token}' is documented under a category the catalog does not give it.");
        }
    }

    [TestMethod]
    public async Task Every_source_file_the_setup_guides_quote_is_one_the_server_can_hand_out()
    {
        var index = await McpCall.TextAsync("GetBrouterSourceFile");

        var paths = SourcePathRegex().Matches(index)
                                     .Select(match => match.Groups["path"].Value)
                                     .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var renderMode in Bit.Brouter.Demo.Server.Services.BrouterSetupGuide.RenderModes)
        {
            var guide = await McpCall.TextAsync("GetBrouterSetupGuide", new() { ["renderMode"] = renderMode });

            var quotedPaths = 0;

            foreach (var quoted in QuotedPathRegex().Matches(guide).Select(match => match.Groups["path"].Value))
            {
                // Samples/ has no standalone WebAssembly project, so that one guide writes its files
                // out by hand and heads each with a bare file name. Only a heading naming a path is
                // a claim that the server can hand that file over.
                if (quoted.Contains('/', StringComparison.Ordinal) is false) continue;

                quotedPaths++;

                Assert.IsTrue(paths.Contains(quoted),
                    $"The '{renderMode}' guide quotes '{quoted}', which the source index does not list.");
            }

            // A guide built from the catalog that suddenly quotes nothing has stopped finding its
            // sample, which would leave the loop above asserting nothing at all.
            if (renderMode is not "standalone-wasm")
            {
                Assert.IsTrue(quotedPaths > 0, $"The '{renderMode}' guide no longer hands out any file from the source catalog.");
            }
        }
    }

    [TestMethod]
    public async Task Every_docs_page_the_site_declares_renders_through_the_tool()
    {
        // The pages are rendered by the very components the site serves, outside the app's own router
        // and layout. A page that reaches for the router or for JS interop while initializing throws
        // there and only there - so this is the test that catches it.
        foreach (var page in DocsCatalog.AllPages)
        {
            var slug = page.Slug.Length == 0 ? "overview" : page.Slug;

            var markdown = await McpCall.TextAsync("GetBrouterDocsPage", new() { ["slug"] = slug });

            Assert.IsFalse(markdown.Contains("could not be rendered", StringComparison.Ordinal),
                $"The '{page.Title}' page did not render: {markdown[..Math.Min(400, markdown.Length)]}");

            Assert.IsTrue(markdown.Length > 500, $"The '{page.Title}' page came back as {markdown.Length} characters, which is not a page.");
            StringAssert.Contains(markdown, "# ", $"The '{page.Title}' page came back with no heading at all.");
        }
    }

    // A suggested call, exactly as a hit spells it: GetBrouterGuideSection(heading: "Async guards").
    [GeneratedRegex("""^(?<tool>\w+)\((?:(?<argument>\w+): "(?<value>[^"]*)")?\)$""")]
    private static partial Regex ToolCallRegex();

    // The setup guide introduces each file it quotes as a "### `path`" heading.
    [GeneratedRegex(@"^### `(?<path>[^`]+)`\r?$", RegexOptions.Multiline)]
    private static partial Regex QuotedPathRegex();

    // The docs index lists a page as "- `slug` - **Title**: description".
    [GeneratedRegex(@"^- `(?<slug>[^`]+)` - \*\*", RegexOptions.Multiline)]
    private static partial Regex SlugRegex();

    // The API index lists a type as "- **Name** (Kind) - summary".
    [GeneratedRegex(@"^- \*\*(?<name>[^*]+)\*\* \((?<kind>[^)]+)\)", RegexOptions.Multiline)]
    private static partial Regex IndexEntryRegex();

    // The docs tool's description quotes the slugs it advertises: 'faq', 'recipes', ...
    [GeneratedRegex(@"'(?<slug>[a-z][a-z-]+)'")]
    private static partial Regex QuotedSlugRegex();

    // The source index lists a file as "- `path` (n lines) - description".
    [GeneratedRegex(@"^- `(?<path>[^`]+)` \(\d+ lines\)", RegexOptions.Multiline)]
    private static partial Regex SourcePathRegex();
}
