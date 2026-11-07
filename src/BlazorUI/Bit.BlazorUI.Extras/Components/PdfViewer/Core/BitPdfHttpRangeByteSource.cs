using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;

namespace Bit.BlazorUI;

/// <summary>Cached synchronous reads with explicit, asynchronous HTTP acquisition.
/// No reader, cursor or cache miss performs network I/O.</summary>
internal sealed class BitPdfHttpRangeByteSource : IBitPdfPartialByteSource, IAsyncDisposable
{
    internal sealed class Block(int start, byte[] bytes)
    {
        internal readonly int Start = start;
        internal readonly byte[] Bytes = bytes;
        internal int Pins;
        internal LinkedListNode<Block>? Node;
    }

    internal sealed class Operation(BitPdfHttpRangeByteSource source, Operation? previous) : IDisposable
    {
        internal readonly HashSet<Block> Pins = [];
        internal int PinnedBytes;
        internal int Requests;
        private readonly Operation? _previous = previous;
        internal bool Released;
        internal void CountRequest()
        {
            for (Operation? owner = this; owner is not null; owner = owner._previous)
            {
                if (Interlocked.Increment(ref owner.Requests) > 1024)
                {
                    throw new BitPdfRangeLimitException("The PDF operation exceeded its range-request limit.");
                }
            }
        }

        internal void Pin(Block block)
        {
            if (!Released && Pins.Add(block))
            {
                block.Pins++;
                PinnedBytes += block.Bytes.Length;
            }
        }

        public void Dispose()
        {
            lock (source._gate)
            {
                if (Released)
                {
                    return;
                }

                Released = true;
                source.ReleasePins(this);
                source._operation.Value = _previous;
            }
        }
    }

    private readonly HttpClient _http;
    private readonly BitPdfSource _source;
    private readonly BitPdfRangeLoadingOptions _options;
    private readonly string _etag;
    private readonly object _gate = new();
    private readonly Dictionary<int, Block> _blocks = [];
    private readonly LinkedList<Block> _lru = [];
    private readonly Dictionary<int, Task> _fetches = [];
    private readonly AsyncLocal<Operation?> _operation = new();
    private readonly SemaphoreSlim _requests = new(2, 2);
    private readonly CancellationTokenSource _cancellation;
    private readonly CancellationToken _token;
    private TaskCompletionSource _changed = NewSignal();
    private Task? _disposeTask;
    private volatile BitPdfRangeVersionException? _failure;
    private volatile bool _disposed;
    private int _accounted;
    private int _peak;
    private int _requestCount;
    private int _restartCount;
    private long _fetchedBytes;

    private BitPdfHttpRangeByteSource(HttpClient http, BitPdfSource source, int length, string etag, CancellationToken token)
    {
        _http = http;
        _source = source;
        _options = source.RangeLoading!;
        Length = length;
        _etag = etag;
        _cancellation = CancellationTokenSource.CreateLinkedTokenSource(token);
        _token = _cancellation.Token;
    }

    public int Length { get; }
    int IBitPdfPartialByteSource.ChunkSize => _options.ChunkSize;

    internal int AccountedBytes
    {
        get
        {
            lock (_gate)
            {
                return _accounted;
            }
        }
    }

    internal int PeakBytes
    {
        get
        {
            lock (_gate)
            {
                return _peak;
            }
        }
    }

    internal int RequestCount => Volatile.Read(ref _requestCount);
    internal int RestartCount => Volatile.Read(ref _restartCount);

    internal void RecordRestart() => Interlocked.Increment(ref _restartCount);

    internal long FetchedBytes => Interlocked.Read(ref _fetchedBytes);

    internal Operation BeginOperation()
    {
        CheckRead();
        var operation = new Operation(this, _operation.Value);
        _operation.Value = operation;
        return operation;
    }

    internal static async Task<BitPdfHttpRangeByteSource> OpenAsync(HttpClient http, BitPdfSource source, CancellationToken token)
    {
        using var request = CreateRequest(source, 0, source.RangeLoading!.ChunkSize - 1, null);
        var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token).ConfigureAwait(false);
        if (response.StatusCode == HttpStatusCode.OK)
        {
            throw new BitPdfRangeFallbackException("The server returned a complete PDF instead of a byte range.", response);
        }

