using System.Buffers;
using System.Collections;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace Bit.Bcrdt.Demo.Samples;

/// <summary>
/// Writes the plain-object projection <c>ToJson()</c> returns as indented JSON.
/// </summary>
/// <remarks>
/// The projection is a tree of <c>object?</c> whose leaves are whatever the document holds, so this walks the
/// handful of shapes it can take instead of handing it to the reflection-based <c>JsonSerializer</c> - which a
/// trimmed WebAssembly or native AOT app cannot rely on. Binary content (a saved version's snapshot, say) is
/// summarized rather than dumped as base64.
/// </remarks>
public static class ProjectionJson
{
    // The text is shown, not embedded in a page or a script, so nothing beyond JSON's own escaping is needed.
    private static readonly JsonWriterOptions Options = new() { Indented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    public static string Format(object? projection)
    {
        var buffer = new ArrayBufferWriter<byte>();
        using (var writer = new Utf8JsonWriter(buffer, Options)) Write(writer, projection);
        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    private static void Write(Utf8JsonWriter writer, object? value)
    {
        switch (value)
        {
            case null: writer.WriteNullValue(); break;
            case string text: writer.WriteStringValue(text); break;
            case bool flag: writer.WriteBooleanValue(flag); break;
            case long number: writer.WriteNumberValue(number); break;
            case int number: writer.WriteNumberValue(number); break;
            case double number: writer.WriteNumberValue(number); break;
            case float number: writer.WriteNumberValue(number); break;
            case decimal number: writer.WriteNumberValue(number); break;
            case DateTimeOffset at: writer.WriteStringValue(at); break;
            case Guid id: writer.WriteStringValue(id); break;
            case byte[] bytes: writer.WriteStringValue($"{bytes.Length} bytes"); break;

            case IEnumerable<KeyValuePair<string, object?>> map:
                writer.WriteStartObject();
                foreach ((string key, object? item) in map)
                {
                    writer.WritePropertyName(key);
                    Write(writer, item);
                }
                writer.WriteEndObject();
                break;

            case IEnumerable list:
                writer.WriteStartArray();
                foreach (object? item in list) Write(writer, item);
                writer.WriteEndArray();
                break;

            default: writer.WriteStringValue(value.ToString()); break;
        }
    }
}
