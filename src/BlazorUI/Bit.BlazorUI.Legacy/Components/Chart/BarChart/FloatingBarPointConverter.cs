using Newtonsoft.Json;

namespace Bit.BlazorUI.Legacy;

internal class FloatingBarPointConverter : JsonConverter<BitChartLegacyFloatingBarPoint>
{
    public override BitChartLegacyFloatingBarPoint ReadJson(JsonReader reader, Type objectType, BitChartLegacyFloatingBarPoint existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        if (reader.TokenType != JsonToken.StartArray)
            throw new JsonReaderException();

        var arr = serializer.Deserialize<double[]>(reader);
        if (arr!.Length != 2)
            throw new JsonReaderException();

        return new BitChartLegacyFloatingBarPoint(arr[0], arr[1]);
    }

    public override void WriteJson(JsonWriter writer, BitChartLegacyFloatingBarPoint value, JsonSerializer serializer)
    {
        writer.WriteStartArray();
        writer.WriteValue(value.Start);
        writer.WriteValue(value.End);
        writer.WriteEndArray();
    }
}
