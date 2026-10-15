using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.PdfViewer;

[TestClass]
public class BitPdfSeekableStreamTests
{
    [TestMethod]
    [DataRow("hello")]
    [DataRow("encrypted")]
    [DataRow("attachments")]
    [DataRow("layers")]
    [DataRow("rich")]
    [DataRow("resources")]
    [DataRow("damaged")]
    [DataRow("prefixed")]
    [DataRow("xref-stream")]
    [DataRow("incremental")]
    [DataRow("article.pdf")]
    [DataRow("sample-pdf.pdf")]
    [DataRow("hello-world.pdf")]
    public async Task SeekableSourceShouldMatchMemoryForParsingTextAndBothRenderers(string fixture)
    {
        byte[] bytes = BitPdfGenericSourceTests.Fixture(fixture);
        string? password = fixture == "encrypted" ? "hunter2" : null;
        var expected = BitPdfDocument.Load(bytes, password);
        var input = new PdfTrackingStream(bytes, shortRead: 1031);
        await using var source = new PdfSeekableSourceAccess(input);
        var actual = source.Load(password);

        Assert.AreEqual(expected.Version, actual.Version);
        Assert.AreEqual(expected.PageCount, actual.PageCount);
        Assert.AreEqual(expected.Metadata.Title, actual.Metadata.Title);
        CollectionAssert.AreEqual(expected.PageLabels.ToArray(), actual.PageLabels.ToArray());
        CollectionAssert.AreEqual(expected.Warnings.ToArray(), actual.Warnings.ToArray());
        Assert.AreEqual(expected.Attachments.Count, actual.Attachments.Count);
        for (int i = 0; i < actual.Attachments.Count; i++)
            CollectionAssert.AreEqual(expected.Attachments[i].Content, actual.Attachments[i].Content);
        for (int i = 0; i < actual.PageCount; i++)
        {
            CollectionAssert.AreEqual(expected.Pages[i].GetContentBytes(), actual.Pages[i].GetContentBytes());
            Assert.AreEqual(expected.Pages[i].ExtractText(), actual.Pages[i].ExtractText());
            Assert.AreEqual(new BitPdfHtmlRenderer(expected.Pages[i], expected.XRef).Render(),
                new BitPdfHtmlRenderer(actual.Pages[i], actual.XRef).Render());
            Assert.AreEqual(JsonSerializer.Serialize(new BitPdfCanvasRenderer(expected.Pages[i], expected.XRef).Render()),
                JsonSerializer.Serialize(new BitPdfCanvasRenderer(actual.Pages[i], actual.XRef).Render()));
        }
        Assert.IsTrue(input.SeekCount > 0);
        Assert.IsTrue(source.PeakBytes <= 16 * 1024 * 1024);
        Assert.IsFalse(source.TryGetContiguous());
    }

    [TestMethod]
    public async Task SeekableSourceShouldCaptureOriginAndSerializeShortReadsAcrossIndependentCursors()
    {
        byte[] bytes = Enumerable.Range(0, 4096).Select(i => (byte)i).ToArray();
        var input = new PdfTrackingStream(new byte[37].Concat(bytes).ToArray(), shortRead: 3) { Position = 37 };
        await using var source = new PdfSeekableSourceAccess(input, cacheSize: 256, blockSize: 64);
        var tasks = Enumerable.Range(0, 8).Select(index => Task.Run(() =>
        {
            var cursor = source.CreateStream(index * 101, 100);
            Assert.IsFalse(cursor is BitPdfStream);
            CollectionAssert.AreEqual(bytes.AsSpan(index * 101, 100).ToArray(), cursor.GetBytes());
            cursor.Reset();
            Assert.AreEqual(bytes[index * 101], cursor.GetByte());
            Assert.AreEqual(index * 101 + 1, cursor.Pos);
        }));
        await Task.WhenAll(tasks);
        CollectionAssert.AreEqual(bytes, await source.ReadAllAsync());
        Assert.AreEqual(1, input.MaximumConcurrentReads);
        Assert.IsFalse(source.TryGetContiguous());
    }

