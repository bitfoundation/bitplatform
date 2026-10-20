using System.Net;
using System.Reflection;
using System.Globalization;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Bit.Bdnd.Demo.Server.Dtos;
using Bit.Bdnd.Demo.Server.Services;
using Microsoft.AspNetCore.Components;

namespace Bit.Bdnd.Tests.Mcp;

/// <summary>
/// The server's claims put to the things they describe: the API reference against the assembly and
/// fresh instances of the types it documents, the drop lab against the rules the guide states for
/// nested drop targets, the guide's own reference tables against the components, the index against
/// the README, and the HTTP mirror against the protocol it mirrors. No expected answers are written
/// down here - the library, its files and the protocol are the expectations.
/// </summary>
[TestClass]
public partial class McpOracleTests
{
    // ---- The API reference -------------------------------------------------

    [TestMethod]
    public async Task Every_parameter_default_of_the_draggable_is_what_a_new_draggable_has()
    {
        // Over the wire, as an agent reads it, against an instance made here.
        var reference = await McpCall.TextAsync("GetBdndApi", new() { ["typeName"] = "BdndDraggable" });

        var checkedCount = AssertDefaultsMatch(typeof(BdndDraggable<object>), reference);

        Assert.IsGreaterThanOrEqualTo(5, checkedCount, "CanDrag, UseCustomDragLayer, UseHandle, DropEffect and Tag are simple values whose defaults can be compared.");
    }

