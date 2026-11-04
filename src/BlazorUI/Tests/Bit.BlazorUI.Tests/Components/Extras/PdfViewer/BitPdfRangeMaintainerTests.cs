using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RangeAccess = Bit.BlazorUI.Tests.Components.Extras.PdfViewer.BitPdfRangeSourceTests.RangeAccess;
using RangeHandler = Bit.BlazorUI.Tests.Components.Extras.PdfViewer.BitPdfRangeSourceTests.RangeHandler;

namespace Bit.BlazorUI.Tests.Components.Extras.PdfViewer;

/// <summary>Regressions for the maintainer's final Range-loading review.</summary>
[TestClass]
public class BitPdfRangeMaintainerTests : BunitTestContext
{
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    private static BitPdfSource Definition(int cacheSize = 16 * 1024 * 1024, int chunkSize = 4096)
        => BitPdfSource.FromUrl("https://pdf.test/document").WithRangeLoading(new()
        {
            CacheSize = cacheSize,
            ChunkSize = chunkSize,
            MaxFallbackSize = 0,
        });

    [TestMethod]
    public void BufferedObjectStreamContentFailureShouldDecodeOnlyOnceAcrossReferences()
    {
        var file = new BitPdfEngineTestFile();
        file.Obj(1, "<< /Type /Catalog /Pages 2 0 R >>");
        file.Obj(2, "<< /Type /Pages /Count 0 /Kids [] >>");
        file.Stream(3, new byte[] { 255, 255, 255 }, "/Type /ObjStm /N 2 /First 8 /Filter /FlateDecode");
        file.XRefStream(4, "/Root 1 0 R", new[]
        {
            (0, 0, 0, 65535), (1, 1, file.Offsets[1], 0), (2, 1, file.Offsets[2], 0),
            (3, 1, file.Offsets[3], 0), (6, 2, 3, 0), (7, 2, 3, 1),
        });
        var document = BitPdfDocument.Load(file.ToArray());
        var raw = (BitPdfBaseStream)document.XRef.Fetch(new BitPdfRef(3, 0))!;
        var counted = new CountingDecodeStream(raw) { Dict = raw.Dict };
        var cache = (Dictionary<(int, int), object?>)typeof(BitPdfXRef).GetField("_cache", Flags)!.GetValue(document.XRef)!;
        cache[(3, 0)] = counted;

        Assert.Throws<BitPdfFormatException>(() => document.XRef.Fetch(new BitPdfRef(6, 0)));
        Assert.IsNull(document.XRef.Fetch(new BitPdfRef(7, 0)));
        Assert.IsNull(document.XRef.Fetch(new BitPdfRef(6, 0)));
        Assert.AreEqual(1, counted.Decodes, "Content failures must not repeatedly inflate the same object stream.");
    }

    [TestMethod]
    public async Task OversizedAttachmentShouldNotHideTheFollowingFileOrRepeatTheScan()
    {
        byte[] bytes = Pdf("/Names << /EmbeddedFiles 10 0 R >>", "", file =>
        {
            file.Obj(10, "<< /Names [(large.bin) 11 0 R (small.txt) 12 0 R] >>");
            file.Obj(11, "<< /Type /Filespec /F (large.bin) /EF << /F 13 0 R >> >>");
            file.Obj(12, "<< /Type /Filespec /F (small.txt) /EF << /F 14 0 R >> >>");
            file.Raw(new string(' ', 8192));
            file.Stream(13, new byte[200000], "/Type /EmbeddedFile");
            file.Stream(14, "small attachment", "/Type /EmbeddedFile");
        });
        Assert.AreEqual(2, BitPdfDocument.Load(bytes).Attachments.Count);
        using var handler = new RangeHandler(bytes);
        Services.AddSingleton(new HttpClient(handler));
        var errors = new List<string>();
        var component = RenderComponent<BitPdfViewer>(p => p.Add(v => v.Source, Definition(128 * 1024)).Add(v => v.OnError, errors.Add));
        component.WaitForAssertion(() => Assert.AreEqual(1, component.Instance.PageCount, string.Join("; ", errors)));
        component.WaitForPages(1);

        await component.InvokeAsync(() => component.Instance.ShowSidebar(BitPdfSidebar.Attachments));
        component.WaitForAssertion(() => Assert.IsTrue(Field<bool>(component.Instance, "_rangeAttachmentsLoaded")));
        Assert.AreEqual(1, component.Instance.Attachments.Count);
        Assert.AreEqual("small.txt", component.Instance.Attachments[0].Name);
        Assert.AreEqual(0, errors.Count, string.Join("; ", errors));
        int requests = handler.Requests.Count;
        await component.InvokeAsync(() => component.Instance.ShowSidebar(BitPdfSidebar.None));
        await component.InvokeAsync(() => component.Instance.ShowSidebar(BitPdfSidebar.Attachments));
        Assert.AreEqual(requests, handler.Requests.Count);
    }

