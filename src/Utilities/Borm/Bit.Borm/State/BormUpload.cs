using Microsoft.AspNetCore.Components.Forms;

namespace Bit.Borm;

/// <summary>
/// Sends one chosen file wherever the application keeps files and returns what the field stores in its place -
/// an id, a URL. See <see cref="BormFieldBuilder.UploadWith"/>.
/// </summary>
public delegate Task<string> BormFileUploader(BormUploadContext upload);

/// <summary>Where an upload stands.</summary>
public enum BormUploadStatus
{
    /// <summary>On its way.</summary>
    Uploading,

    /// <summary>Sent; <see cref="BormUpload.Result"/> is what the field stores.</summary>
    Done,

    /// <summary>The uploader threw; <see cref="BormUpload.Error"/> says why, and it can be tried again.</summary>
    Failed,

    /// <summary>Stopped by the user before it finished.</summary>
    Cancelled
}

/// <summary>One file being uploaded for a field: its progress, its outcome, and the means to stop it or try again.</summary>
public sealed class BormUpload
{
    private CancellationTokenSource? _cts;

    internal BormUpload(IBrowserFile file) => File = file;

    /// <summary>The file chosen.</summary>
    public IBrowserFile File { get; }

    /// <summary>The file's name.</summary>
    public string Name => File.Name;

    /// <summary>The file's size in bytes.</summary>
    public long Size => File.Size;

    /// <summary>How many bytes have been sent so far, as the uploader reported them.</summary>
    public long Sent { get; private set; }

    /// <summary>How far it has got, 0 to 100.</summary>
    public int Percent => Size <= 0 ? (Status == BormUploadStatus.Done ? 100 : 0) : (int)Math.Clamp(Sent * 100 / Size, 0, 100);

    /// <summary>Where it stands.</summary>
    public BormUploadStatus Status { get; private set; } = BormUploadStatus.Uploading;

    /// <summary>What the uploader returned - what the field stores for this file - once it is <see cref="BormUploadStatus.Done"/>.</summary>
    public string? Result { get; private set; }

    /// <summary>Why it failed, for the person who chose the file.</summary>
    public string? Error { get; private set; }

    /// <summary>What the uploader threw, for the application's own logging.</summary>
    public Exception? Exception { get; private set; }

    /// <summary>Raised whenever the upload moves on - a byte count, an outcome.</summary>
    public event Action? Changed;

    /// <summary>Stops the upload. The uploader is told through its cancellation token.</summary>
    public void Cancel() => _cts?.Cancel();

    internal CancellationToken Start()
    {
        _cts?.Dispose();
        _cts = new CancellationTokenSource();
        Sent = 0;
        Status = BormUploadStatus.Uploading;
        Error = null;
        Exception = null;
        Changed?.Invoke();
        return _cts.Token;
    }

    // Raised at most once per percent: a progress bar has a hundred places to be, and a re-render per buffer
    // of a large file would be thousands.
    internal void Report(long sent)
    {
        var before = Percent;
        Sent = Math.Clamp(sent, 0, Math.Max(Size, sent));
        if (Percent != before) Changed?.Invoke();
    }

    internal void Finish(string result)
    {
        Result = result;
        Sent = Size;
        Status = BormUploadStatus.Done;
        Changed?.Invoke();
    }

    internal void Fail(string error, Exception exception)
    {
        Error = error;
        Exception = exception;
        Status = BormUploadStatus.Failed;
        Changed?.Invoke();
    }

    internal void Stopped()
    {
        Status = BormUploadStatus.Cancelled;
        Changed?.Invoke();
    }
}

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
