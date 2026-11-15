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

        // The underlying integer, as Microsoft.Data.Sqlite binds an enum.
        Enum enumeration => Type.GetTypeCode(enumeration.GetType()) is TypeCode.UInt64
            ? Convert.ToUInt64(enumeration, CultureInfo.InvariantCulture)
            : Convert.ToInt64(enumeration, CultureInfo.InvariantCulture),
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
                double or long => new DateTimeOffset(BsqliteValueConversions.ToDateTime(value), TimeSpan.Zero),
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
                double or long => DateOnly.FromDateTime(BsqliteValueConversions.ToDateTime(value)),
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
                double or long => TimeOnly.FromDateTime(BsqliteValueConversions.ToDateTime(value)),
                _ => TimeOnly.Parse(
                    Convert.ToString(value, CultureInfo.InvariantCulture)!,
                    CultureInfo.InvariantCulture),
            };
        }

        if (target == typeof(DateTime))
        {
            return BsqliteValueConversions.ToDateTime(value);
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
            return BsqliteValueConversions.ToDecimal(value);
        }

        if (target.IsEnum)
        {
            return value is string name
                ? Enum.Parse(target, name, ignoreCase: true)
                : Enum.ToObject(target, BsqliteValueConversions.ToInt64(value));
        }

        if (target == typeof(string))
        {
            return BsqliteValueConversions.ToText(value);
        }

        if (target == typeof(byte[]))
        {
            return BsqliteValueConversions.ToBytes(value);
        }

        if (target == typeof(double))
        {
            return BsqliteValueConversions.ToDouble(value);
        }

        if (target == typeof(float))
        {
            return (float)BsqliteValueConversions.ToDouble(value);
        }

        // Integers truncate a REAL, as SQLite's CAST does, and narrow checked, as Microsoft.Data.Sqlite does:
        // Convert.ChangeType would round 3.5 to 4.
        return Type.GetTypeCode(target) switch
        {
            TypeCode.Int64 => BsqliteValueConversions.ToInt64(value),
            TypeCode.Int32 => checked((int)BsqliteValueConversions.ToInt64(value)),
            TypeCode.Int16 => checked((short)BsqliteValueConversions.ToInt64(value)),
            TypeCode.SByte => checked((sbyte)BsqliteValueConversions.ToInt64(value)),
            TypeCode.Byte => checked((byte)BsqliteValueConversions.ToInt64(value)),
            TypeCode.UInt16 => checked((ushort)BsqliteValueConversions.ToInt64(value)),
            TypeCode.UInt32 => checked((uint)BsqliteValueConversions.ToInt64(value)),
            TypeCode.UInt64 => checked((ulong)BsqliteValueConversions.ToInt64(value)),
            _ => Convert.ChangeType(value, target, CultureInfo.InvariantCulture)!,
        };
    }
}
