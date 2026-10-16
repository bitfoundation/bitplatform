using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.PdfViewer;

/// <summary>
/// Covers the parameters that decide how a page is built and how many stay built: RenderMode (a positioned
/// HTML page, or a canvas the browser paints from the engine's display list), TextCoalescing (one span per
/// run, or one per visual line), and the page and thumbnail caches MaxRenderedPageCount and
/// MaxRenderedThumbnailCount bound.
/// </summary>
[TestClass]
public class BitPdfViewerRenderModeTests : BunitTestContext
{
    private const string PaintCall = "BitBlazorUI.PdfViewer.paintCanvasPages";

    private static byte[] TwoRuns() => TestPdf.Build(
    [
        "<< /Type /Catalog /Pages 2 0 R >>",
        "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
        "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 200 200] /Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >>",
        "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>",
        // Two runs on one baseline, one after the other.
        TestPdf.Stream("BT /F1 12 Tf 20 100 Td (Hel) Tj (lo) Tj ET"),
    ], rootObjNum: 1);

    [TestMethod]
    public void BitPdfViewerShouldBuildHtmlPagesByDefault()
    {
        var component = RenderComponent<BitPdfViewer>(parameters =>
        {
            parameters.Add(p => p.Source, BitPdfSource.FromBytes(TestPdf.HelloWorld()));
        });

        component.WaitForPages(1);

        Assert.AreEqual(BitPdfRenderMode.Html, component.Instance.RenderMode);
        Assert.AreEqual(0, component.FindAll("[data-page='1'] canvas").Count);
        Assert.IsTrue(component.FindAll("[data-page='1'] .bit-pdv-html-page span").Count > 0);
        Assert.AreEqual(0, Context.JSInterop.Invocations.Count(i => i.Identifier == PaintCall));
    }

    [TestMethod]
    public void BitPdfViewerShouldPaintACanvasPerPageInCanvasMode()
    {
        var component = RenderComponent<BitPdfViewer>(parameters =>
        {
            parameters.Add(p => p.Source, BitPdfSource.FromBytes(TestPdf.HelloWorld()));
            parameters.Add(p => p.RenderMode, BitPdfRenderMode.Canvas);
            parameters.Add(p => p.InitialZoomMode, BitPdfZoomMode.ActualSize);
        });

        component.WaitForPages(1);

        // The page is a canvas the script paints from the display list, while the text layer still
        // carries the words for selection and search.
        component.WaitForAssertion(() => Assert.AreEqual(1, component.FindAll("[data-page='1'] canvas[data-bit-pdv-canvas]").Count));
        StringAssert.Contains(component.Find("[data-page='1'] .bit-pdv-text-layer").TextContent, "Hello");

        component.WaitForAssertion(() => Assert.IsTrue(Context.JSInterop.Invocations.Any(i => i.Identifier == PaintCall)));
        var paint = Context.JSInterop.Invocations.First(i => i.Identifier == PaintCall);
        Assert.AreEqual(1.0, paint.Arguments[2]);
    }

    [TestMethod]
    public void BitPdfViewerShouldRebuildThePagesWhenTheRenderModeChanges()
    {
        var component = RenderComponent<BitPdfViewer>(parameters =>
        {
            parameters.Add(p => p.Source, BitPdfSource.FromBytes(TestPdf.HelloWorld()));
        });

        component.WaitForPages(1);
        Assert.AreEqual(0, component.FindAll("[data-page='1'] canvas").Count);

        // Same document, another mode: the page fragments are built again rather than kept.
        component.Render(parameters => parameters.Add(p => p.RenderMode, BitPdfRenderMode.Canvas));

        component.WaitForAssertion(() => Assert.AreEqual(1, component.FindAll("[data-page='1'] canvas[data-bit-pdv-canvas]").Count));

        component.Render(parameters => parameters.Add(p => p.RenderMode, BitPdfRenderMode.Html));

        component.WaitForAssertion(() => Assert.AreEqual(0, component.FindAll("[data-page='1'] canvas").Count));
        Assert.IsTrue(component.FindAll("[data-page='1'] .bit-pdv-html-page span").Count > 0);
    }

