using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using Bit.Balette.Demo.Client.Services;
using Bit.Balette.Demo.Server.Dtos;
using Bit.Balette.Demo.Server.Services;
using Microsoft.JSInterop;

namespace Bit.Balette.Tests.Mcp;

/// <summary>
/// The server's claims put to the things they describe: the search lab against the matcher and the
/// ranker it runs, the demo registry against the demo's own commands, the theme tool against the
/// stylesheet and markup it was read from, the API index against the assembly, the guide index
/// against the README, and the HTTP mirror against the protocol it mirrors. No expected answers are
/// written down here - the library, its files and the protocol are the expectations.
/// </summary>
[TestClass]
public partial class McpOracleTests
{
    private static readonly string[] _words =
    [
        "Open", "Go", "to", "Settings", "Toggle", "dark", "mode", "Rename", "project", "Delete", "workspace",
        "Copy", "share", "link", "Export", "CSV", "Show", "minimap", "Word", "wrap", "Assign", "Keyboard", "shortcuts"
    ];

    [TestMethod]
    public async Task The_search_lab_agrees_with_the_matcher_and_the_ranker()
    {
        var random = new Random(20261017);
        var options = new BaletteOptions();
        var ranker = new BaletteRanker(options);

        for (var round = 0; round < 12; round++)
        {
            var commands = Enumerable.Range(0, random.Next(5, 25)).Select(i => new
            {
                title = Phrase(random, 1, 4),
                subtitle = random.Next(3) == 0 ? Phrase(random, 1, 3) : null,
                keywords = random.Next(2) == 0 ? new[] { Phrase(random, 1, 1).ToLowerInvariant() } : [],
                group = random.Next(4) == 0 ? null : _words[random.Next(_words.Length)],
                priority = random.Next(4) == 0 ? random.Next(-100, 101) : 0,
            }).ToArray();

            var query = string.Join(' ', Enumerable.Range(0, random.Next(1, 3)).Select(_ => Term(random)));
            var context = $"round {round}: '{query}'";

            var report = await McpSearchLabTests.InspectAsync(query, JsonSerializer.Serialize(commands), limit: 200);

            // What the palette computes for the same commands and query, called directly.
            var built = commands.Select(c => BaletteCmd.New(c.title).Subtitle(c.subtitle).Group(c.group).Keywords(c.keywords).Priority(c.priority).Build()).ToArray();
            var matches = built.Select(command => BaletteFuzzyMatcher.Default.Match(command, query, options.Weights)).OfType<BaletteMatch>().ToList();
            var ranked = ranker.Rank(matches, query.Trim().Length > 0, _ => 0);

            Assert.AreEqual(built.Length, report.Searched, context);
            Assert.AreEqual(ranked.Count, report.Matched, context);

            var byOrder = report.Results.Where(result => result.Matched).OrderBy(result => result.Order).ToArray();

            Assert.HasCount(ranked.Count, byOrder, context);

            for (var i = 0; i < ranked.Count; i++)
            {
                Assert.AreEqual(ranked[i].Command.Title, byOrder[i].Title, $"{context}, place {i + 1}");
                Assert.AreEqual(ranked[i].Score, byOrder[i].Score!.Value, 0.001, $"{context}: score of '{byOrder[i].Title}'");
                Assert.AreEqual(ranked[i].Rank, byOrder[i].Rank!.Value, 0.001, $"{context}: rank of '{byOrder[i].Title}'");
                CollectionAssert.AreEqual(ranked[i].TitlePositions, byOrder[i].TitlePositions ?? [], $"{context}: title positions of '{byOrder[i].Title}'");
                CollectionAssert.AreEqual(ranked[i].SubtitlePositions, byOrder[i].SubtitlePositions ?? [], $"{context}: subtitle positions of '{byOrder[i].Title}'");
                Assert.AreEqual(ranked[i].MatchedKeyword, byOrder[i].MatchedKeyword, context);
            }

            // Every command the library does not match is reported as such, with the reason why.
            var unmatched = report.Results.Where(result => result.Matched is false).ToArray();

            Assert.HasCount(built.Length - matches.Count, unmatched, context);
            foreach (var result in unmatched) Assert.IsFalse(string.IsNullOrWhiteSpace(result.Reason), $"{context}: '{result.Title}' has no reason.");
        }
    }

