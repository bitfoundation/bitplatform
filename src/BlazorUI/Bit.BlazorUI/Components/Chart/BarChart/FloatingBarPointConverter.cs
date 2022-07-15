using Newtonsoft.Json;

namespace Bit.BlazorUI;

internal class FloatingBarPointConverter : JsonConverter<FloatingBarPoint>
{
    public override FloatingBarPoint ReadJson(JsonReader reader, Type objectType, FloatingBarPoint existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        if (reader.TokenType != JsonToken.StartArray)
            throw new JsonReaderException();

        double[] arr = serializer.Deserialize<double[]>(reader);
        if (arr.Length != 2)
            throw new JsonReaderException();

        return new FloatingBarPoint(arr[0], arr[1]);
    }

    public override void WriteJson(JsonWriter writer, FloatingBarPoint value, JsonSerializer serializer)
    {
        writer.WriteStartArray();
        writer.WriteValue(value.Start);
        writer.WriteValue(value.End);
        writer.WriteEndArray();
    }
}
