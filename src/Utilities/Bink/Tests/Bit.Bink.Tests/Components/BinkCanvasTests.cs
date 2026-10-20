using System.Numerics;
using AngleSharp.Dom;
using Bunit;
using Microsoft.Extensions.DependencyInjection;

namespace Bit.Bink.Tests.Components;

/// <summary>
/// Component-level tests. They render the real <see cref="BinkCanvas"/> with a stubbed JavaScript runtime, so the
/// wiring between parameters, the document, history and selection is covered without a browser.
/// </summary>
[TestClass]
public class BinkCanvasTests : BunitContext
{
    public BinkCanvasTests()
    {
        // The canvas talks to a JavaScript module; loose mode answers those calls with defaults.
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [TestMethod]
    public void Renders_the_two_canvas_layers_and_the_input_surface()
    {
        IRenderedComponent<BinkCanvas> canvas = Render<BinkCanvas>();

        Assert.IsNotNull(canvas.Find("canvas[data-bink-layer='document']"));
        Assert.IsNotNull(canvas.Find("canvas[data-bink-layer='overlay']"));
        Assert.IsNotNull(canvas.Find(".bit-bink-input"));
    }

    [TestMethod]
    public void Exposes_an_accessible_role_name_and_description()
    {
        IRenderedComponent<BinkCanvas> canvas = Render<BinkCanvas>(p => p.Add(c => c.AriaLabel, "Signature"));

        IElement container = canvas.Find(".bit-bink-canvas");
        Assert.AreEqual("application", container.GetAttribute("role"));
        Assert.AreEqual("Signature", container.GetAttribute("aria-label"));
        Assert.Contains("Empty drawing canvas", canvas.Markup, StringComparison.Ordinal);
    }

    [TestMethod]
    public void Creates_its_own_document_when_none_is_supplied()
    {
        IRenderedComponent<BinkCanvas> canvas = Render<BinkCanvas>();
        Assert.IsNotNull(canvas.Instance.CurrentDocument);
        Assert.ContainsSingle(canvas.Instance.CurrentDocument.Layers);
    }

    [TestMethod]
    public void Uses_the_supplied_document()
    {
        var document = new BinkDocument();
        IRenderedComponent<BinkCanvas> canvas = Render<BinkCanvas>(p => p.Add(c => c.Document, document));
        Assert.AreSame(document, canvas.Instance.CurrentDocument);
    }

    [TestMethod]
    public void Records_history_for_changes_made_through_the_document()
    {
        IRenderedComponent<BinkCanvas> canvas = Render<BinkCanvas>();
        BinkDocument document = canvas.Instance.CurrentDocument;

        document.AddElement(new BinkStroke(TestPoints(), new BinkPenBrush()));

        Assert.AreEqual(1, canvas.Instance.History.UndoCount);
        Assert.IsTrue(canvas.Instance.History.CanUndo);
    }

    [TestMethod]
    public void Undo_and_redo_work_through_the_component_api()
    {
        IRenderedComponent<BinkCanvas> canvas = Render<BinkCanvas>();
        BinkDocument document = canvas.Instance.CurrentDocument;
        document.AddElement(new BinkStroke(TestPoints(), new BinkPenBrush()));

        Assert.IsTrue(canvas.Instance.Undo());
        Assert.AreEqual(0, document.ElementCount);

        Assert.IsTrue(canvas.Instance.Redo());
        Assert.AreEqual(1, document.ElementCount);
    }

    [TestMethod]
    public void The_history_limit_parameter_is_applied()
    {
        IRenderedComponent<BinkCanvas> canvas = Render<BinkCanvas>(p => p.Add(c => c.HistoryLimit, 3));
        BinkDocument document = canvas.Instance.CurrentDocument;

        for (int i = 0; i < 10; i++) document.AddElement(new BinkStroke(TestPoints(i * 10), new BinkPenBrush()));

        Assert.AreEqual(3, canvas.Instance.History.UndoCount);
    }

    [TestMethod]
    public void The_accessible_description_follows_the_document()
    {
        IRenderedComponent<BinkCanvas> canvas = Render<BinkCanvas>();
        canvas.Instance.CurrentDocument.AddElement(new BinkStroke(TestPoints(), new BinkPenBrush()) { Description = "A test stroke" });
        canvas.Render(p => { });

        Assert.Contains("Drawing with 1 elements", canvas.Markup, StringComparison.Ordinal);
        Assert.Contains("A test stroke", canvas.Markup, StringComparison.Ordinal);
    }

    [TestMethod]
    public void Clear_empties_the_document_in_one_undoable_step()
    {
        IRenderedComponent<BinkCanvas> canvas = Render<BinkCanvas>();
        BinkDocument document = canvas.Instance.CurrentDocument;
        for (int i = 0; i < 3; i++) document.AddElement(new BinkStroke(TestPoints(i * 20), new BinkPenBrush()));

        canvas.Instance.Clear();
        Assert.IsTrue(document.IsEmpty);

        Assert.IsTrue(canvas.Instance.Undo());
        Assert.AreEqual(3, document.ElementCount);
    }

    [TestMethod]
    public void ReadOnly_blocks_editing_commands()
    {
        IRenderedComponent<BinkCanvas> canvas = Render<BinkCanvas>(p => p.Add(c => c.ReadOnly, true));
        canvas.Instance.CurrentDocument.AddElement(new BinkStroke(TestPoints(), new BinkPenBrush()));

        canvas.Instance.Clear();
        Assert.AreEqual(1, canvas.Instance.CurrentDocument.ElementCount);
        Assert.IsFalse(canvas.Instance.Undo());
    }

    [TestMethod]
    public void Raises_DocumentChanged_for_the_host()
    {
        var changes = new List<BinkDocumentChangedEventArgs>();
        IRenderedComponent<BinkCanvas> canvas = Render<BinkCanvas>(p => p
            .Add(c => c.DocumentChanged, args => changes.Add(args)));

        canvas.Instance.CurrentDocument.AddElement(new BinkStroke(TestPoints(), new BinkPenBrush()));

        Assert.ContainsSingle(changes);
        Assert.AreEqual(BinkChangeOrigin.Local, changes[0].Origin);
    }

    [TestMethod]
    public void History_is_already_updated_when_DocumentChanged_is_raised()
    {
        // A host that re-renders from this callback must see the new undo state, not the previous one.
        int undoCountAtNotification = -1;
        IRenderedComponent<BinkCanvas> canvas = null!;
        canvas = Render<BinkCanvas>(p => p
            .Add(c => c.DocumentChanged, _ => undoCountAtNotification = canvas.Instance.History.UndoCount));

        canvas.Instance.CurrentDocument.AddElement(new BinkStroke(TestPoints(), new BinkPenBrush()));

        Assert.AreEqual(1, undoCountAtNotification);
    }

    [TestMethod]
    public void Selection_is_already_updated_when_DocumentChanged_is_raised()
    {
        int selectionAtNotification = -1;
        IRenderedComponent<BinkCanvas> canvas = null!;
        canvas = Render<BinkCanvas>(p => p
            .Add(c => c.DocumentChanged, _ => selectionAtNotification = canvas.Instance.Selection.Count));

        var stroke = new BinkStroke(TestPoints(), new BinkPenBrush());
        canvas.Instance.CurrentDocument.AddElement(stroke);
        canvas.Instance.Selection.Set(stroke.Id);

        canvas.Instance.CurrentDocument.RemoveElement(stroke.Id);
        Assert.AreEqual(0, selectionAtNotification);
    }

    [TestMethod]
    public void Replacing_the_document_parameter_rebinds_history_and_selection()
    {
        var first = new BinkDocument();
        IRenderedComponent<BinkCanvas> canvas = Render<BinkCanvas>(p => p.Add(c => c.Document, first));
        first.AddElement(new BinkStroke(TestPoints(), new BinkPenBrush()));
        Assert.AreEqual(1, canvas.Instance.History.UndoCount);

        var second = new BinkDocument();
        canvas.Render(p => p.Add(c => c.Document, second));

        Assert.AreSame(second, canvas.Instance.CurrentDocument);
        Assert.AreEqual(0, canvas.Instance.History.UndoCount);

        second.AddElement(new BinkStroke(TestPoints(), new BinkPenBrush()));
        Assert.AreEqual(1, canvas.Instance.History.UndoCount);

        // The old document must no longer drive the component.
        first.AddElement(new BinkStroke(TestPoints(50), new BinkPenBrush()));
        Assert.AreEqual(1, canvas.Instance.History.UndoCount);
    }

    [TestMethod]
    public void The_viewport_parameter_is_applied_and_reported()
    {
        BinkViewport? reported = null;
        IRenderedComponent<BinkCanvas> canvas = Render<BinkCanvas>(p => p
            .Add(c => c.Viewport, new BinkViewport(2f, new Vector2(10, 20)))
            .Add(c => c.ViewportChanged, v => reported = v));

        Assert.AreEqual(2f, canvas.Instance.CurrentViewport.Zoom);

        canvas.Instance.SetViewport(new BinkViewport(3f, Vector2.Zero));
        Assert.AreEqual(3f, canvas.Instance.CurrentViewport.Zoom);
        Assert.AreEqual(3f, reported!.Value.Zoom);
    }

    [TestMethod]
    public async Task Exports_svg_from_the_component()
    {
        IRenderedComponent<BinkCanvas> canvas = Render<BinkCanvas>();
        canvas.Instance.CurrentDocument.AddElement(new BinkStroke(TestPoints(), new BinkPenBrush()));

        string svg = await canvas.Instance.ExportSvgAsync();
        Assert.StartsWith("<svg", svg, StringComparison.Ordinal);
        Assert.Contains("<path", svg, StringComparison.Ordinal);
    }

    [TestMethod]
    public void The_touch_action_style_follows_the_gesture_setting()
    {
        IRenderedComponent<BinkCanvas> withGestures = Render<BinkCanvas>(p => p.Add(c => c.EnableTouchGestures, true));
        Assert.Contains("touch-action:none", withGestures.Find(".bit-bink-canvas").GetAttribute("style")!, StringComparison.Ordinal);

        IRenderedComponent<BinkCanvas> withScrolling = Render<BinkCanvas>(p => p.Add(c => c.EnableTouchGestures, false));
        Assert.Contains("touch-action:pan-x pan-y", withScrolling.Find(".bit-bink-canvas").GetAttribute("style")!, StringComparison.Ordinal);
    }

    private static BinkStrokePoints TestPoints(float offset = 0)
    {
        var buffer = new BinkStrokePointBuffer();
        for (int i = 0; i < 12; i++) buffer.Add(new BinkPoint(offset + i * 5f, 20f + i * 3f, 0.5f, i * 8f));
        return buffer.ToStrokePoints();
    }
}
