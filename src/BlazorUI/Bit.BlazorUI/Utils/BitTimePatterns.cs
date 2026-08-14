using System.Globalization;
using System.Text;

namespace Bit.BlazorUI;

/// <summary>
/// Builds the time-of-day format patterns the pickers display and parse with, out of the patterns
/// of a culture - so a picker in a 24-hour format shows the separators, the order and the designators
/// its culture writes times with, rather than a pattern hardcoded in the component.
/// </summary>
internal static class BitTimePatterns
{
    // The parts of a time of day that are written as a number, which are the ones a leading zero can be
    // missing from. The designator is not among them: 't' and 'tt' are two spellings of the meridiem, not
    // a narrow and a padded one.
    private static readonly char[] _numericSpecifiers = ['h', 'H', 'm', 's'];

    /// <summary>
    /// The pattern of a time of day in the given culture, rewritten into the requested clock format.
    /// </summary>
    /// <param name="culture">The culture whose short/long time patterns are the starting point.</param>
    /// <param name="format">The clock the pattern is rewritten into, 12 or 24 hours.</param>
    /// <param name="withSeconds">Whether the pattern includes the seconds (the long pattern of the culture).</param>
    /// <param name="padded">
    /// Whether every part of the time is written with a leading zero where it needs one. Off, the pattern is
    /// the one the culture writes a time with, which for many of them - en-US among them - leaves the leading
    /// zero off the hour. A pattern a time is <em>displayed</em> with is padded, so it lines up with the
    /// two-digit inputs a picker is operated with; a pattern a time is <em>parsed</em> with is not, since a
    /// padded specifier accepts nothing but a padded value while a narrow one accepts either.
    /// </param>
    public static string GetTimePattern(CultureInfo culture, BitTimeFormat format, bool withSeconds, bool padded = false)
    {
        var pattern = BuildTimePattern(culture, format, withSeconds);

        if (padded is false) return pattern;

        foreach (var specifier in _numericSpecifiers)
        {
            pattern = PadSpecifier(pattern, specifier);
        }

        return pattern;
    }

    private static string BuildTimePattern(CultureInfo culture, BitTimeFormat format, bool withSeconds)
    {
        var pattern = withSeconds
            ? culture.DateTimeFormat.LongTimePattern
            : culture.DateTimeFormat.ShortTimePattern;

        // A lowercase 'h' hour specifier (outside any quoted literal) indicates the culture uses a
        // 12-hour clock, an uppercase 'H' a 24-hour clock.
        var isCulture12Hours = HasSpecifier(pattern, 'h');

        if (format == BitTimeFormat.TwelveHours)
        {
            if (isCulture12Hours) return pattern;

            // Convert the culture's 24-hour pattern to 12-hour by switching the hour specifier
            // and appending the AM/PM designator.
            return $"{ReplaceSpecifier(pattern, 'H', 'h')} tt";
        }

        if (isCulture12Hours is false) return pattern;

        // Convert the culture's 12-hour pattern to 24-hour by switching the hour specifier
        // and removing the AM/PM ('t'/'tt') designator.
        return RemoveSpecifier(ReplaceSpecifier(pattern, 'h', 'H'), 't');
    }

    /// <summary>
    /// Determines whether the given format specifier appears outside of any quoted literal.
    /// </summary>
    private static bool HasSpecifier(string pattern, char specifier)
    {
        var quote = '\0';
        foreach (var ch in pattern)
        {
            if (quote != '\0')
            {
                if (ch == quote) quote = '\0';
                continue;
            }

            if (ch is '\'' or '"') { quote = ch; continue; }

            if (ch == specifier) return true;
        }

        return false;
    }

    /// <summary>
    /// Replaces the given format specifier with another, leaving quoted literals untouched.
    /// </summary>
    private static string ReplaceSpecifier(string pattern, char from, char to)
    {
        var builder = new StringBuilder(pattern.Length);
        var quote = '\0';
        foreach (var ch in pattern)
        {
            if (quote != '\0')
            {
                builder.Append(ch);
                if (ch == quote) quote = '\0';
                continue;
            }

            if (ch is '\'' or '"') { quote = ch; builder.Append(ch); continue; }

            builder.Append(ch == from ? to : ch);
        }

        return builder.ToString();
    }

    /// <summary>
    /// Doubles every single-character run of the given format specifier, so a part of the time that is written
    /// without a leading zero is written with one. A run that is already two characters or longer is left as it
    /// is, as are quoted literals.
    /// </summary>
    private static string PadSpecifier(string pattern, char specifier)
    {
        var builder = new StringBuilder(pattern.Length + 1);
        var quote = '\0';

        for (var i = 0; i < pattern.Length; i++)
        {
            var ch = pattern[i];

            if (quote != '\0')
            {
                builder.Append(ch);
                if (ch == quote) quote = '\0';
                continue;
            }

            if (ch is '\'' or '"') { quote = ch; builder.Append(ch); continue; }

            if (ch != specifier) { builder.Append(ch); continue; }

            var run = 1;
            while (i + run < pattern.Length && pattern[i + run] == specifier) run++;

            builder.Append(specifier, run == 1 ? 2 : run);

            i += run - 1;
        }

        return builder.ToString();
    }

    /// <summary>
    /// Removes the given format specifier (and any resulting redundant whitespace), leaving quoted literals untouched.
    /// </summary>
    private static string RemoveSpecifier(string pattern, char specifier)
    {
        var builder = new StringBuilder(pattern.Length);
        var quote = '\0';
        foreach (var ch in pattern)
        {
            if (quote != '\0')
            {
                builder.Append(ch);
                if (ch == quote) quote = '\0';
                continue;
            }

            if (ch is '\'' or '"') { quote = ch; builder.Append(ch); continue; }

            if (ch == specifier) continue;

            builder.Append(ch);
        }

        // Collapse any double spaces left behind by the removed designator and trim the edges.
        return builder.ToString().Replace("  ", " ").Trim();
    }
}
