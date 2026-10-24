using System.Numerics;
using Bit.Bink.Tests.Components;
using Bunit;
using Microsoft.JSInterop;

namespace Bit.Bink.Tests;

/// <summary>
/// Defects found by reviewing the engine: a late text commit landing on another element, selections left on locked or
/// hidden layers, cancelled erases that stayed, and hit testing under a stretching transform.
/// </summary>
[TestClass]
public sealed class InteractionRegressionTests
{
    private static BinkInputSample Mouse(BinkPointerPhase phase, float x, float y, double time = 0) => new()
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

    [TestMethod]
    public void A_late_commit_saves_its_text_on_the_element_it_was_typed_for()
    {
        // The browser measures the committed text before it is saved, a round trip; a click on another text element
        // can arrive meanwhile and open its editor. The first text used to be saved onto the second element.
        using var context = new TestToolContext();
        var existing = new BinkTextElement(new Vector2(400, 300), "keep me");
        context.Document.AddElement(existing);
        context.History.Clear();
        var tool = new BinkTextTool();
        int finished = 0;
        tool.EditFinished += () => finished++;

        tool.OnPointerDown(context, Mouse(BinkPointerPhase.Down, 100, 100));
        Guid first = tool.Editing!.Id;
        // The click on the existing element arrives before the first commit does.
        tool.OnPointerDown(context, Mouse(BinkPointerPhase.Down, 410, 305));
        Assert.AreEqual(existing.Id, tool.Editing!.Id);
        int finishedBefore = finished;

        tool.CommitEdit(context, first, "hello");

        Assert.AreEqual("hello", ((BinkTextElement)context.Document.GetElement(first)).Text);
        Assert.AreEqual("keep me", ((BinkTextElement)context.Document.GetElement(existing.Id)).Text);
        Assert.AreEqual(existing.Id, tool.Editing?.Id, "The editor open now stays open.");
        Assert.AreEqual(finishedBefore, finished, "Only the edit in progress closes the editor.");
        Assert.AreEqual(1, context.History.UndoCount, "Placing and typing the first box is one step.");

        tool.CommitEdit(context, existing.Id, "edited");
        Assert.AreEqual("edited", ((BinkTextElement)context.Document.GetElement(existing.Id)).Text);
        Assert.IsNull(tool.Editing);
    }

    [TestMethod]
    public void The_selection_drops_what_its_layer_locks_or_hides_and_refuses_it()
    {
        var document = new BinkDocument();
        BinkLayer layer = document.Layers[0];
        BinkStroke stroke = TestData.Stroke(Vector2.Zero, new Vector2(50, 50));
        document.AddElement(stroke, layer.Id);
        using var selection = new BinkSelection(document);

        selection.Set(stroke.Id);
        document.UpdateLayer(layer.Id, p => p with { IsLocked = true });
        Assert.IsTrue(selection.IsEmpty, "Locking the layer drops the selection.");
        selection.Set(stroke.Id);
        selection.Add(stroke.Id);
        Assert.IsTrue(selection.IsEmpty, "Nothing on a locked layer can be selected.");

        document.UpdateLayer(layer.Id, p => p with { IsLocked = false });
        selection.Set(stroke.Id);
        document.UpdateLayer(layer.Id, p => p with { IsVisible = false });
        Assert.IsTrue(selection.IsEmpty, "Hiding the layer drops the selection.");
    }

    [TestMethod]
    public void Delete_after_locking_the_layer_deletes_nothing()
    {
        using var context = new TestToolContext();
        BinkStroke stroke = TestData.Stroke(new Vector2(100, 100), new Vector2(200, 200));
        context.Document.AddElement(stroke);
        var tool = new BinkSelectTool();
        context.Selection.Set(stroke.Id);
        context.Document.UpdateLayer(context.Document.Layers[0].Id, p => p with { IsLocked = true });

        tool.OnKeyDown(context, new BinkToolKeyEvent("Delete", false, false, false));

        Assert.IsTrue(context.Document.ContainsElement(stroke.Id));
    }

