using System.Numerics;
using Bunit;

namespace Bit.Bink.Tests.Components;

/// <summary>
/// The canvas's editing commands as the browser layer drives them: keyboard shortcuts, the clipboard, the wheel, and
/// what crosses the interop boundary for them.
/// </summary>
[TestClass]
public sealed class BinkCanvasEditingTests : BunitContext
{
    private const string ModulePath = "./_content/Bit.Bink/bit-bink.js";

    public BinkCanvasEditingTests()
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

    private static void Key(IRenderedComponent<BinkCanvas> canvas, string key, bool ctrl = false, bool shift = false, bool alt = false)
        => canvas.InvokeAsync(() => ((IBinkInteropClient)canvas.Instance).OnKey(key, ctrl, shift, alt));

    private static (IRenderedComponent<BinkCanvas> Canvas, List<Guid> Ids) WithStrokes(BunitContext context, int count, Action<ComponentParameterCollectionBuilder<BinkCanvas>>? parameters = null)
    {
        IRenderedComponent<BinkCanvas> canvas = context.Render<BinkCanvas>(p =>
        {
            p.Add(c => c.Tool, new BinkSelectTool());
            parameters?.Invoke(p);
        });
        var ids = new List<Guid>();
        canvas.InvokeAsync(() =>
        {
            for (int i = 0; i < count; i++)
            {
                BinkStroke s = TestData.Stroke(new Vector2(20 + i * 30, 20), new Vector2(40 + i * 30, 40));
                canvas.Instance.CurrentDocument.AddElement(s);
                ids.Add(s.Id);
            }
        });
        return (canvas, ids);
    }

    private static List<Guid> Order(IRenderedComponent<BinkCanvas> canvas) => canvas.Instance.CurrentDocument.Layers[0].Elements.Select(e => e.Id).ToList();

    [TestMethod]
    public void Ctrl_D_duplicates_the_selection_and_selects_the_copies()
    {
        (IRenderedComponent<BinkCanvas> canvas, List<Guid> ids) = WithStrokes(this, 2);
        canvas.InvokeAsync(() => canvas.Instance.Selection.Set(ids[0]));

        Key(canvas, "d", ctrl: true);

        Assert.AreEqual(3, canvas.Instance.CurrentDocument.ElementCount);
        Guid copy = canvas.Instance.Selection.Ids.Single();
        Assert.IsFalse(ids.Contains(copy));
        Assert.IsGreaterThan(canvas.Instance.CurrentDocument.GetElement(ids[0]).Bounds.Left, canvas.Instance.CurrentDocument.GetElement(copy).Bounds.Left);
        Assert.AreEqual(1, canvas.Instance.History.UndoCount - 2, "The duplicate is one undo step.");
    }

    [TestMethod]
    public void Bracket_shortcuts_arrange_the_selection()
    {
        (IRenderedComponent<BinkCanvas> canvas, List<Guid> ids) = WithStrokes(this, 3);
        canvas.InvokeAsync(() => canvas.Instance.Selection.Set(ids[0]));

        Key(canvas, "]", ctrl: true);
        CollectionAssert.AreEqual(new[] { ids[1], ids[0], ids[2] }, Order(canvas));
        // Shift turns ] into } on most layouts; both spell "to the front".
        Key(canvas, "}", ctrl: true, shift: true);
        CollectionAssert.AreEqual(new[] { ids[1], ids[2], ids[0] }, Order(canvas));
        Key(canvas, "{", ctrl: true, shift: true);
        CollectionAssert.AreEqual(new[] { ids[0], ids[1], ids[2] }, Order(canvas));
        Key(canvas, "[", ctrl: true);
        CollectionAssert.AreEqual(new[] { ids[0], ids[1], ids[2] }, Order(canvas), "Already at the back.");
    }