    [TestMethod]
    public async Task CacheShouldCountPinnedWindowsCopiesAndEvictOnlyAfterRelease()
    {
        byte[] bytes = Enumerable.Range(0, 40).Select(i => (byte)i).ToArray();
        var input = new PdfTrackingStream(bytes);
        var source = new PdfSeekableSourceAccess(input, cacheSize: 12, blockSize: 4);
        var first = source.Window(0, 4);
        var second = source.Window(4, 4);
        using (var copy = source.Window(10, 4))
        {
            Assert.AreEqual(12, source.AccountedBytes);
            CollectionAssert.AreEqual(new byte[] { 10, 11, 12, 13 }, copy.Bytes);
            Assert.Throws<IOException>(() => source.Window(16, 4));
            CollectionAssert.AreEqual(new byte[] { 0, 1, 2, 3 }, first.Bytes);
        }
        second.Dispose();
        using (var third = source.Window(16, 4))
            CollectionAssert.AreEqual(new byte[] { 16, 17, 18, 19 }, third.Bytes);
        using (var fourth = source.Window(24, 4))
            CollectionAssert.AreEqual(new byte[] { 24, 25, 26, 27 }, fourth.Bytes);
        Assert.AreEqual(12, source.PeakBytes);
        await source.DisposeAsync();
        Assert.AreEqual(4, source.AccountedBytes); // caller still holds the first block
        CollectionAssert.AreEqual(new byte[] { 0, 1, 2, 3 }, first.Bytes);
        first.Dispose();
        first.Dispose();
        Assert.AreEqual(0, source.AccountedBytes);
        Assert.AreEqual(1, input.DisposeCount);
    }

    [TestMethod]
    public async Task FailedOrCancelledReadsShouldReleaseReservationsAndNeverRecoverByScanning()
    {
        var input = new PdfTrackingStream(TestPdf.HelloWorld());
        using var cancellation = new CancellationTokenSource();
        await using var source = new PdfSeekableSourceAccess(input, cancellation.Token);
        input.FailRead = true;
        Assert.Throws<IOException>(() => source.Load());
        Assert.AreEqual(0, source.AccountedBytes);
        Assert.AreEqual(1, input.SeekCount); // header failed; no recovery scan
        input.FailRead = false;
        var document = source.Load();
        _ = document.Pages;
        input.FailRead = true;
        Assert.Throws<IOException>(() => new BitPdfHtmlRenderer(document.Pages[0], document.XRef).Render());
        Assert.Throws<IOException>(() => document.Pages[0].ExtractText());
        input.FailRead = false;
        cancellation.Cancel();
        Assert.Throws<OperationCanceledException>(() => source.CreateStream().GetByte());
        Assert.AreEqual(0, document.Warnings.Count);
    }

    [TestMethod]
    public async Task CacheShouldAccountForAnInFlightFillAndAQueuedCrossBlockCopy()
    {
        using var release = new ManualResetEventSlim();
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var input = new PdfTrackingStream(new byte[32]);
        int reads = 0;
        input.OnRead = () =>
        {
            if (Interlocked.Increment(ref reads) != 1) return;
            started.TrySetResult();
            if (!release.Wait(TimeSpan.FromSeconds(5))) throw new IOException("Test read timed out.");
        };
        await using var source = new PdfSeekableSourceAccess(input, cacheSize: 8, blockSize: 4);
        var fill = Task.Run(() => source.Window(0, 4));
        await started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.AreEqual(4, source.AccountedBytes);
        var copy = Task.Run(() => source.Window(6, 4));
        try
        {
            Assert.IsTrue(SpinWait.SpinUntil(() => source.AccountedBytes == 8, TimeSpan.FromSeconds(5)));
            Assert.Throws<IOException>(() => source.Window(14, 4));
        }
        finally { release.Set(); }
        using var first = await fill;
        using var second = await copy;
        Assert.AreEqual(8, source.PeakBytes);
        Assert.AreEqual(1, input.MaximumConcurrentReads);
    }

