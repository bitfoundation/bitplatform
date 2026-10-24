using System.Numerics;

namespace Bit.Bink.Tests;

/// <summary>Polygons, polylines, paths and SVG path data, and the tools that draw them.</summary>
[TestClass]
public sealed class PrimitiveTests
{
    private static readonly BinkShapeStyle Filled = new() { FillColor = BinkColor.Parse("#2563eb"), StrokeWidth = 2f };

    [TestMethod]
    public void Shape_factories_fill_the_box_they_are_given()
    {
        var box = new BinkRect(10, 20, 100, 60);
        foreach (BinkPolygonShape shape in new[] { BinkPolygonShape.Diamond(box), BinkPolygonShape.Triangle(box), BinkPolygonShape.Regular(box, 6), BinkPolygonShape.Star(box, 5) })
        {
            BinkRect bounds = BinkGeometryMath.Bounds(shape.Points.ToArray());
            Assert.AreEqual(box.Left, bounds.Left, 1e-3f, shape.Points.Count + " points");
            Assert.AreEqual(box.Top, bounds.Top, 1e-3f);
            Assert.AreEqual(box.Right, bounds.Right, 1e-3f);
            Assert.AreEqual(box.Bottom, bounds.Bottom, 1e-3f);
            Assert.IsTrue(shape.IsClosed);
        }
        Assert.HasCount(10, BinkPolygonShape.Star(box, 5).Points);
        Assert.HasCount(6, BinkPolygonShape.Regular(box, 6).Points);
    }

    [TestMethod]
    public void A_polygon_needs_at_least_two_finite_points_and_copies_them()
    {
        Assert.ThrowsExactly<ArgumentException>(() => new BinkPolygonShape([Vector2.Zero]));
        Assert.ThrowsExactly<ArgumentException>(() => new BinkPolygonShape([Vector2.Zero, new Vector2(float.NaN, 1)]));
        var points = new List<Vector2> { Vector2.Zero, new(10, 0) };
        var shape = new BinkPolygonShape(points, isClosed: false);
        points[1] = new Vector2(500, 500);
        Assert.AreEqual(new Vector2(10, 0), shape.Points[1]);
    }

    [TestMethod]
    public void A_filled_polygon_is_hit_inside_and_an_open_polyline_only_on_its_line()
    {
        BinkPolygonShape triangle = BinkPolygonShape.Triangle(new BinkRect(0, 0, 100, 100)) with { Style = Filled };
        Assert.IsTrue(triangle.HitTest(new Vector2(50, 70)));
        Assert.IsFalse(triangle.HitTest(new Vector2(5, 10)), "Outside the triangle but inside its bounds.");

        var open = new BinkPolygonShape([new(0, 0), new(100, 0), new(100, 100)], isClosed: false) { Style = Filled };
        Assert.IsTrue(open.HitTest(new Vector2(100, 50), 2f));
        Assert.IsFalse(open.HitTest(new Vector2(70, 30), 2f), "An open polyline is never filled.");
        Assert.AreEqual("Polyline", open.TypeName);
        Assert.AreEqual("Arrow", (open with { EndArrow = true }).TypeName);
    }

    [TestMethod]
    public void A_marquee_across_the_empty_corner_of_a_diamond_misses_it()
    {
        BinkPolygonShape diamond = BinkPolygonShape.Diamond(new BinkRect(0, 0, 100, 100)) with { Style = Filled };
        Assert.IsFalse(diamond.IntersectsRect(new BinkRect(-5, -5, 20, 20)));
        Assert.IsTrue(diamond.IntersectsRect(new BinkRect(40, -5, 20, 20)), "Across the top vertex.");
        Assert.IsTrue(diamond.IntersectsRect(new BinkRect(45, 45, 10, 10)), "Inside the filled diamond.");
    }

    [TestMethod]
    public void An_arrow_head_follows_the_last_segment_and_widens_the_bounds()
    {
        var plain = new BinkPolygonShape([new(0, 0), new(100, 0), new(100, 100)], isClosed: false) { Style = new BinkShapeStyle { StrokeWidth = 2f } };
        BinkPolygonShape arrow = plain with { EndArrow = true };
        Assert.IsGreaterThan(plain.Bounds.Width, arrow.Bounds.Width);
        Assert.IsGreaterThan(plain.GetPath().VerbCount, arrow.GetPath().VerbCount);
        Assert.AreNotSame(plain.GetPath(), arrow.GetPath(), "A copy with arrows does not reuse the path without them.");
    }

