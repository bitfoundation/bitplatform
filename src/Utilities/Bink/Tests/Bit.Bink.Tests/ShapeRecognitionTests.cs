using System.Numerics;

namespace Bit.Bink.Tests;

/// <summary>Turning hand-drawn strokes into clean shapes, and the draw tool's use of it.</summary>
[TestClass]
public sealed class ShapeRecognitionTests
{
    /// <summary>Points along a closed or open outline, with hand tremor: a seeded wobble across the line.</summary>
    private static Vector2[] Trace(IReadOnlyList<Vector2> corners, bool closed, int perSide = 30, float wobble = 1.5f, int seed = 1)
    {
        var random = new Random(seed);
        var points = new List<Vector2>();
        int sides = closed ? corners.Count : corners.Count - 1;
        for (int s = 0; s < sides; s++)
        {
            Vector2 a = corners[s], b = corners[(s + 1) % corners.Count];
            for (int i = 0; i < perSide; i++)
            {
                Vector2 p = Vector2.Lerp(a, b, i / (float)perSide);
                points.Add(p + new Vector2((float)(random.NextDouble() * 2 - 1), (float)(random.NextDouble() * 2 - 1)) * wobble);
            }
        }
        points.Add(closed ? corners[0] : corners[^1]);
        return [.. points];
    }

    private static Vector2[] Ellipse(Vector2 centre, float rx, float ry, float rotation = 0f, float sweep = 1f, float wobble = 1.5f, int count = 90, int seed = 2)
    {
        var random = new Random(seed);
        var points = new Vector2[count + 1];
        for (int i = 0; i <= count; i++)
        {
            float a = i / (float)count * MathF.Tau * sweep;
            Vector2 p = new(MathF.Cos(a) * rx, MathF.Sin(a) * ry);
            p = BinkGeometryMath.Rotate(p, rotation) + centre;
            points[i] = p + new Vector2((float)(random.NextDouble() * 2 - 1), (float)(random.NextDouble() * 2 - 1)) * wobble;
        }
        return points;
    }

    [TestMethod]
    public void A_straight_stroke_is_a_line_straightened_to_horizontal()
    {
        BinkShapeRecognition? result = BinkShapeRecognizer.Recognize(Trace([new(10, 100), new(210, 112)], closed: false, perSide: 60));

        Assert.IsNotNull(result);
        Assert.AreEqual(BinkShapeKind.Line, result.Kind);
        Assert.AreEqual(result.Points[0].Y, result.Points[1].Y, 0.01f, "Within the angle snap, a line is made level.");
        Assert.AreEqual(200f, Vector2.Distance(result.Points[0], result.Points[1]), 4f);
    }

    [TestMethod]
    public void A_steep_line_beyond_the_snap_keeps_its_angle()
    {
        BinkShapeRecognition? result = BinkShapeRecognizer.Recognize(Trace([new(0, 0), new(100, 30)], closed: false, perSide: 40, wobble: 0.5f));

        Assert.IsNotNull(result);
        Assert.AreEqual(BinkShapeKind.Line, result.Kind);
        Vector2 d = result.Points[1] - result.Points[0];
        Assert.AreEqual(MathF.Atan2(30, 100), MathF.Atan2(d.Y, d.X), 0.03f);
    }

    [TestMethod]
    public void A_closed_box_is_an_upright_rectangle()
    {
        BinkShapeRecognition? result = BinkShapeRecognizer.Recognize(Trace([new(50, 40), new(250, 42), new(248, 160), new(52, 158)], closed: true));

        Assert.IsNotNull(result);
        Assert.AreEqual(BinkShapeKind.Rectangle, result.Kind);
        Assert.AreEqual(0f, result.Rotation);
        Assert.AreEqual(150f, result.Frame.Center.X, 3f);
        Assert.AreEqual(100f, result.Frame.Center.Y, 3f);
        Assert.AreEqual(198f, result.Frame.Width, 6f);
        Assert.AreEqual(118f, result.Frame.Height, 6f);
    }

