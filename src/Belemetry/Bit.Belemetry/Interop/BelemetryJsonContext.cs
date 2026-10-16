using System.Text.Json.Serialization;

namespace Bit.Belemetry;

/// <summary>
/// Source-generated JSON metadata for everything Belemetry serializes itself - the interop payloads and persisted
/// requests - so none of it depends on reflection (trimming and AOT safe).
/// </summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(BelemetryBrowserConfig))]
[JsonSerializable(typeof(BelemetryRelayBatch))]
[JsonSerializable(typeof(BelemetryJsContext))]
[JsonSerializable(typeof(BelemetryHttpRequest))]
[JsonSerializable(typeof(List<BelemetryHttpRequest>))]
[JsonSerializable(typeof(Dictionary<string, object?>))]
[JsonSerializable(typeof(BelemetryConsentRecord))]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(long))]
[JsonSerializable(typeof(double))]
[JsonSerializable(typeof(bool))]
[JsonSerializable(typeof(string[]))]
[JsonSerializable(typeof(long[]))]
[JsonSerializable(typeof(double[]))]
[JsonSerializable(typeof(bool[]))]
internal sealed partial class BelemetryJsonContext : JsonSerializerContext;
