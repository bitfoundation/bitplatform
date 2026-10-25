using System.Net;
using System.Reflection;
using System.Globalization;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Bit.Bortable.Demo.Server.Services;
using Microsoft.AspNetCore.Components;

namespace Bit.Bortable.Tests.Mcp;

/// <summary>
/// The server's claims put to the things they describe: the API reference against fresh instances
/// of the types it documents (and against the README's tables of them), the theme against the
/// stylesheet it was read from (and the README's table of it), the drag lab's event order against
/// the README's account of it, the index against the assembly, and the HTTP mirror against the
/// protocol it mirrors. No expected answers are written down here - the library, its files and the
/// protocol are the expectations.
/// </summary>
[TestClass]
public partial class McpOracleTests
{
    // ---- The API reference -------------------------------------------------

    [TestMethod]
    public async Task Every_parameter_default_of_the_component_is_what_a_new_list_has()
    {
        // Over the wire, as an agent reads it, against two lists made here: where they agree, that is
        // the default; where they do not (the Id is a fresh Guid each time), the reference must give none.
        var reference = await McpCall.TextAsync("GetBortableApi", new() { ["typeName"] = "Bortable" });

        var instance = new Bortable<string>();
        var twin = new Bortable<string>();
        var compared = 0;

        foreach (var property in typeof(Bortable<string>).GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(property => property.IsDefined(typeof(ParameterAttribute))))
        {
            var expected = Literal(property.GetValue(instance));
            if (expected is null) continue;

            var line = reference.Split('\n').SingleOrDefault(line => line.StartsWith($"- **{property.Name}** :", StringComparison.Ordinal));

            Assert.IsNotNull(line, $"Bortable.{property.Name} is missing from its reference.");

            if (expected != Literal(property.GetValue(twin)))
            {
                Assert.DoesNotContain(" = `", line, $"Bortable.{property.Name} differs between two new lists, so it has no default to report.");
                continue;
            }

            Assert.Contains($" = `{expected}`", line, $"Bortable.{property.Name}");
            compared++;
        }

        Assert.IsGreaterThan(12, compared, "Most of the list's parameters are simple values whose defaults can be compared.");

        static string? Literal(object? value) => value switch
        {
            bool flag => flag ? "true" : "false",
            string text => $"\"{text}\"",
            Enum member => $"{member.GetType().Name}.{member}",
            double or int or float => ((IFormattable)value).ToString(null, CultureInfo.InvariantCulture),
            _ => null
        };
    }

    [TestMethod]
    public void Every_exported_type_is_in_the_api_index()
    {
        var exported = typeof(Bortable<>).Assembly.GetExportedTypes()
            .Where(type => type.IsNested is false)
            .ToArray();

        // The one exported type left out is the class the Razor compiler makes of _Imports.razor; leaving
        // out anything else would hide a real type from every agent.
        var skipped = exported.Where(type => type.Name.StartsWith('_')).Select(type => type.Name).ToArray();
        CollectionAssert.AreEquivalent(new[] { "_Imports" }, skipped);

        var expected = exported.Where(type => type.Name.StartsWith('_') is false).Select(BortableApiCatalog.FriendlyName).ToArray();

        CollectionAssert.AreEquivalent(expected, BortableApiCatalog.Types.Select(type => type.Name).ToArray());
    }

    [TestMethod]
    public void Every_public_type_has_its_summary_in_the_index()
    {
        // The XML documentation file has to reach the server's output folder for any of this to work; a
        // build that stops copying it leaves every summary empty without failing anything else.
        foreach (var type in BortableApiCatalog.Types)
        {
            Assert.IsNotNull(type.Summary, $"{type.Name} has no summary in the index.");
        }
    }

