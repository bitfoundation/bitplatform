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

    /// <summary>
    /// Replaces <see langword="null"/> branch objects on a deserialized <see cref="BitTheme"/> so the
    /// graph matches a freshly-constructed <c>new BitTheme()</c>. Sparse JSON (the format produced
    /// by <see cref="Serialize"/>) omits empty nested objects, and <see cref="JsonSerializer"/>
    /// then leaves those branches null on the result; callers that walk the graph (the mapper,
    /// merge, derivation helpers) rely on every branch being non-null.
    /// </summary>
    /// <remarks>
    /// Previously this method walked the type via reflection. Reflection breaks under trimming /
    /// AOT (IL2070/IL2075/IL3050) unless the entire <see cref="BitTheme"/> graph is preserved by
    /// <c>[DynamicallyAccessedMembers]</c>, which is hard to keep correct as the model evolves.
    /// Walking the graph explicitly is verbose but trim-safe and removes the reflection
    /// suppression pragmas that previously hid genuine warnings.
    /// </remarks>
    private static void EnsureNestedObjects(BitTheme theme)
    {
        // Top-level branches.
        theme.Color ??= new BitThemeColors();
        theme.BoxShadow ??= new BitThemeBoxShadows();
        theme.Spacing ??= new BitThemeSpacings();
        theme.ZIndex ??= new BitThemeZIndices();
        theme.Shape ??= new BitThemeShapes();
        theme.Typography ??= new BitThemeTypography();
        theme.Motion ??= new BitThemeMotion();
        theme.Layout ??= new BitThemeLayout();

        // Color branch.
        var color = theme.Color;
        color.Primary ??= new BitThemeColorVariants();
        color.Secondary ??= new BitThemeColorVariants();
        color.Tertiary ??= new BitThemeColorVariants();
        color.Info ??= new BitThemeColorVariants();
        color.Success ??= new BitThemeColorVariants();
        color.Warning ??= new BitThemeColorVariants();
        color.SevereWarning ??= new BitThemeColorVariants();
        color.Error ??= new BitThemeColorVariants();
        color.Foreground ??= new BitThemeGeneralColorVariants();
        color.Background ??= new BitThemeBackgroundColorVariants();
        color.Border ??= new BitThemeGeneralColorVariants();
        color.Neutral ??= new BitThemeNeutralColorVariants();

        // Typography branch.
        var typography = theme.Typography;
        typography.H1 ??= new BitThemeTypographyVariants();
        typography.H2 ??= new BitThemeTypographyVariants();
        typography.H3 ??= new BitThemeTypographyVariants();
        typography.H4 ??= new BitThemeTypographyVariants();
        typography.H5 ??= new BitThemeTypographyVariants();
        typography.H6 ??= new BitThemeTypographyVariants();
        typography.Subtitle1 ??= new BitThemeTypographyVariants();
        typography.Subtitle2 ??= new BitThemeTypographyVariants();
        typography.Body1 ??= new BitThemeTypographyVariants();
        typography.Body2 ??= new BitThemeTypographyVariants();
        typography.Button ??= new BitThemeTypographyVariants();
        typography.Caption1 ??= new BitThemeTypographyVariants();
        typography.Caption2 ??= new BitThemeTypographyVariants();
        typography.Overline ??= new BitThemeTypographyVariants();
        typography.Inherit ??= new BitThemeTypographyVariants();

        // Layout branch.
        theme.Layout.Breakpoints ??= new BitThemeBreakpoints();
    }
}
