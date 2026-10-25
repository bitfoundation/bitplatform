using System.Numerics;

namespace Bit.Bink.Tests;

/// <summary>Arranging, duplicating, the clipboard format, lasso selection, ink measurement and snapping.</summary>
[TestClass]
public sealed class EditingFeatureTests
{
    private static (BinkDocument Document, List<Guid> Ids) Stack(int count)
    {
        var document = new BinkDocument();
        var ids = new List<Guid>();
        for (int i = 0; i < count; i++)
        {
            BinkStroke s = TestData.Stroke(new Vector2(i * 10, 0), new Vector2(i * 10 + 5, 5));
            document.AddElement(s);
            ids.Add(s.Id);
        }
        return (document, ids);
    }

    private static List<Guid> Order(BinkDocument document) => document.Layers[0].Elements.Select(e => e.Id).ToList();

    [TestMethod]
    public void Bring_to_front_and_send_to_back_keep_the_moved_elements_in_order()
    {
        (BinkDocument document, List<Guid> ids) = Stack(6);
        using var history = new BinkHistoryManager(document);

        Assert.IsTrue(document.Arrange([ids[3], ids[1]], BinkArrangement.BringToFront));
        CollectionAssert.AreEqual(new[] { ids[0], ids[2], ids[4], ids[5], ids[1], ids[3] }, Order(document));
        Assert.AreEqual(1, history.UndoCount, "An arrangement is one undo step.");

        Assert.IsTrue(document.Arrange([ids[5], ids[4]], BinkArrangement.SendToBack));
        CollectionAssert.AreEqual(new[] { ids[4], ids[5], ids[0], ids[2], ids[1], ids[3] }, Order(document));

        Assert.IsTrue(history.Undo());
        Assert.IsTrue(history.Undo());
        CollectionAssert.AreEqual(ids, Order(document));
    }

    [TestMethod]
    public void Arranging_what_is_already_in_place_changes_nothing()
    {
        (BinkDocument document, List<Guid> ids) = Stack(4);
        using var history = new BinkHistoryManager(document);
        Assert.IsFalse(document.Arrange([ids[2], ids[3]], BinkArrangement.BringToFront));
        Assert.IsFalse(document.Arrange([ids[0]], BinkArrangement.SendToBack));
        Assert.IsFalse(document.Arrange([ids[3]], BinkArrangement.BringForward));
        Assert.IsFalse(document.Arrange([Guid.NewGuid()], BinkArrangement.BringForward));
        Assert.AreEqual(0, history.UndoCount);
    }

    [TestMethod]
    public void Forward_and_backward_step_over_one_element_and_move_blocks_together()
    {
        (BinkDocument document, List<Guid> ids) = Stack(5);
        Assert.IsTrue(document.Arrange([ids[0], ids[1]], BinkArrangement.BringForward));
        CollectionAssert.AreEqual(new[] { ids[2], ids[0], ids[1], ids[3], ids[4] }, Order(document));

        Assert.IsTrue(document.Arrange([ids[4], ids[1]], BinkArrangement.SendBackward));
        CollectionAssert.AreEqual(new[] { ids[2], ids[1], ids[0], ids[4], ids[3] }, Order(document));
    }

    [TestMethod]
    public void Arranging_stays_within_each_element_s_layer()
    {
        var document = new BinkDocument();
        BinkLayer top = document.AddLayer("Top");
        BinkStroke a = TestData.Stroke(Vector2.Zero, new Vector2(5, 5));
        BinkStroke b = TestData.Stroke(Vector2.Zero, new Vector2(5, 5));
        BinkStroke c = TestData.Stroke(Vector2.Zero, new Vector2(5, 5));
        document.AddElement(a, document.Layers[0].Id);
        document.AddElement(b, document.Layers[0].Id);
        document.AddElement(c, top.Id);
        Assert.IsTrue(document.Arrange([a.Id, c.Id], BinkArrangement.BringToFront));
        Assert.AreEqual(document.Layers[0].Id, document.GetLayerOf(a.Id)!.Id);
        Assert.AreEqual(top.Id, document.GetLayerOf(c.Id)!.Id);
        CollectionAssert.AreEqual(new[] { b.Id, a.Id }, Order(document));
    }