    [TestMethod]
    public void A_box_drawn_at_an_angle_stays_at_that_angle()
    {
        float angle = 0.5f;
        Vector2 c = new(200, 200);
        Vector2[] corners = [new(-80, -50), new(80, -50), new(80, 50), new(-80, 50)];
        for (int i = 0; i < 4; i++) corners[i] = BinkGeometryMath.Rotate(corners[i], angle) + c;

        BinkShapeRecognition? result = BinkShapeRecognizer.Recognize(Trace(corners, closed: true, wobble: 1f));

        Assert.IsNotNull(result);
        Assert.AreEqual(BinkShapeKind.Rectangle, result.Kind);
        Assert.AreEqual(angle, result.Rotation, 0.04f);
        Assert.AreEqual(160f, result.Frame.Width, 5f);
        Assert.AreEqual(100f, result.Frame.Height, 5f);

        var element = (BinkRectangleShape)result.CreateElement(BinkShapeStyle.Default);
        Assert.AreEqual(c.X, element.Bounds.Center.X, 3f, "The element is turned about its own centre.");
        Assert.AreEqual(c.Y, element.Bounds.Center.Y, 3f);
    }

    [TestMethod]
    public void A_nearly_square_box_becomes_a_square()
    {
        BinkShapeRecognition? result = BinkShapeRecognizer.Recognize(Trace([new(0, 0), new(100, 0), new(100, 94), new(0, 94)], closed: true));

        Assert.IsNotNull(result);
        Assert.AreEqual(BinkShapeKind.Rectangle, result.Kind);
        Assert.AreEqual(result.Frame.Width, result.Frame.Height, 0.01f);
    }

    [TestMethod]
    public void A_loop_is_an_ellipse_and_a_round_one_a_circle()
    {
        BinkShapeRecognition? oval = BinkShapeRecognizer.Recognize(Ellipse(new(200, 150), 120, 60));
        Assert.IsNotNull(oval);
        Assert.AreEqual(BinkShapeKind.Ellipse, oval.Kind);
        Assert.AreEqual(240f, oval.Frame.Width, 8f);
        Assert.AreEqual(120f, oval.Frame.Height, 8f);
        Assert.AreEqual(0f, oval.Rotation);

        BinkShapeRecognition? circle = BinkShapeRecognizer.Recognize(Ellipse(new(100, 100), 50, 47));
        Assert.IsNotNull(circle);
        Assert.AreEqual(BinkShapeKind.Ellipse, circle.Kind);
        Assert.AreEqual(circle.Frame.Width, circle.Frame.Height, 0.01f);
    }

    [TestMethod]
    public void A_tilted_ellipse_keeps_its_tilt()
    {
        BinkShapeRecognition? result = BinkShapeRecognizer.Recognize(Ellipse(new(200, 200), 120, 50, rotation: 0.6f, wobble: 1f));

        Assert.IsNotNull(result);
        Assert.AreEqual(BinkShapeKind.Ellipse, result.Kind);
        Assert.AreEqual(0.6f, result.Rotation, 0.05f);
        Assert.AreEqual(240f, result.Frame.Width, 10f);
    }

    [TestMethod]
    public void A_loop_that_overshoots_or_falls_short_of_its_start_still_closes()
    {
        Assert.AreEqual(BinkShapeKind.Ellipse, BinkShapeRecognizer.Recognize(Ellipse(new(100, 100), 60, 40, sweep: 1.12f))?.Kind);
        Assert.AreEqual(BinkShapeKind.Ellipse, BinkShapeRecognizer.Recognize(Ellipse(new(100, 100), 60, 40, sweep: 0.94f))?.Kind);
    }

    [TestMethod]
    public void Three_corners_are_a_triangle_and_corners_at_the_middles_of_the_sides_a_diamond()
    {
        BinkShapeRecognition? triangle = BinkShapeRecognizer.Recognize(Trace([new(100, 20), new(180, 160), new(20, 160)], closed: true));
        Assert.IsNotNull(triangle);
        Assert.AreEqual(BinkShapeKind.Triangle, triangle.Kind);
        Assert.HasCount(3, triangle.Points);

        BinkShapeRecognition? diamond = BinkShapeRecognizer.Recognize(Trace([new(100, 20), new(200, 90), new(100, 160), new(0, 90)], closed: true));
        Assert.IsNotNull(diamond);
        Assert.AreEqual(BinkShapeKind.Diamond, diamond.Kind);
        Assert.IsInstanceOfType<BinkPolygonShape>(diamond.CreateElement(BinkShapeStyle.Default));
    }

