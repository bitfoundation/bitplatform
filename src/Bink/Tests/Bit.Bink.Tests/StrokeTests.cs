using System.Numerics;

namespace Bit.Bink.Tests;

[TestClass]
public class StrokePointsTests
{
    [TestMethod]
    public void Buffer_grows_without_losing_points()
    {
        var buffer = new BinkStrokePointBuffer(8);
        for (int i = 0; i < 1000; i++) buffer.Add(new BinkPoint(i, i * 2, 0.5f, i));
        Assert.AreEqual(1000, buffer.Count);
        Assert.AreEqual(999f, buffer[999].X);
        Assert.AreEqual(1998f, buffer[999].Y);
    }

    [TestMethod]
    public void Optional_channels_are_only_allocated_when_used()
    {
        var buffer = new BinkStrokePointBuffer();
        buffer.Add(new BinkPoint(1, 1));
        Assert.AreEqual(BinkPointChannels.None, buffer.ToStrokePoints().Channels);

        buffer.Add(new BinkPoint(2, 2) { TiltX = 15 });
        Assert.AreEqual(BinkPointChannels.Tilt, buffer.ToStrokePoints().Channels);
    }

    [TestMethod]
    public void Bounds_cover_every_point()
    {
        BinkStrokePoints points = TestData.Circle(new Vector2(100, 100), 50);
        Assert.AreEqual(50f, points.Bounds.Left, 5e-1);
        Assert.AreEqual(150f, points.Bounds.Right, 5e-1);
    }

    [TestMethod]
    public void Transform_moves_positions_and_shares_the_other_channels()
    {
        BinkStrokePoints points = TestData.PressureRamp(new Vector2(0, 0), new Vector2(10, 10), 10);
        BinkStrokePoints moved = points.Transform(Matrix3x2.CreateTranslation(100, 0));
        Assert.AreEqual(points.X[3] + 100, moved.X[3], 5e-5);
        Assert.AreEqual(points.Pressure[3], moved.Pressure[3], 5e-7);
    }

    [TestMethod]
    public void Simplify_keeps_the_shape_and_the_pressure_of_retained_points()
    {
        BinkStrokePoints points = TestData.PressureRamp(new Vector2(0, 0), new Vector2(300, 0), 300);
        BinkStrokePoints simplified = points.Simplify(0.5f);
        Assert.IsTrue(simplified.Count < points.Count);
        Assert.AreEqual(points.X[0], simplified.X[0], 5e-5);
        Assert.AreEqual(points.X[^1], simplified.X[^1], 5e-5);
        Assert.AreEqual(points.Pressure[0], simplified.Pressure[0], 5e-5);
    }

    [TestMethod]
    public void VelocityAt_reflects_the_sample_timing()
    {
        var buffer = new BinkStrokePointBuffer();
        buffer.Add(new BinkPoint(0, 0, 0.5f, 0));
        buffer.Add(new BinkPoint(10, 0, 0.5f, 10));
        buffer.Add(new BinkPoint(20, 0, 0.5f, 20));
        BinkStrokePoints points = buffer.ToStrokePoints();
        Assert.AreEqual(1f, points.VelocityAt(1), 5e-4);
    }
}

[TestClass]
public class StrokeOutlineTests
{
    private static float MeasureWidthAt(BinkPathGeometry outline, float x)
    {
        // Intersects the outline with a vertical line and returns the vertical extent of the crossings.
        var points = new List<Vector2>();
        outline.Flatten(0.05f, (p, _) => points.AddRange(p.Span));
        float min = float.MaxValue, max = float.MinValue;
        for (int i = 1; i < points.Count; i++)
        {
            Vector2 a = points[i - 1];
            Vector2 b = points[i];
            if ((a.X - x) * (b.X - x) > 0) continue;
            float t = MathF.Abs(b.X - a.X) < 1e-6f ? 0 : (x - a.X) / (b.X - a.X);
            float y = a.Y + (b.Y - a.Y) * t;
            min = MathF.Min(min, y);
            max = MathF.Max(max, y);
        }
        return max - min;
    }