    [TestMethod]
    public void The_readme_table_of_parameters_agrees_with_the_component()
    {
        var members = BortableApiCatalog.GetTypeDetails("Bortable")!.Members.Where(member => member.Kind == "Parameter").ToDictionary(member => member.Name, StringComparer.Ordinal);
        var rows = TableRowRegex().Matches(BortableSourceCatalog.GetGuideSection("Bortable parameters")!).ToArray();
        var documented = new HashSet<string>(StringComparer.Ordinal);

        Assert.IsNotEmpty(rows, "The README's table of parameters is gone; this test has nothing left to compare.");

        foreach (var row in rows)
        {
            foreach (var name in CodeRegex().Matches(row.Groups["names"].Value).Select(match => match.Groups["code"].Value))
            {
                documented.Add(name);

                Assert.IsTrue(members.TryGetValue(name, out var member), $"The README documents a parameter '{name}' the component does not have.");
                Assert.AreEqual(row.Groups["type"].Value, member.Type, $"{name}: type");

                // The README writes a default the way a person reads it - `Move`, `bit-bortable-ghost`, `null`,
                // "generated" - and the reference the way C# writes it; both are brought to the bare value.
                var cell = row.Groups["default"].Value.Trim();
                var expected = cell is "" or "generated" or "`null`" ? null : cell.Trim('`');
                var actual = member.Default?.Trim('"');
                if (actual is not null && actual.Contains('.', StringComparison.Ordinal) && member.Type is "BortablePull" or "BortableDirection") actual = actual[(actual.IndexOf('.') + 1)..];

                Assert.AreEqual(expected, actual, $"{name}: default");
            }
        }

        CollectionAssert.AreEquivalent(members.Keys.ToArray(), documented.ToArray(), "Every parameter of the component, and nothing else, should be in the README's table.");
    }

    [TestMethod]
    public void The_readme_table_of_event_arguments_agrees_with_BortableEventArgs()
    {
        var members = BortableApiCatalog.GetTypeDetails("BortableEventArgs")!.Members.ToDictionary(member => member.Name, StringComparer.Ordinal);
        var rows = ArgumentRowRegex().Matches(BortableSourceCatalog.GetGuideSection("Event arguments")!).ToArray();

        Assert.IsNotEmpty(rows);

        foreach (var row in rows)
        {
            Assert.IsTrue(members.TryGetValue(row.Groups["name"].Value, out var member), $"The README documents '{row.Groups["name"].Value}', which BortableEventArgs does not have.");
            Assert.AreEqual(row.Groups["type"].Value, member.Type, row.Groups["name"].Value);
        }

        CollectionAssert.AreEquivalent(members.Keys.ToArray(), rows.Select(row => row.Groups["name"].Value).ToArray());
    }

    [TestMethod]
    public void The_readme_tables_of_enum_values_agree_with_the_enums()
    {
        foreach (var type in new[] { typeof(BortablePull), typeof(BortableDirection) })
        {
            var section = BortableSourceCatalog.GetGuideSection(type.Name)!;
            var values = ValueRowRegex().Matches(section).Select(match => match.Groups["name"].Value).ToArray();

            CollectionAssert.AreEquivalent(Enum.GetNames(type), values, type.Name);
        }
    }

    // ---- The theme and the stylesheet ----------------------------------------

    [TestMethod]
    public void Every_custom_property_the_stylesheet_names_is_in_the_theme_catalog_with_its_default()
    {
        var css = BortableSourceCatalog.Stylesheet;

        Assert.IsFalse(string.IsNullOrEmpty(css), "The shipped stylesheet is not embedded.");

        var named = PropertyNameRegex().Matches(css).Select(match => match.Value).Distinct(StringComparer.Ordinal).ToArray();

        CollectionAssert.AreEquivalent(named, BortableThemeCatalog.Properties.Select(property => property.Name).ToArray());

        var declared = DeclarationRegex().Matches(css).ToDictionary(match => match.Groups["name"].Value, match => match.Groups["value"].Value.Trim(), StringComparer.Ordinal);

        foreach (var property in BortableThemeCatalog.Properties)
        {
            if (declared.TryGetValue(property.Name, out var value))
            {
                Assert.AreEqual(BortableThemeCatalog.DeclaredGroup, property.Group, property.Name);
                Assert.AreEqual(value, property.Value, property.Name);
            }
            else
            {
                // Declared nowhere, so its default is the fallback the rule that reads it writes.
                Assert.AreEqual(BortableThemeCatalog.OverrideGroup, property.Group, property.Name);
                Assert.AreEqual(FallbackOf(css, property.Name), property.Value, property.Name);
            }
        }
    }

