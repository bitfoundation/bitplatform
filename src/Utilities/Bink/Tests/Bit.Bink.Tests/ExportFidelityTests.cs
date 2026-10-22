using System.Globalization;
using System.Numerics;
using System.Xml.Linq;

namespace Bit.Bink.Tests;

/// <summary>
/// SVG export must show what the canvas and the rasterizer show: the same layer isolation, the same text lines,
/// the same crops, and the same erasing however many eraser strokes a layer holds.
/// </summary>
[TestClass]
public class ExportFidelityTests
{
    private static readonly XNamespace Svg = "http://www.w3.org/2000/svg";
    private static readonly XNamespace XLink = "http://www.w3.org/1999/xlink";

    /// <summary>
    /// Every SVG layer group was isolated, while the canvas and the rasterizer isolate only a layer with an eraser
    /// stroke or an opacity below 1. A multiply highlight on an upper layer then saw only its own empty group in
    /// SVG and composited normally, turning black text under it olive instead of keeping it black.
    /// </summary>
    [TestMethod]
    public void Svg_isolates_exactly_the_layers_the_renderer_isolates()
    {
        var document = new BinkDocument(new BinkDocumentSettings { Width = 200, Height = 100, Background = BinkColor.White });
        Guid page = document.Layers[0].Id;
        document.UpdateLayer(page, p => p with { Name = "Page" });
        document.AddElement(TestData.Stroke(new Vector2(10, 50), new Vector2(190, 50), new BinkPenBrush { Width = 6f }), page);

        BinkLayer highlights = document.AddLayer("Highlights");
        document.AddElement(new BinkHighlightElement(new BinkRect(20, 40, 160, 20)), highlights.Id);
        document.AddElement(TestData.Stroke(new Vector2(10, 20), new Vector2(190, 20), new BinkHighlighterBrush()), highlights.Id);

        BinkLayer faded = document.AddLayer("Faded");
        document.UpdateLayer(faded.Id, p => p with { Opacity = 0.5f });
        document.AddElement(TestData.Stroke(new Vector2(10, 80), new Vector2(190, 80), new BinkPenBrush()), faded.Id);

        BinkLayer erased = document.AddLayer("Erased");
        document.AddElement(TestData.Stroke(new Vector2(10, 90), new Vector2(190, 90), new BinkPenBrush()), erased.Id);
        document.AddElement(TestData.Stroke(new Vector2(100, 70), new Vector2(100, 99), new BinkPixelEraserBrush()), erased.Id);

        XDocument svg = XDocument.Parse(BinkSvgExporter.Export(document));
        var groups = svg.Descendants(Svg + "g").Where(g => g.Attribute("data-layer") is not null).ToDictionary(g => g.Attribute("data-layer")!.Value);
        Assert.HasCount(4, groups);

        foreach (BinkLayer layer in document.Layers)
        {
            // The canvas command buffer starts with a group exactly when the renderer isolates the layer.
            var canvas = new BinkCanvasRenderContext();
            BinkDocumentRenderer.RenderLayer(layer, canvas);
            bool rendererIsolates = canvas.Commands[0] == (double)BinkCanvasCommand.PushGroup;

            bool svgIsolates = (groups[layer.Name].Attribute("style")?.Value ?? string.Empty).Contains("isolation:isolate", StringComparison.Ordinal);
            Assert.AreEqual(rendererIsolates, svgIsolates, $"layer \"{layer.Name}\"");
        }

        // The case that matters visually: the multiply highlight must be free to blend with the page below it.
        Assert.IsNull(groups["Highlights"].Attribute("style"));
    }

