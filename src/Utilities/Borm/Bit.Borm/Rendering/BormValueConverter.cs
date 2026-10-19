using System.Collections;
using System.Globalization;

namespace Bit.Borm;

/// <summary>
/// Converts between the string values produced by HTML inputs and the CLR types expected by the model.
/// Conversions are culture-invariant to keep round-tripping deterministic: an <c>&lt;input type="date"&gt;</c>
/// always speaks ISO-8601 and an <c>&lt;input type="number"&gt;</c> always uses a dot decimal separator,
/// regardless of the server's current culture.
/// </summary>
public static class BormValueConverter
{
    /// <summary>Renders a stored value as the string an input element should display.</summary>
    public static string ToInputString(object? value, BormFieldType type)
    {
        if (value is null) return string.Empty;
        return value switch
        {
            string s => s,
            DateTime dt => type switch
            {
                BormFieldType.Date => dt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                BormFieldType.Time => dt.ToString("HH:mm", CultureInfo.InvariantCulture),
                _ => dt.ToString("yyyy-MM-ddTHH:mm", CultureInfo.InvariantCulture)
            },
            // Shown on its own clock — the date and time the value itself states. The input has no room
            // for the offset, so the text that comes back is given the offset the value already had
            // (see FromInputString's `current`), which is what keeps the round trip on the same instant.
            DateTimeOffset dto => type switch
            {
                BormFieldType.Date => dto.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                BormFieldType.Time => dto.ToString("HH:mm", CultureInfo.InvariantCulture),
                _ => dto.ToString("yyyy-MM-ddTHH:mm", CultureInfo.InvariantCulture)
            },
            DateOnly d => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            TimeOnly t => t.ToString("HH:mm", CultureInfo.InvariantCulture),
            // <input type="time"> only understands HH:mm / HH:mm:ss, never "1.02:03:04".
            TimeSpan ts => ts.ToString(@"hh\:mm", CultureInfo.InvariantCulture),
            bool b => b ? "true" : "false",
            _ => ToInvariantString(value)
        };
    }

    /// <summary>
    /// Renders any value as a stable, culture-independent string — used for option matching,
    /// uniqueness checks and JSON export, where the ambient culture must not change the result.
    /// </summary>
    public static string ToInvariantString(object? value) => value switch
    {
        null => string.Empty,
        string s => s,
        IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString() ?? string.Empty
    };

    /// <summary>Parses an input string into the target CLR type (falling back to the field type).</summary>
    public static object? FromInputString(string? raw, Type? targetType, BormFieldType fieldType)
        => FromInputString(raw, targetType, fieldType, current: null);

    /// <summary>
    /// Parses an input string into the target CLR type, given the value the field holds now.
    /// </summary>
    /// <param name="raw">The text the control produced.</param>
    /// <param name="targetType">The CLR type to produce, when known.</param>
    /// <param name="fieldType">The field type, which decides the reading when there is no CLR type.</param>
    /// <param name="current">
    /// The value being replaced. A <see cref="DateTimeOffset"/> input shows the value's own clock with
    /// no offset, so the offset has to come from somewhere: it is taken from the current value, which
    /// puts an unedited round trip back on exactly the instant it started from. Without a current
    /// value (or for text that names its own offset) the reading is as before — the local offset in
    /// force at that time.
    /// </param>
    public static object? FromInputString(string? raw, Type? targetType, BormFieldType fieldType, object? current)
    {
        var type = targetType is null ? null : Nullable.GetUnderlyingType(targetType) ?? targetType;
        var nullable = targetType is not null && Nullable.GetUnderlyingType(targetType) is not null;

        if (string.IsNullOrEmpty(raw))
        {
            if (type is null || nullable || !type.IsValueType) return null;
            // A non-nullable value type cannot hold "no value", so fall back to its default.
            return Activator.CreateInstance(type);
        }

