using System.Buffers.Binary;
using System.IO.Compression;
using System.Numerics;

namespace Bit.Bink.Tests;

/// <summary>
/// The server-side rasterizer runs on untrusted documents, so what an embedded image can make it allocate is
/// bounded by the export's own limits, and isolating a layer costs no more than the area it draws.
/// </summary>
[TestClass]
public class RasterRobustnessTests
{
    /// <summary>
    /// The decoder sized its buffer from the header before checking any data: a hundred bytes claiming 8000 by
    /// 8000 pixels, with the pixel data cut short, allocated 256 MB before failing.
    /// </summary>
    [TestMethod]
    public void A_truncated_png_claiming_a_huge_size_fails_without_allocating_it()
    {
        byte[] bomb = CreatePng(8000, 8000, colorType: 6, rows: Compress(new byte[4000]));
        Assert.IsLessThan(200, bomb.Length);

        long before = GC.GetAllocatedBytesForCurrentThread();
        Assert.ThrowsExactly<FormatException>(() => BinkPngCodec.Decode(bomb));
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.IsLessThan(8L * 1024 * 1024, allocated, $"{allocated} bytes were allocated for a file of {bomb.Length} bytes");
    }

    [TestMethod]
    public void A_png_claiming_one_enormous_row_fails_without_allocating_it()
    {
        byte[] bomb = CreatePng(16_000_000, 1, colorType: 6, rows: Compress(new byte[64]));

        long before = GC.GetAllocatedBytesForCurrentThread();
        Assert.ThrowsExactly<FormatException>(() => BinkPngCodec.Decode(bomb));
        Assert.IsLessThan(8L * 1024 * 1024, GC.GetAllocatedBytesForCurrentThread() - before);
    }

    [TestMethod]
    public void Decoding_still_produces_exactly_the_image()
    {
        // Growing the buffers as rows arrive must not change what is decoded, whatever the filter of each row.
        byte[] png = TestData.CreatePng(301, 97, (x, y) => new BinkColor((byte)(x * 7), (byte)(y * 3), (byte)(x ^ y), (byte)(255 - x % 200)));
        BinkPngCodec.DecodedImage decoded = BinkPngCodec.Decode(png);

        Assert.AreEqual(301, decoded.Width);
        Assert.AreEqual(97, decoded.Height);
        Assert.HasCount(301 * 97 * 4, decoded.Rgba);
        for (int y = 0; y < 97; y++)
        {
            for (int x = 0; x < 301; x++)
            {
                int o = (y * 301 + x) * 4;
                Assert.AreEqual(new BinkColor((byte)(x * 7), (byte)(y * 3), (byte)(x ^ y), (byte)(255 - x % 200)), new BinkColor(decoded.Rgba[o], decoded.Rgba[o + 1], decoded.Rgba[o + 2], decoded.Rgba[o + 3]));
            }
        }
    }

    /// <summary>Embedded images were decoded within the codec's 64-million-pixel default, whatever the export allowed.</summary>
    [TestMethod]
    public void An_embedded_image_larger_than_the_export_allows_is_skipped()
    {
        byte[] png = TestData.CreatePng(1500, 1000, (_, _) => BinkColor.Black);
        var document = new BinkDocument(new BinkDocumentSettings { Width = 100, Height = 100 });
        document.AddElement(new BinkImageElement(BinkImageSource.FromBytes(png, "image/png", 1500, 1000), new BinkRect(0, 0, 100, 100)));

        BinkPngExportResult result = BinkPngExporter.ExportWithDetails(document, new BinkPngExportOptions { MaxPixels = 1_000_000 });
        Assert.AreEqual(1, result.SkippedImages);

        // Within the limit it is drawn.
        Assert.AreEqual(0, BinkPngExporter.ExportWithDetails(document, new BinkPngExportOptions { MaxPixels = 2_000_000 }).SkippedImages);
    }

    /// <summary>
    /// Every decoded image stayed in the cache for the whole export, so twenty distinct images each within the
    /// limit could still need twenty times the memory the limit was meant to bound.
    /// </summary>
    [TestMethod]
    public void Decoded_images_share_one_budget_per_export()
    {
        var document = new BinkDocument(new BinkDocumentSettings { Width = 200, Height = 200, Background = BinkColor.White });
        for (int i = 0; i < 20; i++)
        {
            byte shade = (byte)(i * 10);
            byte[] png = TestData.CreatePng(400, 400, (_, _) => new BinkColor(shade, 0, 0));
            document.AddElement(new BinkImageElement(BinkImageSource.FromBytes(png, "image/png", 400, 400), new BinkRect(i * 10, 0, 10, 10)));
        }

        // 160,000 pixels each against a budget of a million: six fit, the other fourteen are reported.
        BinkPngExportResult result = BinkPngExporter.ExportWithDetails(document, new BinkPngExportOptions { MaxPixels = 1_000_000 });
        Assert.AreEqual(14, result.SkippedImages);
        Assert.IsFalse(result.IsComplete);
    }

