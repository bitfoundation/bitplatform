using System.Text.Json.Serialization;

namespace Bit.Balette;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(Dictionary<string, BaletteCommandUsage>))]
internal sealed partial class BaletteHistoryJsonContext : JsonSerializerContext
{
}
