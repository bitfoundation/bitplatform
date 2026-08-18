using Bit.Brouter.Demo.Server.Dtos;

namespace Bit.Brouter.Tests.Mcp;

/// <summary>
/// The two tools that are not documentation: they run Brouter's own parser over a template an agent
/// is about to ship, and report what the router made of it.
/// <para>
/// Their value rests entirely on the answer being the router's rather than an opinion about it, and
/// the parser is internal - reached by reflection, with a deliberate "unavailable" answer if that
/// surface ever moves. Which makes the first assertion here the important one: an unavailable
/// parser is a successful tool call that quietly stops checking anything, and only a test that
/// looks at the verdict can tell the difference.
/// </para>
/// </summary>
[TestClass]
public class McpTemplateToolTests
{
    private static Task<BrouterTemplateInspectionDto> InspectAsync(string template)
        => McpCall.StructuredAsync<BrouterTemplateInspectionDto>("InspectBrouterRouteTemplate", new() { ["template"] = template });

    private static Task<BrouterRouteTableAnalysisDto> AnalyzeAsync(string templates)
        => McpCall.StructuredAsync<BrouterRouteTableAnalysisDto>("AnalyzeBrouterRouteTable", new() { ["templates"] = templates });

    [TestMethod]
    public async Task The_routers_own_parser_is_the_one_answering()
    {
        var inspection = await InspectAsync("/users/{id:int}");

        Assert.IsTrue(inspection.IsValid,
            $"A plainly valid template did not parse: {inspection.Error}. If this says the parser could not be reached, " +
            "the reflection into Bit.Brouter's internals has broken and both template tools are answering nothing.");

        Assert.AreEqual("users/{id:int}", inspection.NormalizedTemplate);
    }

    [TestMethod]
    public async Task A_parameter_segment_is_reported_with_its_name_and_constraint()
    {
        var inspection = await InspectAsync("/users/{id:int}");

        CollectionAssert.AreEqual(new[] { "id" }, inspection.ParameterNames);
        Assert.AreEqual(2, inspection.Segments!.Length);

        var literal = inspection.Segments[0];
        Assert.AreEqual("Literal", literal.Kind);
        Assert.AreEqual("users", literal.Value);

        var parameter = inspection.Segments[1];
        Assert.AreEqual("Parameter", parameter.Kind);
        CollectionAssert.AreEqual(new[] { "int" }, parameter.Constraints);
        Assert.IsFalse(parameter.IsOptional);
        Assert.IsNull(parameter.DefaultValue);

        // A literal is the most specific thing a segment can be, a parameter less so - that ordering
        // is what ranks two routes that both match.
        Assert.IsTrue(literal.Specificity > parameter.Specificity);
        Assert.AreEqual(inspection.Segments.Sum(segment => segment.Specificity), inspection.Specificity);
    }

    [TestMethod]
    public async Task A_complex_segment_reports_every_parameter_inside_it()
    {
        var inspection = await InspectAsync("/files/{name}.{ext?}");

        Assert.IsTrue(inspection.IsValid);
        CollectionAssert.AreEqual(new[] { "name", "ext" }, inspection.ParameterNames);

        var complex = inspection.Segments!.Last();
        Assert.AreEqual("Complex", complex.Kind);
        CollectionAssert.AreEqual(new[] { "name", "ext" }, complex.ParameterNames);

        Assert.IsTrue(inspection.Notes!.Any(note => note.Contains("right-to-left", StringComparison.Ordinal)),
            "A complex segment's matching order is exactly the surprise the notes exist for.");
    }

    [TestMethod]
    public async Task A_catch_all_is_reported_as_one_and_explained()
    {
        var inspection = await InspectAsync("/assets/{*path:nonfile}");

        Assert.IsTrue(inspection.IsValid);

        var catchAll = inspection.Segments!.Last();
        Assert.AreEqual("CatchAll", catchAll.Kind);
        CollectionAssert.AreEqual(new[] { "path" }, catchAll.ParameterNames);
        CollectionAssert.AreEqual(new[] { "nonfile" }, catchAll.Constraints);

        Assert.IsTrue(inspection.Notes!.Any(note => note.Contains("must be the last segment", StringComparison.Ordinal)));
    }

