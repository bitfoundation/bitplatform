using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RangeHandler = Bit.BlazorUI.Tests.Components.Extras.PdfViewer.BitPdfRangeSourceTests.RangeHandler;

namespace Bit.BlazorUI.Tests.Components.Extras.PdfViewer;

[TestClass]
public class BitPdfRangeViewerTests : BunitTestContext
{
    [TestMethod]
    public async Task RangeViewerShouldPublishRequestedPageWithDeferredSafePropertiesAndExplicitCopies()
    {
        byte[] bytes = SpacedPages();
        using var handler = new RangeHandler(bytes);
        Services.AddSingleton(new HttpClient(handler));
        int loaded = 0, progress = 0;
        var rendered = new List<int>();
        var errors = new List<string>();
        var component = RenderComponent<BitPdfViewer>(p => p
            .Add(v => v.Source, Definition())
            .Add(v => v.OnDocumentLoaded, () => loaded++)
            .Add(v => v.OnPageRendered, (int page) => rendered.Add(page))
            .Add(v => v.OnProgress, (double _) => progress++)
            .Add(v => v.OnError, errors.Add));
        component.WaitForPages(3);
        component.WaitForAssertion(() => Assert.AreEqual(1, loaded, string.Join("; ", errors) + " Requests: " + string.Join(",", handler.Requests)));
        CollectionAssert.AreEqual(new[] { 1 }, rendered);
        Assert.AreEqual("Range title", component.Find(".bit-pdv-surface").GetAttribute("aria-label"));
        Assert.IsNull(component.Instance.GetBytes());
        Assert.IsNull(component.Instance.Metadata);
        Assert.AreEqual(0, component.Instance.FormFields.Count);
        Assert.AreEqual(0, component.Instance.StructureTree.Count);
        Assert.AreEqual(bytes.Length, component.Instance.FileSize);
        Assert.AreEqual(0, progress);
        Assert.IsTrue(handler.Requests.Count < 10);
        Assert.IsFalse(component.Find("button[aria-label='Attachments']").HasAttribute("disabled"));
        Assert.ThrowsExactly<InvalidOperationException>(() => component.Instance.ExtractPageText(1));
        string? text = null;
        await component.InvokeAsync(async () =>
        {
            text = await component.Instance.ExtractPageTextAsync(2);
            await component.Instance.GoToPage(2);
            await component.Instance.LoadDocumentDetailsAsync();
        });
        StringAssert.Contains(text!, "Range page 2");
        Assert.AreEqual(2, component.Instance.CurrentPage);
        CollectionAssert.AreEqual(new[] { 1, 2 }, rendered);
        Assert.AreEqual("Range title", component.Instance.Metadata?.Title);
        int requests = handler.Requests.Count;
        await component.InvokeAsync(() => component.Instance.LoadDocumentDetailsAsync());
        Assert.AreEqual(requests, handler.Requests.Count);
        byte[]? copy = null;
        await component.InvokeAsync(async () => copy = await component.Instance.GetBytesAsync());
        CollectionAssert.AreEqual(bytes, copy!);
        Assert.IsNull(component.Instance.GetBytes());
        Assert.AreEqual(0, errors.Count, string.Join("; ", errors));
        Assert.AreEqual(0, component.Instance.FailedPages.Count);
        await component.InvokeAsync(() => component.Instance.OpenAsync(null));
        Assert.AreEqual(0, component.Instance.PageCount);
    }

    [TestMethod]
    [DataRow(false, -1L, true)]
    [DataRow(false, 0L, false)]
    [DataRow(false, 10L, false)]
    [DataRow(true, 10L, false)]
    [DataRow(true, 100000L, true)]
    public void First200ShouldReuseItsBodyAndEnforceFallbackCap(bool unknownLength, long cap, bool opens)
    {
        using var handler = new FullHandler(TestPdf.HelloWorld(), unknownLength);
        Services.AddSingleton(new HttpClient(handler));
        var errors = new List<string>();
        var source = BitPdfSource.FromUrl("https://pdf.test/document").WithRangeLoading(new() { MaxFallbackSize = cap < 0 ? null : cap });
        var component = RenderComponent<BitPdfViewer>(p => p.Add(v => v.Source, source).Add(v => v.OnError, errors.Add));
        if (opens) component.WaitForPages(1);
        else component.WaitForAssertion(() => Assert.AreEqual(1, errors.Count));
        Assert.AreEqual(1, handler.Requests, "The first 200 response must be reused for fallback.");
        if (opens) Assert.IsNotNull(component.Instance.GetBytes());
        else StringAssert.Contains(errors.Single(), "MaxFallbackSize");
    }

