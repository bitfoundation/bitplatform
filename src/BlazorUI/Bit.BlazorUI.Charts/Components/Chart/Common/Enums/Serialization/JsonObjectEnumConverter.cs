using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;

namespace Bit.BlazorUI;

internal class JsonObjectEnumConverter : JsonConverter<BitChartObjectEnum>
{
    public override BitChartObjectEnum ReadJson(JsonReader reader, Type objectType, [AllowNull] BitChartObjectEnum existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null ||
            reader.TokenType == JsonToken.Undefined)
        {
            return null;
        }

        if (reader.Value == null)
        {
            /* Covers all token types that result in reader.Value not being assigned (yet) except null and undefined
             * Examples are: StartArray, StartObject, ..
             */
            throw new NotSupportedException($"Deserializing ObjectEnums from token type '{reader.TokenType}' isn't supported.");
        }

        object value = reader.Value;
        Type readerValueType = value.GetType();
        /* The Type of reader.Value isn't always what it was when it was serialized.
         * An issue pointing that out: https://github.com/dotnet/orleans/issues/1269#issuecomment-171233788
         * - Any integer number will be of type System.Int64 unless its smaller
         *   than Int64.MinValue or higher than Int64.MaxValue, then it will be of type System.Numerics.BigInteger
         * - Any non-integer number will be of type System.Double
         *
         * Currently we cast down long to int and ignore BigInteger. This means that only int is supported
         * and we don't waste time checking for other options and converting between types.
         *
         * Another option would be to check for a suitable constructor and if there isn't one try to find the
         * most optimal conversion. Even though that sounds nice, it's really not necessary at the moment. There's
         * no need for BigInteger support in the enums and it would hurt performance a bit.
         */

        ObjectEnumFactory factory = ObjectEnumFactory.GetFactory(objectType);

        // special case for long since json.net's default for number deserialization is long but our enums
        // use (and only support) int at the moment. BigInteger isn't supported at all and will throw on the next check.
        if (readerValueType == typeof(long))
        {
            long asLong = (long)value;
            if (asLong < int.MinValue ||
                asLong > int.MaxValue)
            {
                throw new OverflowException($"The deserialized number ({value}) is out of the range of int ({int.MinValue} - {int.MaxValue}).");
            }
            else
            {
                value = (int)asLong;
                readerValueType = typeof(int);
            }
        }

        if (!factory.CanConvertFrom(readerValueType))
        {
            if (readerValueType == typeof(int) &&
                factory.CanConvertFrom(typeof(double)))
            {
                /* Sometimes a value like "0" or "10" might get deserialized as int even though
                 * the ObjectEnum meant to handle a double. In that case, we can convert
                 * the int value to a double and create the enum value from there.
                 */
                value = (double)(int)value; // both casts are required! (else InvalidCastException)
                readerValueType = typeof(double);
            }
            else
            {
                throw new NotSupportedException($"Deserialization {nameof(BitChartObjectEnum)} '{objectType.FullName}' from '{readerValueType.Name}' isn't supported.");
            }
        }

        return factory.Create(value, readerValueType);
    }

    public override void WriteJson(JsonWriter writer, BitChartObjectEnum wrapper, JsonSerializer serializer)
    {
        // Note: wrapper won't be null (json.net wouldn't call this method if it were null)
        Type wrappedType = wrapper.Value.GetType();
        if (!BitChartObjectEnum.IsSupportedSerializationType(wrappedType))
        {
            throw new NotSupportedException($"The type '{wrappedType.FullName}' isn't supported for serialization " +
                                            $"within an instance of any {nameof(BitChartObjectEnum)}-type.");
        }

        // The types we support can always be written in a single Token.
        // If that was not the case, we'd need to handle JsonWriterException here.
        writer.WriteValue(wrapper.Value);
    }
}
