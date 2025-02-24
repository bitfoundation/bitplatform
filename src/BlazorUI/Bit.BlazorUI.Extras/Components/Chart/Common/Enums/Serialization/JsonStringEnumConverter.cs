using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Newtonsoft.Json;

namespace Bit.BlazorUI;

internal class JsonStringEnumConverter : JsonConverter<BitChartStringEnum>
{
    private static readonly Type[] _stringParameterArray = [typeof(string)];
    private static readonly ConcurrentDictionary<Type, ConstructorInfo?> _constructorCache = new();

    [SuppressMessage("Trimming", "IL2111:Method with parameters or return value with `DynamicallyAccessedMembersAttribute` is accessed via reflection. Trimmer can't guarantee availability of the requirements of the method.", Justification = "<Pending>")]
    public override BitChartStringEnum? ReadJson(JsonReader reader, Type objectType, [AllowNull] BitChartStringEnum existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        switch (reader.TokenType)
        {
            case JsonToken.Null:
            case JsonToken.Undefined:
                return null;
            case JsonToken.String:
                var constructor = _constructorCache.GetOrAdd(objectType, GetStringConstructor);

                return (BitChartStringEnum?)constructor?.Invoke([reader.Value]);
            default:
                throw new NotSupportedException($"Deserializing StringEnums from token type '{reader.TokenType}' isn't supported.");
        }
    }

    public override void WriteJson(JsonWriter writer, BitChartStringEnum? value, JsonSerializer serializer)
    {
        ArgumentNullException.ThrowIfNull(value);

        // Note: value won't be null (json.net wouldn't call this method if it were null)
        // ToString was overwritten by StringEnum -> safe to just print the string representation
        writer.WriteValue(value.ToString());
    }

    private ConstructorInfo? GetStringConstructor([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicConstructors)] Type type) =>
        type.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, _stringParameterArray, null);
}