    [TestMethod]
    public void Drawing_the_same_image_again_costs_nothing_from_the_budget()
    {
        byte[] png = TestData.CreatePng(400, 400, (_, _) => BinkColor.Black);
        var source = BinkImageSource.FromBytes(png, "image/png", 400, 400);
        var document = new BinkDocument(new BinkDocumentSettings { Width = 200, Height = 200 });
        for (int i = 0; i < 20; i++) document.AddElement(new BinkImageElement(source, new BinkRect(i * 10, 0, 10, 10)));

        Assert.AreEqual(0, BinkPngExporter.ExportWithDetails(document, new BinkPngExportOptions { MaxPixels = 1_000_000 }).SkippedImages);
    }

    /// <summary>
    /// A very large image drawn into a few device pixels was decoded at full resolution, so one compressible
    /// file could use the whole budget for a thumbnail.
    /// </summary>
    [TestMethod]
    public void A_huge_image_drawn_tiny_is_not_decoded()
    {
        // 5000 by 5000 transparent pixels compress to about a hundred kilobytes of valid data.
        byte[] png = CreatePng(5000, 5000, colorType: 6, rows: CompressRows(5000, 5000, 4));
        var document = new BinkDocument(new BinkDocumentSettings { Width = 100, Height = 100 });
        document.AddElement(new BinkImageElement(BinkImageSource.FromBytes(png, "image/png", 5000, 5000), new BinkRect(10, 10, 10, 10)));

        long before = GC.GetAllocatedBytesForCurrentThread();
        BinkPngExportResult result = BinkPngExporter.ExportWithDetails(document);
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.AreEqual(1, result.SkippedImages);
        Assert.IsLessThan(16L * 1024 * 1024, allocated, $"{allocated} bytes were allocated to draw a 10 by 10 thumbnail");
    }

    /// <summary>tRNS was honoured only for palette images; colour-key transparency in RGB and grey PNGs was ignored.</summary>
    [TestMethod]
    public void Colour_key_transparency_is_honoured_for_rgb_images()
    {
        // Two pixels, white and dark blue, keyed on white.
        byte[] png = CreatePng(2, 1, colorType: 2, rows: Compress([0, 255, 255, 255, 10, 20, 30]), transparency: [0, 255, 0, 255, 0, 255]);
        BinkPngCodec.DecodedImage decoded = BinkPngCodec.Decode(png);

        Assert.AreEqual(0, decoded.Rgba[3], "the keyed colour must be transparent");
        Assert.AreEqual(255, decoded.Rgba[7]);
        Assert.AreEqual(10, decoded.Rgba[4]);
    }

    [TestMethod]
    public void Colour_key_transparency_is_honoured_for_grey_images()
    {
        byte[] png = CreatePng(3, 1, colorType: 0, rows: Compress([0, 0, 128, 0]), transparency: [0, 0]);
        BinkPngCodec.DecodedImage decoded = BinkPngCodec.Decode(png);

        Assert.AreEqual(0, decoded.Rgba[3]);
        Assert.AreEqual(255, decoded.Rgba[7]);
        Assert.AreEqual(128, decoded.Rgba[4]);
        Assert.AreEqual(0, decoded.Rgba[11]);
    }

    [TestMethod]
    public void A_sixteen_bit_key_matches_no_eight_bit_sample()
    {
        // The key is stored in 16 bits; in an 8-bit image a value above 255 can never occur.
        byte[] png = CreatePng(1, 1, colorType: 0, rows: Compress([0, 1]), transparency: [1, 1]);
        Assert.AreEqual(255, BinkPngCodec.Decode(png).Rgba[3]);
    }

    [TestMethod]
    public void An_rgb_image_keyed_on_white_is_not_an_opaque_box_in_png_export()
    {
        byte[] png = CreatePng(2, 2, colorType: 2, rows: Compress([0, 255, 255, 255, 255, 255, 255, 0, 255, 255, 255, 255, 255, 255]), transparency: [0, 255, 0, 255, 0, 255]);
        var document = new BinkDocument(new BinkDocumentSettings { Width = 20, Height = 20, Background = new BinkColor(200, 0, 0) });
        document.AddElement(new BinkImageElement(BinkImageSource.FromBytes(png, "image/png", 2, 2), new BinkRect(0, 0, 20, 20)));

        BinkRasterSurface surface = BinkPngExporter.Render(document, null, out _, out int skipped);
        Assert.AreEqual(0, skipped);
        Assert.AreEqual(new BinkColor(200, 0, 0), surface.GetPixel(10, 10));
    }

