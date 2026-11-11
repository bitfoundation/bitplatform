using System.Buffers;
using System.Collections;
using System.Globalization;
using System.Text.Json;

namespace Bit.Bcrdt.Integrations;

/// <summary>
/// Moves an object between a library's own JSON format and the entries of a collaborative map, one top-level
/// property per key.
/// </summary>
/// <remarks>
/// <para>
/// Compiled into each integration package (Bit.Bcrdt.Bink, Bit.Bcrdt.Blow) rather than shipped from the engine, which
/// knows nothing about the libraries it is adapted to.
/// </para>
/// <para>
/// Splitting an object into one key per property is what makes concurrent edits of different properties - one person
/// moving a node while another renames it - both survive: each key resolves on its own, and only two writes of the
/// same property compete. Reusing the library's serializer, rather than mapping its model by hand, keeps the shared
/// state in exactly the shape the library already saves, validates and versions, including properties added later.
/// </para>
/// <para>
/// Values are stored in the collaborative value model - strings, numbers, booleans, lists and string-keyed
/// dictionaries - rather than as JSON text, so the shared document stays readable and comparable with
/// <see cref="BcrdtAnyCodec.DeepEquals"/>. Keys that start with <c>$</c> are the adapter's own bookkeeping (stacking
/// order, containing layer) and never a property of the object.
/// </para>
/// </remarks>
internal static class BcrdtIntegrationFields
{
    /// <summary>The first character of every bookkeeping key, which no library property starts with.</summary>
    public const char ReservedPrefix = '$';

    /// <summary>Converts a JSON value to the collaborative value model.</summary>
    public static object? FromJson(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.String => element.GetString(),
        // Integers stay integers, so a value written as 120 compares equal to itself after a round trip; the value
        // model compares a long and a double numerically in any case.
        JsonValueKind.Number => element.TryGetInt64(out long integer) ? integer : element.GetDouble(),
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        JsonValueKind.Array => element.EnumerateArray().Select(FromJson).ToList(),
        JsonValueKind.Object => ToDictionary(element),
        _ => null,
    };

    /// <remarks>
    /// Built in ordinal key order, which is the order every other replica decodes an object in: a dictionary kept in
    /// the order the serializer happened to write would hold the same content and still read back differently here
    /// than everywhere else.
    /// </remarks>
    public static Dictionary<string, object?> ToDictionary(JsonElement element)
    {
        var result = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (JsonProperty property in element.EnumerateObject().OrderBy(static property => property.Name, StringComparer.Ordinal))
        {
            result[property.Name] = FromJson(property.Value);
        }
        return result;
    }

    /// <summary>A dictionary of strings as a value-model object, in ordinal key order.</summary>
    public static Dictionary<string, object?> ToDictionary(IEnumerable<KeyValuePair<string, string>> pairs)
    {
        var result = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach ((string key, string value) in pairs.OrderBy(static pair => pair.Key, StringComparer.Ordinal)) result[key] = value;
        return result;
    }

    /// <summary>
    /// Splits a JSON object into its top-level properties, leaving out <paramref name="excluded"/> (the identifier,
    /// which is the map's key, and anything else that is not shared state).
    /// </summary>
    public static Dictionary<string, object?> Split(JsonElement element, IReadOnlySet<string> excluded)
    {
        var result = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (JsonProperty property in element.EnumerateObject())
        {
            if (excluded.Contains(property.Name) || property.Name.Length == 0 || property.Name[0] == ReservedPrefix) continue;
            result[property.Name] = FromJson(property.Value);
        }
        return result;
    }

    /// <summary>Reads the properties stored in an entry, leaving out the bookkeeping keys.</summary>
    public static Dictionary<string, object?> Read(BcrdtMap map)
    {
        var result = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach ((string key, object? value) in map)
        {
            if (key.Length == 0 || key[0] == ReservedPrefix || value is BcrdtAbstractType) continue;
            result[key] = value;
        }
        return result;
    }

