using System.Globalization;
using System.IO.Compression;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;

namespace Bit.Bink.Tests;

/// <summary>PDF export: a well-formed file that draws what the canvas draws, and reports what it cannot.</summary>
[TestClass]
public sealed partial class PdfExportTests
{
    /// <summary>
    /// Reads a PDF the way a viewer starts to: from the cross-reference table at the end, checking that every
    /// offset it gives points at the object it names, and inflating streams.
    /// </summary>
    private sealed partial class Pdf
    {
        public Pdf(byte[] bytes)
        {
            Bytes = bytes;
            Text = Encoding.Latin1.GetString(bytes);
            Assert.StartsWith("%PDF-1.4\n", Text);
            Assert.EndsWith("%%EOF\n", Text);
            int startxref = int.Parse(StartXref().Match(Text).Groups[1].Value, CultureInfo.InvariantCulture);
            Assert.IsTrue(Text.AsSpan(startxref).StartsWith("xref\n"), "startxref points at the table.");
            Match header = XrefHeader().Match(Text, startxref);
            int count = int.Parse(header.Groups[1].Value, CultureInfo.InvariantCulture);
            int position = header.Index + header.Length;
            for (int i = 0; i < count; i++)
            {
                string entry = Text.Substring(position + i * 20, 20);
                if (i == 0) { Assert.AreEqual("0000000000 65535 f \n", entry); continue; }
                int offset = int.Parse(entry[..10], CultureInfo.InvariantCulture);
                Assert.IsTrue(Text.AsSpan(offset).StartsWith($"{i} 0 obj\n"), $"The table's offset for object {i} points at it.");
                int end = Text.IndexOf("\nendobj\n", offset, StringComparison.Ordinal);
                Objects[i] = Text[(offset + $"{i} 0 obj\n".Length)..end];
            }
            StringAssert.Contains(Text, $"/Size {count}");
        }

        public byte[] Bytes { get; }

        public string Text { get; }

        public Dictionary<int, string> Objects { get; } = new();

        /// <summary>The objects whose dictionary contains every fragment.</summary>
        public IEnumerable<KeyValuePair<int, string>> Find(params string[] fragments)
            => Objects.Where(o => fragments.All(f => Dictionary(o.Value).Contains(f, StringComparison.Ordinal)));

        public static string Dictionary(string body)
        {
            int stream = body.IndexOf("\nstream\n", StringComparison.Ordinal);
            return stream < 0 ? body : body[..stream];
        }

        /// <summary>A stream's data, inflated when it is compressed.</summary>
        public string Content(string body)
        {
            int start = body.IndexOf("\nstream\n", StringComparison.Ordinal) + "\nstream\n".Length;
            int length = int.Parse(LengthOf().Match(body).Groups[1].Value, CultureInfo.InvariantCulture);
            byte[] data = Encoding.Latin1.GetBytes(body.Substring(start, length));
            if (!Dictionary(body).Contains("/FlateDecode", StringComparison.Ordinal)) return Encoding.Latin1.GetString(data);
            using var input = new ZLibStream(new MemoryStream(data), CompressionMode.Decompress);
            using var output = new MemoryStream();
            input.CopyTo(output);
            return Encoding.Latin1.GetString(output.ToArray());
        }

        /// <summary>Every content stream inflated and joined: pages and forms.</summary>
        public string AllContent => string.Join("\n", Find("/Length").Where(o => !Dictionary(o.Value).Contains("/Subtype /Image", StringComparison.Ordinal)).Select(o => Content(o.Value)));

        [GeneratedRegex(@"startxref\n(\d+)\n%%EOF\n$")]
        private static partial Regex StartXref();

        [GeneratedRegex(@"xref\n0 (\d+)\n")]
        private static partial Regex XrefHeader();

        [GeneratedRegex(@"/Length (\d+)")]
        private static partial Regex LengthOf();
    }

    private static BinkDocument Page(float width = 400, float height = 300) => new(new BinkDocumentSettings { Width = width, Height = height, Background = BinkColor.White });

