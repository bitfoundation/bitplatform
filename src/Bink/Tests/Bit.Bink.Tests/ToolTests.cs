using System.Numerics;

namespace Bit.Bink.Tests;

[TestClass]
public class DrawToolTests
{
    [TestMethod]
    public void A_gesture_commits_exactly_one_stroke()
    {
        using var context = new TestToolContext();
        var tool = new BinkDrawTool();
        context.Line(tool, new Vector2(10, 10), new Vector2(200, 120), 30);

        Assert.AreEqual(1, context.Document.ElementCount);
        Assert.IsExactInstanceOfType<BinkStroke>(context.Document.Layers[0].Elements[0]);
    }

    [TestMethod]
    public void A_committed_stroke_is_recorded_in_history()
    {
        using var context = new TestToolContext();
        var tool = new BinkDrawTool();
        context.Line(tool, new Vector2(10, 10), new Vector2(200, 120), 30);

        Assert.IsTrue(context.History.CanUndo);
        Assert.AreEqual(1, context.History.UndoCount);

        context.History.Undo();
        Assert.AreEqual(0, context.Document.ElementCount);
    }

    [TestMethod]
    public void The_stroke_lands_on_the_active_layer()
    {
        using var context = new TestToolContext();
        BinkLayer second = context.Document.AddLayer("Second");
        context.ActiveLayerId = second.Id;

        context.Line(new BinkDrawTool(), new Vector2(0, 0), new Vector2(100, 100), 20);

        Assert.IsEmpty(context.Document.Layers[0].Elements);
        Assert.ContainsSingle(second.Elements);
    }

    [TestMethod]
    public void Drawing_is_refused_on_a_locked_layer()
    {
        using var context = new TestToolContext();
        context.Document.UpdateLayer(context.ActiveLayerId, p => p with { IsLocked = true });

        context.Line(new BinkDrawTool(), new Vector2(0, 0), new Vector2(100, 100), 20);
        Assert.AreEqual(0, context.Document.ElementCount);
    }

    [TestMethod]
    public void A_single_tap_still_leaves_a_mark()
    {
        using var context = new TestToolContext();
        context.Gesture(new BinkDrawTool(), BinkPointerKind.Mouse, (50, 50, 0.5f), (50, 50, 0.5f));

        Assert.AreEqual(1, context.Document.ElementCount);
        var stroke = (BinkStroke)context.Document.Layers[0].Elements[0];
        Assert.IsFalse(stroke.Geometry.IsEmpty);
    }

    [TestMethod]
    public void Escape_cancels_the_stroke_in_progress()
    {
        using var context = new TestToolContext();
        var tool = new BinkDrawTool();
        tool.OnPointerDown(context, new BinkInputSample { PointerId = 1, Phase = BinkPointerPhase.Down, X = 10, Y = 10, Pressure = 0.5f, Buttons = 1 });
        tool.OnPointerMove(context, new BinkInputSample { PointerId = 1, Phase = BinkPointerPhase.Move, X = 60, Y = 60, Pressure = 0.5f, Buttons = 1, Timestamp = 16 });
        Assert.IsTrue(tool.IsDrawing);

        Assert.IsTrue(tool.OnKeyDown(context, new BinkToolKeyEvent("Escape", false, false, false)));
        Assert.IsFalse(tool.IsDrawing);
        Assert.AreEqual(0, context.Document.ElementCount);
    }

    [TestMethod]
    public void The_view_transform_is_applied_so_strokes_land_in_document_space()
    {
        using var context = new TestToolContext { Viewport = new BinkViewport(2f, new Vector2(100, 50)) };
        context.Line(new BinkDrawTool(), new Vector2(100, 50), new Vector2(300, 250), 20);

        var stroke = (BinkStroke)context.Document.Layers[0].Elements[0];
        Assert.AreEqual(0f, stroke.Points.X[0], 1f);
        Assert.AreEqual(0f, stroke.Points.Y[0], 1f);
        Assert.AreEqual(100f, stroke.Points.X[^1], 2f);
    }

