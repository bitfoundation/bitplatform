using System.Numerics;

namespace Bit.Bink.Tests;

/// <summary>The broad-nib brush, and brush settings saved through parameters.</summary>
[TestClass]
public sealed class CalligraphyBrushTests
{
    private static BinkRect InkBounds(BinkBrush brush, Vector2 from, Vector2 to)
        => new BinkStroke(TestData.Line(from, to, 20, pressure: 1f), brush).Geometry.Path.Bounds;

    [TestMethod]
    public void The_line_is_thick_across_the_nib_and_thin_along_it()
    {
        var brush = new BinkCalligraphyBrush { Width = 20f, NibAngle = 90f, NibThickness = 0.1f };

        BinkRect across = InkBounds(brush, new Vector2(0, 0), new Vector2(100, 0));
        BinkRect along = InkBounds(brush, new Vector2(0, 0), new Vector2(0, 100));

        Assert.AreEqual(20f, across.Height, 0.5f, "Moving across an upright nib draws its full width.");
        Assert.AreEqual(2f, along.Width, 0.5f, "Moving along it draws only its thickness.");
    }

    [TestMethod]
    public void A_single_tap_leaves_the_nib_s_mark_and_the_ink_has_no_seams()
    {
        var brush = new BinkCalligraphyBrush { Width = 16f, NibAngle = 30f, Color = BinkColor.Black };
        Assert.IsFalse(new BinkStroke(BinkStrokePoints.FromPositions([new Vector2(10, 10)]), brush).Geometry.Path.IsEmpty);

        // A zig-zag rendered by the rasterizer: the middle of every segment is solid ink.
        var document = new BinkDocument(new BinkDocumentSettings { Width = 200, Height = 100, Background = BinkColor.White });
        Vector2[] zigzag = [new(20, 20), new(60, 80), new(100, 20), new(140, 80), new(180, 20)];
        document.AddElement(new BinkStroke(BinkStrokePoints.FromPositions(zigzag), brush));
        BinkRasterSurface surface = BinkPngExporter.Render(document, BinkPngExportOptions.Default, out _, out _);
        for (int i = 1; i < zigzag.Length; i++)
        {
            Vector2 middle = (zigzag[i - 1] + zigzag[i]) / 2f;
            Assert.IsLessThan(64, (int)surface.GetPixel((int)middle.X, (int)middle.Y).R, $"Segment {i} is solid.");
        }
    }

    [TestMethod]
    public void The_nib_survives_saving_and_reaches_collaborators()
    {
        var document = new BinkDocument();
        var brush = new BinkCalligraphyBrush { NibAngle = -20f, NibThickness = 0.3f, Width = 12f };
        var stroke = new BinkStroke(TestData.Line(Vector2.Zero, new Vector2(50, 50)), brush);
        document.AddElement(stroke);

        string json = BinkSerializer.Serialize(document);
        Assert.Contains("\"params\"", json);
        var loaded = (BinkCalligraphyBrush)((BinkStroke)BinkSerializer.Deserialize(json).GetElement(stroke.Id)).Brush;
        Assert.AreEqual(-20f, loaded.NibAngle);
        Assert.AreEqual(0.3f, loaded.NibThickness, 1e-4f);

        var sent = (BinkAddElementOperation)BinkOperationCodec.Deserialize(BinkOperationCodec.Serialize(new BinkAddElementOperation(document.Layers[0].Id, stroke with { Id = Guid.NewGuid() })));
        Assert.AreEqual(-20f, ((BinkCalligraphyBrush)((BinkStroke)sent.Element).Brush).NibAngle);
    }

    [TestMethod]
    public void A_brush_this_build_does_not_know_keeps_its_settings_through_a_round_trip()
    {
        const string json = """{"format":"bink","version":1,"layers":[{"elements":[{"type":"stroke","brush":{"kind":"acme-airbrush","color":"#112233","width":9,"params":{"spray":2.5,"density":0.75}},"points":{"x":[0,10],"y":[0,10]}}]}]}""";
        BinkDocument loaded = BinkSerializer.Deserialize(json);
        string saved = BinkSerializer.Serialize(loaded);
        Assert.Contains("\"kind\":\"acme-airbrush\"", saved);
        Assert.Contains("\"density\":0.75", saved);
        Assert.Contains("\"spray\":2.5", saved);
    }

    [TestMethod]
    public void Settings_from_a_file_are_bounded_and_clamped()
    {
        static string Doc(string parameters)
            => """{"format":"bink","version":1,"layers":[{"elements":[{"type":"stroke","brush":{"kind":"calligraphy","width":9,"params":"""
               + parameters + """},"points":{"x":[0,10],"y":[0,10]}}]}]}""";

        var brush = (BinkCalligraphyBrush)((BinkStroke)BinkSerializer.Deserialize(Doc("""{"nibThickness":50,"nibAngle":725,"unknown":"text"}""")).AllElements.Single()).Brush;
        Assert.AreEqual(1f, brush.NibThickness, "Clamped to what the brush can draw.");
        Assert.AreEqual(5f, brush.NibAngle, 1e-3f, "Angles wrap.");

        string many = "{" + string.Join(",", Enumerable.Range(0, 40).Select(i => $"\"p{i}\":{i}")) + "}";
        Assert.IsFalse(BinkSerializer.TryDeserialize(Doc(many), out _, out string? error));
        Assert.Contains("settings", error!);
    }

    [TestMethod]
    public void A_nib_that_cannot_be_drawn_is_refused()
    {
        var document = new BinkDocument();
        var stroke = new BinkStroke(TestData.Line(Vector2.Zero, new Vector2(10, 10)), new BinkCalligraphyBrush { NibAngle = float.NaN });
        Assert.ThrowsExactly<InvalidOperationException>(() => document.AddElement(stroke));
        Assert.IsTrue(BinkBrushRegistry.Default.Contains(BinkCalligraphyBrush.KindName));
    }
}