    [TestMethod]
    public void Ctrl_C_hands_the_selection_to_the_browser_and_an_internal_paste_brings_it_back()
    {
        BunitJSModuleInterop module = SetupAttachedModule();
        (IRenderedComponent<BinkCanvas> canvas, List<Guid> ids) = WithStrokes(this, 2, p => p.Add(c => c.EnableClipboard, true));
        canvas.WaitForAssertion(() => Assert.Contains(i => i.Identifier == "execute", JSInterop.Invocations));
        canvas.InvokeAsync(() => canvas.Instance.Selection.Set(ids));

        Key(canvas, "c", ctrl: true);
        JSRuntimeInvocation provided = JSInterop.Invocations.Last(i => i.Identifier == "provideClipboard");
        string payload = Assert.IsInstanceOfType<string>(provided.Arguments[1]);
        Assert.IsTrue(BinkClipboard.IsPayload(payload));

        // The browser could not write the system clipboard, so it asks for the copy the canvas kept.
        canvas.InvokeAsync(() => ((IBinkInteropClient)canvas.Instance).OnPasteAsync("internal", null, null, null));
        canvas.WaitForAssertion(() => Assert.AreEqual(4, canvas.Instance.CurrentDocument.ElementCount));
        Assert.HasCount(2, canvas.Instance.Selection.Ids);
        Assert.IsTrue(canvas.Instance.Selection.Ids.All(id => !ids.Contains(id)));

        // And the same payload arriving from the system clipboard pastes too, a further step away.
        canvas.InvokeAsync(() => ((IBinkInteropClient)canvas.Instance).OnPasteAsync("bink", payload, null, null));
        canvas.WaitForAssertion(() => Assert.AreEqual(6, canvas.Instance.CurrentDocument.ElementCount));
    }

    [TestMethod]
    public void Ctrl_X_copies_and_deletes_in_one_undo_step()
    {
        SetupAttachedModule();
        (IRenderedComponent<BinkCanvas> canvas, List<Guid> ids) = WithStrokes(this, 2, p => p.Add(c => c.EnableClipboard, true));
        canvas.InvokeAsync(() => canvas.Instance.Selection.Set(ids[1]));
        int steps = canvas.Instance.History.UndoCount;

        Key(canvas, "x", ctrl: true);

        canvas.WaitForAssertion(() => Assert.AreEqual(1, canvas.Instance.CurrentDocument.ElementCount));
        Assert.AreEqual(steps + 1, canvas.Instance.History.UndoCount);
        canvas.InvokeAsync(() => canvas.Instance.Paste(BinkClipboard.Serialize([TestData.Stroke(Vector2.Zero, new Vector2(5, 5))])));
        Assert.AreEqual(2, canvas.Instance.CurrentDocument.ElementCount);
    }

    [TestMethod]
    public void The_clipboard_is_off_unless_enabled()
    {
        // A signature field that accepted pasted ink would no longer prove anyone signed.
        SetupAttachedModule();
        (IRenderedComponent<BinkCanvas> canvas, List<Guid> ids) = WithStrokes(this, 1);
        canvas.InvokeAsync(() => canvas.Instance.Selection.Set(ids[0]));
        string payload = BinkClipboard.Serialize([canvas.Instance.CurrentDocument.GetElement(ids[0])]);

        Key(canvas, "c", ctrl: true);
        canvas.InvokeAsync(() => ((IBinkInteropClient)canvas.Instance).OnPasteAsync("bink", payload, null, null));

        Assert.DoesNotContain(i => i.Identifier == "provideClipboard", JSInterop.Invocations);
        Assert.AreEqual(1, canvas.Instance.CurrentDocument.ElementCount);
        JSRuntimeInvocation attach = JSInterop.Invocations.First(i => i.Identifier == "attach");
        Assert.IsFalse(((BinkInteropOptions)attach.Arguments[2]!).Clipboard);
    }

