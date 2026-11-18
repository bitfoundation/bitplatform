using System.Text.Json.Serialization;

namespace Bit.BbScanner;

/// <summary>Source generated JSON contracts, so the library works with trimming and AOT.</summary>
[JsonSerializable(typeof(BbScannerCameraInfo))]
[JsonSerializable(typeof(BbScannerCameraDevice[]))]
[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
internal sealed partial class BbScannerJsonContext : JsonSerializerContext;
