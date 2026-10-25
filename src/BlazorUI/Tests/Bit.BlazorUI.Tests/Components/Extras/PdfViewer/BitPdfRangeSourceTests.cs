using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.PdfViewer;

[TestClass]
public class BitPdfRangeSourceTests
{
    [TestMethod]
    public void RangeOptionsShouldHaveStableDefaultsAndSurviveSourceCloning()
    {
        var options = new BitPdfRangeLoadingOptions();
        Assert.AreEqual(16 * 1024 * 1024, options.CacheSize);
        Assert.AreEqual(64 * 1024, options.ChunkSize);
        Assert.IsNull(options.MaxFallbackSize);
        var source = BitPdfSource.FromUrl("https://pdf.test/document").WithRangeLoading(options)
            .WithFileName("report.pdf").WithPassword("secret").WithHeaders(new Dictionary<string, string> { ["Authorization"] = "Bearer test" });
        Assert.AreSame(options, typeof(BitPdfSource).GetProperty("RangeLoading", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(source));
        Assert.IsNull(source.Bytes);
        Assert.AreEqual("report.pdf", source.FileName);
    }

    [TestMethod]
    [DataRow(0, 4, null)]
    [DataRow(-1, 4, null)]
    [DataRow(4, 0, null)]
    [DataRow(4, -1, null)]
    [DataRow(3, 4, null)]
    [DataRow(4, 4, -1L)]
    public void RangeOptionsShouldBeValidatedAtSourceConstruction(int cache, int chunk, long? fallback)
        => Assert.Throws<ArgumentOutOfRangeException>(() => BitPdfSource.FromBytes([1]).WithRangeLoading(
            new() { CacheSize = cache, ChunkSize = chunk, MaxFallbackSize = fallback }));

    [TestMethod]
    public async Task ProbeShouldFetchOnlyTheFirstChunkAndMissingReadsMustNotSendRequests()
    {
        using var handler = new RangeHandler(200000);
        using var http = new HttpClient(handler);
        await using var source = await RangeAccess.Open(http, 131072, 65536);
        Assert.AreEqual(200000, source.Int("Length"));
        Assert.AreEqual(65536L, source.Long("FetchedBytes"));
        Assert.AreEqual(1, handler.Requests.Count);
        Assert.IsNull(handler.Validators[0]);
        Assert.AreEqual("bytes=0-65535", handler.Requests[0]);
        Assert.AreEqual(handler.Bytes[200], source.Cursor(200, 1).GetByte());
        var missing = Assert.Throws<BitPdfMissingDataException>(() => source.Cursor(150000, 1).GetByte());
        Assert.AreEqual(150000, missing.Offset);
        Assert.AreEqual(1, missing.Length);
        Assert.AreEqual(1, handler.Requests.Count);
        await source.Ensure(150000, 1);
        Assert.AreEqual("bytes=131072-196607", handler.Requests[1]);
        Assert.AreEqual(RangeHandler.Tag, handler.Validators[1]);
        Assert.AreEqual(handler.Bytes[150000], source.Cursor(150000, 1).GetByte());
        Assert.IsTrue(source.Int("PeakBytes") <= 131072);
    }

    [TestMethod]
    public async Task ReadsShouldCarryAuthenticationAndTheExactStrongValidator()
    {
        using var handler = new RangeHandler(200);
        using var http = new HttpClient(handler);
        var definition = BitPdfSource.FromUrl("https://pdf.test/document").WithHeaders(new Dictionary<string, string> { ["Authorization"] = "Bearer test" })
            .WithRangeLoading(new() { CacheSize = 32, ChunkSize = 8 });
        await using var source = await RangeAccess.Open(http, definition);
        await source.Ensure(80, 8);
        Assert.AreEqual(2, handler.Authorization.Count);
        Assert.IsTrue(handler.Authorization.All(v => v == "Bearer test"));
        Assert.AreEqual(RangeHandler.Tag, handler.Validators[1]);
    }

    [TestMethod]
    [DataRow("etag")]
    [DataRow("weak")]
    [DataRow("range")]
    [DataRow("total")]
    [DataRow("compressed")]
    [DataRow("short")]
    [DataRow("extra")]
    [DataRow("416")]
    public async Task ProbeMustRejectUnusableDeliveryBeforePublishingBytes(string problem)
    {
        using var handler = new RangeHandler(200) { Problem = problem };
        using var http = new HttpClient(handler);
        await Assert.ThrowsAsync<IOException>(async () => { await using var source = await RangeAccess.Open(http, 32, 8); });
        Assert.AreEqual(1, handler.Requests.Count);
    }

    [TestMethod]
    [DataRow("changed")]
    [DataRow("changedLength")]
    [DataRow("200")]
    public async Task LaterVersionChangesShouldFailWithoutConsumingTheReplacementBody(string problem)
    {
        using var handler = new RangeHandler(200);
        using var http = new HttpClient(handler);
        await using var source = await RangeAccess.Open(http, 32, 8);
        handler.Problem = problem;
        await Assert.ThrowsAsync<IOException>(() => source.Ensure(80, 8));
        Assert.AreEqual(0, handler.LastBody!.Reads);
        Assert.AreEqual(RangeHandler.Tag, handler.Validators[1]);
        Assert.Throws<IOException>(() => source.Cursor(0, 1).GetByte());
    }

    [TestMethod]
    public async Task PinnedWorkingSetMustFailAtTheBudgetWithoutEvictingItsOwnBytesOrLooping()
    {
        using var handler = new RangeHandler(200);
        using var http = new HttpClient(handler);
        await using var source = await RangeAccess.Open(http, 12, 4);
        using var operation = source.Operation();
        await source.Ensure(4, 4);
        await source.Ensure(8, 4);
        await source.Ensure(12, 4);
        await Assert.ThrowsAsync<IOException>(() => source.Ensure(16, 4));
        Assert.AreEqual(4, handler.Requests.Count);
        Assert.AreEqual(12, source.Int("PeakBytes"));
        Assert.AreEqual(handler.Bytes[4], source.Cursor(4, 1).GetByte());
    }

    [TestMethod]
    public async Task OversizedStreamMustFailBeforeARequestAndCopiesMustBeCallerOwned()
    {
        using var handler = new RangeHandler(200);
        using var http = new HttpClient(handler);
        await using var source = await RangeAccess.Open(http, 32, 8);
        await Assert.ThrowsAsync<IOException>(() => source.Ensure(40, 33));
        Assert.AreEqual(1, handler.Requests.Count);
        var bytes = await source.ReadAll();
        CollectionAssert.AreEqual(handler.Bytes, bytes);
        bytes[0] ^= 255;
        await source.Ensure(0, 1);
        Assert.AreEqual(handler.Bytes[0], source.Cursor(0, 1).GetByte());
        Assert.IsTrue(source.Int("PeakBytes") <= 32);
    }

    [TestMethod]
    public async Task OverlappingAcquisitionsShouldShareOnePendingRequest()
    {
        using var handler = new RangeHandler(200);
        using var http = new HttpClient(handler);
        await using var source = await RangeAccess.Open(http, 32, 8);
        handler.Pause = new(TaskCreationOptions.RunContinuationsAsynchronously);
        var first = source.Ensure(16, 8);
        var second = source.Ensure(20, 4);
        Assert.AreEqual(2, handler.Requests.Count);
        handler.Pause.SetResult();
        await Task.WhenAll(first, second);
        Assert.AreEqual(2, handler.Requests.Count);
        Assert.IsTrue(source.Int("PeakBytes") <= 32);
    }

    [TestMethod]
    public async Task RequestTimeoutShouldPropagateWithoutRetryingAndReleaseItsReservation()
    {
        using var handler = new RangeHandler(200);
        using var http = new HttpClient(handler) { Timeout = TimeSpan.FromMilliseconds(100) };
        await using var source = await RangeAccess.Open(http, 32, 8);
        handler.Pause = new(TaskCreationOptions.RunContinuationsAsynchronously);
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        var error = await Assert.ThrowsAsync<IOException>(() => source.Ensure(80, 8, cancellation.Token));
        Assert.IsFalse(cancellation.IsCancellationRequested, "The caller did not cancel the timed-out request.");
        Assert.IsInstanceOfType<TaskCanceledException>(error.InnerException);
        Assert.IsInstanceOfType<TimeoutException>(error.InnerException!.InnerException);
        Assert.AreEqual(2, handler.Requests.Count, "An HTTP timeout must not repeat the range request.");
        Assert.AreEqual(8, source.Int("AccountedBytes"));
        Assert.Throws<BitPdfMissingDataException>(() => source.Cursor(80, 1).GetByte());
        handler.Pause.SetResult();
        await source.Ensure(80, 8);
        Assert.AreEqual(handler.Bytes[80], source.Cursor(80, 1).GetByte());
    }

    [TestMethod]
    public async Task CancelledSharedAcquisitionShouldAllowAnActiveWaiterToRetry()
    {
        using var handler = new RangeHandler(200);
        using var http = new HttpClient(handler);
        await using var source = await RangeAccess.Open(http, 32, 8);
        handler.Pause = new(TaskCreationOptions.RunContinuationsAsynchronously);
        using var cancellation = new CancellationTokenSource();
        var first = source.Ensure(80, 8, cancellation.Token);
        var second = source.Ensure(80, 8);
        Assert.AreEqual(2, handler.Requests.Count);
        cancellation.Cancel();
        await Assert.ThrowsAsync<OperationCanceledException>(() => first);
        handler.Pause.SetResult();
        await second.WaitAsync(TimeSpan.FromSeconds(3));
        Assert.AreEqual(3, handler.Requests.Count);
        Assert.AreEqual(handler.Bytes[80], source.Cursor(80, 1).GetByte());
        Assert.AreEqual(16, source.Int("AccountedBytes"));
    }

    [TestMethod]
    public async Task FailedResponsesShouldReleaseTheirReservationsAndAllowRetry()
    {
        using var handler = new RangeHandler(200);
        using var http = new HttpClient(handler);
        await using var source = await RangeAccess.Open(http, 32, 8);
        handler.Problem = "short";
        await Assert.ThrowsAsync<IOException>(() => source.Ensure(80, 8));
        Assert.AreEqual(8, source.Int("AccountedBytes"));
        Assert.Throws<BitPdfMissingDataException>(() => source.Cursor(80, 1).GetByte());
        Assert.AreEqual(handler.Bytes[0], source.Cursor(0, 1).GetByte());
        handler.Problem = null;
        await source.Ensure(80, 8);
        Assert.AreEqual(handler.Bytes[80], source.Cursor(80, 1).GetByte());
    }

    [TestMethod]
    public async Task NestedAcquisitionsShouldShareTheirRequestLimitWithoutPinningCompletedChildren()
    {
        using var handler = new RangeHandler(200);
        using var http = new HttpClient(handler);
        await using var source = await RangeAccess.Open(http, 32, 8);
        using var budget = source.Operation();
        var error = await Assert.ThrowsAsync<IOException>(async () =>
        {
            for (int i = 1; i < 1030; i++)
            {
                using var child = source.Operation();
                await source.Ensure(i % 25 * 8, 8);
            }
        });
        StringAssert.Contains(error.Message, "range-request limit");
        Assert.IsTrue(handler.Requests.Count <= 1025);
        Assert.IsTrue(source.Int("PeakBytes") <= 32);
    }

    internal sealed class RangeHandler(int length) : HttpMessageHandler
    {
        internal RangeHandler(byte[] bytes) : this(bytes.Length) { Bytes = bytes; }
        internal const string Tag = "\"Case-Sensitive_opaque.v1\"";
        internal readonly byte[] Bytes = Enumerable.Range(0, length).Select(i => (byte)(i % 251)).ToArray();
        internal readonly List<string?> Requests = [];
        internal readonly List<string?> Validators = [];
        internal readonly List<string?> Authorization = [];
        internal string? Problem;
        internal CountingBody? LastBody;
        internal TaskCompletionSource? Pause;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request.Headers.Range?.ToString());
            Validators.Add(request.Headers.TryGetValues("If-Range", out var values) ? values.Single() : null);
            Authorization.Add(request.Headers.Authorization?.ToString());
            if (Pause is { } pause) await pause.Task.WaitAsync(cancellationToken);
            var requested = request.Headers.Range!.Ranges.Single();
            int from = (int)requested.From!.Value, to = (int)Math.Min(Bytes.Length - 1, requested.To!.Value);
            if (Problem == "200") { from = 0; to = Bytes.Length - 1; }
            byte[] data = Bytes[from..(to + 1)];
            if (Problem == "short") data = data[..^1];
            if (Problem == "extra") data = [..data, 42];
            LastBody = new CountingBody(data);
            var response = new HttpResponseMessage(Problem == "200" ? HttpStatusCode.OK : Problem == "416" ? HttpStatusCode.RequestedRangeNotSatisfiable : HttpStatusCode.PartialContent)
            { Content = new StreamContent(LastBody) };
            response.Content.Headers.ContentRange = Problem == "total"
                ? new ContentRangeHeaderValue(from, to)
                : new ContentRangeHeaderValue(Problem == "range" ? from + 1 : from, to, Problem == "changedLength" ? Bytes.Length + 1 : Bytes.Length);
            response.Content.Headers.ContentLength = to - from + 1;
            if (Problem == "compressed") response.Content.Headers.ContentEncoding.Add("gzip");
            if (Problem != "etag") response.Headers.TryAddWithoutValidation("ETag", Problem == "weak" ? "W/" + Tag : Problem == "changed" ? "\"changed\"" : Tag);
            return response;
        }
    }

    internal sealed class CountingBody(byte[] bytes) : MemoryStream(bytes, writable: false)
    {
        internal int Reads;
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        { Reads++; return base.ReadAsync(buffer, cancellationToken); }
    }

    internal sealed class RangeAccess(object instance) : IAsyncDisposable
    {
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        private static readonly Type SourceType = typeof(BitPdfSource).Assembly.GetType("Bit.BlazorUI.BitPdfHttpRangeByteSource", true)!;
        private object? Invoke(string name, params object?[] arguments)
        {
            try { return SourceType.GetMethod(name, Flags)!.Invoke(instance, arguments); }
            catch (TargetInvocationException ex) when (ex.InnerException is not null) { ExceptionDispatchInfo.Capture(ex.InnerException).Throw(); throw; }
        }
        internal static Task<RangeAccess> Open(HttpClient http, int cache, int chunk)
            => Open(http, BitPdfSource.FromUrl("https://pdf.test/document").WithRangeLoading(new() { CacheSize = cache, ChunkSize = chunk }));
        internal static async Task<RangeAccess> Open(HttpClient http, BitPdfSource definition)
        {
            var task = (Task)SourceType.GetMethod("OpenAsync", Flags)!.Invoke(null, [http, definition, CancellationToken.None])!;
            try { await task; }
            catch (Exception ex)
            {
                (ex.GetType().GetProperty("Response", Flags)?.GetValue(ex) as HttpResponseMessage)?.Dispose();
                throw;
            }
            return new(task.GetType().GetProperty("Result")!.GetValue(task)!);
        }
        internal async Task<BitPdfDocument> Load(string? password = null)
        {
            using var operation = Operation();
            for (int attempt = 0; attempt < 512; attempt++)
            {
                try
                {
                    return (BitPdfDocument)typeof(BitPdfDocument).GetMethods(Flags).Single(m => m.Name == "Load" && m.GetParameters().Length == 2 && m.GetParameters()[0].ParameterType.IsInterface).Invoke(null, [instance, password])!;
                }
                catch (TargetInvocationException ex) when (ex.InnerException is BitPdfMissingDataException missing) { await Ensure(missing.Offset, missing.Length); }
            }
            throw new InvalidOperationException("PDF load did not make progress.");
        }
        internal async Task<T> Run<T>(BitPdfDocument document, Func<BitPdfDocument, T> build)
        {
            using var operation = Operation();
            for (int attempt = 0; attempt < 512; attempt++)
            {
                BitPdfMissingDataException? missing = null;
                using (var frame = (IDisposable)typeof(BitPdfDocument).GetMethod("BeginPartialAttempt", Flags)!.Invoke(document, null)!)
                {
                    try
                    {
                        var result = build(document);
                        frame.GetType().GetMethod("Commit", Flags)!.Invoke(frame, null);
                        return result;
                    }
                    catch (BitPdfMissingDataException ex) { missing = ex; }
                    finally { typeof(BitPdfDocument).GetMethod("ReleaseByteWindows", Flags)!.Invoke(document, null); }
                }
                await Ensure(missing!.Offset, missing.Length);
            }
            throw new InvalidOperationException("PDF operation did not make progress.");
        }
        internal Task Ensure(int offset, int length, CancellationToken token = default) => (Task)Invoke("EnsureAsync", offset, length, token)!;
        internal Task<byte[]> ReadAll() => (Task<byte[]>)Invoke("ReadAllAsync", CancellationToken.None)!;
        internal IDisposable Operation() => (IDisposable)Invoke("BeginOperation")!;
        internal BitPdfBaseStream Cursor(int start, int length) => (BitPdfBaseStream)Invoke("CreateStream", start, (int?)length)!;
        internal int Int(string name) => (int)SourceType.GetProperty(name, Flags)!.GetValue(instance)!;
        internal long Long(string name) => (long)SourceType.GetProperty(name, Flags)!.GetValue(instance)!;
        public ValueTask DisposeAsync() => (ValueTask)Invoke("DisposeAsync")!;
    }
}