    [TestMethod]
    public async Task ChangedVersionShouldDiscardTheGenerationWithoutDownloadingOrReloading()
    {
        using var handler = new RangeHandler(SpacedPages());
        Services.AddSingleton(new HttpClient(handler));
        var errors = new List<string>();
        var component = RenderComponent<BitPdfViewer>(p => p.Add(v => v.Source, Definition()).Add(v => v.OnError, errors.Add));
        component.WaitForPages(3);
        // Force a genuinely asynchronous response: cancellation must not hide the validator failure.
        handler.Pause = new(TaskCreationOptions.RunContinuationsAsynchronously);
        handler.Problem = "changed";
        var work = component.InvokeAsync(async () => { await component.Instance.ExtractPageTextAsync(2); });
        component.WaitForAssertion(() => Assert.IsTrue(handler.Requests.Count >= 3));
        handler.Pause.SetResult();
        await Assert.ThrowsAsync<IOException>(() => work);
        component.WaitForAssertion(() => Assert.AreEqual(1, errors.Count));
        Assert.AreEqual(0, component.Instance.PageCount);
        Assert.IsNull(component.Instance.Document);
        Assert.AreEqual(0, handler.LastBody!.Reads);
        int requests = handler.Requests.Count;
        await component.InvokeAsync(() => component.Instance.ExtractPageTextAsync(2));
        Assert.AreEqual(requests, handler.Requests.Count);
    }

