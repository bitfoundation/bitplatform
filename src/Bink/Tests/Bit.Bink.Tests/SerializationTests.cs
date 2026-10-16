using System.Numerics;
using System.Text;
using System.Text.Json;

namespace Bit.Bink.Tests;

[TestClass]
public class PointCodecTests
{
    [TestMethod]
    public void Round_trips_positions_within_the_quantization_step()
    {
        BinkStrokePoints original = TestData.PressureRamp(new Vector2(10.3f, 20.7f), new Vector2(300.15f, 180.9f), 50);
        BinkStrokePoints decoded = BinkPointCodec.Decode(BinkPointCodec.Encode(original));

        Assert.AreEqual(original.Count, decoded.Count);
        const float step = 1f / BinkPointCodec.DefaultQuantization;
        for (int i = 0; i < original.Count; i++)
        {
            Assert.AreEqual(original.X[i], decoded.X[i], step);
            Assert.AreEqual(original.Y[i], decoded.Y[i], step);
            Assert.AreEqual(original.Pressure[i], decoded.Pressure[i], 0.005f);
            Assert.AreEqual(original.Time[i], decoded.Time[i], 0.6f);
        }
    }

    [TestMethod]
    public void Round_trips_tilt_and_twist_when_present()
    {
        var buffer = new BinkStrokePointBuffer();
        for (int i = 0; i < 10; i++)
        {
            buffer.Add(new BinkPoint(i, i * 2f, 0.4f, i * 10f) { TiltX = -40 + i * 8, TiltY = 30 - i * 5, Twist = i * 30f });
        }
        BinkStrokePoints original = buffer.ToStrokePoints();
        BinkStrokePoints decoded = BinkPointCodec.Decode(BinkPointCodec.Encode(original));

        Assert.AreEqual(BinkPointChannels.Tilt | BinkPointChannels.Twist, decoded.Channels);
        for (int i = 0; i < original.Count; i++)
        {
            Assert.AreEqual(original.TiltX[i], decoded.TiltX[i], 1f);
            Assert.AreEqual(original.TiltY[i], decoded.TiltY[i], 1f);
            Assert.AreEqual(original.Twist[i], decoded.Twist[i], 2f);
        }
    }

    [TestMethod]
    public void Omits_optional_channels_when_the_device_did_not_provide_them()
    {
        BinkStrokePoints original = TestData.Line(new Vector2(0, 0), new Vector2(10, 10), 10);
        BinkStrokePoints decoded = BinkPointCodec.Decode(BinkPointCodec.Encode(original));
        Assert.AreEqual(BinkPointChannels.None, decoded.Channels);
    }

    [TestMethod]
    public void Round_trips_strokes_that_move_left_and_up()
    {
        // Negative deltas exercise the zigzag decoding path; ascending-only strokes would not catch a sign bug.
        BinkStrokePoints original = TestData.Line(new Vector2(300f, 250f), new Vector2(-120.5f, -60.25f), 40);
        BinkStrokePoints decoded = BinkPointCodec.Decode(BinkPointCodec.Encode(original));
        const float step = 1f / BinkPointCodec.DefaultQuantization;
        for (int i = 0; i < original.Count; i++)
        {
            Assert.AreEqual(original.X[i], decoded.X[i], step);
            Assert.AreEqual(original.Y[i], decoded.Y[i], step);
        }
    }

    [TestMethod]
    public void Round_trips_a_closed_loop_where_deltas_change_sign()
    {
        BinkStrokePoints original = TestData.Circle(new Vector2(0, 0), 75f, 128);
        BinkStrokePoints decoded = BinkPointCodec.Decode(BinkPointCodec.Encode(original));
        const float step = 1f / BinkPointCodec.DefaultQuantization;
        for (int i = 0; i < original.Count; i++)
        {
            Assert.AreEqual(original.X[i], decoded.X[i], step);
            Assert.AreEqual(original.Y[i], decoded.Y[i], step);
        }
    }

    [TestMethod]
    public void Encoding_is_deterministic()
    {
        BinkStrokePoints points = TestData.Circle(new Vector2(50, 50), 30);
        Assert.AreSequenceEqual(BinkPointCodec.Encode(points), BinkPointCodec.Encode(points));
    }

