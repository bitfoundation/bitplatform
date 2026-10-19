using System.Numerics;
using System.Text.Json.Nodes;
using Bit.Bink.Demo.Server.Dtos;

namespace Bit.Bink.Tests.Mcp;

/// <summary>
/// InspectBinkDocument on the documents an application actually meets: one it wrote itself, one an
/// older or newer version wrote, one with a custom brush or element it does not know, and input
/// that is not a document at all. The documents are built with the library and then edited as JSON,
/// so each test changes exactly the one thing it is about.
/// </summary>
[TestClass]
public class McpDocumentLabTests
{
    [TestMethod]
    public async Task Without_a_document_it_hands_out_a_sample_the_library_reads()
    {
        var report = await InspectAsync(null);

        Assert.IsFalse(report.Valid);
        Assert.IsNotNull(report.Sample);
        Assert.IsTrue(BinkSerializer.TryDeserialize(report.Sample, out var sample, out var error), error);
        Assert.AreEqual(4, sample!.ElementCount);
    }

    [TestMethod]
    public async Task The_sample_is_reported_element_by_element()
    {
        var sample = (await InspectAsync(null)).Sample!;

        var report = await InspectAsync(sample);

        Assert.IsTrue(report.Valid);
        Assert.AreEqual("bink", report.Format);
        Assert.AreEqual(BinkSerializer.CurrentVersion, report.SchemaVersion);
        Assert.AreEqual(320, report.Width);
        Assert.AreEqual(200, report.Height);
        Assert.AreEqual(2, Count(report.Elements, "Stroke"));
        Assert.AreEqual(1, Count(report.Elements, "Rectangle"));
        Assert.AreEqual(1, Count(report.Elements, "Text"));
        Assert.AreEqual(1, Count(report.BrushKinds, BinkPenBrush.KindName));
        Assert.AreEqual(1, Count(report.BrushKinds, BinkHighlighterBrush.KindName));
        Assert.AreEqual(2, Count(report.PointEncodings, "json"));
        Assert.HasCount(1, report.Layers!);
        Assert.IsTrue(report.RoundTripStable);
        Assert.IsLessThan(report.ReadableBytes!.Value, report.CompactBytes!.Value, "Binary points should be smaller than readable JSON points.");
    }

    [TestMethod]
    public async Task Text_the_server_side_png_path_cannot_draw_is_reported()
    {
        var report = await InspectAsync((await InspectAsync(null)).Sample);

        Assert.IsNotNull(report.Png);
        Assert.AreEqual(1, report.Png.SkippedTextElements);
        Assert.AreEqual(320, report.Png.Width);
        Assert.AreEqual(200, report.Png.Height);
        Assert.IsTrue(report.Notes.Any(note => note.Contains("TextRasterizer", StringComparison.Ordinal)));
    }

    [TestMethod]
    public async Task Input_that_is_not_json_is_rejected_with_the_serializers_own_message()
    {
        var report = await InspectAsync("{ not json");

        Assert.IsFalse(report.Valid);
        Assert.IsNotNull(report.Error);
        BinkSerializer.TryDeserialize("{ not json", out _, out var expected);
        Assert.AreEqual(expected, report.Error);
    }

    [TestMethod]
    public async Task Json_that_is_not_a_bink_document_says_so()
    {
        var report = await InspectAsync("""{ "format": "excalidraw", "version": 2 }""");

        Assert.IsFalse(report.Valid);
        Assert.AreEqual("excalidraw", report.Format);
        Assert.Contains("format", report.Error!);
    }

    [TestMethod]
    public async Task A_document_from_a_newer_version_names_the_versions_this_build_reads()
    {
        var json = Edit(Document(), root => root["version"] = BinkSerializer.CurrentVersion + 1);

        var report = await InspectAsync(json);

        Assert.IsFalse(report.Valid);
        Assert.AreEqual(BinkSerializer.CurrentVersion + 1, report.SchemaVersion);
        Assert.Contains("newer version", report.Error!);
        Assert.AreEqual($"{BinkSerializer.MinimumSupportedVersion}..{BinkSerializer.CurrentVersion}", report.SupportedVersions);
    }

