using System.Globalization;

namespace Bit.Borm;

/// <summary>
/// Culture-safe numeric coercion shared by conditions, range validation and input attributes.
/// Boxed numeric types are widened directly rather than round-tripped through
/// <see cref="object.ToString()"/>, which would otherwise format with the ambient culture and then be
/// re-parsed as invariant — turning <c>1.5</c> into <c>15</c> under cultures that use a comma separator.
/// </summary>
internal static class BormNumber
{
    // 2^96, the first magnitude decimal cannot hold. A double states it exactly.
    private const double DecimalLimit = 79228162514264337593543950336d;

    // 2^53: every whole double below it is an integer a long holds exactly.
    private const double ExactIntegerLimit = 9007199254740992d;

    /// <summary>Attempts to widen <paramref name="value"/> to a <see cref="double"/>.</summary>
    /// <remarks>
    /// Text is read as invariant first and in the ambient culture second, but the invariant pass does
    /// not accept a thousands separator. With one, <c>"1,5"</c> typed under a culture that writes a
    /// decimal comma read as fifteen — the invariant culture took the comma for grouping and the
    /// culture that actually meant one and a half never got asked.
    /// </remarks>
    public static bool TryToDouble(object? value, out double result)
    {
        switch (value)
        {
            case null:
                result = 0;
                return false;
            case double d: result = d; return true;
            case float f: result = Widen(f); return true;
            case decimal m: result = (double)m; return true;
            case int i: result = i; return true;
            case long l: result = l; return true;
            case short s: result = s; return true;
            case byte b: result = b; return true;
            case sbyte sb: result = sb; return true;
            case ushort us: result = us; return true;
            case uint ui: result = ui; return true;
            case ulong ul: result = ul; return true;
            case bool:
                result = 0;
                return false;
            case string str:
                return double.TryParse(str, NumberStyles.Float, CultureInfo.InvariantCulture, out result)
                    || double.TryParse(str, NumberStyles.Any, CultureInfo.CurrentCulture, out result);
            default:
                result = 0;
                return false;
        }
    }

    /// <summary>Whether <paramref name="value"/> is a boxed CLR number (not a bool, char, enum or text).</summary>
    public static bool IsNumber(object? value) => value is double or float or decimal or int or long or short
        or byte or sbyte or ushort or uint or ulong;

    /// <summary>
    /// A text form of a number that is the same for every CLR type holding the same value, so
    /// <c>1</c>, <c>1L</c>, <c>1.0</c> and <c>1.0m</c> all read as <c>"1"</c>. Null when
    /// <paramref name="value"/> is not a number.
    /// </summary>
    /// <remarks>
    /// Everything that fits goes through <see cref="TryToDecimal"/>, so a <see cref="double"/> reads as
    /// the number it prints as and <c>0.1</c> is still <c>0.1</c>. Trailing zeros are then dropped,
    /// because <c>1.0m</c> and <c>1m</c> are one number that decimal merely prints two ways.
    /// </remarks>
    public static string? Canonical(object? value)
    {
        if (TryToDecimal(value, out var m))
            // Dividing by one with the maximum scale normalises the scale away: 1.0m becomes 1m.
            return (m / 1.0000000000000000000000000000m).ToString(CultureInfo.InvariantCulture);

        // NaN, infinity, or beyond decimal's range or resolution: the value's own round-trip text, which
        // also keeps one too small for decimal to hold apart from a genuine zero.
        return value is double or float && TryToDouble(value, out var d) ? d.ToString("R", CultureInfo.InvariantCulture) : null;
    }

    /// <summary>
    /// Reads a boxed CLR number as the <see cref="decimal"/> it is written as: a decimal as it is, every
    /// integer type exactly, and a <see cref="double"/> or <see cref="float"/> as the shortest decimal
    /// that reads back as the same value — what it prints as, so <c>0.3</c> is <c>0.3m</c> and
    /// <c>0.1f</c> is <c>0.1m</c> on every runtime. False for text, for NaN or infinity, beyond decimal's
    /// range, and for a non-zero value too small for decimal to hold, which would otherwise read as zero.
    /// </summary>
    /// <remarks>
    /// The runtime's own conversion is not that. Up to .NET 10 it rounded a double to fifteen significant
    /// digits, which made distinct doubles one decimal (<c>1000000000000001.0</c> and
    /// <c>1000000000000002.0</c> both read as <c>1000000000000000</c>), and a float to seven
    /// (<c>16777215f</c> read as <c>16777220</c>). From .NET 11 it keeps the double's exact binary value,
    /// and <c>(decimal)0.3</c> is <c>0.2999999999999999888977697537</c> — a different number from the one
    /// written, so 0.3 stopped being a multiple of 0.1.
    /// </remarks>
    public static bool TryToDecimal(object? value, out decimal result)
    {
        switch (value)
        {
            case decimal m: result = m; return true;
            case int i: result = i; return true;
            case long l: result = l; return true;
            case short s: result = s; return true;
            case byte b: result = b; return true;
            case sbyte sb: result = sb; return true;
            case ushort us: result = us; return true;
            case uint ui: result = ui; return true;
            case ulong ul: result = ul; return true;
            // A whole double below 2^53 is an exact integer; only a fraction needs reading as text.
            case double d when double.IsInteger(d) && Math.Abs(d) < ExactIntegerLimit: result = (long)d; return true;
            case double d: return TryParseDecimal(d, "R", out result);
            case float f: return TryParseDecimal(Widen(f), "R", out result);
            default:
                result = 0;
                return false;
        }
    }