    [TestMethod]
    public void A_cancelled_erase_puts_back_what_it_erased_and_leaves_no_undo_step()
    {
        foreach (BinkEraserMode mode in new[] { BinkEraserMode.Stroke, BinkEraserMode.Segment })
        {
            using var context = new TestToolContext();
            BinkStroke a = TestData.Stroke(new Vector2(100, 100), new Vector2(300, 100));
            BinkStroke b = TestData.Stroke(new Vector2(100, 150), new Vector2(300, 150));
            context.Document.AddElement(a);
            context.Document.AddElement(b);
            context.History.Clear();
            var eraser = new BinkEraserTool { Mode = mode, Radius = 8f };

            // A palm sweeps across both strokes, then the pen lands and the palm's contact is cancelled.
            eraser.OnPointerDown(context, Mouse(BinkPointerPhase.Down, 200, 80));
            eraser.OnPointerMove(context, Mouse(BinkPointerPhase.Move, 200, 170, 16));
            Assert.AreNotEqual(2, context.Document.AllElements.Count(e => ReferenceEquals(e, a) || ReferenceEquals(e, b)), mode + ": the sweep erased");
            eraser.OnPointerCancel(context, Mouse(BinkPointerPhase.Cancel, 200, 170, 32));

            Assert.AreEqual(2, context.Document.ElementCount, mode.ToString());
            Assert.AreSame(a.Points, ((BinkStroke)context.Document.GetElement(a.Id)).Points, mode.ToString());
            Assert.AreSame(b.Points, ((BinkStroke)context.Document.GetElement(b.Id)).Points, mode.ToString());
            Assert.AreEqual(0, context.History.UndoCount, mode + ": a cancelled gesture is not an undo step");
            Assert.IsFalse(eraser.IsErasing);
        }
    }

    [TestMethod]
    public void An_erase_interrupted_by_the_canvas_is_cancelled_too()
    {
        using var context = new TestToolContext();
        BinkStroke a = TestData.Stroke(new Vector2(100, 100), new Vector2(300, 100));
        context.Document.AddElement(a);
        context.History.Clear();
        var eraser = new BinkEraserTool { Mode = BinkEraserMode.Stroke, Radius = 8f };

        eraser.OnPointerDown(context, Mouse(BinkPointerPhase.Down, 200, 100));
        Assert.AreEqual(0, context.Document.ElementCount);
        eraser.Deactivate(context);

        Assert.AreEqual(1, context.Document.ElementCount);
        Assert.AreEqual(0, context.History.UndoCount);
    }

    [TestMethod]
    public void A_completed_erase_is_still_one_undo_step()
    {
        using var context = new TestToolContext();
        context.Document.AddElement(TestData.Stroke(new Vector2(100, 100), new Vector2(300, 100)));
        context.History.Clear();
        context.Line(new BinkEraserTool { Mode = BinkEraserMode.Stroke, Radius = 8f }, new Vector2(200, 50), new Vector2(200, 150));
        Assert.AreEqual(0, context.Document.ElementCount);
        Assert.AreEqual(1, context.History.UndoCount);
    }

    [TestMethod]
    public void Hit_testing_a_stretched_stroke_measures_the_tolerance_in_the_document()
    {
        // A horizontal stroke 2 units wide stretched ten times vertically: its ink reaches 10 units either side of the
        // centerline, and a 5-unit tolerance has to reach 15, not the 25 that converting the tolerance by the
        // transform's average scale gave.
        var stroke = new BinkStroke(BinkStrokePoints.FromPositions([new Vector2(0, 0), new Vector2(100, 0)]), new BinkMarkerBrush { Width = 2f })
        {
            Transform = Matrix3x2.CreateScale(1f, 10f),
        };
        Assert.IsTrue(stroke.HitTest(new Vector2(50, 14), 5f));
        Assert.IsFalse(stroke.HitTest(new Vector2(50, 25), 5f));
        // Along the stroke nothing is stretched: just past the end cap is still within the tolerance.
        Assert.IsTrue(stroke.HitTest(new Vector2(103, 0), 5f));
        Assert.IsFalse(stroke.HitTest(new Vector2(108, 0), 5f));

        // A marquee touching the stretched ink selects it.
        Assert.IsTrue(stroke.IntersectsRect(new BinkRect(40, 5, 20, 25)));
        Assert.IsFalse(stroke.IntersectsRect(new BinkRect(40, 12, 20, 25)));
    }