    [TestMethod]
    public void Duplicating_adds_offset_copies_with_new_ids_as_one_step()
    {
        (BinkDocument document, List<Guid> ids) = Stack(3);
        using var history = new BinkHistoryManager(document);
        IReadOnlyList<BinkElement> copies = document.DuplicateElements([ids[2], ids[0]], new Vector2(10, 20));

        Assert.HasCount(2, copies);
        Assert.AreEqual(5, document.ElementCount);
        Assert.IsTrue(copies.All(c => !ids.Contains(c.Id)));
        // Copies keep the originals' relative order, on top of the layer.
        Assert.AreEqual(document.GetElement(ids[0]).Bounds.Left + 10, copies[0].Bounds.Left, 0.001f);
        Assert.AreEqual(document.GetElement(ids[0]).Bounds.Top + 20, copies[0].Bounds.Top, 0.001f);
        Assert.AreEqual(document.GetElement(ids[2]).Bounds.Left + 10, copies[1].Bounds.Left, 0.001f);
        CollectionAssert.AreEqual(new[] { copies[0].Id, copies[1].Id }, Order(document).TakeLast(2).ToList());

        Assert.AreEqual(1, history.UndoCount);
        Assert.IsTrue(history.Undo());
        Assert.AreEqual(3, document.ElementCount);
    }

    [TestMethod]
    public void The_clipboard_round_trips_elements_as_new_ones()
    {
        BinkDocument source = TestData.SampleDocument();
        List<BinkElement> copied = source.AllElements.Take(4).ToList();
        string payload = BinkClipboard.Serialize(copied);

        Assert.IsTrue(BinkClipboard.IsPayload(payload));
        Assert.IsTrue(BinkClipboard.TryDeserialize(payload, out IReadOnlyList<BinkElement> pasted, out string? error), error);
        Assert.HasCount(4, pasted);
        for (int i = 0; i < pasted.Count; i++)
        {
            Assert.AreNotEqual(copied[i].Id, pasted[i].Id);
            Assert.AreEqual(0, pasted[i].Version);
            Assert.AreEqual(copied[i].GetType(), pasted[i].GetType());
            Assert.AreEqual(copied[i].Bounds.Center.X, pasted[i].Bounds.Center.X, 0.5f);
        }

        // Pasting the same payload twice gives independent copies.
        Assert.IsTrue(BinkClipboard.TryDeserialize(payload, out IReadOnlyList<BinkElement> again, out _));
        Assert.AreNotEqual(pasted[0].Id, again[0].Id);
    }

    [TestMethod]
    public void The_clipboard_leaves_out_masks_and_refuses_what_is_not_bink()
    {
        var eraser = new BinkStroke(TestData.Line(Vector2.Zero, new Vector2(20, 20)), new BinkPixelEraserBrush());
        BinkStroke ink = TestData.Stroke(Vector2.Zero, new Vector2(20, 20));
        Assert.IsTrue(BinkClipboard.TryDeserialize(BinkClipboard.Serialize([ink, eraser]), out IReadOnlyList<BinkElement> pasted, out _));
        Assert.ContainsSingle(pasted);

        Assert.IsFalse(BinkClipboard.IsPayload("hello"));
        Assert.IsFalse(BinkClipboard.TryDeserialize("hello", out IReadOnlyList<BinkElement> none, out string? error));
        Assert.IsEmpty(none);
        Assert.IsNotNull(error);
        Assert.IsFalse(BinkClipboard.TryDeserialize("", out _, out _));
    }

    [TestMethod]
    public void A_saved_document_pastes_its_visible_elements()
    {
        BinkDocument document = TestData.SampleDocument();
        document.UpdateLayer(document.Layers[1].Id, p => p with { IsVisible = false });
        string saved = BinkSerializer.Serialize(document);
        Assert.IsTrue(BinkClipboard.IsPayload(saved));
        Assert.IsTrue(BinkClipboard.TryDeserialize(saved, out IReadOnlyList<BinkElement> pasted, out _));
        Assert.AreEqual(document.Layers[0].Count, pasted.Count);
    }