    /// <summary>
    /// Each isolated layer allocated a scratch surface the size of the whole export and composited all of it: at
    /// the default limit that is 320 MB per layer.
    /// </summary>
    [TestMethod]
    public void Isolated_layers_share_one_scratch_surface()
    {
        var document = new BinkDocument(new BinkDocumentSettings { Width = 1000, Height = 1000, Background = BinkColor.White });
        for (int i = 0; i < 12; i++)
        {
            BinkLayer layer = document.AddLayer("Layer " + i);
            document.UpdateLayer(layer.Id, p => p with { Opacity = 0.5f });
            document.AddElement(TestData.Stroke(new Vector2(20, 20 + i * 70), new Vector2(980, 60 + i * 70), new BinkPenBrush { Width = 8f }), layer.Id);
        }
        BinkPngExporter.Render(document, null, out _, out _);

        long before = GC.GetAllocatedBytesForCurrentThread();
        BinkPngExporter.Render(document, null, out _, out _);
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        long surfaceBytes = 1000L * 1000 * 4;
        Assert.IsLessThan(3 * surfaceBytes, allocated, $"{allocated / (double)surfaceBytes:0.0} surfaces were allocated for 12 isolated layers");
    }

    [TestMethod]
    public void Pooled_groups_render_exactly_what_fresh_surfaces_did()
    {
        var document = new BinkDocument(new BinkDocumentSettings { Width = 240, Height = 200, Background = BinkColor.White });
        Guid page = document.Layers[0].Id;
        document.AddElement(TestData.Stroke(new Vector2(10, 100), new Vector2(230, 100), new BinkPenBrush { Width = 30f }), page);
        document.AddElement(new BinkHighlightElement(new BinkRect(30, 80, 120, 40)), page);

        BinkLayer erased = document.AddLayer("Erased");
        document.AddElement(TestData.Stroke(new Vector2(10, 40), new Vector2(230, 160), new BinkPenBrush { Width = 18f, Color = new BinkColor(30, 90, 200) }), erased.Id);
        document.AddElement(TestData.Stroke(new Vector2(120, 0), new Vector2(120, 200), new BinkPixelEraserBrush { Width = 24f }), erased.Id);
        document.AddElement(TestData.Stroke(new Vector2(20, 180), new Vector2(220, 180), new BinkHighlighterBrush { Width = 20f }), erased.Id);

        BinkLayer faded = document.AddLayer("Faded");
        document.UpdateLayer(faded.Id, p => p with { Opacity = 0.4f });
        document.AddElement(new BinkEllipseShape(new BinkRect(60, 20, 120, 90)) { Style = new BinkShapeStyle { StrokeWidth = 6f, FillColor = new BinkColor(250, 200, 0) } }, faded.Id);

        BinkLayer small = document.AddLayer("Small");
        document.UpdateLayer(small.Id, p => p with { Opacity = 0.7f });
        document.AddElement(TestData.Stroke(new Vector2(200, 20), new Vector2(210, 30), new BinkPenBrush { Width = 4f }), small.Id);

        BinkRasterSurface pooled = BinkPngExporter.Render(document, null, out _, out _);

        // The previous implementation: a fresh full-size surface per isolated layer, composited over its whole area.
        var reference = new BinkRasterSurface(240, 200);
        reference.Clear(BinkColor.White);
        foreach (BinkLayer layer in document.Layers)
        {
            bool isolate = layer.Opacity < 1f || layer.Elements.Any(e => e is BinkStroke { IsEraser: true });
            BinkRasterSurface target = isolate ? new BinkRasterSurface(240, 200) : reference;
            var context = new BinkRasterRenderContext(target, Matrix3x2.Identity);
            foreach (BinkElement element in layer.Elements) element.Draw(context);
            if (isolate) reference.Composite(target, layer.Opacity, BinkBlendMode.Normal, reference.Bounds);
        }

        Assert.IsTrue(pooled.Pixels.SequenceEqual(reference.Pixels), "pooling changed the rendered bytes");
    }

