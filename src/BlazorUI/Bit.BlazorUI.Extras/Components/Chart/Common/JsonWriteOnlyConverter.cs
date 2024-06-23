using Newtonsoft.Json;

namespace Bit.BlazorUI;

internal abstract class JsonWriteOnlyConverter<T> : JsonConverter<T>
{
    public sealed override bool CanRead => false;
    public sealed override bool CanWrite => true;

    public sealed override T ReadJson(JsonReader reader, Type objectType, T existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        throw new NotImplementedException("Don't use me to read JSON");
    }

    public abstract override void WriteJson(JsonWriter writer, T value, JsonSerializer serializer);
}