    [TestMethod]
    public void A_pentagon_is_a_polygon()
    {
        var corners = new Vector2[5];
        for (int i = 0; i < 5; i++) corners[i] = new Vector2(150, 150) + new Vector2(MathF.Cos(i * MathF.Tau / 5 - MathF.PI / 2), MathF.Sin(i * MathF.Tau / 5 - MathF.PI / 2)) * 100;

        BinkShapeRecognition? result = BinkShapeRecognizer.Recognize(Trace(corners, closed: true, wobble: 1f));

        Assert.IsNotNull(result);
        Assert.AreEqual(BinkShapeKind.Polygon, result.Kind);
        Assert.HasCount(5, result.Points);
    }

    [TestMethod]
    public void A_shaft_with_a_head_drawn_in_one_stroke_is_an_arrow()
    {
        // Shaft to the tip, back along one barb, to the tip again, and out along the other.
        Vector2[] stroke = Trace([new(20, 100), new(220, 100), new(195, 85), new(220, 100), new(195, 116)], closed: false, perSide: 25, wobble: 0.8f);

        BinkShapeRecognition? result = BinkShapeRecognizer.Recognize(stroke);

        Assert.IsNotNull(result);
        Assert.AreEqual(BinkShapeKind.Arrow, result.Kind);
        Assert.AreEqual(220f, result.Points[1].X, 4f, "The arrow points where the shaft ended.");
        var element = (BinkLineShape)result.CreateElement(BinkShapeStyle.Default);
        Assert.IsTrue(element.EndArrow);
    }

    [TestMethod]
    public void Writing_scribbles_and_curves_are_left_as_ink()
    {
        // A wave, a spiral and a zig-zag: none is close to any shape.
        var wave = new Vector2[80];
        for (int i = 0; i < wave.Length; i++) wave[i] = new Vector2(i * 3f, 100 + MathF.Sin(i * 0.25f) * 25f);
        Assert.IsNull(BinkShapeRecognizer.Recognize(wave));

        var spiral = new Vector2[120];
        for (int i = 0; i < spiral.Length; i++) spiral[i] = new Vector2(150, 150) + new Vector2(MathF.Cos(i * 0.2f), MathF.Sin(i * 0.2f)) * (10 + i * 0.8f);
        Assert.IsNull(BinkShapeRecognizer.Recognize(spiral));

        Assert.IsNull(BinkShapeRecognizer.Recognize(Trace([new(0, 0), new(40, 60), new(80, 0), new(120, 60), new(160, 0), new(200, 60)], closed: false)));
    }

    [TestMethod]
    public void A_line_drawn_there_and_back_encloses_nothing_and_is_not_a_shape()
    {
        Assert.IsNull(BinkShapeRecognizer.Recognize(Trace([new(0, 0), new(200, 0), new(0, 2)], closed: false, wobble: 0.5f)));
    }

    [TestMethod]
    public void Tiny_strokes_degenerate_input_and_bad_numbers_are_not_recognized()
    {
        Assert.IsNull(BinkShapeRecognizer.Recognize(Trace([new(0, 0), new(6, 0), new(6, 6), new(0, 6)], closed: true, wobble: 0f)));
        Assert.IsNull(BinkShapeRecognizer.Recognize([new Vector2(5, 5)]));
        Assert.IsNull(BinkShapeRecognizer.Recognize([new Vector2(5, 5), new Vector2(5, 5), new Vector2(5, 5)]));
        Assert.IsNull(BinkShapeRecognizer.Recognize([new Vector2(0, 0), new Vector2(float.NaN, 5), new Vector2(100, 100)]));
        Assert.IsNull(BinkShapeRecognizer.Recognize([new Vector2(0, 0), new Vector2(float.PositiveInfinity, 0)]));
    }

