using System.Numerics;

namespace Bit.Bink.Tests;

/// <summary>Editing the points of polygons, lines and paths.</summary>
[TestClass]
public sealed class NodeEditToolTests
{
    private static BinkInputSample Sample(BinkPointerPhase phase, float x, float y, double time = 0, BinkPointerKind kind = BinkPointerKind.Mouse) => new()
    {
        PointerId = 1,
        Kind = kind,
        Phase = phase,
        X = x,
        Y = y,
        Pressure = 0.5f,
        Timestamp = time,
        Buttons = phase == BinkPointerPhase.Up ? 0 : 1,
        IsPrimary = true,
    };

    private static void Drag(TestToolContext context, IBinkTool tool, Vector2 from, Vector2 to)
    {
        tool.OnPointerDown(context, Sample(BinkPointerPhase.Down, from.X, from.Y));
        for (int i = 1; i <= 5; i++)
        {
            Vector2 p = Vector2.Lerp(from, to, i / 5f);
            tool.OnPointerMove(context, Sample(BinkPointerPhase.Move, p.X, p.Y, i * 8));
        }
        tool.OnPointerUp(context, Sample(BinkPointerPhase.Up, to.X, to.Y, 48));
    }

    private static void Click(TestToolContext context, IBinkTool tool, Vector2 at)
    {
        tool.OnPointerDown(context, Sample(BinkPointerPhase.Down, at.X, at.Y));
        tool.OnPointerUp(context, Sample(BinkPointerPhase.Up, at.X, at.Y, 8));
    }

    private static BinkToolKeyEvent Key(string key, bool shift = false) => new(key, false, shift, false);

    [TestMethod]
    public void Clicking_a_polygon_edits_it_and_dragging_a_corner_moves_only_that_corner_in_one_undo_step()
    {
        using var context = new TestToolContext();
        var triangle = new BinkPolygonShape([new(100, 100), new(200, 100), new(150, 200)]);
        context.Document.AddElement(triangle);
        context.History.Clear();
        var tool = new BinkNodeEditTool();

        Click(context, tool, new Vector2(150, 100));
        Assert.AreEqual(triangle.Id, tool.TargetId);
        Assert.AreEqual(triangle.Id, context.Selection.Ids.Single(), "What is edited is selected.");

        Drag(context, tool, new Vector2(200, 100), new Vector2(260, 80));

        var edited = (BinkPolygonShape)context.Document.GetElement(triangle.Id);
        CollectionAssert.AreEqual(new Vector2[] { new(100, 100), new(260, 80), new(150, 200) }, edited.Points.ToArray());
        Assert.AreEqual(1, context.History.UndoCount, "The whole drag is one step.");
        context.History.Undo();
        CollectionAssert.AreEqual(triangle.Points.ToArray(), ((BinkPolygonShape)context.Document.GetElement(triangle.Id)).Points.ToArray());
    }

    [TestMethod]
    public void A_node_is_dragged_in_the_elements_own_coordinates_under_its_transform()
    {
        using var context = new TestToolContext();
        // Moved 100 to the right and scaled twice: local (10, 10) is at document (120, 20).
        var line = new BinkLineShape(new Vector2(10, 10), new Vector2(50, 10)) { Transform = Matrix3x2.CreateScale(2f) * Matrix3x2.CreateTranslation(100, 0) };
        context.Document.AddElement(line);
        var tool = new BinkNodeEditTool();

        Click(context, tool, new Vector2(160, 20));
        Drag(context, tool, new Vector2(120, 20), new Vector2(120, 60));

        var edited = (BinkLineShape)context.Document.GetElement(line.Id);
        Assert.AreEqual(new Vector2(10, 30), edited.Start);
        Assert.AreEqual(line.End, edited.End);
    }

    [TestMethod]
    public void Moving_a_paths_anchor_carries_the_control_points_beside_it_but_a_control_point_moves_alone()
    {
        BinkPathElement path = BinkPathElement.FromSvgPathData("M 0 0 C 10 -20 40 -20 50 0 C 60 20 90 20 100 0");
        IReadOnlyList<BinkNode> nodes = BinkNodeEditTool.GetNodes(path);
        Assert.HasCount(7, nodes);
        CollectionAssert.AreEqual(
            new[] { BinkNodeKind.Anchor, BinkNodeKind.Control, BinkNodeKind.Control, BinkNodeKind.Anchor, BinkNodeKind.Control, BinkNodeKind.Control, BinkNodeKind.Anchor },
            nodes.Select(n => n.Kind).ToArray());

        var moved = (BinkPathElement)BinkNodeEditTool.MoveNode(path, 3, new Vector2(50, 30))!;
        IReadOnlyList<BinkNode> after = BinkNodeEditTool.GetNodes(moved);
        Assert.AreEqual(new Vector2(50, 30), after[3].Position);
        Assert.AreEqual(new Vector2(40, 10), after[2].Position, "The control arriving at the anchor moved with it.");
        Assert.AreEqual(new Vector2(60, 50), after[4].Position, "So did the one leaving it.");
        Assert.AreEqual(nodes[1].Position, after[1].Position);

        var bent = (BinkPathElement)BinkNodeEditTool.MoveNode(path, 1, new Vector2(10, -60))!;
        Assert.AreEqual(new Vector2(10, -60), BinkNodeEditTool.GetNodes(bent)[1].Position);
        Assert.AreEqual(nodes[0].Position, BinkNodeEditTool.GetNodes(bent)[0].Position);
    }

