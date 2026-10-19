using System.Numerics;

namespace Bit.Bink.Tests;

[TestClass]
public class DocumentTests
{
    [TestMethod]
    public void New_document_has_one_layer_and_no_elements()
    {
        var document = new BinkDocument();
        Assert.ContainsSingle(document.Layers);
        Assert.IsTrue(document.IsEmpty);
        Assert.AreEqual(0, document.ElementCount);
    }

    [TestMethod]
    public void AddElement_places_the_element_on_the_active_layer()
    {
        var document = new BinkDocument();
        BinkStroke stroke = TestData.Stroke(new Vector2(0, 0), new Vector2(10, 10));
        document.AddElement(stroke);
        Assert.AreEqual(1, document.ElementCount);
        Assert.AreSame(stroke, document.Layers[0].Elements[0]);
        Assert.AreEqual(document.Layers[0].Id, document.GetLayerOf(stroke.Id)!.Id);
    }

    [TestMethod]
    public void AddElement_rejects_a_duplicate_id()
    {
        var document = new BinkDocument();
        BinkStroke stroke = TestData.Stroke(new Vector2(0, 0), new Vector2(10, 10));
        document.AddElement(stroke);
        Assert.ThrowsExactly<InvalidOperationException>(() => document.AddElement(stroke));
    }

    [TestMethod]
    public void ReplaceElement_keeps_position_and_bumps_the_version()
    {
        var document = new BinkDocument();
        BinkStroke a = TestData.Stroke(new Vector2(0, 0), new Vector2(10, 10));
        BinkStroke b = TestData.Stroke(new Vector2(50, 50), new Vector2(60, 60));
        document.AddElement(a);
        document.AddElement(b);

        document.ReplaceElement(a with { Description = "edited" });

        BinkElement updated = document.Layers[0].Elements[0];
        Assert.AreEqual(a.Id, updated.Id);
        Assert.AreEqual("edited", updated.Description);
        Assert.IsTrue(updated.Version > a.Version);
        Assert.AreEqual(b.Id, document.Layers[0].Elements[1].Id);
    }

    [TestMethod]
    public void Elements_track_their_layer_after_reordering()
    {
        var document = new BinkDocument();
        BinkLayer second = document.AddLayer("Second");
        BinkStroke stroke = TestData.Stroke(new Vector2(0, 0), new Vector2(10, 10));
        document.AddElement(stroke, document.Layers[0].Id);

        document.Apply(new BinkReorderElementOperation(stroke.Id, second.Id, null));

        Assert.AreEqual(second.Id, document.GetLayerOf(stroke.Id)!.Id);
        Assert.IsEmpty(document.Layers[0].Elements);
        Assert.ContainsSingle(second.Elements);
    }

    [TestMethod]
    public void RemoveLayer_refuses_to_remove_the_last_layer()
    {
        var document = new BinkDocument();
        Assert.ThrowsExactly<InvalidOperationException>(() => document.RemoveLayer(document.Layers[0].Id));
    }

    [TestMethod]
    public void ContentBounds_covers_the_visible_elements()
    {
        var document = new BinkDocument();
        document.AddElement(new BinkRectangleShape(new BinkRect(10, 10, 20, 20)) { Style = new BinkShapeStyle { StrokeWidth = 0 } });
        document.AddElement(new BinkRectangleShape(new BinkRect(100, 50, 20, 20)) { Style = new BinkShapeStyle { StrokeWidth = 0 } });
        BinkRect bounds = document.ContentBounds;
        Assert.AreEqual(9f, bounds.Left, 5e-1);
        Assert.AreEqual(121f, bounds.Right, 5e-1);
    }

    [TestMethod]
    public void ContentBounds_ignores_hidden_layers()
    {
        var document = new BinkDocument();
        document.AddElement(new BinkRectangleShape(new BinkRect(10, 10, 20, 20)));
        BinkLayer hidden = document.AddLayer("Hidden");
        document.AddElement(new BinkRectangleShape(new BinkRect(500, 500, 20, 20)), hidden.Id);
        document.UpdateLayer(hidden.Id, p => p with { IsVisible = false });
        Assert.IsTrue(document.ContentBounds.Right < 100);
    }

    [TestMethod]
    public void Clear_removes_everything_in_one_undoable_step()
    {
        var document = new BinkDocument();
        using var history = new BinkHistoryManager(document);
        for (int i = 0; i < 5; i++) document.AddElement(TestData.Stroke(new Vector2(i, 0), new Vector2(i + 5, 10)));

        document.Clear();
        Assert.IsTrue(document.IsEmpty);

        history.Undo();
        Assert.AreEqual(5, document.ElementCount);
    }