    [TestMethod]
    public void The_demo_registry_is_the_commands_the_demos_layout_registers()
    {
        var state = new DemoState(new NoBrowser());
        var roots = DemoCommands.Global(state).Where(command => command.Parent is null).Select(command => command.Id).ToArray();

        CollectionAssert.AreEquivalent(roots, BaletteSearchLab.DemoRegistry.Select(command => command.Id).ToArray());

        // Nested pages are flattened into the registry with a parent, so none of their children is a root candidate.
        Assert.IsFalse(BaletteSearchLab.DemoRegistry.Any(command => command.Id.StartsWith("theme:", StringComparison.Ordinal)));
        Assert.IsGreaterThan(0, BaletteSearchLab.DemoOptions.Modes.Count, "The demo's own options, modes included, should be the ones the lab uses.");
    }

    [TestMethod]
    public void Every_variable_of_the_stylesheet_root_rule_is_in_the_theme_catalog()
    {
        // From the start of a line: the stylesheet's banner comment shows a ":root {" example of its own.
        var root = RuleBody("\n:root {");

        var declared = DeclarationRegex().Matches(root).Select(match => match.Groups["name"].Value).ToArray();

        Assert.IsGreaterThan(20, declared.Length);
        CollectionAssert.AreEqual(declared, BaletteThemeCatalog.Variables.Select(variable => variable.Name).ToArray());
    }

    [TestMethod]
    public void The_three_dark_and_light_rules_of_the_stylesheet_agree()
    {
        // The dark values are written twice - for the system preference and for data-theme="dark" - and
        // the light ones twice, for :root and for a dialog forced to light on a dark page. A palette that
        // looks different depending on which of them applied is a stylesheet bug.
        var light = Declarations(RuleBody("\n:root {"));
        var dark = Declarations(RuleBody(":root[data-theme=\"dark\"],"));
        var system = Declarations(RuleBody(":root:not([data-theme=\"light\"]) {"));
        var forcedLight = Declarations(RuleBody("dialog[data-balette=\"dialog\"][data-theme=\"light\"] {"));

        CollectionAssert.AreEquivalent(dark.ToArray(), system.ToArray(), "The prefers-color-scheme block and the data-theme=\"dark\" block differ.");

        foreach (var (name, value) in forcedLight)
        {
            Assert.AreEqual(light[name], value, $"{name} differs between :root and the dialog forced to light.");
        }

        foreach (var variable in BaletteThemeCatalog.Variables)
        {
            Assert.AreEqual(light[variable.Name], variable.LightValue, variable.Name);
            Assert.AreEqual(dark.TryGetValue(variable.Name, out var value) && value != variable.LightValue ? value : null, variable.DarkValue, variable.Name);
        }

        var accent = BaletteThemeCatalog.Variables.Single(variable => variable.Name == "--bit-balette-accent");
        Assert.AreEqual("#4f46e5", accent.LightValue);
        Assert.IsNotNull(accent.DarkValue);
    }

    [TestMethod]
    public void Every_hook_the_markup_renders_is_in_the_theme_catalog_and_the_guide_names_only_real_ones()
    {
        var rendered = BaletteSourceCatalog.ComponentMarkup
            .SelectMany(file => HookRegex().Matches(file.Markup).Select(match => match.Groups["value"].Value))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        var catalogued = BaletteThemeCatalog.Hooks.Select(hook => hook.Value).ToArray();

        Assert.IsGreaterThan(15, rendered.Length);
        CollectionAssert.AreEquivalent(rendered, catalogued);

        // The guide's own list of hooks, in its "Unstyled" section, may leave some out - it may not name one
        // the palette does not render.
        var unstyled = BaletteSourceCatalog.GetGuideSection("Unstyled")!;
        var from = unstyled.IndexOf("`data-balette` attributes", StringComparison.Ordinal);
        var list = unstyled[(from + "`data-balette` attributes".Length)..unstyled.IndexOf("state attributes", from, StringComparison.Ordinal)];

        foreach (var hook in GuideHookRegex().Matches(list).Select(match => match.Groups["value"].Value))
        {
            CollectionAssert.Contains(catalogued, hook, $"The guide names the hook '{hook}', which no shipped component renders.");
        }
    }

    [TestMethod]
    public void Every_exported_type_is_in_the_api_index()
    {
        var exported = typeof(BaletteOptions).Assembly.GetExportedTypes()
            .Where(type => type.IsNested is false && BaletteApiCatalog.IsRazorImports(type) is false)
            .Select(type => BaletteApiCatalog.FriendlyName(type))
            .ToHashSet(StringComparer.Ordinal);

        var indexed = BaletteApiCatalog.Types.Select(type => type.Name).ToHashSet(StringComparer.Ordinal);

        CollectionAssert.AreEquivalent(exported.ToArray(), indexed.ToArray());
    }