    [TestMethod]
    public void Every_parameter_default_of_every_component_is_what_a_new_instance_has()
    {
        var components = typeof(BdndProvider).Assembly.GetExportedTypes()
            .Where(type => typeof(IComponent).IsAssignableFrom(type) && type.IsAbstract is false && type.Name != "_Imports")
            .ToArray();

        Assert.IsGreaterThanOrEqualTo(5, components.Length);

        foreach (var type in components)
        {
            // A generic component is what TItem="object" makes of it in markup.
            var closed = type.IsGenericTypeDefinition ? type.MakeGenericType([.. type.GetGenericArguments().Select(_ => typeof(object))]) : type;

            AssertDefaultsMatch(closed, BdndApiCatalog.RenderType(type.Name)!);
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
        var exported = typeof(BdndDraggable<>).Assembly.GetExportedTypes()
            // Razor compiles the library's _Imports.razor into a public class nobody calls.
            .Where(type => type.IsNested is false && type.Name != "_Imports")
            .Select(BdndApiCatalog.FriendlyName)
            .ToHashSet(StringComparer.Ordinal);

        var indexed = BdndApiCatalog.Types.Select(type => type.Name).ToHashSet(StringComparer.Ordinal);

        CollectionAssert.AreEquivalent(exported.ToArray(), indexed.ToArray());
    }

    [TestMethod]
    public void Every_public_type_has_its_summary_in_the_index()
    {
        // The XML documentation file has to reach the server's output folder for any of this to work;
        // a build that stops copying it leaves every summary empty without failing anything else. The
        // library's razor components carry their summaries on their partial classes, so none is exempt.
        var missing = BdndApiCatalog.Types.Where(type => type.Summary is null).Select(type => type.Name).ToArray();

        Assert.IsEmpty(missing, $"No XML summary for: {string.Join(", ", missing)}.");
    }

    [TestMethod]
    public void Every_public_member_of_a_component_is_documented()
    {
        foreach (var type in BdndApiCatalog.Types.Where(type => type.Kind == "Component"))
        {
            var undocumented = BdndApiCatalog.GetTypeDetails(type.Name)!.Members
                .Where(member => member.Kind == "Parameter" && member.Summary is null)
                .Select(member => member.Name)
                .ToArray();

            Assert.IsEmpty(undocumented, $"{type.Name}: {string.Join(", ", undocumented)}");
        }
    }

    // ---- The drop lab --------------------------------------------------------

    /// <summary>
    /// Random trees of drop targets, run through the real components, held to the rules the guide
    /// states for them: CanDrop where the type is accepted and the predicate agrees, hovered or not;
    /// IsOver on every hovered target that accepts the type and IsOverShallow on the innermost of
    /// them; OnDrop on every hovered target that can drop, innermost first; and the outermost
    /// non-null result is the one OnEnd receives.
    /// </summary>
    [TestMethod]
    public async Task The_drop_lab_agrees_with_the_rules_the_guide_states()
    {
        var random = new Random(20261018);

        for (var run = 0; run < 40; run++)
        {
            var targets = new List<Model>();
            var roots = Enumerable.Range(0, random.Next(1, 3)).Select(_ => RandomTarget(random, null, 0, targets)).ToArray();
            var hovered = random.Next(5) == 0 ? null : targets[random.Next(targets.Count)];
            var type = random.Next(4) == 0 ? "box" : "card";

            var report = await McpDropLabTests.InspectAsync(McpDropLabTests.Scenario(type, hovered?.Id, [.. roots.Select(root => root.Json)]));
            var context = $"run {run}: {string.Join(", ", targets.Select(target => target.ToString()))} hover {hovered?.Id ?? "none"}, type {type}";

            Assert.IsTrue(report.Valid, $"{context}: {report.Error}");

            var chain = new List<Model>();
            for (var target = hovered; target is not null; target = target.Parent) chain.Insert(0, target);

            bool Accepts(Model target) => target.Accept.Contains(type);
            bool CanDrop(Model target) => Accepts(target) && target.CanDrop;
            bool IsOver(Model target) => chain.Contains(target) && Accepts(target);

            var drops = Enumerable.Reverse(chain).Where(CanDrop).ToArray();

            foreach (var (target, actual) in targets.Zip(report.Targets!))
            {
                Assert.AreEqual(target.Id, actual.Id, context);
                Assert.AreEqual(CanDrop(target), actual.CanDrop, $"{context} - {target.Id}.CanDrop");
                Assert.AreEqual(IsOver(target), actual.IsOver, $"{context} - {target.Id}.IsOver");
                Assert.AreEqual(IsOver(target) && chain.SkipWhile(other => other != target).Skip(1).Any(IsOver) is false, actual.IsOverShallow, $"{context} - {target.Id}.IsOverShallow");
            }

            CollectionAssert.AreEqual(drops.Select(target => target.Id).ToArray(), report.DropOrder, context);
            Assert.AreEqual(drops.Length > 0, report.DropFired, context);
            Assert.AreEqual(drops.Length > 0, report.End!.DidDrop, context);
            Assert.AreEqual(drops.LastOrDefault(target => target.Result is not null)?.Result, report.End.DropResult, context);
        }
    }

    private sealed record Model(string Id, Model? Parent, string[] Accept, bool CanDrop, string? Result, JsonObject Json)
    {
        public override string ToString() => $"{Id}[{string.Join('|', Accept)}{(CanDrop ? null : " no")}{(Result is null ? null : $" ={Result}")}]";
    }

    private static Model RandomTarget(Random random, Model? parent, int depth, List<Model> all)
    {
        string[] types = ["card", "box", "column"];

        var id = $"t{all.Count}";
        var accept = types.Where(_ => random.Next(3) > 0).ToArray();
        var canDrop = random.Next(5) > 0;
        var result = random.Next(2) == 0 ? $"r{all.Count}" : null;

        var json = McpDropLabTests.Target(id, accept, canDrop: canDrop ? null : false, result: result);
        var model = new Model(id, parent, accept, canDrop, result, json);

        all.Add(model);

        if (depth < 3)
        {
            var children = Enumerable.Range(0, random.Next(0, 3)).Select(_ => RandomTarget(random, model, depth + 1, all)).ToArray();

            if (children.Length > 0) json["children"] = new JsonArray([.. children.Select(child => (JsonNode)child.Json)]);
        }

        return model;
    }

    // ---- The guide against the components and the README ---------------------------

    /// <summary>
    /// The README's component reference tables, row by row, against the components themselves: every
    /// parameter a table names exists with that type and that default, and every parameter a component
    /// has is in its table. A default changed in code and not in the README fails here.
    /// </summary>
    [TestMethod]
    public void The_readme_component_reference_agrees_with_the_components()
    {
        var tables = BdndSourceCatalog.GuideSections.Where(section => section.Parent == "Component reference").ToArray();

        Assert.IsGreaterThanOrEqualTo(5, tables.Length, "The README's component reference is gone; this test has nothing left to compare.");

        foreach (var table in tables)
        {
            var details = BdndApiCatalog.GetTypeDetails(table.Heading);

            Assert.IsNotNull(details, $"The README documents '{table.Heading}', which is not a public type.");

            var rows = ReferenceRowRegex().Matches(BdndSourceCatalog.GetGuideSection(table.Key)!).ToArray();
            var parameters = details.Members.Where(member => member.Kind == "Parameter").ToDictionary(member => member.Name, StringComparer.Ordinal);

            foreach (var row in rows)
            {
                var name = row.Groups["name"].Value;
                var context = $"{table.Heading}.{name}";

                Assert.IsTrue(parameters.TryGetValue(name, out var parameter), $"The README documents {context}, which is not a parameter.");
                Assert.AreEqual(Unwrap(row.Groups["type"].Value).Replace("?", string.Empty, StringComparison.Ordinal), parameter.Type!.Replace("?", string.Empty, StringComparison.Ordinal), $"{context}: type");

                var documented = row.Groups["default"].Value.Trim();

                if (documented == "(required)") Assert.IsTrue(parameter.Required, $"{context} is documented as required.");
                else if (documented.Length > 0 && documented != "`null`") Assert.AreEqual(Unwrap(documented), parameter.Default, $"{context}: default");
            }

            // AdditionalAttributes is the "(any other attribute)" row.
            var undocumented = parameters.Keys.Where(name => name != "AdditionalAttributes" && rows.Any(row => row.Groups["name"].Value == name) is false).ToArray();

            Assert.IsEmpty(undocumented, $"The README's table for {table.Heading} leaves out: {string.Join(", ", undocumented)}.");
        }

        static string Unwrap(string cell) => cell.Trim().Trim('`');
    }

    [TestMethod]
    public void The_guide_sections_cover_every_heading_of_the_readme()
    {
        var readme = BdndSourceCatalog.Readme.Replace("\r\n", "\n", StringComparison.Ordinal);

        Assert.IsFalse(string.IsNullOrEmpty(readme), "The README is not embedded.");

        var fenced = false;
        var headings = new List<string>();

        foreach (var line in readme.Split('\n'))
        {
            if (line.TrimStart().StartsWith("```", StringComparison.Ordinal)) { fenced = fenced is false; continue; }
            if (fenced is false && HeadingRegex().Match(line) is { Success: true } match) headings.Add(WebUtility.HtmlDecode(match.Groups["text"].Value.Trim()));
        }

        CollectionAssert.AreEqual(headings, BdndSourceCatalog.GuideSections.Select(section => section.Heading).ToArray());
        Assert.HasCount(BdndSourceCatalog.GuideSections.Length, BdndSourceCatalog.GuideSections.Select(section => section.Key).Distinct(StringComparer.Ordinal), "Every section needs a key of its own.");
    }

    [TestMethod]
    public void Every_demo_page_and_library_component_is_embedded()
    {
        var pages = BdndSourceCatalog.SourceFiles.Where(file => file.Kind == "Page").ToArray();

        // The demo's docs catalog - the source of its navigation - names every page; each of them has to be readable
        // here, title and route included.
        var catalog = BdndSourceCatalog.GetSourceFile("Demo/Client/DocsCatalog.cs")!;
        var linked = CatalogPageRegex().Matches(catalog).Select(match => match.Groups["href"].Value).Where(route => route != "/mcp-server").ToArray();

        Assert.IsGreaterThan(15, linked.Length, $"the catalog names only {linked.Length} pages");
        foreach (var route in linked) Assert.IsTrue(pages.Any(page => page.Route == route && page.Title is not null), $"The navigation links {route}, and no embedded page with a title declares it.");

        foreach (var component in new[] { "BdndDraggable.cs", "BdndDropTarget.cs", "BdndProvider.razor", "BdndDragLayer.razor", "BdndDragHandle.razor" })
        {
            Assert.IsNotNull(BdndSourceCatalog.GetSourceFile($"Library/Components/{component}"), component);
        }

        Assert.Contains("registerDropTarget", BdndSourceCatalog.GetSourceFile(BdndSourceCatalog.ScriptPath)!);
    }

    // ---- The HTTP mirror -------------------------------------------------------

    [TestMethod]
    public async Task Every_tool_is_reachable_as_a_plain_get_with_the_same_answer()
    {
        using var http = McpTestHost.CreateHttpClient();

        (string Tool, string Query, Dictionary<string, object?>? Arguments)[] calls =
        [
            ("SearchBdnd", "?query=sortable%20list&limit=3", new() { ["query"] = "sortable list", ["limit"] = 3 }),
            ("GetBdndSetupGuide", "?renderMode=server", new() { ["renderMode"] = "server" }),
            ("GetBdndGuideSection", "?heading=Drop%20results", new() { ["heading"] = "Drop results" }),
            ("GetBdndApi", "?typeName=BdndDraggable", new() { ["typeName"] = "BdndDraggable" }),
            ("InspectBdndDrop", "", null),
            ("GetBdndSourceFile", "?path=Demo/Client/Pages/Sortable/SortableCard.razor", new() { ["path"] = "Demo/Client/Pages/Sortable/SortableCard.razor" }),
        ];

        CollectionAssert.AreEquivalent(McpSurfaceTests.ExpectedTools, calls.Select(call => call.Tool).ToArray(),
            "The HTTP mirror is being exercised for a different set of tools than the server publishes.");

        foreach (var (tool, query, arguments) in calls)
        {
            using var response = await http.GetAsync($"/api/mcp/{tool}{query}");

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, tool);

            var body = await response.Content.ReadAsStringAsync();

            if (tool is "SearchBdnd" or "InspectBdndDrop")
            {
                Assert.IsNotNull(JsonNode.Parse(body) as JsonObject, tool);
                continue;
            }

            Assert.AreEqual(await McpCall.TextAsync(tool, arguments), body, tool);
        }
    }