    [TestMethod]
    public void Encoding_is_far_smaller_than_a_json_number_array()
    {
        BinkStrokePoints points = TestData.PressureRamp(new Vector2(0, 0), new Vector2(400, 300), 500);
        int binary = BinkPointCodec.Encode(points).Length;
        int json = Encoding.UTF8.GetByteCount(string.Join(',', points.X.ToArray().Select(v => v.ToString("F2", System.Globalization.CultureInfo.InvariantCulture))));
        Assert.IsTrue(binary < json, $"binary was {binary} bytes, a single JSON coordinate array is {json}");
        Assert.IsTrue(binary < points.Count * 8, $"expected under {points.Count * 8} bytes but got {binary}");
    }

    [TestMethod]
    public void Empty_points_round_trip()
    {
        BinkStrokePoints decoded = BinkPointCodec.Decode(BinkPointCodec.Encode(BinkStrokePoints.Empty));
        Assert.AreEqual(0, decoded.Count);
    }

    [TestMethod]
    [DataRow(new byte[] { })]
    [DataRow(new byte[] { 1 })]
    [DataRow(new byte[] { 99, 0, 64, 5 })]
    [DataRow(new byte[] { 1, 0, 64, 200, 200, 200, 200, 200 })]
    public void Malformed_input_throws_rather_than_allocating(byte[] data)
    {
        Assert.ThrowsExactly<FormatException>(() => BinkPointCodec.Decode(data));
    }

    [TestMethod]
    public void A_declared_count_beyond_the_limit_is_rejected()
    {
        // Version 1, no flags, quantization 64, count 1000000 as a varint.
        var bytes = new List<byte> { 1, 0, 64 };
        uint count = 1_000_000;
        while (count >= 0x80) { bytes.Add((byte)(count | 0x80)); count >>= 7; }
        bytes.Add((byte)count);
        Assert.ThrowsExactly<FormatException>(() => BinkPointCodec.Decode(bytes.ToArray(), maxPoints: 1000));
    }
}

[TestClass]
public class SerializerTests
{
    [TestMethod]
    public void Round_trips_a_full_document()
    {
        BinkDocument original = TestData.SampleDocument();
        original.SetMetadata("author", "Test <script>");
        string json = BinkSerializer.Serialize(original);
        BinkDocument loaded = BinkSerializer.Deserialize(json);

        Assert.AreEqual(original.Id, loaded.Id);
        Assert.AreEqual(original.Layers.Count, loaded.Layers.Count);
        Assert.AreEqual(original.ElementCount, loaded.ElementCount);
        Assert.AreEqual(original.Settings.Width, loaded.Settings.Width);
        Assert.AreEqual(original.Settings.Background, loaded.Settings.Background);
        Assert.AreEqual("Test <script>", loaded.Metadata["author"]);

        for (int i = 0; i < original.Layers.Count; i++)
        {
            Assert.AreEqual(original.Layers[i].Name, loaded.Layers[i].Name);
            Assert.AreEqual(original.Layers[i].Count, loaded.Layers[i].Count);
            for (int j = 0; j < original.Layers[i].Count; j++)
            {
                BinkElement a = original.Layers[i].Elements[j];
                BinkElement b = loaded.Layers[i].Elements[j];
                Assert.AreEqual(a.Id, b.Id);
                Assert.AreEqual(a.GetType(), b.GetType());
                Assert.AreEqual(a.Bounds.X, b.Bounds.X, 0.05f);
                Assert.AreEqual(a.Bounds.Width, b.Bounds.Width, 0.05f);
            }
        }
    }

    [TestMethod]
    public void Round_trips_brush_settings()
    {
        var document = new BinkDocument();
        var brush = new BinkPencilBrush
        {
            Color = BinkColor.Parse("#123456"),
            Width = 7.5f,
            Opacity = 0.42f,
            Smoothing = 0.8f,
            Spacing = 0.05f,
            SimulatePressure = false,
            TaperStart = 3f,
            TaperEnd = 9f,
            PressureCurve = new BinkPressureCurve(BinkPressureCurveKind.EaseInOut, 0.05f, 0.95f),
        };
        document.AddElement(new BinkStroke(TestData.Line(new Vector2(0, 0), new Vector2(10, 10)), brush));

        BinkDocument loaded = BinkSerializer.Deserialize(BinkSerializer.Serialize(document));
        var reloaded = (BinkStroke)loaded.Layers[0].Elements[0];

        Assert.IsExactInstanceOfType<BinkPencilBrush>(reloaded.Brush);
        Assert.AreEqual(brush.Color, reloaded.Brush.Color);
        Assert.AreEqual(brush.Width, reloaded.Brush.Width, 5e-4);
        Assert.AreEqual(brush.Opacity, reloaded.Brush.Opacity, 5e-4);
        Assert.AreEqual(brush.Smoothing, reloaded.Brush.Smoothing, 5e-4);
        Assert.AreEqual(brush.Spacing, reloaded.Brush.Spacing, 5e-4);
        Assert.IsFalse(reloaded.Brush.SimulatePressure);
        Assert.AreEqual(brush.TaperEnd, reloaded.Brush.TaperEnd, 5e-4);
        Assert.AreEqual(BinkPressureCurveKind.EaseInOut, reloaded.Brush.PressureCurve.Kind);
        Assert.AreEqual(0.95f, reloaded.Brush.PressureCurve.MaxFactor, 5e-4);
    }

