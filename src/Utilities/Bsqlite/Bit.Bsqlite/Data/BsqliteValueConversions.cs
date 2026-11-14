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
    /// A REAL becomes an INTEGER by truncation toward zero, as <c>CAST(x AS INTEGER)</c> and
    /// <c>sqlite3_column_int64</c> make it - not by rounding, which <see cref="Convert"/> does - and
    /// saturates rather than wrapping past the 64-bit range.
    /// </summary>
    public static long ToInt64(object value) => value switch
    {
        long integer => integer,
        double real => RealToInteger(real),
        string text => long.TryParse(text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : RealToInteger(double.Parse(text, NumberStyles.Float, CultureInfo.InvariantCulture)),
        bool flag => flag ? 1 : 0,
        decimal money => (long)decimal.Truncate(money),
        _ => Convert.ToInt64(value, CultureInfo.InvariantCulture),
    };

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