    [TestMethod]
    public void The_readme_table_of_custom_properties_agrees_with_the_stylesheet()
    {
        var rows = ReadmeThemeRowRegex().Matches(BortableSourceCatalog.GetGuideSection("The custom properties")!).ToArray();

        Assert.IsNotEmpty(rows, "The README's table of custom properties is gone; this test has nothing left to compare.");

        foreach (var row in rows)
        {
            var property = BortableThemeCatalog.Find(row.Groups["name"].Value);
            var cell = row.Groups["default"].Value.Trim();

            Assert.IsNotNull(property, $"The README documents {row.Groups["name"].Value}, which the stylesheet does not have.");

            if (WholeCodeRegex().IsMatch(cell))
            {
                Assert.AreEqual(cell.Trim('`'), property.Value, property.Name);
                continue;
            }

            // "the accent at 12%", "`2px dashed` accent at 60%": the fallback tints the accent with that
            // alpha, and holds whatever the README quotes of it.
            foreach (var quoted in CodeFragmentRegex().Matches(cell).Select(match => match.Groups["code"].Value))
            {
                Assert.Contains(quoted, property.Value, property.Name);
            }

            var percent = PercentRegex().Match(cell);
            Assert.IsTrue(percent.Success, $"{property.Name}: the README's default '{cell}' names neither a value nor a share of the accent.");
            Assert.Contains("--bit-bortable-accent-rgb", property.Value, property.Name);

            var alpha = double.Parse(AlphaRegex().Match(property.Value).Groups["alpha"].Value, CultureInfo.InvariantCulture);
            Assert.AreEqual(double.Parse(percent.Groups["percent"].Value, CultureInfo.InvariantCulture) / 100, alpha, 1e-9, property.Name);
        }

        CollectionAssert.AreEquivalent(BortableThemeCatalog.Properties.Select(property => property.Name).ToArray(),
            rows.Select(row => row.Groups["name"].Value).ToArray(), "Every custom property of the stylesheet, and nothing else, should be in the README's table.");
    }

    [TestMethod]
    public void Every_class_of_the_stylesheet_is_served_and_the_readme_names_it_with_its_parameter()
    {
        var css = BortableSourceCatalog.Stylesheet;
        var classes = ClassRegex().Matches(css).Select(match => match.Groups["name"].Value).Distinct(StringComparer.Ordinal).ToArray();

        Assert.IsGreaterThan(8, classes.Length);
        CollectionAssert.AreEquivalent(classes, BortableStylesheetClasses.Classes.Select(cssClass => cssClass.Name).ToArray());

        var rows = ReadmeClassRowRegex().Matches(BortableSourceCatalog.GetGuideSection("Drag-state classes")!).ToArray();

        CollectionAssert.AreEquivalent(classes, rows.Select(row => row.Groups["name"].Value).ToArray(), "The README's table of classes should name exactly the stylesheet's.");

        foreach (var row in rows)
        {
            var cssClass = BortableStylesheetClasses.Find(row.Groups["name"].Value)!;

            Assert.AreEqual(row.Groups["parameter"].Success ? row.Groups["parameter"].Value : null, cssClass.Parameter, cssClass.Name);
        }
    }

    [TestMethod]
    public void A_class_said_to_be_replaced_by_a_parameter_is_that_parameter_default()
    {
        var list = new Bortable<string>();

        foreach (var cssClass in BortableStylesheetClasses.Classes.Where(cssClass => cssClass.Parameter is not null))
        {
            Assert.AreEqual(cssClass.Name, typeof(Bortable<string>).GetProperty(cssClass.Parameter!)!.GetValue(list), cssClass.Parameter);
        }

        Assert.HasCount(5, BortableStylesheetClasses.Classes.Where(cssClass => cssClass.Parameter is not null), "GhostClass, ChosenClass, SwapClass, SelectedClass and DragClass.");
    }

    // ---- The drag lab against the README ---------------------------------------

