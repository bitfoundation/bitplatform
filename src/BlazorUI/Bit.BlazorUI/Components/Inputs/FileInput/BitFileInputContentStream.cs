namespace Bit.BlazorUI;

/// <summary>
/// Wraps the stream handed out by <see cref="BitFileInput.OpenReadStreamAsync"/> so that the underlying
/// JavaScript stream reference lives exactly as long as the stream that reads through it.
/// Disposing the stream alone leaves the reference - and so the browser side File object it pins - alive
/// for the lifetime of the circuit, which is why the reference is disposed here rather than by the caller.
/// </summary>
internal sealed class BitFileInputContentStream(Stream inner, IJSStreamReference streamReference) : Stream
{
    private bool _disposed;

    public override bool CanRead => inner.CanRead;

    public override bool CanSeek => inner.CanSeek;

    public override bool CanWrite => false;

    public override long Length => inner.Length;

    public override long Position { get => inner.Position; set => inner.Position = value; }

    public override void Flush() => inner.Flush();

    public override Task FlushAsync(CancellationToken cancellationToken) => inner.FlushAsync(cancellationToken);

    public override int Read(byte[] buffer, int offset, int count) => inner.Read(buffer, offset, count);

    public override int Read(Span<byte> buffer) => inner.Read(buffer);

    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        => inner.ReadAsync(buffer, offset, count, cancellationToken);

    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        => inner.ReadAsync(buffer, cancellationToken);

    public override long Seek(long offset, SeekOrigin origin) => inner.Seek(offset, origin);

    public override void SetLength(long value) => throw new NotSupportedException();

    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    protected override void Dispose(bool disposing)
    {
        if (disposing && _disposed is false)
        {
            _disposed = true;

            inner.Dispose();

            // the reference is only disposable asynchronously, so a synchronous dispose can do no more than
            // start the release and let it finish on its own.
            _ = DisposeStreamReferenceAsync();
        }

        base.Dispose(disposing);
    }

    public override async ValueTask DisposeAsync()
    {
        if (_disposed is false)
        {
            _disposed = true;

            await inner.DisposeAsync();

            await DisposeStreamReferenceAsync();
        }

        await base.DisposeAsync();
    }

    private async ValueTask DisposeStreamReferenceAsync()
    {
        try
        {
            await streamReference.DisposeAsync();
        }
        catch (JSDisconnectedException) { } // the runtime is already gone, and the reference with it.
        catch (JSException) { } // the reference may already be released, which is not worth failing a dispose over.
    }
}
