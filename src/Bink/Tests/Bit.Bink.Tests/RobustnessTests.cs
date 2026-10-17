using System.Numerics;

namespace Bit.Bink.Tests;

/// <summary>
/// Regression tests for defects found in review. Each one describes the failure it prevents, because a test
/// whose purpose is forgotten is a test that gets deleted.
/// </summary>
[TestClass]
public class GestureRobustnessTests
{
    [TestMethod]
    public void Switching_tools_mid_drag_does_not_disable_undo()
    {
        // A tool that opens a history group and is swapped out before closing it leaves the group open forever,
        // which silently swallows every later change and makes undo do nothing.
        using var context = new TestToolContext();
        var shape = new BinkRectangleShape(new BinkRect(50, 50, 80, 40)) { Style = new BinkShapeStyle { FillColor = BinkColor.Black } };
        context.Document.AddElement(shape);
        context.History.Clear();

        var select = new BinkSelectTool();
        context.Selection.Set(shape.Id);
        select.OnPointerDown(context, Down(70, 70));
        select.OnPointerMove(context, Move(140, 120));

        select.Deactivate(context);

        var draw = new BinkDrawTool();
        draw.Activate(context);
        context.Line(draw, new Vector2(10, 10), new Vector2(60, 60), 12);

        Assert.IsTrue(context.History.CanUndo);
        Assert.IsTrue(context.History.Undo());
    }

    [TestMethod]
    public void Switching_tools_mid_stroke_discards_the_stroke()
    {
        using var context = new TestToolContext();
        var draw = new BinkDrawTool();
        draw.OnPointerDown(context, Down(10, 10));
        draw.OnPointerMove(context, Move(60, 40));
        Assert.IsTrue(draw.IsDrawing);

        draw.Deactivate(context);

        Assert.IsFalse(draw.IsDrawing);
        Assert.AreEqual(0, context.Document.ElementCount);
    }

    [TestMethod]
    public void An_eraser_gesture_interrupted_by_deactivation_leaves_history_usable()
    {
        using var context = new TestToolContext();
        for (int i = 0; i < 3; i++)
        {
            context.Document.AddElement(new BinkStroke(TestData.Line(new Vector2(0, 20 + i * 30), new Vector2(300, 20 + i * 30), 40), new BinkPenBrush()));
        }
        context.History.Clear();

        var eraser = new BinkEraserTool { Mode = BinkEraserMode.Stroke, Radius = 15f };
        eraser.OnPointerDown(context, Down(100, 20));
        eraser.OnPointerMove(context, Move(150, 50));
        eraser.Deactivate(context);

        context.Document.AddElement(TestData.Stroke(new Vector2(0, 200), new Vector2(50, 220)));
        Assert.IsTrue(context.History.CanUndo);
    }

    [TestMethod]
    public void Changing_the_eraser_mode_mid_gesture_does_not_strand_it()
    {
        using var context = new TestToolContext();
        context.Document.AddElement(new BinkStroke(TestData.Line(new Vector2(0, 50), new Vector2(300, 50), 40), new BinkPenBrush()));
        context.History.Clear();

        var eraser = new BinkEraserTool { Mode = BinkEraserMode.Stroke, Radius = 15f };
        eraser.OnPointerDown(context, Down(100, 50));
        eraser.Mode = BinkEraserMode.Pixel;      // the application changes the setting while the pointer is down
        eraser.OnPointerUp(context, Up(120, 50));

        context.Document.AddElement(TestData.Stroke(new Vector2(0, 200), new Vector2(50, 220)));
        Assert.IsTrue(context.History.CanUndo);
    }

    [TestMethod]
    public void A_stroke_stays_on_the_layer_it_started_on()
    {
        using var context = new TestToolContext();
        BinkLayer second = context.Document.AddLayer("Second");
        context.ActiveLayerId = second.Id;

        var draw = new BinkDrawTool();
        draw.OnPointerDown(context, Down(10, 10));
        draw.OnPointerMove(context, Move(60, 40));

        // The application switches the active layer while the pen is still down.
        context.ActiveLayerId = context.Document.Layers[0].Id;
        draw.OnPointerUp(context, Up(100, 70));

        Assert.ContainsSingle(second.Elements);
        Assert.IsEmpty(context.Document.Layers[0].Elements);
    }