    [TestMethod]
    public void A_constant_pressure_stroke_has_a_constant_width()
    {
        var brush = new BinkPenBrush { Width = 10f, PressureCurve = BinkPressureCurve.Constant, TaperEnd = 0f };
        BinkStrokePoints points = TestData.Line(new Vector2(20, 100), new Vector2(280, 100), 40);
        BinkStrokeGeometry geometry = brush.BuildGeometry(points.AsView());

        foreach (float x in new[] { 80f, 150f, 220f })
        {
            Assert.AreEqual(10f, MeasureWidthAt(geometry.Path, x), 0.8f);
        }
    }

    [TestMethod]
    public void Pressure_changes_the_stroke_width()
    {
        var brush = new BinkPenBrush { Width = 20f, PressureCurve = BinkPressureCurve.Linear, TaperEnd = 0f };
        var buffer = new BinkStrokePointBuffer();
        for (int i = 0; i < 40; i++) buffer.Add(new BinkPoint(i * 10f, 100f, i < 20 ? 0.15f : 1.0f, i * 8f));
        BinkStrokeGeometry geometry = brush.BuildGeometry(buffer.ToStrokePoints().AsView());

        float light = MeasureWidthAt(geometry.Path, 60f);
        float heavy = MeasureWidthAt(geometry.Path, 330f);
        Assert.IsTrue(heavy > light * 2f, $"expected the heavy end ({heavy}) to be much wider than the light end ({light})");
    }

    [TestMethod]
    public void A_single_point_renders_as_a_dot()
    {
        var brush = new BinkPenBrush { Width = 12f };
        var buffer = new BinkStrokePointBuffer();
        buffer.Add(new BinkPoint(50, 50, 1f, 0));
        BinkStrokeGeometry geometry = brush.BuildGeometry(buffer.ToStrokePoints().AsView());

        Assert.IsFalse(geometry.IsEmpty);
        Assert.IsTrue(geometry.Bounds.Width > 4f, "a tap must leave a visible mark");
        Assert.IsTrue(geometry.Bounds.Width < 20f);
    }

    [TestMethod]
    public void Outlines_are_closed_so_they_can_be_filled()
    {
        BinkStrokeGeometry geometry = new BinkPenBrush().BuildGeometry(TestData.Line(new Vector2(0, 0), new Vector2(50, 50), 10).AsView());
        Assert.Contains(BinkPathVerb.Close, geometry.Path.Verbs.ToArray());
        Assert.AreEqual(BinkStrokeGeometryMode.Fill, geometry.Mode);
    }

    [TestMethod]
    public void A_sharp_corner_does_not_produce_a_spike()
    {
        var brush = new BinkPenBrush { Width = 16f, PressureCurve = BinkPressureCurve.Constant };
        var buffer = new BinkStrokePointBuffer();
        // A tight "V": without corner handling the miter would shoot far past the geometry.
        for (int i = 0; i < 10; i++) buffer.Add(new BinkPoint(100 - i * 10f, 100 + i * 10f, 0.5f, i * 8f));
        for (int i = 1; i <= 10; i++) buffer.Add(new BinkPoint(10 + i * 10f, 190 + i * 10f, 0.5f, (10 + i) * 8f));

        BinkStrokeGeometry geometry = brush.BuildGeometry(buffer.ToStrokePoints().AsView());
        BinkRect pointBounds = buffer.Bounds;
        // The outline may only extend by about the stroke radius plus a small allowance.
        Assert.IsTrue(geometry.Bounds.Left > pointBounds.Left - 20f, $"outline extended to {geometry.Bounds.Left} from {pointBounds.Left}");
        Assert.IsTrue(geometry.Bounds.Right < pointBounds.Right + 20f);
        Assert.IsTrue(geometry.Bounds.Bottom < pointBounds.Bottom + 20f);
    }