    [TestMethod]
    public void Batch_raises_one_change_event()
    {
        var document = new BinkDocument();
        int events = 0;
        document.Changed += (_, _) => events++;

        using (document.BeginBatch("Add many"))
        {
            for (int i = 0; i < 10; i++) document.AddElement(TestData.Stroke(new Vector2(i, 0), new Vector2(i + 1, 1)));
        }

        Assert.AreEqual(1, events);
        Assert.AreEqual(10, document.ElementCount);
    }

    [TestMethod]
    public void A_batch_inverse_undoes_its_operations_in_reverse_order()
    {
        // The inverses are collected in forward order and reversed once when the batch closes. Applied in forward
        // order, the removal would run before the replacement and the replacement would then find nothing.
        var document = new BinkDocument();
        BinkDrawingOperation? inverse = null;
        document.Changed += (_, e) => inverse = e.Inverse;
        BinkStroke stroke = TestData.Stroke(new Vector2(0, 0), new Vector2(10, 10));

        using (document.BeginBatch("Add and edit"))
        {
            document.AddElement(stroke);
            document.ReplaceElement(stroke with { Description = "edited" });
            document.SetMetadata("key", "value");
        }

        Assert.IsNotNull(inverse);
        var batch = Assert.IsExactInstanceOfType<BinkBatchOperation>(inverse);
        Assert.AreEqual(3, batch.Operations.Count);
        Assert.IsInstanceOfType<BinkSetMetadataOperation>(batch.Operations[0]);
        Assert.IsInstanceOfType<BinkReplaceElementOperation>(batch.Operations[1]);
        Assert.IsInstanceOfType<BinkRemoveElementOperation>(batch.Operations[2]);

        document.Apply(inverse);
        Assert.AreEqual(0, document.ElementCount);
        Assert.IsFalse(document.Metadata.ContainsKey("key"));
    }

    [TestMethod]
    public void A_failing_operation_inside_a_batch_leaves_the_document_unchanged()
    {
        var document = new BinkDocument();
        BinkStroke existing = TestData.Stroke(new Vector2(0, 0), new Vector2(1, 1));
        document.AddElement(existing);

        var batch = new BinkBatchOperation(
        [
            new BinkAddElementOperation(document.Layers[0].Id, TestData.Stroke(new Vector2(2, 2), new Vector2(3, 3))),
            new BinkAddElementOperation(document.Layers[0].Id, existing), // duplicate id: must fail
        ]);

        Assert.ThrowsExactly<InvalidOperationException>(() => document.Apply(batch));
        Assert.AreEqual(1, document.ElementCount);
    }

    [TestMethod]
    public void TotalPointCount_tracks_additions_and_removals()
    {
        var document = new BinkDocument();
        BinkStroke stroke = TestData.Stroke(new Vector2(0, 0), new Vector2(10, 10), count: 25);
        document.AddElement(stroke);
        Assert.AreEqual(25, document.TotalPointCount);
        document.RemoveElement(stroke.Id);
        Assert.AreEqual(0, document.TotalPointCount);
    }

    [TestMethod]
    public void Limits_reject_a_stroke_with_too_many_points()
    {
        var document = new BinkDocument(BinkDocumentSettings.Default, BinkDocumentLimits.Default with { MaxPointsPerStroke = 10 });
        Assert.ThrowsExactly<InvalidOperationException>(() => document.AddElement(TestData.Stroke(new Vector2(0, 0), new Vector2(1, 1), count: 50)));
    }

    [TestMethod]
    public void Limits_reject_coordinates_far_outside_the_allowed_range()
    {
        var document = new BinkDocument(BinkDocumentSettings.Default, BinkDocumentLimits.Default with { MaxCoordinate = 1000 });
        Assert.ThrowsExactly<InvalidOperationException>(() => document.AddElement(TestData.Stroke(new Vector2(0, 0), new Vector2(50_000, 0))));
    }

    [TestMethod]
    public void Clone_shares_elements_but_not_layer_lists()
    {
        BinkDocument document = TestData.SampleDocument();
        BinkDocument copy = document.Clone();

        Assert.AreEqual(document.ElementCount, copy.ElementCount);
        Assert.AreSame(document.Layers[0].Elements[0], copy.Layers[0].Elements[0]);

        copy.AddElement(TestData.Stroke(new Vector2(0, 0), new Vector2(1, 1)));
        Assert.AreNotEqual(document.ElementCount, copy.ElementCount);
    }
}