    [TestMethod]
    public void Svg_path_data_reads_every_command()
    {
        BinkPathGeometry path = BinkSvgPathData.Parse("M10,10 L20 10 H30 V20 h-10 v10 l-10,0 Z m0,40 c10,0 20,10 20,20 s10,20 20,20 q10,0 10,10 t10,10 C1,2 3,4 5,6 S7,8 9,10 Q1,1 2,2 T3,3 a5 5 0 0 1 10 0 A5,5 0 1,0 20,20 z");
        Assert.IsFalse(path.IsEmpty);
        Assert.AreEqual(BinkPathVerb.MoveTo, path.Verbs[0]);
        Assert.AreEqual(BinkPathVerb.Close, path.Verbs[7]);
        // Implicit coordinates after a moveto are linetos, and numbers may run together.
        BinkPathGeometry implicitLines = BinkSvgPathData.Parse("M0 0 10-10 1.5.5");
        Assert.AreEqual(BinkPathVerb.LineTo, implicitLines.Verbs[1]);
        Assert.AreEqual(BinkPathVerb.LineTo, implicitLines.Verbs[2]);
        Assert.AreEqual(0.5f, implicitLines.Coordinates[^1], 1e-6f);
        Assert.AreEqual(1.5f, implicitLines.Coordinates[^2], 1e-6f);
        Assert.AreEqual(-10f, implicitLines.Coordinates[3], 1e-6f);
        // Scientific notation and arc flags written without separators.
        Assert.AreEqual(1000f, BinkSvgPathData.Parse("M1e3 0").Coordinates[0]);
        Assert.IsFalse(BinkSvgPathData.Parse("M0 0a10 10 0 0110 10").IsEmpty);
    }

    [TestMethod]
    public void An_arc_becomes_curves_that_follow_the_circle()
    {
        // A half circle of radius 50 from (0,50) to (100,50) through the top.
        BinkPathGeometry arc = BinkSvgPathData.Parse("M0 50 A50 50 0 0 1 100 50");
        Assert.AreEqual(3, arc.VerbCount, "A half turn is two quarter-turn cubics after the moveto.");
        var flattened = new List<Vector2>();
        arc.Flatten(0.1f, (points, _) => flattened.AddRange(points.ToArray()));
        foreach (Vector2 p in flattened) Assert.AreEqual(50f, Vector2.Distance(p, new Vector2(50, 50)), 0.1f);
        Assert.AreEqual(0f, flattened.Min(p => p.Y), 0.1f, "It passes through the top.");
    }

    [TestMethod]
    public void An_arc_with_enormous_radii_stays_finite()
    {
        // Squared and multiplied, radii like these overflow a float; the short way round is the straight gap.
        BinkPathGeometry flat = BinkSvgPathData.Parse("M0 0 A1e10 1e10 0 0 1 10 0");
        var points = new List<Vector2>();
        flat.Flatten(0.1f, (p, _) => points.AddRange(p.ToArray()));
        Assert.IsTrue(points.All(p => float.IsFinite(p.X) && float.IsFinite(p.Y) && MathF.Abs(p.Y) < 0.01f));
        Assert.AreEqual(new Vector2(10, 0), points[^1]);

        // The long way round an ellipse that wide reaches past what a float holds, which is reported, not drawn.
        Assert.IsFalse(BinkSvgPathData.TryParse("M0 0 A3e38 3e38 0 1 1 10 0", out _, out string? error));
        Assert.Contains("too large", error!);
    }

    [TestMethod]
    public void A_path_is_hit_where_it_is_filled()
    {
        // A canvas fills every sub-path, closed or not, and by the path's rule; the hit test has to agree.
        BinkPathElement open = BinkPathElement.FromSvgPathData("M0 0 L100 0 L100 100 L0 100") with { Style = Filled };
        Assert.IsTrue(open.HitTest(new Vector2(50, 50)), "An open sub-path is filled as if closed.");

        BinkPathElement ring = BinkPathElement.FromSvgPathData("M0 0 H100 V100 H0 Z M25 25 H75 V75 H25 Z") with { Style = Filled, FillRule = BinkFillRule.EvenOdd };
        Assert.IsFalse(ring.HitTest(new Vector2(50, 50)), "The hole of an even-odd ring.");
        Assert.IsTrue(ring.HitTest(new Vector2(12, 50)));
        Assert.IsFalse((ring with { Transform = Matrix3x2.CreateScale(2f, 1f) }).HitTest(new Vector2(100, 50)), "Stretched too.");
        Assert.IsTrue((ring with { FillRule = BinkFillRule.NonZero }).HitTest(new Vector2(50, 50)), "Both squares wind the same way, so non-zero fills the middle.");
    }