    [TestMethod]
    public void Geometry_is_cached_per_points_and_brush()
    {
        var stroke = new BinkStroke(TestData.Line(new Vector2(0, 0), new Vector2(10, 10)), new BinkPenBrush());
        // Read the property twice: the second read must return the instance the first one cached.
        var cached = stroke.Geometry;
        Assert.AreSame(cached, stroke.Geometry);

        BinkStroke recolored = stroke with { Brush = stroke.Brush with { Color = BinkColor.Parse("#f00") } };
        Assert.AreNotSame(stroke.Geometry, recolored.Geometry);
    }

    [TestMethod]
    public void Centerline_brushes_produce_a_stroked_path()
    {
        BinkStrokeGeometry geometry = new BinkMarkerBrush { Width = 9f }.BuildGeometry(TestData.Line(new Vector2(0, 0), new Vector2(80, 0), 10).AsView());
        Assert.AreEqual(BinkStrokeGeometryMode.Centerline, geometry.Mode);
        Assert.AreEqual(9f, geometry.LineWidth, 5e-4);
    }

    [TestMethod]
    public void Zero_length_input_produces_no_geometry()
    {
        Assert.IsTrue(new BinkPenBrush().BuildGeometry(BinkStrokePoints.Empty.AsView()).IsEmpty);
    }

    [TestMethod]
    public void Duplicate_points_do_not_produce_invalid_geometry()
    {
        var buffer = new BinkStrokePointBuffer();
        for (int i = 0; i < 12; i++) buffer.Add(new BinkPoint(50, 50, 0.5f, i * 8f));
        BinkStrokeGeometry geometry = new BinkPenBrush().BuildGeometry(buffer.ToStrokePoints().AsView());
        foreach (float c in geometry.Path.Coordinates) Assert.IsTrue(float.IsFinite(c), "geometry contained a non-finite coordinate");
    }

    [TestMethod]
    public void A_very_long_stroke_is_handled()
    {
        BinkStrokePoints points = TestData.Circle(new Vector2(0, 0), 200f, 20_000);
        BinkStrokeGeometry geometry = new BinkPenBrush().BuildGeometry(points.AsView());
        Assert.IsFalse(geometry.IsEmpty);
        Assert.IsTrue(geometry.Bounds.Width > 380f);
    }
}

[TestClass]
public class StrokeBuilderTests
{
    private static BinkInputSample Sample(float x, float y, double time, BinkPointerPhase phase = BinkPointerPhase.Move, float pressure = 0.5f, BinkPointerKind kind = BinkPointerKind.Mouse)
        => new() { PointerId = 1, Kind = kind, Phase = phase, X = x, Y = y, Pressure = pressure, Timestamp = time, Buttons = 1, IsPrimary = true };

    [TestMethod]
    public void Spacing_merges_samples_that_are_too_close_together()
    {
        var builder = new BinkStrokeBuilder();
        var brush = new BinkPenBrush { Width = 10f, Spacing = 0.5f, Smoothing = 0f };
        builder.Begin(Sample(0, 0, 0, BinkPointerPhase.Down), brush, Matrix3x2.Identity);
        for (int i = 1; i <= 100; i++) builder.Add(Sample(i * 0.2f, 0, i * 4));
        BinkStrokePoints points = builder.End(Sample(20, 0, 400, BinkPointerPhase.Up));

        // 20 units at a 5-unit minimum spacing is about 4 stored points, not 100.
        Assert.IsTrue(points.Count < 10, $"expected fewer than 10 points but got {points.Count}");
        Assert.IsTrue(points.Count >= 2);
    }