    [TestMethod]
    public void Shift_adds_to_the_selection_so_multi_select_works_without_a_mouse()
    {
        using var context = new TestToolContext();
        var first = new BinkRectangleShape(new BinkRect(20, 20, 40, 40)) { Style = new BinkShapeStyle { FillColor = BinkColor.Black } };
        var second = new BinkRectangleShape(new BinkRect(120, 20, 40, 40)) { Style = new BinkShapeStyle { FillColor = BinkColor.Black } };
        context.Document.AddElement(first);
        context.Document.AddElement(second);

        var tool = new BinkSelectTool();
        context.Gesture(tool, BinkPointerKind.Pen, (40, 40, 0.5f), (40, 40, 0.5f));
        Assert.AreEqual(1, context.Selection.Count);

        tool.OnPointerDown(context, Down(140, 40) with { Shift = true, Kind = BinkPointerKind.Pen });
        tool.OnPointerUp(context, Up(140, 40) with { Shift = true, Kind = BinkPointerKind.Pen });

        Assert.AreEqual(2, context.Selection.Count);
    }

    [TestMethod]
    public void Cancelling_a_drag_leaves_no_history_step()
    {
        using var context = new TestToolContext();
        var shape = new BinkRectangleShape(new BinkRect(50, 50, 60, 40)) { Style = new BinkShapeStyle { FillColor = BinkColor.Black } };
        context.Document.AddElement(shape);
        context.History.Clear();
        context.Selection.Set(shape.Id);

        var tool = new BinkSelectTool();
        tool.OnPointerDown(context, Down(70, 70));
        tool.OnPointerMove(context, Move(180, 160));
        tool.OnPointerCancel(context, new BinkInputSample { PointerId = 1, Phase = BinkPointerPhase.Cancel, X = 180, Y = 160, Timestamp = 32 });

        Assert.AreEqual(50f, context.Document.GetElement(shape.Id).Bounds.X, 2f);
        Assert.IsFalse(context.History.CanUndo);
    }

    [TestMethod]
    public void A_refused_change_does_not_escape_the_tool()
    {
        // Document limits throw. An exception here would unwind through the browser callback and strand the
        // gesture, so tools report the refusal instead.
        var document = new BinkDocument(BinkDocumentSettings.Default, BinkDocumentLimits.Default with { MaxElements = 1 });
        using var context = new TestToolContext(document);
        context.Document.AddElement(TestData.Stroke(new Vector2(0, 0), new Vector2(10, 10)));

        var draw = new BinkDrawTool();
        context.Line(draw, new Vector2(20, 20), new Vector2(80, 80), 12);

        Assert.AreEqual(1, context.Document.ElementCount);
        Assert.Contains(m => m.Contains("could not be applied", StringComparison.Ordinal), context.Announcements);
    }

    [TestMethod]
    public void Text_placement_and_typing_are_one_undo_step()
    {
        using var context = new TestToolContext();
        var tool = new BinkTextTool();

        tool.OnPointerDown(context, Down(40, 60));
        tool.CommitEdit(context, "Reviewed", new Vector2(80, 20));

        Assert.AreEqual(1, context.Document.ElementCount);
        Assert.AreEqual(1, context.History.UndoCount);

        context.History.Undo();
        Assert.AreEqual(0, context.Document.ElementCount);
    }

    [TestMethod]
    public void The_text_tool_refuses_a_locked_layer()
    {
        using var context = new TestToolContext();
        context.Document.UpdateLayer(context.ActiveLayerId, p => p with { IsLocked = true });

        var tool = new BinkTextTool();
        Assert.IsFalse(tool.OnPointerDown(context, Down(40, 60)));
        Assert.AreEqual(0, context.Document.ElementCount);
    }

    [TestMethod]
    public void Panning_does_not_modify_the_document()
    {
        Assert.IsFalse(new BinkPanTool().ModifiesDocument);
        Assert.IsTrue(new BinkDrawTool().ModifiesDocument);
        Assert.IsTrue(new BinkSelectTool().ModifiesDocument);
    }