    [TestMethod]
    public void The_pen_eraser_button_erases_instead_of_drawing()
    {
        using var context = new TestToolContext();
        var tool = new BinkDrawTool();
        // Pen barrel button 32 is the eraser end of a stylus.
        tool.OnPointerDown(context, new BinkInputSample { PointerId = 1, Kind = BinkPointerKind.Pen, Phase = BinkPointerPhase.Down, X = 10, Y = 10, Pressure = 0.5f, Buttons = 32 });
        tool.OnPointerMove(context, new BinkInputSample { PointerId = 1, Kind = BinkPointerKind.Pen, Phase = BinkPointerPhase.Move, X = 80, Y = 40, Pressure = 0.5f, Buttons = 32, Timestamp = 16 });
        tool.OnPointerUp(context, new BinkInputSample { PointerId = 1, Kind = BinkPointerKind.Pen, Phase = BinkPointerPhase.Up, X = 120, Y = 60, Pressure = 0f, Timestamp = 32 });

        var stroke = (BinkStroke)context.Document.Layers[0].Elements[0];
        Assert.IsTrue(stroke.IsEraser);
    }
}

[TestClass]
public class EraserToolTests
{
    private static TestToolContext WithStrokes(int count = 3)
    {
        var context = new TestToolContext();
        for (int i = 0; i < count; i++)
        {
            context.Document.AddElement(new BinkStroke(TestData.Line(new Vector2(0, 20 + i * 40), new Vector2(400, 20 + i * 40), 60), new BinkPenBrush { Width = 4f }));
        }
        // The fixture's own setup is not user history.
        context.History.Clear();
        return context;
    }

    [TestMethod]
    public void Stroke_mode_deletes_the_whole_stroke()
    {
        using TestToolContext context = WithStrokes();
        var tool = new BinkEraserTool { Mode = BinkEraserMode.Stroke, Radius = 10f };
        context.Line(tool, new Vector2(200, 20), new Vector2(210, 20), 4);

        Assert.AreEqual(2, context.Document.ElementCount);
    }

    [TestMethod]
    public void Segment_mode_splits_a_stroke_and_keeps_the_rest()
    {
        using TestToolContext context = WithStrokes(1);
        var tool = new BinkEraserTool { Mode = BinkEraserMode.Segment, Radius = 12f };
        context.Line(tool, new Vector2(200, 20), new Vector2(205, 20), 3);

        Assert.AreEqual(2, context.Document.ElementCount);
        BinkRect first = context.Document.Layers[0].Elements[0].Bounds;
        BinkRect second = context.Document.Layers[0].Elements[1].Bounds;
        Assert.IsTrue(first.Right < second.Left, "the two fragments should sit on either side of the erased gap");
    }

    [TestMethod]
    public void Pixel_mode_adds_an_eraser_stroke_instead_of_deleting()
    {
        using TestToolContext context = WithStrokes(1);
        var tool = new BinkEraserTool { Mode = BinkEraserMode.Pixel, Radius = 12f };
        context.Line(tool, new Vector2(100, 20), new Vector2(300, 20), 20);

        Assert.AreEqual(2, context.Document.ElementCount);
        Assert.IsTrue(((BinkStroke)context.Document.Layers[0].Elements[1]).IsEraser);
    }

    [TestMethod]
    public void A_whole_erase_gesture_is_a_single_undo_step()
    {
        using TestToolContext context = WithStrokes();
        var tool = new BinkEraserTool { Mode = BinkEraserMode.Stroke, Radius = 30f };
        context.Line(tool, new Vector2(0, 20), new Vector2(400, 140), 40);

        Assert.IsTrue(context.Document.ElementCount < 3);
        Assert.AreEqual(1, context.History.UndoCount);

        context.History.Undo();
        Assert.AreEqual(3, context.Document.ElementCount);
    }

    [TestMethod]
    public void Erasing_nothing_creates_no_history_entry()
    {
        using TestToolContext context = WithStrokes();
        var tool = new BinkEraserTool { Mode = BinkEraserMode.Stroke, Radius = 5f };
        context.Line(tool, new Vector2(0, 300), new Vector2(400, 320), 20);

        Assert.AreEqual(3, context.Document.ElementCount);
        Assert.AreEqual(0, context.History.UndoCount);
    }