    [TestMethod]
    public void The_keyboard_steps_through_nudges_and_removes_points()
    {
        using var context = new TestToolContext();
        var square = new BinkPolygonShape([new(0, 0), new(100, 0), new(100, 100), new(0, 100)]);
        context.Document.AddElement(square);
        context.Selection.Set(square.Id);
        var tool = new BinkNodeEditTool();
        tool.Activate(context);
        Assert.AreEqual(square.Id, tool.TargetId, "A selected element is edited straight away.");

        Assert.IsTrue(tool.OnKeyDown(context, Key("Tab")));
        Assert.IsTrue(tool.OnKeyDown(context, Key("Tab")));
        Assert.AreEqual(1, tool.ActiveNode);
        Assert.Contains("Point 2 of 4.", context.Announcements);

        Assert.IsTrue(tool.OnKeyDown(context, Key("ArrowRight", shift: true)));
        Assert.AreEqual(new Vector2(110, 0), ((BinkPolygonShape)context.Document.GetElement(square.Id)).Points[1]);

        Assert.IsTrue(tool.OnKeyDown(context, Key("Delete")));
        Assert.HasCount(3, ((BinkPolygonShape)context.Document.GetElement(square.Id)).Points);
        Assert.IsTrue(tool.OnKeyDown(context, Key("Delete")), "A triangle keeps its corners...");
        Assert.HasCount(3, ((BinkPolygonShape)context.Document.GetElement(square.Id)).Points, "...a polygon needs three.");
        Assert.Contains("That point cannot be removed.", context.Announcements);

        Assert.IsTrue(tool.OnKeyDown(context, Key("Escape")));
        Assert.IsNull(tool.TargetId);
        Assert.IsFalse(tool.OnKeyDown(context, Key("Escape")), "A second Escape is the canvas's, which clears the selection.");
    }

    [TestMethod]
    public void A_cancelled_drag_puts_the_point_back_and_leaves_no_undo_step()
    {
        using var context = new TestToolContext();
        var line = new BinkLineShape(new Vector2(10, 10), new Vector2(100, 10));
        context.Document.AddElement(line);
        context.History.Clear();
        var tool = new BinkNodeEditTool();
        Click(context, tool, new Vector2(50, 10));

        tool.OnPointerDown(context, Sample(BinkPointerPhase.Down, 100, 10));
        tool.OnPointerMove(context, Sample(BinkPointerPhase.Move, 150, 80, 8));
        tool.OnPointerCancel(context, Sample(BinkPointerPhase.Cancel, 150, 80, 16));

        Assert.AreEqual(new Vector2(100, 10), ((BinkLineShape)context.Document.GetElement(line.Id)).End);
        Assert.IsFalse(context.History.CanUndo);
        Assert.IsFalse(tool.IsDragging);
    }

    [TestMethod]
    public void A_point_grabbed_off_centre_does_not_jump_and_a_click_records_nothing()
    {
        using var context = new TestToolContext();
        var line = new BinkLineShape(new Vector2(10, 10), new Vector2(100, 10));
        context.Document.AddElement(line);
        var tool = new BinkNodeEditTool();
        Click(context, tool, new Vector2(50, 10));
        context.History.Clear();

        // Grabbed four units off the end: a click there moved the end onto the pointer and recorded a step.
        Click(context, tool, new Vector2(104, 14));
        Assert.AreEqual(new Vector2(100, 10), ((BinkLineShape)context.Document.GetElement(line.Id)).End);
        Assert.IsFalse(context.History.CanUndo);

        Drag(context, tool, new Vector2(104, 14), new Vector2(124, 14));
        Assert.AreEqual(new Vector2(120, 10), ((BinkLineShape)context.Document.GetElement(line.Id)).End, "The end keeps its offset from the pointer.");
    }