    [TestMethod]
    public async Task PropertiesShouldLoadMetadataWithoutFormsStructureOrAttachments()
    {
        byte[] bytes = Pdf("/Metadata 10 0 R /AcroForm 11 0 R /StructTreeRoot 12 0 R /Names << /EmbeddedFiles 13 0 R >>", "", file =>
        {
            file.Raw(new string(' ', 8192));
            file.Stream(10, "<x:xmpmeta xmlns:x=\"adobe:ns:meta/\"><rdf:RDF xmlns:rdf=\"http://www.w3.org/1999/02/22-rdf-syntax-ns#\"/></x:xmpmeta>", "/Type /Metadata /Subtype /XML");
            file.Raw(new string(' ', 8192));
            file.Obj(11, "<< /Fields [14 0 R] >>");
            file.Obj(14, "<< /FT /Tx /T (Deferred field) /V (value) >>");
            file.Obj(12, "<< /Type /StructTreeRoot /K [15 0 R] >>");
            file.Obj(15, "<< /Type /StructElem /S /P /P 12 0 R /K 0 >>");
            file.Obj(13, "<< /Names [(deferred.txt) 16 0 R] >>");
            file.Obj(16, "<< /F (deferred.txt) /EF << /F 17 0 R >> >>");
            file.Stream(17, "deferred attachment");
        });
        using var handler = new RangeHandler(bytes);
        Services.AddSingleton(new HttpClient(handler));
        var errors = new List<string>();
        var component = RenderComponent<BitPdfViewer>(p => p.Add(v => v.Source, Definition()).Add(v => v.OnError, errors.Add));
        component.WaitForPages(1);

        await component.InvokeAsync(() => (Task)typeof(BitPdfViewer).GetMethod("LoadDetailsForPropertiesAsync", Flags)!.Invoke(component.Instance, null)!);

        Assert.IsNotNull(component.Instance.Metadata);
        Assert.IsTrue(Field<bool>(component.Instance, "_rangeMetadataLoaded"));
        Assert.IsFalse(Field<bool>(component.Instance, "_rangeDetailsLoaded"));
        Assert.IsFalse(Field<bool>(component.Instance, "_rangeAttachmentsLoaded"));
        Assert.AreEqual(0, component.Instance.FormFields.Count);
        Assert.AreEqual(0, component.Instance.StructureTree.Count);
        Assert.AreEqual(0, component.Instance.Attachments.Count);
        Assert.AreEqual(0, errors.Count, string.Join("; ", errors));

        await component.InvokeAsync(() => component.Instance.LoadDocumentDetailsAsync());
        Assert.AreEqual(1, component.Instance.FormFields.Count);
        Assert.AreEqual(1, component.Instance.StructureTree.Count);
        Assert.AreEqual(1, component.Instance.Attachments.Count);
    }

