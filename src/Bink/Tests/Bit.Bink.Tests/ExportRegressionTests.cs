using System.Buffers.Binary;
using System.Globalization;
using System.Numerics;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Bit.Bink.Tests;

/// <summary>
/// Regressions in SVG and PNG export, compositing and the PNG codec.
/// </summary>
[TestClass]
public class ExportRegressionTests
{
    /// <summary>U+FFFE, U+FFFF and unpaired surrogates are not XML characters; text holding them broke the file.</summary>
    [TestMethod]
    public void Svg_export_stays_parseable_with_characters_xml_forbids()
    {
        var document = new BinkDocument(new BinkDocumentSettings { Width = 200, Height = 100 });
        document.AddElement(new BinkTextElement(new Vector2(10, 10), "a￾b￿c\uD800d\uDC00e😀f") { Description = "x\uDBFF" });

        string svg = BinkSvgExporter.Export(document, new BinkSvgExportOptions { IncludeElementDescriptions = true, Title = "t￿" });

        XDocument parsed = XDocument.Parse(svg);
        // A valid pair survives untouched.
        Assert.Contains("😀", parsed.Root!.Value, StringComparison.Ordinal);
    }

    /// <summary>Numbers longer than the 32-character format buffer were written as 0.</summary>
    [TestMethod]
    public void Svg_numbers_keep_large_values()
    {
        using var writer = new StringWriter(CultureInfo.InvariantCulture);
        var context = new BinkSvgRenderContext(writer, decimals: 2);
        context.ClipRect(new BinkRect(1e30f, 0, 10, 10));
        context.CloseAll();

        Match x = Regex.Match(writer.ToString(), "<rect x=\"([^\"]*)\"");
        Assert.IsTrue(x.Success);
        Assert.AreEqual(1e30f, float.Parse(x.Groups[1].Value, CultureInfo.InvariantCulture), 1e25f);
    }

    /// <summary>
    /// A translucent pixel eraser erases partially on canvas and in the rasterizer, but its SVG mask was always
    /// opaque black, which erased completely.
    /// </summary>
    [TestMethod]
    public void A_translucent_eraser_masks_partially_in_svg()
    {
        var document = new BinkDocument(new BinkDocumentSettings { Width = 200, Height = 100 });
        document.AddElement(TestData.Stroke(new Vector2(10, 50), new Vector2(190, 50), new BinkPenBrush { Width = 20f }));
        document.AddElement(TestData.Stroke(new Vector2(100, 10), new Vector2(100, 90), new BinkPixelEraserBrush { Width = 20f, Opacity = 0.5f }));

        string svg = BinkSvgExporter.Export(document);

        Match mask = Regex.Match(svg, "<mask .*?</mask>");
        Assert.IsTrue(mask.Success);
        Assert.Contains("fill-opacity=\"0.5\"", mask.Value, StringComparison.Ordinal);

        // The rasterizer leaves about half the ink under the eraser, which is what the mask now expresses.
        BinkRasterSurface surface = BinkPngExporter.Render(document, new BinkPngExportOptions { Background = BinkColor.Transparent }, out _, out _);
        Assert.IsInRange(100, 160, surface.GetPixel(100, 50).A);
    }

    /// <summary>Compositing a group with destination-out painted it like a normal group instead of erasing.</summary>
    [TestMethod]
    public void Compositing_with_destination_out_erases()
    {
        var target = new BinkRasterSurface(2, 1);
        target.Clear(new BinkColor(255, 0, 0));
        var source = new BinkRasterSurface(2, 1);
        source.Blend(0, 0, [1f, 0.5f], BinkColor.Black, 1f, BinkBlendMode.Normal);

        target.Composite(source, 1f, BinkBlendMode.DestinationOut, target.Bounds);

        Assert.AreEqual(0, target.GetPixel(0, 0).A);
        Assert.IsInRange(126, 129, target.GetPixel(1, 0).A);
        Assert.AreEqual(255, target.GetPixel(1, 0).R);
    }

    /// <summary>
    /// With a pixel limit above what an array can hold, the buffer sizes wrapped in 32-bit arithmetic: decoding
    /// allocated a buffer unrelated to the image instead of rejecting it.
    /// </summary>
    [TestMethod]
    public void Decoding_an_image_too_large_for_an_array_is_a_format_error()
    {
        var png = new MemoryStream();
        png.Write([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]);
        byte[] header = new byte[13];
        BinaryPrimitives.WriteInt32BigEndian(header, 30_000);
        BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(4), 30_000);
        header[8] = 8;
        header[9] = 6;
        WriteChunk(png, "IHDR", header);
        WriteChunk(png, "IEND", []);

        Assert.ThrowsExactly<FormatException>(() => BinkPngCodec.Decode(png.ToArray(), maxPixels: long.MaxValue));
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

    /// <summary>The SVG exporter treats an empty selection as no filter; the PNG exporter drew nothing at all.</summary>
    [TestMethod]
    public void An_empty_selection_filters_nothing_in_either_exporter()
    {
        var document = new BinkDocument(new BinkDocumentSettings { Width = 200, Height = 100, Background = BinkColor.Transparent });
        document.AddElement(TestData.Stroke(new Vector2(10, 50), new Vector2(190, 50), new BinkPenBrush { Width = 20f }));
        Guid[] none = [];

        BinkRasterSurface surface = BinkPngExporter.Render(document, new BinkPngExportOptions { SelectedIds = none }, out _, out _);
        string svg = BinkSvgExporter.Export(document, new BinkSvgExportOptions { SelectedIds = none });

        Assert.AreEqual(255, surface.GetPixel(100, 50).A);
        Assert.Contains("<path", svg, StringComparison.Ordinal);
    }

    /// <summary>The callout validated its text only in the constructor, so a <c>with</c> could store null.</summary>
    [TestMethod]
    public void Callout_text_is_validated_in_with_expressions()
    {
        var callout = new BinkCalloutElement(new BinkRect(0, 0, 100, 40), new Vector2(150, 80), "note");

        Assert.ThrowsExactly<ArgumentNullException>(() => callout with { Text = null! });
        Assert.ThrowsExactly<ArgumentException>(() => callout with { Text = new string('x', BinkTextElement.MaxLength + 1) });
        Assert.AreEqual("changed", (callout with { Text = "changed" }).Text);
    }
}
