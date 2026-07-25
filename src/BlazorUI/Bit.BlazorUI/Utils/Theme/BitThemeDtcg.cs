using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace Bit.BlazorUI;

/// <summary>
/// Bridges <see cref="BitTheme"/> to and from the W3C Design Tokens Community Group (DTCG) JSON
/// format, so design-tooling that speaks DTCG - Tokens Studio (Figma), Style Dictionary v4+,
/// and the broader token pipeline - can round-trip a bit theme.
/// </summary>
/// <remarks>
/// <para>
/// The bridge uses bit's own group/token structure as the DTCG namespace (the same nesting
/// <see cref="BitThemeSerialization"/> emits: <c>color.primary.main</c>, <c>boxShadow.md</c>,
/// <c>motion.duration</c>, …) and the <b>string-value profile</b> of DTCG - each token is
/// <c>{ "$value": "…" }</c>. That profile is what the tooling ecosystem actually consumes, and it
/// is the only one that can carry bit's <c>var(--…)</c> references and composite shadow/font values,
/// which the DTCG 2025.10 object forms for <c>color</c>/<c>dimension</c> cannot express.
/// <see cref="Import"/> is lenient and additionally accepts those object forms (<c>{value,unit}</c>
/// dimensions, <c>{…,hex}</c> colors) and numeric <c>$value</c>s on the way in.
/// </para>
/// <para>
/// <b>Type annotations.</b> <see cref="Export"/> marks the uniformly-typed top-level groups with a
/// DTCG <c>$type</c> that descendants inherit (<c>color</c> → <c>color</c>, <c>boxShadow</c> →
/// <c>shadow</c>, <c>spacing</c> → <c>dimension</c>, <c>zIndex</c> → <c>number</c>), with the one
/// shadow-valued token inside the color group (<c>color.semantic.focusRing</c>) overriding to
/// <c>shadow</c>. Mixed groups (typography, shape, motion, layout) are emitted untyped, which is
/// valid DTCG.
/// </para>
/// <para>
/// <b>Aliases.</b> On import, DTCG aliases (<c>"{color.primary.main}"</c>) are resolved to their
/// concrete values - following chains, with cycle protection - so the result is always valid CSS.
/// A reference that cannot be resolved (dangling path or cycle) drops that one token, which then
/// falls back to the stylesheet default. Export writes concrete values, never aliases.
/// </para>
/// </remarks>
public static partial class BitThemeDtcg
{
    private const string ValueKey = "$value";
    private const string TypeKey = "$type";

    // A pure alias is a token value that is exactly "{dotted.path}"; embedded braces are left
    // untouched (and later screened out by the mapper), so only whole-string references resolve.
    [GeneratedRegex(@"^\{([^{}]+)\}$")]
    private static partial Regex AliasRegex();

    /// <summary>
    /// Exports <paramref name="theme"/> as a DTCG token document. Unset tokens are omitted (the
    /// document is as sparse as the theme), matching <see cref="BitThemeSerialization.Serialize"/>.
    /// </summary>
    public static string Export(BitTheme? theme, bool writeIndented = true)
    {
        // Reuse the hand-mirrored, trim-safe serializer for the model→JSON step, then transform the
        // resulting sparse tree into DTCG shape. This keeps the bridge automatically in sync with
        // the model (every token the serializer covers is covered here) without a second token map.
        var sparse = JsonNode.Parse(BitThemeSerialization.Serialize(theme));
        var dtcg = ToDtcgNode(sparse, path: string.Empty) ?? new JsonObject();
        return dtcg.ToJsonString(writeIndented ? new JsonSerializerOptions { WriteIndented = true } : null);
    }

