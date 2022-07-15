using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Newtonsoft.Json;

namespace Bit.BlazorUI;

internal class JsonStringEnumConverter : JsonConverter<StringEnum>
{
    private static readonly Type[] _stringParameterArray = new[] { typeof(string) };
    private static readonly ConcurrentDictionary<Type, ConstructorInfo> _constructorCache = new ConcurrentDictionary<Type, ConstructorInfo>();

    public override StringEnum ReadJson(JsonReader reader, Type objectType, [AllowNull] StringEnum existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        switch (reader.TokenType)
        {
            case JsonToken.Null:
            case JsonToken.Undefined:
                return null;
            case JsonToken.String:
                ConstructorInfo constructor = _constructorCache.GetOrAdd(objectType, GetStringConstructor);

                return (StringEnum)constructor.Invoke(new[] { reader.Value });
            default:
                throw new NotSupportedException($"Deserializing StringEnums from token type '{reader.TokenType}' isn't supported.");
        }
    }

    public override void WriteJson(JsonWriter writer, StringEnum value, JsonSerializer serializer)
    {
        // Note: value won't be null (json.net wouldn't call this method if it were null)
        // ToString was overwritten by StringEnum -> safe to just print the string representation
        writer.WriteValue(value.ToString());
    }

    private ConstructorInfo GetStringConstructor(Type type) =>
        type.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, _stringParameterArray, null);
}