    [TestMethod]
    public void Pasted_plain_text_becomes_a_text_element_unless_the_application_takes_it()
    {
        IRenderedComponent<BinkCanvas> canvas = Render<BinkCanvas>(p => p.Add(c => c.EnableClipboard, true));
        canvas.InvokeAsync(() => ((IBinkInteropClient)canvas.Instance).OnPasteAsync("text", "Hello there", null, null));
        canvas.WaitForAssertion(() => Assert.AreEqual("Hello there", Assert.IsInstanceOfType<BinkTextElement>(canvas.Instance.CurrentDocument.AllElements.Single()).Text));

        string? received = null;
        IRenderedComponent<BinkCanvas> handled = Render<BinkCanvas>(p => p
            .Add(c => c.EnableClipboard, true)
            .Add(c => c.TextPasted, (string text) => received = text));
        handled.InvokeAsync(() => ((IBinkInteropClient)handled.Instance).OnPasteAsync("text", "Taken", null, null));
        handled.WaitForAssertion(() => Assert.AreEqual("Taken", received));
        Assert.IsTrue(handled.Instance.CurrentDocument.IsEmpty);
    }

    [TestMethod]
    public void A_read_only_canvas_refuses_paste_duplicate_and_arrange()
    {
        (IRenderedComponent<BinkCanvas> canvas, List<Guid> ids) = WithStrokes(this, 2, p => p.Add(c => c.EnableClipboard, true).Add(c => c.ReadOnly, true));
        canvas.InvokeAsync(() => canvas.Instance.Selection.Set(ids[0]));
        string payload = BinkClipboard.Serialize([canvas.Instance.CurrentDocument.GetElement(ids[0])]);

        Key(canvas, "d", ctrl: true);
        Key(canvas, "]", ctrl: true);
        canvas.InvokeAsync(() => ((IBinkInteropClient)canvas.Instance).OnPasteAsync("bink", payload, null, null));
        Assert.AreEqual(0, canvas.Instance.Paste(payload));

        Assert.AreEqual(2, canvas.Instance.CurrentDocument.ElementCount);
        CollectionAssert.AreEqual(ids, Order(canvas));
    }

    [TestMethod]
    public void A_paste_that_would_land_off_screen_lands_in_the_middle_of_the_view()
    {
        IRenderedComponent<BinkCanvas> canvas = Render<BinkCanvas>();
        string payload = BinkClipboard.Serialize([TestData.Stroke(new Vector2(5000, 5000), new Vector2(5020, 5020))]);
        canvas.InvokeAsync(() => canvas.Instance.Paste(payload));
        BinkElement pasted = canvas.Instance.CurrentDocument.AllElements.Single();
        BinkRect visible = canvas.Instance.CurrentViewport.VisibleRect(canvas.Instance.ViewSize);
        Assert.IsTrue(visible.IsEmpty || visible.Contains(pasted.Bounds.Center));

        canvas.InvokeAsync(() => canvas.Instance.Paste(payload, new Vector2(100, 100)));
        BinkElement placed = canvas.Instance.CurrentDocument.AllElements.Last();
        Assert.AreEqual(100f, placed.Bounds.Center.X, 0.01f);
        Assert.AreEqual(100f, placed.Bounds.Center.Y, 0.01f);
    }

    [TestMethod]
    public void The_wheel_pans_and_zooms_within_the_zoom_limits()
    {
        IRenderedComponent<BinkCanvas> canvas = Render<BinkCanvas>(p => p
            .Add(c => c.WheelMode, BinkWheelMode.PanAndZoom)
            .Add(c => c.MaxZoom, 2f));
        var client = (IBinkInteropClient)canvas.Instance;

        canvas.InvokeAsync(() => client.OnWheel(30, 40, 0, 10, 10));
        Assert.AreEqual(new Vector2(-30, -40), canvas.Instance.CurrentViewport.Offset);

        // Zooming in hard stops at MaxZoom, and the point under the pointer stays put.
        Vector2 anchor = new(100, 80);
        Vector2 before = canvas.Instance.CurrentViewport.ToDocument(anchor);
        canvas.InvokeAsync(() => client.OnWheel(0, 0, 3f, anchor.X, anchor.Y));
        Assert.AreEqual(2f, canvas.Instance.CurrentViewport.Zoom, 0.0001f);
        Vector2 after = canvas.Instance.CurrentViewport.ToDocument(anchor);
        Assert.AreEqual(before.X, after.X, 0.01f);
        Assert.AreEqual(before.Y, after.Y, 0.01f);
    }

