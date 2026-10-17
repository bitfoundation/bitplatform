using System.Text.Json;
using System.Text.Json.Serialization;

namespace Bit.Bonboarding;

/// <summary>
/// Source-generated serialization metadata for the persisted records, so the default store stays
/// trim and AOT safe instead of relying on reflection-based <see cref="JsonSerializer"/> calls.
/// </summary>
[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
[JsonSerializable(typeof(BonboardingRecord))]
internal sealed partial class BonboardingJsonContext : JsonSerializerContext;
