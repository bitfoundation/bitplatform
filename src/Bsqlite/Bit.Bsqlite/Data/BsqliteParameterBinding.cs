using System.Globalization;

namespace Bit.Bsqlite.Data;

/// <summary>
/// Converts CLR values onto the SQLite representation Microsoft.Data.Sqlite would bind, and back.
/// </summary>
/// <remarks>
/// <para>
/// Dates, times, and GUIDs each have more than one reasonable SQLite storage form. The wire format
/// therefore refuses them, so this layer - the equivalent of <c>SqliteParameter.Bind</c> - owns the
/// choice. The formats here are Microsoft.Data.Sqlite's defaults (TEXT, not Julian-day REAL or
/// binary GUIDs), which is also what EF Core's SQLite type mapping expects when the store type is
/// TEXT.
/// </para>
/// <para>
/// Values the wire format already knows how to encode (integers, text, blobs, decimals, bools) pass
/// through unchanged on the way in. On the way out, EF materializes through
/// <c>DbDataReader.GetFieldValue&lt;T&gt;</c>, so TEXT cells must be parsed back into those CLR types.
/// </para>
/// </remarks>
public static class BsqliteParameterBinding
{
    /// <summary>
    /// Returns a value the transport can encode, converting dates, times, and GUIDs to TEXT.
    /// </summary>
    public static object? ToTransportValue(object? value) => value switch
    {
        DateTime dateTime => dateTime.ToString(@"yyyy\-MM\-dd HH\:mm\:ss.FFFFFFF", CultureInfo.InvariantCulture),
        DateTimeOffset dateTimeOffset => dateTimeOffset.ToString(
            @"yyyy\-MM\-dd HH\:mm\:ss.FFFFFFFzzz",
            CultureInfo.InvariantCulture),
        DateOnly dateOnly => dateOnly.ToString(@"yyyy\-MM\-dd", CultureInfo.InvariantCulture),
        TimeOnly timeOnly => timeOnly.Ticks % 10_000_000 == 0
            ? timeOnly.ToString(@"HH:mm:ss", CultureInfo.InvariantCulture)
            : timeOnly.ToString(@"HH:mm:ss.fffffff", CultureInfo.InvariantCulture),
        TimeSpan timeSpan => timeSpan.ToString("c"),
        Guid guid => guid.ToString().ToUpperInvariant(),
        _ => value,
    };

    /// <summary>
    /// Converts a transport cell back into <typeparamref name="T"/>.
    /// </summary>
    public static T FromTransportValue<T>(object? value)
    {
        if (value is null or DBNull)
        {
            return default!;
        }

        if (value is T typed)
        {
            return typed;
        }

        return (T)FromTransportValue(value, typeof(T));
    }

    private static object FromTransportValue(object value, Type clrType)
    {
        var target = Nullable.GetUnderlyingType(clrType) ?? clrType;

        if (target == typeof(TimeSpan))
        {
            return value switch
            {
                string text => TimeSpan.Parse(text, CultureInfo.InvariantCulture),
                double days => TimeSpan.FromDays(days),
                float days => TimeSpan.FromDays(days),
                long days => TimeSpan.FromDays(days),
                int days => TimeSpan.FromDays(days),
                _ => TimeSpan.Parse(
                    Convert.ToString(value, CultureInfo.InvariantCulture)!,
                    CultureInfo.InvariantCulture),
            };
        }

        if (target == typeof(DateTimeOffset))
        {
            return value switch
            {
                DateTime dateTime => new DateTimeOffset(dateTime, TimeSpan.Zero),
                string text => DateTimeOffset.Parse(
                    text,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal),
                _ => DateTimeOffset.Parse(
                    Convert.ToString(value, CultureInfo.InvariantCulture)!,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal),
            };
        }

        if (target == typeof(DateOnly))
        {
            return value switch
            {
                DateTime dateTime => DateOnly.FromDateTime(dateTime),
                string text => DateOnly.Parse(text, CultureInfo.InvariantCulture),
                _ => DateOnly.Parse(
                    Convert.ToString(value, CultureInfo.InvariantCulture)!,
                    CultureInfo.InvariantCulture),
            };
        }

        if (target == typeof(TimeOnly))
        {
            return value switch
            {
                string text => TimeOnly.Parse(text, CultureInfo.InvariantCulture),
                _ => TimeOnly.Parse(
                    Convert.ToString(value, CultureInfo.InvariantCulture)!,
                    CultureInfo.InvariantCulture),
            };
        }

        if (target == typeof(DateTime))
        {
            return value switch
            {
                string text => DateTime.Parse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
                _ => Convert.ToDateTime(value, CultureInfo.InvariantCulture),
            };
        }

        if (target == typeof(Guid))
        {
            return value switch
            {
                string text => Guid.Parse(text),
                byte[] bytes => new Guid(bytes),
                _ => Guid.Parse(Convert.ToString(value, CultureInfo.InvariantCulture)!),
            };
        }

        if (target == typeof(bool))
        {
            return value switch
            {
                string text => text is not "0"
                    && !text.Equals("false", StringComparison.OrdinalIgnoreCase),
                _ => Convert.ToInt64(value, CultureInfo.InvariantCulture) != 0,
            };
        }

        if (target == typeof(decimal))
        {
            return value switch
            {
                string text => decimal.Parse(text, NumberStyles.Number, CultureInfo.InvariantCulture),
                _ => Convert.ToDecimal(value, CultureInfo.InvariantCulture),
            };
        }

        if (target.IsEnum)
        {
            return value is string name
                ? Enum.Parse(target, name, ignoreCase: true)
                : Enum.ToObject(target, value);
        }

        return Convert.ChangeType(value, target, CultureInfo.InvariantCulture)!;
    }
}
