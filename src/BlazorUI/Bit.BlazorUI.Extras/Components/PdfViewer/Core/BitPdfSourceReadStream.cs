using System.IO;

namespace Bit.BlazorUI;

/// <summary>An independent download cursor; disposing it does not own the document.</summary>
internal sealed class BitPdfSourceReadStream(BitPdfSeekableStreamByteSource source) : Stream
{
    private int _position;
    private bool _disposed;
    public override bool CanRead => !_disposed;
    public override bool CanSeek => !_disposed;
    public override bool CanWrite => false;
    public override long Length { get { Check(); return source.Length; } }
    public override long Position
    {
        get { Check(); return _position; }
        set { Check(); if (value < 0 || value > source.Length) throw new ArgumentOutOfRangeException(nameof(value)); _position = (int)value; }
    }
    private void Check() { ObjectDisposedException.ThrowIf(_disposed, this); source.ThrowIfDisposed(); }
    public override int Read(byte[] buffer, int offset, int count) => Read(buffer.AsSpan(offset, count));
    public override int Read(Span<byte> buffer)
    {
        Check();
        int count = Math.Min(buffer.Length, source.Length - _position);
        source.ReadExactly(_position, buffer[..count]);
        _position += count;
        return count;
    }
    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        Check();
        int count = Math.Min(buffer.Length, source.Length - _position);
        if (OperatingSystem.IsBrowser())
            await source.ReadExactlyAsync(_position, buffer[..count], cancellationToken).ConfigureAwait(false);
        else
            await Task.Run(async () => await source.ReadExactlyAsync(_position, buffer[..count], cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);
        _position += count;
        return count;
    }
    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        => ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();
    public override long Seek(long offset, SeekOrigin origin)
    {
        Position = checked((origin switch { SeekOrigin.Begin => 0L, SeekOrigin.Current => _position, SeekOrigin.End => source.Length, _ => throw new ArgumentOutOfRangeException(nameof(origin)) }) + offset);
        return _position;
    }
    protected override void Dispose(bool disposing) { _disposed = true; base.Dispose(disposing); }
    public override void Flush() { Check(); }
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
}
