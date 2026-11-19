using Microsoft.AspNetCore.Components.Forms;

namespace Bit.Borm;

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