    /// <summary>
    /// A blank line became an empty tspan positioned by a relative dy, which viewers drop, so the line after it
    /// moved up by one line.
    /// </summary>
    [TestMethod]
    public void Blank_lines_keep_their_height_in_svg_text()
    {
        var style = BinkTextStyle.Default with { FontSize = 20, LineHeight = 1.5f };
        XElement text = ExportText("a\n\nb", style, new Vector2(10, 30));

        float lineHeight = style.FontSize * style.LineHeight;
        float firstBaseline = 30 + style.FontSize * 0.82f;
        XElement b = text.Elements(Svg + "tspan").Single(t => t.Value == "b");
        Assert.AreEqual(firstBaseline + 2 * lineHeight, ParseNumber(b.Attribute("y")), 0.02f);
        foreach (XElement line in text.Elements(Svg + "tspan")) Assert.IsNull(line.Attribute("dy"), "a relative offset depends on the previous line being drawn");
    }

    /// <summary>
    /// The canvas and the size estimate break lines only at '\n'. The SVG writer also broke at '\r', '\f', U+0085,
    /// U+2028 and U+2029, so the same text had a different number of lines in the exported file.
    /// </summary>
    [TestMethod]
    [DataRow("a\u2028b", new[] { "a\u2028b" })]
    [DataRow("a\u2029b\u0085c", new[] { "a\u2029b\u0085c" })]
    [DataRow("a\fb", new[] { "a b" })]
    [DataRow("a\r\nb\r\n\r\nc", new[] { "a", "b", "c" })]
    public void Svg_text_breaks_lines_where_the_canvas_does(string value, string[] expectedLines)
    {
        var style = BinkTextStyle.Default with { FontSize = 10, LineHeight = 1f };
        XElement text = ExportText(value, style, Vector2.Zero);
        string[] lines = text.Elements(Svg + "tspan").Select(t => t.Value).ToArray();
        Assert.AreSequenceEqual(expectedLines, lines);

        // The last line sits as many lines down as the canvas puts it.
        int canvasLines = value.Split('\n').Length;
        Assert.AreEqual(style.FontSize * 0.82f + (canvasLines - 1) * style.FontSize, ParseNumber(text.Elements(Svg + "tspan").Last().Attribute("y")), 0.02f);
    }

    /// <summary>
    /// More than 256 eraser strokes in a layer dropped every mask, so all the erased ink came back. The pen's
    /// eraser end makes one eraser stroke per pointer-up, so a long session gets there.
    /// </summary>
    [TestMethod]
    public void Hundreds_of_consecutive_erasers_still_erase_in_svg()
    {
        var document = new BinkDocument(new BinkDocumentSettings { Width = 400, Height = 100 });
        document.AddElement(TestData.Stroke(new Vector2(0, 50), new Vector2(400, 50), new BinkPenBrush { Width = 20f }));
        for (int i = 0; i < 300; i++) document.AddElement(TestData.Stroke(new Vector2(i, 30), new Vector2(i, 70), new BinkPixelEraserBrush { Width = 1f }, 4));

        XDocument svg = XDocument.Parse(BinkSvgExporter.Export(document));

        // A run of erasers with nothing drawn between them is one erase, so it is one mask holding all of them.
        XElement mask = Assert.ContainsSingle(svg.Descendants(Svg + "mask"));
        Assert.HasCount(300, mask.Elements().Where(e => e.Name != Svg + "rect").SelectMany(e => e.DescendantsAndSelf(Svg + "path")));
        XElement ink = svg.Descendants(Svg + "path").Single(p => !p.Ancestors(Svg + "mask").Any());
        Assert.IsTrue(ink.Ancestors(Svg + "g").Any(g => g.Attribute("mask") is not null), "the ink is not masked at all");
    }

