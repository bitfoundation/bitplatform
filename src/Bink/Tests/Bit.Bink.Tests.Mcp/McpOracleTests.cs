using System.Net;
using System.Numerics;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Bit.Bink.Demo.Server.Services;

namespace Bit.Bink.Tests.Mcp;

/// <summary>
/// The server's claims put to the things they describe: the document lab against the serializer
/// and exporters it runs, the brush catalog against the registry it was read from, the API index
/// against the assembly, the stylesheet classes against the stylesheet, and the HTTP mirror against
/// the protocol it mirrors. No expected answers are written down here - the library, its files and
/// the protocol are the expectations.
/// </summary>
[TestClass]
public partial class McpOracleTests
{
    [TestMethod]
    public async Task The_document_lab_agrees_with_the_serializer_and_the_exporters()
    {
        var random = new Random(20261016);

        for (var i = 0; i < 8; i++)
        {
            var document = RandomDocument(random, i);
            var json = BinkSerializer.Serialize(document, i % 2 == 0 ? BinkSerializerOptions.Default : BinkSerializerOptions.Readable);

            var report = await McpDocumentLabTests.InspectAsync(json, includeSvg: true);

            // What an application gets when it loads the same text.
            var loaded = BinkSerializer.Deserialize(json);
            var png = BinkPngExporter.ExportWithDetails(loaded);
            var context = $"document {i}";

            Assert.IsTrue(report.Valid, context);
            Assert.AreEqual(loaded.Id.ToString(), report.Id, context);
            Assert.AreEqual(loaded.ElementCount, report.Elements!.Sum(count => count.Count), context);
            Assert.AreEqual(loaded.TotalPointCount, report.TotalPoints, context);
            Assert.AreEqual(loaded.Layers.Count, report.Layers!.Length, context);
            Assert.AreEqual(Encoding.UTF8.GetByteCount(BinkSerializer.Serialize(loaded)), report.CompactBytes, context);
            Assert.AreEqual(Encoding.UTF8.GetByteCount(BinkSerializer.Serialize(loaded, BinkSerializerOptions.Readable)), report.ReadableBytes, context);
            Assert.AreEqual(png.Width, report.Png!.Width, context);
            Assert.AreEqual(png.Height, report.Png.Height, context);
            Assert.AreEqual(png.SkippedTextElements, report.Png.SkippedTextElements, context);
            Assert.AreEqual(png.SkippedImages, report.Png.SkippedImages, context);
            var svg = BinkSvgExporter.Export(loaded);

            if (svg.Length <= BinkDocumentLab.MaxSvgLength) Assert.AreEqual(svg, report.Svg, context);
            else Assert.StartsWith(report.Svg!, svg, context);

            foreach (var group in loaded.AllElements.GroupBy(element => element.TypeName))
            {
                Assert.AreEqual(group.Count(), report.Elements!.Single(count => count.Name == group.Key).Count, $"{context}: {group.Key}");
            }
        }
    }

    [TestMethod]
    public void The_brush_catalog_is_the_default_registry()
    {
        CollectionAssert.AreEquivalent(BinkBrushRegistry.Default.Kinds.ToArray(), BinkBrushCatalog.Brushes.Select(brush => brush.Kind).ToArray());

        foreach (var entry in BinkBrushCatalog.Brushes)
        {
            Assert.IsTrue(BinkBrushRegistry.Default.TryCreate(entry.Kind, out var brush));

            Assert.AreEqual(brush!.GetType().Name, entry.TypeName);
            Assert.AreEqual(brush.Color.ToCss(), entry.Color, entry.Kind);
            Assert.AreEqual(brush.Width, entry.Width, entry.Kind);
            Assert.AreEqual(brush.Opacity, entry.Opacity, entry.Kind);
            Assert.AreEqual(brush.BlendMode.ToString(), entry.BlendMode, entry.Kind);
            Assert.AreEqual(brush.Smoothing, entry.Smoothing, entry.Kind);
            Assert.AreEqual(brush.Spacing, entry.Spacing, entry.Kind);
            Assert.AreEqual(brush.SimulatePressure, entry.SimulatePressure, entry.Kind);
            Assert.AreEqual(brush.TaperStart, entry.TaperStart, entry.Kind);
            Assert.AreEqual(brush.TaperEnd, entry.TaperEnd, entry.Kind);

            // The serialized fragment is what the serializer writes, so reading a stroke with it back
            // must produce the same brush.
            using var fragment = JsonDocument.Parse(entry.Serialized);
            Assert.AreEqual(entry.Kind, fragment.RootElement.GetProperty("kind").GetString());

            var stroke = new BinkStroke(BinkStrokePoints.FromPositions([new Vector2(0, 0), new Vector2(10, 10)]), brush);
            var document = new BinkDocument();
            document.AddElement(stroke);

            var roundTripped = (BinkStroke)BinkSerializer.Deserialize(BinkSerializer.Serialize(document)).AllElements.Single();
            Assert.AreEqual(brush, roundTripped.Brush, entry.Kind);
        }
    }

    [TestMethod]
    public void Every_exported_type_is_in_the_api_index()
    {
        var exported = typeof(BinkDocument).Assembly.GetExportedTypes()
            .Where(type => type.IsNested is false)
            .Select(type => BinkApiCatalog.FriendlyName(type))
            .ToHashSet(StringComparer.Ordinal);

        var indexed = BinkApiCatalog.Types.Select(type => type.Name).ToHashSet(StringComparer.Ordinal);

        CollectionAssert.AreEquivalent(exported.ToArray(), indexed.ToArray());
    }

