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
                    var name = reader.GetString()!;
                    reader.Read();
                    headers.Append(name, reader.GetString() ?? string.Empty);
                }
                return headers;

            case JsonTokenType.StartArray:
                while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                {
                    if (reader.TokenType != JsonTokenType.StartArray)
                        throw new JsonException("Expected a [name, value] pair in the header list.");

                    reader.Read();
                    var name = reader.GetString()!;
                    reader.Read();
                    headers.Append(name, reader.GetString() ?? string.Empty);

                    // Anything past the second element is not part of the pair; skip to its end so
                    // a longer array is tolerated rather than derailing the rest of the payload.
                    while (reader.Read() && reader.TokenType != JsonTokenType.EndArray) { }
                }
                return headers;

            default:
                throw new JsonException($"Cannot read headers from a {reader.TokenType} token.");
        }
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
