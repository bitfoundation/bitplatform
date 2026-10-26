using System.Numerics;
using Bunit;

namespace Bit.Bink.Tests.Components;

/// <summary>
/// Where the canvas puts new elements. A layer the host names in <see cref="BinkCanvas.ActiveLayerId"/> is used as
/// named: while it is locked or hidden, drawing and the canvas's own commands are refused with the reason announced,
/// rather than their elements going on a layer nobody chose. Without one, the top-most editable layer takes them.
/// </summary>
[TestClass]
public sealed class BinkCanvasLayerTests : BunitContext
{
    private const string ModulePath = "./_content/Bit.Bink/bit-bink.js";

    public BinkCanvasLayerTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private void SetupAttachedModule()
    {
        BunitJSModuleInterop module = JSInterop.SetupModule(ModulePath);
        module.Mode = JSRuntimeMode.Loose;
        module.Setup<BinkAttachResult?>("attach", _ => true).SetResult(new BinkAttachResult { Width = 400, Height = 300, DevicePixelRatio = 1 });
    }

    /// <summary>A document whose bottom layer, "Ink", is locked, with an editable layer above it.</summary>
    private static (BinkDocument Document, Guid Ink, Guid Above) LockedInkBelowAnEditableLayer()
    {
        var document = new BinkDocument();
        Guid ink = document.Layers[0].Id;
        Guid above = document.AddLayer("Above").Id;
        document.UpdateLayer(ink, p => p with { Name = "Ink", IsLocked = true });
        return (document, ink, above);
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

    private static void Draw(IRenderedComponent<BinkCanvas> canvas)
    {
        BinkInputSample[] samples =
        [
            Mouse(BinkPointerPhase.Down, 10, 10, 0),
            Mouse(BinkPointerPhase.Move, 40, 30, 8),
            Mouse(BinkPointerPhase.Move, 80, 50, 16),
            Mouse(BinkPointerPhase.Up, 120, 70, 24),
        ];
        var data = new double[samples.Length * BinkInputSample.PackedSize];
        for (int i = 0; i < samples.Length; i++) samples[i].WriteTo(data.AsSpan(i * BinkInputSample.PackedSize, BinkInputSample.PackedSize));
        canvas.InvokeAsync(() => ((IBinkInteropClient)canvas.Instance).OnSamples(data, samples.Length));
    }

    // The live region's text, past the layer's name: the markup encodes the quotes around the name.
    private static void AssertAnnounced(IRenderedComponent<BinkCanvas> canvas, string state)
        => canvas.WaitForState(() => canvas.Markup.Contains($"is {state}, so nothing can be added to it.", StringComparison.Ordinal), TimeSpan.FromSeconds(2));

    [TestMethod]
    public void Drawing_on_a_locked_layer_the_host_chose_is_refused_rather_than_moved()
    {
        (BinkDocument document, Guid ink, _) = LockedInkBelowAnEditableLayer();
        IRenderedComponent<BinkCanvas> canvas = Render<BinkCanvas>(p => p.Add(c => c.Document, document).Add(c => c.ActiveLayerId, ink));

        Draw(canvas);

        // The stroke used to go on the editable layer above, while the host still showed "Ink" as the one in use.
        Assert.AreEqual(0, document.ElementCount, "the stroke went on a layer the host did not choose");
        AssertAnnounced(canvas, "locked");
    }

    [TestMethod]
    public void Without_a_chosen_layer_new_elements_go_on_the_top_most_editable_one()
    {
        var document = new BinkDocument();
        Guid bottom = document.Layers[0].Id;
        Guid top = document.AddLayer("Top").Id;
        IRenderedComponent<BinkCanvas> canvas = Render<BinkCanvas>(p => p.Add(c => c.Document, document));

        // The top layer is where they go first; locked, the next one down takes them.
        document.UpdateLayer(top, p => p with { IsLocked = true });
        Draw(canvas);

        Assert.ContainsSingle(document.GetLayer(bottom).Elements);
        Assert.IsEmpty(document.GetLayer(top).Elements);
    }

    [TestMethod]
    public async Task Pasting_onto_a_locked_layer_the_host_chose_is_refused()
    {
        (BinkDocument document, Guid ink, _) = LockedInkBelowAnEditableLayer();
        IRenderedComponent<BinkCanvas> canvas = Render<BinkCanvas>(p => p.Add(c => c.Document, document).Add(c => c.ActiveLayerId, ink));
        string payload = BinkClipboard.Serialize([TestData.Stroke(new Vector2(10, 10), new Vector2(60, 40))]);

        int pasted = await canvas.InvokeAsync(() => canvas.Instance.Paste(payload));

        Assert.AreEqual(0, pasted);
        Assert.AreEqual(0, document.ElementCount);
        AssertAnnounced(canvas, "locked");
    }

    [TestMethod]
    public async Task Importing_or_adding_a_picture_onto_a_hidden_layer_the_host_chose_is_refused()
    {
        var document = new BinkDocument();
        Guid ink = document.Layers[0].Id;
        document.AddLayer("Above");
        document.UpdateLayer(ink, p => p with { Name = "Ink", IsVisible = false });
        IRenderedComponent<BinkCanvas> canvas = Render<BinkCanvas>(p => p.Add(c => c.Document, document).Add(c => c.ActiveLayerId, ink));

        BinkSvgImportResult? imported = await canvas.InvokeAsync(() => canvas.Instance.ImportSvg("<svg xmlns=\"http://www.w3.org/2000/svg\"><rect x=\"1\" y=\"1\" width=\"20\" height=\"10\"/></svg>"));
        BinkImageElement? picture = await canvas.InvokeAsync(() => canvas.Instance.AddImageAsync(BinkImageSource.FromBytes([1, 2, 3], "image/png", 40, 30)));

        Assert.IsNull(imported);
        Assert.IsNull(picture);
        Assert.AreEqual(0, document.ElementCount);
        AssertAnnounced(canvas, "hidden");
    }

    [TestMethod]
    public void No_provisional_trail_is_drawn_for_ink_the_layer_will_refuse()
    {
        // On Blazor Server the browser draws a trail before the stroke reaches .NET. Over a layer that refuses the
        // stroke, it would be ink that never arrives, left on the screen until something else repainted.
        SetupAttachedModule();
        (BinkDocument document, Guid ink, _) = LockedInkBelowAnEditableLayer();
        IRenderedComponent<BinkCanvas> canvas = Render<BinkCanvas>(p => p
            .Add(c => c.Document, document)
            .Add(c => c.ActiveLayerId, ink)
            .Add(c => c.WetInk, true));

        canvas.WaitForAssertion(() => Assert.Contains(i => i.Identifier == "setWetInkStyle", JSInterop.Invocations));
        Assert.IsNull(JSInterop.Invocations.Last(i => i.Identifier == "setWetInkStyle").Arguments[1], "the trail is on over a locked layer");

        canvas.InvokeAsync(() => document.UpdateLayer(ink, p => p with { IsLocked = false }));

        canvas.WaitForAssertion(() => Assert.IsNotNull(JSInterop.Invocations.Last(i => i.Identifier == "setWetInkStyle").Arguments[1], "unlocking the layer did not turn the trail back on"));
    }
}