    [TestMethod]
    public void A_fast_drag_does_not_skip_over_thin_strokes()
    {
        using var context = new TestToolContext();
        // Vertical hairlines, one every 20 units.
        for (int i = 0; i < 10; i++)
        {
            context.Document.AddElement(new BinkStroke(TestData.Line(new Vector2(20 + i * 20, 0), new Vector2(20 + i * 20, 200), 30), new BinkPenBrush { Width = 1f }));
        }

        // Two samples 200 units apart, as a fast flick would produce.
        var tool = new BinkEraserTool { Mode = BinkEraserMode.Stroke, Radius = 6f };
        context.Gesture(tool, BinkPointerKind.Mouse, (10, 100, 0.5f), (210, 100, 0.5f), (210, 100, 0.5f));

        Assert.IsTrue(context.Document.ElementCount <= 1, $"the sweep should have erased nearly every line but {context.Document.ElementCount} remain");
    }
}

[TestClass]
public class SelectToolTests
{
    private static TestToolContext WithShape(out BinkRectangleShape shape)
    {
        var context = new TestToolContext();
        shape = new BinkRectangleShape(new BinkRect(100, 100, 100, 60)) { Style = new BinkShapeStyle { FillColor = BinkColor.White, StrokeWidth = 2f } };
        context.Document.AddElement(shape);
        context.History.Clear();
        return context;
    }

    [TestMethod]
    public void Clicking_an_element_selects_it()
    {
        using TestToolContext context = WithShape(out BinkRectangleShape shape);
        var tool = new BinkSelectTool();
        context.Gesture(tool, BinkPointerKind.Mouse, (150, 130, 0.5f), (150, 130, 0.5f));

        Assert.AreEqual(1, context.Selection.Count);
        Assert.AreEqual(shape.Id, context.Selection.Ids[0]);
    }

    [TestMethod]
    public void Clicking_empty_space_clears_the_selection()
    {
        using TestToolContext context = WithShape(out BinkRectangleShape shape);
        var tool = new BinkSelectTool();
        context.Selection.Set(shape.Id);

        context.Gesture(tool, BinkPointerKind.Mouse, (500, 400, 0.5f), (500, 400, 0.5f));
        Assert.IsTrue(context.Selection.IsEmpty);
    }

    [TestMethod]
    public void Dragging_a_selected_element_moves_it_as_one_undo_step()
    {
        using TestToolContext context = WithShape(out BinkRectangleShape shape);
        var tool = new BinkSelectTool();
        context.Selection.Set(shape.Id);

        context.Line(tool, new Vector2(150, 130), new Vector2(250, 190), 20);

        BinkElement moved = context.Document.GetElement(shape.Id);
        Assert.AreEqual(200f, moved.Bounds.X, 3f);
        Assert.AreEqual(160f, moved.Bounds.Y, 3f);
        Assert.AreEqual(1, context.History.UndoCount);

        context.History.Undo();
        Assert.AreEqual(99f, context.Document.GetElement(shape.Id).Bounds.X, 2f);
    }

    [TestMethod]
    public void A_marquee_selects_everything_it_covers()
    {
        using var context = new TestToolContext();
        for (int i = 0; i < 4; i++)
        {
            context.Document.AddElement(new BinkRectangleShape(new BinkRect(i * 100, 50, 50, 50)));
        }

        var tool = new BinkSelectTool();
        context.Line(tool, new Vector2(0, 0), new Vector2(260, 200), 20);

        Assert.AreEqual(3, context.Selection.Count);
    }

    [TestMethod]
    public void Arrow_keys_nudge_the_selection()
    {
        using TestToolContext context = WithShape(out BinkRectangleShape shape);
        var tool = new BinkSelectTool();
        context.Selection.Set(shape.Id);

        Assert.IsTrue(tool.OnKeyDown(context, new BinkToolKeyEvent("ArrowRight", false, false, false)));
        Assert.AreEqual(100f, context.Document.GetElement(shape.Id).Bounds.X, 1f);

        Assert.IsTrue(tool.OnKeyDown(context, new BinkToolKeyEvent("ArrowRight", false, true, false)));
        Assert.AreEqual(110f, context.Document.GetElement(shape.Id).Bounds.X, 1f);
    }