    [TestMethod]
    public async Task ChangedLengthAndPrematureEndShouldEscapeMalformedContentRecovery()
    {
        var input = new PdfTrackingStream(TestPdf.HelloWorld());
        await using var source = new PdfSeekableSourceAccess(input);
        input.LengthOverride = input.Length + 1;
        Assert.Throws<IOException>(() => source.Load());
        Assert.AreEqual(0, input.ReadCount);
        Assert.AreEqual(0, source.AccountedBytes);
        input.LengthOverride = null;
        input.ReturnEndOfStream = true;
        Assert.Throws<IOException>(() => source.Load());
        Assert.AreEqual(0, source.AccountedBytes);
    }

    [TestMethod]
    [DataRow("hello")]
    [DataRow("encrypted")]
    public async Task RetainedModelsAndCachedRawStreamsShouldThrowAfterRelease(string fixture)
    {
        var input = new PdfTrackingStream(BitPdfGenericSourceTests.Fixture(fixture));
        var source = new PdfSeekableSourceAccess(input);
        var document = source.Load(fixture == "encrypted" ? "hunter2" : null);
        var page = document.Pages[0];
        var xref = document.XRef;
        var raw = (BitPdfBaseStream)page.Dict.Get("Contents")!;
        _ = document.Metadata;
        _ = raw.GetByte();
        int reads = input.ReadCount;
        await source.DisposeAsync();
        await source.DisposeAsync();
        Assert.ThrowsExactly<ObjectDisposedException>(() => _ = document.Pages);
        Assert.ThrowsExactly<ObjectDisposedException>(() => _ = document.Metadata);
        Assert.ThrowsExactly<ObjectDisposedException>(() => xref.Fetch(new BitPdfRef(1, 0)));
        Assert.ThrowsExactly<ObjectDisposedException>(() => xref.Parse());
        Assert.ThrowsExactly<ObjectDisposedException>(() => page.GetContentBytes());
        Assert.ThrowsExactly<ObjectDisposedException>(() => page.ExtractText());
        Assert.ThrowsExactly<ObjectDisposedException>(() => raw.GetByte());
        Assert.ThrowsExactly<ObjectDisposedException>(() => BitPdfStreamDecoder.Decode(raw));
        Assert.AreEqual(reads, input.ReadCount);
        Assert.AreEqual(0, xref.Warnings.Count);
        Assert.AreEqual(1, input.DisposeCount);
    }

    [TestMethod]
    [DataRow(false, true, 20L, 0L)]
    [DataRow(true, false, 20L, 0L)]
    [DataRow(true, true, 20L, -1L)]
    [DataRow(true, true, 20L, 21L)]
    [DataRow(true, true, 2147483648L, 0L)]
    public void SeekableSourceShouldRejectUnsupportedCapabilitiesAndRanges(bool readable, bool seekable, long length, long position)
    {
        using var input = new PdfTrackingStream([])
        {
            Readable = readable, Seekable = seekable, LengthOverride = length, PositionOverride = position,
        };
        Assert.Throws<ArgumentException>(() => new PdfSeekableSourceAccess(input));
    }

    [TestMethod]
    public async Task SeekableSourceShouldAllowLargePhysicalOriginAndCheckDocumentRelativeBounds()
    {
        var input = new PdfTrackingStream([]) { LengthOverride = (long)int.MaxValue + 100, PositionOverride = (long)int.MaxValue + 100 };
        await using var source = new PdfSeekableSourceAccess(input);
        Assert.AreEqual(-1, source.CreateStream().GetByte());
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => source.CreateStream(-1));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => source.CreateStream(0, 1));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => source.Window(0, 1));
    }
}

// Reflection exercises the actual signed Extras backend without adding a public
// Stream load API or substituting a second implementation in the tests.
internal sealed class PdfSeekableSourceAccess : IAsyncDisposable
{
    private static readonly Type SourceType = typeof(BitPdfDocument).Assembly.GetType("Bit.BlazorUI.BitPdfSeekableStreamByteSource", true)!;
    private readonly object _source;
    private readonly List<BitPdfDocument> _documents = [];