    [TestMethod]
    public void A_lasso_selects_what_it_mostly_encloses()
    {
        var document = new BinkDocument();
        BinkStroke inside = TestData.Stroke(new Vector2(20, 20), new Vector2(60, 40));
        BinkStroke grazed = TestData.Stroke(new Vector2(90, 30), new Vector2(200, 30));
        BinkStroke outside = TestData.Stroke(new Vector2(300, 300), new Vector2(320, 320));
        var box = new BinkRectangleShape(new BinkRect(30, 50, 20, 20));
        document.AddElement(inside);
        document.AddElement(grazed);
        document.AddElement(outside);
        document.AddElement(box);
        Vector2[] lasso = [new(0, 0), new(100, 0), new(100, 100), new(0, 100)];

        List<BinkElement> found = BinkDocumentQuery.FindInPolygon(document, lasso);
        CollectionAssert.AreEquivalent(new[] { inside.Id, box.Id }, found.Select(e => e.Id).ToList());

        // Touching is enough at zero coverage.
        Assert.Contains(grazed, BinkDocumentQuery.FindInPolygon(document, lasso, coverage: 0f));
        Assert.HasCount(2, BinkDocumentQuery.FindInPolygon(document, lasso, coverage: 1f));
        Assert.IsEmpty(BinkDocumentQuery.FindInPolygon(document, [new(0, 0), new(1, 1)]));
    }

    [TestMethod]
    public void A_lasso_judges_moved_elements_where_they_are_drawn()
    {
        var document = new BinkDocument();
        BinkStroke moved = TestData.Stroke(new Vector2(20, 20), new Vector2(60, 40)) with { Transform = Matrix3x2.CreateTranslation(400, 0) };
        document.AddElement(moved);
        Assert.IsEmpty(BinkDocumentQuery.FindInPolygon(document, [new(0, 0), new(100, 0), new(100, 100), new(0, 100)]));
        Assert.ContainsSingle(BinkDocumentQuery.FindInPolygon(document, [new(400, 0), new(500, 0), new(500, 100), new(400, 100)]));
    }

    [TestMethod]
    public void The_select_tool_lassos_with_alt_or_when_set_to()
    {
        using var context = new TestToolContext();
        BinkStroke word = TestData.Stroke(new Vector2(110, 110), new Vector2(150, 130));
        BinkStroke above = TestData.Stroke(new Vector2(100, 60), new Vector2(160, 60));
        context.Document.AddElement(word);
        context.Document.AddElement(above);
        var tool = new BinkSelectTool { MarqueeShape = BinkMarqueeShape.Lasso };

        // A loop around the word that passes close to the line above it.
        context.Gesture(tool, BinkPointerKind.Mouse, (95, 95, 0.5f), (170, 95, 0.5f), (175, 120, 0.5f), (170, 145, 0.5f), (95, 145, 0.5f), (90, 100, 0.5f));
        CollectionAssert.AreEqual(new[] { word.Id }, context.Selection.Ids.ToList());
        Assert.IsEmpty(tool.Lasso);
    }

    [TestMethod]
    public void Ink_metrics_measure_length_duration_and_extent()
    {
        var document = new BinkDocument();
        Assert.IsTrue(BinkDocumentQuery.MeasureInk(document).IsEmpty);
        document.AddElement(new BinkStroke(TestData.Line(Vector2.Zero, new Vector2(100, 0), 11), new BinkPenBrush()));
        document.AddElement(new BinkStroke(TestData.Line(Vector2.Zero, new Vector2(0, 50), 6), new BinkPenBrush()) { Transform = Matrix3x2.CreateScale(2f) });
        document.AddElement(new BinkStroke(TestData.Line(Vector2.Zero, new Vector2(500, 0)), new BinkPixelEraserBrush()));

        BinkInkMetrics metrics = BinkDocumentQuery.MeasureInk(document);
        Assert.AreEqual(2, metrics.StrokeCount);
        Assert.AreEqual(17, metrics.PointCount);
        Assert.AreEqual(200f, metrics.Length, 0.01f);
        // TestData spaces points 8 ms apart.
        Assert.AreEqual(TimeSpan.FromMilliseconds(80 + 40), metrics.Duration);
        Assert.IsTrue(metrics.Bounds.Contains(new Vector2(50, 0)));
    }