    [TestMethod]
    [DataRow("api", false)]
    [DataRow("api", true)]
    [DataRow("panel", true)]
    [DataRow("properties", true)]
    public async Task DetailVersionErrorShouldReleaseItsGateBeforeTheHostReloads(string trigger, bool detailsOnLoaded)
    {
        using var handler = new RangeHandler(SpacedPages());
        Services.AddSingleton(new HttpClient(handler));
        var errors = new List<string>();
        bool recovering = false, recovered = false;
        var recovery = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        IRenderedComponent<BitPdfViewer>? component = null;
        component = RenderComponent<BitPdfViewer>(p => p.Add(v => v.Source, Definition())
            .Add(v => v.OnDocumentLoaded, async () =>
            {
                if (recovering && detailsOnLoaded) await component!.Instance.LoadDocumentDetailsAsync();
            })
            .Add(v => v.OnError, async (string message) =>
            {
                errors.Add(message);
                recovering = true;
                handler.Problem = null;
                await component!.Instance.OpenAsync(Definition());
                if (!detailsOnLoaded) await component.Instance.LoadDocumentDetailsAsync();
                recovered = true;
                recovery.SetResult();
            }));
        component.WaitForPages(3);
        component.WaitForAssertion(() => Assert.IsTrue((bool)typeof(BitPdfViewer)
            .GetField("_rangeEssentialsLoaded", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(component.Instance)!));
        handler.Problem = "changed";
        async Task LoadDetailsAsync()
        {
            if (trigger == "panel") await component.InvokeAsync(() => component.Instance.ShowSidebar(BitPdfSidebar.Attachments));
            else if (trigger == "properties")
            {
                await component.InvokeAsync(() => component.Instance.ToggleProperties());
                await recovery.Task;
            }
            else await component.InvokeAsync(() => component.Instance.LoadDocumentDetailsAsync());
        }
        var work = LoadDetailsAsync();
        try
        {
            if (trigger == "api") await Assert.ThrowsAsync<IOException>(() => work.WaitAsync(TimeSpan.FromSeconds(5)));
            else await work.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.IsTrue(recovered, "OnError could not await details from the replacement Range source.");
            Assert.AreEqual(1, errors.Count);
            StringAssert.Contains(errors[0], "changed");
            component.WaitForPages(3);
            Assert.AreEqual("Range title", component.Instance.Metadata?.Title);
            Assert.IsNotNull(component.Instance.Document);
            Assert.IsNull(component.Instance.GetBytes());
            Assert.AreEqual(0, component.Instance.FailedPages.Count);
        }
        finally
        {
            if (!work.IsCompleted)
            {
                // Let a failing regression unwind instead of leaving bUnit's dispatcher blocked.
                await component.InvokeAsync(() => component.Instance.OpenAsync(null)).WaitAsync(TimeSpan.FromSeconds(5));
                recovery.TrySetCanceled();
                try { await work; } catch (OperationCanceledException) { }
            }
        }
    }

    [TestMethod]
    public async Task PendingNetworkReadShouldReleaseTheEngineGateForOtherPageWorkAndReplacement()
    {
        using var handler = new RangeHandler(SpacedPages());
        Services.AddSingleton(new HttpClient(handler));
        var component = RenderComponent<BitPdfViewer>(p => p.Add(v => v.Source, Definition()));
        component.WaitForPages(3);
        handler.Pause = new(TaskCreationOptions.RunContinuationsAsynchronously);
        var work = component.InvokeAsync(async () => { await component.Instance.ExtractPageTextAsync(2); });
        component.WaitForAssertion(() => Assert.IsTrue(handler.Requests.Count >= 3));
        var gate = (SemaphoreSlim)typeof(BitPdfViewer).GetField("_renderGate", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(component.Instance)!;
        Assert.IsTrue(await gate.WaitAsync(TimeSpan.FromSeconds(2)), "HTTP awaited while holding the engine gate.");
        gate.Release();
        await component.InvokeAsync(() => component.Instance.OpenAsync(BitPdfSource.FromBytes(TestPdf.HelloWorld()))).WaitAsync(TimeSpan.FromSeconds(5));
        await Assert.ThrowsAsync<OperationCanceledException>(() => work);
        component.WaitForPages(1);
    }

    [TestMethod]
    public async Task AttachmentBodyShouldStayDeferredUntilThePanelOpens()
    {
        using var handler = new RangeHandler(SpacedPages(attachment: true));
        Services.AddSingleton(new HttpClient(handler));
        var errors = new List<string>();
        var component = RenderComponent<BitPdfViewer>(p => p.Add(v => v.Source,
            BitPdfSource.FromUrl("https://pdf.test/document").WithRangeLoading(new() { CacheSize = 512 * 1024, ChunkSize = 4096 }))
            .Add(v => v.OnError, errors.Add));
        component.WaitForPages(3);
        Assert.IsFalse(component.Instance.HasAttachments);
        Assert.AreEqual(0, component.Instance.Attachments.Count);
        component.WaitForAssertion(() => Assert.IsTrue((bool)typeof(BitPdfViewer)
            .GetField("_rangeEssentialsLoaded", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(component.Instance)!));
        int requests = handler.Requests.Count;
        Assert.Throws<BitPdfMissingDataException>(() => _ = component.Instance.Document!.Attachments);
        Assert.AreEqual(requests, handler.Requests.Count, "A synchronous Document read started HTTP.");
        await component.InvokeAsync(() => component.Instance.ShowSidebar(BitPdfSidebar.Attachments));
        Assert.IsTrue(component.Instance.HasAttachments);
        Assert.AreEqual(1, component.Instance.Attachments.Count);
        Assert.AreEqual(200000, component.Instance.Attachments[0].Content.Length);
        Assert.IsNull(component.Instance.Metadata);
        Assert.IsTrue(handler.Requests.Count > requests);
        Assert.AreEqual(0, errors.Count, string.Join("; ", errors));
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task LaterReadFailureShouldStayInRangeModeAndSearchShouldReportAfterClearingBusyState(bool timeout)
    {
        using var handler = new RangeHandler(SpacedPages());
        Services.AddSingleton(new HttpClient(handler) { Timeout = TimeSpan.FromMilliseconds(100) });
        var errors = new List<string>();
        var component = RenderComponent<BitPdfViewer>(p => p.Add(v => v.Source, Definition()).Add(v => v.OnError, errors.Add));
        component.WaitForPages(3);
        if (timeout) handler.Pause = new(TaskCreationOptions.RunContinuationsAsynchronously);
        else handler.Problem = "416";
        await component.InvokeAsync(() => component.Instance.Search("Range"));
        Assert.AreEqual(1, errors.Count);
        Assert.AreEqual("false", component.Find(".bit-pdv-surface").GetAttribute("aria-busy"));
        Assert.IsNotNull(component.Instance.Document);
        Assert.IsNull(component.Instance.GetBytes());
        Assert.IsTrue(handler.Requests.All(r => r is not null), "A published source fell back to a full download.");
        handler.Problem = null;
        handler.Pause?.SetResult();
        await component.InvokeAsync(() => component.Instance.Search("Range"));
        Assert.AreEqual(3, component.Instance.SearchMatchCount);
        Assert.AreEqual(0, component.Instance.FailedPages.Count);
    }

    [TestMethod]
    public async Task DownloadBridgeFailureMustStillDiscardAChangedGeneration()
    {
        using var handler = new RangeHandler(SpacedPages());
        Services.AddSingleton(new HttpClient(handler));
        var errors = new List<string>();
        var component = RenderComponent<BitPdfViewer>(p => p.Add(v => v.Source, Definition()).Add(v => v.OnError, errors.Add));
        component.WaitForPages(3);
        var planned = Context.JSInterop.SetupVoid("BitBlazorUI.PdfViewer.download", _ => true);
        var work = component.InvokeAsync(() => component.Instance.Download());
        component.WaitForAssertion(() => Assert.AreEqual(1, planned.Invocations.Count));
        var reference = (DotNetStreamReference)planned.Invocations.Single().Arguments[1]!;
        handler.Problem = "changed";
        await Assert.ThrowsAsync<IOException>(async () => { await reference.Stream.CopyToAsync(Stream.Null); });
        // Interop transports a stream failure back as JSException, not its .NET type.
        planned.SetException(new JSException("The original PDF stream could not be read."));
        await work;
        Assert.AreEqual(0, component.Instance.PageCount);
        Assert.IsNull(component.Instance.Document);
        Assert.AreEqual(1, errors.Count);
        StringAssert.Contains(errors[0], "changed");
    }

    [TestMethod]
    [DataRow("weak")]
    [DataRow("recovery")]
    [DataRow("budget")]
    public void InitialFallbackShouldUseTheExistingBufferedParserWithFreshState(string reason)
    {
        byte[] bytes = reason == "recovery" ? BitPdfGenericSourceTests.Fixture("damaged") : SpacedPages();
        using var handler = new FallbackHandler(bytes, reason == "weak");
        Services.AddSingleton(new HttpClient(handler));
        var errors = new List<string>();
        int progress = 0;
        var source = BitPdfSource.FromUrl("https://pdf.test/document").WithRangeLoading(new()
        { CacheSize = reason == "budget" ? 4096 : 16 * 1024 * 1024, ChunkSize = 4096 });
        var component = RenderComponent<BitPdfViewer>(p => p.Add(v => v.Source, source)
            .Add(v => v.OnError, errors.Add).Add(v => v.OnProgress, (double _) => progress++));
        component.WaitForPages(BitPdfDocument.Load(bytes).PageCount);
        Assert.IsNotNull(component.Instance.GetBytes());
        CollectionAssert.AreEqual(bytes, component.Instance.GetBytes()!);
        Assert.AreEqual(1, handler.FullRequests);
        Assert.IsTrue(handler.RangeRequests > 0);
        Assert.IsTrue(progress > 0);
        Assert.AreEqual(0, errors.Count, string.Join("; ", errors));
    }

    [TestMethod]
    public void BufferedFallbackAfterAPasswordPromptShouldReuseTheAcceptedPassword()
    {
        using var handler = new FallbackHandler(TestPdf.Encrypted("pw", contentPadding: 400000), weak: false);
        Services.AddSingleton(new HttpClient(handler));
        int prompts = 0;
        var errors = new List<string>();
        var component = RenderComponent<BitPdfViewer>(p => p
            .Add(v => v.Source, BitPdfSource.FromUrl("https://pdf.test/document")
                .WithRangeLoading(new() { CacheSize = 256 * 1024, ChunkSize = 65536 }))
            .Add(v => v.OnPasswordRequested, () => Task.FromResult(++prompts == 1 ? "pw" : null))
            .Add(v => v.OnError, errors.Add));
        component.WaitForState(() => component.Instance.PageCount == 1 || errors.Count > 0, TimeSpan.FromSeconds(10));
        Assert.AreEqual(1, prompts, "A fresh fallback parser must reuse the accepted password.");
        component.WaitForPages(1);
        Assert.IsNotNull(component.Instance.GetBytes());
        Assert.AreEqual(1, handler.FullRequests);
        Assert.AreEqual(0, errors.Count, string.Join("; ", errors));
    }

    [TestMethod]
    public async Task DistantPageLookupShouldReleaseCommittedSiblingHeadersWithinASmallCache()
    {
        const int count = 72, budget = 256 * 1024;
        using var handler = new RangeHandler(ManySpacedPages(count));
        Services.AddSingleton(new HttpClient(handler));
        var errors = new List<string>();
        var component = RenderComponent<BitPdfViewer>(p => p.Add(v => v.Source,
            BitPdfSource.FromUrl("https://pdf.test/document").WithRangeLoading(new() { CacheSize = budget }))
            .Add(v => v.OnError, errors.Add));
        component.WaitForPages(count);
        await component.InvokeAsync(() => component.Instance.LastPage()).WaitAsync(TimeSpan.FromSeconds(20));
        Assert.AreEqual(count, component.Instance.CurrentPage);
        Assert.AreEqual(0, component.Instance.FailedPages.Count, string.Join("; ", errors));
        Assert.AreEqual(0, errors.Count, string.Join("; ", errors));
        StringAssert.Contains(component.Markup, "Page 72");
        var source = typeof(BitPdfViewer).GetField("_rangeSource", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(component.Instance)!;
        int peak = (int)source.GetType().GetProperty("PeakBytes", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(source)!;
        Assert.IsTrue(peak <= budget);
        Assert.IsNull(component.Instance.GetBytes());
    }

    private static byte[] ManySpacedPages(int count)
    {
        var pdf = new StringBuilder("%PDF-1.7\n");
        int size = 4 + count * 2;
        var offsets = new int[size];
        void Obj(int n, string value) { offsets[n] = pdf.Length; pdf.Append($"{n} 0 obj\n{value}\nendobj\n"); }
        Obj(1, "<< /Type /Catalog /Pages 2 0 R >>");
        Obj(2, $"<< /Type /Pages /Count {count} /Kids [" + string.Join(" ", Enumerable.Range(0, count).Select(i => $"{4 + i * 2} 0 R")) + "] >>");
        Obj(3, "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>");
        for (int i = 0; i < count; i++)
        {
            int page = 4 + i * 2;
            Obj(page, $"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Resources << /Font << /F1 3 0 R >> >> /Contents {page + 1} 0 R >>");
            string content = $"BT /F1 18 Tf 50 700 Td (Page {i + 1}) Tj ET";
            Obj(page + 1, $"<< /Length {content.Length} >>\nstream\n{content}\nendstream");
            // Obsolete, free objects space the live page headers across raw chunks.
            pdf.Append($"{size + i} 0 obj\n<< /Length 70000 >>\nstream\n").Append('a', 70000).Append("\nendstream\nendobj\n");
        }
        int xref = pdf.Length;
        pdf.Append($"xref\n0 {size + count}\n0000000000 65535 f \n");
        for (int i = 1; i < size; i++) pdf.Append($"{offsets[i]:D10} 00000 n \n");
        for (int i = 0; i < count; i++) pdf.Append("0000000000 00000 f \n");
        pdf.Append($"trailer\n<< /Size {size + count} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF\n");
        return Encoding.ASCII.GetBytes(pdf.ToString());
    }

    private static BitPdfSource Definition() => BitPdfSource.FromUrl("https://pdf.test/document")
        .WithRangeLoading(new() { CacheSize = 128 * 1024, ChunkSize = 4096 });

    internal static byte[] SpacedPages(bool attachment = false, bool namedTitle = false)
    {
        var text = new StringBuilder("%PDF-1.7\n");
        int size = attachment ? 14 : 11;
        var offsets = new int[size];
        void Obj(int n, string value) { offsets[n] = text.Length; text.Append($"{n} 0 obj\n{value}\nendobj\n"); }
        Obj(1, attachment ? "<< /Type /Catalog /Pages 2 0 R /Names << /EmbeddedFiles 13 0 R >> >>" : "<< /Type /Catalog /Pages 2 0 R >>");
        Obj(2, "<< /Type /Pages /Count 3 /Kids [3 0 R 5 0 R 7 0 R] >>");
        Obj(9, "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>");
        Obj(10, namedTitle ? "<< /Title /NamedTitle >>" : "<< /Title (Range title) >>");
        if (attachment)
        {
            Obj(12, "<< /Type /Filespec /F (readme.txt) /EF << /F 11 0 R >> >>");
            Obj(13, "<< /Names [(readme.txt) 12 0 R] >>");
        }
        for (int i = 0; i < 3; i++)
        {
            if (attachment && i == 1) Obj(11, "<< /Type /EmbeddedFile /Length 200000 >>\nstream\n" + new string('a', 200000) + "\nendstream");
            if (i > 0) text.Append($"{50 + i} 0 obj\n<< /Length 300000 >>\nstream\n").Append(' ', 300000).Append("\nendstream\nendobj\n");
            int page = 3 + i * 2;
            Obj(page, $"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 {612 + i * 10} {792 + i * 10}] /Resources << /Font << /F1 9 0 R >> >> /Contents {page + 1} 0 R {(attachment && i == 0 ? "/Annots [<< /Type /Annot /Subtype /FileAttachment /Rect [10 10 20 20] /FS 12 0 R >>]" : "")} >>");
            string ops = $"BT /F1 18 Tf 50 700 Td (Range page {i + 1}) Tj ET";
            Obj(page + 1, $"<< /Length {ops.Length} >>\nstream\n{ops}\nendstream");
        }
        int xref = text.Length;
        text.Append($"xref\n0 {size}\n0000000000 65535 f \n");
        for (int i = 1; i < size; i++) text.Append($"{offsets[i]:D10} 00000 n \n");
        text.Append($"trailer\n<< /Size {size} /Root 1 0 R /Info 10 0 R >>\nstartxref\n{xref}\n%%EOF\n");
        return Encoding.ASCII.GetBytes(text.ToString());
    }

    private sealed class FallbackHandler(byte[] bytes, bool weak) : HttpMessageHandler
    {
        internal int RangeRequests, FullRequests;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.Headers.Range is not { } range)
            { FullRequests++; return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(bytes) }); }
            RangeRequests++;
            var part = range.Ranges.Single();
            int from = (int)part.From!.Value, to = (int)Math.Min(bytes.Length - 1, part.To!.Value);
            var response = new HttpResponseMessage(HttpStatusCode.PartialContent) { Content = new ByteArrayContent(bytes[from..(to + 1)]) };
            response.Content.Headers.ContentRange = new ContentRangeHeaderValue(from, to, bytes.Length);
            response.Headers.TryAddWithoutValidation("ETag", weak ? "W/\"weak\"" : "\"strong\"");
            return Task.FromResult(response);
        }
    }

    private sealed class FullHandler(byte[] bytes, bool unknownLength) : HttpMessageHandler
    {
        internal int Requests;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests++;
            HttpContent content = unknownLength ? new UnknownContent(bytes) : new ByteArrayContent(bytes);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = content });
        }
    }
    private sealed class UnknownContent(byte[] bytes) : HttpContent
    {
        protected override bool TryComputeLength(out long length) { length = 0; return false; }
        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context) => stream.WriteAsync(bytes).AsTask();
        protected override Task<Stream> CreateContentReadStreamAsync() => Task.FromResult<Stream>(new MemoryStream(bytes));
    }
}