    [TestMethod]
    public void Round_trips_a_custom_pressure_curve()
    {
        var document = new BinkDocument();
        float[] samples = [0f, 0.1f, 0.6f, 0.9f, 1f];
        document.AddElement(new BinkStroke(TestData.Line(new Vector2(0, 0), new Vector2(5, 5)), new BinkPenBrush { PressureCurve = BinkPressureCurve.FromSamples(samples) }));

        BinkDocument loaded = BinkSerializer.Deserialize(BinkSerializer.Serialize(document));
        BinkPressureCurve curve = ((BinkStroke)loaded.Layers[0].Elements[0]).Brush.PressureCurve;
        Assert.AreEqual(BinkPressureCurveKind.Custom, curve.Kind);
        Assert.AreSequenceEqual(samples, curve.Samples.ToArray());
    }

    [TestMethod]
    public void Json_point_encoding_round_trips_exactly_enough()
    {
        var document = new BinkDocument();
        document.AddElement(new BinkStroke(TestData.PressureRamp(new Vector2(1.234f, 5.678f), new Vector2(100f, 50f), 20), new BinkPenBrush()));
        BinkDocument loaded = BinkSerializer.Deserialize(BinkSerializer.Serialize(document, BinkSerializerOptions.Readable));

        var a = (BinkStroke)document.Layers[0].Elements[0];
        var b = (BinkStroke)loaded.Layers[0].Elements[0];
        for (int i = 0; i < a.Points.Count; i++)
        {
            Assert.AreEqual(a.Points.X[i], b.Points.X[i], 0.001f);
            Assert.AreEqual(a.Points.Pressure[i], b.Points.Pressure[i], 0.001f);
        }
    }

    [TestMethod]
    public void Serialization_is_deterministic()
    {
        BinkDocument document = TestData.SampleDocument();
        Assert.AreEqual(BinkSerializer.Serialize(document), BinkSerializer.Serialize(document));
    }

    [TestMethod]
    public void Round_trip_is_stable_across_two_generations()
    {
        BinkDocument document = TestData.SampleDocument();
        string first = BinkSerializer.Serialize(document);
        string second = BinkSerializer.Serialize(BinkSerializer.Deserialize(first));
        Assert.AreEqual(first, second);
    }

    [TestMethod]
    public void Unknown_element_types_survive_a_round_trip()
    {
        BinkDocument document = TestData.SampleDocument();
        string json = BinkSerializer.Serialize(document);
        // Inject an element type from a hypothetical future version.
        int at = json.IndexOf("\"elements\":[", StringComparison.Ordinal);
        json = json.Insert(at + "\"elements\":[".Length, "{\"type\":\"future-shape\",\"id\":\"9f1d4d1e-0000-4000-8000-000000000001\",\"custom\":{\"a\":1}},");

        BinkDocument loaded = BinkSerializer.Deserialize(json);
        Assert.Contains(e => e is BinkUnknownElement, loaded.AllElements);

        string again = BinkSerializer.Serialize(loaded);
        Assert.Contains("future-shape", again, StringComparison.Ordinal);
        Assert.Contains("\"custom\"", again, StringComparison.Ordinal);
    }

