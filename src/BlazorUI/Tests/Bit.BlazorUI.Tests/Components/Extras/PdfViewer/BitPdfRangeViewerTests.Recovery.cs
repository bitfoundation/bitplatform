using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text;
using System.Text.Json;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RangeHandler = Bit.BlazorUI.Tests.Components.Extras.PdfViewer.BitPdfRangeSourceTests.RangeHandler;

namespace Bit.BlazorUI.Tests.Components.Extras.PdfViewer;

public partial class BitPdfRangeViewerTests
{
    [TestMethod]
    [DataRow(BitPdfSidebar.Bookmarks, false)]
    [DataRow(BitPdfSidebar.Layers, false)]
    [DataRow(BitPdfSidebar.Bookmarks, true)]
    [DataRow(BitPdfSidebar.Layers, true)]
    public async Task SidebarRequestedByLoadedCallbackShouldWaitForDeferredEssentials(BitPdfSidebar sidebar, bool empty)
    {
        byte[] bytes = SpacedPages(essentials: true);
        if (empty)
        {
            // Keep the empty outline in a later range too. Preserve every xref
            // offset while removing its child and the catalog's layer reference.
            const string child = "/First 21 0 R", layer = "22 0 R";
            bytes = Encoding.ASCII.GetBytes(Encoding.ASCII.GetString(bytes)
                .Replace(child, new string(' ', child.Length), StringComparison.Ordinal)
                .Replace(layer, new string(' ', layer.Length), StringComparison.Ordinal));
        }
        using var handler = new RangeHandler(bytes);
        Services.AddSingleton(new HttpClient(handler));
        var changed = new List<BitPdfSidebar>();
        var errors = new List<string>();
        var requested = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var closed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var resume = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        BitPdfViewer? viewer = null;
        // Assign the callback's owner on the renderer before the asynchronous
        // load resumes. Keep Source on the first render, as the host would.
        var component = await Renderer.Dispatcher.InvokeAsync(() =>
        {
            var rendered = RenderComponent<BitPdfViewer>(p => p
                .Add(v => v.Source, Definition()).Add(v => v.OnError, errors.Add)
                .Add(v => v.OnDocumentLoaded, async () =>
                {
                    handler.Pause = resume;
                    await viewer!.ShowSidebar(sidebar);
                    requested.TrySetResult();
                })
                .Add(v => v.OnSidebarChanged, (BitPdfSidebar value) =>
                {
                    changed.Add(value);
                    if (value == BitPdfSidebar.None) closed.TrySetResult();
                }));
            viewer = rendered.Instance;
            return rendered;
        });

        try
        {
            await requested.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.AreEqual(sidebar, component.Instance.Sidebar);
            Assert.IsFalse(component.Instance.HasOutline);
            Assert.IsFalse(component.Instance.HasLayers);
            CollectionAssert.AreEqual(new[] { sidebar }, changed);
        }
        finally { resume.TrySetResult(); }

        if (empty) await closed.Task.WaitAsync(TimeSpan.FromSeconds(5));
        component.WaitForAssertion(() =>
        {
            CollectionAssert.AreEqual(empty ? new[] { sidebar, BitPdfSidebar.None } : new[] { sidebar }, changed);
            Assert.AreEqual(empty ? BitPdfSidebar.None : sidebar, component.Instance.Sidebar);
            Assert.AreEqual(!empty, component.Instance.HasOutline);
            Assert.AreEqual(!empty, component.Instance.HasLayers);
            Assert.AreEqual(0, errors.Count, string.Join("; ", errors));
        }, TimeSpan.FromSeconds(5));
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task DeferredEssentialsShouldRetryTransientFailureAndStopAfterThreeAttempts(bool persistent)
    {
        using var handler = new RangeHandler(SpacedPages(essentials: true));
        Services.AddSingleton(new HttpClient(handler));
        var errors = new List<string>();
        var reported = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var component = RenderComponent<BitPdfViewer>(p => p.Add(v => v.Source, Definition())
            .Add(v => v.OnError, (string message) => { errors.Add(message); reported.TrySetResult(); })
            .Add(v => v.OnDocumentLoaded, () => handler.FailuresRemaining = persistent ? 10 : 1));
        component.WaitForPages(3);
        if (persistent)
        {
            await reported.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.AreEqual(1, errors.Count);
            Assert.AreEqual(3, handler.Failures);
            Assert.IsFalse(component.Instance.HasOutline);
            Assert.IsFalse(component.Instance.HasLayers);
            await component.InvokeAsync(() => component.Render());
            Assert.AreEqual(3, handler.Failures, "A render must not start an unbounded background retry loop.");
            handler.FailuresRemaining = 0;
            await component.InvokeAsync(() => component.Instance.LoadDocumentDetailsAsync());
        }
        component.WaitForAssertion(() =>
        {
            Assert.IsTrue(component.Instance.HasOutline);
            Assert.IsTrue(component.Instance.HasLayers);
            Assert.AreEqual(persistent ? 3 : 1, handler.Failures);
            Assert.AreEqual(persistent ? 1 : 0, errors.Count);
        }, TimeSpan.FromSeconds(5));
    }

    [TestMethod]
    [DataRow(false, false)]
    [DataRow(false, true)]
    [DataRow(true, false)]
    [DataRow(true, true)]
    public async Task RenderingShouldRecoverTransientReadsAndBoundPersistentFailures(bool thumbnail, bool persistent)
    {
        using var handler = new RangeHandler(BitPdfRangeViewerTests.SpacedPages());
        Services.AddSingleton(new HttpClient(handler));
        var errors = new List<string>();
        var component = RenderComponent<BitPdfViewer>(p => p.Add(v => v.Source, Definition()).Add(v => v.OnError, errors.Add));
        component.WaitForPages(3);
        handler.FailuresRemaining = persistent ? 10 : 1;
        await component.InvokeAsync(() => thumbnail ? component.Instance.EnsureThumbsRendered([2]) : component.Instance.EnsurePagesRendered([2]));
        Assert.AreEqual(persistent ? 3 : 1, handler.Failures);
        Assert.AreEqual(persistent ? 1 : 0, errors.Count, string.Join("; ", errors));
        Assert.AreEqual(0, component.Instance.FailedPages.Count, "Transport outages must not become permanent page failures.");
        int requests = handler.Requests.Count;
        await component.InvokeAsync(() => thumbnail ? component.Instance.EnsureThumbsRendered([2]) : component.Instance.EnsurePagesRendered([2]));
        Assert.AreEqual(requests, handler.Requests.Count);
        if (persistent)
        {
            handler.FailuresRemaining = 0;
            await Task.Delay(2100);
            await component.InvokeAsync(() => thumbnail ? component.Instance.EnsureThumbsRendered([2]) : component.Instance.EnsurePagesRendered([2]));
            Assert.IsTrue(handler.Requests.Count > requests, "The pump must retry a recovered endpoint after the cooldown.");
            Assert.AreEqual(1, errors.Count);
            Assert.AreEqual(0, component.Instance.FailedPages.Count);
        }
        Assert.IsNull(component.Instance.GetBytes());
        Assert.IsTrue(handler.Requests.All(r => r is not null));
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task RenderTimeoutShouldSurfaceItsFirstFailureAndAllowALaterViewportRetry(bool thumbnail)
    {
        using var handler = new RangeHandler(BitPdfRangeViewerTests.SpacedPages());
        Services.AddSingleton(new HttpClient(handler) { Timeout = TimeSpan.FromMilliseconds(100) });
        var errors = new List<string>();
        var component = RenderComponent<BitPdfViewer>(p => p.Add(v => v.Source, Definition()).Add(v => v.OnError, errors.Add));
        component.WaitForPages(3);
        int requests = handler.Requests.Count;
        handler.Pause = new(TaskCreationOptions.RunContinuationsAsynchronously);
        await component.InvokeAsync(() => thumbnail ? component.Instance.EnsureThumbsRendered([2]) : component.Instance.EnsurePagesRendered([2]));
        Assert.AreEqual(requests + 1, handler.Requests.Count, "HttpClient.Timeout must not be retried automatically.");
        Assert.AreEqual(1, errors.Count);
        Assert.AreEqual(0, component.Instance.FailedPages.Count);
        handler.Pause.SetResult();
        await Task.Delay(2100);
        await component.InvokeAsync(() => thumbnail ? component.Instance.EnsureThumbsRendered([2]) : component.Instance.EnsurePagesRendered([2]));
        Assert.IsTrue(handler.Requests.Count > requests + 1);
        Assert.AreEqual(1, errors.Count);
        Assert.AreEqual(0, component.Instance.FailedPages.Count);
    }

    [TestMethod]
    [DataRow(BitPdfRenderMode.Html)]
    [DataRow(BitPdfRenderMode.Canvas)]
    public async Task UnusedOversizedXObjectMustNotPreventRenderingReferencedNestedForms(BitPdfRenderMode mode)
    {
        byte[] bytes = UnusedResourcePdf();
        using var handler = new RangeHandler(bytes);
        Services.AddSingleton(new HttpClient(handler));
        var errors = new List<string>();
        int rendered = 0;
        var component = RenderComponent<BitPdfViewer>(p => p.Add(v => v.Source,
            BitPdfSource.FromUrl("https://pdf.test/document").WithRangeLoading(new() { CacheSize = 256 * 1024, ChunkSize = 65536 }))
            .Add(v => v.RenderMode, mode)
            .Add(v => v.OnPageRendered, (int _) => rendered++).Add(v => v.OnError, errors.Add));
        component.WaitForAssertion(() => Assert.AreEqual(1, rendered, string.Join("; ", errors)));
        Assert.AreEqual(0, errors.Count);
        Assert.AreEqual(0, component.Instance.FailedPages.Count);
        Assert.IsNull(component.Instance.GetBytes(), "Unused resources must not cause buffered fallback.");
        string? extracted = null;
        await component.InvokeAsync(async () => extracted = await component.Instance.ExtractPageTextAsync(1));
        StringAssert.Contains(extracted!, "Used form");
        Assert.IsTrue(handler.Requests.Count < 12, string.Join(",", handler.Requests));
    }

    [TestMethod]
    public async Task VersionFailureShouldClearSearchAndReleaseAPrintWaitingForCanvasPaint()
    {
        using var handler = new RangeHandler(SpacedPages());
        Services.AddSingleton(new HttpClient(handler));
        var errors = new List<string>();
        int printing = 0;
        var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        // Register before rendering starts. Only print's later-page paints wait;
        // the initial first-page paint continues normally.
        var paint = Context.JSInterop.SetupVoid("BitBlazorUI.PdfViewer.paintCanvasPages", invocation =>
            printing == 1 && JsonSerializer.SerializeToElement(invocation.Arguments[1], jsonOptions)
                .EnumerateArray().Any(page => page.GetProperty("page").GetInt32() > 1));
        try
        {
            var component = RenderComponent<BitPdfViewer>(p => p.Add(v => v.Source, Definition())
                .Add(v => v.RenderMode, BitPdfRenderMode.Canvas).Add(v => v.OnError, errors.Add)
                .Add(v => v.OnPrinting, (BitPdfPrintArgs args) => args.Cancel = ++printing > 1));
            component.WaitForPages(3);
            await component.InvokeAsync(() => component.Instance.Search("Range"));
            Assert.AreEqual(3, component.Instance.SearchMatchCount);
            var print = component.InvokeAsync(() => component.Instance.Print());
            component.WaitForAssertion(() =>
            {
                Assert.IsTrue(paint.Invocations.Any(invocation => JsonSerializer.SerializeToElement(invocation.Arguments[1], jsonOptions)
                    .EnumerateArray().Any(page => page.GetProperty("page").GetInt32() == 3)));
                Assert.AreEqual("false", component.Find(".bit-pdv-surface").GetAttribute("aria-busy"));
                Assert.IsFalse(print.IsCompleted, "Print must still await the held canvas paint.");
            }, TimeSpan.FromSeconds(5));
            handler.Problem = "changed";
            await Assert.ThrowsAsync<IOException>(() => component.InvokeAsync(async () => { await component.Instance.GetBytesAsync(); }));
            await print.WaitAsync(TimeSpan.FromSeconds(3));
            Assert.AreEqual(0, component.Instance.SearchMatchCount);
            Assert.AreEqual(0, component.Instance.SearchMatchIndex);
            Assert.AreEqual(0, component.Instance.PageCount);
            Assert.AreEqual(1, errors.Count);
            Assert.AreEqual(0, Context.InvocationsOf("BitBlazorUI.PdfViewer.print").Count);

            // A new public print request must reach the host callback. This pins
            // release of the busy guard without reading the viewer's private state.
            await component.InvokeAsync(() => component.Instance.OpenAsync(BitPdfSource.FromBytes(TestPdf.HelloWorld())));
            await component.InvokeAsync(() => component.Instance.Print());
            Assert.AreEqual(2, printing);
        }
        finally { paint.SetVoidResult(); }
    }

    private static byte[] UnusedResourcePdf()
    {
        var text = new StringBuilder("%PDF-1.7\n");
        var offsets = new int[10];
        void Obj(int number, string value) { offsets[number] = text.Length; text.Append($"{number} 0 obj\n{value}\nendobj\n"); }
        void Stream(int number, string content, string attributes = "")
            => Obj(number, $"<< /Length {content.Length} {attributes} >>\nstream\n{content}\nendstream");
        Obj(1, "<< /Type /Catalog /Pages 2 0 R >>");
        Obj(2, "<< /Type /Pages /Count 1 /Kids [3 0 R] >>");
        Obj(3, "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Contents 8 0 R /Resources << /Font << /F1 4 0 R >> /XObject << /Used 5 0 R /Unused 6 0 R >> >> >>");
        Obj(4, "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>");
        Stream(8, "/Used Do");
        Stream(7, new string(' ', 70000));
        Stream(5, "/Nested Do", "/Type /XObject /Subtype /Form /BBox [0 0 612 792] /Resources << /Font << /F1 4 0 R >> /XObject << /Nested 9 0 R /AlsoUnused 6 0 R >> >>");
        Stream(9, "BT /F1 18 Tf 50 700 Td (Used form) Tj ET", "/Type /XObject /Subtype /Form /BBox [0 0 612 792]");
        Stream(6, new string(' ', 512 * 1024), "/Type /XObject /Subtype /Image /Width 512 /Height 1024 /BitsPerComponent 8 /ColorSpace /DeviceGray");
        int xref = text.Length;
        text.Append("xref\n0 10\n0000000000 65535 f \n");
        for (int i = 1; i < offsets.Length; i++) text.Append($"{offsets[i]:D10} 00000 n \n");
        text.Append($"trailer\n<< /Size 10 /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF\n");
        return Encoding.ASCII.GetBytes(text.ToString());
    }
}