    [TestMethod]
    public void A_rectangle_snaps_to_the_nearest_edge_within_reach()
    {
        var document = new BinkDocument(new BinkDocumentSettings { Width = 0, Height = 0 });
        var anchor = new BinkRectangleShape(new BinkRect(100, 100, 50, 50)) { Style = new BinkShapeStyle { StrokeWidth = 0 } };
        document.AddElement(anchor);
        BinkRect target = anchor.Bounds;
        BinkSnapper snapper = BinkSnapper.Create(document, new BinkSnapOptions());

        var guides = new List<BinkSnapGuide>();
        // A box whose left edge is 3 units from the anchor's right edge snaps flush to it.
        BinkRect moving = new(target.Right + 3, 300, 40, 40);
        Vector2 offset = snapper.SnapRect(moving, 6f, guides);
        Assert.AreEqual(-3f, offset.X, 0.001f);
        Assert.AreEqual(0f, offset.Y, 0.001f);
        Assert.ContainsSingle(guides);
        Assert.IsTrue(guides[0].Vertical);
        Assert.AreEqual(target.Right, guides[0].Position, 0.001f);

        // Out of reach, nothing moves.
        Assert.AreEqual(Vector2.Zero, snapper.SnapRect(new BinkRect(target.Right + 30, 300, 40, 40), 6f));
    }

    [TestMethod]
    public void The_grid_applies_when_no_element_is_in_reach_and_moving_elements_are_ignored()
    {
        var document = new BinkDocument(new BinkDocumentSettings { Width = 0, Height = 0 });
        var moving = new BinkRectangleShape(new BinkRect(0, 0, 10, 10));
        document.AddElement(moving);
        BinkSnapper snapper = BinkSnapper.Create(document, new BinkSnapOptions { GridSize = 20 }, new HashSet<Guid> { moving.Id });

        Vector2 offset = snapper.SnapRect(new BinkRect(47, 61, 10, 10), 2f);
        Assert.AreEqual(new Vector2(-7, -1), offset);
        Assert.AreEqual(new Vector2(40, 60), snapper.SnapPoint(new Vector2(47, 61), 2f));
        Assert.AreEqual(new Vector2(40, 61), snapper.SnapPoint(new Vector2(47, 61), 2f, snapY: false));
        Assert.IsFalse(BinkSnapper.Create(document, BinkSnapOptions.None).IsEnabled);
    }

    [TestMethod]
    public void Dragging_a_selection_snaps_and_shows_a_guide()
    {
        using var context = new TestToolContext { Snapping = new BinkSnapOptions { ToPage = false } };
        var anchor = new BinkRectangleShape(new BinkRect(100, 100, 50, 50)) { Style = new BinkShapeStyle { StrokeWidth = 0 } };
        var box = new BinkRectangleShape(new BinkRect(300, 300, 40, 40)) { Style = new BinkShapeStyle { StrokeWidth = 0 } };
        context.Document.AddElement(anchor);
        context.Document.AddElement(box);
        context.Selection.Set(box.Id);
        var tool = new BinkSelectTool();
        float gap = box.Bounds.Left - anchor.Bounds.Left;

        // Drag the box so its left edge ends 4 units right of the anchor's left edge.
        // Unfilled shapes are picked up by their outline.
        context.Gesture(tool, BinkPointerKind.Mouse, (300, 320, 0.5f), (280, 320, 0.5f), (300 - gap + 4, 320, 0.5f));
        Assert.AreEqual(anchor.Bounds.Left, context.Document.GetElement(box.Id).Bounds.Left, 0.01f);
    }

