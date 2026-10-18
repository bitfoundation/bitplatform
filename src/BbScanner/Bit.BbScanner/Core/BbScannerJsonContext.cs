using System.Text.Json.Serialization;

namespace Bit.BbScanner;

/// <summary>Source generated JSON contracts, so the library works with trimming and AOT.</summary>
[JsonSerializable(typeof(BbCameraInfo))]
[JsonSerializable(typeof(BbCameraDevice[]))]
[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
internal sealed partial class BbScannerJsonContext : JsonSerializerContext;