    [TestMethod]
    public async Task StructuralPageFailureAfterPublicationShouldBePermanentAndReportedOnce()
    {
        var file = new BitPdfEngineTestFile();
        file.Obj(1, "<< /Type /Catalog /Pages 2 0 R >>");
        file.Obj(2, "<< /Type /Pages /Count 2 /Kids [3 0 R 8 0 R] >>");
        file.Obj(3, "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 200 200] /Contents 4 0 R >>");
        file.Stream(4, "");
        file.Obj(8, "<< /Type /Pages /Parent 2 0 R /Count 1 /Kids [] >>");
        file.XRefTable("/Size 9 /Root 1 0 R", file.Offsets.Keys.Append(0));
        using var handler = new RangeHandler(file.ToArray());
        Services.AddSingleton(new HttpClient(handler));
        var errors = new List<string>();
        var component = RenderComponent<BitPdfViewer>(p => p.Add(v => v.Source, Definition()).Add(v => v.OnError, errors.Add));
        component.WaitForPages(2);
        var method = typeof(BitPdfViewer).GetMethod("RenderRangePageAsync", Flags)!;

        await component.InvokeAsync(() => (Task<bool>)method.Invoke(component.Instance, new object?[] { 1, false, false })!);
        Assert.IsTrue(component.Instance.FailedPages.Contains(2));
        Assert.AreEqual(1, errors.Count);
        Assert.AreEqual(0, Field<System.Collections.IDictionary>(component.Instance, "_rangeReadFailures").Count);
        await component.InvokeAsync(() => (Task<bool>)method.Invoke(component.Instance, new object?[] { 1, false, false })!);
        Assert.AreEqual(1, errors.Count);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task DependencyDiscoveryShouldIgnoreScalarsAndStopAtItsOptimizationBound(bool manyObjects)
    {
        string values = string.Concat(Enumerable.Repeat(manyObjects ? "<<>> " : "1 ", 66000));
        // Resource dictionaries may carry extension entries. These are valid
        // arrays the renderer does not need, while discovery still visits them.
        byte[] bytes = Pdf("", "", resources: "<< /Font << /F1 5 0 R >> /PrefetchStress [" + values + "] >>");
        using var handler = new RangeHandler(bytes);
        Services.AddSingleton(new HttpClient(handler));
        var errors = new List<string>();
        var component = RenderComponent<BitPdfViewer>(p => p.Add(v => v.Source, Definition(chunkSize: 65536)).Add(v => v.OnError, errors.Add));

        component.WaitForAssertion(() =>
        {
            Assert.AreEqual(1, component.Instance.PageCount, string.Join("; ", errors));
            Assert.AreEqual("false", component.Find(".bit-pdv-surface").GetAttribute("aria-busy"));
        }, TimeSpan.FromSeconds(20));
        Assert.IsNull(component.Instance.GetBytes(), "A valid large resource array must remain in Range mode.");
        Assert.AreEqual(0, errors.Count, string.Join("; ", errors));
        Assert.AreEqual(0, component.Instance.FailedPages.Count);
    }

    [TestMethod]
    public async Task DetailMissesShouldKeepCompleteXrefObjectsWithoutPublishingPartialResults()
    {
        byte[] bytes = Pdf("", "", file =>
        {
            for (int i = 10; i < 90; i++)
            {
                file.Raw(new string(' ', 4096));
                file.Obj(i, i == 89 ? "<< /Value 1 >>" : $"<< /Next {i + 1} 0 R >>");
            }
        });
        using var handler = new RangeHandler(bytes);
        Services.AddSingleton(new HttpClient(handler));
        var component = RenderComponent<BitPdfViewer>(p => p.Add(v => v.Source, Definition()));
        component.WaitForPages(1);
        var firstObjects = new HashSet<object>(ReferenceEqualityComparer.Instance);
        int attempts = 0;
        Func<BitPdfDocument, int> build = document =>
        {
            attempts++;
            var node = (BitPdfDict)document.XRef.Fetch(new BitPdfRef(10, 0))!;
            firstObjects.Add(node);
            while (node.Get("Next") is BitPdfDict next) node = next;
            return (int)(double)node.Get("Value")!;
        };
        var method = typeof(BitPdfViewer).GetMethod("RunRangeWorkAsync", Flags)!.MakeGenericMethod(typeof(int));

        int result = await component.InvokeAsync(() => (Task<int>)method.Invoke(component.Instance,
            new object?[] { component.Instance.Document!, build, CancellationToken.None, null })!);

        Assert.AreEqual(1, result);
        Assert.IsTrue(attempts > 10);
        Assert.AreEqual(1, firstObjects.Count, "A complete dictionary must survive later missing-data restarts.");
        Assert.IsFalse(Field<bool>(component.Instance, "_rangeDetailsLoaded"));
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task WholeFileCopiesShouldUseOneValidatedRequestAndPreserveTheCache(bool download)
    {
        using var handler = new RangeHandler(200000);
        using var http = new HttpClient(handler);
        await using var source = await RangeAccess.Open(http, 32, 8);
        await source.Ensure(80, 8);
        int cacheBytes = source.Int("AccountedBytes");
        int peak = source.Int("PeakBytes");
        int requests = handler.Requests.Count;
        byte[] bytes;
        if (download)
        {
            await using var stream = source.OpenRead();
            using var destination = new MemoryStream();
            await stream.CopyToAsync(destination);
            bytes = destination.ToArray();
        }
        else bytes = await source.ReadAll();

        CollectionAssert.AreEqual(handler.Bytes, bytes);
        Assert.AreEqual(requests + 1, handler.Requests.Count);
        Assert.AreEqual("bytes=0-", handler.Requests[^1]);
        Assert.AreEqual(RangeHandler.Tag, handler.Validators[^1]);
        Assert.AreEqual(cacheBytes, source.Int("AccountedBytes"));
        Assert.AreEqual(peak, source.Int("PeakBytes"));
        Assert.AreEqual(handler.Bytes[80], source.Cursor(80, 1).GetByte());
    }

    [TestMethod]
    [DataRow("changed")]
    [DataRow("changedLength")]
    [DataRow("200")]
    [DataRow("etag")]
    [DataRow("weak")]
    [DataRow("range")]
    [DataRow("compressed")]
    [DataRow("short")]
    [DataRow("extra")]
    public async Task WholeFileCopiesMustValidateHeadersAndTheCompleteBody(string problem)
    {
        using var handler = new RangeHandler(200);
        using var http = new HttpClient(handler);
        await using var source = await RangeAccess.Open(http, 32, 8);
        handler.Problem = problem;

        await Assert.ThrowsAsync<IOException>(() => source.ReadAll());
        Assert.AreEqual(2, handler.Requests.Count);
        if (problem is not ("short" or "extra")) Assert.AreEqual(0, handler.LastBody!.Reads);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task ConcurrentPageBuildsShouldKeepIndependentAttempts(bool background)
    {
        using var handler = new RangeHandler(BitPdfRangeViewerTests.SpacedPages());
        Services.AddSingleton(new HttpClient(handler));
        var errors = new List<string>();
        var component = RenderComponent<BitPdfViewer>(p => p.Add(v => v.Source, Definition())
            .Add(v => v.BackgroundRendering, background).Add(v => v.OnError, errors.Add));
        component.WaitForPages(3);

        var results = await component.InvokeAsync(() => Task.WhenAll(
            component.Instance.RenderPageHtmlAsync(2), component.Instance.RenderPageHtmlAsync(3)));

        StringAssert.Contains(results[0], "Range page 2");
        StringAssert.Contains(results[1], "Range page 3");
        Assert.AreEqual(0, errors.Count, string.Join("; ", errors));
    }

    private static T Field<T>(BitPdfViewer viewer, string name)
        => (T)typeof(BitPdfViewer).GetField(name, Flags)!.GetValue(viewer)!;

    [TestMethod]
    public async Task EncryptedRangeDocumentShouldRenderAfterWrongThenCorrectPasswordWithoutFallback()
    {
        byte[] bytes = TestPdf.Encrypted("pw", contentPadding: 100000);
        string expected = BitPdfDocument.Load(bytes, "pw").Pages[0].ExtractText();
        using var handler = new RangeHandler(bytes);
        Services.AddSingleton(new HttpClient(handler));
        var errors = new List<string>();
        int prompts = 0, progress = 0;
        var component = RenderComponent<BitPdfViewer>(p => p.Add(v => v.Source, Definition())
            .Add(v => v.OnPasswordRequested, () => Task.FromResult<string?>(++prompts == 1 ? "wrong" : "pw"))
            .Add(v => v.OnProgress, (double _) => progress++)
            .Add(v => v.OnError, errors.Add));
        component.WaitForPages(1);

        string actual = await component.InvokeAsync(() => component.Instance.ExtractPageTextAsync(1));
        string html = await component.InvokeAsync(() => component.Instance.RenderPageHtmlAsync(1));

        Assert.AreEqual(expected, actual);
        Assert.IsFalse(string.IsNullOrEmpty(html));
        Assert.AreEqual(2, prompts);
        Assert.AreEqual(0, progress);
        Assert.IsNull(component.Instance.GetBytes());
        Assert.AreEqual(0, errors.Count, string.Join("; ", errors));
        Assert.IsTrue(handler.Requests.All(r => r?.StartsWith("bytes=", StringComparison.Ordinal) is true));
    }

    [TestMethod]
    [DataRow("timeout")]
    [DataRow("cancel")]
    [DataRow("dispose")]
    public async Task CopyBodyWaitShouldHonorTimeoutCancellationAndSourceRetirement(string reason)
    {
        using var handler = new SlowCopyHandler();
        using var http = new HttpClient(handler) { Timeout = TimeSpan.FromMilliseconds(200) };
        await using var source = await RangeAccess.Open(http, 32, 8);
        using var cancellation = new CancellationTokenSource();
        await using var stream = source.OpenRead();
        var work = stream.ReadAsync(new byte[200], cancellation.Token).AsTask();
        await handler.Entered.Task.WaitAsync(TimeSpan.FromSeconds(2));
        if (reason == "cancel") cancellation.Cancel();
        if (reason == "dispose") await source.DisposeAsync();

        if (reason == "timeout") await Assert.ThrowsAsync<IOException>(() => work.WaitAsync(TimeSpan.FromSeconds(2)));
        else await Assert.ThrowsAsync<OperationCanceledException>(() => work.WaitAsync(TimeSpan.FromSeconds(2)));
        Assert.AreEqual(2, handler.Requests, "A copy timeout must surface on its first request.");
        if (reason != "dispose") Assert.AreEqual(0, source.Cursor(0, 1).GetByte());
        await stream.DisposeAsync();
        Assert.IsTrue(handler.Body!.Disposed);
    }

    private static byte[] Pdf(string catalogExtra, string pageExtra, Action<BitPdfEngineTestFile>? write = null, string? resources = null)
    {
        var file = new BitPdfEngineTestFile();
        file.Obj(1, $"<< /Type /Catalog /Pages 2 0 R {catalogExtra} >>");
        file.Obj(2, "<< /Type /Pages /Count 1 /Kids [3 0 R] >>");
        resources ??= "<< /Font << /F1 5 0 R >> >>";
        file.Obj(3, $"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 200 200] /Resources {resources} /Contents 4 0 R {pageExtra} >>");
        file.Stream(4, BitPdfEngineTestFile.ShowText("Maintainer regression"));
        file.Obj(5, "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>");
        file.Obj(6, "<< /Title (Maintainer review) >>");
        write?.Invoke(file);
        file.XRefTable($"/Size {file.Offsets.Keys.Max() + 1} /Root 1 0 R /Info 6 0 R", file.Offsets.Keys.Append(0));
        return file.ToArray();
    }

    private sealed class CountingDecodeStream(BitPdfBaseStream inner) : BitPdfBaseStream
    {
        internal int Decodes;
        public override byte[] GetBytes(int length = 0)
        {
            Decodes++;
            // Simulate the decoder's content-limit failure without allocating
            // a 256 MiB decompression bomb in the unit-test process.
            throw new BitPdfFormatException("Decoded stream too large.");
        }
        public override int GetByte() => inner.GetByte();
        public override byte[] GetByteRange(int begin, int end) => inner.GetByteRange(begin, end);
        public override BitPdfBaseStream MakeSubStream(int start, int length, BitPdfDict? dict = null) => inner.MakeSubStream(start, length, dict);
        public override void MoveStart() => inner.MoveStart();
        public override void Reset()
        {
            Dict = inner.Dict;
            inner.Reset();
        }
    }

    private sealed class SlowCopyHandler : HttpMessageHandler
    {
        internal readonly TaskCompletionSource Entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal int Requests;
        internal SlowBody? Body;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var range = request.Headers.Range!.Ranges.Single();
            int start = (int)range.From!.Value;
            int end = (int)Math.Min(range.To ?? 199, 199);
            HttpContent content = ++Requests == 1 ? new ByteArrayContent(new byte[end - start + 1])
                : new StreamContent(Body = new SlowBody(Entered));
            content.Headers.ContentRange = new ContentRangeHeaderValue(start, end, 200);
            content.Headers.ContentLength = end - start + 1;
            var response = new HttpResponseMessage(HttpStatusCode.PartialContent) { Content = content };
            response.Headers.TryAddWithoutValidation("ETag", RangeHandler.Tag);
            return Task.FromResult(response);
        }
    }

    private sealed class SlowBody(TaskCompletionSource entered) : MemoryStream(new byte[200])
    {
        internal bool Disposed;
        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            entered.TrySetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return 0;
        }
        protected override void Dispose(bool disposing)
        {
            Disposed = true;
            base.Dispose(disposing);
        }
    }
}