    [TestMethod]
    public void The_sample_document_is_a_well_formed_pdf_of_one_page_the_size_of_the_page()
    {
        BinkPdfExportResult result = BinkPdfExporter.ExportWithDetails(TestData.SampleDocument());
        var pdf = new Pdf(result.Bytes);

        Assert.AreEqual(1, result.PageCount);
        Assert.IsTrue(result.IsComplete, "The sample's text is Latin and it has no images.");
        string page = pdf.Find("/Type /Page ").Single().Value;
        StringAssert.Contains(page, "/MediaBox [0 0 300 225]", "400 by 300 units at three quarters of a point each.");
        StringAssert.Contains(pdf.Find("/Type /Pages").Single().Value, "/Count 1");
        StringAssert.Contains(pdf.AllContent, "0.75 0 0 -0.75 0 225 cm", "Document units map to points with y turned down.");
    }

    [TestMethod]
    public void The_same_document_always_gives_the_same_bytes()
    {
        BinkDocument document = TestData.SampleDocument();
        CollectionAssert.AreEqual(BinkPdfExporter.Export(document), BinkPdfExporter.Export(document));
    }

    [TestMethod]
    public void Strokes_shapes_and_paths_stay_vector_with_their_paint()
    {
        BinkDocument document = Page();
        Guid layer = document.Layers[0].Id;
        document.AddElement(new BinkStroke(TestData.Line(new(20, 20), new(200, 60)), new BinkPenBrush { Color = BinkColor.Parse("#ff0000") }), layer);
        document.AddElement(new BinkRectangleShape(new BinkRect(50, 100, 100, 50))
        {
            Style = new BinkShapeStyle { StrokeColor = BinkColor.Parse("#0000ff"), StrokeWidth = 3, Dash = [6, 3], LineJoin = BinkLineJoin.Miter, LineCap = BinkLineCap.Square, FillColor = BinkColor.Parse("#00ff0080") },
        }, layer);
        document.AddElement(new BinkStroke(TestData.Line(new(20, 200), new(300, 200)), new BinkHighlighterBrush()), layer);

        var pdf = new Pdf(BinkPdfExporter.Export(document, new BinkPdfExportOptions { Compress = false }));
        string content = pdf.AllContent;

        StringAssert.Contains(content, "1 0 0 rg", "The pen's red fill.");
        StringAssert.Contains(content, "0 0 1 RG", "The rectangle's blue outline.");
        StringAssert.Contains(content, "3 w 2 J 0 j 4 M", "Width, square cap and miter join keep PDF's own numbering.");
        StringAssert.Contains(content, "[6 3] 0 d");
        StringAssert.Contains(content, " c\n", "Curves stay curves.");
        Assert.IsTrue(pdf.Find("/Type /ExtGState", "/ca 0.502").Any(), "The half-transparent fill is a graphics state.");
        Assert.IsTrue(pdf.Find("/Type /ExtGState", "/BM /Multiply").Any(), "The highlighter multiplies.");
    }

    [TestMethod]
    public void A_translucent_layer_is_drawn_as_one_transparency_group()
    {
        BinkDocument document = Page();
        BinkLayer layer = document.AddLayer("Faded");
        document.UpdateLayer(layer.Id, p => p with { Opacity = 0.4f });
        document.AddElement(new BinkStroke(TestData.Line(new(20, 20), new(200, 60)), new BinkMarkerBrush()), layer.Id);
        document.AddElement(new BinkStroke(TestData.Line(new(20, 60), new(200, 20)), new BinkMarkerBrush()), layer.Id);

        var pdf = new Pdf(BinkPdfExporter.Export(document));

        Assert.ContainsSingle(pdf.Find("/Subtype /Form", "/S /Transparency /I true"));
        Assert.IsTrue(pdf.Find("/Type /ExtGState", "/ca 0.4 /CA 0.4").Any(), "The group's opacity applies to it as a whole.");
    }