    [TestMethod]
    public void A_path_the_document_could_not_read_back_is_not_added()
    {
        var document = new BinkDocument(BinkDocumentSettings.Default, new BinkDocumentLimits { MaxPointsPerStroke = 20 });
        BinkPathElement path = BinkPathElement.FromSvgPathData("M0 0" + string.Concat(Enumerable.Repeat(" L1 1", 100)));

        Assert.ThrowsExactly<InvalidOperationException>(() => document.AddElement(path));
        Assert.AreEqual(0, document.ElementCount);
    }

    [TestMethod]
    public void Malformed_path_data_is_reported_with_where_it_went_wrong()
    {
        Assert.IsTrue(BinkSvgPathData.Parse("").IsEmpty, "Empty path data is valid and draws nothing.");
        foreach (string bad in new[] { "L 10 10", "M 10", "M 0 0 L x", "M0 0 A 10 10 0 2 1 5 5", "M0 0 Z 5", "M 1e 5" })
        {
            Assert.IsFalse(BinkSvgPathData.TryParse(bad, out _, out string? error), bad);
            Assert.Contains("at character", error!, bad);
        }
        Assert.IsFalse(BinkSvgPathData.TryParse("M0 0" + string.Concat(Enumerable.Repeat(" L1 1", 50)), out _, out string? limit, maxSegments: 20));
        Assert.Contains("20 segments", limit!);
        Assert.ThrowsExactly<FormatException>(() => BinkSvgPathData.Parse("M 1e999 0"));
    }

    [TestMethod]
    public void Path_data_round_trips_through_the_writer()
    {
        const string data = "M10,10 C20,0 30,0 40,10 Q50,20 60,10 L70,30 Z";
        BinkPathGeometry path = BinkSvgPathData.Parse(data);
        BinkPathGeometry again = BinkSvgPathData.Parse(path.ToSvgPathData());
        CollectionAssert.AreEqual(path.Verbs.ToArray(), again.Verbs.ToArray());
        CollectionAssert.AreEqual(path.Coordinates.ToArray(), again.Coordinates.ToArray());
    }

    [TestMethod]
    public void Polygons_and_paths_survive_saving_and_loading()
    {
        var document = new BinkDocument();
        var polyline = new BinkPolygonShape([new(0, 0), new(50, 10), new(100, 0)], isClosed: false) { Smooth = true, EndArrow = true, ArrowSize = 4f, Style = Filled };
        BinkPolygonShape star = BinkPolygonShape.Star(new BinkRect(200, 0, 80, 80)) with { Style = Filled };
        BinkPathElement path = BinkPathElement.FromSvgPathData("M0 100 L100 100 L50 150 Z M30 110 L70 110 L50 130 Z") with { FillRule = BinkFillRule.EvenOdd, Style = Filled };
        document.AddElement(polyline);
        document.AddElement(star);
        document.AddElement(path);

        BinkDocument loaded = BinkSerializer.Deserialize(BinkSerializer.Serialize(document));

        var loadedPolyline = (BinkPolygonShape)loaded.GetElement(polyline.Id);
        Assert.IsFalse(loadedPolyline.IsClosed);
        Assert.IsTrue(loadedPolyline.Smooth && loadedPolyline.EndArrow && !loadedPolyline.StartArrow);
        Assert.AreEqual(4f, loadedPolyline.ArrowSize);
        CollectionAssert.AreEqual(polyline.Points.ToArray(), loadedPolyline.Points.ToArray());
        Assert.AreEqual(Filled, loadedPolyline.Style);
        CollectionAssert.AreEqual(star.Points.ToArray(), ((BinkPolygonShape)loaded.GetElement(star.Id)).Points.ToArray(), new PointComparer());
        var loadedPath = (BinkPathElement)loaded.GetElement(path.Id);
        Assert.AreEqual(BinkFillRule.EvenOdd, loadedPath.FillRule);
        CollectionAssert.AreEqual(path.Path.Verbs.ToArray(), loadedPath.Path.Verbs.ToArray());
    }