    [TestMethod]
    public async Task The_drop_lab_runs_over_plain_http_too()
    {
        using var http = McpTestHost.CreateHttpClient();

        using var response = await http.GetAsync($"/api/mcp/InspectBdndDrop?json={Uri.EscapeDataString(BdndDropLab.Sample)}");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        var report = JsonNode.Parse(await response.Content.ReadAsStringAsync())!;

        Assert.IsTrue((bool)report["valid"]!);
        Assert.AreEqual("board", (string?)report["end"]!["dropResult"]);
        Assert.IsNull(report["sample"], "A member with nothing in it is left out, as the MCP transport leaves it out.");
    }

    [GeneratedRegex(@"^\| `(?<name>\w+)` \|(?<type>[^|]*)\|(?<default>[^|]*)\|", RegexOptions.Multiline)]
    private static partial Regex ReferenceRowRegex();

    [GeneratedRegex(@"^#{2,4} (?<text>.+)$")]
    private static partial Regex HeadingRegex();

    // A page's entry opens with its address and its title: new("/dustbin/single", "Single target", "...", ...). The
    // header's links (new("/", "Home")) end after the label.
    [GeneratedRegex(@"\bnew\(""(?<href>/[^""]*)"",\s*""[^""]*"",")]
    private static partial Regex CatalogPageRegex();
}