    [TestMethod]
    public void A_pixel_eraser_becomes_a_soft_mask_over_what_was_drawn_before_it()
    {
        BinkDocument document = Page();
        Guid layer = document.Layers[0].Id;
        document.AddElement(new BinkStroke(TestData.Line(new(20, 100), new(380, 100)), new BinkMarkerBrush { Width = 12 }), layer);
        document.AddElement(new BinkStroke(TestData.Line(new(200, 60), new(200, 140)), new BinkPixelEraserBrush()), layer);
        document.AddElement(new BinkStroke(TestData.Line(new(20, 200), new(380, 200)), new BinkMarkerBrush()), layer);

        var pdf = new Pdf(BinkPdfExporter.Export(document, new BinkPdfExportOptions { Compress = false }));

        KeyValuePair<int, string> state = pdf.Find("/SMask << /Type /Mask /S /Luminosity").Single();
        int maskForm = int.Parse(Regex.Match(state.Value, @"/G (\d+) 0 R").Groups[1].Value, CultureInfo.InvariantCulture);
        string mask = pdf.Content(pdf.Objects[maskForm]);
        Assert.StartsWith("1 g\n", mask, "The mask keeps everything...");
        StringAssert.Contains(mask, "0 g\n", "...except where the eraser passed.");
        Assert.DoesNotContain("200 l", mask, "Ink drawn after the eraser is not in the mask.");

        string erased = pdf.Find("/Subtype /Form", "/Group << /S /Transparency >>").Single().Value;
        Assert.AreEqual(1, Regex.Matches(pdf.Content(erased), "\nS\n").Count, "Only the marker line drawn before the eraser is erased.");
    }

    [TestMethod]
    public void Translucent_erasers_compound_where_they_overlap()
    {
        // Each eraser painted an opaque grey into the mask, so where two half erasers crossed the second replaced the
        // first and half the ink stayed, where the canvas and the rasterizer leave a quarter.
        BinkDocument document = Page();
        Guid layer = document.Layers[0].Id;
        document.AddElement(new BinkStroke(TestData.Line(new(20, 100), new(380, 100)), new BinkMarkerBrush { Width = 12 }), layer);
        document.AddElement(new BinkStroke(TestData.Line(new(200, 60), new(200, 140)), new BinkPixelEraserBrush { Opacity = 0.5f }), layer);
        document.AddElement(new BinkStroke(TestData.Line(new(180, 60), new(220, 140)), new BinkPixelEraserBrush { Opacity = 0.5f }), layer);

        var pdf = new Pdf(BinkPdfExporter.Export(document, new BinkPdfExportOptions { Compress = false }));

        KeyValuePair<int, string> multiply = pdf.Find("/Type /ExtGState", "/BM /Multiply").Single();
        string name = Regex.Match(pdf.Objects.Values.Single(o => o.Contains("/ExtGState <<", StringComparison.Ordinal)), $@"/(G\d+) {multiply.Key} 0 R").Groups[1].Value;
        int maskForm = int.Parse(Regex.Match(pdf.Find("/SMask << /Type /Mask").Single().Value, @"/G (\d+) 0 R").Groups[1].Value, CultureInfo.InvariantCulture);
        string mask = pdf.Content(pdf.Objects[maskForm]);
        Assert.AreEqual(2, Regex.Matches(mask, $"/{name} gs\n0.5 g\n").Count, "Both erasers multiply into the mask.");
    }

    [TestMethod]
    public void A_page_too_large_for_viewers_is_scaled_down_and_a_region_that_is_not_finite_is_refused()
    {
        var pdf = new Pdf(BinkPdfExporter.Export(Page(40_000, 1_000)));
        StringAssert.Contains(pdf.Find("/Type /Page ").Single().Value, "/MediaBox [0 0 14400 360]");

        // The content is scaled exactly as the page is: a scale rounded to three decimals put the right of a page
        // 70,000 units wide 20 points beyond its edge, and blanked one of 30 million.
        foreach (float width in new[] { 70_000f, 30_000_000f })
        {
            var wide = new Pdf(BinkPdfExporter.Export(Page(width, 100)));
            string page = wide.Content(wide.Find("/Type /Page ").Select(p => wide.Objects[int.Parse(Regex.Match(p.Value, @"/Contents (\d+) 0 R").Groups[1].Value, CultureInfo.InvariantCulture)]).Single());
            float scale = float.Parse(page.Split('\n')[1].Split(' ')[0], CultureInfo.InvariantCulture);
            Assert.AreEqual(14_400f, width * scale, 0.5f, $"width {width}");
        }

        Assert.ThrowsExactly<InvalidOperationException>(() => BinkPdfExporter.Export(Page(), new BinkPdfExportOptions { Region = BinkExportRegion.Custom, CustomRegion = new BinkRect(0, 0, float.NaN, 10) }));
    }

