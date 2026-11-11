using System.Collections;
using System.Globalization;

namespace Bit.Bcrdt.Borm;

/// <summary>Moves a form's values onto the collaborative value model and back.</summary>
/// <remarks>
/// <para>
/// Dates, times and enum members are stored as invariant text rather than as the value model's own
/// dates or as numbers. A <see cref="DateOnly"/> or a <see cref="TimeOnly"/> has no counterpart in the
/// value model, and a <see cref="DateTime"/> stored as one would come back as a
/// <see cref="DateTimeOffset"/> in UTC - a different value, and one Borm cannot write into the property
/// it came from. An enum stored by name survives the enum being reordered.
/// </para>
/// <para>
/// On the way back the field's CLR type decides the reading, and Borm's own converter does the rest, so
/// a form bound to a model gets its own types back and a form bound to a dictionary gets what Borm
/// itself would hold there.
/// </para>
/// </remarks>
internal static class BcrdtBormValues
{
    public static object? ToDocument(object? value) => value switch
    {
        null => null,
        string or bool or long or int or short or ushort or byte or sbyte or uint or ulong or double or float
            or decimal or Guid or byte[] => BcrdtAnyCodec.FromObject(value),
        Enum member => member.ToString(),
        DateTime dateTime => dateTime.ToString("o", CultureInfo.InvariantCulture),
        DateTimeOffset dateTimeOffset => dateTimeOffset.ToString("o", CultureInfo.InvariantCulture),
        DateOnly date => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        TimeOnly time => time.ToString("HH:mm:ss.FFFFFFF", CultureInfo.InvariantCulture),
        TimeSpan span => span.ToString("c", CultureInfo.InvariantCulture),
        IDictionary dictionary => ToDocument(dictionary),
        IEnumerable items => ToDocument(items),
        _ => BcrdtAnyCodec.FromObject(value),
    };

    private static List<object?> ToDocument(IEnumerable items)
    {
        List<object?> result = [];
        foreach (object? item in items) result.Add(ToDocument(item));
        return result;
    }

    private static Dictionary<string, object?> ToDocument(IDictionary dictionary)
    {
        var result = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (DictionaryEntry entry in dictionary)
        {
            result[Convert.ToString(entry.Key, CultureInfo.InvariantCulture) ?? string.Empty] = ToDocument(entry.Value);
        }
        return result;
    }

    /// <summary>
    /// Reads a stored value back as the field holds it. A value that cannot be read as the field's type
    /// is handed over as it is, and Borm reports it on the field the way it reports anything else it
    /// cannot take - rather than being dropped here without a word.
    /// </summary>
    public static object? FromDocument(object? value, BormFieldDefinition field)
    {
        if (value is null || field.ValueType is not { } target) return value;

        Type type = Nullable.GetUnderlyingType(target) ?? target;
        if (value is string text)
        {
            if (text.Length == 0 && type != typeof(string)) return null;

            if (type.IsEnum && Enum.TryParse(type, text, ignoreCase: true, out object? member)) return member;
            if (type == typeof(DateOnly) && DateOnly.TryParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly date)) return date;
            if (type == typeof(TimeOnly) && TimeOnly.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out TimeOnly time)) return time;
            if (type == typeof(TimeSpan) && TimeSpan.TryParseExact(text, "c", CultureInfo.InvariantCulture, out TimeSpan span)) return span;
            if (type == typeof(DateTime) && DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTime dateTime)) return dateTime;
            if (type == typeof(DateTimeOffset) && DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTimeOffset dateTimeOffset)) return dateTimeOffset;
        }

        return BormValueConverter.TryCoerce(value, target, out object? converted) ? converted : value;
    }

    /// <summary>Whether the form holds what the document holds.</summary>
    public static bool Same(object? formValue, object? documentValue) =>
        BcrdtAnyCodec.DeepEquals(ToDocument(formValue), documentValue);
}
