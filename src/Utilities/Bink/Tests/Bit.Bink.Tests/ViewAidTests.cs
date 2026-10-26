using System.Numerics;
using Bit.Bink.Tests.Components;
using Bunit;
using Microsoft.JSInterop;

namespace Bit.Bink.Tests;

/// <summary>The view grid, dropping files on the canvas and the eyedropper.</summary>
[TestClass]
public sealed class GridTests
{
    /// <summary>Records what the grid strokes.</summary>
    private sealed class StrokeRecorder : IBinkRenderContext
    {
        public List<(BinkPathGeometry Path, BinkStrokePaint Paint)> Strokes { get; } = [];

        public void StrokePath(BinkPathGeometry path, in BinkStrokePaint paint) => Strokes.Add((path, paint));

        public void Save() { }
        public void Restore() { }
        public void Transform(Matrix3x2 matrix) { }
        public void ClipRect(BinkRect rect) { }
        public void FillPath(BinkPathGeometry path, in BinkFillPaint paint) { }
        public void DrawImage(BinkImageSource image, BinkRect destination, BinkRect? sourceRect, float opacity) { }
        public void DrawText(string text, Vector2 origin, in BinkTextStyle style, float opacity) { }
        public void PushGroup(float opacity, BinkBlendMode blend) { }
        public void PopGroup() { }
    }

    private static int Lines(BinkPathGeometry path) => path.Verbs.ToArray().Count(v => v == BinkPathVerb.MoveTo);

    [TestMethod]
    public void Lines_cover_the_view_with_every_fifth_stronger()
    {
        var painter = new BinkGridPainter();
        var recorder = new StrokeRecorder();
        painter.Draw(recorder, BinkGrid.Default, new BinkRect(0, 0, 400, 300), BinkRect.Empty, zoom: 1f);

        Assert.HasCount(2, recorder.Strokes);
        int minor = Lines(recorder.Strokes[0].Path);
        int major = Lines(recorder.Strokes[1].Path);
        Assert.AreEqual(BinkGrid.Default.Color, recorder.Strokes[0].Paint.Color);
        Assert.AreEqual(BinkGrid.Default.MajorColor, recorder.Strokes[1].Paint.Color);
        // Every fifth line is strong, so about four light ones to each, give or take the ends of each axis.
        Assert.IsGreaterThan(0, major);
        Assert.IsGreaterThan(3 * major, minor);
        Assert.IsLessThan(5 * major, minor);
        Assert.AreEqual(1f, recorder.Strokes[0].Paint.Width, "A hairline: one CSS pixel at any zoom.");
    }

    [TestMethod]
    public void Panning_within_the_same_tiles_reuses_the_paths_and_zooming_out_coarsens_the_grid()
    {
        var painter = new BinkGridPainter();
        var first = new StrokeRecorder();
        var second = new StrokeRecorder();
        painter.Draw(first, BinkGrid.Default, new BinkRect(0, 0, 400, 300), BinkRect.Empty, 1f);
        painter.Draw(second, BinkGrid.Default, new BinkRect(5, 5, 400, 300), BinkRect.Empty, 1f);
        Assert.AreSame(first.Strokes[0].Path, second.Strokes[0].Path, "The browser keeps the path; a small pan must not send a new one.");

        var zoomedOut = new StrokeRecorder();
        painter.Draw(zoomedOut, BinkGrid.Default, new BinkRect(0, 0, 4000, 3000), BinkRect.Empty, 0.1f);
        int drawn = zoomedOut.Strokes.Sum(s => Lines(s.Path));
        Assert.IsLessThan(200, drawn, "At a tenth of the size, cells of 2 pixels would be a grey wash; coarser multiples are drawn.");
    }

    [TestMethod]
    public void A_fixed_page_limits_the_grid_and_dots_are_one_dashed_line_a_row()
    {
        var painter = new BinkGridPainter();
        var recorder = new StrokeRecorder();
        painter.Draw(recorder, BinkGrid.DottedDefault, new BinkRect(-500, -500, 2000, 2000), new BinkRect(0, 0, 200, 100), 1f);

        (BinkPathGeometry path, BinkStrokePaint paint) = recorder.Strokes.Single();
        Assert.AreEqual(0f, path.Bounds.Left);
        Assert.AreEqual(200f, path.Bounds.Right);
        Assert.AreEqual(100f, path.Bounds.Bottom);
        Assert.AreEqual(6, Lines(path), "One row every 20 units from 0 to 100.");
        Assert.AreEqual(BinkLineCap.Round, paint.Cap);
        CollectionAssert.AreEqual(new[] { 0f, 20f }, paint.Dash);
    }
}