    [TestMethod]
    public void Lines_break_at_line_feeds_only()
    {
        BinkDocument document = Page();
        document.AddElement(new BinkTextElement(new Vector2(10, 10), "one\r\ntwo\rthree"), document.Layers[0].Id);

        BinkPdfExportResult result = BinkPdfExporter.ExportWithDetails(document, new BinkPdfExportOptions { Compress = false });
        string content = new Pdf(result.Bytes).AllContent;

        Assert.AreEqual(0, result.SkippedTextElements);
        StringAssert.Contains(content, "(one) Tj");
        StringAssert.Contains(content, "(twothree) Tj", "A lone carriage return draws nothing, as on the canvas and in SVG.");
    }

    [TestMethod]
    public void An_eraser_with_nothing_before_it_erases_nothing_and_adds_no_mask()
    {
        BinkDocument document = Page();
        Guid layer = document.Layers[0].Id;
        document.AddElement(new BinkStroke(TestData.Line(new(200, 60), new(200, 140)), new BinkPixelEraserBrush()), layer);
        document.AddElement(new BinkStroke(TestData.Line(new(20, 100), new(380, 100)), new BinkMarkerBrush()), layer);

        var pdf = new Pdf(BinkPdfExporter.Export(document));

        Assert.IsFalse(pdf.Find("/SMask").Any());
    }

    [TestMethod]
    public void A_png_keeps_its_transparency_and_an_opaque_one_needs_no_mask()
    {
        BinkDocument document = Page();
        Guid layer = document.Layers[0].Id;
        byte[] translucent = TestData.CreatePng(4, 2, (x, _) => new BinkColor(255, 0, 0, (byte)(x * 60)));
        byte[] opaque = TestData.CreatePng(3, 3, (_, _) => BinkColor.Parse("#00ff00"));
        var source = BinkImageSource.FromBytes(translucent, "image/png", 4, 2);
        document.AddElement(new BinkImageElement(source, new BinkRect(10, 10, 40, 20)), layer);
        document.AddElement(new BinkImageElement(source, new BinkRect(60, 10, 40, 20)) { Crop = new BinkRect(1, 0, 2, 2) }, layer);
        document.AddElement(new BinkImageElement(BinkImageSource.FromBytes(opaque, "image/png", 3, 3), new BinkRect(10, 60, 30, 30)), layer);

        BinkPdfExportResult result = BinkPdfExporter.ExportWithDetails(document);
        var pdf = new Pdf(result.Bytes);

        Assert.AreEqual(0, result.SkippedImages);
        Assert.HasCount(2, pdf.Find("/Subtype /Image", "/ColorSpace /DeviceRGB"), "A picture drawn twice is stored once.");
        KeyValuePair<int, string> masked = pdf.Find("/Subtype /Image", "/Width 4", "/SMask").Single();
        int maskId = int.Parse(Regex.Match(masked.Value, @"/SMask (\d+) 0 R").Groups[1].Value, CultureInfo.InvariantCulture);
        StringAssert.Contains(pdf.Objects[maskId], "/ColorSpace /DeviceGray");
        Assert.DoesNotContain("/SMask", Pdf.Dictionary(pdf.Find("/Subtype /Image", "/Width 3").Single().Value));
        string content = pdf.AllContent;
        StringAssert.Contains(content, "60 10 40 20 re W n", "A crop clips to where the picture is drawn...");
        StringAssert.Contains(content, "80 0 0 -20 40 30 cm", "...and draws the whole picture scaled so the crop fills it.");
    }

    /// <summary>
    /// The smallest JPEG header the exporter reads: start of image, an optional Exif segment with an orientation, then
    /// a baseline frame header.
    /// </summary>
    private static byte[] Jpeg(int width, int height, int components, int orientation = 0, bool littleEndian = false)
    {
        var bytes = new List<byte> { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x04, 0x00, 0x00 };
        if (orientation > 0)
        {
            byte[] Short(int v) => littleEndian ? [(byte)v, (byte)(v >> 8)] : [(byte)(v >> 8), (byte)v];
            byte[] Long(int v) => littleEndian ? [(byte)v, (byte)(v >> 8), (byte)(v >> 16), (byte)(v >> 24)] : [(byte)(v >> 24), (byte)(v >> 16), (byte)(v >> 8), (byte)v];
            var exif = new List<byte>();
            exif.AddRange("Exif\0\0"u8.ToArray());
            exif.AddRange(littleEndian ? "II"u8.ToArray() : "MM"u8.ToArray());
            exif.AddRange(Short(42));
            exif.AddRange(Long(8));
            exif.AddRange(Short(1));
            exif.AddRange(Short(0x0112));
            exif.AddRange(Short(3));
            exif.AddRange(Long(1));
            exif.AddRange(Short(orientation));
            exif.AddRange([0, 0]);
            exif.AddRange(Long(0));
            bytes.AddRange([0xFF, 0xE1, (byte)((exif.Count + 2) >> 8), (byte)(exif.Count + 2)]);
            bytes.AddRange(exif);
        }
        bytes.AddRange([0xFF, 0xC0, 0x00, (byte)(8 + components * 3), 8, (byte)(height >> 8), (byte)height, (byte)(width >> 8), (byte)width, (byte)components]);
        for (int i = 0; i < components; i++) bytes.AddRange([(byte)(i + 1), 0x11, 0]);
        bytes.AddRange([0xFF, 0xD9]);
        return [.. bytes];
    }

