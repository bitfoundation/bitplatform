namespace Bit.Bmage;

/// <summary>
/// Buffers compressed bytes and emits them as a sequence of IDAT chunks. Writing many modest chunks instead of one huge
/// one keeps peak memory flat regardless of image size, and every PNG decoder concatenates them transparently.
/// </summary>
internal sealed class BmageIdatChunkWriter : Stream
{
    private const int ChunkSize = 64 * 1024;

    private readonly Stream _output;
    private readonly byte[] _buffer = new byte[ChunkSize];
    private int _used;

    public BmageIdatChunkWriter(Stream output) => _output = output;

    public override bool CanRead => false;
    public override bool CanSeek => false;
    public override bool CanWrite => true;
    public override long Length => throw new NotSupportedException();
    public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

    public override void Write(ReadOnlySpan<byte> buffer)
    {
        while (!buffer.IsEmpty)
        {
            var room = ChunkSize - _used;
            var take = Math.Min(room, buffer.Length);
            buffer[..take].CopyTo(_buffer.AsSpan(_used));
            _used += take;
            buffer = buffer[take..];
            if (_used == ChunkSize) FlushChunk();
        }
    }

    public override void Write(byte[] buffer, int offset, int count) => Write(buffer.AsSpan(offset, count));

    public override void WriteByte(byte value)
    {
        _buffer[_used++] = value;
        if (_used == ChunkSize) FlushChunk();
    }

    private void FlushChunk()
    {
        if (_used == 0) return;
        BmagePngContainer.WriteChunk(_output, "IDAT", _buffer.AsSpan(0, _used));
        _used = 0;
    }

    public override void Flush() { }

    protected override void Dispose(bool disposing)
    {
        if (disposing) FlushChunk();
        base.Dispose(disposing);
    }

    public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
}