/// <summary>Dropping files and picking colours, as the browser layer drives them.</summary>
[TestClass]
public sealed class BinkCanvasDropTests : BunitContext
{
    private const string ModulePath = "./_content/Bit.Bink/bit-bink.js";

    public BinkCanvasDropTests()
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

    private static byte[] Png() => TestData.CreatePng(40, 20, (_, _) => new BinkColor(200, 0, 0));

    [TestMethod]
    public async Task A_dropped_image_is_added_centred_where_it_was_dropped()
    {
        BunitJSModuleInterop module = SetupAttachedModule();
        module.Setup<BinkImageMetrics?>("loadImage", _ => true).SetResult(new BinkImageMetrics { Width = 40, Height = 20 });
        IRenderedComponent<BinkCanvas> canvas = Render<BinkCanvas>(p => p.Add(c => c.EnableDrop, true));
        canvas.WaitForAssertion(() => Assert.Contains(i => i.Identifier == "execute", module.Invocations));

        await canvas.InvokeAsync(() => ((IBinkInteropClient)canvas.Instance).OnDropAsync("image", new TestStreamReference(Png()), "image/png", 150, 100));

        canvas.WaitForAssertion(() => Assert.AreEqual(1, canvas.Instance.CurrentDocument.ElementCount));
        BinkElement image = canvas.Instance.CurrentDocument.AllElements.Single();
        Assert.AreEqual(new Vector2(150, 100), image.Bounds.Center);
        Assert.AreEqual(image.Id, canvas.Instance.Selection.Ids.Single());
    }

    [TestMethod]
    public async Task A_dropped_drawing_pastes_its_elements_there()
    {
        IRenderedComponent<BinkCanvas> canvas = Render<BinkCanvas>(p => p.Add(c => c.EnableDrop, true));
        var saved = new BinkDocument();
        saved.AddElement(new BinkRectangleShape(new BinkRect(0, 0, 40, 40)));
        byte[] file = BinkSerializer.SerializeToUtf8Bytes(saved);

        await canvas.InvokeAsync(() => ((IBinkInteropClient)canvas.Instance).OnDropAsync("bink", new TestStreamReference(file), "application/json", 200, 120));

        canvas.WaitForAssertion(() => Assert.AreEqual(1, canvas.Instance.CurrentDocument.ElementCount));
        Assert.AreEqual(200f, canvas.Instance.CurrentDocument.AllElements.Single().Bounds.Center.X, 0.5f);
    }

    [TestMethod]
    public void Imported_svg_is_one_undo_step_grouped_and_selected()
    {
        IRenderedComponent<BinkCanvas> canvas = Render<BinkCanvas>();
        const string svg = """<svg xmlns="http://www.w3.org/2000/svg"><rect width="20" height="20"/><circle cx="40" cy="10" r="10"/><filter id="f"/></svg>""";

        BinkSvgImportResult? result = null;
        canvas.InvokeAsync(() => result = canvas.Instance.ImportSvg(svg, new Vector2(300, 200)));

        Assert.IsNotNull(result);
        BinkDocument document = canvas.Instance.CurrentDocument;
        Assert.AreEqual(2, document.ElementCount);
        Assert.AreEqual(1, canvas.Instance.History.UndoCount, "Adding and grouping are one step.");
        Assert.AreEqual(1, document.AllElements.Select(e => e.GroupIds.Single()).Distinct().Count());
        Assert.AreEqual(2, canvas.Instance.Selection.Count);
        BinkRect bounds = BinkClipboard.Bounds(document.AllElements);
        Assert.AreEqual(300f, bounds.Center.X, 0.5f);

        canvas.InvokeAsync(() => canvas.Instance.Undo());
        Assert.AreEqual(0, document.ElementCount);
        Assert.IsNull(canvas.Instance.ImportSvg("<html/>"), "Not SVG: nothing imported, and the reason announced.");
    }