    [TestMethod]
    public void The_xml_documentation_reaches_the_server()
    {
        // The XML documentation file has to reach the server's output folder for any of this to work;
        // a build that stops copying it leaves every summary empty without failing anything else. Every
        // public type carries one - the Razor components through a partial class next to their markup - so
        // a type that has none is a gap in what an agent is told, and is named here.
        var withoutSummary = BaletteApiCatalog.Types.Where(type => type.Summary is null).Select(type => type.Name).ToArray();

        Assert.IsEmpty(withoutSummary, $"Public types without an XML summary: {string.Join(", ", withoutSummary)}.");
        Assert.IsNotNull(BaletteApiCatalog.Types.Single(type => type.Name == "BaletteOptions").Summary);
    }

    [TestMethod]
    public void Every_class_of_the_stylesheet_is_a_search_entry()
    {
        var css = BaletteSourceCatalog.Stylesheet;

        Assert.IsFalse(string.IsNullOrEmpty(css), "The shipped stylesheet is not embedded.");

        var classes = ClassRegex().Matches(css).Select(match => match.Value).Distinct(StringComparer.Ordinal).ToArray();
        var selectors = string.Join(' ', BaletteStylesheetClasses.Classes.Select(rule => rule.Selector));

        Assert.IsGreaterThan(20, classes.Length);
        foreach (var name in classes) Assert.Contains(name, selectors);
    }

    [TestMethod]
    public void The_guide_sections_cover_every_heading_of_the_readme()
    {
        var readme = BaletteSourceCatalog.Readme.Replace("\r\n", "\n", StringComparison.Ordinal);

        Assert.IsFalse(string.IsNullOrEmpty(readme), "The README is not embedded.");

        var fenced = false;
        var headings = new List<string>();

        foreach (var line in readme.Split('\n'))
        {
            if (line.TrimStart().StartsWith("```", StringComparison.Ordinal)) { fenced = fenced is false; continue; }
            if (fenced is false && HeadingRegex().Match(line) is { Success: true } match) headings.Add(match.Groups["text"].Value.Trim());
        }

        CollectionAssert.AreEqual(headings, BaletteSourceCatalog.GuideSections.Select(section => section.Heading).ToArray());
        Assert.HasCount(BaletteSourceCatalog.GuideSections.Length, BaletteSourceCatalog.GuideSections.Select(section => section.Key).Distinct(StringComparer.Ordinal), "Every section needs a key of its own.");
    }