    [TestMethod]
    public void Smoothing_reduces_jitter_but_keeps_the_overall_path()
    {
        var jitter = new Random(1234);
        var smooth = new BinkStrokeBuilder();
        var raw = new BinkStrokeBuilder();
        var smoothBrush = new BinkPenBrush { Width = 4f, Smoothing = 0.8f, Spacing = 0f };
        var rawBrush = new BinkPenBrush { Width = 4f, Smoothing = 0f, Spacing = 0f };

        var samples = new List<BinkInputSample>();
        for (int i = 0; i <= 100; i++) samples.Add(Sample(i * 3f, 100 + (float)(jitter.NextDouble() - 0.5) * 6f, i * 8));

        smooth.Begin(samples[0] with { Phase = BinkPointerPhase.Down }, smoothBrush, Matrix3x2.Identity);
        raw.Begin(samples[0] with { Phase = BinkPointerPhase.Down }, rawBrush, Matrix3x2.Identity);
        for (int i = 1; i < samples.Count; i++)
        {
            smooth.Add(samples[i]);
            raw.Add(samples[i]);
        }
        BinkStrokePoints smoothed = smooth.End();
        BinkStrokePoints unsmoothed = raw.End();

        Assert.IsTrue(Roughness(smoothed) < Roughness(unsmoothed) * 0.6f, "smoothing did not reduce jitter");
        Assert.IsTrue(MathF.Abs(smoothed.Bounds.Width - unsmoothed.Bounds.Width) < 12f, "smoothing distorted the overall path");
    }

    private static float Roughness(BinkStrokePoints points)
    {
        float sum = 0;
        for (int i = 2; i < points.Count; i++)
        {
            sum += MathF.Abs(points.Y[i] - 2 * points.Y[i - 1] + points.Y[i - 2]);
        }
        return points.Count > 2 ? sum / (points.Count - 2) : 0;
    }

    [TestMethod]
    public void Simulated_pressure_makes_fast_movement_thinner_than_slow_movement()
    {
        var brush = new BinkPenBrush { Width = 10f, SimulatePressure = true, Spacing = 0f, Smoothing = 0f };

        var slow = new BinkStrokeBuilder();
        slow.Begin(Sample(0, 0, 0, BinkPointerPhase.Down), brush, Matrix3x2.Identity);
        for (int i = 1; i <= 40; i++) slow.Add(Sample(i * 1f, 0, i * 16));
        BinkStrokePoints slowPoints = slow.End();

        var fast = new BinkStrokeBuilder();
        fast.Begin(Sample(0, 0, 0, BinkPointerPhase.Down), brush, Matrix3x2.Identity);
        for (int i = 1; i <= 40; i++) fast.Add(Sample(i * 20f, 0, i * 16));
        BinkStrokePoints fastPoints = fast.End();

        float slowPressure = slowPoints.Pressure[^1];
        float fastPressure = fastPoints.Pressure[^1];
        Assert.IsTrue(fastPressure < slowPressure, $"fast strokes should be lighter: fast {fastPressure}, slow {slowPressure}");
    }

    [TestMethod]
    public void Real_pen_pressure_is_used_instead_of_simulation()
    {
        var builder = new BinkStrokeBuilder { PressureSmoothing = 0f };
        var brush = new BinkPenBrush { Width = 10f, Spacing = 0f, Smoothing = 0f };
        builder.Begin(Sample(0, 0, 0, BinkPointerPhase.Down, 0.2f, BinkPointerKind.Pen), brush, Matrix3x2.Identity);
        for (int i = 1; i <= 10; i++) builder.Add(Sample(i * 5f, 0, i * 16, BinkPointerPhase.Move, 0.9f, BinkPointerKind.Pen));
        BinkStrokePoints points = builder.End();

        Assert.IsTrue(builder.HasRealPressure);
        Assert.AreEqual(0.9f, points.Pressure[^1], 0.05f);
        Assert.IsTrue(builder.InputInfo.HasRealPressure);
        Assert.AreEqual(BinkPointerKind.Pen, builder.InputInfo.Kind);
    }

    [TestMethod]
    public void A_mouse_reporting_the_default_pressure_is_not_treated_as_pressure_hardware()
    {
        var builder = new BinkStrokeBuilder();
        builder.Begin(Sample(0, 0, 0, BinkPointerPhase.Down), new BinkPenBrush(), Matrix3x2.Identity);
        for (int i = 1; i <= 5; i++) builder.Add(Sample(i * 5f, 0, i * 16));
        builder.End();
        Assert.IsFalse(builder.HasRealPressure);
    }

