using System.Text.Json.Serialization;

namespace Bit.BlazorUI;

public class BitFileInfo
{
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
    /// The size of the last uploaded chunk of the file.
    /// </summary>
    public long LastChunkUploadedSize { get; internal set; }

    /// <summary>
    /// The total uploaded size of the file.
    /// </summary>
    public long TotalUploadedSize { get; internal set; }


    internal bool PauseUploadRequested { get; set; }
    internal bool CancelUploadRequested { get; set; }

    /// <summary>
    /// The error message is issued during file validation before uploading the file or at the time of uploading.
    /// </summary>
    [JsonIgnore] public string? Message { get; internal set; }

    /// <summary>
    /// The status of the file in the BitFileUpload.
    /// </summary>
    [JsonIgnore] public BitFileUploadStatus Status { get; internal set; }

    /// <summary>
    /// The HTTP header at upload file.
    /// </summary>
    [JsonIgnore] public IReadOnlyDictionary<string, string>? HttpHeaders { get; set; }
    [JsonIgnore] internal DateTime? StartTimeUpload { get; set; }
}