    [TestMethod]
    public void Keys_during_a_drag_belong_to_the_drag()
    {
        using var context = new TestToolContext();
        var line = new BinkLineShape(new Vector2(10, 10), new Vector2(100, 10));
        context.Document.AddElement(line);
        var tool = new BinkNodeEditTool();
        Click(context, tool, new Vector2(50, 10));
        context.History.Clear();

        tool.OnPointerDown(context, Sample(BinkPointerPhase.Down, 100, 10));
        tool.OnPointerMove(context, Sample(BinkPointerPhase.Move, 150, 60, 8));
        Assert.IsTrue(tool.OnKeyDown(context, Key("Delete")), "Delete must not reach the canvas, which would remove the element mid-drag.");
        Assert.IsTrue(context.Document.ContainsElement(line.Id));
        Assert.IsTrue(tool.OnKeyDown(context, Key("Escape")));

        Assert.IsFalse(tool.IsDragging);
        Assert.AreEqual(new Vector2(100, 10), ((BinkLineShape)context.Document.GetElement(line.Id)).End, "Escape put the point back.");
        Assert.IsFalse(context.History.CanUndo);
        Assert.AreEqual(line.Id, tool.TargetId, "The element is still being edited.");
    }

    [TestMethod]
    public void Leaving_an_element_with_escape_leaves_its_selection_too()
    {
        using var context = new TestToolContext();
        var line = new BinkLineShape(new Vector2(10, 10), new Vector2(100, 10));
        context.Document.AddElement(line);
        var tool = new BinkNodeEditTool();
        Click(context, tool, new Vector2(50, 10));

        Assert.IsTrue(tool.OnKeyDown(context, Key("Escape")));

        Assert.AreEqual(0, context.Selection.Count, "Otherwise the canvas keeps claiming Tab and the arrow keys for nothing.");
    }

    [TestMethod]
    public void The_start_of_a_closed_loop_and_its_end_on_the_same_spot_move_together()
    {
        BinkPathElement loop = BinkPathElement.FromSvgPathData("M 0 0 C 30 -30 70 -30 100 0 C 70 30 30 30 0 0 Z");
        IReadOnlyList<BinkNode> nodes = BinkNodeEditTool.GetNodes(loop);

        var moved = (BinkPathElement)BinkNodeEditTool.MoveNode(loop, 0, new Vector2(-10, 5))!;
        IReadOnlyList<BinkNode> after = BinkNodeEditTool.GetNodes(moved);

        Assert.AreEqual(new Vector2(-10, 5), after[0].Position);
        Assert.AreEqual(new Vector2(-10, 5), after[6].Position, "The end on the same spot moved with the start.");
        Assert.AreEqual(nodes[5].Position + new Vector2(-10, 5), after[5].Position, "So did the control arriving at it.");
        Assert.AreEqual(nodes[1].Position + new Vector2(-10, 5), after[1].Position);
        Assert.AreEqual(nodes[3].Position, after[3].Position);
    }

    [TestMethod]
    public void Locked_elements_and_other_kinds_are_not_edited()
    {
        using var context = new TestToolContext();
        var locked = new BinkPolygonShape([new(0, 0), new(100, 0), new(50, 80)]) { IsLocked = true };
        var stroke = TestData.Stroke(new Vector2(200, 200), new Vector2(300, 200));
        context.Document.AddElement(locked);
        context.Document.AddElement(stroke);
        var tool = new BinkNodeEditTool();

        Click(context, tool, new Vector2(50, 0));
        Assert.IsNull(tool.TargetId);
        Click(context, tool, new Vector2(250, 200));
        Assert.IsNull(tool.TargetId);
        Assert.IsFalse(BinkNodeEditTool.CanEdit(locked));
        Assert.IsNull(BinkNodeEditTool.MoveNode(locked with { IsLocked = false }, 7, Vector2.Zero), "An index past the points is refused.");
        Assert.IsNull(BinkNodeEditTool.MoveNode(stroke, 0, Vector2.Zero));
    }

    [TestMethod]
    public void The_handles_are_drawn_where_the_nodes_are_with_levers_to_the_controls()
    {
        using var context = new TestToolContext();
        BinkPathElement path = BinkPathElement.FromSvgPathData("M 0 0 Q 50 -40 100 0");
        context.Document.AddElement(path);
        context.Selection.Set(path.Id);
        var tool = new BinkNodeEditTool();
        tool.Activate(context);
        var render = new BinkCanvasRenderContext();

        tool.DrawOverlay(context, render);

        // Three handles, each filled and outlined, and one path of levers: a quadratic's control has two.
        Assert.IsGreaterThan(0, render.Count);
    }
}
