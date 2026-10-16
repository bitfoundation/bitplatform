using System.Numerics;

namespace Bit.Bink.Tests;

/// <summary>
/// Pointer routing regressions: a gesture belongs to the contact that started it, and nothing another pointer
/// does may corrupt or discard it.
/// </summary>
[TestClass]
public class PointerRegressionTests(TestContext output)
{
    private static BinkInputSample Sample(int id, BinkPointerPhase phase, float x, float y, double t) => new()
    {
        PointerId = id,
        Kind = BinkPointerKind.Touch,
        Phase = phase,
        X = x,
        Y = y,
        Pressure = 0.5f,
        Timestamp = t,
        Buttons = phase is BinkPointerPhase.Up or BinkPointerPhase.Cancel ? 0 : 1,
    };

    [TestMethod]
    public void A_stroke_with_many_thousands_of_points_completes()
    {
        using var context = new TestToolContext();
        var tool = new BinkDrawTool();
        tool.Activate(context);
        var random = new Random(3);
        var points = new (float, float, float)[20000];
        for (int i = 0; i < points.Length; i++)
        {
            float t = i / (float)points.Length;
            points[i] = (100 + t * 600 + random.NextSingle() * 3, 300 + MathF.Sin(t * 60) * 120, 0.2f + random.NextSingle() * 0.8f);
        }
        context.Gesture(tool, BinkPointerKind.Pen, points);
        BinkStroke s = Assert.IsExactInstanceOfType<BinkStroke>(Assert.ContainsSingle(context.Document.AllElements));
        output.WriteLine($"stored {s.Points.Count} of {points.Length} samples");
        Assert.IsTrue(s.Points.Count > 100);
        Assert.IsFalse(s.Geometry.IsEmpty);
    }

    [TestMethod]
    public void Interleaved_pointers_do_not_corrupt_a_stroke()
    {
        using var context = new TestToolContext();
        var tool = new BinkDrawTool();
        tool.Activate(context);

        tool.OnPointerDown(context, Sample(1, BinkPointerPhase.Down, 100, 100, 0));
        for (int i = 1; i <= 20; i++)
        {
            tool.OnPointerMove(context, Sample(1, BinkPointerPhase.Move, 100 + i * 5, 100, i * 8));
            // A second, uninvited pointer must not contribute points to the first stroke.
            tool.OnPointerMove(context, Sample(2, BinkPointerPhase.Move, 500, 500, i * 8));
        }
        tool.OnPointerUp(context, Sample(1, BinkPointerPhase.Up, 200, 100, 200));

        BinkStroke s = Assert.IsExactInstanceOfType<BinkStroke>(Assert.ContainsSingle(context.Document.AllElements));
        output.WriteLine($"bounds {s.Bounds}");
        Assert.IsTrue(s.Bounds.Right < 400, $"the second pointer leaked into the stroke: {s.Bounds}");
    }

    [TestMethod]
    public void A_cancel_for_another_pointer_does_not_kill_the_active_stroke()
    {
        // A resting palm, a contact the browser rejects, or a stray pointer leaving the element all raise a
        // cancel of their own. Acting on those would throw away the stroke the user is still drawing.
        using var context = new TestToolContext();
        var tool = new BinkDrawTool();
        tool.Activate(context);

        tool.OnPointerDown(context, Sample(1, BinkPointerPhase.Down, 100, 100, 0));
        for (int i = 1; i <= 10; i++) tool.OnPointerMove(context, Sample(1, BinkPointerPhase.Move, 100 + i * 8, 100, i * 8));
        tool.OnPointerCancel(context, Sample(9, BinkPointerPhase.Cancel, 0, 0, 90));
        tool.OnPointerUp(context, Sample(1, BinkPointerPhase.Up, 180, 100, 100));
        output.WriteLine($"elements: {context.Document.ElementCount}");
        Assert.AreEqual(1, context.Document.ElementCount);
    }

    [TestMethod]
    [DataRow("draw", 1)]
    [DataRow("erase", -1)]
    [DataRow("shape", 1)]
    public void A_cancel_from_an_unrelated_pointer_is_ignored_by_every_tool(string toolId, int expectedDelta)
    {
        using var context = new TestToolContext();
        context.Document.AddElement(TestData.Stroke(new Vector2(80, 80), new Vector2(300, 300), count: 40));
        int before = context.Document.ElementCount;
        IBinkTool tool = toolId switch
        {
            "erase" => new BinkEraserTool { Mode = BinkEraserMode.Stroke, Radius = 12 },
            "shape" => new BinkShapeTool(BinkShapeKind.Rectangle),
            _ => new BinkDrawTool(),
        };
        tool.Activate(context);

        Assert.IsTrue(tool.OnPointerDown(context, Sample(1, BinkPointerPhase.Down, 90, 90, 0)));
        for (int i = 1; i <= 6; i++) tool.OnPointerMove(context, Sample(1, BinkPointerPhase.Move, 90 + i * 20, 90 + i * 20, i * 10));

        tool.OnPointerCancel(context, Sample(77, BinkPointerPhase.Cancel, 0, 0, 70));
        tool.OnPointerUp(context, Sample(1, BinkPointerPhase.Up, 240, 240, 100));

        // The gesture was still live when the pointer lifted, so it did what the user asked for.
        output.WriteLine($"{toolId}: {before} -> {context.Document.ElementCount} elements");
        Assert.AreEqual(before + expectedDelta, context.Document.ElementCount);
    }

    [TestMethod]
    public void A_cancel_from_an_unrelated_pointer_does_not_abandon_a_drag()
    {
        using var context = new TestToolContext();
        // Filled, so a press in the middle of it is a hit: an unfilled shape is only grabbable by its outline.
        var target = new BinkRectangleShape(new BinkRect(100, 100, 120, 80))
        {
            Style = BinkShapeStyle.Default with { FillColor = BinkColor.Parse("#dbeafe") },
        };
        context.Document.AddElement(target);
        var tool = new BinkSelectTool();
        tool.Activate(context);

        Assert.IsTrue(tool.OnPointerDown(context, Sample(1, BinkPointerPhase.Down, 160, 140, 0)));
        for (int i = 1; i <= 6; i++) tool.OnPointerMove(context, Sample(1, BinkPointerPhase.Move, 160 + i * 10, 140 + i * 5, i * 10));
        tool.OnPointerCancel(context, Sample(77, BinkPointerPhase.Cancel, 0, 0, 70));
        tool.OnPointerUp(context, Sample(1, BinkPointerPhase.Up, 220, 170, 100));

        BinkRect moved = context.Document.GetElement(target.Id).Bounds;
        output.WriteLine($"moved to {moved}");
        Assert.IsTrue(moved.X > 140f, $"the drag was abandoned by an unrelated cancel: {moved}");
    }
}