    [TestMethod]
    public void Interleaved_drawing_and_erasing_past_the_nesting_limit_never_brings_erased_ink_back()
    {
        const int alternations = 300;
        var document = new BinkDocument(new BinkDocumentSettings { Width = 3200, Height = 100 });
        var erasers = new List<BinkStroke>();
        for (int i = 0; i < alternations; i++)
        {
            document.AddElement(TestData.Stroke(new Vector2(i * 10, 50), new Vector2(i * 10 + 400, 50), new BinkPenBrush { Width = 4f }, 4) with { Description = "ink " + i.ToString(CultureInfo.InvariantCulture) });
            BinkStroke eraser = TestData.Stroke(new Vector2(i * 10 + 5, 30), new Vector2(i * 10 + 5, 70), new BinkPixelEraserBrush { Width = 2f }, 4);
            erasers.Add(eraser);
            document.AddElement(eraser);
        }
        Dictionary<string, int> eraserIndex = erasers.Select((e, i) => (Data: PathData(e), Index: i)).ToDictionary(p => p.Data, p => p.Index);

        XDocument svg = XDocument.Parse(BinkSvgExporter.Export(document, new BinkSvgExportOptions { IncludeElementDescriptions = true }));
        var masks = svg.Descendants(Svg + "mask").ToDictionary(m => m.Attribute("id")!.Value, m => m.Descendants(Svg + "path").Select(p => eraserIndex[p.Attribute("d")!.Value]).ToHashSet());
        Assert.AreEqual(alternations, masks.Values.Sum(m => m.Count), "every eraser must reach a mask exactly once");

        int deepest = 0;
        foreach (XElement described in svg.Descendants(Svg + "g").Where(g => g.Attribute("role")?.Value == "img"))
        {
            int ink = int.Parse(described.Element(Svg + "title")!.Value["ink ".Length..], CultureInfo.InvariantCulture);
            var maskedBy = described.Ancestors(Svg + "g").Select(g => g.Attribute("mask")?.Value).Where(v => v is not null).ToList();
            deepest = Math.Max(deepest, maskedBy.Count);
            var applied = maskedBy.SelectMany(v => masks[v!["url(#".Length..^1]]).ToHashSet();

            // Everything erased after the ink was drawn must apply to it.
            for (int e = ink; e < alternations; e++) Assert.Contains(e, applied, $"eraser {e} no longer applies to ink {ink}");
            // Below the nesting limit the export is exact: nothing erased before the ink was drawn applies to it.
            if (ink < 239) Assert.IsFalse(applied.Any(e => e < ink), $"ink {ink} is masked by an eraser drawn before it");
        }
        Assert.IsTrue(deepest <= 240, $"masks nest {deepest} deep");
    }

    /// <summary>
    /// With the image's natural size unknown, SVG could not place the oversized image behind a clip, so it
    /// squeezed the whole image into the destination where the canvas shows only the crop.
    /// </summary>
    [TestMethod]
    public void A_crop_applies_when_the_natural_size_is_unknown()
    {
        byte[] png = TestData.CreatePng(8, 8, (x, _) => x < 4 ? BinkColor.Black : BinkColor.White);
        var source = BinkImageSource.FromUri("data:image/png;base64," + Convert.ToBase64String(png));
        Assert.AreEqual(0, source.Width);
        var document = new BinkDocument(new BinkDocumentSettings { Width = 100, Height = 100 });
        document.AddElement(new BinkImageElement(source, new BinkRect(10, 20, 40, 30)) { Crop = new BinkRect(2, 1, 4, 3) });

        XDocument svg = XDocument.Parse(BinkSvgExporter.Export(document));
        XElement image = Assert.ContainsSingle(svg.Descendants(Svg + "image"));
        XElement viewport = image.Parent!;
        Assert.AreEqual(Svg + "svg", viewport.Name, "the crop needs a viewport of its own");
        Assert.AreEqual("2 1 4 3", viewport.Attribute("viewBox")?.Value);
        Assert.AreEqual("none", viewport.Attribute("preserveAspectRatio")?.Value);
        Assert.AreEqual(10f, ParseNumber(viewport.Attribute("x")));
        Assert.AreEqual(20f, ParseNumber(viewport.Attribute("y")));
        Assert.AreEqual(40f, ParseNumber(viewport.Attribute("width")));
        Assert.AreEqual(30f, ParseNumber(viewport.Attribute("height")));
        // The image takes its own size in the viewport's coordinates, which are image pixels.
        Assert.IsNull(image.Attribute("width"));
        Assert.IsNull(image.Attribute("height"));
    }

