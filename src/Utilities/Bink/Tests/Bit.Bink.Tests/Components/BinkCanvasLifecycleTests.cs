using System.Numerics;
using Bunit;
using Microsoft.JSInterop;
using TestContext = Microsoft.VisualStudio.TestTools.UnitTesting.TestContext;

namespace Bit.Bink.Tests.Components;

/// <summary>
/// Component lifecycle: repeated creation and disposal, several canvases on one page, and the paths that have to
/// keep working when the browser module never loaded.
/// </summary>
[TestClass]
public class BinkCanvasLifecycleTests : BunitContext
{
    private readonly TestContext _output;

    public BinkCanvasLifecycleTests(TestContext output)
    {
        _output = output;
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private static BinkStrokePoints Points(float offset = 0)
    {
        var buffer = new BinkStrokePointBuffer();
        for (int i = 0; i < 12; i++) buffer.Add(new BinkPoint(offset + i * 5f, 20f + i * 3f, 0.5f, i * 8f));
        return buffer.ToStrokePoints();
    }

    [TestMethod]
    public async Task Repeated_create_draw_dispose_cycles_leave_nothing_behind()
    {
        for (int cycle = 0; cycle < 25; cycle++)
        {
            IRenderedComponent<BinkCanvas> canvas = Render<BinkCanvas>();
            canvas.Instance.CurrentDocument.AddElement(new BinkStroke(Points(cycle), new BinkPenBrush()));
            Assert.AreEqual(1, canvas.Instance.History.UndoCount);
            await canvas.Instance.DisposeAsync();
            // Disposing twice is what a host does when a circuit tears down after an explicit dispose.
            await canvas.Instance.DisposeAsync();
        }
    }

    [TestMethod]
    public async Task A_disposed_canvas_ignores_further_document_changes()
    {
        IRenderedComponent<BinkCanvas> canvas = Render<BinkCanvas>();
        BinkDocument document = canvas.Instance.CurrentDocument;
        await canvas.Instance.DisposeAsync();

        document.AddElement(new BinkStroke(Points(), new BinkPenBrush()));
        _output.WriteLine($"undo count after disposal: {canvas.Instance.History.UndoCount}");
        Assert.IsFalse(canvas.Instance.Undo());
    }

    [TestMethod]
    public void Twenty_canvases_on_one_page_have_distinct_ids_and_isolated_state()
    {
        var canvases = new List<IRenderedComponent<BinkCanvas>>();
        for (int i = 0; i < 20; i++) canvases.Add(Render<BinkCanvas>());

        var ids = canvases.Select(c => c.Instance.ElementId).ToList();
        Assert.AreEqual(ids.Count, ids.Distinct().Count());

        canvases[3].Instance.CurrentDocument.AddElement(new BinkStroke(Points(), new BinkPenBrush()));
        Assert.AreEqual(1, canvases[3].Instance.CurrentDocument.ElementCount);
        for (int i = 0; i < canvases.Count; i++)
        {
            if (i == 3) continue;
            Assert.AreEqual(0, canvases[i].Instance.CurrentDocument.ElementCount);
            Assert.AreEqual(0, canvases[i].Instance.History.UndoCount);
        }
    }

    [TestMethod]
    public void Changing_the_brush_updates_the_wet_ink_style_sent_to_the_browser()
    {
        // The provisional trail is drawn by JavaScript with whatever style it was last told. Sending it only once
        // at attach left it drawing the previous colour and width until the round trip to the server replaced it.
        IRenderedComponent<BinkCanvas> canvas = Render<BinkCanvas>(p => p
            .Add(c => c.WetInk, true)
            .Add(c => c.Brush, new BinkPenBrush { Color = BinkColor.Parse("#ff0000"), Width = 3 }));

        int before = JSInterop.Invocations.Count(i => i.Identifier == "setWetInkStyle");

        canvas.Render(p => p
            .Add(c => c.WetInk, true)
            .Add(c => c.Brush, new BinkPenBrush { Color = BinkColor.Parse("#0000ff"), Width = 12 }));

        var after = JSInterop.Invocations.Where(i => i.Identifier == "setWetInkStyle").ToList();
        foreach (var call in after) _output.WriteLine("  " + string.Join(", ", call.Arguments));
        Assert.IsTrue(after.Count > before, "changing the brush did not update the wet-ink style");
    }

    [TestMethod]
    public void An_announcement_reaches_the_live_region_when_the_browser_module_is_unavailable()
    {
        // Loose mode never completes the attach, so this exercises the canvas whose module was blocked. Its own
        // markup - the live region, the accessible description, the inline editor - still has to update.
        IRenderedComponent<BinkCanvas> canvas = Render<BinkCanvas>();
        canvas.Instance.Announce("Canvas cleared.");
        canvas.WaitForState(() => canvas.Markup.Contains("Canvas cleared.", StringComparison.Ordinal), TimeSpan.FromSeconds(2));
    }

    [TestMethod]
    public void The_accessible_description_is_not_rebuilt_for_every_element_of_a_large_document()
    {
        var document = new BinkDocument();
        for (int i = 0; i < 2000; i++) document.AddElement(new BinkStroke(Points(i), new BinkPenBrush()));
        IRenderedComponent<BinkCanvas> canvas = Render<BinkCanvas>(p => p.Add(c => c.Document, document));
        string markup = canvas.Markup;
        _output.WriteLine($"description length: {markup.Length}");
        Assert.Contains("2000 elements", markup, StringComparison.Ordinal);
        Assert.IsTrue(markup.Length < 20000, $"the accessible description grew to {markup.Length} characters");
    }

    [TestMethod]
    public async Task Exporting_while_the_document_is_being_edited_does_not_throw()
    {
        IRenderedComponent<BinkCanvas> canvas = Render<BinkCanvas>();
        BinkDocument document = canvas.Instance.CurrentDocument;
        for (int i = 0; i < 50; i++) document.AddElement(new BinkStroke(Points(i), new BinkPenBrush()));

        string svg = await Task.Run(() => canvas.Instance.ExportSvg(null));
        Assert.StartsWith("<svg", svg, StringComparison.Ordinal);
    }
}