    /// <summary>
    /// Imports a DTCG token document into a <see cref="BitTheme"/>. Tokens outside bit's group
    /// vocabulary are ignored; aliases are resolved to concrete values. Blank input yields an empty
    /// theme.
    /// </summary>
    /// <exception cref="ArgumentException"><paramref name="dtcgJson"/> is not valid JSON.</exception>
    public static BitTheme Import(string? dtcgJson)
    {
        if (string.IsNullOrWhiteSpace(dtcgJson)) return new BitTheme();

        JsonNode? root;
        try
        {
            root = JsonNode.Parse(dtcgJson);
        }
        catch (JsonException ex)
        {
            throw new ArgumentException("The DTCG document is not valid JSON.", nameof(dtcgJson), ex);
        }

        if (root is not JsonObject rootObject) return new BitTheme();

        var sparse = FromDtcgNode(rootObject, rootObject, new HashSet<string>(StringComparer.Ordinal)) as JsonObject
                     ?? new JsonObject();

        // Round-trip through the serializer so the result is a fully-normalized graph (no null
        // branches) exactly like any other deserialized theme.
        return BitThemeSerialization.Deserialize(sparse.ToJsonString());
    }

    // ── Export: bit sparse JSON → DTCG ────────────────────────────────────────

    private static JsonNode? ToDtcgNode(JsonNode? node, string path)
    {
        if (node is JsonObject obj)
        {
            var group = new JsonObject();

            // A $type on a group is inherited by every descendant token (nearest ancestor wins), so
            // it is set once on the uniformly-typed top-level branch rather than on each leaf.
            var groupType = TopLevelGroupType(path);
            if (groupType is not null)
            {
                group[TypeKey] = groupType;
            }

            foreach (var (key, child) in obj)
            {
                var childPath = path.Length == 0 ? key : $"{path}.{key}";
                var converted = ToDtcgNode(child, childPath);
                if (converted is not null)
                {
                    group[key] = converted;
                }
            }

            return group;
        }

        // Leaf: a bit token value is always a string in the model. Wrap it as a DTCG token object,
        // emitting a JSON *number* $value for number-typed tokens so the output is DTCG-conformant
        // (a `number` token's $value must be a JSON number, not a string).
        if (node is JsonValue)
        {
            var raw = node.GetValue<string>();
            var token = new JsonObject { [ValueKey] = BuildLeafValue(raw, path) };

            var leafType = LeafTypeOverride(path);
            if (leafType is not null)
            {
                token[TypeKey] = leafType;
            }

            return token;
        }

        return null;
    }

    // Builds a leaf's $value node. DTCG requires `number` tokens to carry a JSON number (not a
    // string), so a number-typed token whose value is a plain numeric literal is emitted as a
    // number; everything else (colors, dimensions, shadows, and any non-numeric value such as a
    // var() reference that slipped into a number slot) stays a string, preserving the documented
    // string-value profile that carries var() references and composites.
    private static JsonNode BuildLeafValue(string raw, string path)
    {
        if (EffectiveDtcgType(path) == "number")
        {
            var trimmed = raw.Trim();
            if (double.TryParse(trimmed, NumberStyles.Float, CultureInfo.InvariantCulture, out _))
            {
                // Parse rather than Create(double) so the exact literal is preserved (1300 stays the
                // integer 1300, not 1300.0). JsonNode.Parse rejects JSON-invalid numerics (e.g. a
                // leading '+'), in which case we fall through to the string form.
                try
                {
                    if (JsonNode.Parse(trimmed) is JsonValue parsed
                        && parsed.GetValueKind() == JsonValueKind.Number)
                    {
                        return parsed;
                    }
                }
                catch (JsonException)
                {
                    // Not a JSON-representable number; keep the string form below.
                }
            }
        }

        return JsonValue.Create(raw);
    }

    // The DTCG $type in effect for a leaf: an explicit leaf override wins, otherwise the type
    // inherited from its top-level group (matching what Export writes onto the group node).
    private static string? EffectiveDtcgType(string path)
    {
        var leafType = LeafTypeOverride(path);
        if (leafType is not null) return leafType;

        var topSegment = path.Split('.', 2)[0];
        return TopLevelGroupType(topSegment);
    }

    // The top-level branches whose entire subtree shares one DTCG type. Mixed branches (typography,
    // shape, motion, layout) return null and are emitted untyped.
    private static string? TopLevelGroupType(string path) => path switch
    {
        "color" => "color",
        "boxShadow" => "shadow",
        "spacing" => "dimension",
        "zIndex" => "number",
        _ => null,
    };

