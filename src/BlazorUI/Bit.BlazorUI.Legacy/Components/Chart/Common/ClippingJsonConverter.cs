using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Bit.BlazorUI.Legacy;

internal class ClippingJsonConverter : JsonConverter<BitChartLegacyClipping>
{
    public override BitChartLegacyClipping ReadJson(JsonReader reader, Type objectType, [AllowNull] BitChartLegacyClipping existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Integer)
            return new BitChartLegacyClipping((int?)(long?)reader.Value);

        if (reader.TokenType != JsonToken.StartObject)
            throw new JsonReaderException();

        JObject obj = JObject.Load(reader);
        int? top = GetClippingValue(obj, nameof(BitChartLegacyClipping.Top));
        int? right = GetClippingValue(obj, nameof(BitChartLegacyClipping.Right));
        int? bottom = GetClippingValue(obj, nameof(BitChartLegacyClipping.Bottom));
        int? left = GetClippingValue(obj, nameof(BitChartLegacyClipping.Left));

        return new BitChartLegacyClipping(top, right, bottom, left);
    }

    private static int? GetClippingValue(JObject obj, string name)
    {
        if (!obj.TryGetValue(name, StringComparison.OrdinalIgnoreCase, out JToken? token))
            return null;

        if (token.Type == JTokenType.Boolean && (bool)token is false)
            return null;

        if (token.Type == JTokenType.Integer)
            return (int)token;

        throw new JsonWriterException();
    }

    public override void WriteJson(JsonWriter writer, [AllowNull] BitChartLegacyClipping value, JsonSerializer serializer)
    {
        if (value._equalSides)
        {
            writer.WriteValue(value.Bottom!.Value);
            return;
        }

        var naming = (serializer.ContractResolver as DefaultContractResolver)?.NamingStrategy;

        writer.WriteStartObject();

        WriteAdjustedName(writer, naming, nameof(BitChartLegacyClipping.Top));
        WriteValueOrFalse(writer, value.Top);

        WriteAdjustedName(writer, naming, nameof(BitChartLegacyClipping.Right));
        WriteValueOrFalse(writer, value.Right);

        WriteAdjustedName(writer, naming, nameof(BitChartLegacyClipping.Bottom));
        WriteValueOrFalse(writer, value.Bottom);

        WriteAdjustedName(writer, naming, nameof(BitChartLegacyClipping.Left));
        WriteValueOrFalse(writer, value.Left);

        writer.WriteEndObject();
    }

    private static void WriteAdjustedName(JsonWriter writer, NamingStrategy? namingStrategy, string name)
    {
        if (namingStrategy != null)
        {
            name = namingStrategy.GetPropertyName(name, false);
        }

        writer.WritePropertyName(name);
    }

    private static void WriteValueOrFalse(JsonWriter writer, int? value)
    {
        if (value.HasValue)
        {
            writer.WriteValue(value.Value);
        }
        else
        {
            writer.WriteValue(false);
        }
    }
}