        BitPdfHttpRangeByteSource? opened = null;
        int initialReservation = 0;
        bool initialCommitted = false;
        try
        {
            int length = ValidateHeaders(response, 0, source.RangeLoading.ChunkSize - 1, null, null);
            string etag = ReadStrongTag(response)!;
            opened = new(http, source, length, etag, token);
            opened._requestCount = 1;
            int count = Math.Min(source.RangeLoading.ChunkSize, length);
            lock (opened._gate)
            {
                opened.Reserve(count);
            }

            initialReservation = count;
            await opened.ReadResponseAsync(response, 0, count, null, token).ConfigureAwait(false);
            initialCommitted = true;
            return opened;
        }
        catch (Exception ex)
        {
            if (opened is not null)
            {
                if (!initialCommitted)
                {
                    lock (opened._gate)
                    {
                        opened._accounted -= initialReservation;
                    }
                }

                await opened.DisposeAsync().ConfigureAwait(false);
            }

            if (ex is BitPdfRangeFallbackException or OperationCanceledException or ObjectDisposedException)
            {
                throw;
            }

            throw new BitPdfRangeFallbackException(ex.Message);
        }
        finally
        {
            response.Dispose();
        }
    }

    private static HttpRequestMessage CreateRequest(BitPdfSource source, int start, int? end, string? etag)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, source.Url);
        if (source.Headers is { } headers)
        {
            foreach (var header in headers)
            {
                request.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        // Byte identity and transport coordinates belong to the source, not custom headers.
        request.Headers.Remove("Range");
        request.Headers.Remove("If-Range");
        request.Headers.Range = new RangeHeaderValue(start, end);
        if (etag is not null)
        {
            request.Headers.TryAddWithoutValidation("If-Range", etag);
        }

        request.Options.Set(new HttpRequestOptionsKey<bool>("WebAssemblyEnableStreamingResponse"), true);
        return request;
    }

    private static string? ReadStrongTag(HttpResponseMessage response)
    {
        if (!response.Headers.TryGetValues("ETag", out var values))
        {
            return null;
        }

        string[] tags = values.ToArray();
        if (tags.Length != 1)
        {
            return null;
        }

        string value = tags[0].Trim();
        return value.StartsWith('"')
            && value.EndsWith('"')
            && EntityTagHeaderValue.TryParse(value, out var tag)
            && !tag.IsWeak ? value : null;
    }

    private static int ValidateHeaders(HttpResponseMessage response, int start, int end, int? length,
        string? etag, bool allowShortRange = false)
    {
        if (response.StatusCode == HttpStatusCode.OK && etag is not null)
        {
            throw new BitPdfRangeVersionException("The PDF server no longer honored If-Range. Assign a new source to reload the document.");
        }

        if (response.StatusCode != HttpStatusCode.PartialContent)
        {
            throw new BitPdfByteSourceIOException($"The PDF range request returned HTTP {(int)response.StatusCode}.");
        }

        var range = response.Content.Headers.ContentRange;
        if (range is null
            || !range.HasLength
            || !range.HasRange
            || !StringComparer.OrdinalIgnoreCase.Equals(range.Unit, "bytes")
            || range.Length is not > 0
            || range.Length > int.MaxValue)
        {
            throw new BitPdfRangePermanentException("The PDF response has no usable byte Content-Range and total length.");
        }

        int total = (int)range.Length.Value;
        string? tag = ReadStrongTag(response);
        if (etag is not null && ((tag is not null && !StringComparer.Ordinal.Equals(tag, etag)) || total != length))
        {
            throw new BitPdfRangeVersionException("The PDF changed while it was open. Assign a new source to reload the document.");
        }

        if (tag is null)
        {
            throw new BitPdfRangePermanentException("HTTP Range loading requires an exposed, strong ETag on every response.");
        }

        int requestedEnd = Math.Min(end, total - 1);
        if (range.From != start
            || range.To < start
            || range.To > requestedEnd
            || (!allowShortRange && range.To != requestedEnd)
            || (response.Content.Headers.ContentLength is long actual
                && actual != range.To - start + 1))
        {
            throw new BitPdfRangePermanentException("The PDF Content-Range or body length does not match the requested bytes.");
        }

        if (response.Content.Headers.ContentEncoding.Any(e => !StringComparer.OrdinalIgnoreCase.Equals(e, "identity")))
        {
            throw new BitPdfRangePermanentException("Compressed HTTP PDF responses cannot be addressed with byte ranges.");
        }

        return total;
    }

    private int ValidateCopyResponse(HttpResponseMessage response, int start)
    {
        if (response.StatusCode != HttpStatusCode.OK)
        {
            ValidateHeaders(response, start, Length - 1, Length, _etag, allowShortRange: true);
            return checked((int)response.Content.Headers.ContentRange!.To!.Value + 1);
        }

        string? tag = ReadStrongTag(response);
        if (tag is not null && !StringComparer.Ordinal.Equals(tag, _etag))
        {
            throw new BitPdfRangeVersionException("The PDF changed while it was open. Assign a new source to reload the document.");
        }

        // A full representation is safe only for a copy starting at zero, with
        // the exact captured version and length. Cache acquisitions stay strict.
        if (start != 0 || tag is null || response.Content.Headers.ContentLength != Length
            || response.Content.Headers.ContentEncoding.Any(e => !StringComparer.OrdinalIgnoreCase.Equals(e, "identity")))
        {
            throw new BitPdfRangePermanentException("The PDF copy response does not identify the complete original representation.");
        }

        return Length;
    }

    public void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    public void CheckRead()
    {
        ThrowIfDisposed();
        if (_failure is { } failure)
        {
            throw failure;
        }

        _token.ThrowIfCancellationRequested();
    }

    public bool TryGetContiguous(out ReadOnlyMemory<byte> memory)
    {
        CheckRead();
        memory = default;
        return false;
    }

    private void ValidateRange(int offset, int length)
    {
        if ((uint)offset > (uint)Length)
        {
            throw new ArgumentOutOfRangeException(nameof(offset));
        }

        if (length < 0 || length > Length - offset)
        {
            throw new ArgumentOutOfRangeException(nameof(length));
        }
    }

    private int StartOf(int offset) => offset / _options.ChunkSize * _options.ChunkSize;

    public bool IsDataAvailable(int offset, int length)
    {
        CheckRead();
        ValidateRange(offset, length);
        if (length == 0)
        {
            return true;
        }

        lock (_gate)
        {
            for (long start = StartOf(offset); start < (long)offset + length; start += _options.ChunkSize)
            {
                if (!_blocks.ContainsKey((int)start))
                {
                    return false;
                }
            }

            return true;
        }
    }

    private List<Block> GetBlocks(int offset, int length)
    {
        var blocks = new List<Block>();
        for (long start = StartOf(offset); start < (long)offset + length; start += _options.ChunkSize)
        {
            if (!_blocks.TryGetValue((int)start, out var block))
            {
                throw new BitPdfMissingDataException(offset, length);
            }

            blocks.Add(block);
        }

        foreach (var block in blocks)
        {
            _operation.Value?.Pin(block);
            Touch(block);
        }

        return blocks;
    }

    public BitPdfByteSourceWindow GetMemory(int offset, int length)
    {
        CheckRead();
        ValidateRange(offset, length);
        if (length == 0)
        {
            return new(ReadOnlyMemory<byte>.Empty);
        }

        lock (_gate)
        {
            var blocks = GetBlocks(offset, length);
            if (blocks.Count == 1)
            {
                var block = blocks[0];
                block.Pins++;
                return new(block.Bytes.AsMemory(offset - block.Start, length), new BitPdfByteSourceLease(() => Unpin(block)));
            }

            foreach (var block in blocks)
            {
                block.Pins++;
            }

            try
            {
                Reserve(length);
                try
                {
                    var bytes = new byte[length];
                    Copy(blocks, offset, bytes);
                    return new(bytes, new BitPdfByteSourceLease(() =>
                    {
                        lock (_gate)
                        {
                            _accounted -= length;
                            Pulse();
                        }
                    }));
                }
                catch
                {
                    _accounted -= length;
                    throw;
                }
            }
            finally
            {
                foreach (var block in blocks)
                {
                    Unpin(block);
                }
            }
        }
    }

    private static void Copy(List<Block> blocks, int offset, Span<byte> destination)
    {
        foreach (var block in blocks)
        {
            int from = Math.Max(offset - block.Start, 0);
            int count = Math.Min(block.Bytes.Length - from, destination.Length);
            block.Bytes.AsSpan(from, count).CopyTo(destination);
            destination = destination[count..];
            offset += count;
        }
    }

    private void ReadExactly(int offset, Span<byte> destination)
    {
        CheckRead();
        ValidateRange(offset, destination.Length);
        if (destination.IsEmpty)
        {
            return;
        }

        lock (_gate)
        {
            Copy(GetBlocks(offset, destination.Length), offset, destination);
        }
    }

    private int ReadByte(int offset)
    {
        CheckRead();
        ValidateRange(offset, 0);
        if (offset == Length)
        {
            return -1;
        }

        lock (_gate)
        {
            if (!_blocks.TryGetValue(StartOf(offset), out var block))
            {
                throw new BitPdfMissingDataException(offset, 1);
            }

            _operation.Value?.Pin(block);
            Touch(block);
            return block.Bytes[offset - block.Start];
        }
    }

    private void Touch(Block block)
    {
        if (_lru.First != block.Node)
        {
            _lru.Remove(block.Node!);
            _lru.AddFirst(block.Node!);
        }
    }

    private static TaskCompletionSource NewSignal() => new(TaskCreationOptions.RunContinuationsAsynchronously);

    private void Pulse()
    {
        var signal = _changed;
        _changed = NewSignal();
        signal.TrySetResult();
    }

    private void Remove(Block block)
    {
        _blocks.Remove(block.Start);
        _lru.Remove(block.Node!);
        _accounted -= block.Bytes.Length;
    }

    private void Unpin(Block block)
    {
        lock (_gate)
        {
            block.Pins--;
            if (_disposed && block.Pins == 0)
            {
                Remove(block);
            }

            Pulse();
        }
    }

    private void ReleasePins(Operation operation)
    {
        foreach (var block in operation.Pins)
        {
            Unpin(block);
        }

        operation.Pins.Clear();
        operation.PinnedBytes = 0;
        Pulse();
    }

    private bool EvictFor(int count)
    {
        while (count > _options.CacheSize - _accounted)
        {
            var node = _lru.Last;
            while (node is not null && node.Value.Pins != 0)
            {
                node = node.Previous;
            }

            if (node is null)
            {
                return false;
            }

            Remove(node.Value);
        }

        return true;
    }

    private void Reserve(int count)
    {
        CheckRead();
        if (!EvictFor(count))
        {
            throw new BitPdfRangeBudgetException();
        }

        _accounted += count;
        _peak = Math.Max(_peak, _accounted);
    }

    internal async Task EnsureAsync(int offset, int length, CancellationToken cancellationToken = default)
    {
        CheckRead();
        ValidateRange(offset, length);
        if (length == 0)
        {
            return;
        }

        int alignedStart = StartOf(offset);
        long alignedEnd = Math.Min(Length,
            ((long)offset + length + _options.ChunkSize - 1) / _options.ChunkSize * _options.ChunkSize);
        if (alignedEnd - alignedStart > _options.CacheSize)
        {
            throw new BitPdfRangeBudgetException();
        }

        var owner = _operation.Value;
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(_token, cancellationToken);
        while (true)
        {
            CheckRead();
            linked.Token.ThrowIfCancellationRequested();
            Task pending;
            TaskCompletionSource? completion = null;
            Task? capacity = null;
            int first = alignedStart, count = 0;
            lock (_gate)
            {
                while (first < alignedEnd && _blocks.ContainsKey(first))
                {
                    owner?.Pin(_blocks[first]);
                    first = (int)Math.Min(alignedEnd, (long)first + _options.ChunkSize);
                }

                if (first >= alignedEnd)
                {
                    return;
                }

                if (_fetches.TryGetValue(first, out var active))
                {
                    pending = active;
                }
                else
                {
                    int batchSize = Math.Max(_options.ChunkSize, Math.Min(1024 * 1024, _options.CacheSize));
                    int limit = (int)Math.Min(alignedEnd, (long)first + batchSize);
                    long end = first;
                    while (end < limit && !_blocks.ContainsKey((int)end) && !_fetches.ContainsKey((int)end))
                    {
                        end = Math.Min(alignedEnd, end + _options.ChunkSize);
                    }

                    count = (int)end - first;
                    if (!EvictFor(count))
                    {
                        // Accounting includes cached blocks, in-flight reservations
                        // and leased windows. An operation's own pinned working set
                        // must fit; another operation's pins are temporary contention.
                        if (owner is null || owner.PinnedBytes + (long)count > _options.CacheSize)
                        {
                            throw new BitPdfRangeBudgetException();
                        }

                        // A retry can reacquire its bytes. Release its pins before
                        // waiting so concurrent operations cannot keep each other full.
                        ReleasePins(owner);
                        if (EvictFor(count))
                        {
                            Pulse();
                        }
                        else
                        {
                            // Capture under the lock so an unpin cannot be missed.
                            capacity = _changed.Task;
                        }
                    }

                    if (capacity is null)
                    {
                        Reserve(count);
                        completion = NewSignal();
                        pending = completion.Task;
                        for (long start = first; start < (long)first + count; start += _options.ChunkSize)
                        {
                            _fetches.Add((int)start, pending);
                        }
                    }
                    else
                    {
                        pending = capacity;
                    }
                }
            }

            // Capacity and HTTP waits both happen outside the cache lock.
            if (capacity is not null)
            {
                await WaitForAcquisitionAsync(capacity, linked.Token).ConfigureAwait(false);
                continue;
            }

            if (completion is not null)
            {
                _ = FillAsync(first, count, owner, completion, linked.Token);
            }

            await WaitForAcquisitionAsync(pending, linked.Token).ConfigureAwait(false);
        }
    }

    private async Task WaitForAcquisitionAsync(Task pending, CancellationToken token)
    {
        try
        {
            await pending.WaitAsync(token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (_failure is not null)
        {
            throw _failure;
        }
        catch (OperationCanceledException) when (!token.IsCancellationRequested)
        {
            // Another waiter cancelled the shared producer. The caller's next
            // pass can reserve a replacement; transport timeouts still propagate.
        }
    }

    internal int MetadataChunkSize => _options.ChunkSize;
    internal int MetadataBatchSize => Math.Min(1024 * 1024, _options.CacheSize / 4);

    internal async Task EnsureRangesAsync(IEnumerable<(int Offset, int Length)> ranges, CancellationToken token)
    {
        // Merge overlapping/adjacent dependency windows before occupying request slots.
        var ordered = ranges.Where(r => r.Length > 0).OrderBy(r => r.Offset).ToArray();
        var merged = new List<(int Offset, int Length)>();
        foreach (var range in ordered)
        {
            if (merged.Count > 0)
            {
                var last = merged[^1];
                long lastEnd = (long)last.Offset + last.Length;
                long mergedEnd = Math.Max((long)range.Offset + range.Length, lastEnd);
                long alignedEnd = Math.Min(Length,
                    (mergedEnd + _options.ChunkSize - 1) / _options.ChunkSize * _options.ChunkSize);
                long reservedLength = alignedEnd - StartOf(last.Offset);
                if (range.Offset <= lastEnd && reservedLength <= Math.Min(_options.CacheSize, 1024 * 1024))
                {
                    merged[^1] = (last.Offset, (int)(mergedEnd - last.Offset));
                    continue;
                }
            }

            merged.Add(range);
        }

        await Task.WhenAll(merged.Select(r => EnsureAsync(r.Offset, r.Length, token))).ConfigureAwait(false);
    }

    private async Task FillAsync(int start, int count, Operation? owner, TaskCompletionSource completion, CancellationToken token)
    {
        bool entered = false, committed = false;
        try
        {
            await _requests.WaitAsync(token).ConfigureAwait(false);
            entered = true;
            owner?.CountRequest();
            using var request = CreateRequest(_source, start, start + count - 1, _etag);
            Interlocked.Increment(ref _requestCount);
            using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token).ConfigureAwait(false);
            ValidateHeaders(response, start, start + count - 1, Length, _etag);
            await ReadResponseAsync(response, start, count, owner, token).ConfigureAwait(false);
            committed = true;
            completion.TrySetResult();
        }
        catch (BitPdfRangeVersionException ex)
        {
            _failure = ex;
            _cancellation.Cancel();
            completion.TrySetException(ex);
        }
        // A cancelled producer lets other shared waiters acquire the range again.
        // HttpClient timeouts do not cancel our token and must surface as I/O failures.
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            completion.TrySetCanceled(token);
        }
        catch (Exception ex)
        {
            completion.TrySetException(ex is BitPdfByteSourceIOException ? ex : new BitPdfByteSourceIOException("Reading the PDF range failed.", ex));
        }
        finally
        {
            lock (_gate)
            {
                for (long offset = start; offset < (long)start + count; offset += _options.ChunkSize)
                {
                    _fetches.Remove((int)offset);
                }

                if (!committed)
                {
                    _accounted -= count;
                }

                Pulse();
            }

            if (entered)
            {
                _requests.Release();
            }
        }
    }

    private async Task ReadResponseAsync(HttpResponseMessage response, int start, int count, Operation? owner, CancellationToken token)
    {
        var blocks = new List<Block>();
        await using var body = await response.Content.ReadAsStreamAsync(token).ConfigureAwait(false);
        for (int consumed = 0; consumed < count;)
        {
            var bytes = new byte[Math.Min(_options.ChunkSize, count - consumed)];
            int got = 0;
            while (got < bytes.Length)
            {
                int read = await body.ReadAsync(bytes.AsMemory(got), token).ConfigureAwait(false);
                if (read == 0)
                {
                    throw new BitPdfByteSourceIOException("The PDF range body ended prematurely.");
                }

                got += read;
            }

            blocks.Add(new(start + consumed, bytes));
            consumed += bytes.Length;
        }

        // Read the EOF probe into existing storage: an invalid extra byte is never committed.
        var last = blocks[^1].Bytes;
        if (await body.ReadAsync(last.AsMemory(last.Length - 1, 1), token).ConfigureAwait(false) != 0)
        {
            throw new BitPdfByteSourceIOException("The PDF range body exceeds its declared range.");
        }

        CheckRead();
        token.ThrowIfCancellationRequested();
        lock (_gate)
        {
            CheckRead();
            foreach (var block in blocks)
            {
                block.Node = _lru.AddFirst(block);
                _blocks.Add(block.Start, block);
                owner?.Pin(block);
            }
        }

        Interlocked.Add(ref _fetchedBytes, count);
    }

    internal async Task<byte[]> ReadAllAsync(CancellationToken token)
    {
        CheckRead();
        if (Length > Array.MaxLength)
        {
            throw new InvalidOperationException("The PDF is too large for a single managed byte array.");
        }

        await using var stream = OpenReadStream();
        var bytes = new byte[Length];
        for (int start = 0; start < Length;)
        {
            start += await stream.ReadAsync(bytes.AsMemory(start), token).ConfigureAwait(false);
        }

        CheckRead();
        token.ThrowIfCancellationRequested();
        return bytes;
    }

    public BitPdfBaseStream CreateStream(int start = 0, int? length = null)
    {
        CheckRead();
        ValidateRange(start, length ?? Length - start);
        return new Cursor(this, start, length ?? Length - start);
    }

    internal Stream OpenReadStream()
    {
        CheckRead();
        return new DownloadStream(this);
    }

    public ValueTask DisposeAsync()
    {
        lock (_gate)
        {
            return new(_disposeTask ??= CloseAsync());
        }
    }

    private async Task CloseAsync()
    {
        _disposed = true;
        _cancellation.Cancel();
        Task[] pending;
        lock (_gate)
        {
            pending = _fetches.Values.Distinct().ToArray();
            Pulse();
        }

        try
        {
            await Task.WhenAll(pending).ConfigureAwait(false);
        }
        catch
        {
        }

        lock (_gate)
        {
            foreach (var block in _blocks.Values.Where(b => b.Pins == 0).ToArray())
            {
                Remove(block);
            }
        }

        _cancellation.Dispose();
    }

    private sealed class Cursor : BitPdfBaseStream
    {
        private readonly BitPdfHttpRangeByteSource _source;
        internal override IBitPdfByteSource ByteSource => _source;

        internal Cursor(BitPdfHttpRangeByteSource source, int start, int length)
        {
            _source = source;
            Start = Pos = start;
            End = start + length;
        }

        public override int GetByte()
        {
            BitPdfXRef.TrackPartialStream(this);
            _source.CheckRead();
            if (Pos < Start || Pos > End)
            {
                throw new ArgumentOutOfRangeException(nameof(Pos));
            }

            if (Pos == End)
            {
                return -1;
            }

            try
            {
                int value = _source.ReadByte(Pos);
                Pos++;
                return value;
            }
            catch (BitPdfMissingDataException) when (Dict is not null)
            {
                throw new BitPdfMissingDataException(Start, Length);
            }
        }

        public override byte[] GetBytes(int length = 0)
        {
            BitPdfXRef.TrackPartialStream(this);
            _source.CheckRead();
            if (Pos < Start || Pos > End)
            {
                throw new ArgumentOutOfRangeException(nameof(Pos));
            }

            int count = length <= 0 ? End - Pos : Math.Min(length, End - Pos);
            byte[] bytes = new byte[count];
            try
            {
                _source.ReadExactly(Pos, bytes);
            }
            catch (BitPdfMissingDataException) when (Dict is not null)
            {
                throw new BitPdfMissingDataException(Start, Length);
            }

            Pos += count;
            return bytes;
        }

        public override byte[] GetByteRange(int begin, int end)
        {
            begin = Math.Clamp(begin, Start, End);
            end = Math.Clamp(end, Start, End);
            var bytes = new byte[Math.Max(0, end - begin)];
            _source.ReadExactly(begin, bytes);
            return bytes;
        }

        public override void Skip(int n = 1)
        {
            BitPdfXRef.TrackPartialStream(this);
            base.Skip(n);
        }

        public override void Reset()
        {
            BitPdfXRef.TrackPartialStream(this);
            _source.ThrowIfDisposed();
            Pos = Start;
        }

        public override void MoveStart()
        {
            BitPdfXRef.TrackPartialStream(this);
            _source.CheckRead();
            if (Pos < Start || Pos > End)
            {
                throw new ArgumentOutOfRangeException(nameof(Pos));
            }

            Start = Pos;
        }

        public override BitPdfBaseStream MakeSubStream(int start, int length, BitPdfDict? dict = null)
        {
            var stream = _source.CreateStream(start, length);
            stream.Dict = dict;
            return stream;
        }
    }

    private sealed class DownloadStream(BitPdfHttpRangeByteSource source) : Stream
    {
        // Copies own their responses and bypass the LRU. Usually one open-ended
        // request suffices; shortened responses resume at the first unread byte.
        private readonly CancellationTokenSource _cancellation = CancellationTokenSource.CreateLinkedTokenSource(source._token);
        private readonly byte[] _probe = new byte[1];
        private HttpResponseMessage? _response;
        private Stream? _body;
        private int _position;
        private int _responseEnd;
        private bool _disposed;
        public override bool CanRead => !_disposed;
        public override bool CanSeek => !_disposed;
        public override bool CanWrite => false;
        public override long Length => source.Length;

        public override long Position
        {
            get => _position;
            set
            {
                ObjectDisposedException.ThrowIf(_disposed, this);
                if (value < 0 || value > Length)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }

                if (value == _position)
                {
                    return;
                }

                CloseResponse();
                _position = (int)value;
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            ArgumentNullException.ThrowIfNull(buffer);
            var destination = buffer.AsSpan(offset, count);
            ObjectDisposedException.ThrowIf(_disposed, this);
            count = Math.Min(count, source.Length - _position);
            // Synchronous reads remain cache-only and never start HTTP work.
            source.ReadExactly(_position, destination[..count]);
            if (count > 0)
            {
                CloseResponse();
            }

            _position += count;
            return count;
        }

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            source.CheckRead();
            cancellationToken.ThrowIfCancellationRequested();
            if (buffer.IsEmpty || _position == source.Length)
            {
                return 0;
            }

            using var readCancellation = CancellationTokenSource.CreateLinkedTokenSource(_cancellation.Token, cancellationToken);
            // ResponseHeadersRead limits HttpClient.Timeout to the headers.
            // Apply the same timeout to each body read, including the EOF probe.
            if (source._http.Timeout != Timeout.InfiniteTimeSpan)
            {
                readCancellation.CancelAfter(source._http.Timeout);
            }

            using var registration = readCancellation.Token.Register(() => _cancellation.Cancel());
            try
            {
                if (_body is null)
                {
                    // Each validated segment advances the position, so copies do
                    // not consume the parser's missing-data restart/request budget.
                    using var request = CreateRequest(source._source, _position, null, source._etag);
                    Interlocked.Increment(ref source._requestCount);
                    var response = await source._http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, _cancellation.Token).ConfigureAwait(false);
                    try
                    {
                        _responseEnd = source.ValidateCopyResponse(response, _position);
                        var body = await response.Content.ReadAsStreamAsync(readCancellation.Token).ConfigureAwait(false);
                        source.CheckRead();
                        _response = response;
                        _body = body;
                    }
                    catch
                    {
                        response.Dispose();
                        throw;
                    }
                }

                int count = Math.Min(buffer.Length, _responseEnd - _position);
                int read = await _body.ReadAsync(buffer[..count], readCancellation.Token).ConfigureAwait(false);
                if (read == 0)
                {
                    throw new BitPdfByteSourceIOException("The PDF range body ended prematurely.");
                }

                if (_position + read == _responseEnd
                    && await _body.ReadAsync(_probe, readCancellation.Token).ConfigureAwait(false) != 0)
                {
                    throw new BitPdfRangePermanentException("The PDF range body exceeds its declared range.");
                }

                source.CheckRead();
                readCancellation.Token.ThrowIfCancellationRequested();
                _position += read;
                if (_position == _responseEnd)
                {
                    CloseResponse();
                }

                Interlocked.Add(ref source._fetchedBytes, read);
                return read;
            }
            catch (BitPdfRangeVersionException ex)
            {
                source._failure = ex;
                source._cancellation.Cancel();
                throw;
            }
            catch (OperationCanceledException) when (source._failure is not null)
            {
                throw source._failure;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested || source._token.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex) when (ex is HttpRequestException or IOException or OperationCanceledException
                && ex is not BitPdfByteSourceIOException)
            {
                throw new BitPdfByteSourceIOException("Reading the PDF copy failed.", ex);
            }
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();
        public override long Seek(long offset, SeekOrigin origin)
        {
            Position = checked((origin switch
            {
                SeekOrigin.Begin => 0L,
                SeekOrigin.Current => Position,
                SeekOrigin.End => Length,
                _ => throw new ArgumentOutOfRangeException(nameof(origin))
            }) + offset);
            return Position;
        }

        public override void Flush()
        {
        }

        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        private void CloseResponse()
        {
            _body = null;
            _response?.Dispose();
            _response = null;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && !_disposed)
            {
                _disposed = true;
                _cancellation.Cancel();
                CloseResponse();
                _cancellation.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