    [TestMethod]
    public async Task An_unregistered_brush_kind_is_named_even_though_it_loads_as_a_pen()
    {
        var json = Edit(Document(), root => root["layers"]![0]!["elements"]![0]!["brush"]!["kind"] = "crayon");

        var report = await InspectAsync(json);

        Assert.IsTrue(report.Valid);
        Assert.AreEqual(1, Count(report.BrushKinds, "crayon"));
        Assert.IsTrue(report.Notes.Any(note => note.Contains("'crayon' is not registered", StringComparison.Ordinal)));
    }

    [TestMethod]
    public async Task An_unknown_element_type_is_kept_and_reported()
    {
        var json = Edit(Document(), root => root["layers"]![0]!["elements"]!.AsArray().Add(new JsonObject
        {
            ["type"] = "sticker",
            ["id"] = Guid.NewGuid().ToString(),
            ["emoji"] = "star"
        }));

        var report = await InspectAsync(json);

        Assert.IsTrue(report.Valid);
        Assert.AreEqual(1, Count(report.Elements, "Unknown (sticker)"));
        Assert.IsTrue(report.RoundTripStable);
        Assert.IsTrue(report.Notes.Any(note => note.Contains("'sticker'", StringComparison.Ordinal) && note.Contains("BinkUnknownElement", StringComparison.Ordinal)));
    }

    [TestMethod]
    public async Task Ink_outside_the_page_and_hidden_layers_are_pointed_out()
    {
        var document = new BinkDocument(new BinkDocumentSettings { Width = 200, Height = 100 });
        document.AddElement(new BinkStroke(BinkStrokePoints.FromPositions([new Vector2(500, 500), new Vector2(600, 520)]), new BinkPenBrush()));
        var hidden = document.AddLayer(new BinkLayerProperties { Name = "Notes", IsVisible = false });
        document.AddElement(new BinkRectangleShape(new BinkRect(10, 10, 20, 20)), hidden.Id);

        var report = await InspectAsync(BinkSerializer.Serialize(document));

        Assert.IsTrue(report.Notes.Any(note => note.Contains("outside the 200 x 100 page", StringComparison.Ordinal)));
        Assert.IsTrue(report.Notes.Any(note => note.Contains("'Notes'", StringComparison.Ordinal) && note.Contains("IncludeHiddenLayers", StringComparison.Ordinal)));
        Assert.IsFalse(report.Layers!.Single(layer => layer.Name == "Notes").Visible);
    }

    [TestMethod]
    public async Task The_svg_is_returned_only_when_asked_for_and_follows_the_region()
    {
        var json = Document();

        var without = await InspectAsync(json);
        var page = await InspectAsync(json, includeSvg: true);
        var content = await InspectAsync(json, includeSvg: true, region: "Content");

        Assert.IsNull(without.Svg);
        Assert.StartsWith("<svg", page.Svg!);
        Assert.AreNotEqual(page.Svg, content.Svg);
    }

    [TestMethod]
    public async Task A_document_past_the_tool_limit_is_refused_with_the_reason()
    {
        var huge = new string(' ', 2 * 1024 * 1024 + 1) + Document();

        var report = await InspectAsync(huge);

        Assert.IsFalse(report.Valid);
        Assert.IsTrue(report.Notes.Any(note => note.Contains("2097152 bytes", StringComparison.Ordinal)));
    }

    internal static Task<BinkDocumentReportDto> InspectAsync(string? json, bool includeSvg = false, string? region = null)
    {
        var arguments = new Dictionary<string, object?>();
        if (json is not null) arguments["json"] = json;
        if (includeSvg) arguments["includeSvg"] = true;
        if (region is not null) arguments["region"] = region;

        return McpCall.StructuredAsync<BinkDocumentReportDto>("InspectBinkDocument", arguments);
    }

    /// <summary>A small document: one pen stroke near the top left of an 800 x 600 page.</summary>
    private static string Document()
    {
        var document = new BinkDocument();
        document.AddElement(new BinkStroke(BinkStrokePoints.FromPositions([new Vector2(10, 10), new Vector2(60, 40), new Vector2(120, 30)]), new BinkPenBrush()));

        return BinkSerializer.Serialize(document);
    }

    private static string Edit(string json, Action<JsonNode> edit)
    {
        var root = JsonNode.Parse(json)!;
        edit(root);
        return root.ToJsonString();
    }

    private static int Count(BinkCountDto[]? counts, string name) => counts?.SingleOrDefault(count => count.Name == name)?.Count ?? 0;
}