        // No target type: infer from the field type (the dictionary-backed, schema-only case).
        // NumberStyles.Float, not Any: Any lets a comma through as a thousands separator, which read the
        // "1,5" a decimal-comma user means as one and a half as fifteen.
        if (type is null)
        {
            return fieldType switch
            {
                BormFieldType.Integer =>
                    long.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var l) ? l : raw,
                BormFieldType.Number or BormFieldType.Range =>
                    double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var d) ? d : raw,
                BormFieldType.Checkbox => IsTruthy(raw),
                _ => raw
            };
        }

        return TryParse(raw, type, current, out var parsed)
            ? parsed
            : raw; // keep the raw input so the user can correct it; validation flags it
    }

    /// <summary>
    /// Coerces an arbitrary value to <paramref name="targetType"/>, handling nullables, enums,
    /// dates, and element-wise conversion of collections (so a <c>List&lt;string&gt;</c> gathered by a
    /// multi-select can be stored into a <c>List&lt;TEnum&gt;</c> property). Returns false when no safe
    /// conversion exists, letting callers leave the model untouched rather than throw.
    /// </summary>
    public static bool TryCoerce(object? value, Type targetType, out object? result)
    {
        result = null;
        var underlying = Nullable.GetUnderlyingType(targetType) ?? targetType;

        if (value is null)
        {
            // Reference and nullable targets accept null; a bare value type does not.
            if (!targetType.IsValueType || Nullable.GetUnderlyingType(targetType) is not null) return true;
            result = Activator.CreateInstance(underlying);
            return true;
        }

        if (underlying.IsInstanceOfType(value))
        {
            result = value;
            return true;
        }

        if (value is string s)
        {
            if (s.Length == 0 && (!targetType.IsValueType || Nullable.GetUnderlyingType(targetType) is not null))
                return true;
            return TryParse(s, underlying, current: null, out result);
        }

        if (TryCoerceCollection(value, targetType, underlying, out result))
            return true;

        // A whole-number target is not offered a fraction to round. Convert.ChangeType rounds to even,
        // so 2.5 arrived as 2 and 3.7 as 4 — a different number, stored without a word.
        if (IsWholeNumberType(underlying.IsEnum ? Enum.GetUnderlyingType(underlying) : underlying) && HasFraction(value))
            return false;

        try
        {
            if (underlying.IsEnum)
            {
                var member = Enum.ToObject(underlying, Convert.ChangeType(value, Enum.GetUnderlyingType(underlying), CultureInfo.InvariantCulture));
                if (!IsAcceptableEnumValue(underlying, member)) return false;
                result = member;
                return true;
            }
            if (value is IConvertible && typeof(IConvertible).IsAssignableFrom(underlying))
            {
                result = Convert.ChangeType(value, underlying, CultureInfo.InvariantCulture);
                return true;
            }
        }
        catch (Exception ex) when (ex is InvalidCastException or FormatException or OverflowException or ArgumentException)
        {
            // fall through
        }

        return false;
    }

    /// <summary>
    /// Materialises a new collection of <paramref name="targetType"/> holding the elements of
    /// <paramref name="value"/>, converting each one. Supports arrays, any type assignable from
    /// <c>List&lt;TElement&gt;</c> (<c>List&lt;T&gt;</c>, <c>IList&lt;T&gt;</c>, <c>ICollection&lt;T&gt;</c>,
    /// <c>IEnumerable&lt;T&gt;</c>), a set declared by its interface (<c>ISet&lt;T&gt;</c>,
    /// <c>IReadOnlySet&lt;T&gt;</c>), and any concrete collection that can be built empty and added to
    /// (<c>HashSet&lt;T&gt;</c>, <c>ObservableCollection&lt;T&gt;</c>, <c>Collection&lt;T&gt;</c>) or
    /// built from its contents (<c>ReadOnlyCollection&lt;T&gt;</c>).
    /// </summary>
    /// <remarks>
    /// Only the first two used to be understood, so a multi-select over a <c>HashSet&lt;DayOfWeek&gt;</c>
    /// — the natural type for "which days" — failed every write and reported the user's ticks as a value
    /// the field could not accept.
    /// </remarks>
    private static bool TryCoerceCollection(object value, Type targetType, Type underlying, out object? result)
    {
        result = null;
        if (value is not IEnumerable source || value is string) return false;

        var elementType = BormFieldTypeResolver.GetEnumerableElementType(underlying);
        if (elementType is null) return false;

        var listType = typeof(List<>).MakeGenericType(elementType);
        var list = (IList)Activator.CreateInstance(listType)!;
        foreach (var item in source)
        {
            if (!TryCoerce(item, elementType, out var converted)) return false;
            list.Add(converted);
        }

        if (underlying.IsArray)
        {
            var array = Array.CreateInstance(elementType, list.Count);
            list.CopyTo(array, 0);
            result = array;
            return true;
        }

        if (underlying.IsAssignableFrom(listType))
        {
            result = list;
            return true;
        }

        try
        {
            var setType = typeof(HashSet<>).MakeGenericType(elementType);
            if (underlying.IsAssignableFrom(setType))
            {
                result = Activator.CreateInstance(setType, list);
                return result is not null;
            }

            if (underlying.IsInterface || underlying.IsAbstract) return false;

            var collectionType = typeof(ICollection<>).MakeGenericType(elementType);
            if (collectionType.IsAssignableFrom(underlying) && underlying.GetConstructor(Type.EmptyTypes) is not null)
            {
                var collection = Activator.CreateInstance(underlying)!;
                var add = collectionType.GetMethod(nameof(ICollection<object>.Add))!;
                foreach (var item in list) add.Invoke(collection, [item]);
                result = collection;
                return true;
            }

            var fromContents = underlying.GetConstructor([typeof(IList<>).MakeGenericType(elementType)])
                               ?? underlying.GetConstructor([typeof(IEnumerable<>).MakeGenericType(elementType)]);
            if (fromContents is not null)
            {
                result = fromContents.Invoke([list]);
                return true;
            }
        }
        catch (Exception ex) when (ex is System.Reflection.TargetInvocationException or MissingMethodException
                                       or MemberAccessException or ArgumentException or NotSupportedException)
        {
            // A collection that would not be built or would not take an item; the write is refused below.
        }

        result = null;
        return false;
    }

    /// <summary>Parses a string into <paramref name="type"/> using invariant, unambiguous formats.</summary>
    /// <remarks>
    /// Every common type is read with its own <c>TryParse</c>. This sits on the path of every condition
    /// that compares a typed value with text (<c>Equals("Code", 1)</c> while Code holds "other"), and
    /// conditions are evaluated on every render — so text that is not a number, a date or a GUID must
    /// be a <c>false</c>, not an exception thrown and caught each time. Only types with no parser of
    /// their own still go through <see cref="Convert.ChangeType(object, Type, IFormatProvider)"/>.
    /// </remarks>
    private static bool TryParse(string raw, Type type, object? current, out object? result)
    {
        result = null;
        var invariant = CultureInfo.InvariantCulture;

        if (type == typeof(string)) { result = raw; return true; }
        if (type.IsEnum)
        {
            // Enum.Parse takes any number for any enum, so a posted "999" became a member the
            // dropdown never offered. A [Flags] value is a combination by nature and stays open.
            if (!Enum.TryParse(type, raw, ignoreCase: true, out var member) || !IsAcceptableEnumValue(type, member!))
                return false;
            result = member;
            return true;
        }
        if (type == typeof(bool)) { result = IsTruthy(raw); return true; }

        // Integers take no separators and no fraction; Convert.ChangeType reads them the same way.
        if (type == typeof(int)) return Parsed(int.TryParse(raw, NumberStyles.Integer, invariant, out var i32), i32, out result);
        if (type == typeof(long)) return Parsed(long.TryParse(raw, NumberStyles.Integer, invariant, out var i64), i64, out result);
        if (type == typeof(short)) return Parsed(short.TryParse(raw, NumberStyles.Integer, invariant, out var i16), i16, out result);
        if (type == typeof(byte)) return Parsed(byte.TryParse(raw, NumberStyles.Integer, invariant, out var u8), u8, out result);
        if (type == typeof(sbyte)) return Parsed(sbyte.TryParse(raw, NumberStyles.Integer, invariant, out var i8), i8, out result);
        if (type == typeof(ushort)) return Parsed(ushort.TryParse(raw, NumberStyles.Integer, invariant, out var u16), u16, out result);
        if (type == typeof(uint)) return Parsed(uint.TryParse(raw, NumberStyles.Integer, invariant, out var u32), u32, out result);
        if (type == typeof(ulong)) return Parsed(ulong.TryParse(raw, NumberStyles.Integer, invariant, out var u64), u64, out result);

        // Convert.ChangeType reads fractional types with a thousands separator allowed, so "1,5" was
        // fifteen. A number input never sends a separator; anything with one is not a number here.
        if (type == typeof(double)) return Parsed(double.TryParse(raw, NumberStyles.Float, invariant, out var dbl), dbl, out result);
        if (type == typeof(float)) return Parsed(float.TryParse(raw, NumberStyles.Float, invariant, out var flt), flt, out result);
        if (type == typeof(decimal)) return Parsed(decimal.TryParse(raw, NumberStyles.Float, invariant, out var dec), dec, out result);

        if (type == typeof(DateTime)) return Parsed(DateTime.TryParse(raw, invariant, DateTimeStyles.None, out var dt), dt, out result);
        if (type == typeof(DateTimeOffset)) return TryParseTimestamp(raw, current, out result);
        if (type == typeof(DateOnly)) return Parsed(DateOnly.TryParse(raw, invariant, DateTimeStyles.None, out var date), date, out result);
        if (type == typeof(TimeOnly)) return Parsed(TimeOnly.TryParse(raw, invariant, DateTimeStyles.None, out var time), time, out result);
        // <input type="time"> yields "HH:mm"; TimeSpan.Parse reads that as days when given "12", so be explicit.
        if (type == typeof(TimeSpan)) { result = ParseTimeSpan(raw); return result is not null; }
        if (type == typeof(Guid)) return Parsed(Guid.TryParse(raw, out var guid), guid, out result);
        if (type == typeof(Uri)) return Parsed(Uri.TryCreate(raw, UriKind.RelativeOrAbsolute, out var uri), uri, out result);
        if (type == typeof(char)) { result = raw.Length == 1 ? raw[0] : (object?)null; return result is not null; }

        try
        {
            result = Convert.ChangeType(raw, type, CultureInfo.InvariantCulture);
            return true;
        }
        catch (Exception ex) when (ex is FormatException or InvalidCastException or OverflowException or ArgumentException)
        {
            result = null;
            return false;
        }
    }

    /// <summary>
    /// Reads a timestamp. Text that states its own offset (or <c>Z</c>) is taken at its word. Text with
    /// none — all an input can produce — gets the offset of the value it replaces, so editing a value
    /// stored at +05:00 keeps it at +05:00 and an untouched round trip lands on the same instant; with no
    /// value to go by, it gets the local offset in force at that date and time.
    /// </summary>
    private static bool TryParseTimestamp(string raw, object? current, out object? result)
    {
        result = null;
        if (!DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var clock)) return false;

        if (clock.Kind != DateTimeKind.Unspecified)
        {
            if (!DateTimeOffset.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.None, out var stated)) return false;
            result = stated;
            return true;
        }

        var offset = current is DateTimeOffset existing ? existing.Offset : TimeZoneInfo.Local.GetUtcOffset(clock);
        try
        {
            result = new DateTimeOffset(clock, offset);
            return true;
        }
        catch (ArgumentException)
        {
            // The clock time and offset together fall outside what DateTimeOffset can represent.
            return false;
        }
    }

    private static bool Parsed<T>(bool ok, T value, out object? result)
    {
        result = ok ? value : null;
        return ok;
    }

    /// <summary>
    /// Reads the text of a time input. Every time an input produces has a colon in it, and without one
    /// the text is refused: <c>"12"</c> reads as twelve <em>days</em> under every <see cref="TimeSpan"/>
    /// format there is, which is the one reading nobody typing into a time box means.
    /// </summary>
    private static object? ParseTimeSpan(string raw)
    {
        if (!raw.Contains(':', StringComparison.Ordinal)) return null;

        return TimeSpan.TryParseExact(raw, ["hh\\:mm", "hh\\:mm\\:ss", "c", "g", "G"], CultureInfo.InvariantCulture, out var ts)
            ? ts
            : TimeSpan.TryParse(raw, CultureInfo.InvariantCulture, out var loose) ? loose : null;
    }

    /// <summary>
    /// Whether <paramref name="member"/> is a value <paramref name="enumType"/> actually declares. A
    /// <c>[Flags]</c> enum is exempt: any combination of its bits is a legitimate value.
    /// </summary>
    private static bool IsAcceptableEnumValue(Type enumType, object member)
        => enumType.IsDefined(typeof(FlagsAttribute), inherit: false) || Enum.IsDefined(enumType, member);

    private static bool IsWholeNumberType(Type type)
        => type == typeof(int) || type == typeof(long) || type == typeof(short) || type == typeof(byte)
           || type == typeof(sbyte) || type == typeof(ushort) || type == typeof(uint) || type == typeof(ulong);

    private static bool HasFraction(object value) => value switch
    {
        double d => d != Math.Truncate(d),
        float f => f != MathF.Truncate(f),
        decimal m => m != decimal.Truncate(m),
        _ => false
    };

    private static bool IsTruthy(string raw)
        => raw.Equals("true", StringComparison.OrdinalIgnoreCase)
           || raw.Equals("on", StringComparison.OrdinalIgnoreCase)
           || raw == "1";
}