    [TestMethod]
    public void A_jpeg_is_copied_in_as_it_is_and_what_cannot_be_embedded_is_reported()
    {
        BinkDocument document = Page();
        Guid layer = document.Layers[0].Id;
        byte[] jpeg = Jpeg(300, 200, 3);
        document.AddElement(new BinkImageElement(BinkImageSource.FromBytes(jpeg, "image/jpeg", 300, 200), new BinkRect(0, 0, 150, 100)), layer);
        document.AddElement(new BinkImageElement(BinkImageSource.FromBytes(Jpeg(10, 10, 4), "image/jpeg", 10, 10), new BinkRect(0, 0, 10, 10)), layer);
        document.AddElement(new BinkImageElement(BinkImageSource.FromUri("https://example.com/a.png", 10, 10), new BinkRect(0, 0, 10, 10)), layer);
        document.AddElement(new BinkImageElement(BinkImageSource.FromBytes([1, 2, 3], "image/webp", 10, 10), new BinkRect(0, 0, 10, 10)), layer);

        BinkPdfExportResult result = BinkPdfExporter.ExportWithDetails(document);
        var pdf = new Pdf(result.Bytes);

        string image = pdf.Find("/Subtype /Image", "/Filter /DCTDecode").Single().Value;
        StringAssert.Contains(image, "/Width 300 /Height 200 /ColorSpace /DeviceRGB");
        StringAssert.Contains(image, $"/Length {jpeg.Length}");
        Assert.AreEqual(3, result.SkippedImages, "CMYK, remote and WebP pictures are reported, not drawn wrongly.");
        Assert.IsFalse(result.IsComplete);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void A_photo_is_placed_turned_as_its_exif_orientation_says_a_browser_shows_it(bool littleEndian)
    {
        // Stored 300 by 200 and marked "turn 90 degrees clockwise": a browser shows it 200 wide and 300 high, and so
        // must the PDF, whose DCT filter ignores the mark.
        BinkDocument document = Page();
        byte[] photo = Jpeg(300, 200, 3, orientation: 6, littleEndian);
        document.AddElement(new BinkImageElement(BinkImageSource.FromBytes(photo, "image/jpeg", 200, 300), new BinkRect(0, 0, 100, 150)), document.Layers[0].Id);

        string content = new Pdf(BinkPdfExporter.Export(document, new BinkPdfExportOptions { Compress = false })).AllContent;

        StringAssert.Contains(content, "0 150 100 0 0 0 cm", "The stored rows run down the right of the picture, its columns across it.");
    }

    [TestMethod]
    public void A_crop_of_a_turned_photo_is_measured_in_the_picture_as_shown()
    {
        BinkDocument document = Page();
        byte[] photo = Jpeg(300, 200, 3, orientation: 8);
        // The bottom half of the picture as shown: 200 wide, 150 of its 300 high.
        document.AddElement(new BinkImageElement(BinkImageSource.FromBytes(photo, "image/jpeg", 200, 300), new BinkRect(0, 0, 200, 150)) { Crop = new BinkRect(0, 150, 200, 150) }, document.Layers[0].Id);

        string content = new Pdf(BinkPdfExporter.Export(document, new BinkPdfExportOptions { Compress = false })).AllContent;

        // The whole picture, 200 by 300, sits 150 above the destination; orientation 8 maps (u, v) to (1 - v, 1 - u).
        StringAssert.Contains(content, "0 0 200 150 re W n");
        StringAssert.Contains(content, "0 -300 -200 0 200 150 cm");
    }

    [TestMethod]
    public void A_picture_claiming_more_pixels_than_allowed_is_skipped_before_it_is_decoded()
    {
        BinkDocument document = Page();
        byte[] png = TestData.CreatePng(40, 40, (_, _) => BinkColor.Black);
        document.AddElement(new BinkImageElement(BinkImageSource.FromBytes(png, "image/png", 40, 40), new BinkRect(0, 0, 40, 40)), document.Layers[0].Id);

        BinkPdfExportResult result = BinkPdfExporter.ExportWithDetails(document, new BinkPdfExportOptions { MaxImagePixels = 1000 });

        Assert.AreEqual(1, result.SkippedImages);
        Assert.IsFalse(new Pdf(result.Bytes).Find("/Subtype /Image").Any());
    }

    [TestMethod]
    public void Text_is_real_text_in_a_standard_face_and_other_scripts_are_reported()
    {
        BinkDocument document = Page();
        Guid layer = document.Layers[0].Id;
        document.AddElement(new BinkTextElement(new Vector2(20, 20), "Total (net): 5 €\nsecond line") { Style = BinkTextStyle.Default with { Bold = true, FontSize = 20 } }, layer);
        document.AddElement(new BinkTextElement(new Vector2(20, 100), "code") { Style = BinkTextStyle.Default with { FontFamily = "Consolas, monospace", Align = BinkTextAlign.Right } }, layer);
        document.AddElement(new BinkTextElement(new Vector2(20, 150), "Note") { Style = BinkTextStyle.Default with { FontFamily = "Georgia, serif", Italic = true } }, layer);
        document.AddElement(new BinkTextElement(new Vector2(20, 200), "سلام دنیا"), layer);

        BinkPdfExportResult result = BinkPdfExporter.ExportWithDetails(document, new BinkPdfExportOptions { Compress = false });
        var pdf = new Pdf(result.Bytes);
        string content = pdf.AllContent;

        Assert.AreEqual(1, result.SkippedTextElements, "The Persian text cannot be set in the standard fonts.");
        StringAssert.Contains(content, @"(Total \(net\): 5 \200) Tj", "Parentheses are escaped and the euro is WinAnsi's 0x80.");
        StringAssert.Contains(content, "(second line) Tj");
        StringAssert.Contains(content, "1 0 0 -1 20 36.4 Tm", "The first baseline sits 0.82 of the size below the top, as in SVG.");
        StringAssert.Contains(content, "1 0 0 -1 20 61.4 Tm", "Lines are a line height apart.");
        StringAssert.Contains(content, "1 0 0 -1 -18.4 113.12 Tm", "Right-aligned Courier ends at the origin: four characters of 0.6 em at 16 units.");
        Assert.IsTrue(pdf.Find("/BaseFont /Helvetica-Bold").Any());
        Assert.IsTrue(pdf.Find("/BaseFont /Courier ").Any());
        Assert.IsTrue(pdf.Find("/BaseFont /Times-Italic").Any());
    }

    [TestMethod]
    public void Documents_become_pages_each_the_size_of_its_own_region()
    {
        BinkDocument first = Page(400, 300);
        BinkDocument second = Page(200, 800);

        BinkPdfExportResult result = BinkPdfExporter.ExportWithDetails([first, second], new BinkPdfExportOptions { Scale = 1f });
        var pdf = new Pdf(result.Bytes);

        Assert.AreEqual(2, result.PageCount);
        StringAssert.Contains(pdf.Find("/Type /Pages").Single().Value, "/Count 2");
        string[] boxes = [.. pdf.Find("/Type /Page ").Select(p => Regex.Match(p.Value, @"/MediaBox \[[^\]]+\]").Value)];
        CollectionAssert.AreEqual(new[] { "/MediaBox [0 0 400 300]", "/MediaBox [0 0 200 800]" }, boxes);
    }

    [TestMethod]
    public void The_properties_carry_any_language_and_the_title_is_shown()
    {
        var pdf = new Pdf(BinkPdfExporter.Export(Page(), new BinkPdfExportOptions { Title = "Plan (ü)", Description = "طرح", Author = "Bink", Language = "fa-IR" }));

        string info = pdf.Find("/Producer (Bit.Bink)").Single().Value;
        StringAssert.Contains(info, "/Title <FEFF0050006C0061006E0020002800FC0029>", "UTF-16, so any character survives.");
        StringAssert.Contains(info, "/Subject <FEFF06370631062D>");
        string catalog = pdf.Find("/Type /Catalog").Single().Value;
        StringAssert.Contains(catalog, "/DisplayDocTitle true");
        StringAssert.Contains(catalog, "/Lang <FEFF00660061002D00490052>");
    }

    [TestMethod]
    public void A_selection_exports_alone_cropped_to_itself()
    {
        BinkDocument document = Page();
        Guid layer = document.Layers[0].Id;
        var keep = new BinkRectangleShape(new BinkRect(100, 100, 50, 50));
        document.AddElement(keep, layer);
        document.AddElement(new BinkEllipseShape(new BinkRect(300, 200, 50, 50)), layer);

        var pdf = new Pdf(BinkPdfExporter.Export(document, new BinkPdfExportOptions { Region = BinkExportRegion.Selection, SelectedIds = [keep.Id], Padding = 0, Scale = 1, Compress = false }));

        BinkRect bounds = keep.Bounds;
        StringAssert.Contains(pdf.Find("/Type /Page ").Single().Value, $"/MediaBox [0 0 {BinkPdfFile.Number(bounds.Width)} {BinkPdfFile.Number(bounds.Height)}]");
        Assert.AreEqual(1, Regex.Matches(pdf.AllContent, "\nS\n").Count, "Only the selected shape is drawn.");
    }

    [TestMethod]
    public void Bad_arguments_are_refused()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => BinkPdfExporter.Export(Page(), new BinkPdfExportOptions { Scale = 0 }));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => BinkPdfExporter.Export(Page(), new BinkPdfExportOptions { Scale = float.NaN }));
        Assert.ThrowsExactly<ArgumentException>(() => BinkPdfExporter.ExportWithDetails(Array.Empty<BinkDocument>()));
        Assert.ThrowsExactly<ArgumentException>(() => BinkPdfExporter.ExportWithDetails([Page(), null!]));
    }

    [TestMethod]
    public void Numbers_are_written_the_way_pdf_reads_them()
    {
        Assert.AreEqual("0", BinkPdfFile.Number(float.NaN));
        Assert.AreEqual("0", BinkPdfFile.Number(float.PositiveInfinity));
        Assert.AreEqual("0", BinkPdfFile.Number(-0.0001f));
        Assert.AreEqual("1234567", BinkPdfFile.Number(1234567f), "Never in exponent form.");
        Assert.AreEqual("-2.5", BinkPdfFile.Number(-2.5f));
        Assert.AreEqual("0.333", BinkPdfFile.Number(1f / 3f));
    }

    [TestMethod]
    public void Every_character_the_encoding_has_is_measured_in_every_face()
    {
        // The width tables cover the printable ASCII range one entry per character; one missing entry would throw
        // for the characters after it or shift every width.
        var all = new StringBuilder();
        for (char c = ' '; c <= '~'; c++) all.Append(c);
        all.Append("€éÿ—™");
        Assert.IsTrue(BinkPdfFont.TryEncode(all.ToString(), out byte[] encoded));
        foreach (BinkPdfFace face in Enum.GetValues<BinkPdfFace>())
        {
            Assert.IsGreaterThan(0f, BinkPdfFont.Measure(encoded, face, bold: false));
            Assert.IsGreaterThan(0f, BinkPdfFont.Measure(encoded, face, bold: true));
        }
        Assert.AreEqual(0.278f + 0.556f, BinkPdfFont.Measure("1"u8.ToArray().Prepend((byte)' ').ToArray(), BinkPdfFace.Helvetica, bold: false), 0.0001f);
        Assert.AreEqual(0.584f, BinkPdfFont.Measure("~"u8, BinkPdfFace.Helvetica, bold: false), 0.0001f, "The last entry is the tilde's.");
        Assert.IsFalse(BinkPdfFont.TryEncode("Ω", out _));
    }

    [TestMethod]
    public void A_stream_receives_the_same_file()
    {
        BinkDocument document = TestData.SampleDocument();
        using var stream = new MemoryStream();
        BinkPdfExportResult result = BinkPdfExporter.Export(document, stream);

        CollectionAssert.AreEqual(result.Bytes, stream.ToArray());
        CollectionAssert.AreEqual(BinkPdfExporter.Export(document), stream.ToArray());
    }
}