    [TestMethod]
    public void The_view_to_document_transform_is_applied()
    {
        var builder = new BinkStrokeBuilder();
        Matrix3x2 viewToDocument = new BinkViewport(2f, new Vector2(50, 20)).ViewToDocument;
        builder.Begin(Sample(50, 20, 0, BinkPointerPhase.Down), new BinkPenBrush { Spacing = 0f, Smoothing = 0f }, viewToDocument);
        builder.Add(Sample(150, 220, 16));
        BinkStrokePoints points = builder.End();

        Assert.AreEqual(0f, points.X[0], 5e-4);
        Assert.AreEqual(0f, points.Y[0], 5e-4);
        Assert.AreEqual(50f, points.X[^1], 5e-4);
        Assert.AreEqual(100f, points.Y[^1], 5e-4);
    }

    [TestMethod]
    public void The_stroke_ends_exactly_where_the_pointer_lifted()
    {
        var builder = new BinkStrokeBuilder();
        builder.Begin(Sample(0, 0, 0, BinkPointerPhase.Down), new BinkPenBrush { Width = 4f, Smoothing = 0.9f }, Matrix3x2.Identity);
        for (int i = 1; i <= 20; i++) builder.Add(Sample(i * 5f, 0, i * 16));
        BinkStrokePoints points = builder.End(Sample(100, 0, 340, BinkPointerPhase.Up));
        Assert.AreEqual(100f, points.X[^1], 0.5f);
    }

    [TestMethod]
    public void Cancel_discards_the_stroke()
    {
        var builder = new BinkStrokeBuilder();
        builder.Begin(Sample(0, 0, 0, BinkPointerPhase.Down), new BinkPenBrush(), Matrix3x2.Identity);
        builder.Add(Sample(10, 10, 16));
        builder.Cancel();
        Assert.IsFalse(builder.IsActive);
        Assert.AreEqual(0, builder.Count);
    }
}

[TestClass]
public class EraserTests
{
    [TestMethod]
    public void Segment_erasing_splits_a_stroke_in_two()
    {
        var stroke = new BinkStroke(TestData.Line(new Vector2(0, 0), new Vector2(100, 0), 101), new BinkPenBrush { Width = 2f });
        var fragments = new List<BinkStroke>();
        bool erased = BinkStrokeEditor.EraseSegments(stroke, [new Vector2(50, 0)], 8f, fragments);

        Assert.IsTrue(erased);
        Assert.AreEqual(2, fragments.Count);
        Assert.IsTrue(fragments[0].Points.Bounds.Right < 45f);
        Assert.IsTrue(fragments[1].Points.Bounds.Left > 55f);
        Assert.AreEqual(stroke.Id, fragments[0].Id);
        Assert.AreNotEqual(stroke.Id, fragments[1].Id);
    }

    [TestMethod]
    public void Erasing_the_middle_of_nothing_leaves_the_stroke_untouched()
    {
        var stroke = new BinkStroke(TestData.Line(new Vector2(0, 0), new Vector2(100, 0), 50), new BinkPenBrush());
        var fragments = new List<BinkStroke>();
        Assert.IsFalse(BinkStrokeEditor.EraseSegments(stroke, [new Vector2(50, 500)], 8f, fragments));
    }

    [TestMethod]
    public void Erasing_everything_leaves_no_fragments()
    {
        var stroke = new BinkStroke(TestData.Line(new Vector2(0, 0), new Vector2(10, 0), 20), new BinkPenBrush());
        var fragments = new List<BinkStroke>();
        Assert.IsTrue(BinkStrokeEditor.EraseSegments(stroke, [new Vector2(5, 0)], 50f, fragments));
        Assert.IsEmpty(fragments);
    }

