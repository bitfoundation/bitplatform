using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
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
        var node = JsonSerializer.SerializeToNode(theme ?? new BitTheme(), Options);
        if (node is JsonObject root)
            PruneEmptyObjects(root);
        return node?.ToJsonString(Options) ?? "{}";
    }

    public static BitTheme Deserialize(string json)
    {
        return string.IsNullOrWhiteSpace(json)
            ? new BitTheme()
            : (JsonSerializer.Deserialize<BitTheme>(json, Options) ?? new BitTheme());
    }

    // Recursively removes nested objects that have no token values set (all-null properties).
    // Works bottom-up: prune children first, then remove the child key if it became empty.
    private static void PruneEmptyObjects(JsonObject obj)
    {
        foreach (var key in obj.Select(p => p.Key).ToList())
        {
            if (obj[key] is JsonObject child)
            {
                PruneEmptyObjects(child);
                if (child.Count == 0)
                    obj.Remove(key);
            }
        }
    }
}
