using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.PdfViewer;

public partial class BitPdfRangeSourceTests
{
    [TestMethod]
    public async Task TruncatedCopyBodyShouldRemainATransportFailure()
    {
        using var handler = new RangeHandler(200);
        using var http = new HttpClient(handler);
        await using var source = await RangeAccess.Open(http, 32, 8);
        handler.Problem = "short";

        var error = await Assert.ThrowsAsync<IOException>(() => source.ReadAll());

        Assert.AreEqual("BitPdfByteSourceIOException", error.GetType().Name);
        Assert.AreEqual(handler.Bytes[0], source.Cursor(0, 1).GetByte());
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task CopyContinuationShouldRejectChangedOrUnusableFullResponsesBeforeReading(bool changedVersion)
    {
        using var handler = new RangeHandler(200) { OpenEndedLimit = 7 };
        using var http = new HttpClient(handler);
        await using var source = await RangeAccess.Open(http, 32, 8);
        await using var stream = source.OpenRead();
        var buffer = new byte[200];
        Assert.AreEqual(7, await stream.ReadAsync(buffer, TestContext.CancellationToken));
        handler.Problem = changedVersion ? "200changed" : "200";

        await Assert.ThrowsAsync<IOException>(() => stream.ReadAsync(buffer, TestContext.CancellationToken).AsTask());

        Assert.AreEqual("bytes=7-", handler.Requests[^1]);
        Assert.AreEqual(RangeHandler.Tag, handler.Validators[^1]);
        Assert.AreEqual(0, handler.LastBody!.Reads);
        if (changedVersion) Assert.Throws<IOException>(() => source.Cursor(0, 1).GetByte());
        else Assert.AreEqual(handler.Bytes[0], source.Cursor(0, 1).GetByte());
    }

    [TestMethod]
    public async Task RepeatedReadsOfTheMostRecentBlockShouldKeepItThroughEviction()
    {
        using var handler = new RangeHandler(200);
        using var http = new HttpClient(handler);
        await using var source = await RangeAccess.Open(http, 16, 8);
        await source.Ensure(8, 8, TestContext.CancellationToken);
        for (int i = 0; i < 10; i++) Assert.AreEqual(handler.Bytes[0], source.Cursor(0, 1).GetByte());
        await source.Ensure(16, 8, TestContext.CancellationToken);

        Assert.AreEqual(handler.Bytes[0], source.Cursor(0, 1).GetByte());
        Assert.Throws<BitPdfMissingDataException>(() => source.Cursor(8, 1).GetByte());
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task WholeCopyShouldAcceptAFullResponseForTheSameVersionWithoutRetiringTheSource(bool download)
    {
        using var handler = new RangeHandler(200);
        using var http = new HttpClient(handler);
        await using var source = await RangeAccess.Open(http, 32, 8);
        handler.Problem = "200";

        byte[] bytes;
        if (download)
        {
            await using var stream = source.OpenRead();
            using var destination = new MemoryStream();
            await stream.CopyToAsync(destination, TestContext.CancellationToken);
            bytes = destination.ToArray();
        }
        else bytes = await source.ReadAll();

        Assert.AreSequenceEqual(handler.Bytes, bytes);
        Assert.HasCount(2, handler.Requests);
        Assert.AreEqual(RangeHandler.Tag, handler.Validators[^1]);
        Assert.AreEqual(handler.Bytes[0], source.Cursor(0, 1).GetByte());
        Assert.AreEqual(8, source.Int("AccountedBytes"));
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task WholeCopyShouldResumeShortenedRangesWithoutEvictingCachedBytes(bool download)
    {
        using var handler = new RangeHandler(8200) { OpenEndedLimit = 7 };
        using var http = new HttpClient(handler);
        await using var source = await RangeAccess.Open(http, 32, 8);

        byte[] bytes;
        if (download)
        {
            await using var stream = source.OpenRead();
            using var destination = new MemoryStream();
            await stream.CopyToAsync(destination, TestContext.CancellationToken);
            bytes = destination.ToArray();
        }
        else bytes = await source.ReadAll();

        Assert.AreSequenceEqual(handler.Bytes, bytes);
        Assert.HasCount(1 + (handler.Bytes.Length + 6) / 7, handler.Requests);
        Assert.AreEqual("bytes=7-", handler.Requests[2]);
        Assert.IsTrue(handler.Validators.Skip(1).All(v => v == RangeHandler.Tag));
        Assert.AreEqual(handler.Bytes[0], source.Cursor(0, 1).GetByte());
        Assert.AreEqual(8, source.Int("AccountedBytes"));
    }

    [TestMethod]
    [DataRow("200etag", false)]
    [DataRow("200weak", false)]
    [DataRow("200length", false)]
    [DataRow("200compressed", false)]
    [DataRow("200changed", true)]
    public async Task FullCopyResponseShouldRejectUnverifiableBytesBeforeReadingItsBody(string problem, bool versionChanged)
    {
        using var handler = new RangeHandler(200);
        using var http = new HttpClient(handler);
        await using var source = await RangeAccess.Open(http, 32, 8);
        handler.Problem = problem;

        await Assert.ThrowsAsync<IOException>(() => source.ReadAll());

        Assert.AreEqual(0, handler.LastBody!.Reads);
        if (versionChanged) Assert.Throws<IOException>(() => source.Cursor(0, 1).GetByte());
        else Assert.AreEqual(handler.Bytes[0], source.Cursor(0, 1).GetByte());
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task WholeFileCopiesShouldUseOneValidatedRequestAndPreserveTheCache(bool download)
    {
        using var handler = new RangeHandler(200000);
        using var http = new HttpClient(handler);
        await using var source = await RangeAccess.Open(http, 32, 8);
        await source.Ensure(80, 8, TestContext.CancellationToken);
        int cacheBytes = source.Int("AccountedBytes");
        int peak = source.Int("PeakBytes");
        int requests = handler.Requests.Count;
        byte[] bytes;
        if (download)
        {
            await using var stream = source.OpenRead();
            using var destination = new MemoryStream();
            await stream.CopyToAsync(destination, TestContext.CancellationToken);
            bytes = destination.ToArray();
        }
        else bytes = await source.ReadAll();

        Assert.AreSequenceEqual(handler.Bytes, bytes);
        Assert.HasCount(requests + 1, handler.Requests);
        Assert.AreEqual("bytes=0-", handler.Requests[^1]);
        Assert.AreEqual(RangeHandler.Tag, handler.Validators[^1]);
        Assert.AreEqual(cacheBytes, source.Int("AccountedBytes"));
        Assert.AreEqual(peak, source.Int("PeakBytes"));
        Assert.AreEqual(handler.Bytes[80], source.Cursor(80, 1).GetByte());
    }

    [TestMethod]
    [DataRow("changed")]
    [DataRow("changedLength")]
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
        Assert.HasCount(2, handler.Requests);
        if (problem is not ("short" or "extra")) Assert.AreEqual(0, handler.LastBody!.Reads);
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
        await handler.Entered.Task.WaitAsync(TimeSpan.FromSeconds(2), TestContext.CancellationToken);
        if (reason == "cancel") cancellation.Cancel();
        if (reason == "dispose") await source.DisposeAsync();

        if (reason == "timeout") await Assert.ThrowsAsync<IOException>(() => work.WaitAsync(TimeSpan.FromSeconds(2), TestContext.CancellationToken));
        else await Assert.ThrowsAsync<OperationCanceledException>(() => work.WaitAsync(TimeSpan.FromSeconds(2), TestContext.CancellationToken));
        Assert.AreEqual(2, handler.Requests, "A copy timeout must surface on its first request.");
        if (reason != "dispose") Assert.AreEqual(0, source.Cursor(0, 1).GetByte());
        await stream.DisposeAsync();
        Assert.IsTrue(handler.Body!.Disposed);
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

    public global::Microsoft.VisualStudio.TestTools.UnitTesting.TestContext TestContext { get; set; }
}
