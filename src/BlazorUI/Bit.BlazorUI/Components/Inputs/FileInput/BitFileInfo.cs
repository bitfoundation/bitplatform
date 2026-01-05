using System.Text.Json.Serialization;

namespace Bit.BlazorUI;

public class BitFileInputInfo
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
    /// The error message issued during file validation.
    /// </summary>
    [JsonIgnore] public string? Message { get; internal set; }

    /// <summary>
    /// Indicates whether the file is valid according to the specified constraints.
    /// </summary>
    [JsonIgnore] public bool IsValid { get; internal set; } = true;
}
