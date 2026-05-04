using System.Text.Json;
using System.Text.Json.Serialization;

namespace Bit.BlazorUI;

/// <summary>
/// Serialize and deserialize <see cref="BitTheme"/> for storage, admin UIs, or sharing brand tokens.
/// </summary>
public static class BitThemeSerialization
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public static string Serialize(BitTheme theme)
    {
        return JsonSerializer.Serialize(theme ?? new BitTheme(), Options);
    }

    public static BitTheme Deserialize(string json)
    {
        return string.IsNullOrWhiteSpace(json)
            ? new BitTheme()
            : (JsonSerializer.Deserialize<BitTheme>(json, Options) ?? new BitTheme());
    }
}
