using Microsoft.AspNetCore.Components.Forms;

namespace Bit.Borm;

/// <summary>What a <see cref="BormFileUploader"/> is handed: the file, a way to read it that reports progress, and when to stop.</summary>
public sealed class BormUploadContext
{
    private readonly BormUpload _upload;

    internal BormUploadContext(BormUpload upload, BormState state, BormFieldDefinition field, string path, CancellationToken cancellationToken)
    {
        _upload = upload;
        State = state;
        Field = field;
        Path = path;
        CancellationToken = cancellationToken;
    }

    /// <summary>The file chosen.</summary>
    public IBrowserFile File => _upload.File;

    /// <summary>The form.</summary>
    public BormState State { get; }

    /// <summary>The field the file was chosen for.</summary>
    public BormFieldDefinition Field { get; }

    /// <summary>The path of that field.</summary>
    public string Path { get; }

    /// <summary>Cancelled when the user stops the upload, clears the field or leaves the form.</summary>
    public CancellationToken CancellationToken { get; }

    /// <summary>
    /// Opens the file for reading, counting what is read as sent. Read from it into the request, and the bar
    /// moves as the file goes. The limit is <paramref name="maxAllowedSize"/>, else the field's
    /// <see cref="BormFieldDefinition.MaxFileSize"/>, else 50 MB.
    /// </summary>
    public Stream OpenReadStream(long? maxAllowedSize = null)
        => new ProgressStream(File.OpenReadStream(maxAllowedSize ?? Field.MaxFileSize ?? 50L * 1024 * 1024, CancellationToken), _upload.Report);

    /// <summary>Reports progress for an uploader that reads the file some other way.</summary>
    public void Report(long bytesSent) => _upload.Report(bytesSent);

    private sealed class ProgressStream(Stream inner, Action<long> report) : Stream
    {
        private long _read;

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => inner.Length;

        public override long Position
        {
            get => _read;
            set => throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count) => Count(inner.Read(buffer, offset, count));

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            => Count(await inner.ReadAsync(buffer.AsMemory(offset, count), cancellationToken));

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
            => Count(await inner.ReadAsync(buffer, cancellationToken));

        private int Count(int read)
        {
            _read += read;
            report(_read);
            return read;
        }

        public override void Flush() { }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            if (disposing) inner.Dispose();
            base.Dispose(disposing);
        }

        public override async ValueTask DisposeAsync()
        {
            await inner.DisposeAsync();
            await base.DisposeAsync();
        }
    }
}
