using System.Numerics;

namespace Bit.Bink.Tests;

/// <summary>
/// Undo and redo regressions, including gestures a host interrupts part way through.
/// </summary>
[TestClass]
public class HistoryRegressionTests(TestContext output)
{
    [TestMethod]
    public void A_scope_disposed_after_the_group_was_force_closed_is_a_no_op()
    {
        // A tool opens a group at pointer down and disposes it at pointer up. If the host closes the group in
        // between - LoadDocument, a document swap, disposing the manager - the tool's own dispose still runs, and
        // closing an already closed group must not throw.
        var document = new BinkDocument();
        using var history = new BinkHistoryManager(document);
        IDisposable group = history.BeginGroup("Interrupted");
        document.AddElement(TestData.Stroke(new Vector2(0, 0), new Vector2(10, 10)));
        history.EndOpenGroups();
        group.Dispose();
        document.AddElement(TestData.Stroke(new Vector2(1, 1), new Vector2(9, 9)));
        Assert.AreEqual(2, history.UndoCount);
    }

    [TestMethod]
    public void A_scope_disposed_after_the_manager_was_disposed_is_a_no_op()
    {
        var document = new BinkDocument();
        var history = new BinkHistoryManager(document);
        IDisposable group = history.BeginGroup("Interrupted");
        document.AddElement(TestData.Stroke(new Vector2(0, 0), new Vector2(10, 10)));
        history.Dispose();
        group.Dispose();
    }

    [TestMethod]
    public void A_long_random_edit_sequence_stays_consistent()
    {
        var random = new Random(20260909);
        var document = new BinkDocument();
        using var history = new BinkHistoryManager(document, 20000);
        using var selection = new BinkSelection(document);
        var log = new List<string>();

        for (int step = 0; step < 4000; step++)
        {
            int action = random.Next(6);
            switch (action)
            {
                case 0:
                case 1:
                    document.AddElement(TestData.Stroke(new Vector2(random.Next(500), random.Next(500)), new Vector2(random.Next(500), random.Next(500)), count: 4));
                    log.Add("add");
                    break;
                case 2:
                    if (!document.IsEmpty)
                    {
                        BinkElement victim = document.AllElements.ElementAt(random.Next(document.ElementCount));
                        document.RemoveElement(victim.Id);
                        log.Add("remove");
                    }
                    break;
                case 3:
                    if (!document.IsEmpty)
                    {
                        BinkElement e = document.AllElements.ElementAt(random.Next(document.ElementCount));
                        document.ReplaceElement(e.Translate(new Vector2(1, 1)));
                        log.Add("move");
                    }
                    break;
                case 4:
                    log.Add(history.Undo() ? "undo" : "undo-noop");
                    break;
                case 5:
                    log.Add(history.Redo() ? "redo" : "redo-noop");
                    break;
            }

            // The element index and the layer lists must never disagree, whatever the sequence was.
            Assert.AreEqual(document.ElementCount, document.AllElements.Count());
            foreach (BinkElement e in document.AllElements)
            {
                Assert.IsTrue(document.ContainsElement(e.Id));
                Assert.IsNotNull(document.GetLayerOf(e.Id));
            }
        }

        int guard = 0;
        while (history.CanUndo && guard++ < 100_000) history.Undo();
        output.WriteLine($"after full undo: {document.ElementCount} elements, {document.TotalPointCount} points, log tail {string.Join(",", log.TakeLast(10))}");
        Assert.IsTrue(document.IsEmpty, $"{document.ElementCount} elements survived a full undo");
        Assert.AreEqual(0, document.TotalPointCount);
    }