    [TestMethod]
    public void Hit_testing_a_stretched_shape_outline_measures_it_in_the_document()
    {
        var rect = new BinkRectangleShape(new BinkRect(0, 0, 100, 10)) { Style = new BinkShapeStyle { StrokeWidth = 2f }, Transform = Matrix3x2.CreateScale(1f, 10f) };
        // An outline is hit within 1.5 local units of its centre line at the least; the stretch makes that 15 above
        // and below the top edge, and the tolerance adds 4.
        Assert.IsTrue(rect.HitTest(new Vector2(50, -17), 4f));
        Assert.IsFalse(rect.HitTest(new Vector2(50, -20), 4f));
        // The empty middle of the unfilled rectangle is not a hit.
        Assert.IsFalse(rect.HitTest(new Vector2(50, 50), 4f));

        var line = new BinkLineShape(new Vector2(0, 0), new Vector2(100, 0)) { Style = new BinkShapeStyle { StrokeWidth = 2f }, Transform = Matrix3x2.CreateScale(1f, 10f) };
        Assert.IsTrue(line.HitTest(new Vector2(50, 22), 4f));
        Assert.IsFalse(line.HitTest(new Vector2(50, 26), 4f));
    }

    [TestMethod]
    public void An_even_scale_converts_the_tolerance_exactly_as_before()
    {
        var stroke = new BinkStroke(BinkStrokePoints.FromPositions([new Vector2(0, 0), new Vector2(100, 0)]), new BinkMarkerBrush { Width = 2f })
        {
            Transform = Matrix3x2.CreateScale(3f) * Matrix3x2.CreateRotation(0.5f),
        };
        Vector2 onInk = Vector2.Transform(new Vector2(50, 0), stroke.Transform);
        Vector2 normal = Vector2.Normalize(Vector2.TransformNormal(new Vector2(0, 1), stroke.Transform));
        Assert.IsTrue(stroke.HitTest(onInk + normal * 7.9f, 5f));
        Assert.IsFalse(stroke.HitTest(onInk + normal * 8.2f, 5f));
        Assert.IsTrue(BinkGeometryMath.IsConformal(stroke.Transform));
        Assert.IsFalse(BinkGeometryMath.IsConformal(Matrix3x2.CreateScale(1f, 2f)));
        Assert.AreEqual(10f, BinkGeometryMath.MaxScale(Matrix3x2.CreateScale(1f, 10f) * Matrix3x2.CreateRotation(1f)), 1e-3f);
    }
}

/// <summary>Defects found by reviewing the browser layer's contract with the component.</summary>
[TestClass]
public sealed class BrowserContractRegressionTests : BunitContext
{
    private const string ModulePath = "./_content/Bit.Bink/bit-bink.js";

    public BrowserContractRegressionTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private BunitJSModuleInterop SetupAttachedModule()
    {
        BunitJSModuleInterop module = JSInterop.SetupModule(ModulePath);
        module.Mode = JSRuntimeMode.Loose;
        module.Setup<BinkAttachResult?>("attach", _ => true).SetResult(new BinkAttachResult { Width = 400, Height = 300, DevicePixelRatio = 1 });
        return module;
    }

    private static BinkInputSample Sample(int pointer, BinkPointerKind kind, BinkPointerPhase phase, float x, float y, bool coalesced = false, bool predicted = false) => new()
    {
        PointerId = pointer,
        Kind = kind,
        Phase = phase,
        X = x,
        Y = y,
        Pressure = 0.5f,
        Buttons = phase == BinkPointerPhase.Up ? 0 : 1,
        IsPrimary = pointer == 1,
        IsCoalesced = coalesced,
        IsPredicted = predicted,
    };

    private static void Send(IRenderedComponent<BinkCanvas> canvas, params BinkInputSample[] samples)
    {
        var data = new double[samples.Length * BinkInputSample.PackedSize];
        for (int i = 0; i < samples.Length; i++) samples[i].WriteTo(data.AsSpan(i * BinkInputSample.PackedSize, BinkInputSample.PackedSize));
        canvas.InvokeAsync(() => ((IBinkInteropClient)canvas.Instance).OnSamples(data, samples.Length));
    }

