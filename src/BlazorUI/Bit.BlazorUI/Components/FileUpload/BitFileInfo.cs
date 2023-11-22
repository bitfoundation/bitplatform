using System.Text.Json.Serialization;

namespace Bit.BlazorUI;

public class BitFileInfo
{
    [JsonPropertyName("type")] public string ContentType { get; set; } = string.Empty;
    [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
    [JsonPropertyName("size")] public long Size { get; set; }
    [JsonPropertyName("fileId")] public string FileId { get; set; } = string.Empty;
    [JsonPropertyName("index")] public int Index { get; set; }


    public long LastChunkUploadedSize { get; internal set; }
    public long TotalUploadedSize { get; internal set; }


    internal bool PauseUploadRequested { get; set; }
    internal bool CancelUploadRequested { get; set; }


    [JsonIgnore] public string? Message { get; internal set; }
    [JsonIgnore] public BitFileUploadStatus Status { get; internal set; }
    [JsonIgnore] internal DateTime? StartTimeUpload { get; set; }
}