    [TestMethod]
    public void Delete_removes_the_selection()
    {
        using TestToolContext context = WithShape(out BinkRectangleShape shape);
        var tool = new BinkSelectTool();
        context.Selection.Set(shape.Id);

        Assert.IsTrue(tool.OnKeyDown(context, new BinkToolKeyEvent("Delete", false, false, false)));
        Assert.AreEqual(0, context.Document.ElementCount);
    }

    [TestMethod]
    public void Resizing_from_a_corner_scales_the_element()
    {
        using TestToolContext context = WithShape(out BinkRectangleShape shape);
        var tool = new BinkSelectTool();
        context.Selection.Set(shape.Id);

        // The selection box is inflated by the stroke width, so the corner handle sits slightly outside.
        BinkRect bounds = context.Selection.Bounds;
        var corner = new Vector2(bounds.Right, bounds.Bottom);
        context.Line(tool, corner, corner + new Vector2(100, 60), 20);

        BinkRect after = context.Document.GetElement(shape.Id).Bounds;
        Assert.IsTrue(after.Width > bounds.Width * 1.5f, $"expected the element to grow from {bounds.Width} but it is {after.Width}");
    }

    [TestMethod]
    public void A_cancelled_drag_restores_the_original_position()
    {
        using TestToolContext context = WithShape(out BinkRectangleShape shape);
        var tool = new BinkSelectTool();
        context.Selection.Set(shape.Id);
        float originalX = context.Document.GetElement(shape.Id).Bounds.X;

        tool.OnPointerDown(context, new BinkInputSample { PointerId = 1, Phase = BinkPointerPhase.Down, X = 150, Y = 130, Buttons = 1 });
        tool.OnPointerMove(context, new BinkInputSample { PointerId = 1, Phase = BinkPointerPhase.Move, X = 300, Y = 300, Buttons = 1, Timestamp = 16 });
        tool.OnPointerCancel(context, new BinkInputSample { PointerId = 1, Phase = BinkPointerPhase.Cancel, X = 300, Y = 300, Timestamp = 32 });

        Assert.AreEqual(originalX, context.Document.GetElement(shape.Id).Bounds.X, 1f);
    }
}

[TestClass]
public class ShapeToolTests
{
    [TestMethod]
    public void Dragging_creates_a_rectangle()
    {
        using var context = new TestToolContext();
        context.Line(new BinkShapeTool(BinkShapeKind.Rectangle), new Vector2(50, 50), new Vector2(200, 150), 10);

        var shape = Assert.IsExactInstanceOfType<BinkRectangleShape>(context.Document.Layers[0].Elements[0]);
        Assert.AreEqual(new BinkRect(50, 50, 150, 100), shape.Rect);
    }

    [TestMethod]
    public void Dragging_creates_an_arrow_with_a_head()
    {
        using var context = new TestToolContext();
        context.Line(new BinkShapeTool(BinkShapeKind.Arrow), new Vector2(10, 10), new Vector2(200, 90), 10);

        var line = Assert.IsExactInstanceOfType<BinkLineShape>(context.Document.Layers[0].Elements[0]);
        Assert.IsTrue(line.EndArrow);
    }

    [TestMethod]
    public void A_click_without_a_drag_creates_nothing()
    {
        using var context = new TestToolContext();
        context.Gesture(new BinkShapeTool(BinkShapeKind.Ellipse), BinkPointerKind.Mouse, (50, 50, 0.5f), (50, 50, 0.5f));
        Assert.AreEqual(0, context.Document.ElementCount);
    }

    [TestMethod]
    public void The_shape_uses_the_context_style()
    {
        using var context = new TestToolContext { ShapeStyle = new BinkShapeStyle { StrokeColor = BinkColor.Parse("#ff0000"), StrokeWidth = 5f } };
        context.Line(new BinkShapeTool(BinkShapeKind.Rectangle), new Vector2(0, 0), new Vector2(100, 100), 10);

        var shape = (BinkRectangleShape)context.Document.Layers[0].Elements[0];
        Assert.AreEqual(BinkColor.Parse("#ff0000"), shape.Style.StrokeColor);
        Assert.AreEqual(5f, shape.Style.StrokeWidth);
    }
}

