using Microsoft.JSInterop;

namespace Bit.Bdnd;

/// <summary>The files of one native drop the browser module keeps for a target - until the target's next drop, or its disposal.</summary>
internal sealed class BdndNativeDropFiles(BdndInterop interop, string dropId)
{
    public BdndInterop Interop { get; } = interop;

    public string DropId { get; } = dropId;

    /// <summary>The module no longer keeps the files: the target took another drop, or was disposed.</summary>
    public bool Released { get; private set; }

    public void Release() => Released = true;
}

/// <summary>Reads a file of a native drop the browser module keeps, by the drop and the file's place in it.</summary>
internal sealed class BdndNativeFileSource(BdndNativeDropFiles drop, int index)
{
    public bool Released => drop.Released;

    public async Task<Stream> OpenReadStreamAsync(long maxAllowedSize, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var interop = drop.Interop;
        var dropId = drop.DropId;
        IJSStreamReference? reference;
        try
        {
            reference = await interop.ReadNativeFileAsync(dropId, index);
        }
        catch (JSException exception)
        {
            throw new InvalidOperationException(
                "The file is no longer available: a drop target keeps the files of its last drop, until the next drop on it or until it is disposed.",
                exception);
        }
        if (reference is null) throw new InvalidOperationException("The browser module handed out no file to read.");
        // the call itself takes no cancellation token: cancelled after the module handed the file out, the reference
        // would never be released, and the file would stay in the browser's memory
        if (cancellationToken.IsCancellationRequested)
        {
            await reference.DisposeAsync();
            cancellationToken.ThrowIfCancellationRequested();
        }
        try
        {
            return new OwningStream(await reference.OpenReadStreamAsync(maxAllowedSize, cancellationToken), reference);
        }
        catch
        {
            await reference.DisposeAsync();
            throw;
        }
    }

    // A stream that releases the JS stream reference it reads from once it is disposed.
    private sealed class OwningStream(Stream inner, IJSStreamReference reference) : Stream
    {
        public override bool CanRead => inner.CanRead;
        public override bool CanSeek => inner.CanSeek;
        public override bool CanWrite => false;
        public override long Length => inner.Length;

        public override long Position
        {
            get => inner.Position;
            set => inner.Position = value;
        }

        public override void Flush() => inner.Flush();
        public override int Read(byte[] buffer, int offset, int count) => inner.Read(buffer, offset, count);

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
            inner.ReadAsync(buffer, offset, count, cancellationToken);

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) =>
            inner.ReadAsync(buffer, cancellationToken);

        public override long Seek(long offset, SeekOrigin origin) => inner.Seek(offset, origin);
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                inner.Dispose();
                _ = reference.DisposeAsync().AsTask().ContinueWith(static t => _ = t.Exception, TaskScheduler.Default);
            }
            base.Dispose(disposing);
        }

        public override async ValueTask DisposeAsync()
        {
            await inner.DisposeAsync();
            try
            {
                await reference.DisposeAsync();
            }
            catch (Exception exception) when (exception is JSException or JSDisconnectedException or TaskCanceledException) { }
            await base.DisposeAsync();
        }
    }
}
