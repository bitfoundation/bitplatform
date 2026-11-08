using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Bit.BlazorUI;

/// <summary>
/// Serializes a <see cref="DateTimeOffset"/> as Unix time in milliseconds (a JSON number). The public models of the
/// <see cref="BitResourceTimeline"/> expose <see cref="DateTimeOffset"/> while its rendering engine works in
/// milliseconds; this converts at the interop boundary without an intermediate projection.
/// </summary>
internal sealed class BitResourceTimelineUnixMillisecondsConverter : JsonConverter<DateTimeOffset>
{
    public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var ms = reader.TokenType == JsonTokenType.String
            ? long.Parse(reader.GetString()!, CultureInfo.InvariantCulture)
            : reader.GetInt64();

        return DateTimeOffset.FromUnixTimeMilliseconds(ms);
    }

    public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value.ToUnixTimeMilliseconds());
    }
}
