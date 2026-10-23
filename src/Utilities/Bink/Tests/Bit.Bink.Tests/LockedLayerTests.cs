using System.Numerics;

namespace Bit.Bink.Tests;

/// <summary>
/// A locked or hidden layer refuses new elements from every tool and says why, including when it was locked or hidden
/// while a gesture was under way: the element is refused, never put on some other layer.
/// </summary>
[TestClass]
public class LockedLayerTests
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

    private static void Click(TestToolContext context, IBinkTool tool, float x, float y, double time)
    {
        tool.OnPointerDown(context, Sample(BinkPointerPhase.Down, x, y, time));
        tool.OnPointerUp(context, Sample(BinkPointerPhase.Up, x, y, time + 10));
    }

    [TestMethod]
    public void A_refused_stroke_says_why()
    {
        using var context = new TestToolContext();
        context.Document.UpdateLayer(context.ActiveLayerId, p => p with { Name = "Ink", IsLocked = true });

        context.Line(new BinkDrawTool(), new Vector2(0, 0), new Vector2(100, 100));

        Assert.AreEqual(0, context.Document.ElementCount);
        Assert.Contains("The layer \"Ink\" is locked, so nothing can be added to it.", context.Announcements);
    }

    [TestMethod]
    public void A_hidden_layer_refuses_ink_and_shapes_nobody_could_see()
    {
        using var context = new TestToolContext();
        context.Document.UpdateLayer(context.ActiveLayerId, p => p with { IsVisible = false });

        context.Line(new BinkDrawTool(), new Vector2(0, 0), new Vector2(100, 100));
        context.Line(new BinkShapeTool(BinkShapeKind.Rectangle), new Vector2(0, 0), new Vector2(100, 100));

        Assert.AreEqual(0, context.Document.ElementCount);
        Assert.HasCount(2, context.Announcements);
        Assert.IsTrue(context.Announcements.All(a => a.Contains("is hidden", StringComparison.Ordinal)), string.Join(" | ", context.Announcements));
    }

    [TestMethod]
    public void A_stroke_whose_layer_was_locked_while_the_pen_was_down_is_refused_rather_than_moved()
    {
        using var context = new TestToolContext();
        Guid started = context.ActiveLayerId;
        Guid other = context.Document.AddLayer("Other").Id;
        var draw = new BinkDrawTool();
        draw.OnPointerDown(context, Sample(BinkPointerPhase.Down, 10, 10, 0));
        draw.OnPointerMove(context, Sample(BinkPointerPhase.Move, 60, 40, 8));

        // A collaborator, or the application, locks the layer and picks another while the pen is still down.
        context.Document.UpdateLayer(started, p => p with { IsLocked = true });
        context.ActiveLayerId = other;
        draw.OnPointerUp(context, Sample(BinkPointerPhase.Up, 100, 70, 16));

        Assert.AreEqual(0, context.Document.ElementCount, "the stroke was moved to a layer it was not drawn on");
        Assert.ContainsSingle(a => a.Contains("is locked", StringComparison.Ordinal), context.Announcements);
    }

    [TestMethod]
    public void A_stroke_whose_layer_was_deleted_while_the_pen_was_down_goes_to_the_active_layer()
    {
        using var context = new TestToolContext();
        Guid started = context.ActiveLayerId;
        Guid other = context.Document.AddLayer("Other").Id;
        var draw = new BinkDrawTool();
        draw.OnPointerDown(context, Sample(BinkPointerPhase.Down, 10, 10, 0));
        draw.OnPointerMove(context, Sample(BinkPointerPhase.Move, 60, 40, 8));

        context.Document.RemoveLayer(started);
        context.ActiveLayerId = other;
        draw.OnPointerUp(context, Sample(BinkPointerPhase.Up, 100, 70, 16));

        Assert.ContainsSingle(context.Document.GetLayer(other).Elements);
    }

    [TestMethod]
    public void A_shape_whose_layer_was_locked_during_the_drag_is_refused()
    {
        using var context = new TestToolContext();
        var shape = new BinkShapeTool(BinkShapeKind.Rectangle);
        shape.OnPointerDown(context, Sample(BinkPointerPhase.Down, 10, 10, 0));
        shape.OnPointerMove(context, Sample(BinkPointerPhase.Move, 60, 40, 8));

        context.Document.UpdateLayer(context.ActiveLayerId, p => p with { IsLocked = true });
        shape.OnPointerUp(context, Sample(BinkPointerPhase.Up, 100, 70, 16));

        Assert.AreEqual(0, context.Document.ElementCount);
        Assert.ContainsSingle(a => a.Contains("is locked", StringComparison.Ordinal), context.Announcements);
    }

    [TestMethod]
    public void A_polyline_whose_layer_was_locked_before_it_was_finished_is_refused()
    {
        using var context = new TestToolContext();
        var tool = new BinkPolylineTool();
        Click(context, tool, 100, 100, 1000);
        Click(context, tool, 200, 100, 2000);

        context.Document.UpdateLayer(context.ActiveLayerId, p => p with { IsLocked = true });

        Assert.IsNull(tool.Finish(context, closed: false));
        Assert.AreEqual(0, context.Document.ElementCount);
        Assert.ContainsSingle(a => a.Contains("is locked", StringComparison.Ordinal), context.Announcements);
    }
}