    [TestMethod]
    public void A_reused_scratch_surface_starts_empty()
    {
        var surface = new BinkRasterSurface(40, 10);
        surface.Clear(new BinkColor(200, 0, 0));
        var context = new BinkRasterRenderContext(surface, Matrix3x2.Identity);
        var square = new BinkPathBuilder(6).AddRect(new BinkRect(0, 0, 10, 10)).ToGeometry();
        var other = new BinkPathBuilder(6).AddRect(new BinkRect(20, 0, 10, 10)).ToGeometry();

        // The first group leaves pixels on the left of its scratch surface.
        context.PushGroup(1f, BinkBlendMode.Normal);
        context.FillPath(square, new BinkFillPaint(BinkColor.Black));
        context.PopGroup();

        // The second group, at the same depth, erases with whatever it holds. Anything the first left behind
        // would erase the left square too.
        context.PushGroup(1f, BinkBlendMode.DestinationOut);
        context.FillPath(other, new BinkFillPaint(BinkColor.Black));
        context.PopGroup();

        Assert.AreEqual(BinkColor.Black, surface.GetPixel(5, 5));
        Assert.AreEqual(0, surface.GetPixel(25, 5).A);
        Assert.AreEqual(new BinkColor(200, 0, 0), surface.GetPixel(35, 5));
    }

    [TestMethod]
    public void Nested_groups_composite_what_their_children_drew()
    {
        var surface = new BinkRasterSurface(40, 10);
        var context = new BinkRasterRenderContext(surface, Matrix3x2.Identity);
        context.PushGroup(1f, BinkBlendMode.Normal);
        context.PushGroup(0.5f, BinkBlendMode.Normal);
        context.FillPath(new BinkPathBuilder(6).AddRect(new BinkRect(30, 0, 10, 10)).ToGeometry(), new BinkFillPaint(BinkColor.Black));
        context.PopGroup();
        context.PopGroup();

        Assert.IsInRange(126, 129, surface.GetPixel(35, 5).A);
        Assert.AreEqual(0, surface.GetPixel(5, 5).A);
    }

    /// <summary>
    /// The rasterizer raised every dash entry to at least 0.05, so an all-zero dash came out as a dense dashed
    /// line where the canvas and SVG, which treat a pattern with no length as no pattern, draw it solid.
    /// </summary>
    [TestMethod]
    [DataRow(new[] { 0f, 0f })]
    [DataRow(new[] { 0f })]
    [DataRow(new[] { -3f, 0f })]
    public void A_dash_pattern_with_no_length_strokes_solid(float[] dash)
    {
        var line = new BinkPathBuilder(4).MoveTo(new Vector2(5, 10)).LineTo(new Vector2(95, 10)).ToGeometry();

        BinkRasterSurface Draw(float[]? pattern)
        {
            var surface = new BinkRasterSurface(100, 20);
            new BinkRasterRenderContext(surface, Matrix3x2.Identity)
                .StrokePath(line, new BinkStrokePaint(BinkColor.Black, 4f, Cap: BinkLineCap.Butt, Dash: pattern));
            return surface;
        }

        Assert.IsTrue(Draw(dash).Pixels.SequenceEqual(Draw(null).Pixels));
    }

    private static byte[] Compress(byte[] data)
    {
        using var output = new MemoryStream();
        using (var zlib = new ZLibStream(output, CompressionLevel.SmallestSize, leaveOpen: true)) zlib.Write(data);
        return output.ToArray();
    }

    private static byte[] CompressRows(int width, int height, int channels)
    {
        using var output = new MemoryStream();
        using (var zlib = new ZLibStream(output, CompressionLevel.SmallestSize, leaveOpen: true))
        {
            byte[] row = new byte[width * channels + 1];
            for (int y = 0; y < height; y++) zlib.Write(row);
        }
        return output.ToArray();
    }

    private static byte[] CreatePng(int width, int height, byte colorType, byte[] rows, byte[]? transparency = null)
    {
        using var png = new MemoryStream();
        png.Write([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]);
        byte[] header = new byte[13];
        BinaryPrimitives.WriteInt32BigEndian(header, width);
        BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(4), height);
        header[8] = 8;
        header[9] = colorType;
        WriteChunk(png, "IHDR", header);
        if (transparency is not null) WriteChunk(png, "tRNS", transparency);
        WriteChunk(png, "IDAT", rows);
        WriteChunk(png, "IEND", []);
        return png.ToArray();
    }

    private static void WriteChunk(Stream stream, string type, byte[] data)
    {
        byte[] length = new byte[4];
        BinaryPrimitives.WriteInt32BigEndian(length, data.Length);
        stream.Write(length);
        stream.Write(System.Text.Encoding.ASCII.GetBytes(type));
        stream.Write(data);
        stream.Write(new byte[4]); // the decoder does not check CRCs
    }
}
