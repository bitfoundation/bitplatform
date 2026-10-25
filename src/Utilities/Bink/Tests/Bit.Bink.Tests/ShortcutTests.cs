using System.Numerics;
using Bunit;

namespace Bit.Bink.Tests;

/// <summary>Chords written as text, and shortcuts the application takes back from the canvas.</summary>
[TestClass]
public sealed class ShortcutTests : BunitContext
{
    private const string ModulePath = "./_content/Bit.Bink/bit-bink.js";

    public ShortcutTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [TestMethod]
    public void Chords_have_one_spelling()
    {
        Assert.AreEqual("Ctrl+Shift+Z", BinkShortcut.Normalize("shift+ctrl+z"));
        Assert.AreEqual("Ctrl+Shift+Z", BinkShortcut.Normalize("Cmd + Shift + Z"));
        Assert.AreEqual("Ctrl+Alt+K", BinkShortcut.Normalize("Option+Control+k"));
        Assert.AreEqual("Delete", BinkShortcut.Normalize("del"));
        Assert.AreEqual("Escape", BinkShortcut.Normalize("ESC"));
        Assert.AreEqual("ArrowLeft", BinkShortcut.Normalize("arrowleft"));
        Assert.AreEqual("Ctrl++", BinkShortcut.Normalize("Ctrl++"));
        Assert.AreEqual("F5", BinkShortcut.Normalize("F5"));
        Assert.AreEqual("Shift+F12", BinkShortcut.Normalize("shift+f12"));
        Assert.AreEqual("Ctrl+Fn", BinkShortcut.Normalize("Ctrl+Fn"), "Not a function key; kept as written.");
        Assert.IsNull(BinkShortcut.Normalize("Ctrl+Shift"));
        Assert.IsNull(BinkShortcut.Normalize("  "));
    }

    private (IRenderedComponent<BinkCanvas> Canvas, BinkRectangleShape Shape) WithSelectedShape(params string[] disabled)
    {
        IRenderedComponent<BinkCanvas> canvas = Render<BinkCanvas>(p => p
            .Add(c => c.Tool, new BinkSelectTool())
            .Add(c => c.DisabledShortcuts, disabled));
        var shape = new BinkRectangleShape(new BinkRect(10, 10, 50, 50));
        canvas.InvokeAsync(() =>
        {
            canvas.Instance.CurrentDocument.AddElement(shape);
            canvas.Instance.Selection.Set(shape.Id);
        });
        return (canvas, shape);
    }

    private static void Key(IRenderedComponent<BinkCanvas> canvas, string key, bool ctrl = false, bool shift = false)
        => canvas.InvokeAsync(() => ((IBinkInteropClient)canvas.Instance).OnKey(key, ctrl, shift, false));

    [TestMethod]
    public void A_disabled_shortcut_reaches_neither_the_tool_nor_the_canvas()
    {
        (IRenderedComponent<BinkCanvas> canvas, BinkRectangleShape shape) = WithSelectedShape("Delete", "ctrl+d");

        Key(canvas, "Delete");
        Key(canvas, "d", ctrl: true);
        Assert.AreEqual(1, canvas.Instance.CurrentDocument.ElementCount, "Neither deleted by the select tool nor duplicated by the canvas.");

        Key(canvas, "ArrowRight");
        Assert.AreEqual(shape.Bounds.Left + 1f, canvas.Instance.CurrentDocument.GetElement(shape.Id).Bounds.Left, 0.01f, "Every other shortcut still works.");
        Key(canvas, "Backspace");
        Assert.AreEqual(0, canvas.Instance.CurrentDocument.ElementCount);
    }

    [TestMethod]
    public void A_chord_written_with_the_unshifted_key_matches_its_shifted_press()
    {
        (IRenderedComponent<BinkCanvas> canvas, BinkRectangleShape shape) = WithSelectedShape("Ctrl+Shift+]");
        var other = new BinkRectangleShape(new BinkRect(100, 10, 50, 50));
        canvas.InvokeAsync(() =>
        {
            canvas.Instance.CurrentDocument.AddElement(other);
            canvas.Instance.Selection.Set(shape.Id);
        });

        // Shift turns ] into } on most layouts; that is what the browser reports.
        Key(canvas, "}", ctrl: true, shift: true);
        Assert.AreEqual(shape.Id, canvas.Instance.CurrentDocument.Layers[0].Elements[0].Id, "Not brought to the front.");
    }

    [TestMethod]
    public void The_browser_layer_is_told_which_shortcuts_to_leave_alone()
    {
        BunitJSModuleInterop module = JSInterop.SetupModule(ModulePath);
        module.Mode = JSRuntimeMode.Loose;
        module.Setup<BinkAttachResult?>("attach", _ => true).SetResult(new BinkAttachResult { Width = 400, Height = 300, DevicePixelRatio = 1 });

        IRenderedComponent<BinkCanvas> canvas = Render<BinkCanvas>(p => p.Add(c => c.DisabledShortcuts, ["ctrl+a", "Delete"]));

        canvas.WaitForAssertion(() => Assert.Contains(i => i.Identifier == "attach", module.Invocations));
        var options = (BinkInteropOptions)module.Invocations.First(i => i.Identifier == "attach").Arguments[2]!;
        CollectionAssert.AreEqual(new[] { "Ctrl+A", "Delete" }, options.DisabledShortcuts);

        int sent = module.Invocations.Count(i => i.Identifier == "setOptions");
        canvas.Render(p => p.Add(c => c.Height, "320px"));
        Assert.AreEqual(sent, module.Invocations.Count(i => i.Identifier == "setOptions"), "Unchanged options are not sent again on every render.");
    }
}
