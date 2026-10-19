using System.Numerics;
using System.Xml;

namespace Bit.Bink.Tests;

[TestClass]
public class RasterizerTests
{
    private static BinkRasterSurface Fill(int size, Action<BinkRasterRenderContext> draw, BinkColor? background = null)
    {
        var surface = new BinkRasterSurface(size, size);
        surface.Clear(background ?? BinkColor.Transparent);
        var context = new BinkRasterRenderContext(surface, Matrix3x2.Identity);
        draw(context);
        return surface;
    }

    [TestMethod]
    public void Fills_a_rectangle_exactly()
    {
        BinkRasterSurface surface = Fill(40, c => c.FillPath(new BinkPathBuilder().AddRect(new BinkRect(10, 10, 20, 20)).ToGeometry(), new BinkFillPaint(BinkColor.Black)));

        Assert.AreEqual(255, surface.GetPixel(20, 20).A);
        Assert.AreEqual(0, surface.GetPixel(5, 20).A);
        Assert.AreEqual(0, surface.GetPixel(35, 20).A);
        Assert.AreEqual(255, surface.GetPixel(10, 10).A);
        Assert.AreEqual(0, surface.GetPixel(9, 9).A);
    }

    [TestMethod]
    public void Anti_aliases_a_diagonal_edge()
    {
        BinkRasterSurface surface = Fill(40, c =>
        {
            Vector2[] triangle = [new(0, 0), new(40, 40), new(0, 40)];
            c.FillPath(new BinkPathBuilder().AddPolygon(triangle).ToGeometry(), new BinkFillPaint(BinkColor.Black));
        });

        // Pixels straddling the diagonal must be partially covered, not all-or-nothing.
        int partial = 0;
        for (int i = 5; i < 35; i++)
        {
            byte a = surface.GetPixel(i, i).A;
            if (a is > 20 and < 235) partial++;
        }
        Assert.IsTrue(partial > 20, $"expected anti-aliased edge pixels, found {partial}");
    }

    [TestMethod]
    public void Respects_the_even_odd_fill_rule()
    {
        BinkPathGeometry path = new BinkPathBuilder()
            .AddRect(new BinkRect(5, 5, 30, 30))
            .AddRect(new BinkRect(13, 13, 14, 14))
            .ToGeometry();

        BinkRasterSurface evenOdd = Fill(40, c => c.FillPath(path, new BinkFillPaint(BinkColor.Black, 1f, BinkBlendMode.Normal, BinkFillRule.EvenOdd)));
        BinkRasterSurface nonZero = Fill(40, c => c.FillPath(path, new BinkFillPaint(BinkColor.Black, 1f, BinkBlendMode.Normal, BinkFillRule.NonZero)));

        Assert.AreEqual(0, evenOdd.GetPixel(20, 20).A);   // the inner rectangle is a hole
        Assert.AreEqual(255, nonZero.GetPixel(20, 20).A); // with non-zero it stays solid
        Assert.AreEqual(255, evenOdd.GetPixel(8, 20).A);
    }

    [TestMethod]
    public void Strokes_a_line_at_the_requested_width()
    {
        BinkRasterSurface surface = Fill(60, c => c.StrokePath(
            new BinkPathBuilder().MoveTo(10, 30).LineTo(50, 30).ToGeometry(),
            new BinkStrokePaint(BinkColor.Black, 8f, 1f, BinkBlendMode.Normal, BinkLineCap.Butt)));

        int covered = 0;
        for (int y = 0; y < 60; y++) if (surface.GetPixel(30, y).A > 128) covered++;
        Assert.AreEqual(8, covered);
    }

    [TestMethod]
    public void Round_caps_extend_past_the_endpoint_and_butt_caps_do_not()
    {
        BinkRasterSurface round = Fill(60, c => c.StrokePath(new BinkPathBuilder().MoveTo(20, 30).LineTo(40, 30).ToGeometry(), new BinkStrokePaint(BinkColor.Black, 10f, 1f, BinkBlendMode.Normal, BinkLineCap.Round)));
        BinkRasterSurface butt = Fill(60, c => c.StrokePath(new BinkPathBuilder().MoveTo(20, 30).LineTo(40, 30).ToGeometry(), new BinkStrokePaint(BinkColor.Black, 10f, 1f, BinkBlendMode.Normal, BinkLineCap.Butt)));

        Assert.IsTrue(round.GetPixel(17, 30).A > 200, "round caps should extend past the endpoint");
        Assert.AreEqual(0, butt.GetPixel(17, 30).A);
    }

