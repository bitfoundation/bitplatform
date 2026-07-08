namespace Bit.Bmotion;

/// <summary>
/// A conservative validator for the string-valued CSS props Bit.Bmotion writes verbatim into an
/// element's inline <c>style</c> (colors, dimensions, box-shadow, filter, CSS custom properties).
/// It is a defence-in-depth guard for apps that bind <b>untrusted</b> input to these props; it is
/// intentionally strict and off by default (see <see cref="BmCssSafeMode"/>).
/// </summary>
internal static class BmotionCssValidator
{
    // Characters that never legitimately appear inside a single CSS *value* but are the primary
    // vectors for breaking out of an inline-style declaration.
    private static readonly char[] _forbiddenChars = { ';', '{', '}', '<', '>', '\n', '\r' };

    // Case-insensitive substrings that indicate a scripting / structural injection attempt.
    private static readonly string[] _forbiddenSequences = { "javascript:", "expression(", "/*", "*/", "</", "@import" };

    /// <summary>
    /// Returns <c>true</c> when the value is safe to write into inline style. A <c>null</c> value is
    /// treated as safe (nothing is written). CSS custom-property values are subject to the same rules.
    /// </summary>
    public static bool IsSafe(string? value)
    {
        if (string.IsNullOrEmpty(value)) return true;

        foreach (var c in _forbiddenChars)
            if (value.IndexOf(c) >= 0) return false;

        foreach (var seq in _forbiddenSequences)
            if (value.Contains(seq, StringComparison.OrdinalIgnoreCase)) return false;

        return true;
    }
}