    /// <summary>The canvas draws nothing for a crop with no area; SVG drew the whole image.</summary>
    [TestMethod]
    [DataRow(8, 8)]
    [DataRow(0, 0)]
    public void A_crop_with_no_area_draws_nothing(int naturalWidth, int naturalHeight)
    {
        byte[] png = TestData.CreatePng(8, 8, (_, _) => BinkColor.Black);
        var source = BinkImageSource.FromUri("data:image/png;base64," + Convert.ToBase64String(png), naturalWidth, naturalHeight);
        var document = new BinkDocument(new BinkDocumentSettings { Width = 100, Height = 100 });
        document.AddElement(new BinkImageElement(source, new BinkRect(10, 20, 40, 30)) { Crop = new BinkRect(2, 1, 0, 3) });

        XDocument svg = XDocument.Parse(BinkSvgExporter.Export(document));
        Assert.IsEmpty(svg.Descendants(Svg + "image"));
    }

    /// <summary>The file declared SVG 1.1 but referenced images only with the SVG 2 <c>href</c>.</summary>
    [TestMethod]
    public void Images_carry_an_xlink_reference_for_svg_1_1_consumers()
    {
        byte[] png = TestData.CreatePng(4, 4, (_, _) => BinkColor.Black);
        var source = BinkImageSource.FromBytes(png, "image/png", 4, 4);
        var document = new BinkDocument(new BinkDocumentSettings { Width = 100, Height = 100 });
        document.AddElement(new BinkImageElement(source, new BinkRect(0, 0, 50, 50)));
        document.AddElement(new BinkImageElement(source, new BinkRect(50, 50, 50, 50)) { Crop = new BinkRect(1, 1, 2, 2) });

        XDocument svg = XDocument.Parse(BinkSvgExporter.Export(document));
        Assert.HasCount(2, svg.Descendants(Svg + "image"));
        foreach (XElement image in svg.Descendants(Svg + "image"))
        {
            Assert.AreEqual(source.Uri, image.Attribute("href")?.Value);
            Assert.AreEqual(source.Uri, image.Attribute(XLink + "href")?.Value);
        }
    }

    [TestMethod]
    public void A_standalone_svg_fragment_with_an_image_is_well_formed()
    {
        // A render context used outside the exporter has no root element declaring the xlink prefix for it.
        byte[] png = TestData.CreatePng(4, 4, (_, _) => BinkColor.Black);
        using var writer = new StringWriter(CultureInfo.InvariantCulture);
        var context = new BinkSvgRenderContext(writer);
        context.DrawImage(BinkImageSource.FromBytes(png, "image/png", 4, 4), new BinkRect(0, 0, 10, 10), null, 1f);

        XElement image = XElement.Parse(writer.ToString());
        Assert.IsNotNull(image.Attribute(XLink + "href"));
    }

    private static XElement ExportText(string value, BinkTextStyle style, Vector2 origin)
    {
        var document = new BinkDocument(new BinkDocumentSettings { Width = 200, Height = 200 });
        document.AddElement(new BinkTextElement(origin, value) { Style = style });
        XDocument svg = XDocument.Parse(BinkSvgExporter.Export(document));
        return Assert.ContainsSingle(svg.Descendants(Svg + "text"));
    }

    private static string PathData(BinkStroke stroke)
    {
        using var writer = new StringWriter(CultureInfo.InvariantCulture);
        stroke.Geometry.Path.WriteSvgPathData(writer, 2);
        return writer.ToString();
    }

    private static float ParseNumber(XAttribute? attribute)
    {
        Assert.IsNotNull(attribute);
        return float.Parse(attribute.Value, CultureInfo.InvariantCulture);
    }
}
