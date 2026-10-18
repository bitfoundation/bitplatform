using System.Net;
using System.Reflection;
using System.Text.Json;
using System.Globalization;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Bit.Blow.Demo.Server.Dtos;
using Bit.Blow.Demo.Server.Services;
using Microsoft.AspNetCore.Components;

namespace Bit.Blow.Tests.Mcp;

/// <summary>
/// The server's claims put to the things they describe: the API reference against fresh instances
/// of the types it documents, the flow lab against the layout and graph helpers it reports on, the
/// theme against the stylesheet it was read from (and the README's table of it), the index against
/// the assembly, and the HTTP mirror against the protocol it mirrors. No expected answers are written
/// down here - the library, its files and the protocol are the expectations.
/// </summary>
[TestClass]
public partial class McpOracleTests
{
    // ---- The API reference -------------------------------------------------

    [TestMethod]
    public async Task Every_parameter_default_of_the_canvas_is_what_a_new_canvas_has()
    {
        // Over the wire, as an agent reads it, against an instance made here.
        var reference = await McpCall.TextAsync("GetBlowApi", new() { ["typeName"] = nameof(BlowCanvas) });

        var checkedCount = AssertDefaultsMatch(typeof(BlowCanvas), reference);

        Assert.IsGreaterThan(60, checkedCount, "Most of the canvas's parameters are simple values whose defaults can be compared.");
    }

    [TestMethod]
    public void Every_parameter_default_of_every_component_is_what_a_new_instance_has()
    {
        foreach (var type in typeof(BlowCanvas).Assembly.GetExportedTypes().Where(type => typeof(IComponent).IsAssignableFrom(type) && type.IsAbstract is false && type.IsGenericTypeDefinition is false))
        {
            AssertDefaultsMatch(type, BlowApiCatalog.RenderType(type.Name)!);
        }
    }

    /// <summary>
    /// Compares every parameter of a simple type - a number, a flag, a string, an enum - with the value
    /// the property holds on a new instance, and returns how many it compared.
    /// </summary>
    private static int AssertDefaultsMatch(Type type, string reference)
    {
        var instance = Activator.CreateInstance(type)!;
        var compared = 0;

        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(property => property.IsDefined(typeof(ParameterAttribute))))
        {
            var value = property.GetValue(instance);

            string? expected = value switch
            {
                bool flag => flag ? "true" : "false",
                string text => $"\"{text}\"",
                Enum member => $"{member.GetType().Name}.{member}",
                double or int or float => ((IFormattable)value).ToString(null, CultureInfo.InvariantCulture),
                _ => null
            };

            if (expected is null) continue;

            var line = reference.Split('\n').SingleOrDefault(line => line.StartsWith($"- **{property.Name}** :", StringComparison.Ordinal));

            Assert.IsNotNull(line, $"{type.Name}.{property.Name} is missing from its reference.");
            Assert.Contains($" = `{expected}`", line, $"{type.Name}.{property.Name}");

            compared++;
        }

