using System.Globalization;
using System.Text;

namespace Bit.Bheadless;

/// <summary>
/// Culture-aware number formatting and lenient parsing for number inputs: what a person types
/// ("1,234.5", "1 234,5", "$12", "15 %", "\u22123") is read the way the culture writes it, and a
/// value is shown with a standard .NET numeric format string ("N2", "C", "P0" …).
/// </summary>
public static class BitBhNumberParser
{
    /// <summary>Formats <paramref name="value"/> with <paramref name="format"/> in <paramref name="culture"/>.</summary>
    public static string Format(decimal value, string? format, CultureInfo culture)
        => string.IsNullOrEmpty(format) ? value.ToString("G29", culture) : value.ToString(format, culture);

    /// <summary>Whether <paramref name="format"/> is a percent format (<c>P</c> / <c>P2</c> …), whose text is 100 times the value.</summary>
    public static bool IsPercent(string? format)
        => format is { Length: > 0 } f && (f[0] is 'P' or 'p') && (f.Length == 1 || char.IsDigit(f[1]));

    /// <summary>
    /// Parses what was typed into a number field. Group separators, the currency symbol, the percent
    /// sign and white space are ignored; the culture's decimal separator is used, and a lone "." is taken
    /// as a decimal point too when the culture's group separator is not "."; the Unicode minus and
    /// parentheses (accounting negatives) are understood. Text in a percent format is divided by 100.
    /// </summary>
    /// <returns>True with the value, or false when the text is not a number. Empty text is false.</returns>
    public static bool TryParse(string? text, string? format, CultureInfo culture, out decimal value)
    {
        value = 0;
        if (string.IsNullOrWhiteSpace(text)) return false;

        var nf = culture.NumberFormat;
        var s = text.Trim();
        var negative = false;
        if (s.StartsWith('(') && s.EndsWith(')'))
        {
            negative = true;
            s = s[1..^1];
        }

        var builder = new StringBuilder(s.Length);
        var decimalSeen = false;
        for (var i = 0; i < s.Length; i++)
        {
            var c = s[i];
            if (char.IsDigit(c))
            {
                // Digits of other scripts (Arabic-Indic, Persian …) read as their values.
                builder.Append((char)('0' + (int)char.GetNumericValue(c)));
                continue;
            }
            if (Matches(s, i, nf.NumberDecimalSeparator) || Matches(s, i, nf.CurrencyDecimalSeparator) || Matches(s, i, nf.PercentDecimalSeparator))
            {
                if (decimalSeen) return false;
                decimalSeen = true;
                builder.Append('.');
                i += SeparatorLength(s, i, nf) - 1;
                continue;
            }
            if (c == '.' && nf.NumberGroupSeparator != "." && nf.CurrencyGroupSeparator != ".")
            {
                if (decimalSeen) return false;
                decimalSeen = true;
                builder.Append('.');
                continue;
            }
            if (c is '-' or '\u2212' || Matches(s, i, nf.NegativeSign))
            {
                // A sign leads the number, or trails it (cultures that write "5-").
                if (builder.Length > 0 && i != s.Length - 1) return false;
                negative = !negative;
                continue;
            }
            if (c == '+' || Matches(s, i, nf.PositiveSign)) continue;
            if (IsIgnorable(c, nf)) continue;
            if (Matches(s, i, nf.CurrencySymbol))
            {
                i += nf.CurrencySymbol.Length - 1;
                continue;
            }
            return false;
        }

        var digits = builder.ToString();
        if (digits.Length == 0 || digits == ".") return false;
        if (!decimal.TryParse(digits, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var parsed)) return false;
        if (negative) parsed = -parsed;
        if (IsPercent(format)) parsed /= 100;
        value = parsed;
        return true;
    }

    /// <summary>
    /// Whether <paramref name="text"/> could still become a number as more is typed ("", "-", "1.", "1,2").
    /// Used to keep what a person is typing instead of reverting it mid-edit.
    /// </summary>
    public static bool IsPartial(string? text, string? format, CultureInfo culture)
    {
        if (string.IsNullOrWhiteSpace(text)) return true;
        var trimmed = text.Trim();
        var nf = culture.NumberFormat;
        if (trimmed is "-" or "\u2212" or "+" or "(" || trimmed == nf.NegativeSign) return true;
        if (TryParse(text, format, culture, out _)) return true;
        // A trailing decimal separator or sign: "12." / "12,".
        foreach (var suffix in new[] { nf.NumberDecimalSeparator, ".", nf.NegativeSign })
            if (suffix.Length > 0 && trimmed.EndsWith(suffix, StringComparison.Ordinal) && TryParse(trimmed[..^suffix.Length], format, culture, out _))
                return true;
        return false;
    }

    /// <summary>The characters a number in <paramref name="culture"/> can be typed with (for input filtering).</summary>
    public static string AllowedCharacters(string? format, CultureInfo culture)
    {
        var nf = culture.NumberFormat;
        var set = new HashSet<char>("0123456789.,-+()\u2212 \u00A0\u202F'");
        foreach (var s in new[] { nf.NumberDecimalSeparator, nf.NumberGroupSeparator, nf.NegativeSign, nf.PositiveSign,
                                  nf.CurrencySymbol, nf.CurrencyDecimalSeparator, nf.CurrencyGroupSeparator, nf.PercentSymbol,
                                  nf.PercentDecimalSeparator, nf.PercentGroupSeparator })
            foreach (var c in s) set.Add(c);
        foreach (var d in nf.NativeDigits) foreach (var c in d) set.Add(c);
        return string.Concat(set.OrderBy(c => c));
    }

    private static bool IsIgnorable(char c, NumberFormatInfo nf)
        => char.IsWhiteSpace(c) || c is '\u00A0' or '\u202F' or '\'' or '%' or '\u066A'
           || nf.NumberGroupSeparator.Contains(c) || nf.CurrencyGroupSeparator.Contains(c) || nf.PercentGroupSeparator.Contains(c)
           || nf.PercentSymbol.Contains(c) || nf.PerMilleSymbol.Contains(c);

    private static bool Matches(string s, int index, string token)
        => token.Length > 0 && string.CompareOrdinal(s, index, token, 0, token.Length) == 0;

    private static int SeparatorLength(string s, int index, NumberFormatInfo nf)
    {
        foreach (var sep in new[] { nf.NumberDecimalSeparator, nf.CurrencyDecimalSeparator, nf.PercentDecimalSeparator })
            if (Matches(s, index, sep)) return sep.Length;
        return 1;
    }
}
