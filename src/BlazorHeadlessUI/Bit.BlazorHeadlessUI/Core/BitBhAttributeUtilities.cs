namespace Bit.BlazorHeadlessUI;

/// <summary>
/// Provides utilities for merging HTML attributes from internal component logic
/// with user-supplied additional attributes. Handles special merging rules for
/// class (space-concatenation) and style (semicolon-concatenation).
/// </summary>
public static class BitBhAttributeUtilities
{
    /// <summary>
    /// Merges <paramref name="additionalAttributes"/> over <paramref name="baseAttributes"/>.
    /// <c>class</c> values are space-joined, <c>style</c> values are semicolon-joined
    /// (consumer declarations last so they win), all other consumer values replace
    /// internal ones.
    /// </summary>
    public static Dictionary<string, object> Merge(
        Dictionary<string, object> baseAttributes,
        IReadOnlyDictionary<string, object>? additionalAttributes)
    {
        if (additionalAttributes is null or { Count: 0 })
            return baseAttributes;

        var merged = new Dictionary<string, object>(baseAttributes, StringComparer.OrdinalIgnoreCase);

        foreach (var (key, value) in additionalAttributes)
        {
            if (key.Equals("class", StringComparison.OrdinalIgnoreCase)
                && merged.TryGetValue("class", out var existingClass))
            {
                merged["class"] = JoinClasses(existingClass?.ToString(), value?.ToString());
            }
            else if (key.Equals("style", StringComparison.OrdinalIgnoreCase)
                     && merged.TryGetValue("style", out var existingStyle))
            {
                merged["style"] = JoinStyles(existingStyle?.ToString(), value?.ToString());
            }
            else if (key.Equals("aria-describedby", StringComparison.OrdinalIgnoreCase)
                     && merged.TryGetValue("aria-describedby", out var existingDescription))
            {
                // Descriptions add up: the consumer's own description never drops the one a part
                // wires in (a tooltip, a field's description or error message), nor the other way round.
                var ids = $"{value} {existingDescription}".Split(' ', StringSplitOptions.RemoveEmptyEntries);
                merged["aria-describedby"] = string.Join(' ', ids.Distinct(StringComparer.Ordinal));
            }
            else
            {
                merged[key] = value!;
            }
        }

        return merged;
    }

    /// <summary>Joins two class lists, ignoring empty values.</summary>
    public static string JoinClasses(string? a, string? b)
    {
        if (string.IsNullOrWhiteSpace(a)) return b?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(b)) return a.Trim();
        return $"{a.Trim()} {b.Trim()}";
    }

    /// <summary>Joins two inline style declarations, ignoring empty values.</summary>
    public static string JoinStyles(string? a, string? b)
    {
        var left = a?.Trim().TrimEnd(';');
        var right = b?.Trim().TrimEnd(';');
        if (string.IsNullOrEmpty(left)) return right ?? string.Empty;
        if (string.IsNullOrEmpty(right)) return left;
        return $"{left};{right}";
    }

    /// <summary>Sets <c>data-{name}="{value}"</c>.</summary>
    public static void SetDataAttribute(
        Dictionary<string, object> attributes,
        string name,
        string value)
    {
        attributes[$"data-{name}"] = value;
    }

    /// <summary>Adds the presence attribute <c>data-{name}</c> when <paramref name="condition"/> is true.</summary>
    public static void SetDataAttribute(
        Dictionary<string, object> attributes,
        string name,
        bool condition)
    {
        if (condition)
            attributes[$"data-{name}"] = "";
    }

    /// <summary>Appends space-separated id references to an IDREFS attribute (aria-describedby, …).</summary>
    public static void AppendIdRefs(Dictionary<string, object> attributes, string name, params string?[] ids)
    {
        var existing = attributes.TryGetValue(name, out var v) ? v?.ToString() : null;
        var tokens = new List<string>();
        if (!string.IsNullOrWhiteSpace(existing))
            tokens.AddRange(existing.Split(' ', StringSplitOptions.RemoveEmptyEntries));
        foreach (var id in ids)
        {
            if (!string.IsNullOrWhiteSpace(id) && !tokens.Contains(id))
                tokens.Add(id);
        }
        if (tokens.Count > 0)
            attributes[name] = string.Join(' ', tokens);
    }
}