    private static BinkInputSample Down(float x, float y) => new() { PointerId = 1, Phase = BinkPointerPhase.Down, X = x, Y = y, Pressure = 0.5f, Buttons = 1, IsPrimary = true };

    private static BinkInputSample Move(float x, float y) => new() { PointerId = 1, Phase = BinkPointerPhase.Move, X = x, Y = y, Pressure = 0.5f, Buttons = 1, Timestamp = 16, IsPrimary = true };

    private static BinkInputSample Up(float x, float y) => new() { PointerId = 1, Phase = BinkPointerPhase.Up, X = x, Y = y, Timestamp = 32, IsPrimary = true };
}

[TestClass]
public class HistoryRobustnessTests
{
    [TestMethod]
    public void An_undo_that_cannot_be_applied_keeps_the_step()
    {
        // A collaborator can delete the element a local undo step refers to. Losing the step silently would be
        // worse than refusing: the user's earlier work would become unreachable.
        var document = new BinkDocument();
        using var history = new BinkHistoryManager(document);
        BinkStroke first = TestData.Stroke(new Vector2(0, 0), new Vector2(10, 10));
        BinkStroke second = TestData.Stroke(new Vector2(20, 20), new Vector2(30, 30));
        document.AddElement(first);
        document.AddElement(second);

        // Remove the newest element as a remote change, so the local undo step for it can no longer apply.
        document.Apply(new BinkRemoveElementOperation(second.Id), BinkChangeOrigin.Remote);

        Assert.AreEqual(2, history.UndoCount);
        Assert.IsFalse(history.Undo());
        Assert.AreEqual(2, history.UndoCount);
    }

    [TestMethod]
    public void Disposing_closes_a_group_left_open()
    {
        var document = new BinkDocument();
        var history = new BinkHistoryManager(document);
        IDisposable group = history.BeginGroup("Interrupted");
        document.AddElement(TestData.Stroke(new Vector2(0, 0), new Vector2(10, 10)));

        history.Dispose();
        GC.KeepAlive(group);

        // The group was closed, so the change it captured became a real step rather than vanishing.
        Assert.AreEqual(1, history.UndoCount);
    }

    [TestMethod]
    public void EndOpenGroups_restores_a_usable_history()
    {
        var document = new BinkDocument();
        using var history = new BinkHistoryManager(document);
        history.BeginGroup("Abandoned");
        document.AddElement(TestData.Stroke(new Vector2(0, 0), new Vector2(10, 10)));
        Assert.IsFalse(history.CanUndo);

        history.EndOpenGroups();

        Assert.IsTrue(history.CanUndo);
        document.AddElement(TestData.Stroke(new Vector2(20, 20), new Vector2(30, 30)));
        Assert.AreEqual(2, history.UndoCount);
    }
}

[TestClass]
public class DocumentRobustnessTests
{
    [TestMethod]
    public void The_selection_can_be_iterated_while_deleting()
    {
        var document = new BinkDocument();
        using var selection = new BinkSelection(document);
        for (int i = 0; i < 5; i++) document.AddElement(TestData.Stroke(new Vector2(i * 20, 0), new Vector2(i * 20 + 10, 10)));
        selection.SelectAll();

        // The obvious consumer idiom. Handing out the live list would throw here.
        foreach (Guid id in selection.Ids) document.RemoveElement(id);

        Assert.IsTrue(document.IsEmpty);
    }

    [TestMethod]
    public void RemoveElements_tolerates_a_repeated_id()
    {
        var document = new BinkDocument();
        BinkStroke stroke = TestData.Stroke(new Vector2(0, 0), new Vector2(10, 10));
        document.AddElement(stroke);

        document.RemoveElements([stroke.Id, stroke.Id]);
        Assert.IsTrue(document.IsEmpty);
    }

