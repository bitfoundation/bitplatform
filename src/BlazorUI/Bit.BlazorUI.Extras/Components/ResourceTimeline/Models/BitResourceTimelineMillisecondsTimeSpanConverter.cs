using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Bit.BlazorUI;

/// <summary>
/// Serializes a <see cref="TimeSpan"/> as a whole number of milliseconds (a JSON number), the duration representation
/// the rendering engine of the <see cref="BitResourceTimeline"/> expects.
/// </summary>
internal sealed class BitResourceTimelineMillisecondsTimeSpanConverter : JsonConverter<TimeSpan>
{
    public override TimeSpan Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var ms = reader.TokenType == JsonTokenType.String
            ? long.Parse(reader.GetString()!, CultureInfo.InvariantCulture)
            : reader.GetInt64();

        return TimeSpan.FromMilliseconds(ms);
    }

    public override void Write(Utf8JsonWriter writer, TimeSpan value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue((long)value.TotalMilliseconds);
    }
}