    /// <summary>
    /// Reads a <see cref="double"/> as a <see cref="decimal"/> at the fifteen significant digits every
    /// double can vouch for, which drops the noise binary arithmetic leaves past them:
    /// <c>0.1 + 0.2</c> is <c>0.30000000000000004</c>, and reads here as <c>0.3m</c>. False where
    /// <see cref="TryToDecimal"/> is.
    /// </summary>
    public static bool TryToDecimalAtFifteenDigits(double value, out decimal result)
        => TryParseDecimal(value, "G15", out result);

    /// <summary>
    /// Widens a <see cref="float"/> to the <see cref="double"/> of the number it prints as. A plain
    /// widening keeps the float's binary noise, so <c>0.1f</c> became <c>0.10000000149011612</c> and
    /// failed a maximum of <c>0.1</c>.
    /// </summary>
    public static double Widen(float value)
    {
        if (!float.IsFinite(value)) return value;

        Span<char> text = stackalloc char[16];
        value.TryFormat(text, out var length, "R", CultureInfo.InvariantCulture);
        return double.Parse(text[..length], NumberStyles.Float, CultureInfo.InvariantCulture);
    }

    private static bool TryParseDecimal(double value, string format, out decimal result)
    {
        result = 0;
        if (!double.IsFinite(value) || Math.Abs(value) >= DecimalLimit) return false;

        Span<char> text = stackalloc char[32];
        value.TryFormat(text, out var length, format, CultureInfo.InvariantCulture);
        var parsed = decimal.Parse(text[..length], NumberStyles.Float, CultureInfo.InvariantCulture);
        if (parsed != 0)
        {
            result = parsed;
            return true;
        }

        // A plain zero: parsing "1E-30" or "-0" leaves a zero carrying a sign or a scale of 28, which
        // shows wherever the decimal is printed. A non-zero value that rounds to it is no zero at all.
        return value == 0;
    }

    /// <summary>
    /// Widens a date, time or timestamp to the same OLE automation number the schema stores bounds as,
    /// so a date window can be checked by the one numeric range rule the rest of the library uses.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A time of day is the fraction of a day it represents — which is precisely its OLE automation
    /// value on day zero, so <c>09:00</c> compares equal whether it arrived as a <see cref="TimeOnly"/>,
    /// a <see cref="TimeSpan"/> or the string <c>"09:00"</c>. Parsing tries a time before a date for the
    /// same reason: <c>DateTime.Parse("09:00")</c> silently attaches <em>today</em> to it, which would
    /// put the bound and the value hundreds of thousands apart.
    /// </para>
    /// <para>
    /// A timestamp is judged by its own clock time — the date and time it states, which is what its
    /// input shows (see <see cref="BormValueConverter.ToInputString"/>) — not by the server's
    /// zone, which would move a date window by however far the server happens to be from the value.
    /// </para>
    /// </remarks>
    public static bool TryToOADate(object? value, out double result)
    {
        switch (value)
        {
            case DateTime dt: result = ToOADate(dt); return true;
            case DateTimeOffset dto: result = ToOADate(dto.DateTime); return true;
            case DateOnly d: result = ToOADate(d.ToDateTime(TimeOnly.MinValue)); return true;
            case TimeOnly t: result = t.ToTimeSpan().TotalDays; return true;
            case TimeSpan ts: result = ts.TotalDays; return true;
            // A month or a week a picker wrote as text stands for its first day, as its date-typed form does.
            case string s when BormPeriod.TryParseAny(s, out var period):
                result = ToOADate(period);
                return true;
            case string s:
                if (TimeOnly.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out var time))
                {
                    result = time.ToTimeSpan().TotalDays;
                    return true;
                }
                if (DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
                {
                    result = ToOADate(parsed);
                    return true;
                }
                result = 0;
                return false;
            default:
                result = 0;
                return false;
        }
    }

    /// <summary>
    /// <see cref="DateTime.ToOADate"/>, minus its exception. The OLE automation scale starts in the
    /// year 100, and anything earlier made <c>ToOADate</c> throw — out of a validation pass, on a date
    /// a user can type into any date input (<c>0099-05-01</c>, or the first digits of a year typed one
    /// at a time). Such a date lies before every bound the scale can express, so it is placed below all
    /// of them: a minimum rejects it and a maximum lets it through, exactly as for any other early date.
    /// </summary>
    public static double ToOADate(DateTime value)
    {
        try
        {
            return value.ToOADate();
        }
        catch (OverflowException)
        {
            return double.NegativeInfinity;
        }
    }

    /// <summary>Formats a number for an HTML attribute — always invariant, so <c>1.5</c> never becomes <c>1,5</c>.</summary>
    public static string? ToAttribute(double? value)
        => value?.ToString("R", CultureInfo.InvariantCulture);
}
