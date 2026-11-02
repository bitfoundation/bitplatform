namespace Bit.Bdnd;

/// <summary>Reads the attributes a component was given, to add its own without overriding the application's.</summary>
internal static class BdndAttributes
{
    /// <summary>Whether <paramref name="attributes"/> sets <paramref name="name"/> (attribute names ignore case).</summary>
    public static bool Has(IReadOnlyDictionary<string, object>? attributes, string name) =>
        attributes?.Keys.Any(key => string.Equals(key, name, StringComparison.OrdinalIgnoreCase)) == true;

    /// <summary>
    /// The <c>aria-describedby</c> to render: the ids <paramref name="attributes"/> already give, followed by
    /// <paramref name="id"/>.
    /// </summary>
    public static string? DescribedBy(IReadOnlyDictionary<string, object>? attributes, string? id)
    {
        var own = attributes?
            .FirstOrDefault(a => string.Equals(a.Key, "aria-describedby", StringComparison.OrdinalIgnoreCase))
            .Value?.ToString();
        if (string.IsNullOrWhiteSpace(own)) return id;
        return id is null ? own : $"{own} {id}";
    }
}