    [TestMethod]
    public void Alt_dragging_moves_a_copy_and_is_one_compact_undo_step()
    {
        using var context = new TestToolContext();
        var box = new BinkRectangleShape(new BinkRect(100, 100, 40, 40));
        context.Document.AddElement(box);
        context.History.Clear();
        var tool = new BinkSelectTool();
        var points = new List<(float, float, float)> { (100, 120, 0.5f) };
        for (int i = 1; i <= 60; i++) points.Add((100 + i * 2, 120, 0.5f));
        BinkInputSample Sample(int index, BinkPointerPhase phase) => new()
        {
            PointerId = 1, Kind = BinkPointerKind.Mouse, Phase = phase, X = points[index].Item1, Y = points[index].Item2,
            Pressure = 0.5f, Timestamp = index * 8, Buttons = phase == BinkPointerPhase.Up ? 0 : 1, IsPrimary = true, Alt = true,
        };
        tool.OnPointerDown(context, Sample(0, BinkPointerPhase.Down));
        for (int i = 1; i < points.Count - 1; i++) tool.OnPointerMove(context, Sample(i, BinkPointerPhase.Move));
        tool.OnPointerUp(context, Sample(points.Count - 1, BinkPointerPhase.Up));

        Assert.AreEqual(2, context.Document.ElementCount);
        Assert.AreEqual(new BinkRect(100, 100, 40, 40), ((BinkRectangleShape)context.Document.GetElement(box.Id)).Rect);
        Guid copy = context.Selection.Ids.Single();
        Assert.AreNotEqual(box.Id, copy);
        Assert.AreEqual(box.Bounds.Left + 120, context.Document.GetElement(copy).Bounds.Left, 0.01f);

        BinkHistoryEntry entry = context.History.UndoStack.Single();
        Assert.AreEqual("Duplicate", entry.Label);
        Assert.ContainsSingle(((BinkBatchOperation)entry.Forward).Operations, "The copy and every frame of its drag fold into one addition.");
        Assert.IsTrue(context.History.Undo());
        Assert.AreEqual(1, context.Document.ElementCount);
        Assert.IsTrue(context.History.Redo());
        Assert.AreEqual(box.Bounds.Left + 120, context.Document.GetElement(copy).Bounds.Left, 0.01f);
    }

    [TestMethod]
    public void Cancelling_an_alt_drag_removes_the_copy()
    {
        using var context = new TestToolContext();
        var box = new BinkRectangleShape(new BinkRect(100, 100, 40, 40));
        context.Document.AddElement(box);
        context.History.Clear();
        var tool = new BinkSelectTool();
        tool.OnPointerDown(context, new BinkInputSample { PointerId = 1, Kind = BinkPointerKind.Mouse, Phase = BinkPointerPhase.Down, X = 100, Y = 120, Buttons = 1, Alt = true });
        tool.OnPointerMove(context, new BinkInputSample { PointerId = 1, Kind = BinkPointerKind.Mouse, Phase = BinkPointerPhase.Move, X = 160, Y = 120, Buttons = 1, Alt = true });
        Assert.AreEqual(2, context.Document.ElementCount);
        tool.OnKeyDown(context, new BinkToolKeyEvent("Escape", false, false, false));

        Assert.AreEqual(1, context.Document.ElementCount);
        CollectionAssert.AreEqual(new[] { box.Id }, context.Selection.Ids.ToList());
        Assert.IsFalse(context.History.CanUndo);
    }

    [TestMethod]
    public void Shift_rotates_in_fifteen_degree_steps()
    {
        using var context = new TestToolContext();
        var box = new BinkRectangleShape(new BinkRect(100, 100, 100, 100));
        context.Document.AddElement(box);
        context.Selection.Set(box.Id);
        var tool = new BinkSelectTool();
        BinkRect bounds = context.Selection.Bounds;
        Vector2 handle = new(bounds.Center.X, bounds.Top - tool.RotateHandleDistance);
        Vector2 center = bounds.Center;
        // About 20 degrees clockwise, which snaps to 15.
        float angle = 20f * MathF.PI / 180f;
        Vector2 to = center + Vector2.Transform(handle - center, Matrix3x2.CreateRotation(angle));
        BinkInputSample Sample(Vector2 p, BinkPointerPhase phase) => new()
        {
            PointerId = 1, Kind = BinkPointerKind.Mouse, Phase = phase, X = p.X, Y = p.Y, Buttons = phase == BinkPointerPhase.Up ? 0 : 1, Shift = true,
        };
        Assert.IsTrue(tool.OnPointerDown(context, Sample(handle, BinkPointerPhase.Down)));
        tool.OnPointerMove(context, Sample(to, BinkPointerPhase.Move));
        tool.OnPointerUp(context, Sample(to, BinkPointerPhase.Up));

        Matrix3x2 m = context.Document.GetElement(box.Id).Transform;
        Assert.AreEqual(15f, MathF.Atan2(m.M12, m.M11) * 180f / MathF.PI, 0.01f);
    }
}
