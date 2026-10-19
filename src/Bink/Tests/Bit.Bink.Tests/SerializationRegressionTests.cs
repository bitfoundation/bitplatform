using System.Globalization;
using System.Numerics;
using System.Xml;

namespace Bit.Bink.Tests;

/// <summary>
/// Regressions for reading untrusted documents and images: malformed input must always surface as an
/// <see cref="BinkFormatException"/> or a format error, never as an exception the caller was not told to expect.
/// </summary>
[TestClass]
public class SerializationRegressionTests(TestContext output)
{
    [TestMethod]
    public void Invalid_utf8_in_a_string_is_reported_as_a_format_error()
    {
        // JsonElement.GetString throws InvalidOperationException on a string that is not valid UTF-8. Callers were
        // promised BinkFormatException, and TryDeserialize only catches that one, so it would escape unhandled.
        string valid = BinkSerializer.Serialize(TestData.SampleDocument());
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(valid);
        int at = valid.IndexOf("Drawing", StringComparison.Ordinal);
        Assert.IsTrue(at > 0);
        bytes[at] = 0xC3;
        bytes[at + 1] = 0x28;

        Assert.ThrowsExactly<BinkFormatException>(() => BinkSerializer.Deserialize(bytes));
        Assert.IsFalse(BinkSerializer.TryDeserialize(bytes, out BinkDocument? parsed, out string? error));
        Assert.IsNull(parsed);
        Assert.IsFalse(string.IsNullOrEmpty(error));
    }

    [TestMethod]
    public void An_empty_metadata_key_does_not_fail_the_load()
    {
        const string json = """
            {"format":"bink","version":1,"metadata":{"":"x","keep":"y"},"layers":[{"elements":[]}]}
            """;
        BinkDocument document = BinkSerializer.Deserialize(json);
        Assert.AreEqual("y", document.Metadata["keep"]);
        Assert.IsFalse(document.Metadata.ContainsKey(string.Empty));
    }

    [TestMethod]
    public void Repeated_round_trips_converge()
    {
        BinkDocument document = TestData.SampleDocument();
        string a = BinkSerializer.Serialize(document);
        string b = BinkSerializer.Serialize(BinkSerializer.Deserialize(a));
        string c = BinkSerializer.Serialize(BinkSerializer.Deserialize(b));
        Assert.AreEqual(b, c);
    }

    [TestMethod]
    public void Editing_after_a_round_trip_still_undoes_correctly()
    {
        BinkDocument document = BinkSerializer.Deserialize(BinkSerializer.Serialize(TestData.SampleDocument()));
        using var history = new BinkHistoryManager(document);
        int before = document.ElementCount;
        document.AddElement(TestData.Stroke(new Vector2(5, 5), new Vector2(50, 50)));
        Assert.IsTrue(history.Undo());
        Assert.AreEqual(before, document.ElementCount);
        string round = BinkSerializer.Serialize(document);
        Assert.AreEqual(BinkSerializer.Serialize(BinkSerializer.Deserialize(round)), round);
    }

    [TestMethod]
    public void Mutated_documents_never_crash_the_reader()
    {
        string valid = BinkSerializer.Serialize(TestData.SampleDocument());
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(valid);
        var random = new Random(7);
        var unexpected = new List<string>();

        for (int i = 0; i < 5000; i++)
        {
            byte[] copy = (byte[])bytes.Clone();
            int mutations = 1 + random.Next(6);
            for (int m = 0; m < mutations; m++) copy[random.Next(copy.Length)] = (byte)random.Next(256);
            try
            {
                BinkSerializer.Deserialize(copy);
            }
            catch (BinkFormatException)
            {
            }
#pragma warning disable CA1031 // Anything other than BinkFormatException is exactly what this test is looking for.
            catch (Exception ex)
            {
                unexpected.Add($"{ex.GetType().Name}: {ex.Message}");
            }
#pragma warning restore CA1031
        }
        output.WriteLine($"unexpected: {unexpected.Count}");
        foreach (string u in unexpected.Distinct().Take(15)) output.WriteLine("  " + u);
        Assert.IsEmpty(unexpected);
    }

