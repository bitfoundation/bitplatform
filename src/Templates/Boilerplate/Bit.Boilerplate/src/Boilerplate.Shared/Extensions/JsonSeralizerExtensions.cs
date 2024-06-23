using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json;

public static class JsonSerializerOptionsExtensions
{
    /// <summary>
    /// https://devblogs.microsoft.com/dotnet/try-the-new-system-text-json-source-generator/
    /// </summary>
    public static JsonTypeInfo<T> GetTypeInfo<T>(this JsonSerializerOptions options)
    {
        if (options.TryGetTypeInfo(typeof(T), out var result))
        {
            return (JsonTypeInfo<T>)result;
        }

        throw new InvalidOperationException($"Add [JsonSerializable(typeof({GetTypeDisplayName(typeof(T))}))] to the {nameof(AppJsonContext)}");
    }

    private static string GetTypeDisplayName(Type type)
    {
        if (type.IsGenericType)
        {
            return $"{type.Name.Split('`')[0]}<{string.Join(", ", type.GetGenericArguments().Select(GetTypeDisplayName))}>";
        }

        return type.Name;
    }
}
