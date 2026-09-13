using System.Text.Json.Serialization;

namespace Bit.BlazorUI;

public class BitFileInfo
{
    // the bounds of DateTimeOffset itself in unix milliseconds, which a browser can well report a
    // timestamp outside of - a file whose modification time the file system never really had.
    private const long MIN_UNIX_MILLISECONDS = -62135596800000;
    private const long MAX_UNIX_MILLISECONDS = 253402300799999;



    /// <summary>
    /// The Content-Type of the selected file.
    /// </summary>
    [JsonPropertyName("type")] public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// The name of the selected file.
    /// </summary>
    [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The size of the selected file.
    /// </summary>
    [JsonPropertyName("size")] public long Size { get; set; }

    /// <summary>
    /// The file ID of the selected file, this is a GUID.
    /// </summary>
    [JsonPropertyName("fileId")] public string FileId { get; set; } = string.Empty;

    /// <summary>
    /// The index of the selected file.
    /// </summary>
    [JsonPropertyName("index")] public int Index { get; set; }

    /// <summary>
    /// The last modified time of the file reported by the browser, in milliseconds since the Unix epoch.
    /// </summary>
    [JsonPropertyName("lastModified")] public long LastModified { get; set; }

    /// <summary>
    /// An object URL of the file content that can be used as the source of an img element to preview image files.
    /// This is only populated for image files when the ShowPreview parameter of the BitFileUpload is enabled.
    /// </summary>
    [JsonPropertyName("previewUrl")] public string? PreviewUrl { get; set; }

    /// <summary>
    /// The width of the image in pixels, only populated for decodable image files when the
    /// ReadImageDimensions parameter of the BitFileUpload is enabled. It is null for anything else.
    /// </summary>
    [JsonPropertyName("width")] public int? Width { get; set; }

    /// <summary>
    /// The height of the image in pixels, only populated for decodable image files when the
    /// ReadImageDimensions parameter of the BitFileUpload is enabled. It is null for anything else.
    /// </summary>
    [JsonPropertyName("height")] public int? Height { get; set; }

    /// <summary>
    /// The last modified time of the file reported by the browser, as a DateTimeOffset. A timestamp the
    /// browser reports outside of the range of a DateTimeOffset is clamped to the closest end of it.
    /// </summary>
    [JsonIgnore]
    public DateTimeOffset LastModifiedDate => DateTimeOffset.FromUnixTimeMilliseconds(
        Math.Clamp(LastModified, MIN_UNIX_MILLISECONDS, MAX_UNIX_MILLISECONDS));

    /// <summary>
    /// The size of the last uploaded chunk of the file.
    /// </summary>
    public long LastChunkUploadedSize { get; internal set; }

    /// <summary>
    /// The total uploaded size of the file.
    /// </summary>
    public long TotalUploadedSize { get; internal set; }

    /// <summary>
    /// The observed speed of the upload of this file in bytes per second, measured over the request currently
    /// in flight. It is null while the file is not uploading and until the first progress report arrives.
    /// </summary>
    [JsonIgnore] public double? UploadSpeed { get; internal set; }

    /// <summary>
    /// The estimated time left before the upload of this file completes, derived from the
    /// <see cref="UploadSpeed"/> and the bytes still to be sent. It is null whenever the speed is unknown.
    /// </summary>
    [JsonIgnore] public TimeSpan? RemainingTime { get; internal set; }

    /// <summary>
    /// Whether the file is waiting in the upload queue for a free slot of the ConcurrentUploads limit,
    /// which is what tells a file that is about to start apart from one that was never asked to upload.
    /// </summary>
    [JsonIgnore] public bool IsQueued { get; internal set; }


    // Whether a request of this file is on the wire right now. A second request for the same file would
    // take over the connection of the first one and start it over from the beginning, so a repeated
    // upload call has to find out that this file is already busy and leave it alone.
    internal bool IsRequestInFlight { get; set; }

    // The moment the request currently in flight was sent and the byte count already uploaded back then,
    // which is what the upload speed of that request is measured against.
    internal DateTime? TransferStartTime { get; set; }
    internal long TransferStartOffset { get; set; }

    // The span of the in-flight upload request, remembered at send time since the dynamic chunk size
    // can change before the response of that request arrives.
    internal long PendingChunkSize { get; set; }

    // Whether this specific file is being removed from the server, driving its own spinner in the UI.
    internal bool IsRemoving { get; set; }

    // The number of automatic retries already spent on the upload of this file,
    // reset by a successful chunk and by a manual retry.
    internal int AutoRetryAttempts { get; set; }

    // The URL the queued upload of this file was requested with, remembered so that a file waiting for a
    // free slot still goes to the very endpoint the Upload call that queued it asked for.
    internal string? QueuedUploadUrl { get; set; }

    // Tracks whether the file was rejected by a list level rule (a duplicate, MaxCount or MaxTotalSize),
    // so it can be taken back once removals free up room or drop the original of a duplicate.
    internal bool ListValidationFailed { get; set; }

    /// <summary>
    /// The message attached to the current <see cref="Status"/> of the file: the reason it was rejected by
    /// the validations before the upload, or the body of the server response of its upload or removal.
    /// </summary>
    [JsonIgnore] public string? Message { get; internal set; }

    /// <summary>
    /// The status of the file in the BitFileUpload.
    /// </summary>
    [JsonIgnore] public BitFileUploadStatus Status { get; internal set; }

    /// <summary>
    /// The HTTP header at upload file.
    /// </summary>
    [JsonIgnore] public Dictionary<string, string>? HttpHeaders { get; set; }

    /// <summary>
    /// Additional multipart form fields sent alongside the content of this specific file in its upload
    /// requests, merged over the ones of the UploadRequestFormFields parameter of the BitFileUpload.
    /// The natural place to fill it in is the OnUploading callback.
    /// </summary>
    [JsonIgnore] public Dictionary<string, string>? FormFields { get; set; }

    [JsonIgnore] internal DateTime? StartTimeUpload { get; set; }
}