        return compared;
    }

    [TestMethod]
    public void Every_exported_type_is_in_the_api_index()
    {
        var exported = typeof(BlowCanvas).Assembly.GetExportedTypes()
            .Where(type => type.IsNested is false)
            .Select(type => BlowApiCatalog.FriendlyName(type))
            .ToHashSet(StringComparer.Ordinal);

        var indexed = BlowApiCatalog.Types.Select(type => type.Name).ToHashSet(StringComparer.Ordinal);

        CollectionAssert.AreEquivalent(exported.ToArray(), indexed.ToArray());
    }

    [TestMethod]
    public void Every_documented_public_type_has_its_summary_in_the_index()
    {
        // The XML documentation file has to reach the server's output folder for any of this to work;
        // a build that stops copying it leaves every summary empty without failing anything else. A
        // razor component cannot carry an XML summary on its class, so components are not counted.
        var types = BlowApiCatalog.Types.Where(type => type.Kind != "Component").ToArray();
        var withSummary = types.Count(type => type.Summary is not null);

        Assert.IsGreaterThan(types.Length * 9 / 10, withSummary, "Most public types should come with their XML summary.");
    }

    // ---- The flow lab --------------------------------------------------------

    [TestMethod]
    public async Task The_flow_lab_agrees_with_the_graph_helpers_and_the_layouts()
    {
        var random = new Random(20261018);

        foreach (var layout in new[] { "layered", "layered-left-to-right", "grid", "circle" })
        {
            var (nodes, edges) = RandomFlow(random);
            var json = JsonSerializer.Serialize(new BlowSnapshot(nodes, edges, BlowViewport.Identity));

            var report = await McpFlowLabTests.InspectAsync(json, layout);
            var context = layout;

            Assert.IsTrue(report.Valid, $"{context}: {report.Error}");
            Assert.IsEmpty(report.Problems!, context);
            Assert.AreEqual(nodes.Count, report.Nodes, context);
            Assert.AreEqual(edges.Count, report.Edges, context);

            foreach (var group in nodes.GroupBy(node => node.Type)) Assert.AreEqual(group.Count(), report.NodeTypes!.Single(count => count.Name == group.Key).Count, $"{context}: {group.Key}");
            foreach (var group in edges.GroupBy(edge => edge.Type)) Assert.AreEqual(group.Count(), report.EdgeTypes!.Single(count => count.Name == group.Key.ToString()).Count, $"{context}: {group.Key}");

            // A flat flow's nodes are where their Position says, so the helper needs no canvas to agree.
            var bounds = BlowGraphUtils.GetNodesBounds(nodes);
            Assert.AreEqual(new BlowRectDto { X = Math.Round(bounds.X, 2), Y = Math.Round(bounds.Y, 2), Width = Math.Round(bounds.Width, 2), Height = Math.Round(bounds.Height, 2) }, report.Bounds, context);

            // The same layout, run here on a copy of the same nodes, puts every node in the same place.
            var copy = nodes.Select(node => node.Clone()).ToList();

            switch (layout)
            {
                case "grid": BlowLayout.Grid(copy); break;
                case "circle": BlowLayout.Circle(copy); break;
                case "layered-left-to-right": BlowLayout.Layered(copy, edges, new BlowLayeredLayoutOptions { Direction = BlowLayoutDirection.LeftToRight }); break;
                default: BlowLayout.Layered(copy, edges); break;
            }

            Assert.HasCount(copy.Count, report.Layout!.Positions, $"{context}: every node gets a position.");

            foreach (var node in copy)
            {
                var position = report.Layout.Positions.Single(position => position.Id == node.Id);

                Assert.AreEqual(Math.Round(node.Position.X, 2), position.X, $"{context}: {node.Id}.X");
                Assert.AreEqual(Math.Round(node.Position.Y, 2), position.Y, $"{context}: {node.Id}.Y");
            }
        }
    }

    [TestMethod]
    public async Task A_dangling_edge_is_flagged_with_the_code_the_library_defines()
    {
        var json = JsonSerializer.Serialize(new BlowSnapshot(
            [new BlowNode { Id = "a" }],
            [new BlowEdge { Id = "a-ghost", Source = "a", Target = "ghost" }],
            BlowViewport.Identity));

        var report = await McpFlowLabTests.InspectAsync(json);

        var problem = report.Problems!.Single();

        Assert.AreEqual(BlowErrorCode.MissingEdgeEndpoint, problem.Code);
        Assert.Contains("'a-ghost'", problem.Message);
        Assert.Contains("'ghost'", problem.Message);
    }

    [TestMethod]
    public async Task A_parent_cycle_really_does_throw_its_nodes_far_away()
    {
        // The note claims the canvas sums the relative positions round the loop; where it puts the two
        // nodes is the evidence, against the (0, 0) to (100, 100) their own positions describe. They fly
        // off together, so it is the bounds' corner that moves, not their size.
        var json = JsonSerializer.Serialize(new BlowSnapshot(
            [new BlowNode { Id = "p", ParentId = "q", Position = new(100, 0) }, new BlowNode { Id = "q", ParentId = "p", Position = new(0, 100) }],
            [],
            BlowViewport.Identity));

        var report = await McpFlowLabTests.InspectAsync(json);

        Assert.IsGreaterThan(1000, report.Bounds!.X + report.Bounds.Y);
        Assert.IsTrue(report.Notes.Any(note => note.Contains("parent cycle", StringComparison.Ordinal)));
    }

    /// <summary>A flat flow with some of everything the report counts: every built-in node type, custom ones, every edge type, sizes given and not.</summary>
    private static (List<BlowNode> Nodes, List<BlowEdge> Edges) RandomFlow(Random random)
    {
        string[] types = ["default", "input", "output", "process", "decision"];
        var edgeTypes = Enum.GetValues<BlowEdgeType>();

        var nodes = Enumerable.Range(0, random.Next(5, 14)).Select(i => new BlowNode
        {
            Id = $"n{i}",
            Type = types[random.Next(types.Length)],
            Position = new(random.Next(-200, 800), random.Next(-100, 600)),
            Width = random.Next(3) == 0 ? random.Next(80, 260) : null,
            Height = random.Next(3) == 0 ? random.Next(30, 120) : null,
        }).ToList();

        var edges = Enumerable.Range(0, random.Next(4, 18)).Select(i => new BlowEdge
        {
            Id = $"e{i}",
            Source = nodes[random.Next(nodes.Count)].Id,
            Target = nodes[random.Next(nodes.Count)].Id,
            Type = edgeTypes[random.Next(edgeTypes.Length)],
        }).ToList();

        return (nodes, edges);
    }

    // ---- The theme and the stylesheet ------------------------------------------

    [TestMethod]
    public void Every_custom_property_the_root_declares_is_in_the_theme_catalog()
    {
        var css = BlowSourceCatalog.Stylesheet;

        Assert.IsFalse(string.IsNullOrEmpty(css), "The shipped stylesheet is not embedded.");

        var declared = DeclarationRegex().Matches(css).Select(match => match.Groups["name"].Value).Distinct(StringComparer.Ordinal).ToArray();

        CollectionAssert.AreEquivalent(declared, BlowThemeCatalog.Properties.Select(property => property.Name).ToArray());

        foreach (var property in BlowThemeCatalog.Properties)
        {
            // The first declaration is the one on .bit-blow itself; a later one is a colour mode copying the dark palette over it.
            var value = DeclarationRegex().Matches(css).First(match => match.Groups["name"].Value == property.Name).Groups["value"].Value.Trim();

            Assert.AreEqual(value, property.Value, property.Name);
        }
    }

    [TestMethod]
    public void The_dark_class_and_the_system_media_query_copy_the_same_palette()
    {
        // The stylesheet writes the copy twice - once for ColorMode.Dark, once for System on a dark OS -
        // because CSS cannot share a block between a class and a media query. Two lists kept by hand are
        // exactly what drifts, so they are held to each other here.
        static Dictionary<string, string> Copies(string selector) => BlowThemeCatalog.Rules
            .Where(rule => rule.Selector == selector)
            .SelectMany(rule => rule.Declarations)
            .ToDictionary(declaration => declaration.Name, declaration => declaration.Value, StringComparer.Ordinal);

        var dark = Copies(".bit-blow.bit-blow-dark");
        var system = Copies(".bit-blow.bit-blow-system");

        Assert.IsNotEmpty(dark);
        CollectionAssert.AreEquivalent(dark.ToArray(), system.ToArray());
    }

    [TestMethod]
    public void The_readme_table_of_custom_properties_agrees_with_the_stylesheet()
    {
        var rows = ReadmeThemeRowRegex().Matches(BlowSourceCatalog.Readme).ToArray();

        Assert.IsNotEmpty(rows, "The README's table of custom properties is gone; this test has nothing left to compare.");

        foreach (var row in rows)
        {
            var property = BlowThemeCatalog.Find(row.Groups["name"].Value);

            Assert.IsNotNull(property, $"The README documents {row.Groups["name"].Value}, which the stylesheet does not declare.");
            Assert.AreEqual(row.Groups["light"].Value, property.Value, $"{property.Name}: light");
            Assert.AreEqual(row.Groups["dark"].Value, property.DarkValue ?? property.Value, $"{property.Name}: dark");
        }
    }

    [TestMethod]
    public void Every_class_of_the_stylesheet_is_a_search_entry()
    {
        var css = BlowSourceCatalog.Stylesheet;

        var classes = ClassRegex().Matches(css).Select(match => match.Value).Distinct(StringComparer.Ordinal).ToArray();
        var selectors = string.Join(' ', BlowStylesheetClasses.Classes.Select(rule => rule.Selector));

        Assert.IsGreaterThan(20, classes.Length);
        foreach (var name in classes) Assert.Contains(name, selectors);
    }

    // ---- The guide and the HTTP mirror -------------------------------------------

    [TestMethod]
    public void The_guide_sections_cover_every_heading_of_the_readme()
    {
        var readme = BlowSourceCatalog.Readme.Replace("\r\n", "\n", StringComparison.Ordinal);

        Assert.IsFalse(string.IsNullOrEmpty(readme), "The README is not embedded.");

        var fenced = false;
        var headings = new List<string>();

        foreach (var line in readme.Split('\n'))
        {
            if (line.TrimStart().StartsWith("```", StringComparison.Ordinal)) { fenced = fenced is false; continue; }
            if (fenced is false && HeadingRegex().Match(line) is { Success: true } match) headings.Add(match.Groups["text"].Value.Trim());
        }

        CollectionAssert.AreEqual(headings, BlowSourceCatalog.GuideSections.Select(section => section.Heading).ToArray());
        Assert.HasCount(BlowSourceCatalog.GuideSections.Length, BlowSourceCatalog.GuideSections.Select(section => section.Key).Distinct(StringComparer.Ordinal), "Every section needs a key of its own.");
    }

    [TestMethod]
    public void Every_example_of_the_gallery_has_its_page()
    {
        foreach (var example in Bit.Blow.Demo.Client.Examples.ExampleCatalog.All)
        {
            Assert.IsNotNull(BlowSourceCatalog.FindByRoute($"examples/{example.Route}"), $"The gallery lists '{example.Route}', and no embedded page declares /examples/{example.Route}.");
        }
    }

    [TestMethod]
    public async Task Every_tool_is_reachable_as_a_plain_get_with_the_same_answer()
    {
        using var http = McpTestHost.CreateHttpClient();

        (string Tool, string Query, Dictionary<string, object?>? Arguments)[] calls =
        [
            ("SearchBlow", "?query=custom%20node&limit=3", new() { ["query"] = "custom node", ["limit"] = 3 }),
            ("GetBlowSetupGuide", "?renderMode=server", new() { ["renderMode"] = "server" }),
            ("GetBlowGuideSection", "?heading=Theming", new() { ["heading"] = "Theming" }),
            ("GetBlowApi", "?typeName=BlowNode", new() { ["typeName"] = "BlowNode" }),
            ("GetBlowTheme", "?property=--bit-blow-edge-stroke", new() { ["property"] = "--bit-blow-edge-stroke" }),
            ("InspectBlowFlow", "", null),
            ("GetBlowSourceFile", "?path=Demo/Client/Pages/CustomNodes.razor", new() { ["path"] = "Demo/Client/Pages/CustomNodes.razor" }),
        ];

        CollectionAssert.AreEquivalent(McpSurfaceTests.ExpectedTools, calls.Select(call => call.Tool).ToArray(),
            "The HTTP mirror is being exercised for a different set of tools than the server publishes.");

        foreach (var (tool, query, arguments) in calls)
        {
            using var response = await http.GetAsync($"/api/mcp/{tool}{query}");

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, tool);

            var body = await response.Content.ReadAsStringAsync();

            if (tool is "SearchBlow" or "InspectBlowFlow")
            {
                Assert.IsNotNull(JsonNode.Parse(body) as JsonObject, tool);
                continue;
            }

            Assert.AreEqual(await McpCall.TextAsync(tool, arguments), body, tool);
        }
    }

    [GeneratedRegex(@"^\s*(?<name>--bit-blow-[a-z0-9-]+)\s*:\s*(?<value>[^;]+);", RegexOptions.Multiline)]
    private static partial Regex DeclarationRegex();

    [GeneratedRegex(@"^\| `(?<name>--bit-blow-[a-z0-9-]+)` \| `(?<light>[^`]+)` \| `(?<dark>[^`]+)` \|", RegexOptions.Multiline)]
    private static partial Regex ReadmeThemeRowRegex();

    [GeneratedRegex(@"\.bit-blow-[a-z0-9-]+")]
    private static partial Regex ClassRegex();

    [GeneratedRegex(@"^#{2,4} (?<text>.+)$")]
    private static partial Regex HeadingRegex();
}