    [TestMethod]
    [DataRow("draw")]
    [DataRow("erase-stroke")]
    [DataRow("erase-segment")]
    [DataRow("select")]
    [DataRow("shape")]
    [DataRow("text")]
    public void A_gesture_interrupted_by_a_document_swap_leaves_history_usable(string toolId)
    {
        using var context = new TestToolContext();
        context.Document.AddElement(TestData.Stroke(new Vector2(50, 50), new Vector2(250, 250), new BinkPenBrush { Width = 6 }));
        IBinkTool tool = toolId switch
        {
            "erase-stroke" => new BinkEraserTool { Mode = BinkEraserMode.Stroke, Radius = 20 },
            "erase-segment" => new BinkEraserTool { Mode = BinkEraserMode.Segment, Radius = 20 },
            "select" => new BinkSelectTool(),
            "shape" => new BinkShapeTool(BinkShapeKind.Rectangle),
            "text" => new BinkTextTool(),
            _ => new BinkDrawTool(),
        };
        tool.Activate(context);

        BinkInputSample S(BinkPointerPhase phase, float x, float y, double t) => new()
        { PointerId = 3, Kind = BinkPointerKind.Mouse, Phase = phase, X = x, Y = y, Pressure = 0.5f, Timestamp = t, Buttons = phase == BinkPointerPhase.Up ? 0 : 1 };

        tool.OnPointerDown(context, S(BinkPointerPhase.Down, 60, 60, 0));
        for (int i = 1; i <= 6; i++) tool.OnPointerMove(context, S(BinkPointerPhase.Move, 60 + i * 20, 60 + i * 20, i * 10));

        // Halfway through the gesture the host tears the session down, exactly as LoadDocument does.
        context.History.EndOpenGroups();
        context.History.Dispose();

        // The tool still finishes its gesture, and nothing it does may throw.
        tool.OnPointerUp(context, S(BinkPointerPhase.Up, 220, 220, 100));
        tool.Deactivate(context);
        output.WriteLine($"{toolId}: {context.Document.ElementCount} elements after the interrupted gesture");
    }

    [TestMethod]
    public void A_gesture_interrupted_by_a_tool_change_leaves_history_usable()
    {
        using var context = new TestToolContext();
        var eraser = new BinkEraserTool { Mode = BinkEraserMode.Segment, Radius = 15 };
        context.Document.AddElement(TestData.Stroke(new Vector2(50, 100), new Vector2(450, 100), new BinkPenBrush { Width = 5 }, 60));
        eraser.Activate(context);

        BinkInputSample S(BinkPointerPhase phase, float x, float y) => new()
        { PointerId = 1, Kind = BinkPointerKind.Mouse, Phase = phase, X = x, Y = y, Pressure = 0.5f, Buttons = phase == BinkPointerPhase.Up ? 0 : 1 };

        eraser.OnPointerDown(context, S(BinkPointerPhase.Down, 200, 100));
        eraser.OnPointerMove(context, S(BinkPointerPhase.Move, 220, 100));
        eraser.Deactivate(context);

        var pen = new BinkDrawTool();
        pen.Activate(context);
        context.Line(pen, new Vector2(10, 300), new Vector2(200, 320), 20);

        output.WriteLine($"undo steps: {context.History.UndoCount}");
        Assert.IsTrue(context.History.CanUndo, "history was left disabled by the interrupted erase");
        Assert.IsTrue(context.History.Undo());
        Assert.IsTrue(context.History.Undo());
    }

    [TestMethod]
    public void Exporting_a_document_that_is_then_cleared_produces_valid_output()
    {
        var document = new BinkDocument();
        for (int i = 0; i < 300; i++) document.AddElement(TestData.Stroke(new Vector2(i, 0), new Vector2(i, 50), count: 4));

        string svg = BinkSvgExporter.Export(document);
        var xml = new System.Xml.XmlDocument();
        xml.LoadXml(svg);
        Assert.IsNotNull(xml.DocumentElement);

        int before = document.ElementCount;
        document.Clear();
        Assert.AreEqual(0, document.ElementCount);
        Assert.AreEqual(300, before);
    }

    [TestMethod]
    public void Redo_into_a_layer_a_collaborator_removed_is_refused_not_thrown()
    {
        // The missing layer used to surface as KeyNotFoundException, which the history manager does not catch:
        // the redo crashed instead of being refused like any other step the document moved past.
        var document = new BinkDocument();
        using var history = new BinkHistoryManager(document);
        BinkLayer notes = document.AddLayer("Notes");
        document.AddElement(TestData.Stroke(new Vector2(0, 0), new Vector2(10, 10)), notes.Id);
        Assert.IsTrue(history.Undo());

        document.Apply(new BinkRemoveLayerOperation(notes.Id), BinkChangeOrigin.Remote);

        Assert.IsFalse(history.Redo());
        Assert.AreEqual(1, history.RedoCount);
    }