    [TestMethod]
    public void Replacing_a_stroke_with_one_of_the_same_size_does_not_trip_the_point_limit()
    {
        var document = new BinkDocument(BinkDocumentSettings.Default, BinkDocumentLimits.Default with { MaxTotalPoints = 30 });
        var stroke = new BinkStroke(TestData.Line(new Vector2(0, 0), new Vector2(100, 100), 30), new BinkPenBrush());
        document.AddElement(stroke);

        // Recolouring is a replace of identical size and must be accepted, not refused for exceeding the total.
        document.ReplaceElement(stroke with { Brush = stroke.Brush with { Color = BinkColor.Parse("#f00") } });
        Assert.AreEqual(30, document.TotalPointCount);
    }

    [TestMethod]
    public void A_failed_replace_leaves_the_point_count_intact()
    {
        var document = new BinkDocument(BinkDocumentSettings.Default, BinkDocumentLimits.Default with { MaxPointsPerStroke = 40 });
        var stroke = new BinkStroke(TestData.Line(new Vector2(0, 0), new Vector2(50, 50), 20), new BinkPenBrush());
        document.AddElement(stroke);
        long before = document.TotalPointCount;

        Assert.ThrowsExactly<InvalidOperationException>(() =>
            document.ReplaceElement(stroke with { Points = TestData.Line(new Vector2(0, 0), new Vector2(50, 50), 100) }));

        Assert.AreEqual(before, document.TotalPointCount);
    }

    [TestMethod]
    public void A_zero_height_rectangle_still_intersects()
    {
        // A flat rectangle is a line, not "no rectangle". Treating it as empty would cull such elements from
        // rendering and hit testing.
        var line = new BinkRect(10, 50, 100, 0);
        Assert.IsTrue(line.Intersects(new BinkRect(0, 40, 200, 20)));
        Assert.IsFalse(BinkRect.Empty.Intersects(new BinkRect(0, 0, 10, 10)));
    }
}

[TestClass]
public class UntrustedInputTests
{
    [TestMethod]
    public void A_document_full_of_eraser_strokes_exports_without_exhausting_the_stack()
    {
        // Eraser strokes nest masks in SVG. A recursive implementation nested once per eraser, which a document
        // controls, and a stack overflow cannot be caught.
        var document = new BinkDocument(new BinkDocumentSettings { Width = 200, Height = 200 });
        Guid layer = document.Layers[0].Id;
        document.AddElement(new BinkStroke(TestData.Line(new Vector2(0, 100), new Vector2(200, 100), 10), new BinkPenBrush()), layer);
        using (document.BeginBatch("Erasers"))
        {
            for (int i = 0; i < 5000; i++)
            {
                document.AddElement(new BinkStroke(TestData.Line(new Vector2(i % 200, 0), new Vector2(i % 200, 200), 4), new BinkPixelEraserBrush()), layer);
            }
        }

        string svg = BinkSvgExporter.Export(document);
        Assert.StartsWith("<svg", svg, StringComparison.Ordinal);
        Assert.EndsWith("</svg>", svg, StringComparison.Ordinal);
    }

    [TestMethod]
    public void A_hostile_id_prefix_cannot_break_out_of_an_attribute()
    {
        var document = new BinkDocument(new BinkDocumentSettings { Width = 100, Height = 100 });
        document.AddElement(new BinkStroke(TestData.Line(new Vector2(0, 50), new Vector2(100, 50), 10), new BinkPenBrush()));
        document.AddElement(new BinkStroke(TestData.Line(new Vector2(50, 0), new Vector2(50, 100), 10), new BinkPixelEraserBrush()));

        string svg = BinkSvgExporter.Export(document, new BinkSvgExportOptions { IdPrefix = "a\" onload=\"alert(1)" });
        Assert.DoesNotContain("onload", svg, StringComparison.OrdinalIgnoreCase);
    }

    [TestMethod]
    public void A_document_beyond_its_limits_is_reported_as_malformed()
    {
        // TryDeserialize promises not to throw. Limit violations used to surface as InvalidOperationException.
        var document = new BinkDocument();
        for (int i = 0; i < 20; i++) document.AddElement(TestData.Stroke(new Vector2(i * 10, 0), new Vector2(i * 10 + 5, 5)));
        string json = BinkSerializer.Serialize(document);

        var options = new BinkDeserializerOptions { Limits = BinkDocumentLimits.Default with { MaxElements = 5 } };
        Assert.ThrowsExactly<BinkFormatException>(() => BinkSerializer.Deserialize(json, options));
        Assert.IsFalse(BinkSerializer.TryDeserialize(json, out _, out string? error, options));
        Assert.IsFalse(string.IsNullOrWhiteSpace(error));
    }

