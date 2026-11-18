namespace Bit.Balette;

internal static class BaletteCommandIdGenerator
{
    public static string FromTitle(string title, string? group, string? parent)
    {
        var chars = title.ToLowerInvariant().Select(c => char.IsLetterOrDigit(c) ? c : '-').ToArray();
        var slug = new string(chars).Trim('-');
        while (slug.Contains("--", StringComparison.Ordinal))
        {
            slug = slug.Replace("--", "-", StringComparison.Ordinal);
        }

        // A title of symbols only ("🚀", "+", "→") has no letter to slug: its code points stand in, so two such titles
        // in one group do not both derive the bare group and replace each other.
        if (slug.Length == 0 && !string.IsNullOrWhiteSpace(title))
        {
            slug = "u" + string.Join('-', title.Trim().EnumerateRunes().Select(r => r.Value.ToString("x", System.Globalization.CultureInfo.InvariantCulture)));
        }

        var prefixParts = new List<string>(2);
        if (!string.IsNullOrWhiteSpace(parent))
        {
            prefixParts.Add(parent.ToLowerInvariant());
        }

        if (!string.IsNullOrWhiteSpace(group))
        {
            prefixParts.Add(group.ToLowerInvariant().Replace(' ', '-'));
        }

        return prefixParts.Count == 0 ? slug : string.Join('.', prefixParts) + "." + slug;
    }
}