    [TestMethod]
    public async Task The_drag_lab_raises_the_completion_events_in_the_order_the_readme_states()
    {
        var section = BortableSourceCatalog.GetGuideSection("Order of events")!.ReplaceLineEndings(" ");

        var within = EventRegex().Matches(Sentence(section, "Within a list:")).Select(match => match.Value).ToArray();
        var across = Sentence(section, "Across lists:").Split(", ").Select(clause => (
            Event: EventRegex().Match(clause).Value,
            Source: clause.Contains("on the source", StringComparison.Ordinal),
            UnlessClone: clause.Contains("unless it was a clone", StringComparison.Ordinal))).ToArray();

        Assert.IsNotEmpty(within);
        Assert.IsNotEmpty(across);

        var reorder = await McpDragLabTests.InspectAsync("""{ "lists": [{ "id": "a", "items": ["A", "B", "C"] }], "drag": { "item": "A", "index": 2 } }""");
        var move = await McpDragLabTests.InspectAsync("""{ "lists": [{ "id": "a", "group": "g", "items": ["A", "B"] }, { "id": "b", "group": "g", "items": ["X"] }], "drag": { "item": "A", "to": "b", "index": 1 } }""");
        var clone = await McpDragLabTests.InspectAsync("""{ "lists": [{ "id": "a", "group": "g", "pull": "clone", "cloneSuffix": "+", "items": ["A", "B"] }, { "id": "b", "group": "g", "items": ["X"] }], "drag": { "item": "A", "to": "b", "index": 1 } }""");

        CollectionAssert.AreEqual(within, Completion(reorder), "within a list");
        CollectionAssert.AreEqual(across.Select(clause => $"{clause.Event}:{(clause.Source ? "a" : "b")}").ToArray(), Completion(move, withList: true), "across lists");
        CollectionAssert.AreEqual(across.Where(clause => clause.UnlessClone is false).Select(clause => $"{clause.Event}:{(clause.Source ? "a" : "b")}").ToArray(), Completion(clone, withList: true), "a clone");

        static string Sentence(string text, string start)
        {
            var at = text.IndexOf(start, StringComparison.Ordinal);
            Assert.IsGreaterThanOrEqualTo(0, at, $"The README's order of events no longer says '{start}'.");

            var end = text.IndexOf(". ", at, StringComparison.Ordinal);

            return text[(at + start.Length)..(end < 0 ? text.Length : end)];
        }

        static string[] Completion(Bit.Bortable.Demo.Server.Dtos.BortableDragReportDto report, bool withList = false)
            => [.. report.Events!.Where(e => e.Event is not ("OnStart" or "OnChange" or "ItemsChanged")).Select(e => withList ? $"{e.Event}:{e.List}" : e.Event)];
    }

    // ---- The guide and the HTTP mirror -------------------------------------------

    [TestMethod]
    public void The_guide_sections_cover_every_heading_of_the_readme()
    {
        var readme = BortableSourceCatalog.Readme.Replace("\r\n", "\n", StringComparison.Ordinal);

        Assert.IsFalse(string.IsNullOrEmpty(readme), "The README is not embedded.");

        var fenced = false;
        var headings = new List<string>();

        foreach (var line in readme.Split('\n'))
        {
            if (line.TrimStart().StartsWith("```", StringComparison.Ordinal)) { fenced = fenced is false; continue; }
            if (fenced is false && HeadingRegex().Match(line) is { Success: true } match) headings.Add(match.Groups["text"].Value.Trim());
        }

        CollectionAssert.AreEqual(headings, BortableSourceCatalog.GuideSections.Select(section => section.Heading).ToArray());
        Assert.HasCount(BortableSourceCatalog.GuideSections.Length, BortableSourceCatalog.GuideSections.Select(section => section.Key).Distinct(StringComparer.Ordinal), "Every section needs a key of its own.");
    }