    // The lone non-color leaf living under the color group: its value is a box-shadow, so it
    // overrides the inherited color type.
    private static string? LeafTypeOverride(string path) => path switch
    {
        "color.semantic.focusRing" => "shadow",
        _ => null,
    };

    // ── Import: DTCG → bit sparse JSON ────────────────────────────────────────

    private static JsonNode? FromDtcgNode(JsonNode? node, JsonObject root, HashSet<string> resolving)
    {
        if (node is not JsonObject obj) return null;

        // A token: has $value. Resolve it to a concrete string leaf.
        if (obj.ContainsKey(ValueKey))
        {
            var resolved = ResolveValue(obj[ValueKey], root, resolving);
            return resolved is null ? null : JsonValue.Create(resolved);
        }

        // A group: recurse into non-metadata children.
        var group = new JsonObject();
        foreach (var (key, child) in obj)
        {
            if (key.StartsWith('$')) continue; // $type / $description / $extensions / …

            var converted = FromDtcgNode(child, root, resolving);
            if (converted is not null)
            {
                group[key] = converted;
            }
        }

        return group.Count == 0 ? null : group;
    }

    private static string? ResolveValue(JsonNode? valueNode, JsonObject root, HashSet<string> resolving)
    {
        if (valueNode is null) return null;

        switch (valueNode.GetValueKind())
        {
            case JsonValueKind.String:
                var text = valueNode.GetValue<string>();
                var alias = AliasRegex().Match(text);
                if (alias.Success is false) return text;
                return ResolveAlias(alias.Groups[1].Value, root, resolving);

            case JsonValueKind.Number:
                // Preserve the author's numeric formatting ("600", "1.5") verbatim.
                return valueNode.ToJsonString();

            case JsonValueKind.Object:
                return ResolveObjectValue(valueNode.AsObject());

            // Arrays (composite shadow lists, …), booleans, and nulls have no string mapping in
            // bit's model; skip so the token falls back to its stylesheet default.
            default:
                return null;
        }
    }

    // Accepts the DTCG 2025.10 object forms on import even though export never produces them.
    private static string? ResolveObjectValue(JsonObject value)
    {
        // Color object: prefer the hex convenience field when present.
        if (value.TryGetPropertyValue("hex", out var hex) && hex?.GetValueKind() == JsonValueKind.String)
        {
            return hex.GetValue<string>();
        }

        // Dimension object: { "value": 16, "unit": "px" } → "16px". Only number/string magnitudes
        // are meaningful; a bool/object/array "value" has no dimension mapping, so drop the token
        // (return null) rather than calling GetValue<string>() on it - which would throw
        // InvalidOperationException and break Import's documented lenient (never-throw) contract.
        if (value.TryGetPropertyValue("value", out var v) && v is not null
            && value.TryGetPropertyValue("unit", out var u) && u?.GetValueKind() == JsonValueKind.String)
        {
            var valueKind = v.GetValueKind();
            if (valueKind is not (JsonValueKind.Number or JsonValueKind.String)) return null;

            var number = valueKind == JsonValueKind.Number ? v.ToJsonString() : v.GetValue<string>();
            return $"{number}{u.GetValue<string>()}";
        }

        return null;
    }

    private static string? ResolveAlias(string path, JsonObject root, HashSet<string> resolving)
    {
        // Cycle guard: a → b → a would otherwise recurse forever.
        if (resolving.Add(path) is false) return null;

        try
        {
            var target = NavigateToToken(path, root);
            if (target is null || target.ContainsKey(ValueKey) is false) return null;

            return ResolveValue(target[ValueKey], root, resolving);
        }
        finally
        {
            resolving.Remove(path);
        }
    }

    private static JsonObject? NavigateToToken(string dottedPath, JsonObject root)
    {
        JsonObject current = root;
        foreach (var segment in dottedPath.Split('.'))
        {
            if (current[segment] is JsonObject next)
            {
                current = next;
            }
            else
            {
                return null;
            }
        }

        return current;
    }
}
