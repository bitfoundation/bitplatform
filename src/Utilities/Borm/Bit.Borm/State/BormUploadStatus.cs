namespace Bit.Borm;

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