    [TestMethod]
    public async Task Every_tool_is_reachable_as_a_plain_get_with_the_same_answer()
    {
        using var http = McpTestHost.CreateHttpClient();

        (string Tool, string Query, Dictionary<string, object?>? Arguments)[] calls =
        [
            ("SearchBortable", "?query=drag%20handle&limit=3", new() { ["query"] = "drag handle", ["limit"] = 3 }),
            ("GetBortableSetupGuide", "?renderMode=server", new() { ["renderMode"] = "server" }),
            ("GetBortableGuideSection", "?heading=Shared%20lists", new() { ["heading"] = "Shared lists" }),
            ("GetBortableApi", "?typeName=BortablePull", new() { ["typeName"] = "BortablePull" }),
            ("GetBortableTheme", "?name=--bit-bortable-accent-rgb", new() { ["name"] = "--bit-bortable-accent-rgb" }),
            ("InspectBortableDrag", "", null),
            ("GetBortableSourceFile", "?path=Demo/Client/Pages/Shared.razor", new() { ["path"] = "Demo/Client/Pages/Shared.razor" }),
        ];

        CollectionAssert.AreEquivalent(McpSurfaceTests.ExpectedTools, calls.Select(call => call.Tool).ToArray(),
            "The HTTP mirror is being exercised for a different set of tools than the server publishes.");

        foreach (var (tool, query, arguments) in calls)
        {
            using var response = await http.GetAsync($"/api/mcp/{tool}{query}");

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, tool);

            var body = await response.Content.ReadAsStringAsync();

            if (tool is "SearchBortable" or "InspectBortableDrag")
            {
                Assert.IsNotNull(JsonNode.Parse(body) as JsonObject, tool);
                continue;
            }

            Assert.AreEqual(await McpCall.TextAsync(tool, arguments), body, tool);
        }
    }

    [TestMethod]
    public async Task The_drag_lab_takes_its_json_from_the_query_string_too()
    {
        using var http = McpTestHost.CreateHttpClient();

        var json = """{ "lists": [{ "id": "a", "group": "g", "items": ["A"] }, { "id": "b", "group": "g", "put": false, "items": [] }], "drag": { "item": "A", "to": "b" } }""";

        var body = JsonNode.Parse(await http.GetStringAsync($"/api/mcp/InspectBortableDrag?json={Uri.EscapeDataString(json)}"))!.AsObject();

        Assert.IsTrue(body["valid"]!.GetValue<bool>());
        Assert.IsFalse(body["changed"]!.GetValue<bool>());
        Assert.AreEqual((await McpDragLabTests.InspectAsync(json)).Outcome, body["outcome"]!.GetValue<string>());
    }

    /// <summary>The fallback a rule writes for a property it reads, its parentheses counted.</summary>
    private static string FallbackOf(string css, string name)
    {
        var start = css.IndexOf($"var({name},", StringComparison.Ordinal);
        Assert.IsGreaterThanOrEqualTo(0, start, $"No rule reads {name} with a fallback.");

        start += name.Length + 5;
        var depth = 1;
        var end = start;

        for (; depth > 0; end++)
        {
            if (css[end] == '(') depth++;
            else if (css[end] == ')') depth--;
        }

        return css[start..(end - 1)].Trim();
    }

    [GeneratedRegex(@"--bit-bortable-[a-z0-9-]+")]
    private static partial Regex PropertyNameRegex();

    [GeneratedRegex(@"^\s*(?<name>--bit-bortable-[a-z0-9-]+)\s*:\s*(?<value>[^;]+);", RegexOptions.Multiline)]
    private static partial Regex DeclarationRegex();

    [GeneratedRegex(@"^\| `(?<name>--bit-bortable-[a-z0-9-]+)` \|(?<default>[^|]+)\|", RegexOptions.Multiline)]
    private static partial Regex ReadmeThemeRowRegex();

    [GeneratedRegex(@"\.(?<name>bit-bortable(?:-[a-z0-9]+)*)(?![a-z0-9-])")]
    private static partial Regex ClassRegex();

    [GeneratedRegex(@"^\| `(?<name>bit-bortable[a-z0-9-]*)`(?: \(`(?<parameter>\w+)`\))? \|", RegexOptions.Multiline)]
    private static partial Regex ReadmeClassRowRegex();

    [GeneratedRegex(@"^\| (?<names>`\w+`(?:, `\w+`)*) \| `(?<type>[^`]+)` \|(?<default>[^|]*)\|", RegexOptions.Multiline)]
    private static partial Regex TableRowRegex();

    [GeneratedRegex(@"^\| `(?<name>\w+)` \| `(?<type>[^`]+)` \|", RegexOptions.Multiline)]
    private static partial Regex ArgumentRowRegex();

    [GeneratedRegex(@"^\| `(?<name>\w+)` \| [^`|]", RegexOptions.Multiline)]
    private static partial Regex ValueRowRegex();

    [GeneratedRegex(@"`(?<code>\w+)`")]
    private static partial Regex CodeRegex();

    [GeneratedRegex(@"`(?<code>[^`]+)`")]
    private static partial Regex CodeFragmentRegex();

    [GeneratedRegex(@"^`[^`]+`$")]
    private static partial Regex WholeCodeRegex();

    [GeneratedRegex(@"(?<percent>\d+)%")]
    private static partial Regex PercentRegex();

    [GeneratedRegex(@",\s*(?<alpha>[0-9.]+)\s*\)\s*$")]
    private static partial Regex AlphaRegex();

    [GeneratedRegex(@"\bOn[A-Z][a-z]+\b")]
    private static partial Regex EventRegex();

    [GeneratedRegex(@"^#{2,4} (?<text>.+)$")]
    private static partial Regex HeadingRegex();
}