    [TestMethod]
    public void Erasing_an_end_shortens_the_stroke_without_splitting_it()
    {
        var stroke = new BinkStroke(TestData.Line(new Vector2(0, 0), new Vector2(100, 0), 101), new BinkPenBrush { Width = 2f });
        var fragments = new List<BinkStroke>();
        Assert.IsTrue(BinkStrokeEditor.EraseSegments(stroke, [new Vector2(100, 0)], 20f, fragments));
        Assert.ContainsSingle(fragments);
        Assert.IsTrue(fragments[0].Points.Bounds.Right < 80f);
    }

    [TestMethod]
    public void Segment_erasing_respects_the_element_transform()
    {
        var stroke = new BinkStroke(TestData.Line(new Vector2(0, 0), new Vector2(100, 0), 101), new BinkPenBrush { Width = 2f })
        {
            Transform = Matrix3x2.CreateTranslation(500, 500),
        };
        var fragments = new List<BinkStroke>();
        Assert.IsFalse(BinkStrokeEditor.EraseSegments(stroke, [new Vector2(50, 0)], 8f, fragments));
        Assert.IsTrue(BinkStrokeEditor.EraseSegments(stroke, [new Vector2(550, 500)], 8f, fragments));
        Assert.AreEqual(2, fragments.Count);
    }

    [TestMethod]
    public void A_swept_eraser_path_erases_along_its_whole_length()
    {
        var stroke = new BinkStroke(TestData.Line(new Vector2(0, 50), new Vector2(200, 50), 201), new BinkPenBrush { Width = 2f });
        var fragments = new List<BinkStroke>();
        Vector2[] path = [new(40, 50), new(160, 50)];
        Assert.IsTrue(BinkStrokeEditor.EraseSegments(stroke, path, 5f, fragments));
        Assert.AreEqual(2, fragments.Count);
        Assert.IsTrue(fragments[0].Points.Bounds.Right < 40f);
        Assert.IsTrue(fragments[1].Points.Bounds.Left > 160f);
    }
}

[TestClass]
public class PressureFidelityTests
{
    private static BinkInputSample Pen(float x, float y, double time, float pressure, BinkPointerPhase phase = BinkPointerPhase.Move)
        => new() { PointerId = 1, Kind = BinkPointerKind.Pen, Phase = phase, X = x, Y = y, Pressure = pressure, Timestamp = time, Buttons = phase == BinkPointerPhase.Up ? 0 : 1, IsPrimary = true };

    [TestMethod]
    public void The_builder_keeps_the_full_pressure_range_of_a_pen()
    {
        var builder = new BinkStrokeBuilder();
        builder.Begin(Pen(40, 150, 0, 0.12f, BinkPointerPhase.Down), new BinkPenBrush { Width = 3f }, Matrix3x2.Identity);
        for (int i = 1; i <= 60; i++) builder.Add(Pen(40 + i * 8f, 150, i * 5, i < 30 ? 0.12f : 1.0f));
        BinkStrokePoints points = builder.End(Pen(520, 150, 305, 0f, BinkPointerPhase.Up));

        Assert.IsTrue(points.Pressure[0] < 0.3f, $"the light start should stay light but was {points.Pressure[0]}");
        float heaviest = 0f;
        foreach (float p in points.Pressure) heaviest = MathF.Max(heaviest, p);
        Assert.IsTrue(heaviest > 0.9f, $"the heavy half should reach full pressure but the maximum was {heaviest}");
    }

    [TestMethod]
    public void Simplification_preserves_pressure_variation_on_a_straight_line()
    {
        // A perfectly straight line with a pressure ramp: position-only simplification would collapse it to two
        // points and throw the pressure information away.
        var buffer = new BinkStrokePointBuffer();
        for (int i = 0; i <= 60; i++) buffer.Add(new BinkPoint(i * 8f, 100f, i < 30 ? 0.12f : 1.0f, i * 5f));
        BinkStrokePoints original = buffer.ToStrokePoints();

        BinkStrokePoints simplified = original.Simplify(0.08f);

        Assert.IsTrue(simplified.Count >= 3, $"expected the pressure step to be preserved but only {simplified.Count} points remain");
        float min = 1f, max = 0f;
        foreach (float p in simplified.Pressure)
        {
            min = MathF.Min(min, p);
            max = MathF.Max(max, p);
        }
        Assert.IsTrue(min < 0.3f && max > 0.9f, $"pressure range collapsed to {min}..{max}");
    }

