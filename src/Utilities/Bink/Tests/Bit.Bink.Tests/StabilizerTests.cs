using System.Numerics;

namespace Bit.Bink.Tests;

/// <summary>The pulled-string stabilizer: tremor smaller than the string never reaches the ink.</summary>
[TestClass]
public sealed class StabilizerTests
{
    private static BinkInputSample Sample(BinkPointerPhase phase, float x, float y, double time) => new()
    {
        PointerId = 1,
        Kind = BinkPointerKind.Mouse,
        Phase = phase,
        X = x,
        Y = y,
        Pressure = 0.5f,
        Timestamp = time,
        Buttons = phase == BinkPointerPhase.Up ? 0 : 1,
        IsPrimary = true,
    };

    /// <summary>A horizontal line drawn with a shaking hand: up and down by <paramref name="tremor"/> every sample.</summary>
    private static BinkStrokePoints Shaky(float stabilizer, float tremor = 4f, BinkViewport? viewport = null)
    {
        var builder = new BinkStrokeBuilder { StabilizerRadius = stabilizer };
        var brush = new BinkPenBrush { Smoothing = 0f, Spacing = 0f };
        Matrix3x2 toDocument = (viewport ?? BinkViewport.Identity).ViewToDocument;
        builder.Begin(Sample(BinkPointerPhase.Down, 0, 100, 0), brush, toDocument);
        for (int i = 1; i < 100; i++) builder.Add(Sample(BinkPointerPhase.Move, i * 3f, 100 + (i % 2 == 0 ? tremor : -tremor), i * 8));
        return builder.End(Sample(BinkPointerPhase.Up, 300, 100, 800));
    }

    private static float Wobble(BinkStrokePoints points)
    {
        // How far the ink strays from the line it was meant to be, leaving out the finishing segment.
        float worst = 0f;
        for (int i = 0; i < points.Count - 1; i++) worst = MathF.Max(worst, MathF.Abs(points.Y[i] - 100f));
        return worst;
    }

    [TestMethod]
    public void Without_a_stabilizer_the_tremor_reaches_the_ink()
    {
        Assert.IsGreaterThan(3f, Wobble(Shaky(0f)));
    }

    [TestMethod]
    public void A_string_longer_than_the_tremor_keeps_it_out_of_the_ink()
    {
        BinkStrokePoints points = Shaky(12f);

        Assert.IsLessThan(1f, Wobble(points), "The tremor is a quarter of what reaches the ink without a stabilizer, or less.");
    }

    [TestMethod]
    public void The_ink_trails_while_drawing_and_is_finished_to_where_the_pen_lifted()
    {
        var builder = new BinkStrokeBuilder { StabilizerRadius = 20f };
        var brush = new BinkPenBrush { Smoothing = 0f, Spacing = 0f };
        builder.Begin(Sample(BinkPointerPhase.Down, 0, 0, 0), brush, Matrix3x2.Identity);
        for (int i = 1; i <= 10; i++) builder.Add(Sample(BinkPointerPhase.Move, i * 10f, 0, i * 8));

        Assert.AreEqual(80f, builder.Points.Last.X, 0.01f, "The ink is a string's length behind the pointer.");

        BinkStrokePoints points = builder.End(Sample(BinkPointerPhase.Up, 100, 0, 96));
        Assert.AreEqual(100f, points.PositionAt(points.Count - 1).X, 0.01f);
    }

    [TestMethod]
    public void A_stroke_shorter_than_the_string_is_still_a_line_to_where_it_lifted()
    {
        // The bar of a t never pulls the string taut; it used to end as a dot at the pen-down point.
        var builder = new BinkStrokeBuilder { StabilizerRadius = 30f };
        builder.Begin(Sample(BinkPointerPhase.Down, 0, 0, 0), new BinkPenBrush(), Matrix3x2.Identity);
        for (int i = 1; i <= 5; i++) builder.Add(Sample(BinkPointerPhase.Move, i * 5f, 0, i * 8));

        BinkStrokePoints points = builder.End(Sample(BinkPointerPhase.Up, 25, 0, 48));

        Assert.AreEqual(2, points.Count);
        Assert.AreEqual(25f, points.PositionAt(1).X, 0.01f);
    }

