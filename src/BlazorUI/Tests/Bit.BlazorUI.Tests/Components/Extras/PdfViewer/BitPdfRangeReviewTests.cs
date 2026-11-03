using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RangeHandler = Bit.BlazorUI.Tests.Components.Extras.PdfViewer.BitPdfRangeSourceTests.RangeHandler;

namespace Bit.BlazorUI.Tests.Components.Extras.PdfViewer;

[TestClass]
public class BitPdfRangeReviewTests : BunitTestContext
{
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;
    private static BitPdfSource Definition() => BitPdfSource.FromUrl("https://pdf.test/document")
        .WithRangeLoading(new() { CacheSize = 128 * 1024, ChunkSize = 4096 });
    private static bool EssentialsLoaded(BitPdfViewer viewer)
        => (bool)typeof(BitPdfViewer).GetField("_rangeEssentialsLoaded", Flags)!.GetValue(viewer)!;

    [TestMethod]
    [DataRow(BitPdfSidebar.Bookmarks, false)]
    [DataRow(BitPdfSidebar.Layers, false)]
    [DataRow(BitPdfSidebar.Bookmarks, true)]
    [DataRow(BitPdfSidebar.Layers, true)]
    public void SidebarRequestedByLoadedCallbackShouldWaitForDeferredEssentials(BitPdfSidebar sidebar, bool empty)
    {
        using var handler = new RangeHandler(BitPdfRangeViewerTests.SpacedPages(essentials: !empty));
        Services.AddSingleton(new HttpClient(handler));
        var changed = new List<BitPdfSidebar>();
        var errors = new List<string>();
        IRenderedComponent<BitPdfViewer>? component = null;
        component = RenderComponent<BitPdfViewer>(p => p
            .Add(v => v.Source, Definition()).Add(v => v.OnError, errors.Add)
            .Add(v => v.OnDocumentLoaded, async () => await component!.Instance.ShowSidebar(sidebar))
            .Add(v => v.OnSidebarChanged, (BitPdfSidebar value) => changed.Add(value)));
        component.WaitForAssertion(() => Assert.IsTrue(EssentialsLoaded(component.Instance)), TimeSpan.FromSeconds(5));
        component.WaitForAssertion(() => Assert.AreEqual(empty ? BitPdfSidebar.None : sidebar, component.Instance.Sidebar));
        CollectionAssert.AreEqual(empty ? new[] { sidebar, BitPdfSidebar.None } : new[] { sidebar }, changed);
        Assert.AreEqual(0, errors.Count, string.Join("; ", errors));

    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task DeferredEssentialsShouldRetryTransientFailureAndStopAfterThreeAttempts(bool persistent)
    {
        using var handler = new RangeHandler(BitPdfRangeViewerTests.SpacedPages(essentials: true));
        Services.AddSingleton(new HttpClient(handler));
        var errors = new List<string>();
        var component = RenderComponent<BitPdfViewer>(p => p.Add(v => v.Source, Definition())
            .Add(v => v.OnError, errors.Add)
            .Add(v => v.OnDocumentLoaded, () => handler.FailuresRemaining = persistent ? 10 : 1));
        component.WaitForPages(3);
        await component.InvokeAsync(async () =>
        {
            var background = (Task?)typeof(BitPdfViewer).GetField("_rangeBackgroundTask", Flags)!.GetValue(component.Instance);
            Assert.IsNotNull(background);
            await background.WaitAsync(TimeSpan.FromSeconds(5));
        });
        if (persistent)
        {
            Assert.AreEqual(1, errors.Count);
            Assert.AreEqual(3, handler.Failures);
            Assert.IsFalse(EssentialsLoaded(component.Instance));
            await component.InvokeAsync(() => component.Render());
            Assert.AreEqual(3, handler.Failures, "A render must not start an unbounded background retry loop.");
            handler.FailuresRemaining = 0;
            await component.InvokeAsync(() => component.Instance.LoadDocumentDetailsAsync());
        }
        component.WaitForAssertion(() => Assert.IsTrue(EssentialsLoaded(component.Instance)), TimeSpan.FromSeconds(5));
        Assert.IsTrue(component.Instance.HasOutline);
        Assert.IsTrue(component.Instance.HasLayers);
        Assert.AreEqual(persistent ? 3 : 1, handler.Failures);
        Assert.AreEqual(persistent ? 1 : 0, errors.Count);
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
        using var handler = new RangeHandler(BitPdfRangeViewerTests.SpacedPages());
        Services.AddSingleton(new HttpClient(handler));
        var errors = new List<string>();
        var component = RenderComponent<BitPdfViewer>(p => p.Add(v => v.Source, Definition())
            .Add(v => v.RenderMode, BitPdfRenderMode.Canvas).Add(v => v.OnError, errors.Add));
        component.WaitForPages(3);
        await component.InvokeAsync(() => component.Instance.Search("Range"));
        Assert.AreEqual(3, component.Instance.SearchMatchCount);
        var paint = Context.JSInterop.SetupVoid("BitBlazorUI.PdfViewer.paintCanvasPages", _ => true);
        var print = component.InvokeAsync(() => component.Instance.Print());
        component.WaitForAssertion(() => Assert.IsNotNull(typeof(BitPdfViewer).GetField("_canvasPaintSignal", Flags)!.GetValue(component.Instance)), TimeSpan.FromSeconds(5));
        handler.Problem = "changed";
        try
        {
            await Assert.ThrowsAsync<IOException>(() => component.InvokeAsync(async () => { await component.Instance.GetBytesAsync(); }));
            await print.WaitAsync(TimeSpan.FromSeconds(3));
            Assert.AreEqual(0, component.Instance.SearchMatchCount);
            Assert.AreEqual(0, component.Instance.SearchMatchIndex);
            Assert.AreEqual(0, component.Instance.PageCount);
            Assert.AreEqual(1, errors.Count);
            Assert.AreEqual(0, Context.InvocationsOf("BitBlazorUI.PdfViewer.print").Count);
            Assert.IsFalse((bool)typeof(BitPdfViewer).GetField("_printBusy", Flags)!.GetValue(component.Instance)!);
        }
        finally { paint.SetVoidResult(); }
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task StreamDetailContentFailureMustNotHideIndependentDetailsButSourceFailureMustPropagate(bool sourceFailure)
    {
        var component = RenderComponent<BitPdfViewer>(p => p.Add(v => v.Source,
            BitPdfSource.FromStreamFactory(_ => Task.FromResult<Stream>(new MemoryStream(TestPdf.HelloWorld())))));
        component.WaitForPages(1);
        var document = component.Instance.Document!;
        var error = sourceFailure
            ? (Exception)Activator.CreateInstance(typeof(BitPdfDocument).Assembly.GetType("Bit.BlazorUI.BitPdfByteSourceIOException", true)!, ["Provider failed.", null])!
            : new BitPdfFormatException("Malformed form.");
        document.Catalog.Set("AcroForm", new BitPdfRef(12345, 0));
        document.Catalog.XRef = new ThrowingXRef(error);
        var element = new BitPdfDict(); element.Set("S", BitPdfName.Get("P"));
        var structure = new BitPdfDict(); structure.Set("K", element);
        document.Catalog.Set("StructTreeRoot", structure);
        if (sourceFailure)
            await Assert.ThrowsAsync<IOException>(() => component.InvokeAsync(() => component.Instance.LoadDocumentDetailsAsync()));
        else
        {
            await component.InvokeAsync(() => component.Instance.LoadDocumentDetailsAsync());
            Assert.AreEqual(0, component.Instance.FormFields.Count);
            Assert.AreEqual(1, component.Instance.StructureTree.Count);
            Assert.AreEqual("P", component.Instance.StructureTree[0].Type);
        }
    }

    [TestMethod]
    [DataRow("FlateDecode")]
    [DataRow("RunLengthDecode")]
    [DataRow("ASCII85Decode")]
    public void HintDecoderMustEnforceItsLimitDuringExpansion(string filter)
    {
        byte[] encoded;
        int expected;
        if (filter == "FlateDecode")
        {
            using var output = new MemoryStream();
            using (var compressor = new ZLibStream(output, CompressionLevel.Fastest, leaveOpen: true))
                compressor.Write(new byte[4 * 1024 * 1024]);
            encoded = output.ToArray(); expected = 4 * 1024 * 1024;
        }
        else if (filter == "RunLengthDecode")
        { encoded = Enumerable.Range(0, 256).SelectMany(_ => new byte[] { 129, 42 }).Append((byte)128).ToArray(); expected = 32768; }
        else { encoded = Enumerable.Repeat((byte)'z', 1024).ToArray(); expected = 4096; }
        var dictionary = new BitPdfDict(); dictionary.Set("Filter", BitPdfName.Get(filter));
        var stream = new BitPdfStream(encoded) { Dict = dictionary };
        var decode = typeof(BitPdfStreamDecoder).GetMethods(Flags).Single(m => m.Name == "Decode" && m.GetParameters()[1].ParameterType == typeof(int));
        long before = GC.GetAllocatedBytesForCurrentThread();
        var error = Assert.ThrowsExactly<TargetInvocationException>(() => decode.Invoke(null, [stream, 1024, false]));
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.IsInstanceOfType<BitPdfFormatException>(error.InnerException);
        Assert.IsTrue(allocated < 1024 * 1024, $"Small hint budget allocated {allocated} bytes.");
        Assert.AreEqual(expected, BitPdfStreamDecoder.Decode(stream).Length, "The public decoder keeps its existing limit.");
    }

    [TestMethod]
    public void ReparsedCidWidthArraysMustShareAStableFontBudgetAcrossAttempts()
    {
        var budget = typeof(BitPdfDocument).Assembly.GetType("Bit.BlazorUI.BitPdfBudget", true)!;
        using var scope = (IDisposable)budget.GetMethod("BeginRetryScope", Flags)!.Invoke(null, null)!;
        var xref = new ReparsedFontXRef();
        BitPdfDict Font()
        {
            var font = new BitPdfDict(xref);
            font.Set("Subtype", BitPdfName.Get("Type0"));
            font.Set("DescendantFonts", new List<object?> { new BitPdfRef(1, 0) });
            return font;
        }
        Assert.ThrowsExactly<InvalidOperationException>(() => BitPdfFont.Create(Font(), xref));
        var parsed = BitPdfFont.Create(Font(), xref);
        Assert.AreEqual(1000d, parsed.Decode([0, 0])[0].Width1000);
        Assert.AreEqual(1, xref.Misses, "A new /W list must not reset the million-entry expansion budget.");
    }

    private sealed class ThrowingXRef(Exception error) : IBitPdfXRef
    {
        public object? Fetch(BitPdfRef reference, bool suppressEncryption = false) => throw error;
        public object? FetchIfRef(object? value, bool suppressEncryption = false) => value is BitPdfRef reference ? Fetch(reference) : value;
    }

    private sealed class ReparsedFontXRef : IBitPdfXRef
    {
        internal int Misses;
        public object? Fetch(BitPdfRef reference, bool suppressEncryption = false)
        {
            if (reference.Num == 2) { Misses++; throw new InvalidOperationException("Missing width operand."); }
            var widths = new List<object?>();
            for (int i = 0; i < 16; i++) widths.AddRange(new object?[] { 0d, 65535d, 100d });
            widths.Add(new BitPdfRef(2, 0));
            var dictionary = new BitPdfDict(this); dictionary.Set("W", widths);
            return dictionary;
        }
        public object? FetchIfRef(object? value, bool suppressEncryption = false) => value is BitPdfRef reference ? Fetch(reference) : value;
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
