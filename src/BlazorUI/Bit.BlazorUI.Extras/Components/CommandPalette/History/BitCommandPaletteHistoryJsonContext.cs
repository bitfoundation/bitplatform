using System.Text.Json.Serialization;

namespace Bit.BlazorUI;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(Dictionary<string, BitCommandPaletteCommandUsage>))]
internal sealed partial class BitCommandPaletteHistoryJsonContext : JsonSerializerContext
{
}
