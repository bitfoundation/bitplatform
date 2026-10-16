using System.Numerics;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Bit.Bink.Tests.Components;

/// <summary>
/// The contract between the component and its browser layer: what reaches JavaScript, and what the component does
/// when the browser calls back or could not be reached at all.
/// </summary>
[TestClass]
public class BinkCanvasInteropTests : BunitContext
{
    private const string ModulePath = "./_content/Bit.Bink/bit-bink.js";

    public BinkCanvasInteropTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    /// <summary>Makes the module's attach succeed, so the component reaches its attached state.</summary>
    private void SetupAttachedModule()
    {
        BunitJSModuleInterop module = JSInterop.SetupModule(ModulePath);
        module.Mode = JSRuntimeMode.Loose;
        module.Setup<BinkAttachResult?>("attach", _ => true).SetResult(new BinkAttachResult { Width = 400, Height = 300, DevicePixelRatio = 1 });
    }

    private static double[] Samples(params BinkInputSample[] samples)
    {
        var data = new double[samples.Length * BinkInputSample.PackedSize];
        for (int i = 0; i < samples.Length; i++) samples[i].WriteTo(data.AsSpan(i * BinkInputSample.PackedSize, BinkInputSample.PackedSize));
        return data;
    }

    private static BinkInputSample Mouse(BinkPointerPhase phase, float x, float y, double time) => new()
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

    private static void Send(IRenderedComponent<BinkCanvas> canvas, params BinkInputSample[] samples)
    {
        double[] data = Samples(samples);
        canvas.InvokeAsync(() => ((IBinkInteropClient)canvas.Instance).OnSamples(data, samples.Length));
    }

    [TestMethod]
    public async Task An_image_with_a_known_size_is_loaded_into_the_browser_when_added()
    {
        // The canvas only draws images the browser has decoded. Skipping the load because the size was already
        // known left the element in the document and the canvas showing nothing but the background.
        IRenderedComponent<BinkCanvas> canvas = Render<BinkCanvas>();
        BinkImageSource source = BinkImageSource.FromBytes([1, 2, 3], "image/png", 640, 400);

        BinkImageElement? element = await canvas.InvokeAsync(() => canvas.Instance.AddImageAsync(source, Vector2.Zero));

        Assert.IsNotNull(element);
        Assert.AreEqual(640, element!.Source.Width);
        Assert.Contains(i => i.Identifier == "loadImage" && Equals(i.Arguments[1], source.Uri), JSInterop.Invocations);
    }

    [TestMethod]
    public void A_redraw_request_from_the_browser_repaints_the_document_layer()
    {
        // Sent when an image the last frame referenced finishes decoding, or a discarded canvas is restored.
        SetupAttachedModule();
        IRenderedComponent<BinkCanvas> canvas = Render<BinkCanvas>();
        canvas.Instance.CurrentDocument.AddElement(new BinkStroke(BinkStrokePoints.FromPositions([new Vector2(10, 10), new Vector2(90, 60)]), new BinkPenBrush()));
        canvas.WaitForAssertion(() => Assert.Contains(i => i.Identifier == "execute", JSInterop.Invocations));

        int before = JSInterop.Invocations.Count(i => i.Identifier == "execute" && Equals(i.Arguments[1], "document"));
        canvas.InvokeAsync(() => ((IBinkInteropClient)canvas.Instance).OnRedrawRequested());

        canvas.WaitForAssertion(() => Assert.IsTrue(
            JSInterop.Invocations.Count(i => i.Identifier == "execute" && Equals(i.Arguments[1], "document")) > before,
            "the document layer was not redrawn"));
    }

    [TestMethod]
    public void Loading_a_document_mid_stroke_does_not_commit_the_stroke_into_the_new_document()
    {
        IRenderedComponent<BinkCanvas> canvas = Render<BinkCanvas>();
        BinkDocument first = canvas.Instance.CurrentDocument;
        Send(canvas, Mouse(BinkPointerPhase.Down, 10, 10, 0), Mouse(BinkPointerPhase.Move, 40, 30, 8), Mouse(BinkPointerPhase.Move, 80, 50, 16));

        var second = new BinkDocument();
        canvas.InvokeAsync(() => canvas.Instance.LoadDocument(second));
        Send(canvas, Mouse(BinkPointerPhase.Up, 120, 70, 24));

        Assert.AreEqual(0, first.ElementCount);
        Assert.AreEqual(0, second.ElementCount);
        Assert.IsFalse(canvas.Instance.History.CanUndo);
    }

