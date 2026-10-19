namespace Bit.Bonboarding;

/// <summary>
/// Matches a step's declared route against the current location. Deliberately tiny: routes are
/// compared as paths, optionally with a trailing <c>*</c> for prefix matching.
/// </summary>
public static class BonboardingRouteMatcher
{
    /// <summary>
    /// True when <paramref name="relativePath"/> satisfies <paramref name="pattern"/>. Query
    /// strings and fragments are ignored, comparison is case-insensitive, and a trailing
    /// <c>*</c> matches any deeper path.
    /// </summary>
    public static bool Matches(string? pattern, string? relativePath)
    {
        if (string.IsNullOrEmpty(pattern)) return true;

        var actual = Normalize(relativePath);

        if (pattern.EndsWith('*'))
        {
            var prefix = Normalize(pattern[..^1]);
            return prefix.Length == 0
                || actual.Equals(prefix, StringComparison.OrdinalIgnoreCase)
                || actual.StartsWith(prefix + "/", StringComparison.OrdinalIgnoreCase);
        }

        return actual.Equals(Normalize(pattern), StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Strips the query, the fragment, and any leading or trailing slashes.</summary>
    public static string Normalize(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return string.Empty;

        var value = path;

        var fragment = value.IndexOf('#');
        if (fragment >= 0) value = value[..fragment];

        var query = value.IndexOf('?');
        if (query >= 0) value = value[..query];

        return value.Trim('/');
    }
}