    [TestMethod]
    public void Hostile_polygons_and_paths_are_refused()
    {
        static string Doc(string element) => $$"""{"format":"bink","version":1,"layers":[{"elements":[{{element}}]}]}""";
        Assert.IsFalse(BinkSerializer.TryDeserialize(Doc("""{"type":"polygon","points":[1,2,3]}"""), out _, out _));
        Assert.IsFalse(BinkSerializer.TryDeserialize(Doc("""{"type":"polygon","points":[1,2]}"""), out _, out _));
        Assert.IsFalse(BinkSerializer.TryDeserialize(Doc("""{"type":"path","d":"Q 1 2"}"""), out _, out string? error));
        Assert.Contains("path", error!);
        string far = Doc("""{"type":"path","d":"M0 0 L 5000000 0"}""");
        Assert.IsFalse(BinkSerializer.TryDeserialize(far, out _, out _), "Coordinates are held to the document's range like every other element's.");
        var limits = new BinkDeserializerOptions { Limits = new BinkDocumentLimits { MaxPointsPerStroke = 20 } };
        string many = Doc($$"""{"type":"path","d":"M0 0{{string.Concat(Enumerable.Repeat(" L1 1", 100))}}"}""");
        Assert.IsFalse(BinkSerializer.TryDeserialize(many, out _, out _, limits), "Path data is bounded like stroke points.");
    }

    [TestMethod]
    public void New_shapes_render_the_same_in_svg_and_the_rasterizer()
    {
        var document = new BinkDocument(new BinkDocumentSettings { Width = 120, Height = 120 });
        document.AddElement(BinkPolygonShape.Diamond(new BinkRect(10, 10, 100, 100)) with { Style = new BinkShapeStyle { FillColor = BinkColor.Black, StrokeWidth = 0f } });
        string svg = BinkSvgExporter.Export(document);
        Assert.Contains("<path", svg);

        BinkRasterSurface surface = BinkPngExporter.Render(document, BinkPngExportOptions.Default, out _, out _);
        Assert.IsGreaterThan(200, surface.GetPixel(60, 60).A, "The centre is filled.");
        Assert.AreEqual(255, surface.GetPixel(15, 15).R, "The corner outside the diamond stays white.");
    }

    private sealed class PointComparer : System.Collections.IComparer
    {
        public int Compare(object? x, object? y) => Vector2.Distance((Vector2)x!, (Vector2)y!) < 0.01f ? 0 : 1;
    }
}

/// <summary>The shape tool's new kinds and the polyline tool.</summary>
[TestClass]
public sealed class PrimitiveToolTests
{
    private static BinkInputSample Sample(BinkPointerPhase phase, float x, float y, double time, bool shift = false) => new()
    {
        PointerId = 1,
        Kind = BinkPointerKind.Mouse,
        Phase = phase,
        X = x,
        Y = y,
        Pressure = 0.5f,
        Timestamp = time,
        Buttons = phase is BinkPointerPhase.Up or BinkPointerPhase.Hover ? 0 : 1,
        IsPrimary = true,
        Shift = shift,
    };

    private static void Click(TestToolContext context, IBinkTool tool, float x, float y, double time)
    {
        tool.OnPointerDown(context, Sample(BinkPointerPhase.Down, x, y, time));
        tool.OnPointerUp(context, Sample(BinkPointerPhase.Up, x, y, time + 30));
    }

    [TestMethod]
    public void The_shape_tool_draws_diamonds_triangles_polygons_and_stars()
    {
        foreach (BinkShapeKind kind in new[] { BinkShapeKind.Diamond, BinkShapeKind.Triangle, BinkShapeKind.Polygon, BinkShapeKind.Star })
        {
            using var context = new TestToolContext();
            context.Line(new BinkShapeTool(kind) { PolygonSides = 8 }, new Vector2(100, 100), new Vector2(200, 180));
            var shape = (BinkPolygonShape)context.Document.AllElements.Single();
            Assert.AreEqual(100f, BinkGeometryMath.Bounds(shape.Points.ToArray()).Left, 0.5f, kind.ToString());
            if (kind == BinkShapeKind.Polygon) Assert.HasCount(8, shape.Points);
        }
    }

    [TestMethod]
    public void Enter_adds_a_shape_in_the_middle_of_the_view_for_keyboard_users()
    {
        using var context = new TestToolContext { Viewport = new BinkViewport(2f, Vector2.Zero) };
        var tool = new BinkShapeTool(BinkShapeKind.Diamond);

        Assert.IsTrue(tool.OnKeyDown(context, new BinkToolKeyEvent("Enter", false, false, false)));

        BinkElement shape = context.Document.AllElements.Single();
        Assert.IsInstanceOfType<BinkPolygonShape>(shape);
        Vector2 middle = context.Viewport.ToDocument(context.ViewSize / 2f);
        Assert.AreEqual(middle.X, shape.Bounds.Center.X, 0.5f);
        Assert.AreEqual(60f, ((BinkPolygonShape)shape).Points.Max(p => p.X) - ((BinkPolygonShape)shape).Points.Min(p => p.X), 0.01f, "120 screen pixels wide at zoom 2.");
        Assert.AreEqual(1, context.History.UndoCount);
        Assert.Contains("added in the middle of the view", context.Announcements[^1]);

        context.Document.UpdateLayer(context.Document.Layers[0].Id, p => p with { IsLocked = true });
        Assert.IsFalse(tool.OnKeyDown(context, new BinkToolKeyEvent("Enter", false, false, false)), "Nothing is added to a locked layer.");
    }