    [TestMethod]
    public void BitPdfViewerShouldKeepOneSpanPerRunWithExactCoalescing()
    {
        var component = RenderComponent<BitPdfViewer>(parameters =>
        {
            parameters.Add(p => p.Source, BitPdfSource.FromBytes(TwoRuns()));
        });

        component.WaitForPages(1);

        Assert.AreEqual(BitPdfTextCoalescing.Exact, component.Instance.TextCoalescing);
        CollectionAssert.AreEqual(new[] { "Hel", "lo" }, PaintedTexts(component));
    }

    // The text of every painted span of the first page, in paint order.
    private static string[] PaintedTexts(IRenderedComponent<BitPdfViewer> component)
    {
        return component.Find("[data-page='1'] .bit-pdv-html-page > [aria-hidden='true']")
                        .QuerySelectorAll("span")
                        .Where(s => s.ChildElementCount == 0)
                        .Select(s => s.TextContent)
                        .ToArray();
    }

    [TestMethod]
    public void BitPdfViewerShouldMergeTheRunsOfALineWithCompactCoalescing()
    {
        var component = RenderComponent<BitPdfViewer>(parameters =>
        {
            parameters.Add(p => p.Source, BitPdfSource.FromBytes(TwoRuns()));
            parameters.Add(p => p.TextCoalescing, BitPdfTextCoalescing.Compact);
        });

        component.WaitForPages(1);

        CollectionAssert.AreEqual(new[] { "Hello" }, PaintedTexts(component));

        // The public fragment follows the parameter too.
        StringAssert.Contains(component.Instance.RenderPageHtml(1), ">Hello</span>");

        // Switching back rebuilds the page with one span per run again.
        component.Render(parameters => parameters.Add(p => p.TextCoalescing, BitPdfTextCoalescing.Exact));

        component.WaitForAssertion(() => CollectionAssert.AreEqual(new[] { "Hel", "lo" }, PaintedTexts(component)));
    }

    [TestMethod]
    public void BitPdfRendererShouldMergeAdjacentRunsOnlyInCompactMode()
    {
        var document = BitPdfDocument.Load(TwoRuns());

        string exact = new BitPdfHtmlRenderer(document.Pages[0], document.XRef).Render();
        string compact = new BitPdfHtmlRenderer(document.Pages[0], document.XRef) { TextCoalescing = BitPdfTextCoalescing.Compact }.Render();

        // The selection layer holds the line as one run in both modes; only the painted layer tells them apart.
        static int Count(string html, string text) => (html.Length - html.Replace(text, string.Empty).Length) / text.Length;

        StringAssert.Contains(exact, ">Hel</span>");
        StringAssert.Contains(exact, ">lo</span>");
        Assert.IsFalse(compact.Contains(">Hel</span>"));
        Assert.IsFalse(compact.Contains(">lo</span>"));
        Assert.AreEqual(Count(exact, ">Hello</span>") + 1, Count(compact, ">Hello</span>"), "Compact paints the two runs as one.");
    }

    [TestMethod]
    public async Task BitPdfViewerShouldEvictThePagesFurthestFromTheCurrentOneBeyondMaxRenderedPageCount()
    {
        var component = RenderComponent<BitPdfViewer>(parameters =>
        {
            parameters.Add(p => p.Source, BitPdfSource.FromBytes(TestPdf.MultiPage(6)));
            parameters.Add(p => p.MaxRenderedPageCount, 1);
        });

        component.WaitForPages(6);
        Assert.AreEqual(1, component.FindAll("[data-page='1'] .bit-pdv-html-page").Count);

        await component.InvokeAsync(() => component.Instance.LastPage());

        // Only the page in front of the reader stays built; the first reverts to a placeholder of its size,
        // to be built again when it is scrolled back into view.
        component.WaitForAssertion(() =>
        {
            Assert.AreEqual(1, component.FindAll("[data-page='6'] .bit-pdv-html-page").Count);
            Assert.AreEqual(1, component.FindAll("[data-page='1'] .bit-pdv-page-placeholder").Count);
        });
        Assert.AreEqual(1, component.FindAll("[data-page] .bit-pdv-html-page").Count);
        StringAssert.Contains(component.Find("[data-page='1']").GetAttribute("style"), "height:");

        // An evicted page is not a failed one.
        Assert.AreEqual(0, component.Instance.FailedPages.Count);
    }

