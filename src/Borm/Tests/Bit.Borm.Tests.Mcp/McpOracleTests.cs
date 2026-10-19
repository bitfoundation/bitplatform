using System.Net;
using System.Reflection;
using System.Globalization;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Bit.Borm.Demo.Server.Services;
using Microsoft.AspNetCore.Components;

namespace Bit.Borm.Tests.Mcp;

/// <summary>
/// The server's claims put to the things they describe: the API reference against fresh instances
/// of the types it documents, the schema lab against the importer, checks and exporter it reports
/// on, the theme against the stylesheet it was read from (and the README's table of it), the
/// extensions against the guide's table of them, the index against the assemblies, and the HTTP
/// mirror against the protocol it mirrors. No expected answers are written down here - the library,
/// its files and the protocol are the expectations.
/// </summary>
[TestClass]
public partial class McpOracleTests
{
    // ---- The API reference -------------------------------------------------

    [TestMethod]
    public async Task Every_parameter_default_of_the_view_is_what_a_new_view_has()
    {
        // Over the wire, as an agent reads it, against an instance made here.
        var reference = await McpCall.TextAsync("GetBormApi", new() { ["typeName"] = nameof(BormView) });

        var checkedCount = AssertDefaultsMatch(new BormView(), reference, parametersOnly: true);

        Assert.IsGreaterThan(5, checkedCount, "Several of BormView's parameters are simple values whose defaults can be compared.");
    }

    [TestMethod]
    public void Every_parameter_default_of_every_component_is_what_a_new_instance_has()
    {
        foreach (var type in typeof(BormView).Assembly.GetExportedTypes().Where(type => BormApiCatalog.IsApi(type) && typeof(IComponent).IsAssignableFrom(type) && type.IsAbstract is false && type.IsGenericTypeDefinition is false))
        {
            AssertDefaultsMatch(Activator.CreateInstance(type)!, BormApiCatalog.RenderType(type.Name)!, parametersOnly: true);
        }
    }

    [TestMethod]
    public void The_defaults_of_the_types_built_from_arguments_are_what_the_library_builds()
    {
        // These have no parameterless constructor, so the reference builds them from the plainest
        // arguments it can find; here they are built the way an application builds them, and every
        // setting a caller can change has to start out where the reference says it does.
        using var state = new BormState(new BormDefinition(), new BormDictionaryDataAccessor());

        Assert.IsGreaterThan(4, AssertDefaultsMatch(state, BormApiCatalog.RenderType(nameof(BormState))!, parametersOnly: false));
        Assert.IsGreaterThan(8, AssertDefaultsMatch(new BormFieldDefinition("email", BormFieldType.Email), BormApiCatalog.RenderType(nameof(BormFieldDefinition))!, parametersOnly: false, "Name", "Type"));
        Assert.IsGreaterThan(0, AssertDefaultsMatch(new BormDefinition(), BormApiCatalog.RenderType(nameof(BormDefinition))!, parametersOnly: false));
    }

