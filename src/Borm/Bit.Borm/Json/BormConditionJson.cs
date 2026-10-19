using System.Globalization;
using System.Text.Json;

namespace Bit.Borm;

/// <summary>
/// Reads and writes the serialisable conditions (<see cref="BormFieldCondition"/> and
/// <see cref="BormConditionGroup"/>) used by the <c>x-visibleWhen</c>, <c>x-disabledWhen</c> and
/// <c>x-requiredWhen</c> schema extensions. JSON Schema has no vocabulary for "show this field when
/// that one says X", so Borm carries it in a small, explicit extension instead of trying to
/// encode UI intent in <c>if</c>/<c>then</c>.
/// </summary>
/// <remarks>
/// A single condition is <c>{ "field": "Country", "op": "Equals", "value": "US" }</c>; a group is
/// <c>{ "all": [ … ] }</c> or <c>{ "any": [ … ] }</c>. Conditions backed by a delegate
/// (<see cref="BormPredicateCondition"/>) cannot be represented and are skipped on export.
/// </remarks>
public static class BormConditionJson
{
    /// <summary>
    /// Parses a condition from its JSON form, returning null when the element is not a condition — or
    /// is one this library cannot read faithfully.
    /// </summary>
    /// <remarks>
    /// "Faithfully" is the point. An <c>op</c> that names no operator (<c>"neq"</c>, <c>"!="</c>, a
    /// number) used to fall back to <c>Equals</c>, which turned "hide when not X" into "hide when X";
    /// and a group with one unreadable member kept the others, which widens an <c>all</c> and narrows
    /// an <c>any</c>. Both are now unreadable as a whole — the same rule the writer follows when it
    /// refuses to export a group with a member it cannot represent.
    /// </remarks>
    public static IBormCondition? Read(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object) return null;

        if (element.TryGetProperty("all", out var all) && all.ValueKind == JsonValueKind.Array)
            return ReadMany(all) is { } allOf ? new BormConditionGroup(BormConditionLogic.And, allOf) : null;

        if (element.TryGetProperty("any", out var any) && any.ValueKind == JsonValueKind.Array)
            return ReadMany(any) is { } anyOf ? new BormConditionGroup(BormConditionLogic.Or, anyOf) : null;

        if (!element.TryGetProperty("field", out var field) || field.ValueKind != JsonValueKind.String)
            return null;

        // No `op` at all is the shorthand for Equals; an `op` that is there has to name an operator.
        var op = BormConditionOperator.Equals;
        if (element.TryGetProperty("op", out var opElement) && !TryReadOperator(opElement, out op))
            return null;

        var value = element.TryGetProperty("value", out var valueElement) ? ReadValue(valueElement) : null;