    [TestMethod]
    public void The_wheel_is_left_to_the_page_by_default()
    {
        IRenderedComponent<BinkCanvas> canvas = Render<BinkCanvas>();
        canvas.InvokeAsync(() => ((IBinkInteropClient)canvas.Instance).OnWheel(30, 40, 1f, 10, 10));
        Assert.AreEqual(BinkViewport.Identity, canvas.Instance.CurrentViewport);
    }

    [TestMethod]
    public void Wheel_and_clipboard_settings_reach_the_browser_layer()
    {
        SetupAttachedModule();
        IRenderedComponent<BinkCanvas> canvas = Render<BinkCanvas>(p => p
            .Add(c => c.WheelMode, BinkWheelMode.Zoom)
            .Add(c => c.EnableClipboard, true));
        canvas.WaitForAssertion(() => Assert.Contains(i => i.Identifier == "attach", JSInterop.Invocations));
        var options = (BinkInteropOptions)JSInterop.Invocations.First(i => i.Identifier == "attach").Arguments[2]!;
        Assert.AreEqual((int)BinkWheelMode.Zoom, options.WheelMode);
        Assert.IsTrue(options.Clipboard);
    }

    [TestMethod]
    public void Off_WebAssembly_the_document_travels_as_packed_floats_and_refers_to_retained_paths()
    {
        SetupAttachedModule();
        IRenderedComponent<BinkCanvas> canvas = Render<BinkCanvas>();
        canvas.InvokeAsync(() => canvas.Instance.CurrentDocument.AddElement(TestData.Stroke(new Vector2(10, 10), new Vector2(200, 100))));
        canvas.WaitForAssertion(() => Assert.Contains(i => i.Identifier == "execute" && Equals(i.Arguments[1], "document"), JSInterop.Invocations));

        JSRuntimeInvocation first = JSInterop.Invocations.Last(i => i.Identifier == "execute" && Equals(i.Arguments[1], "document"));
        byte[] firstBytes = Assert.IsInstanceOfType<byte[]>(first.Arguments[2]);
        Assert.AreEqual((int)first.Arguments[3]! * 4, firstBytes.Length);

        int frames = JSInterop.Invocations.Count(i => i.Identifier == "execute" && Equals(i.Arguments[1], "document"));
        canvas.InvokeAsync(() => canvas.Instance.Refresh());
        canvas.WaitForAssertion(() => Assert.IsGreaterThan(frames, JSInterop.Invocations.Count(i => i.Identifier == "execute" && Equals(i.Arguments[1], "document"))));
        byte[] secondBytes = (byte[])JSInterop.Invocations.Last(i => i.Identifier == "execute" && Equals(i.Arguments[1], "document")).Arguments[2]!;
        Assert.IsLessThan(firstBytes.Length / 3, secondBytes.Length, "A redraw of an unchanged stroke refers to the path the browser holds.");
    }

    [TestMethod]
    public void A_lost_resource_redefines_everything_on_the_next_frame()
    {
        SetupAttachedModule();
        IRenderedComponent<BinkCanvas> canvas = Render<BinkCanvas>();
        canvas.InvokeAsync(() => canvas.Instance.CurrentDocument.AddElement(TestData.Stroke(new Vector2(10, 10), new Vector2(200, 100))));
        canvas.WaitForAssertion(() => Assert.Contains(i => i.Identifier == "execute" && Equals(i.Arguments[1], "document"), JSInterop.Invocations));
        byte[] first = (byte[])JSInterop.Invocations.Last(i => i.Identifier == "execute" && Equals(i.Arguments[1], "document")).Arguments[2]!;

        canvas.InvokeAsync(() => ((IBinkInteropClient)canvas.Instance).OnResourcesLost());
        canvas.WaitForAssertion(() =>
        {
            byte[] latest = (byte[])JSInterop.Invocations.Last(i => i.Identifier == "execute" && Equals(i.Arguments[1], "document")).Arguments[2]!;
            Assert.AreEqual(first.Length, latest.Length, "After a loss the stroke is defined in full again.");
            Assert.AreNotSame(first, latest);
        });
    }
}