    [TestMethod]
    public async Task BitPdfViewerShouldKeepEveryRenderedPageWithinMaxRenderedPageCount()
    {
        var component = RenderComponent<BitPdfViewer>(parameters =>
        {
            parameters.Add(p => p.Source, BitPdfSource.FromBytes(TestPdf.MultiPage(6)));
        });

        component.WaitForPages(6);

        await component.InvokeAsync(() => component.Instance.LastPage());

        // Two pages are far below the default cap of 24, so both stay built.
        component.WaitForAssertion(() => Assert.AreEqual(1, component.FindAll("[data-page='6'] .bit-pdv-html-page").Count));
        Assert.AreEqual(1, component.FindAll("[data-page='1'] .bit-pdv-html-page").Count);
    }

    [TestMethod]
    public async Task BitPdfViewerShouldEvictThumbnailsBeyondMaxRenderedThumbnailCount()
    {
        var component = RenderComponent<BitPdfViewer>(parameters =>
        {
            parameters.Add(p => p.Source, BitPdfSource.FromBytes(TestPdf.MultiPage(4)));
            parameters.Add(p => p.DefaultSidebar, BitPdfSidebar.Thumbnails);
            parameters.Add(p => p.MaxRenderedThumbnailCount, 1);
        });

        component.WaitForAssertion(() => Assert.AreEqual(4, component.FindAll("[data-thumb]").Count));

        // The sidebar's spy asks for the thumbnails it scrolls into view, one after the other.
        await component.InvokeAsync(() => component.Instance.EnsureThumbsRendered([1, 2, 3]));

        // Each one built evicts the ones outside its window, so only the last one asked for stays.
        component.WaitForAssertion(() =>
        {
            Assert.AreEqual(1, component.FindAll("[data-thumb='3'] .bit-pdv-html-page").Count);
            Assert.AreEqual(1, component.FindAll("[data-thumb] .bit-pdv-html-page").Count);
        });
    }

    [TestMethod]
    public async Task BitPdfViewerShouldKeepEveryRenderedThumbnailWithinMaxRenderedThumbnailCount()
    {
        var component = RenderComponent<BitPdfViewer>(parameters =>
        {
            parameters.Add(p => p.Source, BitPdfSource.FromBytes(TestPdf.MultiPage(4)));
            parameters.Add(p => p.DefaultSidebar, BitPdfSidebar.Thumbnails);
        });

        component.WaitForAssertion(() => Assert.AreEqual(4, component.FindAll("[data-thumb]").Count));

        await component.InvokeAsync(() => component.Instance.EnsureThumbsRendered([1, 2, 3]));

        component.WaitForAssertion(() => Assert.AreEqual(3, component.FindAll("[data-thumb] .bit-pdv-html-page").Count));
    }

    [TestMethod]
    public void BitPdfViewerThumbnailsShouldStayHtmlInCanvasMode()
    {
        var component = RenderComponent<BitPdfViewer>(parameters =>
        {
            parameters.Add(p => p.Source, BitPdfSource.FromBytes(TestPdf.HelloWorld()));
            parameters.Add(p => p.RenderMode, BitPdfRenderMode.Canvas);
            parameters.Add(p => p.DefaultSidebar, BitPdfSidebar.Thumbnails);
        });

        component.WaitForAssertion(() => Assert.AreEqual(1, component.FindAll("[data-thumb]").Count));

        component.InvokeAsync(() => component.Instance.EnsureThumbsRendered([1])).GetAwaiter().GetResult();

        // The script paints the main surface's canvases only, so a thumbnail that was a canvas would stay
        // blank: it is built as self-contained HTML instead.
        component.WaitForAssertion(() => Assert.AreEqual(1, component.FindAll("[data-thumb='1'] .bit-pdv-html-page").Count));
        Assert.AreEqual(0, component.FindAll("[data-thumb='1'] canvas").Count);
        Assert.IsTrue(component.FindAll("[data-thumb='1'] .bit-pdv-html-page span").Count > 0);
    }
}