    [TestMethod]
    public void A_lift_closer_than_the_brush_spacing_still_ends_the_stroke_where_it_lifted()
    {
        // The spacing thins samples along the stroke; the lift is its end, and dropping it left a dot.
        var builder = new BinkStrokeBuilder { StabilizerRadius = 30f };
        var brush = new BinkPenBrush { Width = 10f, Spacing = 0.5f };
        builder.Begin(Sample(BinkPointerPhase.Down, 0, 0, 0), brush, Matrix3x2.Identity);
        builder.Add(Sample(BinkPointerPhase.Move, 2, 0, 8));

        BinkStrokePoints points = builder.End(Sample(BinkPointerPhase.Up, 3, 0, 16));

        Assert.AreEqual(2, points.Count, "A lift 3 units away is under the 5 unit spacing, and kept.");
        Assert.AreEqual(3f, points.PositionAt(1).X, 0.01f);
        Assert.AreNotEqual(points.PositionAt(0), points.PositionAt(1));
    }

    [TestMethod]
    public void A_stabilizer_turned_on_during_a_stroke_pulls_from_where_the_pointer_is()
    {
        var builder = new BinkStrokeBuilder();
        var brush = new BinkPenBrush { Smoothing = 0f, Spacing = 0f };
        builder.Begin(Sample(BinkPointerPhase.Down, 0, 0, 0), brush, Matrix3x2.Identity);
        for (int i = 1; i <= 10; i++) builder.Add(Sample(BinkPointerPhase.Move, i * 10f, 0, i * 8));
        Assert.AreEqual(100f, builder.Points.Last.X, 0.01f);

        builder.StabilizerRadius = 5f;
        builder.Add(Sample(BinkPointerPhase.Move, 100, 20, 88));

        // The string starts at the pointer's last position, (100, 0), so pulled taut it is straight below it. Pulled
        // from the pen-down point, the ink would cut back across to about (95, 19).
        Assert.AreEqual(100f, builder.Points.Last.X, 0.01f, "The ink never moves back towards where the stroke began.");
        Assert.AreEqual(15f, builder.Points.Last.Y, 0.01f);
    }

    [TestMethod]
    public void Movement_inside_the_string_draws_nothing()
    {
        var builder = new BinkStrokeBuilder { StabilizerRadius = 30f };
        builder.Begin(Sample(BinkPointerPhase.Down, 50, 50, 0), new BinkPenBrush(), Matrix3x2.Identity);

        Assert.IsFalse(builder.Add(Sample(BinkPointerPhase.Move, 70, 60, 8)));
        Assert.IsFalse(builder.Add(Sample(BinkPointerPhase.Move, 40, 30, 16)));
        Assert.AreEqual(1, builder.Count);
    }

    [TestMethod]
    public void The_tool_measures_the_string_on_screen_so_it_feels_the_same_at_every_zoom()
    {
        using var context = new TestToolContext { Viewport = new BinkViewport(4f, Vector2.Zero) };
        context.Brush = new BinkPenBrush { Smoothing = 0f, Spacing = 0f };
        var tool = new BinkDrawTool { Stabilization = 20f, SimplifyTolerance = 0f };

        tool.OnPointerDown(context, Sample(BinkPointerPhase.Down, 0, 0, 0));
        for (int i = 1; i <= 10; i++) tool.OnPointerMove(context, Sample(BinkPointerPhase.Move, i * 20f, 0, i * 8));
        tool.DrawOverlay(context, new BinkCanvasRenderContext());

        // The pointer is at 200 pixels, 50 units; twenty pixels at four times zoom is five units behind it.
        BinkRect live = tool.LiveBounds;
        Assert.AreEqual(45f + 2f, live.Right, 1.5f, "The live ink ends five units short of the pointer, plus the pen's half width.");
    }

    [TestMethod]
    public void Predictions_are_not_drawn_ahead_of_a_stabilized_line()
    {
        using var context = new TestToolContext();
        context.Brush = new BinkPenBrush { Smoothing = 0f, Spacing = 0f, Width = 2f };
        var tool = new BinkDrawTool { Stabilization = 20f };
        tool.OnPointerDown(context, Sample(BinkPointerPhase.Down, 0, 0, 0));
        for (int i = 1; i <= 10; i++) tool.OnPointerMove(context, Sample(BinkPointerPhase.Move, i * 10f, 0, i * 8));
        tool.DrawOverlay(context, new BinkCanvasRenderContext());
        float before = tool.LiveBounds.Right;

        tool.OnPointerMove(context, Sample(BinkPointerPhase.Move, 140, 0, 88) with { IsPredicted = true });
        tool.DrawOverlay(context, new BinkCanvasRenderContext());

        Assert.AreEqual(before, tool.LiveBounds.Right);
    }
}