[TestClass]
public class HistoryTests
{
    [TestMethod]
    public void Undo_and_redo_restore_the_element()
    {
        var document = new BinkDocument();
        using var history = new BinkHistoryManager(document);
        BinkStroke stroke = TestData.Stroke(new Vector2(0, 0), new Vector2(10, 10));

        document.AddElement(stroke);
        Assert.IsTrue(history.CanUndo);

        Assert.IsTrue(history.Undo());
        Assert.IsTrue(document.IsEmpty);
        Assert.IsTrue(history.CanRedo);

        Assert.IsTrue(history.Redo());
        Assert.AreEqual(1, document.ElementCount);
        Assert.AreEqual(stroke.Id, document.Layers[0].Elements[0].Id);
    }

    [TestMethod]
    public void Undo_restores_the_z_order_position()
    {
        var document = new BinkDocument();
        using var history = new BinkHistoryManager(document);
        BinkStroke a = TestData.Stroke(new Vector2(0, 0), new Vector2(1, 1));
        BinkStroke b = TestData.Stroke(new Vector2(2, 2), new Vector2(3, 3));
        BinkStroke c = TestData.Stroke(new Vector2(4, 4), new Vector2(5, 5));
        document.AddElement(a);
        document.AddElement(b);
        document.AddElement(c);

        document.RemoveElement(b.Id);
        history.Undo();

        Assert.AreSequenceEqual([a.Id, b.Id, c.Id], document.Layers[0].Elements.Select(e => e.Id));
    }

    [TestMethod]
    public void A_new_change_clears_the_redo_stack()
    {
        var document = new BinkDocument();
        using var history = new BinkHistoryManager(document);
        document.AddElement(TestData.Stroke(new Vector2(0, 0), new Vector2(1, 1)));
        history.Undo();
        Assert.IsTrue(history.CanRedo);

        document.AddElement(TestData.Stroke(new Vector2(5, 5), new Vector2(6, 6)));
        Assert.IsFalse(history.CanRedo);
    }

    [TestMethod]
    public void Groups_collapse_many_changes_into_one_step()
    {
        var document = new BinkDocument();
        using var history = new BinkHistoryManager(document);
        BinkStroke stroke = TestData.Stroke(new Vector2(0, 0), new Vector2(10, 10));
        document.AddElement(stroke);

        using (history.BeginGroup("Drag"))
        {
            for (int i = 1; i <= 20; i++)
            {
                document.ReplaceElement(document.GetElement(stroke.Id) with { Transform = Matrix3x2.CreateTranslation(i, 0) });
            }
        }

        Assert.AreEqual(2, history.UndoCount);
        history.Undo();
        Assert.AreEqual(Matrix3x2.Identity, document.GetElement(stroke.Id).Transform);
    }

    [TestMethod]
    public void Remote_and_load_changes_are_not_recorded()
    {
        var document = new BinkDocument();
        using var history = new BinkHistoryManager(document);
        document.Apply(new BinkAddElementOperation(document.Layers[0].Id, TestData.Stroke(new Vector2(0, 0), new Vector2(1, 1))), BinkChangeOrigin.Remote);
        Assert.IsFalse(history.CanUndo);
    }

    [TestMethod]
    public void The_limit_drops_the_oldest_steps()
    {
        var document = new BinkDocument();
        using var history = new BinkHistoryManager(document, limit: 5);
        for (int i = 0; i < 20; i++) document.AddElement(TestData.Stroke(new Vector2(i, 0), new Vector2(i + 1, 1)));
        Assert.AreEqual(5, history.UndoCount);
    }

    [TestMethod]
    public void Undoing_everything_returns_to_the_original_document()
    {
        var document = new BinkDocument();
        using var history = new BinkHistoryManager(document);
        Guid layerId = document.Layers[0].Id;

        document.AddElement(TestData.Stroke(new Vector2(0, 0), new Vector2(10, 10)));
        BinkLayer layer = document.AddLayer("Second");
        document.AddElement(TestData.Stroke(new Vector2(20, 20), new Vector2(30, 30)), layer.Id);
        document.UpdateLayer(layerId, p => p with { Opacity = 0.5f });
        document.SetMetadata("author", "test");

        while (history.CanUndo) history.Undo();

        Assert.ContainsSingle(document.Layers);
        Assert.IsTrue(document.IsEmpty);
        Assert.AreEqual(1f, document.Layers[0].Opacity);
        Assert.IsEmpty(document.Metadata);
    }

    [TestMethod]
    public void History_does_not_copy_stroke_point_data()
    {
        var document = new BinkDocument();
        using var history = new BinkHistoryManager(document);
        BinkStroke stroke = TestData.Stroke(new Vector2(0, 0), new Vector2(100, 100), count: 500);
        document.AddElement(stroke);
        document.RemoveElement(stroke.Id);

        // The undo entry references the same points instance, so undo history costs no per-point memory.
        var operation = (BinkAddElementOperation)history.UndoStack[^1].Inverse;
        Assert.AreSame(stroke.Points, ((BinkStroke)operation.Element).Points);
    }
}

