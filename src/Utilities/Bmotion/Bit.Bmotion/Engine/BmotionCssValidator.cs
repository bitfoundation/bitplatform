using System.Globalization;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Bit.Bmotion;

/// <summary>
/// A conservative validator for the CSS declarations Bit.Bmotion writes verbatim into an element's inline
/// <c>style</c> (colors, dimensions, box-shadow, filter, the free-form <c>Css</c> entries, CSS custom properties): their
/// values, and the property names a free-form entry or a custom property brings. It is a defence-in-depth guard for
/// apps that bind <b>untrusted</b> input to these props; it is intentionally strict and off by default (see
/// <see cref="BmCssSafeMode"/>).
/// </summary>
internal static class BmotionCssValidator
{
    // Characters that never legitimately appear inside a single CSS *value* but are the primary
    // vectors for breaking out of an inline-style declaration. Backslash is rejected outright:
    // CSS escape sequences (e.g. "\3C" for '<', "\6A avascript:") would otherwise smuggle any
    // forbidden character or sequence past the substring checks below.
    private static readonly char[] _forbiddenChars = { ';', '{', '}', '<', '>', '\\' };

    // Case-insensitive substrings that indicate a scripting / structural injection attempt, or that make the browser
    // fetch a URL the value names - url() and src(), and the image functions that take a bare string as one (image(),
    // image-set() and its -webkit- form) - which would let an untrusted value send requests anywhere (tracking, CSS
    // data exfiltration). An SVG reference such as url(#blur) goes with them.
    private static readonly string[] _forbiddenSequences =
        { "javascript:", "expression(", "/*", "*/", "</", "@import", "url(", "src(", "image(", "image-set(" };

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

    /// <summary>
    /// Returns <c>true</c> when <paramref name="name"/> is safe to write as a property name: ASCII letters, digits,
    /// <c>-</c> and <c>_</c> only - every CSS property, in either case form, and every custom property anyone names -
    /// so nothing in it can end the declaration or start another.
    /// </summary>
    public static bool IsSafeName(string? name)
    {
        if (string.IsNullOrEmpty(name)) return false;

        foreach (var c in name)
            if (!char.IsAsciiLetterOrDigit(c) && c != '-' && c != '_') return false;

        return true;
    }

    // The most of an untrusted name or value a warning or an exception quotes.
    private const int MaxQuoted = 128;

    /// <summary>
    /// An untrusted name or value as a warning or an exception quotes it: control and other invisible formatting
    /// characters escaped (a raw newline would start a forged log line, a bidi override would disguise one), and cut to
    /// <see cref="MaxQuoted"/> characters, so a huge value isn't written to the log whole.
    /// </summary>
    internal static string Quote(string? text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;

        int length = Math.Min(text.Length, MaxQuoted);
        if (length < text.Length && char.IsHighSurrogate(text[length - 1])) length--; // don't split a surrogate pair
        var sb = new StringBuilder(length + 32);
        foreach (var c in text.AsSpan(0, length))
        {
            if (char.IsControl(c) || char.GetUnicodeCategory(c) is UnicodeCategory.Format or UnicodeCategory.LineSeparator or UnicodeCategory.ParagraphSeparator)
                sb.Append(CultureInfo.InvariantCulture, $"\\u{(int)c:x4}");
            else
                sb.Append(c);
        }
        if (length < text.Length) sb.Append(CultureInfo.InvariantCulture, $"... ({text.Length} characters)");
        return sb.ToString();
    }

    /// <summary>
    /// Applies CSS safe mode (<see cref="BitBmotionOptions.CssSafeMode"/>) to every string-valued CSS declaration
    /// <paramref name="props"/> carries - its name and its value: nothing when it is Off, a warning per rejected
    /// declaration (which is still written) when Warn, an exception for the first rejected one when Throw.
    /// <para>
    /// It checks the props as they are now. A caller that writes them later serializes them in the same synchronous
    /// step, so what is written is what was checked.
    /// </para>
    /// </summary>
    /// <param name="props">The target to check.</param>
    /// <param name="mode">The policy: <see cref="BitBmotionOptions.CssSafeMode"/>.</param>
    /// <param name="logger">Where Warn reports; <c>null</c> reports nowhere.</param>
    /// <param name="target">What the warning names: the element's id, or the selector that picks the elements.</param>
    /// <param name="warned">
    /// The rejections already reported for this target, so a declaration checked again on every render is reported
    /// once rather than once per check; <c>null</c> reports every rejection.
    /// </param>
    public static void Enforce(BmProps props, BmCssSafeMode mode, ILogger? logger, string? target, BmotionCssWarnings? warned = null)
    {
        if (mode == BmCssSafeMode.Off) return;
        foreach (var (prop, value) in props.EnumerateCssDeclarations())
        {
            var rejected = !IsSafeName(prop) ? "property name" : !IsSafe(value) ? "value" : null;
            if (rejected is null) continue;
            if (mode == BmCssSafeMode.Throw)
                throw new InvalidOperationException(
                    $"Bit.Bmotion CSS safe mode rejected the {rejected} of '{Quote(prop)}': \"{Quote(value)}\". " +
                    "It contains characters/sequences that could break out of the inline style or load a URL.");
            bool stopping = false;
            if (warned?.ShouldReport(target, prop, value, out stopping) == false)
            {
                if (stopping)
                    logger?.LogWarning(
                        "Bit.Bmotion CSS safe mode: '{Target}' has more than {Max} rejected declarations; further ones are not reported.",
                        Quote(target), BmotionCssWarnings.Max);
                continue;
            }
            logger?.LogWarning(
                "Bit.Bmotion CSS safe mode: rejected {Rejected} for '{Prop}' on '{Target}': {Value}",
                rejected, Quote(prop), Quote(target), Quote(value));
        }
    }
}