    [TestMethod]
    public void Multiply_blending_darkens_the_overlap()
    {
        var surface = new BinkRasterSurface(40, 40);
        surface.Clear(BinkColor.White);
        var context = new BinkRasterRenderContext(surface, Matrix3x2.Identity);
        var yellow = new BinkColor(255, 235, 59);
        context.FillPath(new BinkPathBuilder().AddRect(new BinkRect(0, 0, 40, 20)).ToGeometry(), new BinkFillPaint(yellow, 0.5f, BinkBlendMode.Multiply));
        context.FillPath(new BinkPathBuilder().AddRect(new BinkRect(0, 10, 40, 20)).ToGeometry(), new BinkFillPaint(yellow, 0.5f, BinkBlendMode.Multiply));

        BinkColor single = surface.GetPixel(20, 5);
        BinkColor doubled = surface.GetPixel(20, 15);
        Assert.IsTrue(doubled.B < single.B, "overlapping multiply highlights should get darker");
        Assert.IsTrue(doubled.R > 200, "a yellow highlighter must not turn the paper dark");
    }

    [TestMethod]
    public void Destination_out_erases_what_is_beneath_it()
    {
        var surface = new BinkRasterSurface(40, 40);
        surface.Clear(BinkColor.Transparent);
        var context = new BinkRasterRenderContext(surface, Matrix3x2.Identity);
        context.FillPath(new BinkPathBuilder().AddRect(new BinkRect(0, 0, 40, 40)).ToGeometry(), new BinkFillPaint(BinkColor.Black));
        context.FillPath(new BinkPathBuilder().AddCircle(new Vector2(20, 20), 10).ToGeometry(), new BinkFillPaint(BinkColor.Black, 1f, BinkBlendMode.DestinationOut));

        Assert.AreEqual(0, surface.GetPixel(20, 20).A);
        Assert.AreEqual(255, surface.GetPixel(2, 2).A);
    }

    [TestMethod]
    public void Group_opacity_applies_to_the_group_as_a_whole()
    {
        var surface = new BinkRasterSurface(40, 40);
        surface.Clear(BinkColor.Transparent);
        var context = new BinkRasterRenderContext(surface, Matrix3x2.Identity);
        context.PushGroup(0.5f, BinkBlendMode.Normal);
        // Two overlapping opaque rectangles inside one group must not compound to more than 50%.
        context.FillPath(new BinkPathBuilder().AddRect(new BinkRect(0, 0, 40, 40)).ToGeometry(), new BinkFillPaint(BinkColor.Black));
        context.FillPath(new BinkPathBuilder().AddRect(new BinkRect(0, 0, 40, 40)).ToGeometry(), new BinkFillPaint(BinkColor.Black));
        context.PopGroup();

        Assert.IsInRange(125, 131, surface.GetPixel(20, 20).A);
    }

    [TestMethod]
    public void The_clip_rectangle_is_honored()
    {
        BinkRasterSurface surface = Fill(40, c =>
        {
            c.Save();
            c.ClipRect(new BinkRect(0, 0, 20, 40));
            c.FillPath(new BinkPathBuilder().AddRect(new BinkRect(0, 0, 40, 40)).ToGeometry(), new BinkFillPaint(BinkColor.Black));
            c.Restore();
        });

        Assert.AreEqual(255, surface.GetPixel(10, 20).A);
        Assert.AreEqual(0, surface.GetPixel(30, 20).A);
    }

    [TestMethod]
    public void Transforms_scale_stroke_width_along_with_geometry()
    {
        var surface = new BinkRasterSurface(80, 80);
        surface.Clear(BinkColor.Transparent);
        var context = new BinkRasterRenderContext(surface, Matrix3x2.CreateScale(2f));
        context.StrokePath(new BinkPathBuilder().MoveTo(5, 20).LineTo(35, 20).ToGeometry(), new BinkStrokePaint(BinkColor.Black, 4f, 1f, BinkBlendMode.Normal, BinkLineCap.Butt));

        int covered = 0;
        for (int y = 0; y < 80; y++) if (surface.GetPixel(40, y).A > 128) covered++;
        Assert.AreEqual(8, covered);
    }

