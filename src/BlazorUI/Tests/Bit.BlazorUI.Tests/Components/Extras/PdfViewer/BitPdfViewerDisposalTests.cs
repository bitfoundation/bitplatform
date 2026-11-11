using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RangeHandler = Bit.BlazorUI.Tests.Components.Extras.PdfViewer.BitPdfRangeSourceTests.RangeHandler;

namespace Bit.BlazorUI.Tests.Components.Extras.PdfViewer;

[TestClass]
public class BitPdfViewerDisposalTests : BunitTestContext
{
    [TestMethod]
    [DataRow("buffered", "page")]
    [DataRow("buffered", "link")]
    [DataRow("buffered", "resize")]
    [DataRow("buffered", "wheel")]
    [DataRow("stream", "page")]
    [DataRow("stream", "link")]
    [DataRow("stream", "resize")]
    [DataRow("stream", "wheel")]
    [DataRow("range", "page")]
    [DataRow("range", "link")]
    [DataRow("range", "resize")]
    [DataRow("range", "wheel")]
    public async Task LateBrowserCallbacksMustNotChangeStateWhileTeardownIsPending(string kind, string callback)
    {
        byte[] bytes = BitPdfRangeViewerTests.SpacedPages();
        using var handler = new RangeHandler(bytes);
        Services.AddSingleton(new HttpClient(handler));
        var source = kind == "range" ? Definition() : kind == "stream"
            ? BitPdfSource.FromStreamFactory(_ => Task.FromResult<Stream>(new MemoryStream(bytes))) : BitPdfSource.FromBytes(bytes);
        int pageEvents = 0, zoomEvents = 0;
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var teardown = Context.JSInterop.SetupVoid("BitBlazorUI.PdfViewer.disposeScrollSpy", _ => { entered.TrySetResult(); return true; });
        var component = RenderComponent<BitPdfViewer>(p => p.Add(v => v.Source, source)
            .Add(v => v.OnPageChanged, (int _) => pageEvents++)
            .Add(v => v.OnZoomChanged, (double _) => zoomEvents++));
        component.WaitForPages(3);
        var disposal = component.InvokeAsync(async () => await component.Instance.DisposeAsync());
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.CancellationToken);
        int page = component.Instance.CurrentPage;
        double zoom = component.Instance.Zoom;
        pageEvents = 0; zoomEvents = 0;
        int anchors = Context.InvocationsOf("BitBlazorUI.PdfViewer.stashViewAnchor").Count;
        int measurements = Context.InvocationsOf("BitBlazorUI.PdfViewer.getViewport").Count;
        try
        {
            await component.InvokeAsync(() => callback switch
            {
                "page" => component.Instance.OnPageVisible(2),
                "link" => component.Instance.OnLinkNavigate(2, null),
                "resize" => component.Instance.OnViewportResized(),
                _ => component.Instance.OnWheelZoom(-1),
            });
            Assert.AreEqual(page, component.Instance.CurrentPage);
            Assert.AreEqual(zoom, component.Instance.Zoom);
            Assert.AreEqual(0, pageEvents);
            Assert.AreEqual(0, zoomEvents);
            Assert.HasCount(anchors, Context.InvocationsOf("BitBlazorUI.PdfViewer.stashViewAnchor"));
            Assert.HasCount(measurements, Context.InvocationsOf("BitBlazorUI.PdfViewer.getViewport"));
        }
        finally
        {
            teardown.SetVoidResult();
            await disposal.WaitAsync(TimeSpan.FromSeconds(5), TestContext.CancellationToken);
        }
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task RetiringANonBufferedSourceMustClearTheViewerDocumentBeforeJsTeardown(bool range)
    {
        byte[] bytes = BitPdfRangeViewerTests.SpacedPages();
        using var handler = new RangeHandler(bytes);
        Services.AddSingleton(new HttpClient(handler));
        var source = range ? Definition() : BitPdfSource.FromStreamFactory(_ => Task.FromResult<Stream>(new MemoryStream(bytes)));
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var teardown = Context.JSInterop.SetupVoid("BitBlazorUI.PdfViewer.disposeScrollSpy", _ => { entered.TrySetResult(); return true; });
        var component = RenderComponent<BitPdfViewer>(p => p.Add(v => v.Source, source));
        component.WaitForPages(3);
        var disposal = component.InvokeAsync(async () => await component.Instance.DisposeAsync());
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.CancellationToken);
        try
        {
            Assert.IsNull(component.Instance.Document, "The viewer must not expose a retired source's document.");
            Assert.IsNull(component.Instance.Metadata);
            Assert.IsEmpty(component.Instance.FormFields);
            Assert.IsEmpty(component.Instance.StructureTree);
            Assert.AreEqual(0L, component.Instance.FileSize);
        }
        finally
        {
            teardown.SetVoidResult();
            await disposal.WaitAsync(TimeSpan.FromSeconds(5), TestContext.CancellationToken);
        }
    }

    private static BitPdfSource Definition() => BitPdfSource.FromUrl("https://pdf.test/document")
        .WithRangeLoading(new() { CacheSize = 128 * 1024, ChunkSize = 4096 });

    public global::Microsoft.VisualStudio.TestTools.UnitTesting.TestContext TestContext { get; set; }
}
