using System.IO;

namespace Bit.BlazorUI;

/// <summary>
/// An owned seekable stream with document-relative cursors and a bounded raw
/// block cache. All seek/read pairs share one gate, including asynchronous copies.
/// Decoded resources and arrays explicitly returned to callers are not cache entries.
/// </summary>
internal sealed class BitPdfSeekableStreamByteSource : IBitPdfByteSource, IBitPdfByteSourceLifetime, IAsyncDisposable
{
    internal const int DefaultBlockSize = 64 * 1024;
    internal const int DefaultCacheSize = 16 * 1024 * 1024;

    private sealed class Block(int start, byte[] bytes)
    {
        public int Start { get; } = start;
        public byte[] Bytes { get; } = bytes;
        public int Pins;
        public LinkedListNode<Block>? Node;
    }

    private readonly Stream _stream;
    private readonly long _origin;
    private readonly long _physicalLength;
    private readonly int _blockSize;
    private readonly int _capacity;
    private readonly object _cacheGate = new();
    private readonly object _disposeGate = new();
    private readonly SemaphoreSlim _ioGate = new(1, 1);
    private readonly CancellationTokenSource _cancellation;
    private readonly CancellationToken _token;
    private readonly Dictionary<int, Block> _blocks = [];
    private readonly LinkedList<Block> _lru = [];
    private Block? _last;
    private int _accounted;
    private int _peak;
    private int _released;
    private Task? _disposeTask;

    public BitPdfSeekableStreamByteSource(Stream stream, CancellationToken cancellationToken = default,
        int cacheSize = DefaultCacheSize, int blockSize = DefaultBlockSize)
    {
        ArgumentNullException.ThrowIfNull(stream);
        if (!stream.CanRead || !stream.CanSeek)
            throw new ArgumentException("A PDF stream factory must return a readable, seekable stream supporting synchronous reads.", nameof(stream));
        _physicalLength = stream.Length;
        _origin = stream.Position;
        if (_origin < 0 || _origin > _physicalLength || _physicalLength - _origin > int.MaxValue)
            throw new ArgumentException("The remaining PDF stream length must be between zero and Int32.MaxValue.", nameof(stream));
        if (blockSize <= 0 || cacheSize < blockSize)
            throw new ArgumentOutOfRangeException(nameof(cacheSize));
        _stream = stream;
        Length = (int)(_physicalLength - _origin);
        _blockSize = blockSize;
        _capacity = cacheSize;
        _cancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _token = _cancellation.Token;
    }

    public int Length { get; }
    internal int AccountedBytes { get { lock (_cacheGate) return _accounted; } }
    internal int PeakBytes { get { lock (_cacheGate) return _peak; } }