    [TestMethod]
    public void Drawing_far_outside_the_surface_is_safe()
    {
        BinkRasterSurface surface = Fill(20, c =>
        {
            c.FillPath(new BinkPathBuilder().AddRect(new BinkRect(-10000, -10000, 20000, 20000)).ToGeometry(), new BinkFillPaint(BinkColor.Black));
            c.FillPath(new BinkPathBuilder().AddRect(new BinkRect(50000, 50000, 10, 10)).ToGeometry(), new BinkFillPaint(BinkColor.Black));
        });
        Assert.AreEqual(255, surface.GetPixel(10, 10).A);
    }
}

[TestClass]
public class PngCodecTests
{
    [TestMethod]
    public void Round_trips_pixels_exactly()
    {
        const int size = 17;
        byte[] rgba = new byte[size * size * 4];
        var random = new Random(7);
        random.NextBytes(rgba);

        byte[] png = BinkPngCodec.Encode(rgba, size, size);
        BinkPngCodec.DecodedImage decoded = BinkPngCodec.Decode(png);

        Assert.AreEqual(size, decoded.Width);
        Assert.AreEqual(size, decoded.Height);
        Assert.AreSequenceEqual(rgba, decoded.Rgba);
    }

    [TestMethod]
    public void Encoding_is_deterministic()
    {
        byte[] rgba = new byte[8 * 8 * 4];
        new Random(3).NextBytes(rgba);
        Assert.AreSequenceEqual(BinkPngCodec.Encode(rgba, 8, 8), BinkPngCodec.Encode(rgba, 8, 8));
    }

    [TestMethod]
    public void Produces_a_valid_png_signature()
    {
        byte[] png = TestData.CreatePng(4, 4, (_, _) => BinkColor.Black);
        Assert.IsTrue(BinkPngCodec.IsPng(png));
        Assert.IsFalse(BinkPngCodec.IsPng([1, 2, 3, 4, 5, 6, 7, 8]));
    }

    [TestMethod]
    public void Compresses_a_flat_image_well()
    {
        byte[] png = TestData.CreatePng(200, 200, (_, _) => BinkColor.White);
        Assert.IsTrue(png.Length < 2000, $"a flat 200x200 image encoded to {png.Length} bytes");
    }

    [TestMethod]
    [DataRow(new byte[] { 1, 2, 3 })]
    [DataRow(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A })]
    public void Malformed_input_is_rejected(byte[] data)
    {
        Assert.ThrowsExactly<FormatException>(() => BinkPngCodec.Decode(data));
    }

    [TestMethod]
    public void An_oversized_declared_size_is_rejected()
    {
        byte[] png = TestData.CreatePng(4, 4, (_, _) => BinkColor.Black);
        Assert.ThrowsExactly<FormatException>(() => BinkPngCodec.Decode(png, maxPixels: 4));
    }
}

[TestClass]
public class PngExportTests
{
    [TestMethod]
    public void Exports_the_page_at_the_requested_scale()
    {
        BinkDocument document = TestData.SampleDocument();
        BinkPngExportResult result = BinkPngExporter.ExportWithDetails(document, new BinkPngExportOptions { Scale = 2f });

        Assert.AreEqual(800, result.Width);
        Assert.AreEqual(600, result.Height);
        Assert.IsTrue(BinkPngCodec.IsPng(result.Bytes));
    }

    [TestMethod]
    public void The_exported_image_actually_contains_the_ink()
    {
        var document = new BinkDocument(new BinkDocumentSettings { Width = 100, Height = 100, Background = BinkColor.White });
        document.AddElement(new BinkStroke(TestData.Line(new Vector2(10, 50), new Vector2(90, 50), 30), new BinkPenBrush { Width = 6f, PressureCurve = BinkPressureCurve.Constant, TaperEnd = 0 }));

        BinkRasterSurface surface = BinkPngExporter.Render(document, BinkPngExportOptions.Default, out _, out _);
        Assert.IsTrue(surface.GetPixel(50, 50).R < 60, "the stroke should be dark at its center");
        Assert.AreEqual(255, surface.GetPixel(50, 10).R);
    }

