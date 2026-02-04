using System.Text.Json.Serialization;

namespace Bit.BlazorUI;

/// <summary>
/// Represents metadata and validation information for a file selected through <see cref="BitFileInput"/>.
/// </summary>
public class BitFileInputInfo
{
    /// <summary>
    /// Gets or sets the MIME content type of the selected file (e.g., "image/png", "application/pdf").
    /// </summary>
    [JsonPropertyName("type")] public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the selected file, including its extension.
    /// </summary>
    [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the size of the selected file in bytes.
    /// </summary>
    [JsonPropertyName("size")] public long Size { get; set; }

    /// <summary>
    /// Gets or sets the unique identifier (GUID) assigned to the selected file.
    /// </summary>
    [JsonPropertyName("fileId")] public string FileId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the zero-based index of the file in the selection list.
    /// </summary>
    [JsonPropertyName("index")] public int Index { get; set; }

    /// <summary>
    /// Gets or sets the validation error message if the file failed validation.
    /// This property is null when the file is valid.
    /// </summary>
    [JsonIgnore] public string? Message { get; internal set; }

    /// <summary>
    /// Gets or sets a value indicating whether the file passed all validation checks
    /// (size constraints and allowed extensions).
    /// </summary>
    [JsonIgnore] public bool IsValid { get; internal set; } = true;
}