    [TestMethod]
    public async Task A_dropped_svg_becomes_editable_shapes()
    {
        IRenderedComponent<BinkCanvas> canvas = Render<BinkCanvas>(p => p.Add(c => c.EnableDrop, true));
        byte[] file = "<svg xmlns=\"http://www.w3.org/2000/svg\"><path d=\"M0 0 L40 0 L20 30 Z\" fill=\"#123456\"/></svg>"u8.ToArray();

        await canvas.InvokeAsync(() => ((IBinkInteropClient)canvas.Instance).OnDropAsync("svg", new TestStreamReference(file), "image/svg+xml", 100, 80));

        canvas.WaitForAssertion(() => Assert.AreEqual(1, canvas.Instance.CurrentDocument.ElementCount));
        Assert.IsInstanceOfType<BinkPathElement>(canvas.Instance.CurrentDocument.AllElements.Single());
    }

    [TestMethod]
    public async Task Drops_are_refused_unless_turned_on_and_never_on_a_read_only_canvas()
    {
        foreach (bool readOnly in new[] { false, true })
        {
            IRenderedComponent<BinkCanvas> canvas = Render<BinkCanvas>(p => p.Add(c => c.EnableDrop, readOnly).Add(c => c.ReadOnly, readOnly));
            var stream = new TestStreamReference(Png());
            await canvas.InvokeAsync(() => ((IBinkInteropClient)canvas.Instance).OnDropAsync("image", stream, "image/png", 10, 10));
            Assert.AreEqual(0, canvas.Instance.CurrentDocument.ElementCount);
            Assert.IsTrue(stream.Disposed, "The browser's stream is released even when the drop is refused.");
        }
    }

    [TestMethod]
    public void A_copy_hands_the_browser_a_picture_of_the_selection_with_the_drawing()
    {
        BunitJSModuleInterop module = SetupAttachedModule();
        IRenderedComponent<BinkCanvas> canvas = Render<BinkCanvas>(p => p.Add(c => c.EnableClipboard, true).Add(c => c.Tool, new BinkSelectTool()));
        canvas.WaitForAssertion(() => Assert.Contains(i => i.Identifier == "execute", module.Invocations));
        var shape = new BinkRectangleShape(new BinkRect(10, 10, 100, 50)) { Style = new BinkShapeStyle { FillColor = BinkColor.Black } };
        canvas.InvokeAsync(() =>
        {
            canvas.Instance.CurrentDocument.AddElement(shape);
            canvas.Instance.Selection.Set(shape.Id);
        });

        canvas.InvokeAsync(() => ((IBinkInteropClient)canvas.Instance).OnKey("c", true, false, false));

        canvas.WaitForAssertion(() => Assert.Contains(i => i.Identifier == "provideClipboard", module.Invocations));
        JSRuntimeInvocation provide = module.Invocations.Last(i => i.Identifier == "provideClipboard");
        StringAssert.StartsWith((string)provide.Arguments[1]!, "{\"format\":\"bink\"");
        // Twice the selection's bounds and an 8 unit margin.
        Assert.AreEqual((int)MathF.Ceiling((shape.Bounds.Width + 16f) * 2f), provide.Arguments[2]);
        Assert.AreEqual((int)MathF.Ceiling((shape.Bounds.Height + 16f) * 2f), provide.Arguments[3]);
        Assert.IsGreaterThan(0, (int)provide.Arguments[5]!, "The commands that draw the picture.");
    }

    [TestMethod]
    public async Task The_eyedropper_returns_the_picked_colour_or_null()
    {
        BunitJSModuleInterop module = SetupAttachedModule();
        module.Setup<string?>("pickColor", _ => true).SetResult("#1d4ed8");
        module.Setup<bool>("supportsEyeDropper", _ => true).SetResult(true);
        IRenderedComponent<BinkCanvas> canvas = Render<BinkCanvas>();
        canvas.WaitForAssertion(() => Assert.Contains(i => i.Identifier == "execute", module.Invocations));

        Assert.IsTrue(await canvas.InvokeAsync(() => canvas.Instance.SupportsEyeDropperAsync()));
        Assert.AreEqual(BinkColor.Parse("#1d4ed8"), await canvas.InvokeAsync(() => canvas.Instance.PickColorAsync()));

        module.Setup<string?>("pickColor", _ => true).SetResult(null);
        Assert.IsNull(await canvas.InvokeAsync(() => canvas.Instance.PickColorAsync()), "Cancelled, or no eyedropper.");
    }
}
