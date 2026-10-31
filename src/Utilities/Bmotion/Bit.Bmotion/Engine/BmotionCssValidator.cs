using Microsoft.Extensions.Logging;

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
    // vectors for breaking out of an inline-style declaration. Backslash is rejected outright:
    // CSS escape sequences (e.g. "\3C" for '<', "\6A avascript:") would otherwise smuggle any
    // forbidden character or sequence past the substring checks below.
    private static readonly char[] _forbiddenChars = { ';', '{', '}', '<', '>', '\\' };

    // Case-insensitive substrings that indicate a scripting / structural injection attempt.
    private static readonly string[] _forbiddenSequences = { "javascript:", "expression(", "/*", "*/", "</", "@import" };

    /// <summary>
    /// Returns <c>true</c> when the value is safe to write into inline style. A <c>null</c> value is
    /// treated as safe (nothing is written). CSS custom-property values are subject to the same rules.
    /// </summary>
    public static bool IsSafe(string? value)
    {
        if (string.IsNullOrEmpty(value)) return true;

        foreach (var c in value)
            if (char.IsControl(c) || Array.IndexOf(_forbiddenChars, c) >= 0) return false;

        foreach (var seq in _forbiddenSequences)
            if (value.Contains(seq, StringComparison.OrdinalIgnoreCase)) return false;

        return true;
    }

    // How many rejections a warned set remembers. A target whose value keeps changing (bound to input) would otherwise
    // hold every value it ever had; past this the set starts over, at worst reporting a value again.
    private const int MaxWarned = 64;

    /// <summary>
    /// Applies CSS safe mode (<see cref="BitBmotionOptions.CssSafeMode"/>) to every string-valued CSS declaration
    /// <paramref name="props"/> carries: nothing when it is Off, a warning per rejected value (which is still written)
    /// when Warn, an exception for the first rejected value when Throw.
    /// </summary>
    /// <param name="target">What the warning names: the element's id, or the selector that picks the elements.</param>
    /// <param name="warned">
    /// The rejections already reported for this target, so a value checked again on every render is reported once
    /// rather than once per check; <c>null</c> reports every rejection.
    /// </param>
    public static void Enforce(BmProps props, BmCssSafeMode mode, ILogger? logger, string? target, HashSet<string>? warned = null)
    {
        if (mode == BmCssSafeMode.Off) return;
        foreach (var (prop, value) in props.EnumerateCssStringValues())
        {
            if (IsSafe(value)) continue;
            if (mode == BmCssSafeMode.Throw)
                throw new InvalidOperationException(
                    $"Bit.Bmotion CSS safe mode rejected the value for '{prop}': \"{value}\". " +
                    "It contains characters/sequences that could break out of the inline style.");
            if (warned is not null)
            {
                var key = $"{prop}\n{value}";
                if (warned.Contains(key)) continue;
                if (warned.Count >= MaxWarned) warned.Clear();
                warned.Add(key);
            }
            logger?.LogWarning(
                "Bit.Bmotion CSS safe mode: rejected value for '{Prop}' on element '{Id}': {Value}",
                prop, target, value);
        }
    }
}