[TestClass]
public class SelectionTests
{
    [TestMethod]
    public void Selection_drops_elements_that_are_removed()
    {
        var document = new BinkDocument();
        using var selection = new BinkSelection(document);
        BinkStroke stroke = TestData.Stroke(new Vector2(0, 0), new Vector2(10, 10));
        document.AddElement(stroke);
        selection.Set(stroke.Id);
        Assert.AreEqual(1, selection.Count);

        document.RemoveElement(stroke.Id);
        Assert.IsTrue(selection.IsEmpty);
    }

    [TestMethod]
    public void Eraser_strokes_cannot_be_selected()
    {
        var document = new BinkDocument();
        using var selection = new BinkSelection(document);
        var eraser = new BinkStroke(TestData.Line(new Vector2(0, 0), new Vector2(10, 10)), new BinkPixelEraserBrush());
        document.AddElement(eraser);
        selection.Add(eraser.Id);
        Assert.IsTrue(selection.IsEmpty);
    }

    [TestMethod]
    public void SelectAll_skips_locked_and_hidden_layers()
    {
        var document = new BinkDocument();
        using var selection = new BinkSelection(document);
        document.AddElement(TestData.Stroke(new Vector2(0, 0), new Vector2(10, 10)));
        BinkLayer locked = document.AddLayer("Locked");
        document.AddElement(TestData.Stroke(new Vector2(20, 20), new Vector2(30, 30)), locked.Id);
        document.UpdateLayer(locked.Id, p => p with { IsLocked = true });

        selection.SelectAll();
        Assert.AreEqual(1, selection.Count);
    }

    [TestMethod]
    public void HitTest_returns_the_topmost_element()
    {
        var document = new BinkDocument();
        var bottom = new BinkRectangleShape(new BinkRect(0, 0, 100, 100)) { Style = new BinkShapeStyle { FillColor = BinkColor.White } };
        var top = new BinkRectangleShape(new BinkRect(10, 10, 50, 50)) { Style = new BinkShapeStyle { FillColor = BinkColor.Black } };
        document.AddElement(bottom);
        document.AddElement(top);

        Assert.AreEqual(top.Id, BinkDocumentQuery.HitTest(document, new Vector2(30, 30))!.Id);
        Assert.AreEqual(bottom.Id, BinkDocumentQuery.HitTest(document, new Vector2(90, 90))!.Id);
    }

    [TestMethod]
    public void HitTest_respects_the_element_transform()
    {
        var document = new BinkDocument();
        var rect = new BinkRectangleShape(new BinkRect(0, 0, 20, 20)) { Style = new BinkShapeStyle { FillColor = BinkColor.Black }, Transform = Matrix3x2.CreateTranslation(100, 100) };
        document.AddElement(rect);

        Assert.IsNull(BinkDocumentQuery.HitTest(document, new Vector2(10, 10), tolerance: 0));
        Assert.IsNotNull(BinkDocumentQuery.HitTest(document, new Vector2(110, 110), tolerance: 0));
    }

    [TestMethod]
    public void FindInRect_distinguishes_intersect_from_contain()
    {
        var document = new BinkDocument();
        document.AddElement(TestData.Stroke(new Vector2(0, 0), new Vector2(100, 0)));
        Assert.ContainsSingle(BinkDocumentQuery.FindInRect(document, new BinkRect(50, -10, 100, 20), BinkRectSelectionMode.Intersect));
        Assert.IsEmpty(BinkDocumentQuery.FindInRect(document, new BinkRect(50, -10, 100, 20), BinkRectSelectionMode.Contain));
        Assert.ContainsSingle(BinkDocumentQuery.FindInRect(document, new BinkRect(-20, -20, 200, 40), BinkRectSelectionMode.Contain));
    }

    [TestMethod]
    public void Rect_selection_does_not_match_a_stroke_that_only_passes_nearby()
    {
        var document = new BinkDocument();
        // A large diagonal stroke whose bounding box covers the query rectangle but whose ink does not.
        document.AddElement(TestData.Stroke(new Vector2(0, 0), new Vector2(200, 200), count: 50));
        Assert.IsEmpty(BinkDocumentQuery.FindInRect(document, new BinkRect(150, 20, 20, 20)));
        Assert.ContainsSingle(BinkDocumentQuery.FindInRect(document, new BinkRect(90, 90, 20, 20)));
    }
}
