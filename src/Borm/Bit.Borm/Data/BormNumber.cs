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
            case float f: result = f; return true;
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
    /// Everything that fits goes through <see cref="decimal"/>, which holds every integer type exactly
    /// and reads a <see cref="double"/> at the fifteen significant digits it can be trusted to — the
    /// precision at which <c>0.1</c> is still <c>0.1</c>. Trailing zeros are then dropped, because
    /// <c>1.0m</c> and <c>1m</c> are one number that decimal merely prints two ways.
    /// </remarks>
    public static string? Canonical(object? value)
    {
        decimal m;
        switch (value)
        {
            case decimal dm: m = dm; break;
            case int i: m = i; break;
            case long l: m = l; break;
            case short s: m = s; break;
            case byte b: m = b; break;
            case sbyte sb: m = sb; break;
            case ushort us: m = us; break;
            case uint ui: m = ui; break;
            case ulong ul: m = ul; break;
            case double or float:
                var d = Convert.ToDouble(value, CultureInfo.InvariantCulture);
                if (!double.IsFinite(d) || Math.Abs(d) >= 7.9e28) return d.ToString("R", CultureInfo.InvariantCulture);
                m = (decimal)d;
                // Too small for decimal to hold: keep it apart from a genuine zero.
                if (m == 0 && d != 0) return d.ToString("R", CultureInfo.InvariantCulture);
                break;
            default:
                return null;
        }

        // Dividing by one with the maximum scale normalises the scale away: 1.0m becomes 1m.
        return (m / 1.0000000000000000000000000000m).ToString(CultureInfo.InvariantCulture);
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