    [TestMethod]
    public async Task Png_export_falls_back_to_the_managed_rasterizer_when_the_browser_layer_is_not_attached()
    {
        // Loose mode loads the module but never completes the attach. The browser has no surface to render into,
        // so the export used to come back empty instead of using the .NET rasterizer.
        IRenderedComponent<BinkCanvas> canvas = Render<BinkCanvas>();
        canvas.Instance.CurrentDocument.AddElement(new BinkStroke(BinkStrokePoints.FromPositions([new Vector2(10, 10), new Vector2(90, 60)]), new BinkPenBrush()));

        byte[] png = await canvas.InvokeAsync(() => canvas.Instance.ExportPngAsync(new BinkPngExportOptions { Region = BinkExportRegion.Content }));

        Assert.IsNotNull(png);
        Assert.IsTrue(png.Length > 8, "the export returned no image");
        Assert.AreSequenceEqual(new byte[] { 0x89, (byte)'P', (byte)'N', (byte)'G' }, png[..4]);
    }

    [TestMethod]
    public async Task Saving_reports_why_when_the_browser_module_failed_to_load()
    {
        // A blocked module used to make every save a silent no-op, which looks to the user like a broken button.
        Services.AddSingleton<IJSRuntime>(new BlockedModuleRuntime());
        IRenderedComponent<BinkCanvas> canvas = Render<BinkCanvas>();
        canvas.WaitForAssertion(() => Assert.IsNotNull(canvas.Instance.AttachFailed));

        var error = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => canvas.InvokeAsync(() => canvas.Instance.SaveSvgAsync()));
        Assert.Contains("Blocked by policy", error.Message, StringComparison.Ordinal);
    }

    [TestMethod]
    public void A_canvas_that_cannot_start_says_so_instead_of_waiting_silently()
    {
        // Ready is never raised when the module is blocked, so an application that showed the reason from Ready,
        // as the hosting documentation once did, never showed anything.
        Services.AddSingleton<IJSRuntime>(new BlockedModuleRuntime());
        string? reason = null;
        bool ready = false;
        IRenderedComponent<BinkCanvas> canvas = Render<BinkCanvas>(p => p
            .Add(c => c.Unavailable, (string r) => reason = r)
            .Add(c => c.Ready, (BinkInputCapabilities _) => ready = true));

        canvas.WaitForAssertion(() => Assert.AreEqual("Blocked by policy", reason));
        Assert.IsFalse(ready);
        // A screen reader user focusing the canvas hears why it does nothing.
        Assert.Contains("Drawing is unavailable: Blocked by policy", canvas.Find($"#{canvas.Instance.ElementId}-desc").TextContent, StringComparison.Ordinal);
    }

    /// <summary>A runtime whose module import fails, as it does under a content security policy that blocks it.</summary>
    private sealed class BlockedModuleRuntime : IJSRuntime
    {
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
            => InvokeAsync<TValue>(identifier, CancellationToken.None, args);

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
            => ValueTask.FromException<TValue>(new JSException("Blocked by policy"));
    }

    [TestMethod]
    public void The_container_carries_the_element_id()
    {
        IRenderedComponent<BinkCanvas> canvas = Render<BinkCanvas>();
        Assert.AreEqual(canvas.Instance.ElementId, canvas.Find(".bit-bink-canvas").GetAttribute("id"));
        Assert.IsNotNull(canvas.Find($"#{canvas.Instance.ElementId}-desc"));
    }

    [TestMethod]
    public void Turning_keyboard_shortcuts_off_after_attach_reaches_the_browser_layer()
    {
        // The browser layer prevents the default action of the keys it owns synchronously, from the options it
        // was given. Without an update it kept swallowing Ctrl+Z and Ctrl+A after shortcuts were turned off.
        SetupAttachedModule();
        IRenderedComponent<BinkCanvas> canvas = Render<BinkCanvas>(p => p.Add(c => c.EnableKeyboardShortcuts, true));
        canvas.WaitForAssertion(() => Assert.Contains(i => i.Identifier == "setCursor", JSInterop.Invocations));

        canvas.Render(p => p.Add(c => c.EnableKeyboardShortcuts, false));

        JSRuntimeInvocation update = Assert.ContainsSingle(i => i.Identifier == "setOptions", JSInterop.Invocations);
        Assert.IsFalse(Assert.IsExactInstanceOfType<BinkInteropOptions>(update.Arguments[1]).HandleKeyboard);
    }

    [TestMethod]
    public void Switching_tools_through_the_api_closes_the_inline_text_editor()
    {
        var document = new BinkDocument();
        document.AddElement(new BinkTextElement(new Vector2(20, 20), "Hello"));
        var text = new BinkTextTool();
        IRenderedComponent<BinkCanvas> canvas = Render<BinkCanvas>(p => p.Add(c => c.Document, document).Add(c => c.Tool, text));

        Send(canvas, Mouse(BinkPointerPhase.Down, 25, 25, 0));
        canvas.WaitForAssertion(() => Assert.IsNotEmpty(canvas.FindAll("textarea.bit-bink-text-editor")));

        // No parameter changes, so nothing but the component itself can re-render its markup.
        canvas.InvokeAsync(() => canvas.Instance.SetTool(new BinkDrawTool()));

        canvas.WaitForAssertion(() => Assert.IsEmpty(canvas.FindAll("textarea.bit-bink-text-editor")));
    }
}
