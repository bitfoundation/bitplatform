using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Bit.BlazorUI;

/// <summary>
/// Reads a <see cref="BitVirtualKeyboardRow"/> from a shorthand string or from an object with its keys, and writes it as
/// an object, which keeps every value of its keys.
/// </summary>
internal sealed class BitVirtualKeyboardRowJsonConverter : JsonConverter<BitVirtualKeyboardRow>
{
    public override BitVirtualKeyboardRow? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null) return null;

        if (reader.TokenType == JsonTokenType.String)
        {
            try
            {
                return BitVirtualKeyboardShorthand.ParseRow(reader.GetString()!);
            }
            catch (FormatException e)
            {
                throw new JsonException(e.Message, e);
            }
        }

        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("A row is a shorthand string or an object with its keys.");
        }

        var keysInfo = (JsonTypeInfo<List<BitVirtualKeyboardKey>>)options.GetTypeInfo(typeof(List<BitVirtualKeyboardKey>));
        var alignInfo = (JsonTypeInfo<BitVirtualKeyboardRowAlign>)options.GetTypeInfo(typeof(BitVirtualKeyboardRowAlign));

        var keys = new List<BitVirtualKeyboardKey>();
        var align = BitVirtualKeyboardRowAlign.Center;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                return new BitVirtualKeyboardRow { Keys = keys, Align = align };
            }

            if (reader.TokenType != JsonTokenType.PropertyName) throw new JsonException("A property name was expected in a row.");

            var name = reader.GetString();
            reader.Read();

            if (string.Equals(name, "keys", StringComparison.OrdinalIgnoreCase))
            {
                keys = JsonSerializer.Deserialize(ref reader, keysInfo) ?? [];
            }
            else if (string.Equals(name, "align", StringComparison.OrdinalIgnoreCase))
            {
                align = JsonSerializer.Deserialize(ref reader, alignInfo);
            }
            else
            {
                reader.Skip();
            }
        }

        throw new JsonException("A row object was not closed.");
    }

    public override void Write(Utf8JsonWriter writer, BitVirtualKeyboardRow value, JsonSerializerOptions options)
    {
        var keysInfo = (JsonTypeInfo<List<BitVirtualKeyboardKey>>)options.GetTypeInfo(typeof(List<BitVirtualKeyboardKey>));
        var alignInfo = (JsonTypeInfo<BitVirtualKeyboardRowAlign>)options.GetTypeInfo(typeof(BitVirtualKeyboardRowAlign));

        writer.WriteStartObject();
        writer.WritePropertyName(options.PropertyNamingPolicy?.ConvertName("Keys") ?? "Keys");
        JsonSerializer.Serialize(writer, value.Keys, keysInfo);

        if (value.Align != BitVirtualKeyboardRowAlign.Center)
        {
            writer.WritePropertyName(options.PropertyNamingPolicy?.ConvertName("Align") ?? "Align");
            JsonSerializer.Serialize(writer, value.Align, alignInfo);
        }

        writer.WriteEndObject();
    }
}