    /// <summary>
    /// Compares every settable member of a simple type - a number, a flag, a string, an enum - with the
    /// value it holds on the instance given, and returns how many it compared. The members the
    /// constructor was handed are left out: they hold what was passed, which is no default.
    /// </summary>
    private static int AssertDefaultsMatch(object instance, string reference, bool parametersOnly, params string[] passedIn)
    {
        var type = instance.GetType();
        var compared = 0;

        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (parametersOnly ? property.IsDefined(typeof(ParameterAttribute)) is false : property.SetMethod?.IsPublic is not true) continue;
            if (passedIn.Contains(property.Name)) continue;

            var value = property.GetValue(instance);

            string? expected = value switch
            {
                bool flag => flag ? "true" : "false",
                string text => $"\"{text}\"",
                Enum member => $"{member.GetType().Name}.{member}",
                double or int or long or float => ((IFormattable)value).ToString(null, CultureInfo.InvariantCulture),
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
        var exported = BormApiCatalog.Assemblies.SelectMany(assembly => assembly.GetExportedTypes())
            .Where(type => type.IsNested is false && type.Name != "_Imports")
            .Select(BormApiCatalog.FriendlyName)
            .ToHashSet(StringComparer.Ordinal);

        var indexed = BormApiCatalog.Types.Select(type => type.Name).ToHashSet(StringComparer.Ordinal);

        CollectionAssert.AreEquivalent(exported.ToArray(), indexed.ToArray());
        Assert.Contains(nameof(BormFluentValidationIntegration), indexed, "The FluentValidation integration is part of the API, though it ships separately.");
    }

    [TestMethod]
    public void Every_documented_public_type_has_its_summary_in_the_index()
    {
        // The XML documentation files have to reach the server's output folder for any of this to work;
        // a build that stops copying them leaves every summary empty without failing anything else. A
        // razor component cannot carry an XML summary on its class, so components are not counted.
        var types = BormApiCatalog.Types.Where(type => type.Kind != "Component").ToArray();
        var withSummary = types.Count(type => type.Summary is not null);

        Assert.IsGreaterThan(types.Length * 8 / 10, withSummary, "Most public types should come with their XML summary.");
        Assert.IsNotNull(BormApiCatalog.Types.Single(type => type.Name == nameof(BormFluentValidationIntegration)).Summary,
            "The integration's documentation lives in a file of its own, which has to be read as well.");
    }

    // ---- The schema lab ------------------------------------------------------

    /// <summary>
    /// The documents the demo's own JSON Schema page offers, read out of the page - realistic schemas
    /// written by hand, with conditions, steps and widgets in them.
    /// </summary>
    private static IEnumerable<(string Name, string Schema)> DemoSchemas()
    {
        var page = typeof(Bit.Borm.Demo.Client._Imports).Assembly.GetType("Bit.Borm.Demo.Client.Pages.JsonSchemaDemo")!;

        foreach (var field in page.GetFields(BindingFlags.NonPublic | BindingFlags.Static).Where(field => field.IsLiteral && field.Name.EndsWith("Schema", StringComparison.Ordinal)))
        {
            yield return (field.Name, (string)field.GetRawConstantValue()!);
        }
    }

    [TestMethod]
    public async Task The_schema_lab_agrees_with_the_importer_the_checks_and_the_exporter()
    {
        var documents = DemoSchemas().ToList();

        // Schemas the library writes itself, from the demo's own models: every one of them a document an
        // application gets by exporting a generated form.
        foreach (var model in typeof(Bit.Borm.Demo.Client._Imports).Assembly.GetTypes()
                     .Where(type => type.Namespace == "Bit.Borm.Demo.Client.Forms" && type.IsClass && type.IsAbstract is false && type.GetConstructor(Type.EmptyTypes) is not null)
                     .Where(type => type.GetProperties().Length > 0 && type.BaseType == typeof(object)))
        {
            documents.Add((model.Name, BormJsonSchemaExporter.Export(BormSchemaGenerator.Generate(model))));
        }

        Assert.IsGreaterThan(3, documents.Count, "The demo's JSON Schema page and models should give the lab something to chew on.");

        foreach (var (name, schema) in documents)
        {
            var report = await McpSchemaLabTests.InspectAsync(schema);

            Assert.IsTrue(BormJsonSchemaImporter.TryImport(schema, out var definition, out var error), $"{name}: {error}");
            Assert.IsTrue(report.Valid, $"{name}: {report.Error}");

            // Every field the importer built, and no other, depth first.
            Assert.AreEqual(Count(definition.Fields), report.FieldCount, name);
            CollectionAssert.AreEquivalent(definition.Fields.Select(field => field.Name).ToArray(), report.Fields!.Where(field => field.Path.Contains('.') is false && field.Path.Contains('[') is false).Select(field => field.Path).ToArray(), name);

            // The checks, word for word, and the document the exporter writes.
            CollectionAssert.AreEqual(definition.Validate().Select(diagnostic => diagnostic.ToString()).ToArray(),
                report.Diagnostics!.Select(diagnostic => $"[{diagnostic.Severity}] {(diagnostic.Path.Length == 0 ? "(form)" : diagnostic.Path)}: {diagnostic.Message}").ToArray(), name);
            Assert.AreEqual(BormJsonSchemaExporter.Export(definition).ReplaceLineEndings("\n"), report.Export, name);

            Assert.IsTrue(report.RoundTrips, $"{name} does not survive Export and a second import: {string.Join(" ", report.Notes)}");
        }

        static int Count(IEnumerable<BormFieldDefinition> fields) => fields.Sum(field => 1 + field.Type switch
        {
            BormFieldType.Object => Count(field.Children),
            _ when field.ItemTemplate is { Type: BormFieldType.Object } template => Count(template.Children),
            _ when field.ItemTemplate is not null => 1,
            _ => 0
        });
    }

    [TestMethod]
    public void The_extensions_the_lab_knows_are_the_ones_the_guide_documents()
    {
        // The lab reads the extensions out of the library's IL; the guide lists them by hand. Each is the
        // other's check: a literal the guide does not mention is an undocumented extension, and a row the
        // library has no literal for is documentation of something that does nothing.
        var section = BormSourceCatalog.GetGuideSection("Extensions and export")!;

        var documented = ExtensionRegex().Matches(section).Select(match => match.Value).ToHashSet(StringComparer.Ordinal);

        Assert.IsGreaterThan(20, BormSchemaLab.Extensions.Length);
        CollectionAssert.AreEquivalent(documented.ToArray(), BormSchemaLab.Extensions);
    }

    [GeneratedRegex(@"(?<=`)x-[A-Za-z]+(?=`)")]
    private static partial Regex ExtensionRegex();

    // ---- The theme and the stylesheet ------------------------------------------

    [TestMethod]
    public void Every_custom_property_the_form_declares_is_in_the_theme_catalog()
    {
        var css = BormSourceCatalog.Stylesheet;

        Assert.IsFalse(string.IsNullOrEmpty(css), "The shipped stylesheet is not embedded.");

        var declared = DeclarationRegex().Matches(css).Select(match => match.Groups["name"].Value).Distinct(StringComparer.Ordinal).ToArray();

        CollectionAssert.AreEquivalent(declared, BormThemeCatalog.Properties.Select(property => property.Name).ToArray());

        foreach (var property in BormThemeCatalog.Properties)
        {
            // The first declaration is the one on .bit-borm-form itself; a later one is a colour mode declaring it again.
            var value = DeclarationRegex().Matches(css).First(match => match.Groups["name"].Value == property.Name).Groups["value"].Value.Trim();

            Assert.AreEqual(value, property.Value, property.Name);
        }
    }

    [TestMethod]
    public void The_media_query_and_the_data_theme_switch_declare_the_same_dark_theme()
    {
        // The stylesheet writes the dark theme twice - once for the OS preference, once for an app's own
        // data-theme switch - because CSS cannot share a block between a media query and a selector. Two
        // lists kept by hand are exactly what drifts, so they are held to each other here.
        var system = Declarations(rule => rule.Media is not null && rule.Media.Contains("prefers-color-scheme: dark", StringComparison.Ordinal) && rule.Selector == BormThemeCatalog.RootSelector);
        var dark = Declarations(rule => rule.Media is null && rule.Selector.Contains("data-theme=\"dark\"", StringComparison.Ordinal));

        Assert.IsNotEmpty(dark);
        CollectionAssert.AreEquivalent(dark.ToArray(), system.ToArray());
    }

    [TestMethod]
    public void The_light_switch_restores_exactly_what_the_dark_theme_changes()
    {
        // data-theme="light" exists to beat the media query on a dark OS, so it has to put back every
        // property the dark theme changes - one it forgets stays dark in a "light" form - and to put each
        // back to the value the form declares.
        var dark = Declarations(rule => rule.Media is null && rule.Selector.Contains("data-theme=\"dark\"", StringComparison.Ordinal));
        var light = Declarations(rule => rule.Media is null && rule.Selector.Contains("data-theme=\"light\"", StringComparison.Ordinal));

        CollectionAssert.AreEquivalent(dark.Keys.ToArray(), light.Keys.ToArray());

        foreach (var (name, value) in light)
        {
            if (name.StartsWith("--", StringComparison.Ordinal)) Assert.AreEqual(BormThemeCatalog.Find(name)!.Value, value, name);
        }
    }

    private static Dictionary<string, string> Declarations(Func<BormThemeCatalog.CssRule, bool> where) => BormThemeCatalog.Rules
        .Where(where)
        .SelectMany(rule => rule.Declarations)
        .ToDictionary(declaration => declaration.Name, declaration => declaration.Value, StringComparer.Ordinal);

    [TestMethod]
    public void The_readme_table_of_custom_properties_agrees_with_the_stylesheet()
    {
        var rows = ReadmeThemeRowRegex().Matches(BormSourceCatalog.Readme).ToArray();

        Assert.IsNotEmpty(rows, "The README's table of custom properties is gone; this test has nothing left to compare.");

        foreach (var row in rows)
        {
            var property = BormThemeCatalog.Find(row.Groups["name"].Value);

            Assert.IsNotNull(property, $"The README documents {row.Groups["name"].Value}, which the stylesheet does not declare.");
            Assert.AreEqual(row.Groups["light"].Value, property.Value, $"{property.Name}: light");
            Assert.AreEqual(row.Groups["dark"].Value, property.DarkValue ?? property.Value, $"{property.Name}: dark");
            Assert.IsNotNull(property.Purpose, $"{property.Name}: the guide's description was not picked up.");
        }

        Assert.HasCount(BormThemeCatalog.Properties.Length, rows, "Every custom property should have its row in the guide's table.");
    }

    [TestMethod]
    public void Every_class_of_the_stylesheet_is_a_search_entry()
    {
        var css = BormSourceCatalog.Stylesheet;

        var classes = ClassRegex().Matches(css).Select(match => match.Value).Distinct(StringComparer.Ordinal).ToArray();
        var selectors = string.Join(' ', BormStylesheetClasses.Classes.Select(rule => rule.Selector));

        Assert.IsGreaterThan(40, classes.Length);
        foreach (var name in classes) Assert.Contains(name, selectors);
    }

    // ---- The guide, the demo and the HTTP mirror -------------------------------------

    [TestMethod]
    public void The_guide_sections_cover_every_heading_of_the_readme()
    {
        var readme = BormSourceCatalog.Readme.Replace("\r\n", "\n", StringComparison.Ordinal);

        Assert.IsFalse(string.IsNullOrEmpty(readme), "The README is not embedded.");

        var fenced = false;
        var headings = new List<string>();

        foreach (var line in readme.Split('\n'))
        {
            if (line.TrimStart().StartsWith("```", StringComparison.Ordinal)) { fenced = fenced is false; continue; }
            if (fenced is false && HeadingRegex().Match(line) is { Success: true } match) headings.Add(match.Groups["text"].Value.Trim());
        }

        CollectionAssert.AreEqual(headings, BormSourceCatalog.GuideSections.Select(section => section.Heading).ToArray());
        Assert.HasCount(BormSourceCatalog.GuideSections.Length, BormSourceCatalog.GuideSections.Select(section => section.Key).Distinct(StringComparer.Ordinal), "Every section needs a key of its own.");
    }

    [TestMethod]
    public void Every_page_of_the_demo_menu_has_its_source()
    {
        // The menu is the demo's own list of its forms; every one of them must be a file an agent can read.
        var menu = BormSourceCatalog.GetSourceFile("Demo/Client/Layout/NavMenu.razor")!;
        var routes = NavLinkRegex().Matches(menu).Select(match => $"/{match.Groups["route"].Value}").Where(route => route != "/mcp-server").ToArray();

        Assert.IsGreaterThan(15, routes.Length);

        foreach (var route in routes)
        {
            Assert.IsTrue(BormSourceCatalog.SourceFiles.Any(file => string.Equals(file.Route, route, StringComparison.OrdinalIgnoreCase)),
                $"The menu links to '{route}', and no embedded page declares it.");
        }
    }

    [TestMethod]
    public async Task Every_tool_is_reachable_as_a_plain_get_with_the_same_answer()
    {
        using var http = McpTestHost.CreateHttpClient();

        (string Tool, string Query, Dictionary<string, object?>? Arguments)[] calls =
        [
            ("SearchBorm", "?query=cascading%20options&limit=3", new() { ["query"] = "cascading options", ["limit"] = 3 }),
            ("GetBormSetupGuide", "?hostingModel=server", new() { ["hostingModel"] = "server" }),
            ("GetBormGuideSection", "?heading=Theming", new() { ["heading"] = "Theming" }),
            ("GetBormApi", "?typeName=BormView", new() { ["typeName"] = "BormView" }),
            ("GetBormTheme", "?property=--bit-borm-accent", new() { ["property"] = "--bit-borm-accent" }),
            ("InspectBormSchema", "", null),
            ("GetBormSourceFile", "?path=Demo/Client/Pages/WizardDemo.razor", new() { ["path"] = "Demo/Client/Pages/WizardDemo.razor" }),
        ];

        CollectionAssert.AreEquivalent(McpSurfaceTests.ExpectedTools, calls.Select(call => call.Tool).ToArray(),
            "The HTTP mirror is being exercised for a different set of tools than the server publishes.");

        foreach (var (tool, query, arguments) in calls)
        {
            using var response = await http.GetAsync($"/api/mcp/{tool}{query}");

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, tool);

            var body = await response.Content.ReadAsStringAsync();

            if (tool is "SearchBorm" or "InspectBormSchema")
            {
                Assert.IsNotNull(JsonNode.Parse(body) as JsonObject, tool);
                continue;
            }

            Assert.AreEqual(await McpCall.TextAsync(tool, arguments), body, tool);
        }
    }

    [GeneratedRegex(@"^\s*(?<name>--bit-borm-[a-z0-9-]+)\s*:\s*(?<value>[^;]+);", RegexOptions.Multiline)]
    private static partial Regex DeclarationRegex();

    [GeneratedRegex(@"^\| `(?<name>--bit-borm-[a-z0-9-]+)` \| `(?<light>[^`]+)` \| `(?<dark>[^`]+)` \|", RegexOptions.Multiline)]
    private static partial Regex ReadmeThemeRowRegex();

    [GeneratedRegex(@"\.bit-borm-[a-z0-9_-]+")]
    private static partial Regex ClassRegex();

    [GeneratedRegex(@"^#{2,4} (?<text>.+)$")]
    private static partial Regex HeadingRegex();

    [GeneratedRegex(@"<NavLink href=""(?<route>[^""]+)""")]
    private static partial Regex NavLinkRegex();
}
