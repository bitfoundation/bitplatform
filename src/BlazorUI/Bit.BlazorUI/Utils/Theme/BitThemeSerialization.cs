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

    /// <summary>
    /// Normalizes <paramref name="theme"/> in place so every nested branch object is non-null,
    /// matching a freshly-constructed <c>new BitTheme()</c>. Returns the same instance for
    /// convenient chaining.
    /// </summary>
    /// <remarks>
    /// Exposed for the CSS-variable mapper and merge entry points (<see cref="BitThemeMapper"/>):
    /// those walk the graph (<c>theme.Color.Primary.Main</c>, <c>theme.Layout.Breakpoints.Xs</c>, …)
    /// and would throw <see cref="NullReferenceException"/> on a hand-constructed sparse theme
    /// (for example <c>new BitTheme { Color = null }</c> or <c>new BitThemeColors { Primary = null }</c>,
    /// both reachable because the branch setters are public). The deserialization path already runs
    /// this; running it at the mapper/merge entry covers the common hand-constructed path too.
    /// </remarks>
    internal static BitTheme Normalize(BitTheme theme)
    {
        ArgumentNullException.ThrowIfNull(theme);

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
    /// Replaces <see langword="null"/> branch objects on a <see cref="BitTheme"/> so the
    /// graph matches a freshly-constructed <c>new BitTheme()</c>. Callers that walk the graph (the
    /// mapper, merge, derivation helpers) rely on every branch being non-null.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is a safety net for <em>externally-authored</em> JSON. The sparse format produced by
    /// <see cref="Serialize"/> never emits a branch as explicit <c>null</c> (null leaves are omitted
    /// and empty objects are pruned), and <see cref="JsonSerializer"/> leaves <em>absent</em>
    /// properties at their constructor-initialized <c>new()</c> values - so a framework round-trip
    /// already yields a fully-populated graph. The case this guards is hand-written or third-party
    /// JSON that contains an explicit <c>"color": null</c> (or similar), which the serializer would
    /// otherwise honor by setting the branch to <see langword="null"/>.
    /// </para>
    /// <para>
    /// Previously this method walked the type via reflection. Reflection breaks under trimming /
    /// AOT (IL2070/IL2075/IL3050) unless the entire <see cref="BitTheme"/> graph is preserved by
    /// <c>[DynamicallyAccessedMembers]</c>, which is hard to keep correct as the model evolves.
    /// Walking the graph explicitly is verbose but trim-safe and removes the reflection
    /// suppression pragmas that previously hid genuine warnings.
    /// </para>
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
        color.Semantic ??= new BitThemeSemanticColors();

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
        typography.Button ??= new BitThemeLabelTypographyVariants();
        typography.Caption1 ??= new BitThemeTypographyVariants();
        typography.Caption2 ??= new BitThemeTypographyVariants();
        typography.Overline ??= new BitThemeLabelTypographyVariants();
        typography.Inherit ??= new BitThemeInheritTypographyVariants();

        // Layout branch.
        theme.Layout.Breakpoints ??= new BitThemeBreakpoints();
    }
}
