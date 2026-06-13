using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Bit.BlazorUI;

/// <summary>
/// Serialize and deserialize <see cref="BitTheme"/> for storage, admin UIs, or sharing brand tokens.
/// Serialization omits empty nested JSON objects so sparse themes contain fewer properties; deserialization restores
/// the usual eagerly-initialized graph so callers (for example <see cref="BitThemeUtilities.Merge"/>) never see null branches.
/// </summary>
public static class BitThemeSerialization
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private static readonly JsonSerializerOptions IndentedOptions = new(Options) { WriteIndented = true };
    private static readonly JsonSerializerOptions JsonNodeIndentedWriteOptions = new() { WriteIndented = true };

    public static string Serialize(BitTheme? theme, bool writeIndented = false)
    {
        var serializeOptions = writeIndented ? IndentedOptions : Options;
        var raw = JsonSerializer.Serialize(theme ?? new BitTheme(), serializeOptions);
        var node = JsonNode.Parse(raw);
        PruneEmptyObjectNodes(node);
        return node!.ToJsonString(writeIndented ? JsonNodeIndentedWriteOptions : null);
    }

    public static BitTheme Deserialize(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return new BitTheme();

        var theme = JsonSerializer.Deserialize<BitTheme>(json, Options) ?? new BitTheme();
        EnsureNestedObjects(theme);
        return theme;
    }

    /// <summary>Removes JSON object properties whose value is an empty object <c>{}</c>, depth-first, so parents collapse when all children were empty.</summary>
    private static void PruneEmptyObjectNodes(JsonNode? node)
    {
        if (node is JsonObject obj)
        {
            foreach (var key in obj.Select(p => p.Key).ToList())
            {
                var child = obj[key];
                PruneEmptyObjectNodes(child);
                if (obj[key] is JsonObject childObj && childObj.Count == 0)
                    obj.Remove(key);
            }
        }
        else if (node is JsonArray arr)
        {
            foreach (var item in arr)
                PruneEmptyObjectNodes(item);
        }
    }

    /// <summary>Replaces null reference-type properties (except <see cref="string"/>) so the tree matches a <c>new BitTheme()</c> graph after sparse JSON.</summary>
#pragma warning disable IL2072 // Activator.CreateInstance - BitTheme-related POCOs only, all have public parameterless constructors.
#pragma warning disable IL2075 // GetType().GetProperties - only instances from BitTheme deserialization.
    private static void EnsureNestedObjects(object obj, HashSet<object>? visited = null)
    {
        visited ??= new HashSet<object>(ReferenceEqualityComparer.Instance);
        if (!visited.Add(obj))
            return;

        foreach (var prop in obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!prop.CanRead || !prop.CanWrite || prop.GetIndexParameters().Length > 0)
                continue;

            var pt = prop.PropertyType;
            if (pt == typeof(string) || pt.IsValueType)
                continue;

            var val = prop.GetValue(obj);
            if (val is null)
            {
                val = Activator.CreateInstance(pt);
                if (val is null)
                    continue;
                prop.SetValue(obj, val);
            }

            EnsureNestedObjects(val, visited);
        }
    }
#pragma warning restore IL2075
#pragma warning restore IL2072
}