    [TestMethod]
    public void A_tool_working_on_a_layer_that_no_longer_exists_does_not_strand_history()
    {
        using var context = new TestToolContext();
        context.ActiveLayerId = Guid.NewGuid();

        // The pen reports the refusal instead of throwing out of the pointer callback.
        context.Line(new BinkDrawTool(), new Vector2(10, 10), new Vector2(100, 60));
        Assert.AreEqual(0, context.Document.ElementCount);

        // The text tool opens its undo group before adding the box; a throw there used to leave it open for good.
        Assert.IsFalse(new BinkTextTool().CreateAt(context, new Vector2(20, 20)));

        context.ActiveLayerId = context.Document.Layers[0].Id;
        context.Line(new BinkDrawTool(), new Vector2(10, 10), new Vector2(100, 60));
        Assert.IsTrue(context.History.Undo(), "history was left disabled by the refused text box");
    }

    [TestMethod]
    public void A_text_box_left_blank_leaves_no_undo_step()
    {
        using var context = new TestToolContext();
        var tool = new BinkTextTool();
        Assert.IsTrue(tool.CreateAt(context, new Vector2(20, 20)));
        tool.CommitEdit(context, "   ");

        Assert.AreEqual(0, context.Document.ElementCount);
        Assert.IsFalse(context.History.CanUndo, "an undo step that adds and removes the same empty box does nothing");
    }

    [TestMethod]
    public void A_failed_batch_puts_elements_back_at_their_original_version()
    {
        // The rollback is never reported to collaborators, so a version bump here would leave this replica ahead
        // of everyone else and make it discard their next edit of the element.
        var document = new BinkDocument();
        BinkStroke stroke = TestData.Stroke(new Vector2(0, 0), new Vector2(10, 10));
        document.AddElement(stroke);
        BinkElement before = document.GetElement(stroke.Id);

        var batch = new BinkBatchOperation(
        [
            new BinkReplaceElementOperation(before with { Description = "edited" }),
            new BinkRemoveElementOperation(Guid.NewGuid()),
        ]);
        Assert.ThrowsExactly<InvalidOperationException>(() => document.Apply(batch));

        BinkElement after = document.GetElement(stroke.Id);
        Assert.IsNull(after.Description);
        Assert.AreEqual(before.Version, after.Version);
    }

    private static List<int> ReportedVersions(BinkDrawingOperation operation)
    {
        var versions = new List<int>();
        void Collect(BinkDrawingOperation op)
        {
            if (op is BinkReplaceElementOperation replace) versions.Add(replace.Element.Version);
            else if (op is BinkBatchOperation batch) foreach (BinkDrawingOperation inner in batch.Operations) Collect(inner);
        }
        Collect(operation);
        return versions;
    }

    [TestMethod]
    public void Redoing_a_drag_reports_the_versions_it_stores()
    {
        // What collaborators receive for a redo must match what the sender keeps, or the replicas disagree about
        // which version is newest.
        var document = new BinkDocument();
        using var history = new BinkHistoryManager(document);
        BinkStroke stroke = TestData.Stroke(new Vector2(0, 0), new Vector2(10, 10));
        document.AddElement(stroke);
        using (history.BeginGroup("Move"))
        {
            for (int i = 1; i <= 3; i++) document.ReplaceElement(document.GetElement(stroke.Id) with { Transform = Matrix3x2.CreateTranslation(i * 10, 0) });
        }
        Assert.IsTrue(history.Undo());

        BinkDrawingOperation? reported = null;
        document.Changed += (_, e) => reported = e.Operation;
        Assert.IsTrue(history.Redo());

        List<int> versions = ReportedVersions(reported!);
        Assert.AreEqual(document.GetElement(stroke.Id).Version, versions[^1]);
        Assert.AreEqual(Matrix3x2.CreateTranslation(30, 0), document.GetElement(stroke.Id).Transform);
    }

