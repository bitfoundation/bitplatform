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
        // Three separate code paths enumerate them: the tool, the resource listing in Program.cs and
        // the completion table. A page added to one and not the others is invisible in the rest.
        var listed = (await McpCall.StructuredAsync<BrouterDocsPageDto[]>("GetBrouterDocsList"))
                     .Select(page => page.Slug.Length == 0 ? "overview" : page.Slug)
                     .ToArray();

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
    public async Task Every_public_type_the_listing_names_has_a_reference_behind_it()
    {
        var types = await McpCall.StructuredAsync<BrouterApiTypeDto[]>("GetBrouterApiList");

        foreach (var type in types)
        {
            var result = await McpCall.StructuredAsync<BrouterApiDetailsResultDto>("GetBrouterApiDetails", new() { ["typeName"] = type.Name });

            Assert.IsNotNull(result.Details, $"'{type.Name}' is listed as a public type but has no reference: {result.Message}");
            Assert.AreEqual(type.Name, result.Details.Name);
            Assert.AreEqual(type.Kind, result.Details.Kind);
        }
    }

    [TestMethod]
    public async Task Every_constraint_the_server_documents_parses_in_a_real_template()
    {
        // The catalog is hand-written; the parser is the router's. A constraint documented with a
        // token the parser does not accept would send an agent to a template that throws on render.
        var constraints = await McpCall.StructuredAsync<BrouterConstraintDto[]>("GetBrouterRouteConstraints");

        foreach (var constraint in constraints)
        {
            var inspection = await McpCall.StructuredAsync<BrouterTemplateInspectionDto>(
                "InspectBrouterRouteTemplate", new() { ["template"] = $"/c/{constraint.Token.Split('(')[0]}/{{value:{constraint.Token}}}" });

            Assert.IsTrue(inspection.IsValid, $"The documented constraint '{constraint.Token}' does not parse: {inspection.Error}");
        }
    }

    [TestMethod]
    public async Task Every_constraint_the_server_documents_is_one_this_site_actually_demonstrates()
    {
        // The TryUrl points at a live route of this documentation site. The site declares one route
        // per catalog entry, so what has to hold is that the tool and the route table are still
        // reading the same catalog - and that the URL the tool builds fits the route the site
        // generates from it.
        var constraints = await McpCall.StructuredAsync<BrouterConstraintDto[]>("GetBrouterRouteConstraints");
        var routeTable = await McpCall.TextAsync("GetBrouterSourceFile", new() { ["path"] = "Demo/Client/AppRouter.razor" });

        StringAssert.Contains(routeTable, "ConstraintCatalog.All",
            "The demo no longer generates its constraint-tester routes from the catalog the tool answers from.");
        StringAssert.Contains(routeTable, "/c/{c.Kind}/",
            "The demo's constraint-tester route no longer has the shape the tool's TryUrl is built for.");

        var kinds = ConstraintCatalog.All.Select(constraint => constraint.Kind).ToArray();

        foreach (var constraint in constraints)
        {
            CollectionAssert.Contains(kinds, constraint.TryUrl.Split('/')[2],
                $"'{constraint.Token}' advertises {constraint.TryUrl}, which is not one of the site's constraint routes.");
        }
    }

    [TestMethod]
    public async Task Every_source_file_the_setup_guides_quote_is_one_the_server_can_hand_out()
    {
        var files = await McpCall.StructuredAsync<BrouterSourceFileDto[]>("GetBrouterSourceFiles");
        var paths = files.Select(file => file.Path).ToHashSet(StringComparer.OrdinalIgnoreCase);

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
                    $"The '{renderMode}' guide quotes '{quoted}', which GetBrouterSourceFiles does not list.");
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
}