    public PdfSeekableSourceAccess(Stream input, CancellationToken cancellationToken = default,
        int cacheSize = 16 * 1024 * 1024, int blockSize = 64 * 1024)
        => _source = Invoke(() => Activator.CreateInstance(SourceType, [input, cancellationToken, cacheSize, blockSize])!);

    public int AccountedBytes => (int)SourceType.GetProperty("AccountedBytes", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(_source)!;
    public int PeakBytes => (int)SourceType.GetProperty("PeakBytes", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(_source)!;
    public BitPdfBaseStream CreateStream(int start = 0, int? length = null)
        => (BitPdfBaseStream)Call("CreateStream", start, length)!;
    public PdfSourceWindow Window(int offset, int count) => new(Call("GetMemory", offset, count)!);
    public bool TryGetContiguous() => (bool)Call("TryGetContiguous", new object?[] { null })!;
    public Task<byte[]> ReadAllAsync(CancellationToken cancellationToken = default)
        => (Task<byte[]>)Call("ReadAllAsync", cancellationToken)!;
    public BitPdfDocument Load(string? password = null)
    {
        var method = typeof(BitPdfDocument).GetMethods(BindingFlags.Static | BindingFlags.NonPublic).Single(m => m.Name == "Load");
        var document = (BitPdfDocument)Invoke(() => method.Invoke(null, [_source, password])!);
        _documents.Add(document);
        return document;
    }
    private object? Call(string name, params object?[] args)
        => Invoke(() => SourceType.GetMethod(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(_source, args));
    private static T Invoke<T>(Func<T> action)
    {
        try { return action(); }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
            throw;
        }
    }
    public ValueTask DisposeAsync()
    {
        foreach (var document in _documents)
            typeof(BitPdfDocument).GetMethod("ReleaseByteWindows", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(document, null);
        return ((IAsyncDisposable)_source).DisposeAsync();
    }
}

internal sealed class PdfSourceWindow(object window) : IDisposable
{
    public byte[] Bytes => ((ReadOnlyMemory<byte>)window.GetType().GetProperty("Memory")!.GetValue(window)!).ToArray();
    public void Dispose() => ((IDisposable)window).Dispose();
}

internal class PdfTrackingStream(byte[] bytes, int shortRead = int.MaxValue) : Stream
{
    private readonly MemoryStream _inner = new(bytes, writable: false);
    private int _active;
    public int ReadCount;
    public int SeekCount;
    public int DisposeCount;
    public int MaximumConcurrentReads;
    public bool FailRead;
    public bool ReturnEndOfStream;
    public bool Readable = true;
    public bool Seekable = true;
    public long? LengthOverride;
    public long? PositionOverride;
    public Action? OnRead;
    public override bool CanRead => Readable && _inner.CanRead;
    public override bool CanSeek => Seekable && _inner.CanSeek;
    public override bool CanWrite => false;
    public override long Length => LengthOverride ?? _inner.Length;
    public override long Position { get => PositionOverride ?? _inner.Position; set => _inner.Position = value; }
    public override long Seek(long offset, SeekOrigin loc) { Interlocked.Increment(ref SeekCount); return _inner.Seek(offset, loc); }
    public override int Read(Span<byte> buffer)
    {
        int active = Interlocked.Increment(ref _active);
        MaximumConcurrentReads = Math.Max(MaximumConcurrentReads, active);
        try
        {
            OnRead?.Invoke();
            Interlocked.Increment(ref ReadCount);
            if (FailRead) throw new IOException("Simulated provider failure.");
            if (ReturnEndOfStream) return 0;
            return _inner.Read(buffer[..Math.Min(shortRead, buffer.Length)]);
        }
        finally { Interlocked.Decrement(ref _active); }
    }
    public override int Read(byte[] buffer, int offset, int count) => Read(buffer.AsSpan(offset, count));
    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(Read(buffer.Span));
    }
    public override void Flush() { }
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    protected override void Dispose(bool disposing)
    {
        if (disposing) { Interlocked.Increment(ref DisposeCount); _inner.Dispose(); }
        base.Dispose(disposing);
    }
}
