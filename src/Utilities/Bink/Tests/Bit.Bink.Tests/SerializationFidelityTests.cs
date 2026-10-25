using System.Numerics;
using System.Text.Json;

namespace Bit.Bink.Tests;

/// <summary>
/// Regressions where a document did not come back as it was saved: brush fields that changed on load, an
/// unregistered brush that became a pen, and point values that loaded but could not be written again.
/// </summary>
[TestClass]
public class SerializationFidelityTests
{
    private static BinkBrush[] BuiltInBrushes() => [new BinkPenBrush(), new BinkPencilBrush(), new BinkMarkerBrush(), new BinkHighlighterBrush(), new BinkPixelEraserBrush()];

    /// <summary>A brush whose optional fields all hold <see cref="BinkBrush"/>'s own defaults.</summary>
    private static BinkBrush WithBaseDefaults(BinkBrush brush) => brush with
    {
        Opacity = 1f,
        BlendMode = BinkBlendMode.Normal,
        Smoothing = 0.5f,
        Spacing = 0.25f,
        SimulatePressure = true,
        TaperStart = 0f,
        TaperEnd = 0f,
    };

    private static string Save(BinkBrush brush)
    {
        var document = new BinkDocument();
        document.AddElement(new BinkStroke(TestData.Line(new Vector2(0, 0), new Vector2(40, 30)), brush));
        return BinkSerializer.Serialize(document);
    }

    private static BinkBrush Load(string json) => ((BinkStroke)BinkSerializer.Deserialize(json).AllElements.Single()).Brush;

    private static void AssertSameFields(BinkBrush expected, BinkBrush actual)
    {
        string kind = expected.Kind;
        Assert.AreEqual(expected.Kind, actual.Kind);
        Assert.AreEqual(expected.Opacity, actual.Opacity, 5e-4, kind + " opacity");
        Assert.AreEqual(expected.BlendMode, actual.BlendMode, kind + " blend");
        Assert.AreEqual(expected.Smoothing, actual.Smoothing, 5e-4, kind + " smoothing");
        Assert.AreEqual(expected.Spacing, actual.Spacing, 5e-4, kind + " spacing");
        Assert.AreEqual(expected.SimulatePressure, actual.SimulatePressure, kind + " simulatePressure");
        Assert.AreEqual(expected.TaperStart, actual.TaperStart, 5e-4, kind + " taperStart");
        Assert.AreEqual(expected.TaperEnd, actual.TaperEnd, 5e-4, kind + " taperEnd");
    }

    [TestMethod]
    public void Every_built_in_brush_keeps_fields_that_hold_the_base_defaults()
    {
        // The writer left a field out when it equalled BinkBrush's default, and the reader filled it from the kind's
        // own default: a pen without a taper came back with a 6-unit one, a normal highlighter as a 45% multiply.
        foreach (BinkBrush brush in BuiltInBrushes().Select(WithBaseDefaults)) AssertSameFields(brush, Load(Save(brush)));
    }

    [TestMethod]
    public void Every_built_in_brush_keeps_its_own_defaults()
    {
        foreach (BinkBrush brush in BuiltInBrushes()) AssertSameFields(brush, Load(Save(brush)));
    }

    [TestMethod]
    public void A_reader_that_falls_back_to_the_kinds_defaults_reads_the_same_brush()
    {
        // Readers released before this fix fill a missing field from the kind's defaults. Every field where those
        // differ from BinkBrush's is written, so such a reader still gets the brush that was saved.
        foreach (BinkBrush brush in BuiltInBrushes().Concat(BuiltInBrushes().Select(WithBaseDefaults)))
        {
            using JsonDocument json = JsonDocument.Parse(Save(brush));
            JsonElement written = json.RootElement.GetProperty("layers")[0].GetProperty("elements")[0].GetProperty("brush");
            Assert.IsTrue(BinkBrushRegistry.Default.TryCreate(brush.Kind, out BinkBrush? defaults));

            float Number(string name, float fallback) => written.TryGetProperty(name, out JsonElement v) ? v.GetSingle() : fallback;
            bool Flag(string name, bool fallback) => written.TryGetProperty(name, out JsonElement v) ? v.GetBoolean() : fallback;
            var older = defaults! with
            {
                Opacity = Number("opacity", defaults.Opacity),
                BlendMode = written.TryGetProperty("blend", out JsonElement blend)
                    ? blend.GetString() switch { "multiply" => BinkBlendMode.Multiply, "screen" => BinkBlendMode.Screen, "erase" => BinkBlendMode.DestinationOut, _ => BinkBlendMode.Normal }
                    : defaults.BlendMode,
                Smoothing = Number("smoothing", defaults.Smoothing),
                Spacing = Number("spacing", defaults.Spacing),
                SimulatePressure = Flag("simulatePressure", defaults.SimulatePressure),
                TaperStart = Number("taperStart", defaults.TaperStart),
                TaperEnd = Number("taperEnd", defaults.TaperEnd),
            };
            AssertSameFields(brush, older);
        }
    }