    [TestMethod]
    public void A_select_drag_moves_with_the_measured_samples_when_predictions_follow_them()
    {
        // Every sample of a coalesced list was flagged as coalesced and predictions were appended after them, so a
        // tool that skips intermediate samples skipped all of them and the selection did not move until release.
        IRenderedComponent<BinkCanvas> canvas = Render<BinkCanvas>(p => p.Add(c => c.Tool, new BinkSelectTool()));
        var shape = new BinkRectangleShape(new BinkRect(100, 100, 50, 50)) { Style = new BinkShapeStyle { FillColor = BinkColor.White } };
        canvas.InvokeAsync(() => canvas.Instance.CurrentDocument.AddElement(shape));

        Send(canvas, Sample(1, BinkPointerKind.Mouse, BinkPointerPhase.Down, 120, 120));
        Send(canvas,
            Sample(1, BinkPointerKind.Mouse, BinkPointerPhase.Move, 140, 120, coalesced: true),
            Sample(1, BinkPointerKind.Mouse, BinkPointerPhase.Move, 160, 120, coalesced: true),
            Sample(1, BinkPointerKind.Mouse, BinkPointerPhase.Move, 170, 120, predicted: true));

        Assert.AreEqual(139f, canvas.Instance.CurrentDocument.GetElement(shape.Id).Bounds.Left, 2f, "The last measured move is applied.");
    }

    [TestMethod]
    public void A_pinch_follows_both_fingers_when_their_samples_share_a_batch()
    {
        IRenderedComponent<BinkCanvas> canvas = Render<BinkCanvas>(p => p
            .Add(c => c.Tool, new BinkSelectTool())
            .Add(c => c.EnableTouchGestures, true));

        Send(canvas,
            Sample(1, BinkPointerKind.Touch, BinkPointerPhase.Down, 100, 100),
            Sample(2, BinkPointerKind.Touch, BinkPointerPhase.Down, 200, 100));
        Send(canvas,
            Sample(1, BinkPointerKind.Touch, BinkPointerPhase.Move, 90, 100, coalesced: true),
            Sample(1, BinkPointerKind.Touch, BinkPointerPhase.Move, 80, 100, coalesced: true),
            Sample(2, BinkPointerKind.Touch, BinkPointerPhase.Move, 210, 100, coalesced: true),
            Sample(2, BinkPointerKind.Touch, BinkPointerPhase.Move, 220, 100, coalesced: true));

        // The fingers went from 100 apart to 140 apart. Following only the second finger gave 1.2.
        Assert.AreEqual(1.4f, canvas.Instance.CurrentViewport.Zoom, 0.01f);
    }

    [TestMethod]
    public void The_provisional_trail_is_only_offered_for_tools_that_draw_ink()
    {
        BunitJSModuleInterop module = SetupAttachedModule();
        IRenderedComponent<BinkCanvas> canvas = Render<BinkCanvas>(p => p.Add(c => c.WetInk, true));
        canvas.WaitForAssertion(() => Assert.Contains(i => i.Identifier == "setWetInkStyle", module.Invocations));
        Assert.IsNotNull(module.Invocations.Last(i => i.Identifier == "setWetInkStyle").Arguments[1], "The pen draws a trail.");

        canvas.InvokeAsync(() => canvas.Instance.SetTool(new BinkSelectTool()));
        canvas.WaitForAssertion(() => Assert.IsNull(module.Invocations.Last(i => i.Identifier == "setWetInkStyle").Arguments[1]));

        canvas.InvokeAsync(() => canvas.Instance.SetTool(new BinkDrawTool()));
        canvas.WaitForAssertion(() => Assert.IsNotNull(module.Invocations.Last(i => i.Identifier == "setWetInkStyle").Arguments[1]));

        canvas.Render(p => p.Add(c => c.ReadOnly, true));
        canvas.WaitForAssertion(() => Assert.IsNull(module.Invocations.Last(i => i.Identifier == "setWetInkStyle").Arguments[1]));
    }

    [TestMethod]
    public async Task An_exported_image_is_read_from_a_stream()
    {
        // Returned as a byte array, an image over 32 KB is a single message Blazor Server refuses, closing the circuit.
        BunitJSModuleInterop module = SetupAttachedModule();
        byte[] large = new byte[200_000];
        large[0] = 0x89;
        var stream = new TestStreamReference(large);
        module.Setup<IJSStreamReference>("renderToImage", _ => true).SetResult(stream);
        IRenderedComponent<BinkCanvas> canvas = Render<BinkCanvas>();
        canvas.WaitForAssertion(() => Assert.Contains(i => i.Identifier == "execute", module.Invocations));

        byte[] png = await canvas.InvokeAsync(() => canvas.Instance.ExportPngAsync());

        Assert.HasCount(large.Length, png);
        Assert.IsTrue(stream.Disposed, "The browser stream is released once read.");
    }