    [TestMethod]
    public void Unknown_element_types_can_be_dropped_on_request()
    {
        BinkDocument document = TestData.SampleDocument();
        string source = BinkSerializer.Serialize(document);
        int at = source.IndexOf("\"elements\":[", StringComparison.Ordinal);
        string json = source.Insert(at + "\"elements\":[".Length, "{\"type\":\"future-shape\",\"id\":\"9f1d4d1e-0000-4000-8000-000000000001\"},");

        BinkDocument loaded = BinkSerializer.Deserialize(json, BinkDeserializerOptions.Default with { PreserveUnknownElements = false });
        Assert.DoesNotContain(e => e is BinkUnknownElement, loaded.AllElements);
    }

    [TestMethod]
    public void An_unknown_brush_kind_still_renders_with_its_stored_appearance()
    {
        BinkDocument document = TestData.SampleDocument();
        string json = BinkSerializer.Serialize(document).Replace("\"kind\":\"pen\"", "\"kind\":\"glitter-2030\"", StringComparison.Ordinal);
        BinkDocument loaded = BinkSerializer.Deserialize(json);
        var stroke = (BinkStroke)loaded.Layers[0].Elements[0];
        Assert.AreEqual(5f, stroke.Brush.Width, 5e-4);
        Assert.IsFalse(stroke.Geometry.IsEmpty);
    }

    [TestMethod]
    public void Duplicate_element_ids_are_repaired_instead_of_failing_the_load()
    {
        // A hand-edited or merged document can repeat an id; the drawing is still usable.
        string json = """
        {"format":"bink","version":1,"layers":[{"id":"11111111-1111-4111-8111-111111111111","name":"L","elements":[
          {"type":"rect","id":"22222222-2222-4222-8222-222222222222","rect":[0,0,10,10]},
          {"type":"rect","id":"22222222-2222-4222-8222-222222222222","rect":[20,20,10,10]}
        ]}]}
        """;
        BinkDocument document = BinkSerializer.Deserialize(json);
        Assert.AreEqual(2, document.ElementCount);
        Assert.AreEqual(2, document.AllElements.Select(e => e.Id).Distinct().Count());
    }

    [TestMethod]
    public void The_output_is_valid_json()
    {
        using JsonDocument parsed = JsonDocument.Parse(BinkSerializer.Serialize(TestData.SampleDocument()));
        Assert.AreEqual("bink", parsed.RootElement.GetProperty("format").GetString());
        Assert.AreEqual(1, parsed.RootElement.GetProperty("version").GetInt32());
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("{}")]
    [DataRow("not json")]
    [DataRow("{\"format\":\"other\",\"version\":1}")]
    [DataRow("{\"format\":\"bink\",\"version\":1}")]
    [DataRow("{\"format\":\"bink\",\"version\":1,\"layers\":{}}")]
    [DataRow("[1,2,3]")]
    public void Malformed_documents_are_rejected_with_a_clear_error(string json)
    {
        Assert.ThrowsExactly<BinkFormatException>(() => BinkSerializer.Deserialize(json));
        Assert.IsFalse(BinkSerializer.TryDeserialize(json, out _, out string? error));
        Assert.IsFalse(string.IsNullOrWhiteSpace(error));
    }