    public void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _released) != 0, this);
    }

    public void CheckRead()
    {
        ThrowIfDisposed();
        _token.ThrowIfCancellationRequested();
    }

    public bool TryGetContiguous(out ReadOnlyMemory<byte> memory)
    {
        ThrowIfDisposed();
        memory = default;
        return false;
    }

    public BitPdfByteSourceWindow GetMemory(int offset, int length)
    {
        CheckRead();
        ValidateRange(offset, length);
        if (length == 0) return new(ReadOnlyMemory<byte>.Empty);
        int start = offset / _blockSize * _blockSize;
        int count = Math.Min(_blockSize, Length - start);
        if (length <= count - (offset - start))
        {
            lock (_cacheGate)
            {
                ThrowIfDisposed();
                if (_blocks.TryGetValue(start, out var hit)) return Pin(hit, offset, length);
            }
            EnterRead();
            try
            {
                ThrowIfDisposed();
                lock (_cacheGate)
                {
                    if (_blocks.TryGetValue(start, out var hit)) return Pin(hit, offset, length);
                    Reserve(count);
                }
                try
                {
                    var bytes = new byte[count];
                    ReadLocked(start, bytes);
                    lock (_cacheGate)
                    {
                        ThrowIfDisposed();
                        var block = new Block(start, bytes);
                        block.Node = _lru.AddFirst(block);
                        _blocks.Add(start, block);
                        return Pin(block, offset, length);
                    }
                }
                catch
                {
                    lock (_cacheGate) _accounted -= count;
                    throw;
                }
            }
            finally { _ioGate.Release(); }
        }

        // A cross-block scanner window is an exact, separately accounted copy.
        // It never leaves untracked aliases behind when cache blocks are evicted.
        lock (_cacheGate) Reserve(length);
        try
        {
            var bytes = new byte[length];
            ReadExactly(offset, bytes);
            return new(bytes, new BitPdfByteSourceLease(() => { lock (_cacheGate) _accounted -= length; }));
        }
        catch
        {
            lock (_cacheGate) _accounted -= length;
            throw;
        }
    }

    private BitPdfByteSourceWindow Pin(Block block, int offset, int length)
    {
        Touch(block);
        block.Pins++;
        return new(block.Bytes.AsMemory(offset - block.Start, length), new BitPdfByteSourceLease(() =>
        {
            lock (_cacheGate)
            {
                block.Pins--;
                if (_released != 0 && block.Pins == 0) Remove(block);
            }
        }));
    }

    private void Touch(Block block)
    {
        _lru.Remove(block.Node!);
        _lru.AddFirst(block.Node!);
        _last = block;
    }

    private void Remove(Block block)
    {
        _blocks.Remove(block.Start);
        _lru.Remove(block.Node!);
        if (ReferenceEquals(_last, block)) _last = null;
        _accounted -= block.Bytes.Length;
    }

    private void Reserve(int count)
    {
        ThrowIfDisposed();
        while (count > _capacity - _accounted)
        {
            var node = _lru.Last;
            while (node is not null && node.Value.Pins != 0) node = node.Previous;
            if (node is null)
                throw new BitPdfByteSourceIOException("The PDF raw-byte cache budget is exhausted by active windows.");
            Remove(node.Value);
        }
        _accounted += count;
        _peak = Math.Max(_peak, _accounted);
    }

    internal int ReadByte(int offset)
    {
        CheckRead();
        ValidateRange(offset, 0);
        if (offset == Length) return -1;
        lock (_cacheGate)
        {
            ThrowIfDisposed();
            var block = _last;
            if (block is not null && (uint)(offset - block.Start) < (uint)block.Bytes.Length)
                return block.Bytes[offset - block.Start];
            if (_blocks.TryGetValue(offset / _blockSize * _blockSize, out block))
            {
                Touch(block);
                return block.Bytes[offset - block.Start];
            }
        }
        using var window = GetMemory(offset, 1);
        return window.Span[0];
    }

    internal void ReadExactly(int offset, Span<byte> destination)
    {
        CheckRead();
        ValidateRange(offset, destination.Length);
        EnterRead();
        try { ReadLocked(offset, destination); }
        finally { _ioGate.Release(); }
    }

    private void EnterRead()
    {
        if (!OperatingSystem.IsBrowser())
        {
            _ioGate.Wait(_token);
            return;
        }
        CheckRead();
        // A zero timeout completes immediately, with no queued waiter. A browser
        // cannot block its only thread waiting for an asynchronous copy to finish.
        if (!_ioGate.WaitAsync(0).GetAwaiter().GetResult())
            throw new BitPdfByteSourceIOException("Await the active PDF stream copy before starting a synchronous browser read.");
    }

    private void ReadLocked(int offset, Span<byte> destination)
    {
        try
        {
            CheckLength();
            _stream.Seek(_origin + offset, SeekOrigin.Begin);
            while (!destination.IsEmpty)
            {
                CheckRead();
                int got = _stream.Read(destination);
                if (got <= 0 || got > destination.Length)
                    throw new BitPdfByteSourceIOException("The PDF stream ended before the requested bytes were read.");
                destination = destination[got..];
            }
            CheckLength();
        }
        catch (Exception ex) when (ex is IOException and not BitPdfByteSourceIOException or NotSupportedException)
        {
            throw new BitPdfByteSourceIOException("Reading the PDF stream failed.", ex);
        }
    }

    private void CheckLength()
    {
        CheckRead();
        if (_stream.Length != _physicalLength)
            throw new BitPdfByteSourceIOException("The PDF stream length changed while the document was open.");
    }

    internal async ValueTask ReadExactlyAsync(int offset, Memory<byte> destination, CancellationToken cancellationToken)
    {
        CheckRead();
        ValidateRange(offset, destination.Length);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(_token, cancellationToken);
        await _ioGate.WaitAsync(linked.Token).ConfigureAwait(false);
        try
        {
            CheckLength();
            _stream.Seek(_origin + offset, SeekOrigin.Begin);
            while (!destination.IsEmpty)
            {
                linked.Token.ThrowIfCancellationRequested();
                CheckRead();
                int got = await _stream.ReadAsync(destination, linked.Token).ConfigureAwait(false);
                if (got <= 0 || got > destination.Length)
                    throw new BitPdfByteSourceIOException("The PDF stream ended before the requested bytes were read.");
                destination = destination[got..];
            }
            CheckLength();
        }
        catch (Exception ex) when (ex is IOException and not BitPdfByteSourceIOException or NotSupportedException)
        {
            throw new BitPdfByteSourceIOException("Reading the PDF stream failed.", ex);
        }
        finally { _ioGate.Release(); }
    }

    internal async Task<byte[]> ReadAllAsync(CancellationToken cancellationToken)
    {
        ThrowIfDisposed();
        cancellationToken.ThrowIfCancellationRequested();
        if (Length > Array.MaxLength)
            throw new InvalidOperationException("The PDF is too large for a single managed byte array.");
        var bytes = new byte[Length];
        for (int offset = 0; offset < bytes.Length;)
        {
            int count = Math.Min(DefaultBlockSize, bytes.Length - offset);
            await ReadExactlyAsync(offset, bytes.AsMemory(offset, count), cancellationToken).ConfigureAwait(false);
            offset += count;
        }
        ThrowIfDisposed();
        cancellationToken.ThrowIfCancellationRequested();
        return bytes;
    }

    public BitPdfBaseStream CreateStream(int start = 0, int? length = null)
    {
        CheckRead();
        ValidateRange(start, 0);
        int count = length ?? Length - start;
        ValidateRange(start, count);
        return new Cursor(this, start, count);
    }

    internal Stream OpenReadStream() { ThrowIfDisposed(); return new BitPdfSourceReadStream(this); }

    private void ValidateRange(int offset, int count)
    {
        if ((uint)offset > (uint)Length) throw new ArgumentOutOfRangeException(nameof(offset));
        if (count < 0 || count > Length - offset) throw new ArgumentOutOfRangeException(nameof(count));
    }

    public ValueTask DisposeAsync()
    {
        lock (_disposeGate) return new(_disposeTask ??= CloseAsync());
    }

    private async Task CloseAsync()
    {
        Interlocked.Exchange(ref _released, 1);
        _cancellation.Cancel();
        await _ioGate.WaitAsync().ConfigureAwait(false);
        try { await _stream.DisposeAsync().ConfigureAwait(false); }
        finally
        {
            lock (_cacheGate)
            {
                var node = _lru.First;
                while (node is not null)
                {
                    var next = node.Next;
                    if (node.Value.Pins == 0) Remove(node.Value);
                    node = next;
                }
            }
            _ioGate.Release();
            _cancellation.Dispose();
        }
    }

    private sealed class Cursor : BitPdfBaseStream
    {
        private readonly BitPdfSeekableStreamByteSource _source;
        internal override IBitPdfByteSource ByteSource => _source;
        public Cursor(BitPdfSeekableStreamByteSource source, int start, int length)
        {
            _source = source;
            Start = Pos = start;
            End = start + length;
        }
        public override int GetByte()
        {
            _source.CheckRead();
            if (Pos < Start || Pos > End) throw new ArgumentOutOfRangeException(nameof(Pos));
            if (Pos == End) return -1;
            int value = _source.ReadByte(Pos);
            Pos++;
            return value;
        }
        public override byte[] GetBytes(int length = 0)
        {
            _source.CheckRead();
            if (Pos < Start || Pos > End) throw new ArgumentOutOfRangeException(nameof(Pos));
            int count = length <= 0 ? End - Pos : Math.Min(length, End - Pos);
            var bytes = new byte[count];
            _source.ReadExactly(Pos, bytes);
            Pos += count;
            return bytes;
        }
        public override byte[] GetByteRange(int begin, int end)
        {
            _source.CheckRead();
            begin = Math.Clamp(begin, Start, End);
            end = Math.Clamp(end, Start, End);
            var bytes = new byte[Math.Max(0, end - begin)];
            _source.ReadExactly(begin, bytes);
            return bytes;
        }
        public override void Reset() { _source.ThrowIfDisposed(); Pos = Start; }
        public override void MoveStart()
        {
            _source.CheckRead();
            if (Pos < Start || Pos > End) throw new ArgumentOutOfRangeException(nameof(Pos));
            Start = Pos;
        }
        public override BitPdfBaseStream MakeSubStream(int start, int length, BitPdfDict? dict = null)
        {
            var stream = _source.CreateStream(start, length);
            stream.Dict = dict;
            return stream;
        }
    }
}