        return new BormFieldCondition(field.GetString()!, op, value);
    }

    /// <summary>
    /// Reads an operator by its name, ignoring case. <see cref="Enum.TryParse{TEnum}(string?, bool, out TEnum)"/>
    /// alone also accepts numbers ("99") and comma-separated lists, neither of which names an operator.
    /// </summary>
    private static bool TryReadOperator(JsonElement element, out BormConditionOperator op)
    {
        op = default;
        if (element.ValueKind != JsonValueKind.String || element.GetString() is not { } name) return false;

        foreach (var candidate in Enum.GetValues<BormConditionOperator>())
        {
            if (string.Equals(candidate.ToString(), name.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                op = candidate;
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Writes a condition. Returns false — writing nothing — when the condition cannot be represented
    /// in JSON, so the caller can decide whether to omit the property entirely.
    /// </summary>
    public static bool TryWrite(Utf8JsonWriter writer, IBormCondition condition)
    {
        switch (condition)
        {
            // A comparison with a value that has no JSON form (NaN, infinity) cannot be written as
            // itself, and writing it as null turned "less than infinity" into "never".
            case BormFieldCondition field when !IsWritable(field.Value):
                return false;

            case BormFieldCondition field:
                writer.WriteStartObject();
                writer.WriteString("field", field.FieldPath);
                writer.WriteString("op", field.Operator.ToString());
                if (field.Value is not null)
                {
                    writer.WritePropertyName("value");
                    WriteValue(writer, field.Value);
                }
                writer.WriteEndObject();
                return true;

            case BormConditionGroup group:
                // A group with an unrepresentable child would silently change meaning, so it is
                // dropped as a whole rather than exported half-complete.
                var children = group.Conditions.Where(CanWrite).ToList();
                if (children.Count != group.Conditions.Count) return false;

                writer.WriteStartObject();
                writer.WritePropertyName(group.Logic == BormConditionLogic.And ? "all" : "any");
                writer.WriteStartArray();
                foreach (var child in children) TryWrite(writer, child);
                writer.WriteEndArray();
                writer.WriteEndObject();
                return true;

            default:
                return false;
        }
    }

    /// <summary>Whether <paramref name="condition"/> can be represented in JSON at all.</summary>
    public static bool CanWrite(IBormCondition? condition) => condition switch
    {
        null => false,
        BormFieldCondition field => IsWritable(field.Value),
        BormConditionGroup group => group.Conditions.All(CanWrite),
        _ => false
    };

    /// <summary>Writes <paramref name="condition"/> under <paramref name="propertyName"/> when it is representable.</summary>
    public static void WriteIfRepresentable(Utf8JsonWriter writer, string propertyName, IBormCondition? condition)
    {
        if (!CanWrite(condition)) return;
        writer.WritePropertyName(propertyName);
        TryWrite(writer, condition!);
    }

    /// <summary>Reads every member of a group, or nothing at all when one of them cannot be read.</summary>
    private static IBormCondition[]? ReadMany(JsonElement array)
    {
        var conditions = new List<IBormCondition>();
        foreach (var element in array.EnumerateArray())
        {
            if (Read(element) is not { } condition) return null;
            conditions.Add(condition);
        }
        return [.. conditions];
    }

    internal static object? ReadValue(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.String => element.GetString(),
        JsonValueKind.Number => element.TryGetInt64(out var l) ? l
            : element.TryGetDouble(out var d) ? d
            : element.GetRawText(),
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        JsonValueKind.Array => element.EnumerateArray().Select(ReadValue).ToList(),
        _ => null
    };

    /// <summary>
    /// Whether <see cref="WriteValue"/> can give <paramref name="value"/> a faithful JSON form. Code and
    /// numbers that are not finite have none: <c>Utf8JsonWriter</c> throws on NaN and infinity.
    /// </summary>
    internal static bool IsWritable(object? value) => value switch
    {
        double d => double.IsFinite(d),
        float f => float.IsFinite(f),
        Delegate => false,
        string => true,
        System.Collections.IEnumerable items => items.Cast<object?>().All(IsWritable),
        _ => true
    };

    /// <summary>
    /// Writes a value as JSON. Every integral type is a number — a <c>short</c> default used to come
    /// back as the string "5" — and dates and times are ISO 8601, which is what <c>format: date</c>
    /// and friends require and what an <c>&lt;input type="date"&gt;</c> will display. The invariant
    /// culture's own rendering ("10/18/2026 00:00:00") is neither.
    /// </summary>
    /// <param name="writer">The writer.</param>
    /// <param name="value">The value.</param>
    /// <param name="fieldType">
    /// The field the value belongs to, when there is one. A date field's default is written as just
    /// the date and a time field's as just the time, because that is the shape their inputs accept.
    /// </param>
    internal static void WriteValue(Utf8JsonWriter writer, object? value, BormFieldType? fieldType = null)
    {
        switch (value)
        {
            case null: writer.WriteNullValue(); break;
            case bool b: writer.WriteBooleanValue(b); break;
            case sbyte or byte or short or ushort or int or uint or long:
                writer.WriteNumberValue(Convert.ToInt64(value, CultureInfo.InvariantCulture));
                break;
            case ulong ul: writer.WriteNumberValue(ul); break;
            // A number with no JSON form is written as the absence of one rather than failing the export.
            case double d when !double.IsFinite(d): writer.WriteNullValue(); break;
            case float f when !float.IsFinite(f): writer.WriteNullValue(); break;
            case double d: writer.WriteNumberValue(d); break;
            case float f: writer.WriteNumberValue(f); break;
            case decimal m: writer.WriteNumberValue(m); break;
            case string s: writer.WriteStringValue(s); break;
            case DateTime dt: writer.WriteStringValue(FormatDateTime(dt, fieldType)); break;
            case DateTimeOffset dto: writer.WriteStringValue(FormatDateTimeOffset(dto, fieldType)); break;
            case DateOnly date: writer.WriteStringValue(date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)); break;
            case TimeOnly time: writer.WriteStringValue(time.ToString("HH:mm:ss", CultureInfo.InvariantCulture)); break;
            case TimeSpan span: writer.WriteStringValue(span.ToString("c", CultureInfo.InvariantCulture)); break;
            case System.Collections.IEnumerable e:
                writer.WriteStartArray();
                foreach (var item in e) WriteValue(writer, item);
                writer.WriteEndArray();
                break;
            default:
                writer.WriteStringValue(BormValueConverter.ToInvariantString(value));
                break;
        }
    }

    private static string FormatDateTime(DateTime value, BormFieldType? fieldType) => fieldType switch
    {
        BormFieldType.Date => value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        BormFieldType.Time => value.ToString("HH:mm:ss", CultureInfo.InvariantCulture),
        // A datetime-local input takes no offset, so a field's own default is written without one.
        BormFieldType.DateTime => value.ToString("yyyy-MM-dd'T'HH:mm:ss", CultureInfo.InvariantCulture),
        _ => value.ToString("yyyy-MM-dd'T'HH:mm:ss.FFFFFFFK", CultureInfo.InvariantCulture)
    };

    private static string FormatDateTimeOffset(DateTimeOffset value, BormFieldType? fieldType) => fieldType switch
    {
        BormFieldType.Date or BormFieldType.Time or BormFieldType.DateTime
            => FormatDateTime(value.DateTime, fieldType),
        _ => value.ToString("yyyy-MM-dd'T'HH:mm:ss.FFFFFFFzzz", CultureInfo.InvariantCulture)
    };
}