    [TestMethod]
    public void Clicks_place_points_and_a_double_click_finishes_as_one_undo_step()
    {
        using var context = new TestToolContext();
        context.History.Clear();
        var tool = new BinkPolylineTool { EndArrow = true };
        Assert.IsTrue(tool.WantsHover);

        Click(context, tool, 100, 100, 1000);
        tool.OnPointerMove(context, Sample(BinkPointerPhase.Hover, 180, 120, 1100));
        Assert.AreEqual(1, tool.Points.Count);
        Click(context, tool, 200, 100, 2000);
        Click(context, tool, 200, 200, 3000);
        Assert.AreEqual(0, context.Document.ElementCount, "Nothing is added until the polyline is finished.");
        Click(context, tool, 200, 200, 3200);

        var shape = (BinkPolygonShape)context.Document.AllElements.Single();
        Assert.IsFalse(shape.IsClosed);
        Assert.IsTrue(shape.EndArrow);
        CollectionAssert.AreEqual(new[] { new Vector2(100, 100), new Vector2(200, 100), new Vector2(200, 200) }, shape.Points.ToArray());
        Assert.AreEqual(1, context.History.UndoCount);
        Assert.IsFalse(tool.IsDrawing);
    }

    [TestMethod]
    public void Clicking_the_first_point_closes_the_polygon()
    {
        using var context = new TestToolContext();
        var tool = new BinkPolylineTool();
        Click(context, tool, 100, 100, 1000);
        Click(context, tool, 200, 100, 2000);
        Click(context, tool, 150, 200, 3000);
        Click(context, tool, 103, 98, 4000);

        var shape = (BinkPolygonShape)context.Document.AllElements.Single();
        Assert.IsTrue(shape.IsClosed);
        Assert.HasCount(3, shape.Points);
    }

    [TestMethod]
    public void Enter_finishes_Backspace_takes_a_point_back_and_Escape_abandons()
    {
        using var context = new TestToolContext();
        var tool = new BinkPolylineTool();
        Click(context, tool, 100, 100, 1000);
        Click(context, tool, 200, 100, 2000);
        Click(context, tool, 300, 100, 3000);
        Assert.IsTrue(tool.OnKeyDown(context, new BinkToolKeyEvent("Backspace", false, false, false)));
        Assert.AreEqual(2, tool.Points.Count);
        Assert.IsTrue(tool.OnKeyDown(context, new BinkToolKeyEvent("Enter", false, false, false)));
        Assert.HasCount(2, ((BinkPolygonShape)context.Document.AllElements.Single()).Points);

        Click(context, tool, 100, 300, 5000);
        Click(context, tool, 200, 300, 6000);
        Assert.IsTrue(tool.OnKeyDown(context, new BinkToolKeyEvent("Escape", false, false, false)));
        Assert.AreEqual(1, context.Document.ElementCount, "The abandoned polyline added nothing.");
        Assert.IsFalse(tool.OnKeyDown(context, new BinkToolKeyEvent("Escape", false, false, false)), "With nothing in progress, Escape is not the tool's.");
    }

    [TestMethod]
    public void A_touch_dragged_to_the_spot_places_the_point_where_it_lifts()
    {
        using var context = new TestToolContext();
        var tool = new BinkPolylineTool();
        Click(context, tool, 100, 100, 1000);
        tool.OnPointerDown(context, Sample(BinkPointerPhase.Down, 120, 110, 2000));
        tool.OnPointerMove(context, Sample(BinkPointerPhase.Move, 250, 160, 2016));
        tool.OnPointerUp(context, Sample(BinkPointerPhase.Up, 260, 160, 2032));
        CollectionAssert.AreEqual(new[] { new Vector2(100, 100), new Vector2(260, 160) }, tool.Points.ToArray());
    }

    [TestMethod]
    public void Shift_keeps_segments_to_15_degree_steps()
    {
        using var context = new TestToolContext();
        var tool = new BinkPolylineTool();
        Click(context, tool, 100, 100, 1000);
        tool.OnPointerDown(context, Sample(BinkPointerPhase.Down, 200, 104, 2000, shift: true));
        tool.OnPointerUp(context, Sample(BinkPointerPhase.Up, 200, 104, 2030, shift: true));
        Assert.AreEqual(100f, tool.Points[^1].Y, 0.01f, "Nearly horizontal becomes horizontal.");
    }
}
