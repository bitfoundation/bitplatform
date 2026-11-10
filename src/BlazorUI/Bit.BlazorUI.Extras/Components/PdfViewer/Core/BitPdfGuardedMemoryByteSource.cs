namespace Bit.BlazorUI;

/// <summary>
/// Decrypted stream bytes are resource storage, separate from the raw cache,
/// but streams returned through the document still share its active-load lifetime.
/// </summary>
internal sealed class BitPdfGuardedMemoryByteSource(byte[] bytes, IBitPdfByteSourceLifetime lifetime)
    : IBitPdfByteSource, IBitPdfByteSourceLifetime
{
    private readonly BitPdfMemoryByteSource _memory = new(bytes);
    public int Length => _memory.Length;
    public void ThrowIfDisposed() => lifetime.ThrowIfDisposed();
    public void CheckRead() => lifetime.CheckRead();
    public bool TryGetContiguous(out ReadOnlyMemory<byte> memory)
    {
        CheckRead();
        // Use guarded reader windows even when a parser retains its own reader.
        memory = default;
        return false;
    }
    public BitPdfByteSourceWindow GetMemory(int offset, int length)
    {
        CheckRead();
        return _memory.GetMemory(offset, length);
    }
    public BitPdfBaseStream CreateStream(int start = 0, int? length = null)
    {
        CheckRead();
        return new Cursor(this, _memory.CreateStream(start, length));
    }

    private sealed class Cursor : BitPdfBaseStream
    {
        private readonly BitPdfGuardedMemoryByteSource _source;
        private readonly BitPdfBaseStream _inner;
        internal override IBitPdfByteSource ByteSource => _source;
        public Cursor(BitPdfGuardedMemoryByteSource source, BitPdfBaseStream inner)
        {
            _source = source;
            _inner = inner;
            Start = Pos = inner.Start;
            End = inner.End;
        }
        public override int GetByte()
        {
            BitPdfXRef.TrackPartialStream(this);
            _source.CheckRead();
            _inner.Pos = Pos;
            int result = _inner.GetByte();
            Pos = _inner.Pos;
            return result;
        }
        public override byte[] GetBytes(int length = 0)
        {
            BitPdfXRef.TrackPartialStream(this);
            _source.CheckRead();
            _inner.Pos = Pos;
            byte[] result = _inner.GetBytes(length);
            Pos = _inner.Pos;
            return result;
        }
        public override byte[] GetByteRange(int begin, int end)
        {
            BitPdfXRef.TrackPartialStream(this);
            _source.CheckRead();
            return _inner.GetByteRange(begin, end);
        }
        public override void Skip(int n = 1) { BitPdfXRef.TrackPartialStream(this); base.Skip(n); }
        public override void Reset() { BitPdfXRef.TrackPartialStream(this); _source.CheckRead(); Pos = Start; }
        public override void MoveStart()
        {
            BitPdfXRef.TrackPartialStream(this);
            _source.CheckRead();
            _inner.Pos = Pos;
            _inner.MoveStart();
            Start = _inner.Start;
        }
        public override BitPdfBaseStream MakeSubStream(int start, int length, BitPdfDict? dict = null)
        {
            var stream = _source.CreateStream(start, length);
            stream.Dict = dict;
            return stream;
        }
    }
}