    [TestMethod]
    public void Only_the_kinds_allowed_are_recognized()
    {
        Vector2[] box = Trace([new(50, 40), new(250, 40), new(250, 160), new(50, 160)], closed: true);

        var noRectangles = new BinkShapeRecognizerOptions { Kinds = new HashSet<BinkShapeKind> { BinkShapeKind.Polygon } };
        BinkShapeRecognition? polygon = BinkShapeRecognizer.Recognize(box, noRectangles);
        Assert.IsNotNull(polygon);
        Assert.AreEqual(BinkShapeKind.Polygon, polygon.Kind, "A rectangle left out may still be a polygon.");

        var linesOnly = new BinkShapeRecognizerOptions { Kinds = new HashSet<BinkShapeKind> { BinkShapeKind.Line } };
        Assert.IsNull(BinkShapeRecognizer.Recognize(box, linesOnly));
    }

    [TestMethod]
    public void A_tighter_tolerance_refuses_a_sloppy_drawing()
    {
        Vector2[] sloppy = Ellipse(new(200, 150), 100, 70, wobble: 6f);
        Assert.IsNotNull(BinkShapeRecognizer.Recognize(sloppy, new BinkShapeRecognizerOptions { Tolerance = 0.08f }));
        Assert.IsNull(BinkShapeRecognizer.Recognize(sloppy, new BinkShapeRecognizerOptions { Tolerance = 0.005f }));
    }

    [TestMethod]
    public void Drawing_speed_and_point_density_do_not_change_the_result()
    {
        Vector2[] sparse = Trace([new(0, 0), new(200, 0), new(200, 100), new(0, 100)], closed: true, perSide: 6, wobble: 0.5f);
        Vector2[] dense = Trace([new(0, 0), new(200, 0), new(200, 100), new(0, 100)], closed: true, perSide: 200, wobble: 0.5f);

        BinkShapeRecognition? a = BinkShapeRecognizer.Recognize(sparse);
        BinkShapeRecognition? b = BinkShapeRecognizer.Recognize(dense);

        Assert.AreEqual(BinkShapeKind.Rectangle, a?.Kind);
        Assert.AreEqual(BinkShapeKind.Rectangle, b?.Kind);
        Assert.AreEqual(a!.Frame.Width, b!.Frame.Width, 3f);
    }

    [TestMethod]
    public void Recognition_holds_across_many_hands()
    {
        // Twenty seeded drawings of each shape, at two sizes and with a visible tremor: every one is recognized as
        // what it is, so the thresholds are not tuned to one lucky drawing.
        for (int seed = 0; seed < 20; seed++)
        {
            foreach (float size in new[] { 60f, 300f })
            {
                float wobble = size * 0.012f;
                Vector2 o = new(100, 100);
                Assert.AreEqual(BinkShapeKind.Rectangle, BinkShapeRecognizer.Recognize(Trace([o, o + new Vector2(size, 0), o + new Vector2(size, size * 0.6f), o + new Vector2(0, size * 0.6f)], true, wobble: wobble, seed: seed))?.Kind, $"rectangle {seed} {size}");
                Assert.AreEqual(BinkShapeKind.Ellipse, BinkShapeRecognizer.Recognize(Ellipse(o, size / 2, size / 3, wobble: wobble, seed: seed))?.Kind, $"ellipse {seed} {size}");
                Assert.AreEqual(BinkShapeKind.Triangle, BinkShapeRecognizer.Recognize(Trace([o, o + new Vector2(size, size * 0.2f), o + new Vector2(size * 0.3f, size)], true, wobble: wobble, seed: seed))?.Kind, $"triangle {seed} {size}");
                Assert.AreEqual(BinkShapeKind.Line, BinkShapeRecognizer.Recognize(Trace([o, o + new Vector2(size, size * 0.7f)], false, perSide: 50, wobble: wobble, seed: seed))?.Kind, $"line {seed} {size}");
                Assert.AreEqual(BinkShapeKind.Arrow, BinkShapeRecognizer.Recognize(Trace([o, o + new Vector2(size, 0), o + new Vector2(size * 0.85f, -size * 0.1f)], false, perSide: 30, wobble: wobble, seed: seed))?.Kind, $"one-barbed arrow {seed} {size}");
            }
        }
    }