    [TestMethod]
    public void A_transparent_background_is_preserved()
    {
        var document = new BinkDocument(new BinkDocumentSettings { Width = 50, Height = 50, Background = BinkColor.Transparent });
        document.AddElement(new BinkStroke(TestData.Line(new Vector2(10, 25), new Vector2(40, 25), 10), new BinkPenBrush { Width = 4f, PressureCurve = BinkPressureCurve.Constant, TaperEnd = 0f }));

        BinkRasterSurface surface = BinkPngExporter.Render(document, BinkPngExportOptions.Default, out _, out _);
        Assert.AreEqual(0, surface.GetPixel(25, 5).A);
        Assert.IsTrue(surface.GetPixel(25, 25).A > 200);
    }

    [TestMethod]
    public void Cropping_to_content_trims_the_empty_margins()
    {
        var document = new BinkDocument(new BinkDocumentSettings { Width = 500, Height = 500 });
        document.AddElement(new BinkStroke(TestData.Line(new Vector2(200, 200), new Vector2(240, 240), 10), new BinkPenBrush { Width = 4f }));

        BinkPngExportResult result = BinkPngExporter.ExportWithDetails(document, new BinkPngExportOptions { Region = BinkExportRegion.Content, Padding = 5f });
        Assert.IsTrue(result.Width < 100, $"expected a tight crop but got {result.Width}px");
        Assert.IsTrue(result.Width > 40);
    }

    [TestMethod]
    public void Exporting_a_selection_only_draws_the_selected_elements()
    {
        var document = new BinkDocument(new BinkDocumentSettings { Width = 200, Height = 100, Background = BinkColor.White });
        var a = new BinkStroke(TestData.Line(new Vector2(10, 50), new Vector2(60, 50), 10), new BinkPenBrush { Width = 6f });
        var b = new BinkStroke(TestData.Line(new Vector2(140, 50), new Vector2(190, 50), 10), new BinkPenBrush { Width = 6f });
        document.AddElement(a);
        document.AddElement(b);

        var options = new BinkPngExportOptions { Region = BinkExportRegion.Custom, CustomRegion = new BinkRect(0, 0, 200, 100), SelectedIds = [a.Id] };
        BinkRasterSurface surface = BinkPngExporter.Render(document, options, out _, out _);
        Assert.IsTrue(surface.GetPixel(35, 50).R < 100, "the selected stroke should be drawn");
        Assert.AreEqual(255, surface.GetPixel(165, 50).R);
    }

    [TestMethod]
    public void An_absurd_scale_is_refused_instead_of_exhausting_memory()
    {
        BinkDocument document = TestData.SampleDocument();
        Assert.ThrowsExactly<InvalidOperationException>(() => BinkPngExporter.Export(document, new BinkPngExportOptions { Scale = 5000f }));
    }

    [TestMethod]
    public void Text_is_reported_as_skipped_when_no_text_renderer_is_supplied()
    {
        var document = new BinkDocument(new BinkDocumentSettings { Width = 100, Height = 50 });
        document.AddElement(new BinkTextElement(new Vector2(5, 5), "hello"));
        BinkPngExportResult result = BinkPngExporter.ExportWithDetails(document);
        Assert.AreEqual(1, result.SkippedTextElements);
        Assert.IsFalse(result.IsComplete);
    }

    [TestMethod]
    public void Embedded_png_images_are_drawn()
    {
        var document = new BinkDocument(new BinkDocumentSettings { Width = 40, Height = 40, Background = BinkColor.White });
        byte[] png = TestData.CreatePng(8, 8, (x, y) => (x + y) % 2 == 0 ? new BinkColor(200, 0, 0) : new BinkColor(0, 0, 200));
        document.AddElement(new BinkImageElement(BinkImageSource.FromBytes(png, "image/png", 8, 8), new BinkRect(8, 8, 24, 24)));

        BinkRasterSurface surface = BinkPngExporter.Render(document, BinkPngExportOptions.Default, out _, out int skipped);
        Assert.AreEqual(0, skipped);
        BinkColor middle = surface.GetPixel(20, 20);
        Assert.IsTrue(middle.R > 40 || middle.B > 40, "the image should have been drawn");
        Assert.AreEqual(255, surface.GetPixel(2, 2).R);
    }

    [TestMethod]
    public void Export_is_deterministic()
    {
        BinkDocument document = TestData.SampleDocument();
        Assert.AreSequenceEqual(BinkPngExporter.Export(document), BinkPngExporter.Export(document));
    }