    [TestMethod]
    public void Simplification_still_removes_redundant_points_on_a_constant_pressure_line()
    {
        var buffer = new BinkStrokePointBuffer();
        for (int i = 0; i <= 200; i++) buffer.Add(new BinkPoint(i * 2f, 50f, 0.5f, i * 4f));
        BinkStrokePoints simplified = buffer.ToStrokePoints().Simplify(0.1f);
        Assert.IsTrue(simplified.Count < 10, $"a straight constant-pressure line should collapse but kept {simplified.Count} points");
    }

    [TestMethod]
    public void A_pressure_ramp_produces_a_widening_outline()
    {
        var buffer = new BinkStrokePointBuffer();
        for (int i = 0; i <= 60; i++) buffer.Add(new BinkPoint(i * 8f, 100f, i < 30 ? 0.12f : 1.0f, i * 5f));
        var brush = new BinkPenBrush { Width = 12f, TaperEnd = 0f };
        BinkStrokeGeometry geometry = brush.BuildGeometry(buffer.ToStrokePoints().Simplify(0.08f).AsView());

        float light = MeasureHeight(geometry.Path, 100f);
        float heavy = MeasureHeight(geometry.Path, 400f);
        Assert.IsTrue(heavy > light * 2f, $"expected the heavy half to be much wider: light {light}, heavy {heavy}");
    }

    [TestMethod]
    public void The_full_input_pipeline_keeps_a_pressure_step_visible()
    {
        // Mirrors the handwriting sample exactly: a pen writing a straight line, light then heavy.
        var brush = new BinkPenBrush { Width = 3f, Smoothing = 0.35f, Spacing = 0.12f, TaperEnd = 5f };
        var builder = new BinkStrokeBuilder();
        builder.Begin(Pen(40, 150, 0, 0.12f, BinkPointerPhase.Down), brush, Matrix3x2.Identity);
        for (int i = 1; i <= 60; i++) builder.Add(Pen(40 + i * 8f, 150, i * 5, i < 30 ? 0.12f : 1.0f));
        BinkStrokePoints points = builder.End(Pen(520, 150, 305, 0f, BinkPointerPhase.Up)).Simplify(0.08f);

        BinkStrokeGeometry geometry = brush.BuildGeometry(points.AsView());
        float light = MeasureHeight(geometry.Path, 140f);
        float heavy = MeasureHeight(geometry.Path, 420f);

        Assert.IsTrue(heavy > light * 1.6f,
            $"pressure should widen the stroke: light {light:0.00}, heavy {heavy:0.00}, {points.Count} points, pressures {string.Join(", ", points.Pressure.ToArray().Select(v => v.ToString("0.00")))}");
    }

    private static float MeasureHeight(BinkPathGeometry outline, float x)
    {
        var points = new List<System.Numerics.Vector2>();
        outline.Flatten(0.05f, (p, _) => points.AddRange(p.Span));
        float min = float.MaxValue, max = float.MinValue;
        for (int i = 1; i < points.Count; i++)
        {
            System.Numerics.Vector2 a = points[i - 1];
            System.Numerics.Vector2 b = points[i];
            if ((a.X - x) * (b.X - x) > 0) continue;
            float t = MathF.Abs(b.X - a.X) < 1e-6f ? 0 : (x - a.X) / (b.X - a.X);
            float y = a.Y + (b.Y - a.Y) * t;
            min = MathF.Min(min, y);
            max = MathF.Max(max, y);
        }
        return max - min;
    }
}