[TestClass]
public class PanToolTests
{
    [TestMethod]
    public void Dragging_pans_the_viewport_without_changing_the_document()
    {
        using var context = new TestToolContext();
        context.Document.AddElement(TestData.Stroke(new Vector2(0, 0), new Vector2(50, 50)));
        context.History.Clear();
        long revision = context.Document.Revision;

        context.Line(new BinkPanTool(), new Vector2(100, 100), new Vector2(160, 130), 10);

        Assert.AreEqual(60f, context.Viewport.Offset.X, 1f);
        Assert.AreEqual(30f, context.Viewport.Offset.Y, 1f);
        Assert.AreEqual(revision, context.Document.Revision);
        Assert.AreEqual(0, context.History.UndoCount);
    }

    [TestMethod]
    public void Two_pointers_pinch_to_zoom()
    {
        using var context = new TestToolContext();
        var tool = new BinkPanTool();

        tool.OnPointerDown(context, new BinkInputSample { PointerId = 1, Kind = BinkPointerKind.Touch, Phase = BinkPointerPhase.Down, X = 200, Y = 200, Buttons = 1 });
        tool.OnPointerDown(context, new BinkInputSample { PointerId = 2, Kind = BinkPointerKind.Touch, Phase = BinkPointerPhase.Down, X = 300, Y = 200, Buttons = 1 });
        // Spread the fingers apart around the same centre.
        tool.OnPointerMove(context, new BinkInputSample { PointerId = 1, Kind = BinkPointerKind.Touch, Phase = BinkPointerPhase.Move, X = 150, Y = 200, Buttons = 1, Timestamp = 16 });
        tool.OnPointerMove(context, new BinkInputSample { PointerId = 2, Kind = BinkPointerKind.Touch, Phase = BinkPointerPhase.Move, X = 350, Y = 200, Buttons = 1, Timestamp = 16 });

        Assert.IsTrue(context.Viewport.Zoom > 1.5f, $"pinching apart should zoom in but zoom is {context.Viewport.Zoom}");
    }
}

[TestClass]
public class TextToolTests
{
    [TestMethod]
    public void Clicking_creates_a_text_element_and_asks_for_an_editor()
    {
        using var context = new TestToolContext();
        var tool = new BinkTextTool();
        BinkTextEditRequest? request = null;
        tool.EditRequested += r => request = r;

        tool.OnPointerDown(context, new BinkInputSample { PointerId = 1, Phase = BinkPointerPhase.Down, X = 40, Y = 60, Buttons = 1 });

        Assert.IsNotNull(request);
        Assert.AreEqual(1, context.Document.ElementCount);
        Assert.IsNotNull(tool.Editing);
    }

    [TestMethod]
    public void Committing_empty_text_removes_the_element()
    {
        using var context = new TestToolContext();
        var tool = new BinkTextTool();
        tool.OnPointerDown(context, new BinkInputSample { PointerId = 1, Phase = BinkPointerPhase.Down, X = 40, Y = 60, Buttons = 1 });

        tool.CommitEdit(context, "   ");
        Assert.AreEqual(0, context.Document.ElementCount);
    }

    [TestMethod]
    public void Committing_text_updates_the_element()
    {
        using var context = new TestToolContext();
        var tool = new BinkTextTool();
        tool.OnPointerDown(context, new BinkInputSample { PointerId = 1, Phase = BinkPointerPhase.Down, X = 40, Y = 60, Buttons = 1 });

        tool.CommitEdit(context, "Hello", new Vector2(60, 20));
        var text = (BinkTextElement)context.Document.Layers[0].Elements[0];
        Assert.AreEqual("Hello", text.Text);
        Assert.AreEqual(new Vector2(60, 20), text.Size);
    }

    [TestMethod]
    public void Clicking_existing_text_edits_it_rather_than_creating_another()
    {
        using var context = new TestToolContext();
        context.Document.AddElement(new BinkTextElement(new Vector2(40, 60), "Existing") { Size = new Vector2(120, 20) });
        var tool = new BinkTextTool();

        tool.OnPointerDown(context, new BinkInputSample { PointerId = 1, Phase = BinkPointerPhase.Down, X = 60, Y = 66, Buttons = 1 });

        Assert.AreEqual(1, context.Document.ElementCount);
        Assert.AreEqual("Existing", tool.Editing!.Text);
    }
}