    [TestMethod]
    public void Structurally_hostile_documents_are_rejected_cleanly()
    {
        string[] hostile =
        [
            "{}",
            "[]",
            "null",
            "{\"format\":\"bink\"}",
            "{\"format\":\"bink\",\"version\":1,\"layers\":[]}",
            "{\"format\":\"bink\",\"version\":1,\"layers\":[{\"elements\":[{\"type\":\"stroke\",\"points\":{\"encoding\":\"json\",\"x\":[1e39],\"y\":[1]}}]}]}",
            "{\"format\":\"bink\",\"version\":1,\"settings\":{\"width\":-5,\"height\":-5},\"layers\":[]}",
            "{\"format\":\"bink\",\"version\":1,\"layers\":[{\"elements\":[{\"type\":\"stroke\",\"points\":{\"encoding\":\"b64\",\"data\":\"////////\"}}]}]}",
            "{\"format\":\"bink\",\"version\":1,\"layers\":[{\"elements\":[{\"type\":\"text\",\"position\":[0,0],\"text\":\"x\",\"textStyle\":{\"size\":1e30}}]}]}",
            "{\"format\":\"bink\",\"version\":1,\"layers\":[{\"elements\":[{\"type\":\"image\",\"src\":\"javascript:alert(1)\",\"rect\":[0,0,10,10]}]}]}",
            "{\"format\":\"bink\",\"version\":1,\"layers\":[{\"elements\":[{\"type\":\"ellipse\",\"rect\":[0,0,-1e30,-1e30]}]}]}",
            "{\"format\":\"bink\",\"version\":1,\"layers\":[{\"elements\":[{\"type\":\"stroke\",\"transform\":[1000000,0,0,1000000,0,0],\"points\":{\"encoding\":\"json\",\"x\":[0,100],\"y\":[0,100]}}]}]}",
        ];
        foreach (string json in hostile)
        {
            try
            {
                BinkDocument d = BinkSerializer.Deserialize(json);
                // Whatever survived must still be a coherent, exportable document.
                string svg = BinkSvgExporter.Export(d);
                var xml = new XmlDocument();
                xml.LoadXml(svg);
                output.WriteLine($"accepted: {d.ElementCount} elements, content bounds {d.ContentBounds}");
            }
            catch (BinkFormatException ex)
            {
                output.WriteLine($"rejected: {ex.Message}");
            }
        }
    }

    [TestMethod]
    public void A_loaded_page_size_is_clamped_like_an_edited_one()
    {
        // UpdateSettings clamps the page to the coordinate limit; a file used to be able to declare any size.
        BinkDocument document = BinkSerializer.Deserialize("{\"format\":\"bink\",\"version\":1,\"settings\":{\"width\":1e30,\"height\":-5},\"layers\":[]}");
        Assert.AreEqual(document.Limits.MaxCoordinate, document.Settings.Width);
        Assert.AreEqual(0f, document.Settings.Height);
    }

    [TestMethod]
    public void The_total_point_limit_holds_within_a_single_layer()
    {
        // Each stroke was checked against the document's total before any of its layer was counted, so one layer
        // could carry any number of points past MaxTotalPoints.
        var source = new BinkDocument();
        for (int i = 0; i < 3; i++) source.AddElement(new BinkStroke(TestData.Line(new Vector2(i * 10, 0), new Vector2(i * 10, 100), 40), new BinkPenBrush()));
        string json = BinkSerializer.Serialize(source);
        var options = new BinkDeserializerOptions { Limits = BinkDocumentLimits.Default with { MaxTotalPoints = 100 } };

        Assert.ThrowsExactly<BinkFormatException>(() => BinkSerializer.Deserialize(json, options));
    }

    [TestMethod]
    public void A_layer_arriving_with_repeated_element_ids_is_refused()
    {
        // A collaborator's addLayer is not repaired the way a loaded file is. Two elements behind one index entry
        // would leave one of them impossible to select, delete or undo.
        var document = new BinkDocument();
        BinkStroke stroke = TestData.Stroke(new Vector2(0, 0), new Vector2(10, 10));
        var add = new BinkAddLayerOperation(Guid.NewGuid(), new BinkLayerProperties(), null, [stroke, stroke with { Description = "twin" }]);

        Assert.ThrowsExactly<InvalidOperationException>(() => document.Apply(add, BinkChangeOrigin.Remote));
        Assert.ContainsSingle(document.Layers);
        Assert.AreEqual(0, document.ElementCount);
    }