    [TestMethod]
    public void A_newer_schema_version_is_refused_with_an_actionable_message()
    {
        BinkFormatException ex = Assert.ThrowsExactly<BinkFormatException>(() => BinkSerializer.Deserialize("{\"format\":\"bink\",\"version\":99,\"layers\":[]}"));
        Assert.Contains("newer version", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [TestMethod]
    public void Oversized_input_is_rejected_before_parsing()
    {
        string json = BinkSerializer.Serialize(TestData.SampleDocument());
        Assert.ThrowsExactly<BinkFormatException>(() => BinkSerializer.Deserialize(json, BinkDeserializerOptions.Default with { MaxInputBytes = 10 }));
    }

    [TestMethod]
    public void Coordinates_beyond_the_limit_are_rejected()
    {
        string json = """
        {"format":"bink","version":1,"layers":[{"id":"11111111-1111-4111-8111-111111111111","name":"L","elements":[
          {"type":"stroke","id":"22222222-2222-4222-8222-222222222222",
           "brush":{"kind":"pen","color":"#000000","width":2},
           "points":{"count":2,"encoding":"json","x":[0,99999999],"y":[0,0],"p":[0.5,0.5],"t":[0,10]}}
        ]}]}
        """;
        Assert.ThrowsExactly<BinkFormatException>(() => BinkSerializer.Deserialize(json));
    }

    [TestMethod]
    public void A_singular_transform_is_replaced_by_identity()
    {
        string json = """
        {"format":"bink","version":1,"layers":[{"id":"11111111-1111-4111-8111-111111111111","name":"L","elements":[
          {"type":"rect","id":"22222222-2222-4222-8222-222222222222","rect":[0,0,10,10],"transform":[0,0,0,0,0,0]}
        ]}]}
        """;
        BinkDocument document = BinkSerializer.Deserialize(json);
        Assert.AreEqual(Matrix3x2.Identity, document.Layers[0].Elements[0].Transform);
    }

    [TestMethod]
    public void Mismatched_point_array_lengths_are_rejected()
    {
        string json = """
        {"format":"bink","version":1,"layers":[{"id":"11111111-1111-4111-8111-111111111111","name":"L","elements":[
          {"type":"stroke","id":"22222222-2222-4222-8222-222222222222",
           "brush":{"kind":"pen","color":"#000000","width":2},
           "points":{"count":3,"encoding":"json","x":[0,1,2],"y":[0,1],"p":[0.5],"t":[0]}}
        ]}]}
        """;
        Assert.ThrowsExactly<BinkFormatException>(() => BinkSerializer.Deserialize(json));
    }

    [TestMethod]
    public void Images_with_a_disallowed_scheme_are_dropped()
    {
        string json = """
        {"format":"bink","version":1,"layers":[{"id":"11111111-1111-4111-8111-111111111111","name":"L","elements":[
          {"type":"image","id":"22222222-2222-4222-8222-222222222222","rect":[0,0,10,10],"src":"javascript:alert(1)"},
          {"type":"image","id":"33333333-3333-4333-8333-333333333333","rect":[0,0,10,10],"src":"data:text/html;base64,PHNjcmlwdD4="}
        ]}]}
        """;
        BinkDocument document = BinkSerializer.Deserialize(json);
        Assert.IsEmpty(document.AllElements);
    }

    [TestMethod]
    public void Remote_images_can_be_refused()
    {
        string json = """
        {"format":"bink","version":1,"layers":[{"id":"11111111-1111-4111-8111-111111111111","name":"L","elements":[
          {"type":"image","id":"22222222-2222-4222-8222-222222222222","rect":[0,0,10,10],"src":"https://example.com/x.png"}
        ]}]}
        """;
        Assert.ContainsSingle(BinkSerializer.Deserialize(json).AllElements);
        Assert.IsEmpty(BinkSerializer.Deserialize(json, BinkDeserializerOptions.Default with { AllowRemoteImages = false }).AllElements);
    }

    [TestMethod]
    public void A_dangerous_font_family_is_replaced_with_the_default()
    {
        string json = """
        {"format":"bink","version":1,"layers":[{"id":"11111111-1111-4111-8111-111111111111","name":"L","elements":[
          {"type":"text","id":"22222222-2222-4222-8222-222222222222","position":[0,0],"text":"hi",
           "textStyle":{"font":"x;} body{display:none} .a{font-family:x","size":12,"color":"#000000"}}
        ]}]}
        """;
        BinkDocument document = BinkSerializer.Deserialize(json);
        var text = (BinkTextElement)document.Layers[0].Elements[0];
        Assert.AreEqual(BinkTextStyle.Default.FontFamily, text.Style.FontFamily);
    }

    [TestMethod]
    public async Task Stream_round_trip_works()
    {
        BinkDocument document = TestData.SampleDocument();
        using var stream = new MemoryStream();
        await BinkSerializer.SerializeAsync(document, stream);
        stream.Position = 0;
        BinkDocument loaded = await BinkSerializer.DeserializeAsync(stream);
        Assert.AreEqual(document.ElementCount, loaded.ElementCount);
    }

    [TestMethod]
    public void Images_can_be_saved_without_their_payload()
    {
        var document = new BinkDocument();
        byte[] png = TestData.CreatePng(4, 4, (_, _) => BinkColor.Black);
        document.AddElement(new BinkImageElement(BinkImageSource.FromBytes(png, "image/png", 4, 4), new BinkRect(0, 0, 40, 40)));

        string full = BinkSerializer.Serialize(document);
        string stripped = BinkSerializer.Serialize(document, BinkSerializerOptions.Default with { IncludeEmbeddedImages = false });
        Assert.IsTrue(stripped.Length < full.Length);
        Assert.Contains("srcOmitted", stripped, StringComparison.Ordinal);
    }
}