    [TestMethod]
    public void A_batch_replacing_one_element_several_times_reports_the_versions_it_stores()
    {
        // Each replacement in the batch is stored one version above the previous one. Deciding every version from
        // the state before the batch reported the same version for each, which did not match what was stored.
        var document = new BinkDocument();
        BinkStroke stroke = TestData.Stroke(new Vector2(0, 0), new Vector2(10, 10));
        document.AddElement(stroke);
        var frames = Enumerable.Range(1, 3)
            .Select(i => (BinkDrawingOperation)new BinkReplaceElementOperation(stroke with { Transform = Matrix3x2.CreateTranslation(i * 10, 0) }))
            .ToList();

        BinkDrawingOperation? reported = null;
        document.Changed += (_, e) => reported = e.Operation;
        document.Apply(new BinkBatchOperation(frames, "Move"));

        List<int> versions = ReportedVersions(reported!);
        Assert.AreEqual(3, versions.Count);
        Assert.AreEqual(versions.Distinct().Count(), versions.Count);
        Assert.AreEqual(document.GetElement(stroke.Id).Version, versions[^1]);
    }

    [TestMethod]
    public void A_drag_is_one_replacement_per_element_however_many_frames_it_took()
    {
        // Undoing a drag replayed every frame of it. A two-second drag of a large selection made undo take seconds
        // and kept every frame's elements alive in the history.
        var document = new BinkDocument();
        using var history = new BinkHistoryManager(document);
        BinkStroke a = TestData.Stroke(new Vector2(0, 0), new Vector2(10, 10));
        BinkStroke b = TestData.Stroke(new Vector2(20, 0), new Vector2(30, 10));
        document.AddElement(a);
        document.AddElement(b);
        using (history.BeginGroup("Move"))
        {
            for (int i = 1; i <= 100; i++)
            {
                Matrix3x2 m = Matrix3x2.CreateTranslation(i, 0);
                document.ReplaceElements([a with { Transform = m }, b with { Transform = m }], "Move");
            }
        }

        BinkHistoryEntry entry = history.UndoStack[^1];
        Assert.AreEqual("Move", entry.Label);
        Assert.AreEqual(2, ((BinkBatchOperation)entry.Forward).Operations.Count);
        Assert.AreEqual(2, ((BinkBatchOperation)entry.Inverse).Operations.Count);

        Assert.IsTrue(history.Undo());
        Assert.AreEqual(Matrix3x2.Identity, document.GetElement(a.Id).Transform);
        Assert.AreEqual(Matrix3x2.Identity, document.GetElement(b.Id).Transform);
        Assert.IsTrue(history.Redo());
        Assert.AreEqual(Matrix3x2.CreateTranslation(100, 0), document.GetElement(a.Id).Transform);
        Assert.AreEqual(Matrix3x2.CreateTranslation(100, 0), document.GetElement(b.Id).Transform);
    }

    [TestMethod]
    public void A_group_that_adds_or_removes_is_kept_as_recorded()
    {
        // Only replacements commute into one per element. A group that also adds an element replays in order.
        var document = new BinkDocument();
        using var history = new BinkHistoryManager(document);
        BinkStroke a = TestData.Stroke(new Vector2(0, 0), new Vector2(10, 10));
        document.AddElement(a);
        BinkStroke added = TestData.Stroke(new Vector2(40, 0), new Vector2(50, 10));
        using (history.BeginGroup("Edit"))
        {
            for (int i = 1; i <= 40; i++) document.ReplaceElement(document.GetElement(a.Id) with { Transform = Matrix3x2.CreateTranslation(i, 0) });
            document.AddElement(added);
            document.ReplaceElement(document.GetElement(added.Id) with { Description = "moved" });
        }

        Assert.IsTrue(history.Undo());
        Assert.IsFalse(document.ContainsElement(added.Id));
        Assert.AreEqual(Matrix3x2.Identity, document.GetElement(a.Id).Transform);
        Assert.IsTrue(history.Redo());
        Assert.AreEqual("moved", document.GetElement(added.Id).Description);
        Assert.AreEqual(Matrix3x2.CreateTranslation(40, 0), document.GetElement(a.Id).Transform);
    }

