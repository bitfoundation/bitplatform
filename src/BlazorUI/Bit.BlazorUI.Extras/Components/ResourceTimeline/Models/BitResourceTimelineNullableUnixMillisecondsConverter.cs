using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Bit.BlazorUI;

/// <summary>
/// The nullable form of <see cref="BitResourceTimelineUnixMillisecondsConverter"/>: a set value is written as Unix
/// milliseconds, and <c>null</c> stays a JSON null.
/// </summary>
internal sealed class BitResourceTimelineNullableUnixMillisecondsConverter : JsonConverter<DateTimeOffset?>
{
    public override DateTimeOffset? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null) return null;

        var ms = reader.TokenType == JsonTokenType.String
            ? long.Parse(reader.GetString()!, CultureInfo.InvariantCulture)
            : reader.GetInt64();

        return DateTimeOffset.FromUnixTimeMilliseconds(ms);
    }

    public override void Write(Utf8JsonWriter writer, DateTimeOffset? value, JsonSerializerOptions options)
    {
        if (value is { } time)
        {
            writer.WriteNumberValue(time.ToUnixTimeMilliseconds());
        }
        else
        {
            writer.WriteNullValue();
        }
    }
}