    [TestMethod]
    public void A_missing_brush_field_reads_as_the_base_default()
    {
        const string json = """
        {"format":"bink","version":1,"layers":[{"id":"11111111-1111-4111-8111-111111111111","name":"L","elements":[
          {"type":"stroke","id":"22222222-2222-4222-8222-222222222222",
           "brush":{"kind":"highlighter","color":"#ffeb3b","width":20},
           "points":{"count":2,"encoding":"json","x":[0,10],"y":[0,10],"p":[0.5,0.5],"t":[0,10]}}
        ]}]}
        """;
        AssertSameFields(WithBaseDefaults(new BinkHighlighterBrush()), Load(json));
    }

    [TestMethod]
    public void A_pen_without_a_taper_reaches_a_collaborator_without_one()
    {
        // Collaboration writes elements with the same code, so peers drew a different stroke from the sender's.
        var document = new BinkDocument();
        var stroke = new BinkStroke(TestData.Line(new Vector2(0, 0), new Vector2(40, 30)), new BinkPenBrush { TaperEnd = 0f });
        string json = BinkOperationCodec.Serialize(new BinkAddElementOperation(document.Layers[0].Id, stroke));

        var received = (BinkAddElementOperation)BinkOperationCodec.Deserialize(json);
        Assert.AreEqual(0f, ((BinkStroke)received.Element).Brush.TaperEnd);
    }

    [TestMethod]
    public void An_unregistered_brush_kind_survives_a_round_trip_and_still_renders()
    {
        // Unknown elements were preserved, but an unknown brush was rewritten as "pen" on the next save.
        BinkDocument document = TestData.SampleDocument();
        var marker = (BinkStroke)document.Layers[0].Elements[1];
        string json = BinkSerializer.Serialize(document).Replace("\"kind\":\"marker\"", "\"kind\":\"glitter-2030\"", StringComparison.Ordinal);

        BinkDocument loaded = BinkSerializer.Deserialize(json);
        var stroke = (BinkStroke)loaded.Layers[0].Elements[1];
        Assert.AreEqual("glitter-2030", stroke.Brush.Kind);
        Assert.AreEqual(marker.Brush.Color, stroke.Brush.Color);
        Assert.AreEqual(marker.Brush.Width, stroke.Brush.Width, 5e-4);
        Assert.IsFalse(stroke.Geometry.IsEmpty);

        string saved = BinkSerializer.Serialize(loaded);
        Assert.Contains("\"kind\":\"glitter-2030\"", saved, StringComparison.Ordinal);
        Assert.AreEqual(saved, BinkSerializer.Serialize(BinkSerializer.Deserialize(saved)));
    }

    [TestMethod]
    public void A_time_too_large_for_the_binary_encoding_loads_clamped_and_still_saves()
    {
        // Only x and y were range-checked. The point loaded, then every save threw FormatException, which is not
        // the BinkFormatException callers were promised.
        const string json = """
        {"format":"bink","version":1,"layers":[{"id":"11111111-1111-4111-8111-111111111111","name":"L","elements":[
          {"type":"stroke","id":"22222222-2222-4222-8222-222222222222",
           "brush":{"kind":"pen","color":"#000000","width":2},
           "points":{"count":2,"encoding":"json","x":[0,10],"y":[0,10],"p":[0.5,0.5],"t":[0,3000000000]}}
        ]}]}
        """;
        BinkDocument document = BinkSerializer.Deserialize(json);
        float time = ((BinkStroke)document.AllElements.Single()).Points.Time[1];
        Assert.IsLessThanOrEqualTo(1e9f, time);

        string saved = BinkSerializer.Serialize(document);
        Assert.AreEqual(time, ((BinkStroke)BinkSerializer.Deserialize(saved).AllElements.Single()).Points.Time[1]);
    }

