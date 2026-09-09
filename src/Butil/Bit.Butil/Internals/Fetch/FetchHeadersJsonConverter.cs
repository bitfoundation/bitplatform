using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Bit.Butil;

/// <summary>
/// Moves <see cref="FetchHeaders"/> across the interop boundary as an array of
/// <c>["name", "value"]</c> pairs - the shape <c>Headers</c> itself iterates as, and the only one
/// that survives a repeated header. A JSON object is accepted on the way in as well, so a payload
/// written when this was a dictionary still reads.
/// </summary>
internal sealed class FetchHeadersJsonConverter : JsonConverter<FetchHeaders>
{
    public override FetchHeaders Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var headers = new FetchHeaders();

        switch (reader.TokenType)
        {
            case JsonTokenType.Null:
                return headers;

            case JsonTokenType.StartObject:
                while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
                {
                    // A property name is always a string token, so it needs none of the guarding the
                    // value does - it is the value that a hand-written payload can get wrong.
                    var name = reader.GetString()!;
                    reader.Read();
                    headers.Append(name, ReadHeaderValue(ref reader));
                }
                return headers;

            case JsonTokenType.StartArray:
                while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                {
                    if (reader.TokenType != JsonTokenType.StartArray)
                        throw new JsonException("Expected a [name, value] pair in the header list.");

                    reader.Read();
                    var name = ReadHeaderName(ref reader);
                    reader.Read();
                    headers.Append(name, ReadHeaderValue(ref reader));

                    // Anything past the second element is not part of the pair; skip to its end so
                    // a longer array is tolerated rather than derailing the rest of the payload.
                    // Skip() is what steps over a nested array or object whole - reading one token
                    // at a time would leave the reader inside it and break the next pair.
                    while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                    {
                        reader.Skip();
                    }
                }
                return headers;

            default:
                throw new JsonException($"Cannot read headers from a {reader.TokenType} token.");
        }
    }

    /// <summary>
    /// The name half of a pair. It has to be a string and it has to be non-empty - <c>Append</c>
    /// rejects an empty name, and doing so from inside a converter as an <c>ArgumentException</c>
    /// would surface as something other than a malformed payload.
    /// </summary>
    private static string ReadHeaderName(ref Utf8JsonReader reader)
    {
        if (reader.TokenType != JsonTokenType.String)
            throw Malformed(ref reader, "a header name");

        var name = reader.GetString();
        return string.IsNullOrEmpty(name)
            ? throw new JsonException("A header name in the header list is empty.")
            : name;
    }

    /// <summary>
    /// The value half. <c>null</c> reads as the empty string - the distinction a header list draws
    /// is present versus absent, and a name that is there with no value is present.
    /// </summary>
    private static string ReadHeaderValue(ref Utf8JsonReader reader)
        => reader.TokenType switch
        {
            JsonTokenType.String => reader.GetString() ?? string.Empty,
            JsonTokenType.Null => string.Empty,
            _ => throw Malformed(ref reader, "a header value")
        };

    /// <summary>
    /// The failure both of the above raise. <see cref="JsonException"/> rather than whatever
    /// <c>GetString()</c> would have thrown: it is the exception System.Text.Json expects from a
    /// converter, and the only one it decorates with the path to the member that failed. The token is
    /// skipped first so the reader is left past a nested object or array rather than inside it -
    /// nothing here catches, but a converter that desyncs the reader on the way out is a trap for
    /// anything that later does.
    /// </summary>
    private static JsonException Malformed(ref Utf8JsonReader reader, string what)
    {
        var found = reader.TokenType;
        reader.Skip();

        return new JsonException($"Expected {what} to be a string in the header list, found {found}.");
    }

    public override void Write(Utf8JsonWriter writer, FetchHeaders value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();

        foreach (var entry in value)
        {
            writer.WriteStartArray();
            writer.WriteStringValue(entry.Key);
            writer.WriteStringValue(entry.Value);
            writer.WriteEndArray();
        }

        writer.WriteEndArray();
    }
}