    [TestMethod]
    public void The_reported_error_is_that_of_the_shape_as_drawn_and_never_above_the_tolerance()
    {
        // Snapping straightens and evens a shape after it is fitted. The error used to be the fit's, so a snapped
        // shape could stray further from the stroke than the tolerance the result promises; now snapping that would
        // is not done.
        var options = new BinkShapeRecognizerOptions { Tolerance = 0.02f, AngleSnap = 10f, ProportionSnap = 0.12f };
        for (int seed = 0; seed < 20; seed++)
        {
            float tilt = 0.12f; // inside the snap, so straightening is tried
            Vector2[] corners = [new(-100, -55), new(100, -55), new(100, 55), new(-100, 55)];
            for (int i = 0; i < 4; i++) corners[i] = BinkGeometryMath.Rotate(corners[i], tilt) + new Vector2(200, 200);
            Vector2[][] strokes =
            [
                Trace(corners, closed: true, wobble: 0.5f, seed: seed),
                Ellipse(new(200, 200), 100, 92, rotation: tilt, wobble: 0.5f, seed: seed),
                Trace([new(0, 0), new(200, 30)], closed: false, perSide: 60, wobble: 0.5f, seed: seed),
            ];
            foreach (Vector2[] stroke in strokes)
            {
                BinkShapeRecognition? result = BinkShapeRecognizer.Recognize(stroke, options);
                Assert.IsNotNull(result, $"seed {seed}");
                Assert.IsLessThanOrEqualTo(options.Tolerance, result.Error, $"{result.Kind} seed {seed}");
            }
        }
    }

    [TestMethod]
    public void A_regular_reading_that_fits_badly_gives_way_to_the_next()
    {
        // A box turned 45 degrees has its corners at the middles of its bounds' sides, as a diamond does, but a
        // diamond of those bounds is a square: it fitted badly, and an ellipse used to win over it.
        BinkShapeRecognition? turned = BinkShapeRecognizer.Recognize(Trace([new(121, 0), new(200, 79), new(79, 200), new(0, 121)], closed: true, wobble: 0.3f));
        Assert.AreEqual(BinkShapeKind.Rectangle, turned?.Kind);

        var regularOnly = new BinkShapeRecognizerOptions { Kinds = new HashSet<BinkShapeKind> { BinkShapeKind.Polygon, BinkShapeKind.Rectangle, BinkShapeKind.Diamond } };
        Assert.IsNotNull(BinkShapeRecognizer.Recognize(Trace([new(121, 0), new(200, 79), new(79, 200), new(0, 121)], closed: true, wobble: 0.3f), regularOnly));

        // One slanted side: four corners, but not a rectangle.
        BinkShapeRecognition? slanted = BinkShapeRecognizer.Recognize(Trace([new(0, 0), new(200, 44), new(200, 120), new(0, 76)], closed: true, wobble: 0.3f));
        Assert.AreEqual(BinkShapeKind.Polygon, slanted?.Kind);
    }

    [TestMethod]
    public void Open_strokes_that_turn_are_not_shapes()
    {
        // An L, a U and a check mark: open, not straight, and no arrow head.
        Assert.IsNull(BinkShapeRecognizer.Recognize(Trace([new(0, 0), new(0, 100), new(80, 100)], false)));
        Assert.IsNull(BinkShapeRecognizer.Recognize(Trace([new(0, 0), new(0, 100), new(80, 100), new(80, 0)], false)));
        Assert.IsNull(BinkShapeRecognizer.Recognize(Trace([new(0, 50), new(30, 90), new(100, 0)], false)));
    }

    // The draw tool.

    private static void Draw(TestToolContext context, BinkDrawTool tool, IReadOnlyList<Vector2> points, double holdMs = 0)
    {
        double time = 0;
        BinkInputSample Make(Vector2 p, BinkPointerPhase phase) => new()
        {
            PointerId = 1,
            Kind = BinkPointerKind.Mouse,
            Phase = phase,
            X = p.X,
            Y = p.Y,
            Pressure = 0.5f,
            Timestamp = time,
            Buttons = phase == BinkPointerPhase.Up ? 0 : 1,
            IsPrimary = true,
        };
        tool.OnPointerDown(context, Make(points[0], BinkPointerPhase.Down));
        for (int i = 1; i < points.Count; i++)
        {
            time += 8;
            tool.OnPointerMove(context, Make(points[i], BinkPointerPhase.Move));
        }
        time += 8 + holdMs;
        tool.OnPointerUp(context, Make(points[^1], BinkPointerPhase.Up));
    }

