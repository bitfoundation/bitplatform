using System.Collections.Concurrent;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json;

public static partial class JsonSerializerOptionsExtensions
{
    private static readonly ConcurrentDictionary<Type, JsonTypeInfo> _typeInfoCache = [];

    /// <summary>
    /// https://devblogs.microsoft.com/dotnet/try-the-new-system-text-json-source-generator/
    /// </summary>
    public static JsonTypeInfo<T> GetTypeInfo<T>(this JsonSerializerOptions options)
    {
        if (options.TryGetTypeInfo(typeof(T), out var result))
        {
            return (JsonTypeInfo<T>)result;
        }

        return (JsonTypeInfo<T>)_typeInfoCache.GetOrAdd(typeof(T), _ => JsonTypeInfo.CreateJsonTypeInfo<T>(options));
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

public class CompositeJsonTypeInfoResolver(params IJsonTypeInfoResolver[] resolvers)
    : IJsonTypeInfoResolver
{
    public JsonTypeInfo? GetTypeInfo(Type type, JsonSerializerOptions options)
    {
        foreach (var resolver in resolvers)
        {
            var typeInfo = resolver.GetTypeInfo(type, options);
            if (typeInfo != null)
            {
                return typeInfo;
            }
        }
        return null;
    }
}
