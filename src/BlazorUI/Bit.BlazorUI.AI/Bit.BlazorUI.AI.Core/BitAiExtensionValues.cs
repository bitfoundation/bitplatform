using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization.Metadata;

namespace Bit.BlazorUI;

/// <summary>
/// Reads one value of the application-defined <c>Extensions</c> of a <see cref="BitAiMessage"/> or a
/// <see cref="BitAiConversation"/> as a type of the app, whether it is still the live value the app put there or the
/// <see cref="JsonElement"/> a conversation restored from JSON holds in its place.
/// </summary>
internal static class BitAiExtensionValues
{
    private static readonly JsonSerializerOptions WebOptions = new(JsonSerializerDefaults.Web);

    internal static bool TryGet<T>(Dictionary<string, object?> extensions, string key, JsonTypeInfo<T> typeInfo, [MaybeNullWhen(false)] out T value)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(typeInfo);

        return TryGet(extensions, key, element => element.Deserialize(typeInfo), out value);
    }

    [RequiresUnreferencedCode(BitAiTrimming.JsonUnreferencedCode)]
    [RequiresDynamicCode(BitAiTrimming.JsonDynamicCode)]
    internal static bool TryGet<T>(Dictionary<string, object?> extensions, string key, JsonSerializerOptions? options, [MaybeNullWhen(false)] out T value)
    {
        ArgumentNullException.ThrowIfNull(key);

        return TryGet(extensions, key, element => element.Deserialize<T>(options ?? WebOptions), out value);
    }

    // The live value is handed back as it is. A JSON value - what a restored conversation holds - is read as T, and one
    // that does not read as T counts as absent rather than throwing, as a value of another type (or a null) does. The
    // restored value stays the JSON value it is: written back as T, it would make the next serialization depend on the
    // app's type being known to the serializer's options. Options that cannot read T at all still throw: that is a
    // misconfiguration, not a value of another shape.
    private static bool TryGet<T>(Dictionary<string, object?> extensions, string key, Func<JsonElement, T?> read, [MaybeNullWhen(false)] out T value)
    {
        value = default;

        if (extensions.TryGetValue(key, out var raw) is false) return false;

        switch (raw)
        {
            case T typed:
                value = typed;
                return true;

            case JsonElement element when typeof(T) != typeof(JsonElement):
                try
                {
                    if (read(element) is not { } result) return false;

                    value = result;
                    return true;
                }
                catch (JsonException)
                {
                    return false;
                }

            default:
                return false;
        }
    }
}