    private static Vector2[] Box => Trace([new(50, 40), new(250, 42), new(248, 160), new(52, 158)], closed: true);

    [TestMethod]
    public void Recognition_is_off_by_default()
    {
        using var context = new TestToolContext();
        Draw(context, new BinkDrawTool(), Box);

        Assert.IsInstanceOfType<BinkStroke>(context.Document.AllElements.Single());
    }

    [TestMethod]
    public void A_recognized_stroke_is_replaced_by_the_shape_and_one_undo_brings_the_ink_back()
    {
        using var context = new TestToolContext();
        context.Brush = new BinkMarkerBrush { Color = BinkColor.Parse("#c62828"), Width = 6f, Opacity = 0.8f };
        var tool = new BinkDrawTool { ShapeRecognition = BinkShapeRecognitionMode.Always };

        Draw(context, tool, Box);

        var shape = (BinkRectangleShape)context.Document.AllElements.Single();
        Assert.AreEqual(BinkColor.Parse("#c62828"), shape.Style.StrokeColor);
        Assert.AreEqual(6f, shape.Style.StrokeWidth, 0.01f, "A marker has no pressure, so its width is kept.");
        Assert.AreEqual(0.8f, shape.Style.Opacity);
        Assert.Contains("Drawn as rectangle.", context.Announcements);

        Assert.IsTrue(context.History.Undo());
        Assert.IsInstanceOfType<BinkStroke>(context.Document.AllElements.Single(), "Undo returns the ink.");
        Assert.IsTrue(context.History.Undo());
        Assert.AreEqual(0, context.Document.ElementCount);
        Assert.IsTrue(context.History.Redo());
        Assert.IsTrue(context.History.Redo());
        Assert.IsInstanceOfType<BinkRectangleShape>(context.Document.AllElements.Single());
    }

    [TestMethod]
    public void The_shape_keeps_the_strokes_place_in_the_stacking_order()
    {
        using var context = new TestToolContext();
        var tool = new BinkDrawTool { ShapeRecognition = BinkShapeRecognitionMode.Always };
        context.Line(tool, new Vector2(0, 300), new Vector2(5, 300), 3); // too small to be anything: stays ink
        Draw(context, tool, Box);

        IReadOnlyList<BinkElement> elements = context.Document.Layers[0].Elements;
        Assert.HasCount(2, elements);
        Assert.IsInstanceOfType<BinkStroke>(elements[0]);
        Assert.IsInstanceOfType<BinkRectangleShape>(elements[1]);
    }

    [TestMethod]
    public void On_hold_only_a_stroke_whose_pen_rested_before_lifting_becomes_a_shape()
    {
        using var context = new TestToolContext();
        var tool = new BinkDrawTool { ShapeRecognition = BinkShapeRecognitionMode.OnHold };

        Draw(context, tool, Box);
        Assert.IsInstanceOfType<BinkStroke>(context.Document.AllElements.Single(), "Lifted at once: ink.");

        context.Document.Clear();
        Draw(context, tool, Box, holdMs: 600);
        Assert.IsInstanceOfType<BinkRectangleShape>(context.Document.AllElements.Single(), "Rested half a second: a shape.");
    }

    [TestMethod]
    public void Pixel_eraser_strokes_are_never_recognized()
    {
        using var context = new TestToolContext();
        context.Brush = new BinkPixelEraserBrush();
        Draw(context, new BinkDrawTool { ShapeRecognition = BinkShapeRecognitionMode.Always }, Box);

        Assert.IsInstanceOfType<BinkStroke>(context.Document.AllElements.Single());
    }

    [TestMethod]
    public void A_recognized_shape_survives_a_save_and_load()
    {
        using var context = new TestToolContext();
        Draw(context, new BinkDrawTool { ShapeRecognition = BinkShapeRecognitionMode.Always }, Ellipse(new(300, 200), 120, 80));

        BinkDocument loaded = BinkSerializer.Deserialize(BinkSerializer.Serialize(context.Document));

        Assert.IsInstanceOfType<BinkEllipseShape>(loaded.AllElements.Single());
    }
}
