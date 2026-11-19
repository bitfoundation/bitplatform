using System.Text.Json.Serialization;

namespace Bit.Bmage;

/// <summary>Dimensions reported by the browser after decoding.</summary>
public sealed class BmageBrowserImageInfo
{
    [JsonPropertyName("width")] public int Width { get; set; }
    [JsonPropertyName("height")] public int Height { get; set; }
    [JsonPropertyName("mimeType")] public string? MimeType { get; set; }
    [JsonPropertyName("byteLength")] public long ByteLength { get; set; }
}