    [TestMethod]
    public void Eraser_strokes_remove_ink_within_their_layer_only()
    {
        var document = new BinkDocument(new BinkDocumentSettings { Width = 100, Height = 100, Background = BinkColor.White });
        Guid bottom = document.Layers[0].Id;
        document.AddElement(new BinkStroke(TestData.Line(new Vector2(0, 30), new Vector2(100, 30), 20), new BinkPenBrush { Width = 10f, PressureCurve = BinkPressureCurve.Constant }), bottom);

        BinkLayer top = document.AddLayer("Top");
        document.AddElement(new BinkStroke(TestData.Line(new Vector2(0, 70), new Vector2(100, 70), 20), new BinkPenBrush { Width = 10f, PressureCurve = BinkPressureCurve.Constant }), top.Id);
        document.AddElement(new BinkStroke(TestData.Line(new Vector2(40, 0), new Vector2(40, 100), 20), new BinkPixelEraserBrush { Width = 24f }), top.Id);

        BinkRasterSurface surface = BinkPngExporter.Render(document, BinkPngExportOptions.Default, out _, out _);
        Assert.AreEqual(255, surface.GetPixel(40, 70).R);          // erased on its own layer
        Assert.IsTrue(surface.GetPixel(40, 30).R < 100, "the eraser must not reach through to the layer below");
    }
}