    [TestMethod]
    public async Task A_default_value_is_reported_together_with_what_it_binds()
    {
        var inspection = await InspectAsync("/blog/{action=Index}");

        var parameter = inspection.Segments!.Last();
        Assert.AreEqual("Index", parameter.DefaultValue);

        Assert.IsTrue(inspection.Notes!.Any(note => note.Contains("binds \"Index\"", StringComparison.Ordinal)),
            "The note that says what a default actually does is missing.");
    }

    [TestMethod]
    public async Task An_optional_parameter_that_is_not_last_is_called_out()
    {
        // It parses, and then never matches the shorter URL its author expected - the kind of bug
        // that produces a 404 rather than an error.
        var inspection = await InspectAsync("/{a?}/{b}");

        Assert.IsTrue(inspection.IsValid);
        Assert.IsTrue(inspection.Notes!.Any(note => note.Contains("matches as required", StringComparison.Ordinal)),
            "A middle optional was not flagged.");
    }

    [TestMethod]
    public async Task A_chained_constraint_is_reported_in_evaluation_order_and_explained()
    {
        var inspection = await InspectAsync("/c/chain/{v:int:min(1):max(5)}");

        var parameter = inspection.Segments!.Last();
        CollectionAssert.AreEqual(new[] { "int", "min(1)", "max(5)" }, parameter.Constraints);

        Assert.IsTrue(inspection.Notes!.Any(note => note.Contains("last TYPE constraint", StringComparison.Ordinal)));
    }

    [TestMethod]
    public async Task A_custom_constraint_registered_by_the_app_resolves_like_a_built_in_one()
    {
        // The tool inspects with the running app's own constraint registry, so the demo's "slug"
        // constraint has to parse here exactly as it does at run time.
        var inspection = await InspectAsync("/p/{value:slug}");

        Assert.IsTrue(inspection.IsValid, $"The app's custom 'slug' constraint did not resolve: {inspection.Error}");
        CollectionAssert.AreEqual(new[] { "slug" }, inspection.Segments!.Last().Constraints);
    }

    [TestMethod]
    public async Task An_unknown_constraint_comes_back_with_the_error_the_router_would_throw()
    {
        var inspection = await InspectAsync("/p/{value:nosuchconstraint}");

        Assert.IsFalse(inspection.IsValid);
        Assert.IsNotNull(inspection.Error);
        StringAssert.Contains(inspection.Error, "nosuchconstraint");
    }

    [TestMethod]
    public async Task An_invalid_template_comes_back_with_the_exact_parser_error()
    {
        (string Template, string Expected)[] invalid =
        [
            ("/users/{id", "Missing '}'"),
            ("/users/{}", "empty"),
            ("/{*rest}/more", "catch-all"),
        ];

        foreach (var (template, expected) in invalid)
        {
            var inspection = await InspectAsync(template);

            Assert.IsFalse(inspection.IsValid, $"'{template}' was reported as valid.");
            Assert.IsNotNull(inspection.Error);
            StringAssert.Contains(inspection.Error.ToLowerInvariant(), expected.ToLowerInvariant(),
                $"'{template}' came back with an unhelpful error: {inspection.Error}");
        }
    }

    [TestMethod]
    public async Task The_index_route_is_a_valid_template_rather_than_an_error()
    {
        // "" is what a child route declares to be its parent's index; refusing it would send an agent
        // looking for a syntax it does not need.
        var inspection = await InspectAsync("");

        Assert.IsTrue(inspection.IsValid);
        Assert.AreEqual(0, inspection.Segments!.Length);
    }

    [TestMethod]
    public async Task A_route_table_is_ranked_the_way_the_router_prefers_its_routes()
    {
        var analysis = await AnalyzeAsync("""
            /users/{id}
            /users/new
            /users/{id:int}
            /{*path}
            """);

        Assert.AreEqual(4, analysis.Routes.Length);
        Assert.IsTrue(analysis.Routes.All(route => route.IsValid));

        // Most specific first: a literal beats a constrained parameter, which beats a bare one,
        // which beats a catch-all.
        CollectionAssert.AreEqual(
            new[] { "/users/new", "/users/{id:int}", "/users/{id}", "/{*path}" },
            analysis.Routes.Select(route => route.Template).ToArray());

        CollectionAssert.AreEqual(new[] { 1, 2, 3, 4 }, analysis.Routes.Select(route => route.MatchOrder).ToArray());
    }

