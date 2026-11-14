using System.Globalization;
using System.Text;

namespace Bit.Bsqlite.Data;

/// <summary>
/// Reads a cell - <see langword="long"/>, <see langword="double"/>, <see langword="string"/> or a blob - as
/// another type the way SQLite converts between storage classes, which is what Microsoft.Data.Sqlite's
/// getters return, so code reads the same values in the browser as it does against SQLite on a server.
/// </summary>
internal static class BsqliteValueConversions
{
    /// <summary>The Julian day of 1970-01-01T00:00:00, SQLite's <c>julianday('1970-01-01')</c>.</summary>
    private const double UnixEpochJulianDay = 2440587.5;

    /// <summary>
    /// A cell read as an INTEGER, as <c>sqlite3_column_int64</c> - and so Microsoft.Data.Sqlite's
    /// <c>GetInt64</c> and <c>GetFieldValue&lt;long&gt;</c> - reads it. A REAL is truncated toward zero, as
    /// <c>CAST(x AS INTEGER)</c> does - not rounded, which <see cref="Convert"/> does - and TEXT is read by
    /// its leading integer, <c>0</c> when it has none, rather than refused; both saturate rather than wrap
    /// past the 64-bit range.
    /// </summary>
    public static long ToInt64(object value) => value switch
    {
        long integer => integer,
        double real => RealToInteger(real),
        string text => TextToInteger(text),
        bool flag => flag ? 1 : 0,
        decimal money => (long)decimal.Truncate(money),
        _ => Convert.ToInt64(value, CultureInfo.InvariantCulture),
    };

    /// <summary>
    /// A cell read as a REAL, as <c>sqlite3_column_double</c> - and so Microsoft.Data.Sqlite's
    /// <c>GetDouble</c> - reads it: TEXT by its leading number, <c>0</c> when it has none, rather than refused.
    /// </summary>
    public static double ColumnToDouble(object value)
        => value is string text ? TextToReal(text) : ToDouble(value);

    /// <summary>
    /// SQLite's <c>sqlite3Atoi64</c>: leading spaces, a sign, then as many digits as there are - <c>12abc</c>
    /// is 12, <c>1.9</c> is 1, <c>abc</c> is 0 - saturating past the 64-bit range.
    /// </summary>
    private static long TextToInteger(string text)
    {
        var span = text.AsSpan().TrimStart();
        var end = span.Length > 0 && span[0] is '+' or '-' ? 1 : 0;
        var digitsStart = end;
        while (end < span.Length && char.IsAsciiDigit(span[end]))
        {
            end++;
        }

        if (end == digitsStart)
        {
            return 0;
        }

        return long.TryParse(span[..end], NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : span[0] == '-' ? long.MinValue : long.MaxValue;
    }

    /// <summary>
    /// SQLite's <c>sqlite3AtoF</c>: the longest leading run that reads as a number - digits, a point, an
    /// exponent - and <c>0</c> when there is none.
    /// </summary>
    private static double TextToReal(string text)
    {
        var span = text.AsSpan().TrimStart();
        var end = span.Length > 0 && span[0] is '+' or '-' ? 1 : 0;
        var digits = 0;
        while (end < span.Length && char.IsAsciiDigit(span[end]))
        {
            end++;
            digits++;
        }

        if (end < span.Length && span[end] == '.')
        {
            end++;
            while (end < span.Length && char.IsAsciiDigit(span[end]))
            {
                end++;
                digits++;
            }
        }

        if (digits == 0)
        {
            return 0;
        }

        if (end < span.Length && span[end] is 'e' or 'E')
        {
            var exponent = end + 1;
            if (exponent < span.Length && span[exponent] is '+' or '-')
            {
                exponent++;
            }

            var exponentDigits = exponent;
            while (exponentDigits < span.Length && char.IsAsciiDigit(span[exponentDigits]))
            {
                exponentDigits++;
            }

            if (exponentDigits > exponent)
            {
                end = exponentDigits;
            }
        }

        return double.Parse(span[..end], NumberStyles.Float, CultureInfo.InvariantCulture);
    }

    public static double ToDouble(object value) => value switch
    {
        double real => real,
        long integer => integer,
        string text => double.Parse(text, NumberStyles.Float, CultureInfo.InvariantCulture),
        _ => Convert.ToDouble(value, CultureInfo.InvariantCulture),
    };

    public static decimal ToDecimal(object value) => value switch
    {
        decimal money => money,
        long integer => integer,
        double real => (decimal)real,
        string text => decimal.Parse(text, NumberStyles.Number | NumberStyles.AllowExponent, CultureInfo.InvariantCulture),
        _ => Convert.ToDecimal(value, CultureInfo.InvariantCulture),
    };

    /// <summary>
    /// A cell as SQLite's own text: a blob is read as UTF-8, and a REAL keeps its decimal point - 1.0 is
    /// <c>1.0</c>, not <c>1</c>.
    /// </summary>
    public static string ToText(object value) => value switch
    {
        string text => text,
        byte[] bytes => Encoding.UTF8.GetString(bytes),
        long integer => integer.ToString(CultureInfo.InvariantCulture),
        double real => RealToText(real),
        _ => Convert.ToString(value, CultureInfo.InvariantCulture)!,
    };

    public static byte[] ToBytes(object value) => value switch
    {
        byte[] bytes => bytes,
        string text => Encoding.UTF8.GetBytes(text),
        _ => throw new InvalidCastException($"Cannot read {value.GetType()} as bytes."),
    };

    /// <summary>
    /// TEXT in any format <see cref="DateTime.Parse(string, IFormatProvider, DateTimeStyles)"/> reads, or a
    /// Julian day number - what <c>julianday()</c> returns, and how some schemas store their dates.
    /// </summary>
    public static DateTime ToDateTime(object value) => value switch
    {
        DateTime dateTime => dateTime,
        string text => DateTime.Parse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
        double julianDay => FromJulianDay(julianDay),
        long julianDay => FromJulianDay(julianDay),
        _ => Convert.ToDateTime(value, CultureInfo.InvariantCulture),
    };

    /// <summary>A Julian day number as a date and time, to the millisecond SQLite's date functions keep.</summary>
    public static DateTime FromJulianDay(double julianDay)
        => new DateTime(1970, 1, 1).AddMilliseconds(Math.Round((julianDay - UnixEpochJulianDay) * 86_400_000d));

    private static long RealToInteger(double real) => real switch
    {
        >= 9223372036854775807d => long.MaxValue,
        <= -9223372036854775808d => long.MinValue,
        _ => (long)real,
    };

    /// <summary>
    /// SQLite's text for a REAL: the shortest digits that read back as the same double - as SQLite 3.43 and
    /// later print one, and .NET does - and never in a form that reads as an integer.
    /// </summary>
    private static string RealToText(double real)
    {
        if (double.IsPositiveInfinity(real))
        {
            return "Inf";
        }

        if (double.IsNegativeInfinity(real))
        {
            return "-Inf";
        }

        var text = real.ToString("R", CultureInfo.InvariantCulture);
        var exponent = text.IndexOf('E', StringComparison.Ordinal);
        if (exponent < 0)
        {
            return text.Contains('.', StringComparison.Ordinal) ? text : text + ".0";
        }

        var mantissa = text[..exponent];
        if (!mantissa.Contains('.', StringComparison.Ordinal))
        {
            mantissa += ".0";
        }

        return mantissa + "e" + text[(exponent + 1)..];
    }
}