    [TestMethod]
    public void Deleting_a_selection_in_any_order_undoes_to_the_original_order()
    {
        var document = new BinkDocument();
        using var history = new BinkHistoryManager(document);
        BinkLayer second = document.AddLayer("Second");
        var first = new List<Guid>();
        var other = new List<Guid>();
        for (int i = 0; i < 12; i++)
        {
            BinkStroke s = TestData.Stroke(new Vector2(i, 0), new Vector2(i + 5, 5));
            document.AddElement(s, i % 3 == 0 ? second.Id : document.Layers[0].Id);
            (i % 3 == 0 ? other : first).Add(s.Id);
        }
        List<Guid> before = document.AllElements.Select(e => e.Id).ToList();

        var random = new Random(5);
        List<Guid> doomed = before.Where((_, i) => i % 2 == 0).OrderBy(_ => random.Next()).ToList();
        document.RemoveElements(doomed);
        Assert.IsTrue(doomed.All(id => !document.ContainsElement(id)));

        Assert.IsTrue(history.Undo());
        CollectionAssert.AreEqual(before, document.AllElements.Select(e => e.Id).ToList());
        foreach (Guid id in first) Assert.AreEqual(document.Layers[0].Id, document.GetLayerOf(id)!.Id);
        foreach (Guid id in other) Assert.AreEqual(second.Id, document.GetLayerOf(id)!.Id);
    }

    [TestMethod]
    public void Element_lookups_stay_correct_through_inserts_removals_and_reorders()
    {
        // The layer keeps a position index; every structural change must leave lookups exact.
        var document = new BinkDocument();
        Guid layer = document.Layers[0].Id;
        var random = new Random(11);
        var expected = new List<Guid>();
        for (int step = 0; step < 400; step++)
        {
            int action = random.Next(4);
            if (action == 0 || expected.Count < 3)
            {
                BinkStroke s = TestData.Stroke(new Vector2(step, 0), new Vector2(step + 1, 1));
                int at = random.Next(expected.Count + 1);
                document.Apply(new BinkAddElementOperation(layer, s, at));
                expected.Insert(at, s.Id);
            }
            else if (action == 1)
            {
                int at = random.Next(expected.Count);
                document.RemoveElement(expected[at]);
                expected.RemoveAt(at);
            }
            else if (action == 2)
            {
                int from = random.Next(expected.Count);
                int to = random.Next(expected.Count);
                Guid id = expected[from];
                document.Apply(new BinkReorderElementOperation(id, layer, to));
                expected.RemoveAt(from);
                expected.Insert(Math.Clamp(to, 0, expected.Count), id);
            }
            else
            {
                Guid id = expected[random.Next(expected.Count)];
                document.ReplaceElement(document.GetElement(id) with { Description = "v" + step });
            }

            Guid probe = expected[random.Next(expected.Count)];
            Assert.AreEqual(expected.IndexOf(probe), document.Layers[0].IndexOf(probe));
            Assert.AreEqual(-1, document.Layers[0].IndexOf(Guid.NewGuid()));
        }
        CollectionAssert.AreEqual(expected, document.Layers[0].Elements.Select(e => e.Id).ToList());
        foreach (Guid id in expected) Assert.AreEqual(expected.IndexOf(id), document.Layers[0].IndexOf(id));
    }

    [TestMethod]
    public void An_element_at_the_highest_version_does_not_wrap_to_a_negative_one()
    {
        var document = new BinkDocument();
        BinkStroke stroke = TestData.Stroke(new Vector2(0, 0), new Vector2(10, 10)) with { Version = int.MaxValue };
        document.AddElement(stroke);

        document.ReplaceElement(document.GetElement(stroke.Id) with { Description = "edited" });

        Assert.AreEqual(int.MaxValue, document.GetElement(stroke.Id).Version);
    }
}