    [TestMethod]
    public void Every_sample_export_is_well_formed_and_has_a_sane_view_box()
    {
        BinkDocument document = TestData.SampleDocument();
        document.AddElement(new BinkStroke(TestData.Line(new Vector2(30, 30), new Vector2(200, 100), 20), new BinkPixelEraserBrush { Width = 20 }));
        foreach (BinkExportRegion region in Enum.GetValues<BinkExportRegion>())
        {
            string svg = BinkSvgExporter.Export(document, new BinkSvgExportOptions { Region = region, IncludeXmlDeclaration = true, IncludeElementDescriptions = true });
            var xml = new XmlDocument();
            xml.LoadXml(svg);
            string viewBox = xml.DocumentElement!.GetAttribute("viewBox");
            output.WriteLine($"{region}: viewBox={viewBox} width={xml.DocumentElement.GetAttribute("width")}");
            Assert.IsFalse(string.IsNullOrWhiteSpace(viewBox));
            string[] parts = viewBox.Split(' ');
            Assert.AreEqual(4, parts.Length);
            Assert.IsTrue(float.Parse(parts[2], CultureInfo.InvariantCulture) > 0);
            Assert.IsTrue(float.Parse(parts[3], CultureInfo.InvariantCulture) > 0);
            Assert.DoesNotContain("NaN", svg, StringComparison.Ordinal);
            Assert.DoesNotContain("Infinity", svg, StringComparison.Ordinal);
            Assert.DoesNotContain("E+", svg, StringComparison.Ordinal);
        }
    }

    [TestMethod]
    public void A_fuzzed_png_never_escapes_the_decoder_as_an_unexpected_exception()
    {
        // The decoder runs on embedded data: URIs, which arrive inside untrusted documents.
        byte[] valid = BinkPngExporter.Export(TestData.SampleDocument(), new BinkPngExportOptions { Scale = 0.2f });
        var random = new Random(4242);
        var unexpected = new List<string>();
        int decoded = 0;

        for (int i = 0; i < 4000; i++)
        {
            byte[] copy = (byte[])valid.Clone();
            int mutations = 1 + random.Next(8);
            for (int m = 0; m < mutations; m++) copy[random.Next(copy.Length)] = (byte)random.Next(256);
            try
            {
                BinkPngCodec.DecodedImage image = BinkPngCodec.Decode(copy);
                decoded++;
                Assert.IsTrue(image.Width > 0 && image.Height > 0);
                Assert.AreEqual(image.Width * image.Height * 4, image.Rgba.Length);
            }
            catch (FormatException)
            {
            }
            catch (InvalidDataException)
            {
            }
#pragma warning disable CA1031
            catch (Exception ex)
            {
                unexpected.Add($"{ex.GetType().Name}: {ex.Message}");
            }
#pragma warning restore CA1031
        }

        output.WriteLine($"decoded {decoded} mutated images, {unexpected.Count} unexpected exceptions");
        foreach (string u in unexpected.Distinct().Take(10)) output.WriteLine("  " + u);
        Assert.IsEmpty(unexpected);
    }

    [TestMethod]
    public void Truncated_and_absurd_png_headers_are_refused_quickly()
    {
        byte[] valid = BinkPngExporter.Export(TestData.SampleDocument(), new BinkPngExportOptions { Scale = 0.2f });
        var cases = new List<byte[]>
        {
            Array.Empty<byte>(),
            new byte[] { 0 },
            valid[..8],
            valid[..20],
            valid[..(valid.Length / 2)],
        };

        // An IHDR claiming a colossal image must be refused rather than allocating for it.
        byte[] huge = (byte[])valid.Clone();
        huge[16] = 0x7F; huge[17] = 0xFF; huge[18] = 0xFF; huge[19] = 0xF0;
        huge[20] = 0x7F; huge[21] = 0xFF; huge[22] = 0xFF; huge[23] = 0xF0;
        cases.Add(huge);

        foreach (byte[] data in cases)
        {
            var clock = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                BinkPngCodec.Decode(data);
            }
            catch (Exception ex) when (ex is FormatException or InvalidDataException or ArgumentException)
            {
                output.WriteLine($"{data.Length} bytes rejected: {ex.Message}");
            }
            clock.Stop();
            Assert.IsTrue(clock.Elapsed.TotalSeconds < 5, $"decoding {data.Length} bytes took {clock.Elapsed.TotalSeconds:0.0} s");
        }
    }
}