    [TestMethod]
    public async Task Templates_that_match_the_same_urls_are_reported_as_ambiguous()
    {
        var analysis = await AnalyzeAsync("/users/{id}\n/users/{userId}");

        Assert.AreEqual(1, analysis.Ambiguous.Length);
        CollectionAssert.AreEquivalent(new[] { "/users/{id}", "/users/{userId}" }, analysis.Ambiguous[0]);

        Assert.IsTrue(analysis.Notes.Any(note => note.Contains("registration order", StringComparison.Ordinal)),
            "The analysis reports the collision without saying what the router does about it.");
    }

    [TestMethod]
    public async Task Templates_that_bind_different_values_for_the_same_url_are_not_ambiguous()
    {
        // "{page}" and "{page=1}" accept the same URLs, and bind differently when the segment is
        // absent - the router keeps them apart, so this must not report them as a collision.
        var analysis = await AnalyzeAsync("/x/{page}\n/x/{page=1}");

        Assert.AreEqual(0, analysis.Ambiguous.Length);
    }

    [TestMethod]
    public async Task An_invalid_template_in_a_table_is_reported_without_taking_the_rest_down()
    {
        var analysis = await AnalyzeAsync("/ok\n/broken/{id\n/also-ok");

        Assert.AreEqual(3, analysis.Routes.Length);
        Assert.AreEqual(2, analysis.Routes.Count(route => route.IsValid));

        var broken = analysis.Routes.Single(route => route.IsValid is false);
        Assert.AreEqual("/broken/{id", broken.Template);
        Assert.IsNotNull(broken.Error);

        // Sorted last: a route that cannot register never wins anything.
        Assert.AreEqual(3, broken.MatchOrder);

        Assert.IsTrue(analysis.Notes.Any(note => note.Contains("never registers", StringComparison.Ordinal)));
    }

    [TestMethod]
    public async Task A_pasted_route_table_is_split_on_the_separators_a_person_would_use()
    {
        // A comma inside range(1,10) is part of the grammar, not a separator - splitting there would
        // tear one valid template into two invalid ones and blame the caller for it.
        var analysis = await AnalyzeAsync("/a/{id:range(1,10)}; /b/{code:length(2,4)}, /c\n/d");

        CollectionAssert.AreEquivalent(
            new[] { "/a/{id:range(1,10)}", "/b/{code:length(2,4)}", "/c", "/d" },
            analysis.Routes.Select(route => route.Template).ToArray());

        Assert.IsTrue(analysis.Routes.All(route => route.IsValid));
    }

    [TestMethod]
    public async Task An_empty_route_table_answers_with_the_caveat_rather_than_with_nothing()
    {
        var analysis = await AnalyzeAsync("   \n  \n");

        Assert.AreEqual(0, analysis.Routes.Length);
        Assert.AreEqual(0, analysis.Ambiguous.Length);

        // The standing caveat: specificity ranks routes that all match, it does not decide matching.
        Assert.IsTrue(analysis.Notes.Length > 0);
    }

    [TestMethod]
    public async Task A_file_pasted_in_by_mistake_is_answered_about_rather_than_parsed_whole()
    {
        var pasted = string.Join('\n', Enumerable.Range(0, 500).Select(index => $"/route{index}/{{id:int}}"));

        var analysis = await AnalyzeAsync(pasted);

        Assert.AreEqual(200, analysis.Routes.Length, "The number of analyzed templates is capped at 200.");
    }

    [TestMethod]
    public async Task The_whole_route_table_of_this_site_analyzes_clean()
    {
        // The strongest end-to-end statement available: the demo's own routes are a real, working
        // table this very server is routed by. If the analyzer calls any of them invalid or
        // ambiguous, it is the analyzer that is wrong - the app is running.
        var routeTable = await McpCall.TextAsync("GetBrouterSourceFile", new() { ["path"] = "Demo/Client/AppRouter.razor" });

        var templates = System.Text.RegularExpressions.Regex.Matches(routeTable, "Path=\"(?<path>[^\"]*)\"")
                              .Select(match => match.Groups["path"].Value)
                              .Where(path => path.Length > 0)
                              .Distinct(StringComparer.Ordinal)
                              .ToArray();

        Assert.IsTrue(templates.Length > 30, $"Only {templates.Length} route templates were found in the demo's route table.");

        var analysis = await AnalyzeAsync(string.Join('\n', templates));

        var invalid = analysis.Routes.Where(route => route.IsValid is false).ToArray();

        Assert.AreEqual(0, invalid.Length,
            $"Templates the running site routes by were reported invalid: {string.Join(", ", invalid.Select(route => $"'{route.Template}' ({route.Error})"))}");
    }
}