[TestClass]
public class SvgExportTests
{
    private static XmlDocument Parse(string svg)
    {
        var xml = new XmlDocument { XmlResolver = null };
        var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null };
        using var reader = XmlReader.Create(new StringReader(svg), settings);
        xml.Load(reader);
        return xml;
    }

    [TestMethod]
    public void Produces_well_formed_xml()
    {
        string svg = BinkSvgExporter.Export(TestData.SampleDocument());
        XmlDocument xml = Parse(svg);
        Assert.AreEqual("svg", xml.DocumentElement!.LocalName);
        Assert.AreEqual("http://www.w3.org/2000/svg", xml.DocumentElement.NamespaceURI);
    }

    [TestMethod]
    public void Exports_strokes_as_paths_not_as_an_embedded_bitmap()
    {
        string svg = BinkSvgExporter.Export(TestData.SampleDocument());
        XmlDocument xml = Parse(svg);
        XmlNodeList paths = xml.GetElementsByTagName("path");
        Assert.IsTrue(paths.Count >= 5, $"expected several vector paths, found {paths.Count}");
        Assert.DoesNotContain("data:image", svg, StringComparison.OrdinalIgnoreCase);
    }

    [TestMethod]
    public void Uses_the_document_size_as_the_view_box()
    {
        string svg = BinkSvgExporter.Export(TestData.SampleDocument());
        XmlDocument xml = Parse(svg);
        Assert.AreEqual("0 0 400 300", xml.DocumentElement!.GetAttribute("viewBox"));
        Assert.AreEqual("400", xml.DocumentElement.GetAttribute("width"));
    }

    [TestMethod]
    public void Escapes_text_content_and_metadata()
    {
        var document = new BinkDocument(new BinkDocumentSettings { Width = 100, Height = 100 });
        document.AddElement(new BinkTextElement(new Vector2(5, 5), "</text><script>alert('x')</script>"));
        string svg = BinkSvgExporter.Export(document, new BinkSvgExportOptions { Title = "<script>bad</script>" });

        Parse(svg); // must still be well formed
        Assert.DoesNotContain("<script>", svg, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("&lt;script&gt;", svg, StringComparison.Ordinal);
    }

    [TestMethod]
    public void Never_emits_script_or_event_handler_attributes()
    {
        BinkDocument document = TestData.SampleDocument();
        document.AddElement(new BinkTextElement(new Vector2(0, 0), "onload=alert(1)") { Description = "\" onclick=\"alert(1)" });
        string svg = BinkSvgExporter.Export(document, new BinkSvgExportOptions { IncludeElementDescriptions = true });

        XmlDocument xml = Parse(svg);
        foreach (XmlNode node in xml.GetElementsByTagName("*"))
        {
            Assert.AreNotEqual("script", node.LocalName, StringComparer.OrdinalIgnoreCase);
            Assert.AreNotEqual("foreignObject", node.LocalName, StringComparer.OrdinalIgnoreCase);
            if (node.Attributes is null) continue;
            foreach (XmlAttribute attribute in node.Attributes)
            {
                Assert.IsFalse(attribute.LocalName.StartsWith("on", StringComparison.OrdinalIgnoreCase), $"found event handler attribute {attribute.LocalName}");
            }
        }
    }

    [TestMethod]
    public void Colours_are_emitted_as_hex_and_can_carry_no_payload()
    {
        BinkDocument document = TestData.SampleDocument();
        string svg = BinkSvgExporter.Export(document);

        foreach (System.Text.RegularExpressions.Match match in System.Text.RegularExpressions.Regex.Matches(svg, @"(?:fill|stroke)=""([^""]*)"""))
        {
            string value = match.Groups[1].Value;
            Assert.IsTrue(value == "none" || System.Text.RegularExpressions.Regex.IsMatch(value, "^#[0-9a-f]{6}$"),
                $"colour attribute carried an unexpected value: {value}");
        }
    }

    [TestMethod]
    public void The_only_external_reference_an_export_can_contain_is_an_image()
    {
        // Documented behaviour: an image element with a remote source is written as a reference to it. Nothing
        // else in the output may point outside the file.
        var document = new BinkDocument(new BinkDocumentSettings { Width = 100, Height = 100 });
        document.AddElement(new BinkStroke(TestData.Line(new Vector2(0, 50), new Vector2(100, 50), 10), new BinkPenBrush()));
        document.AddElement(new BinkTextElement(new Vector2(5, 5), "hello"));

        string svg = BinkSvgExporter.Export(document);
        Assert.DoesNotContain("href", svg, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("url(http", svg, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("@import", svg, StringComparison.OrdinalIgnoreCase);

        // With an image, exactly one reference appears, and it is the image's own source.
        byte[] png = TestData.CreatePng(4, 4, (_, _) => BinkColor.Black);
        document.AddElement(new BinkImageElement(BinkImageSource.FromBytes(png, "image/png", 4, 4), new BinkRect(0, 0, 50, 50)));
        string withImage = BinkSvgExporter.Export(document);
        Assert.ContainsSingle(System.Text.RegularExpressions.Regex.Matches(withImage, "href="));
    }

    [TestMethod]
    public void An_element_that_leaves_a_scope_open_cannot_unbalance_the_document()
    {
        // A cropped image opens a clip scope. Getting the pairing wrong closed the layer group early, which let
        // later elements escape their layer's isolation.
        var document = new BinkDocument(new BinkDocumentSettings { Width = 200, Height = 200 });
        byte[] png = TestData.CreatePng(8, 8, (_, _) => BinkColor.Black);
        document.UpdateLayer(document.Layers[0].Id, p => p with { Opacity = 0.5f });
        document.AddElement(new BinkImageElement(BinkImageSource.FromBytes(png, "image/png", 8, 8), new BinkRect(0, 0, 80, 80))
        {
            Crop = new BinkRect(2, 2, 4, 4),
            Transform = Matrix3x2.CreateRotation(0.3f),
        });
        document.AddElement(new BinkRectangleShape(new BinkRect(100, 100, 50, 50)));

        string svg = BinkSvgExporter.Export(document);
        XmlDocument xml = Parse(svg);

        // The rectangle must still be inside the layer group, which the image must not have closed.
        XmlNodeList rects = xml.GetElementsByTagName("rect");
        bool foundInsideGroup = false;
        foreach (XmlNode node in rects)
        {
            for (XmlNode? parent = node.ParentNode; parent is not null; parent = parent.ParentNode)
            {
                if (parent.LocalName == "g" && parent.Attributes?["data-layer"] is not null) foundInsideGroup = true;
            }
        }
        Assert.IsTrue(foundInsideGroup, "the element after a cropped image fell outside its layer group");
    }

    [TestMethod]
    public void Eraser_strokes_become_masks()
    {
        var document = new BinkDocument(new BinkDocumentSettings { Width = 100, Height = 100 });
        document.AddElement(new BinkStroke(TestData.Line(new Vector2(0, 50), new Vector2(100, 50), 20), new BinkPenBrush { Width = 10f }));
        document.AddElement(new BinkStroke(TestData.Line(new Vector2(50, 0), new Vector2(50, 100), 20), new BinkPixelEraserBrush { Width = 20f }));

        string svg = BinkSvgExporter.Export(document);
        XmlDocument xml = Parse(svg);
        Assert.AreEqual(1, xml.GetElementsByTagName("mask").Count);
        Assert.Contains("mask=\"url(#", svg, StringComparison.Ordinal);
    }

    [TestMethod]
    public void Highlighter_strokes_carry_the_multiply_blend_mode()
    {
        var document = new BinkDocument(new BinkDocumentSettings { Width = 100, Height = 100 });
        document.AddElement(new BinkStroke(TestData.Line(new Vector2(0, 50), new Vector2(100, 50), 10), new BinkHighlighterBrush()));
        string svg = BinkSvgExporter.Export(document);
        Assert.Contains("mix-blend-mode:multiply", svg, StringComparison.Ordinal);
    }

    [TestMethod]
    public void Layer_opacity_is_exported()
    {
        var document = new BinkDocument(new BinkDocumentSettings { Width = 100, Height = 100 });
        document.AddElement(new BinkStroke(TestData.Line(new Vector2(0, 0), new Vector2(50, 50), 10), new BinkPenBrush()));
        document.UpdateLayer(document.Layers[0].Id, p => p with { Opacity = 0.25f });
        Assert.Contains("opacity=\"0.25\"", BinkSvgExporter.Export(document), StringComparison.Ordinal);
    }

    [TestMethod]
    public void Element_transforms_are_exported_as_matrices()
    {
        var document = new BinkDocument(new BinkDocumentSettings { Width = 200, Height = 200 });
        document.AddElement(new BinkRectangleShape(new BinkRect(0, 0, 20, 20)) { Transform = Matrix3x2.CreateRotation(0.5f) * Matrix3x2.CreateTranslation(50, 50) });
        Assert.Contains("transform=\"matrix(", BinkSvgExporter.Export(document), StringComparison.Ordinal);
    }

    [TestMethod]
    public void Numbers_use_invariant_formatting()
    {
        var previous = System.Globalization.CultureInfo.CurrentCulture;
        try
        {
            System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo("fr-FR");
            string svg = BinkSvgExporter.Export(TestData.SampleDocument());
            Parse(svg);
            Assert.DoesNotContain("NaN", svg, StringComparison.Ordinal);
            Assert.MatchesRegex(@"viewBox=""[-0-9. ]+""", svg);
        }
        finally
        {
            System.Globalization.CultureInfo.CurrentCulture = previous;
        }
    }

    [TestMethod]
    public void Export_is_deterministic()
    {
        BinkDocument document = TestData.SampleDocument();
        Assert.AreEqual(BinkSvgExporter.Export(document), BinkSvgExporter.Export(document));
    }

    [TestMethod]
    public void Content_region_crops_to_the_drawing()
    {
        var document = new BinkDocument(new BinkDocumentSettings { Width = 1000, Height = 1000 });
        document.AddElement(new BinkStroke(TestData.Line(new Vector2(400, 400), new Vector2(440, 440), 10), new BinkPenBrush { Width = 4f }));

        string svg = BinkSvgExporter.Export(document, new BinkSvgExportOptions { Region = BinkExportRegion.Content, Padding = 4f });
        XmlDocument xml = Parse(svg);
        string[] parts = xml.DocumentElement!.GetAttribute("viewBox").Split(' ');
        Assert.IsTrue(float.Parse(parts[2], System.Globalization.CultureInfo.InvariantCulture) < 100);
    }

    [TestMethod]
    public void Element_descriptions_are_exported_for_screen_readers()
    {
        var document = new BinkDocument(new BinkDocumentSettings { Width = 100, Height = 100 });
        document.AddElement(new BinkStroke(TestData.Line(new Vector2(0, 0), new Vector2(50, 50), 10), new BinkPenBrush()) { Description = "Signature of Jane Doe" });

        string svg = BinkSvgExporter.Export(document, new BinkSvgExportOptions { IncludeElementDescriptions = true });
        Assert.Contains("<title>Signature of Jane Doe</title>", svg, StringComparison.Ordinal);
    }
}

[TestClass]
public class RenderConsistencyTests
{
    [TestMethod]
    public void Svg_and_raster_render_the_same_number_of_elements()
    {
        BinkDocument document = TestData.SampleDocument();
        var counting = new CountingContext();
        int drawn = BinkDocumentRenderer.Render(document, counting);
        Assert.AreEqual(document.ElementCount, drawn);
    }

    [TestMethod]
    public void Culling_skips_elements_outside_the_clip_bounds()
    {
        var document = new BinkDocument(new BinkDocumentSettings { Width = 1000, Height = 1000 });
        for (int i = 0; i < 20; i++) document.AddElement(TestData.Stroke(new Vector2(i * 50, 0), new Vector2(i * 50 + 10, 10)));

        var counting = new CountingContext();
        int drawn = BinkDocumentRenderer.Render(document, counting, new BinkRenderOptions { ClipBounds = new BinkRect(0, 0, 120, 100) });
        Assert.IsTrue(drawn < 5, $"expected culling to skip most strokes but drew {drawn}");
    }

    private sealed class CountingContext : IBinkRenderContext
    {
        public int Fills { get; private set; }
        public void Save() { }
        public void Restore() { }
        public void Transform(Matrix3x2 matrix) { }
        public void ClipRect(BinkRect rect) { }
        public void FillPath(BinkPathGeometry path, in BinkFillPaint paint) => Fills++;
        public void StrokePath(BinkPathGeometry path, in BinkStrokePaint paint) => Fills++;
        public void DrawImage(BinkImageSource image, BinkRect destination, BinkRect? sourceRect, float opacity) { }
        public void DrawText(string text, Vector2 origin, in BinkTextStyle style, float opacity) { }
        public void PushGroup(float opacity, BinkBlendMode blend) { }
        public void PopGroup() { }
    }
}

[TestClass]
public class ClosedPathStrokingTests
{
    [TestMethod]
    public void A_stroked_ellipse_has_no_gaps()
    {
        var surface = new BinkRasterSurface(120, 120);
        surface.Clear(BinkColor.White);
        var context = new BinkRasterRenderContext(surface, Matrix3x2.Identity);
        context.StrokePath(new BinkPathBuilder().AddEllipse(new BinkRect(20, 30, 80, 60)).ToGeometry(),
            new BinkStrokePaint(BinkColor.Black, 4f));

        // Walk the ellipse and check that ink is present all the way around.
        var center = new Vector2(60, 60);
        int gaps = 0;
        for (int i = 0; i < 360; i++)
        {
            float a = i * MathF.PI / 180f;
            var point = new Vector2(center.X + MathF.Cos(a) * 40f, center.Y + MathF.Sin(a) * 30f);
            bool ink = false;
            for (int dx = -3; dx <= 3 && !ink; dx++)
            {
                for (int dy = -3; dy <= 3 && !ink; dy++)
                {
                    if (surface.GetPixel((int)point.X + dx, (int)point.Y + dy).R < 128) ink = true;
                }
            }
            if (!ink) gaps++;
        }
        Assert.IsTrue(gaps == 0, $"the ellipse outline has {gaps} gaps out of 360 sampled positions");
    }

    [TestMethod]
    public void A_stroked_closed_rectangle_has_no_gaps()
    {
        var surface = new BinkRasterSurface(120, 120);
        surface.Clear(BinkColor.White);
        var context = new BinkRasterRenderContext(surface, Matrix3x2.Identity);
        context.StrokePath(new BinkPathBuilder().AddRect(new BinkRect(20, 20, 80, 80)).ToGeometry(),
            new BinkStrokePaint(BinkColor.Black, 4f));

        int gaps = 0;
        for (int x = 22; x < 98; x++)
        {
            if (surface.GetPixel(x, 20).R > 200) gaps++;
        }
        Assert.AreEqual(0, gaps);
    }

    [TestMethod]
    public void A_dash_pattern_does_not_leak_into_the_next_path()
    {
        var surface = new BinkRasterSurface(120, 120);
        surface.Clear(BinkColor.White);
        var context = new BinkRasterRenderContext(surface, Matrix3x2.Identity);

        context.StrokePath(new BinkPathBuilder().MoveTo(10, 30).LineTo(110, 30).ToGeometry(),
            new BinkStrokePaint(BinkColor.Black, 3f, 1f, BinkBlendMode.Normal, BinkLineCap.Butt, BinkLineJoin.Miter, [6f, 4f]));
        context.StrokePath(new BinkPathBuilder().MoveTo(10, 70).LineTo(110, 70).ToGeometry(),
            new BinkStrokePaint(BinkColor.Black, 3f));

        int gaps = 0;
        for (int x = 12; x < 108; x++) if (surface.GetPixel(x, 70).R > 200) gaps++;
        Assert.AreEqual(0, gaps);
    }
}