    [TestMethod]
    public async Task Every_tool_is_reachable_as_a_plain_get_with_the_same_answer()
    {
        using var http = McpTestHost.CreateHttpClient();

        (string Tool, string Query, Dictionary<string, object?>? Arguments)[] calls =
        [
            ("SearchBalette", "?query=prompt&limit=3", new() { ["query"] = "prompt", ["limit"] = 3 }),
            ("GetBaletteSetupGuide", "?renderMode=server", new() { ["renderMode"] = "server" }),
            ("GetBaletteGuideSection", "?heading=Modes", new() { ["heading"] = "Modes" }),
            ("GetBaletteApi", "?typeName=BaletteCommand", new() { ["typeName"] = "BaletteCommand" }),
            ("GetBaletteThemeVariables", "", null),
            ("InspectBaletteSearch", "?query=rc", new() { ["query"] = "rc" }),
            ("GetBaletteSourceFile", "?path=Demo/Client/Services/DemoCommands.cs", new() { ["path"] = "Demo/Client/Services/DemoCommands.cs" }),
        ];

        CollectionAssert.AreEquivalent(McpSurfaceTests.ExpectedTools, calls.Select(call => call.Tool).ToArray(),
            "The HTTP mirror is being exercised for a different set of tools than the server publishes.");

        foreach (var (tool, query, arguments) in calls)
        {
            using var response = await http.GetAsync($"/api/mcp/{tool}{query}");

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, tool);

            var body = await response.Content.ReadAsStringAsync();

            if (tool is "SearchBalette" or "InspectBaletteSearch")
            {
                Assert.StartsWith("{", body, tool);
                continue;
            }

            Assert.AreEqual(await McpCall.TextAsync(tool, arguments), body, tool);
        }
    }

    [TestMethod]
    public async Task The_search_lab_answers_the_same_over_http_and_over_the_protocol()
    {
        using var http = McpTestHost.CreateHttpClient();

        var overHttp = JsonSerializer.Deserialize<BaletteSearchInspectionDto>(await http.GetStringAsync("/api/mcp/InspectBaletteSearch?query=rc"), new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
        var overMcp = await McpSearchLabTests.InspectAsync("rc");

        CollectionAssert.AreEqual(overMcp.Results.Select(result => result.Id).ToArray(), overHttp.Results.Select(result => result.Id).ToArray());
        CollectionAssert.AreEqual(overMcp.Notes, overHttp.Notes);
    }

    [TestMethod]
    public async Task The_demos_mcp_page_lists_this_server_and_its_links_answer()
    {
        // The page that tells people what this server offers is read from the repository, not
        // embedded: it is excluded from the source files on purpose. Out of the repository it has
        // nothing to be held to.
        var page = FindDemoFile("Client", "Pages", "McpServer.razor");
        if (page is null) Assert.Inconclusive("The demo's McpServer.razor is not next to this build.");

        var markup = await File.ReadAllTextAsync(page);

        foreach (var tool in McpSurfaceTests.ExpectedTools) Assert.Contains($"<code>{tool}</code>", markup, tool);
        foreach (var prompt in new[] { "add-balette-to-app", "implement-balette-feature", "debug-balette-palette" }) Assert.Contains(prompt, markup, prompt);

        using var http = McpTestHost.CreateHttpClient();

        var links = PageLinkRegex().Matches(markup).Select(match => WebUtility.HtmlDecode(match.Groups["href"].Value)).Distinct().ToArray();

        Assert.IsNotEmpty(links);

        foreach (var link in links)
        {
            using var response = await http.GetAsync(link);

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, link);

            var body = await response.Content.ReadAsStringAsync();

            Assert.DoesNotContain("has no public type called", body, link);
            Assert.DoesNotContain("No source file at", body, link);
        }
    }

    private static string? FindDemoFile(params string[] parts)
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            var candidate = Path.Combine([directory.FullName, "Bit.Balette.Demo", .. parts]);
            if (File.Exists(candidate)) return candidate;
        }

        return null;
    }

    private static string RuleBody(string opening)
    {
        var css = BaletteSourceCatalog.Stylesheet.Replace("\r\n", "\n", StringComparison.Ordinal);

        var start = css.IndexOf(opening, StringComparison.Ordinal);
        Assert.IsGreaterThanOrEqualTo(0, start, $"The stylesheet has no rule opening with '{opening}'.");

        var open = css.IndexOf('{', start);
        var close = css.IndexOf('}', open);

        return css[(open + 1)..close];
    }

    private static Dictionary<string, string> Declarations(string body)
        => DeclarationRegex().Matches(body).ToDictionary(match => match.Groups["name"].Value, match => match.Groups["value"].Value.Trim(), StringComparer.Ordinal);

    private static string Phrase(Random random, int min, int max)
        => string.Join(' ', Enumerable.Range(0, random.Next(min, max + 1)).Select(_ => _words[random.Next(_words.Length)]));

    /// <summary>A query term: mostly a fuzzy fragment of a word, sometimes with an operator in front.</summary>
    private static string Term(Random random)
    {
        var word = _words[random.Next(_words.Length)];
        var length = Math.Min(word.Length, random.Next(1, 4));
        var fragment = random.Next(3) == 0 ? word[..length] : word[..length].ToLowerInvariant();

        return random.Next(6) switch
        {
            0 => $"!{fragment.ToLowerInvariant()}",
            1 => $"^{fragment.ToLowerInvariant()}",
            2 => $"'{fragment.ToLowerInvariant()}",
            _ => fragment
        };
    }

    /// <summary>DemoState's JavaScript runtime, which no command built here ever calls.</summary>
    private sealed class NoBrowser : IJSRuntime
    {
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) => throw new InvalidOperationException();

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) => throw new InvalidOperationException();
    }

    [GeneratedRegex(@"(?m)^\s*(?<name>--bit-balette-[a-z0-9-]+)\s*:\s*(?<value>[^;]+);")]
    private static partial Regex DeclarationRegex();

    [GeneratedRegex(@"\bdata-balette=""(?<value>[a-z-]+)""")]
    private static partial Regex HookRegex();

    [GeneratedRegex(@"`(?<value>[a-z-]+)`")]
    private static partial Regex GuideHookRegex();

    [GeneratedRegex(@"\.bit-balette-[a-z0-9-]+")]
    private static partial Regex ClassRegex();

    [GeneratedRegex(@"^#{2,4} (?<text>.+)$")]
    private static partial Regex HeadingRegex();

    [GeneratedRegex(@"href=""(?<href>/api/mcp/[^""]+)""")]
    private static partial Regex PageLinkRegex();
}