    [TestMethod]
    public void A_highlight_with_too_many_rectangles_is_rejected()
    {
        var rects = string.Join(',', Enumerable.Range(0, 200_000).Select(i => $"[{i},0,4,10]"));
        string json = $$"""
        {"format":"bink","version":1,"layers":[{"id":"11111111-1111-4111-8111-111111111111","name":"L","elements":[
          {"type":"highlight","id":"22222222-2222-4222-8222-222222222222","rects":[{{rects}}]}
        ]}]}
        """;
        Assert.ThrowsExactly<BinkFormatException>(() => BinkSerializer.Deserialize(json));
    }

    [TestMethod]
    public void A_highlight_with_non_finite_coordinates_is_sanitized()
    {
        string json = """
        {"format":"bink","version":1,"layers":[{"id":"11111111-1111-4111-8111-111111111111","name":"L","elements":[
          {"type":"highlight","id":"22222222-2222-4222-8222-222222222222","rects":[[1e39,1e39,1e39,1e39]]}
        ]}]}
        """;
        BinkDocument document = BinkSerializer.Deserialize(json);
        foreach (BinkElement element in document.AllElements)
        {
            BinkRect bounds = element.Bounds;
            Assert.IsTrue(float.IsFinite(bounds.X) && float.IsFinite(bounds.Width));
        }
    }

    [TestMethod]
    public void Point_data_that_claims_more_points_than_it_contains_is_rejected()
    {
        // Version 1, no flags, quantization 64, a large count and almost no payload.
        var bytes = new List<byte> { 1, 0, 64 };
        uint count = 100_000;
        while (count >= 0x80) { bytes.Add((byte)(count | 0x80)); count >>= 7; }
        bytes.Add((byte)count);
        bytes.AddRange(Enumerable.Repeat((byte)1, 10));

        Assert.ThrowsExactly<FormatException>(() => BinkPointCodec.Decode(bytes.ToArray()));
    }

    [TestMethod]
    [DataRow(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x7F, 0xFF, 0xFF, 0xF8, 0x49, 0x48, 0x44, 0x52 })]
    public void A_png_with_a_hostile_chunk_length_is_rejected(byte[] data)
    {
        // The length is close to int.MaxValue; the bounds check must not overflow into a passing comparison.
        Assert.ThrowsExactly<FormatException>(() => BinkPngCodec.Decode(data));
    }

    [TestMethod]
    public void A_png_with_corrupt_compressed_data_is_reported_as_a_format_error()
    {
        byte[] valid = TestData.CreatePng(8, 8, (_, _) => BinkColor.Black);
        byte[] corrupt = valid.ToArray();
        // Damage the middle of the file, which is inside the compressed image data.
        for (int i = valid.Length / 2; i < valid.Length / 2 + 8 && i < corrupt.Length; i++) corrupt[i] ^= 0xFF;

        Assert.ThrowsExactly<FormatException>(() => BinkPngCodec.Decode(corrupt));
    }

    [TestMethod]
    public void An_undecodable_embedded_image_is_reported_rather_than_failing_the_export()
    {
        var document = new BinkDocument(new BinkDocumentSettings { Width = 50, Height = 50 });
        byte[] valid = TestData.CreatePng(8, 8, (_, _) => BinkColor.Black);
        byte[] corrupt = valid.ToArray();
        for (int i = valid.Length / 2; i < valid.Length / 2 + 8 && i < corrupt.Length; i++) corrupt[i] ^= 0xFF;
        document.AddElement(new BinkImageElement(BinkImageSource.FromBytes(corrupt, "image/png", 8, 8), new BinkRect(0, 0, 50, 50)));

        BinkPngExportResult result = BinkPngExporter.ExportWithDetails(document);
        Assert.AreEqual(1, result.SkippedImages);
        Assert.IsTrue(result.Bytes.Length > 0);
    }
}