    [TestMethod]
    public async Task A_long_text_arrives_as_a_stream_and_is_committed()
    {
        IRenderedComponent<BinkCanvas> canvas = Render<BinkCanvas>(p => p.Add(c => c.Tool, new BinkTextTool()));
        // Placing a box opens the editor, which is what a commit closes.
        Send(canvas, Sample(1, BinkPointerKind.Mouse, BinkPointerPhase.Down, 50, 50), Sample(1, BinkPointerKind.Mouse, BinkPointerPhase.Up, 50, 50));
        canvas.WaitForAssertion(() => Assert.IsNotNull(canvas.Find("textarea")));

        string text = new('x', 20_000);
        var callbacks = new BinkInterop.InteropCallbacks(canvas.Instance);
        await canvas.InvokeAsync(() => callbacks.OnTextEditorKeyStream("Enter", new TestStreamReference(System.Text.Encoding.UTF8.GetBytes(text))));

        canvas.WaitForAssertion(() => Assert.AreEqual(text, canvas.Instance.CurrentDocument.AllElements.OfType<BinkTextElement>().Single().Text));
    }

    [TestMethod]
    public async Task A_stream_over_the_limit_is_refused_as_too_large()
    {
        // The runtime refuses it with an ArgumentOutOfRangeException; every caller handles "too large" as an IOException.
        var stream = new TestStreamReference(new byte[100]);
        await Assert.ThrowsExactlyAsync<IOException>(() => BinkInterop.ReadBytesAsync(stream, 99));
        Assert.IsTrue(stream.Disposed);
    }

    [TestMethod]
    public async Task A_long_text_still_arriving_goes_to_the_box_it_was_typed_in()
    {
        IRenderedComponent<BinkCanvas> canvas = Render<BinkCanvas>(p => p.Add(c => c.Tool, new BinkTextTool()));
        var other = new BinkTextElement(new Vector2(400, 40), "Other") { Size = new Vector2(120, 30) };
        await canvas.InvokeAsync(() =>
        {
            canvas.Instance.CurrentDocument.AddElement(other);
            canvas.Instance.History.Clear();
        });
        Send(canvas, Sample(1, BinkPointerKind.Mouse, BinkPointerPhase.Down, 50, 50), Sample(1, BinkPointerKind.Mouse, BinkPointerPhase.Up, 50, 50));
        canvas.WaitForAssertion(() => Assert.IsNotNull(canvas.Find("textarea")));
        Guid typedIn = canvas.Instance.CurrentDocument.AllElements.OfType<BinkTextElement>().Single(e => e.Id != other.Id).Id;

        // The click on the other text blurs the editor, which sends the long text; while it is still being read, the
        // click itself opens the editor on the other text.
        string text = new('x', 20_000);
        var arrival = new TaskCompletionSource();
        var callbacks = new BinkInterop.InteropCallbacks(canvas.Instance);
        Task commit = canvas.InvokeAsync(() => callbacks.OnTextEditorKeyStream("Enter", new TestStreamReference(System.Text.Encoding.UTF8.GetBytes(text)) { Arrival = arrival.Task }));
        Send(canvas, Sample(1, BinkPointerKind.Mouse, BinkPointerPhase.Down, 420, 50), Sample(1, BinkPointerKind.Mouse, BinkPointerPhase.Up, 420, 50));
        canvas.WaitForAssertion(() => Assert.AreEqual(other.Id, ((IBinkInteropClient)canvas.Instance).TextEditorElementId));
        arrival.SetResult();
        await commit;

        canvas.WaitForAssertion(() => Assert.AreEqual(text, ((BinkTextElement)canvas.Instance.CurrentDocument.GetElement(typedIn)).Text));
        Assert.AreEqual("Other", ((BinkTextElement)canvas.Instance.CurrentDocument.GetElement(other.Id)).Text);
        Assert.AreEqual(other.Id, ((IBinkInteropClient)canvas.Instance).TextEditorElementId, "The editor now open is left alone.");
        Assert.AreEqual(1, canvas.Instance.History.UndoCount, "Placing the box and typing into it is one closed undo step.");
    }
}