    /// <summary>
    /// Writes the properties that differ from what the entry holds, and removes the stored properties the object no
    /// longer has that <paramref name="canRemove"/> allows.
    /// </summary>
    /// <remarks>
    /// Only changed properties are written. Writing every property instead would make saving one property revert
    /// whatever a collaborator changed in the others since, and would make every edit as large as the object.
    /// </remarks>
    /// <returns>Whether anything was written.</returns>
    public static bool Write(BcrdtMap map, IReadOnlyDictionary<string, object?> fields, Func<string, bool> canRemove)
    {
        bool wrote = false;
        foreach ((string key, object? value) in fields)
        {
            if (map.TryGetValue(key, out object? current) && BcrdtAnyCodec.DeepEquals(current, value)) continue;
            map.Set(key, value);
            wrote = true;
        }

        List<string>? stale = null;
        foreach (string key in map.Keys)
        {
            if (key.Length == 0 || key[0] == ReservedPrefix || fields.ContainsKey(key) || !canRemove(key)) continue;
            (stale ??= []).Add(key);
        }
        if (stale is null) return wrote;

        foreach (string key in stale) map.Delete(key);
        return true;
    }

    /// <summary>Creates an entry holding <paramref name="fields"/>, ready to be inserted.</summary>
    public static BcrdtMap Create(IReadOnlyDictionary<string, object?> fields)
    {
        // A detached map buffers its contents and replays them on insertion, so the entry and its properties arrive
        // at every replica together.
        var map = new BcrdtMap();
        foreach ((string key, object? value) in fields) map.Set(key, value);
        return map;
    }

    /// <summary>
    /// Writes an object's properties back as one JSON object, with its identifier first, in the shape the library's
    /// own reader expects.
    /// </summary>
    public static byte[] Compose(string idProperty, string id, IEnumerable<KeyValuePair<string, object?>> fields)
    {
        var buffer = new ArrayBufferWriter<byte>(256);
        using (var writer = new Utf8JsonWriter(buffer))
        {
            WriteObject(writer, idProperty, id, fields);
        }
        return buffer.WrittenSpan.ToArray();
    }

    /// <summary>Writes an object's properties as one JSON object, with its identifier first.</summary>
    public static void WriteObject(Utf8JsonWriter writer, string idProperty, string id, IEnumerable<KeyValuePair<string, object?>> fields)
    {
        writer.WriteStartObject();
        writer.WriteString(idProperty, id);
        foreach ((string key, object? value) in fields)
        {
            if (key == idProperty) continue;
            writer.WritePropertyName(key);
            WriteValue(writer, value, 0);
        }
        writer.WriteEndObject();
    }

    /// <summary>Writes a value-model value as JSON.</summary>
    public static void WriteValue(Utf8JsonWriter writer, object? value) => WriteValue(writer, value, 0);

    private static void WriteValue(Utf8JsonWriter writer, object? value, int depth)
    {
        // Values are written by peers, so a hostile one could nest without end; the libraries' readers refuse far
        // shallower documents than this anyway.
        if (depth > 128)
        {
            writer.WriteNullValue();
            return;
        }

        switch (value)
        {
            case null:
                writer.WriteNullValue();
                break;
            case string text:
                writer.WriteStringValue(text);
                break;
            case bool flag:
                writer.WriteBooleanValue(flag);
                break;
            case long integer:
                writer.WriteNumberValue(integer);
                break;
            case int integer:
                writer.WriteNumberValue(integer);
                break;
            // JSON has no NaN or infinity. A peer could still store one; it reads as null, the same on every
            // replica, rather than failing the whole change.
            case double number when double.IsFinite(number):
                writer.WriteNumberValue(number);
                break;
            case float number when float.IsFinite(number):
                writer.WriteNumberValue(number);
                break;
            case double or float:
                writer.WriteNullValue();
                break;
            case decimal number:
                writer.WriteNumberValue(number);
                break;
            case byte[] bytes:
                writer.WriteBase64StringValue(bytes);
                break;
            case DateTimeOffset timestamp:
                writer.WriteStringValue(timestamp);
                break;
            case Guid guid:
                writer.WriteStringValue(guid);
                break;
            case IEnumerable<KeyValuePair<string, object?>> map:
                writer.WriteStartObject();
                foreach ((string key, object? item) in map)
                {
                    writer.WritePropertyName(key);
                    WriteValue(writer, item, depth + 1);
                }
                writer.WriteEndObject();
                break;
            case IEnumerable list:
                writer.WriteStartArray();
                foreach (object? item in list) WriteValue(writer, item, depth + 1);
                writer.WriteEndArray();
                break;
            default:
                writer.WriteStringValue(Convert.ToString(value, CultureInfo.InvariantCulture));
                break;
        }
    }
}