    [TestMethod]
    public void Every_documented_public_type_has_its_summary_in_the_index()
    {
        // The XML documentation file has to reach the server's output folder for any of this to work;
        // a build that stops copying it leaves every summary empty without failing anything else.
        var withSummary = BinkApiCatalog.Types.Count(type => type.Summary is not null);

        Assert.IsGreaterThan(BinkApiCatalog.Types.Length * 9 / 10, withSummary, "Most public types should come with their XML summary.");
    }

    [TestMethod]
    public void Every_class_of_the_stylesheet_is_a_search_entry()
    {
        var css = BinkSourceCatalog.Stylesheet;

        Assert.IsFalse(string.IsNullOrEmpty(css), "The shipped stylesheet is not embedded.");

        var classes = ClassRegex().Matches(css).Select(match => match.Value).Distinct(StringComparer.Ordinal).ToArray();
        var selectors = string.Join(' ', BinkStylesheetClasses.Classes.Select(rule => rule.Selector));

        Assert.IsGreaterThan(5, classes.Length);
        foreach (var name in classes) Assert.Contains(name, selectors);
    }

    [TestMethod]
    public void The_guide_sections_cover_every_heading_of_the_readme()
    {
        var readme = BinkSourceCatalog.Readme.Replace("\r\n", "\n", StringComparison.Ordinal);

        Assert.IsFalse(string.IsNullOrEmpty(readme), "The README is not embedded.");

        var fenced = false;
        var headings = new List<string>();

        foreach (var line in readme.Split('\n'))
        {
            if (line.TrimStart().StartsWith("```", StringComparison.Ordinal)) { fenced = fenced is false; continue; }
            if (fenced is false && HeadingRegex().Match(line) is { Success: true } match) headings.Add(match.Groups["text"].Value.Trim());
        }

        CollectionAssert.AreEqual(headings, BinkSourceCatalog.GuideSections.Select(section => section.Heading).ToArray());
        Assert.HasCount(BinkSourceCatalog.GuideSections.Length, BinkSourceCatalog.GuideSections.Select(section => section.Key).Distinct(StringComparer.Ordinal), "Every section needs a key of its own.");
    }

    [TestMethod]
    public async Task Every_tool_is_reachable_as_a_plain_get_with_the_same_answer()
    {
        using var http = McpTestHost.CreateHttpClient();

        (string Tool, string Query, Dictionary<string, object?>? Arguments)[] calls =
        [
            ("SearchBink", "?query=signature&limit=3", new() { ["query"] = "signature", ["limit"] = 3 }),
            ("GetBinkSetupGuide", "?renderMode=server", new() { ["renderMode"] = "server" }),
            ("GetBinkGuideSection", "?heading=Serialization", new() { ["heading"] = "Serialization" }),
            ("GetBinkApi", "?typeName=BinkCanvas", new() { ["typeName"] = "BinkCanvas" }),
            ("GetBinkBrushes", "?kind=pen", new() { ["kind"] = "pen" }),
            ("InspectBinkDocument", "", null),
            ("GetBinkSourceFile", "?path=Demo/Client/Pages/Signature.razor", new() { ["path"] = "Demo/Client/Pages/Signature.razor" }),
        ];

        CollectionAssert.AreEquivalent(McpSurfaceTests.ExpectedTools, calls.Select(call => call.Tool).ToArray(),
            "The HTTP mirror is being exercised for a different set of tools than the server publishes.");

        foreach (var (tool, query, arguments) in calls)
        {
            using var response = await http.GetAsync($"/api/mcp/{tool}{query}");

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, tool);

            var body = await response.Content.ReadAsStringAsync();

            if (tool is "SearchBink" or "InspectBinkDocument")
            {
                Assert.StartsWith("{", body, tool);
                continue;
            }

            Assert.AreEqual(await McpCall.TextAsync(tool, arguments), body, tool);
        }
    }

    /// <summary>
    /// A document with some of everything the lab reports on: strokes with every built-in brush, shapes,
    /// text, a second layer that is sometimes hidden, and - in every other document - an unbounded page.
    /// </summary>
    private static BinkDocument RandomDocument(Random random, int seed)
    {
        var settings = seed % 3 == 2
            ? new BinkDocumentSettings { Width = 0, Height = 0 }
            : new BinkDocumentSettings { Width = random.Next(200, 900), Height = random.Next(150, 700) };

        var document = new BinkDocument(settings);
        var kinds = BinkBrushRegistry.Default.Kinds.Order(StringComparer.Ordinal).ToArray();

        for (var i = 0; i < random.Next(2, 6); i++)
        {
            var points = Enumerable.Range(0, random.Next(2, 30)).Select(_ => new Vector2(random.Next(0, 800), random.Next(0, 600))).ToArray();

            BinkBrushRegistry.Default.TryCreate(kinds[random.Next(kinds.Length)], out var brush);
            document.AddElement(new BinkStroke(BinkStrokePoints.FromPositions(points), brush!));
        }

        document.AddElement(new BinkEllipseShape(new BinkRect(random.Next(0, 400), random.Next(0, 300), 40, 30)));

        if (seed % 2 == 1) document.AddElement(new BinkTextElement(new Vector2(random.Next(0, 300), random.Next(0, 200)), "Note"));

        var second = document.AddLayer(new BinkLayerProperties { Name = "Overlay", IsVisible = seed % 4 != 3 });
        document.AddElement(new BinkRectangleShape(new BinkRect(random.Next(0, 500), random.Next(0, 400), 60, 20)), second.Id);

        return document;
    }

    [GeneratedRegex(@"\.bit-bink-[a-z0-9-]+")]
    private static partial Regex ClassRegex();

    [GeneratedRegex(@"^#{2,4} (?<text>.+)$")]
    private static partial Regex HeadingRegex();
}