    [TestMethod]
    public void Json_tilt_and_twist_are_kept_to_the_ranges_the_binary_encoding_stores()
    {
        const string json = """
        {"format":"bink","version":1,"layers":[{"id":"11111111-1111-4111-8111-111111111111","name":"L","elements":[
          {"type":"stroke","id":"22222222-2222-4222-8222-222222222222",
           "brush":{"kind":"pen","color":"#000000","width":2},
           "points":{"count":2,"encoding":"json","x":[0,10],"y":[0,10],"p":[0.5,0.5],"t":[0,10],
                     "tx":[0,500],"ty":[-500,0],"tw":[-10,1000]}}
        ]}]}
        """;
        BinkStrokePoints points = ((BinkStroke)BinkSerializer.Deserialize(json).AllElements.Single()).Points;
        Assert.AreEqual(90f, points.TiltX[1]);
        Assert.AreEqual(-90f, points.TiltY[0]);
        Assert.AreEqual(0f, points.Twist[0]);
        Assert.AreEqual(360f, points.Twist[1]);
    }

    [TestMethod]
    public void Binary_tilt_past_ninety_degrees_and_times_past_the_limit_are_clamped()
    {
        // [version][flags: tilt][quantization 64][2 points][x x][y y][pressure pressure]
        // [time deltas of 2^31 - 1 each, which add up past any sane time][tilt 127/-128, 0/0]
        byte[] data =
        [
            1, 1, 64, 2, 0, 0, 0, 0, 128, 128,
            0xFE, 0xFF, 0xFF, 0xFF, 0x0F, 0xFE, 0xFF, 0xFF, 0xFF, 0x0F,
            127, 128, 0, 0,
        ];
        BinkStrokePoints points = BinkPointCodec.Decode(data);
        Assert.AreEqual(90f, points.TiltX[0]);
        Assert.AreEqual(-90f, points.TiltY[0]);
        Assert.IsLessThanOrEqualTo(1e9f, points.Time[1]);

        // What was read can be written again.
        BinkStrokePoints again = BinkPointCodec.Decode(BinkPointCodec.Encode(points));
        Assert.AreEqual(points.Time[1], again.Time[1]);
    }

    [TestMethod]
    public void Encoding_saturates_values_it_cannot_carry_instead_of_throwing()
    {
        BinkStrokePoints points = BinkStrokePoints.FromArrays([0f, 10f, 20f], [0f, 5f, 0f], [0.5f, 0.5f, 0.5f], [0f, 3e9f, -3e9f]);
        BinkStrokePoints decoded = BinkPointCodec.Decode(BinkPointCodec.Encode(points));

        Assert.AreEqual(1e9f, decoded.Time[1]);
        Assert.AreEqual(-1e9f, decoded.Time[2]);
        // A clamped value does not shift the channels or the points after it.
        Assert.AreEqual(10f, decoded.X[1], 0.02);
        Assert.AreEqual(20f, decoded.X[2], 0.02);
        Assert.AreEqual(5f, decoded.Y[1], 0.02);
    }

    [TestMethod]
    public void A_raised_quantization_on_large_coordinates_falls_back_to_a_coarser_grid()
    {
        // 5,000 units on a grid of a million steps per unit is past what a 32-bit delta holds; saving threw.
        var document = new BinkDocument();
        document.AddElement(new BinkStroke(TestData.Line(new Vector2(-5000, 0), new Vector2(5000, 3000), 10), new BinkPenBrush()));
        string json = BinkSerializer.Serialize(document, BinkSerializerOptions.Default with { Quantization = 1_000_000 });

        var original = (BinkStroke)document.AllElements.Single();
        var loaded = (BinkStroke)BinkSerializer.Deserialize(json).AllElements.Single();
        for (int i = 0; i < original.Points.Count; i++)
        {
            Assert.AreEqual(original.Points.X[i], loaded.Points.X[i], 1e-3);
            Assert.AreEqual(original.Points.Y[i], loaded.Points.Y[i], 1e-3);
        }
    }

    [TestMethod]
    public void A_raised_coordinate_limit_still_saves_and_loads()
    {
        var limits = BinkDocumentLimits.Default with { MaxCoordinate = 100_000_000f };
        var document = new BinkDocument(new BinkDocumentSettings(), limits);
        document.AddElement(new BinkStroke(TestData.Line(new Vector2(-50_000_000, 0), new Vector2(50_000_000, 1000), 10), new BinkPenBrush()));

        string json = BinkSerializer.Serialize(document);
        var original = (BinkStroke)document.AllElements.Single();
        var loaded = (BinkStroke)BinkSerializer.Deserialize(json, new BinkDeserializerOptions { Limits = limits }).AllElements.Single();
        for (int i = 0; i < original.Points.Count; i++) Assert.AreEqual(original.Points.X[i], loaded.Points.X[i], 8.0);
    }
}
