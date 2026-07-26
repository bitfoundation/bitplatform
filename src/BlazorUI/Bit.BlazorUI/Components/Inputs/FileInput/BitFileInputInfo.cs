using System.Text.Json.Serialization;

namespace Bit.BlazorUI;

/// <summary>
/// Represents metadata, validation state, and content of a file selected through <see cref="BitFileInput"/>.
/// Provides access to file properties such as name, size, and content type, as well as the file's byte array content.
/// </summary>
public class BitFileInputInfo
{
    /// <summary>
    /// The MIME content type of the file (e.g., "image/png", "application/pdf").
    /// </summary>
    [JsonPropertyName("type")] public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// The name of the file including its extension (e.g., "document.pdf").
    /// </summary>
    [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The size of the file in bytes.
    /// </summary>
    [JsonPropertyName("size")] public long Size { get; set; }

    /// <summary>
    /// A unique identifier (GUID) assigned to the file upon selection, used to reference the file in JavaScript interop.
    /// </summary>
    [JsonPropertyName("fileId")] public string FileId { get; set; } = string.Empty;

    /// <summary>
    /// The zero-based index of the file in the current selection list.
    /// </summary>
    [JsonPropertyName("index")] public int Index { get; set; }

    /// <summary>
    /// The last modified time of the file reported by the browser, in milliseconds since the Unix epoch.
    /// </summary>
    [JsonPropertyName("lastModified")] public long LastModified { get; set; }

    /// <summary>
    /// An object URL of the file content that can be used as the source of an img element to preview image files.
    /// This is only populated for image files when the ShowPreview parameter of the BitFileInput is enabled.
    /// </summary>
    [JsonPropertyName("previewUrl")] public string? PreviewUrl { get; set; }

    /// <summary>
    /// The width of the image in pixels, only populated for decodable image files when the
    /// ReadImageDimensions parameter of the BitFileInput is enabled. It is null for anything else.
    /// </summary>
    [JsonPropertyName("width")] public int? Width { get; set; }

    /// <summary>
    /// The height of the image in pixels, only populated for decodable image files when the
    /// ReadImageDimensions parameter of the BitFileInput is enabled. It is null for anything else.
    /// </summary>
    [JsonPropertyName("height")] public int? Height { get; set; }

    /// <summary>
    /// The last modified time of the file reported by the browser, as a DateTimeOffset.
    /// </summary>
    [JsonIgnore] public DateTimeOffset LastModifiedDate => DateTimeOffset.FromUnixTimeMilliseconds(LastModified);

    /// <summary>
    /// The validation error message when the file has failed a validation check (e.g., size or extension).
    /// This is null when the file is valid.
    /// </summary>
    [JsonIgnore] public string? Message { get; internal set; }

    /// <summary>
    /// Whether the file has passed all validation checks including size constraints and allowed extensions.
    /// </summary>
    [JsonIgnore] public bool IsValid { get; internal set; } = true;

    /// <summary>
    /// The file content as a byte array, populated by calling <see cref="BitFileInput.ReadContentAsync(BitFileInputInfo)"/>.
    /// This is null by default and only loaded on demand to avoid unnecessary memory usage.
    /// </summary>
    [JsonIgnore] public byte[]? Content { get; internal set; }

    // Tracks whether the file was invalidated by a list level limit (MaxCount or MaxTotalSize),
    // so it can become valid again once removals free up room.
    [JsonIgnore] internal bool ListValidationFailed { get; set; }
}
