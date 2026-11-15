using System.Text.Json.Serialization;

namespace Bit.BlazorUI;

/// <summary>
/// Writes one of the package's enumerations as a camelCase string, as <see cref="JsonStringEnumConverter{TEnum}"/> does,
/// and reads a value this version does not know - a member a newer server or a newer snapshot added - as
/// <paramref name="fallback"/> rather than failing what holds it.
/// </summary>
/// <param name="fallback">What a value this version does not know reads as.</param>
/// <remarks>
/// The wire format and the snapshots change by addition only, and a reader skips what it does not know: a new member of
/// a status, a finish reason or an error kind is an addition, so an older reader takes it for the member that says the
/// least (<c>Unknown</c> where the enumeration has one) instead of losing the whole event or conversation. A value that is
/// neither a string nor a number is still an error.
/// </remarks>
internal sealed class BitAiLenientEnumConverter<TEnum>(TEnum fallback) : JsonConverter<TEnum> where TEnum : struct, Enum
{
    private readonly JsonStringEnumConverter<TEnum> _factory = new(JsonNamingPolicy.CamelCase);
    private Cached? _inner;

    public override TEnum Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType is not (JsonTokenType.String or JsonTokenType.Number))
        {
            return Inner(options).Read(ref reader, typeToConvert, options);
        }

        try
        {
            // A string or a number is one token, so the reader is where it has to be whether the read succeeds or not.
            var value = Inner(options).Read(ref reader, typeToConvert, options);
            return Enum.IsDefined(value) ? value : fallback;
        }
        catch (JsonException)
        {
            return fallback;
        }
    }

    public override void Write(Utf8JsonWriter writer, TEnum value, JsonSerializerOptions options) =>
        Inner(options).Write(writer, value, options);

    public override TEnum ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        Inner(options).ReadAsPropertyName(ref reader, typeToConvert, options);

    public override void WriteAsPropertyName(Utf8JsonWriter writer, TEnum value, JsonSerializerOptions options) =>
        Inner(options).WriteAsPropertyName(writer, value, options);

    private JsonConverter<TEnum> Inner(JsonSerializerOptions options)
    {
        if (_inner is { } inner && ReferenceEquals(inner.Options, options)) return inner.Converter;

        var converter = (JsonConverter<TEnum>)_factory.CreateConverter(typeof(TEnum), options)!;
        _inner = new Cached(options, converter);
        return converter;
    }

    // One reference, so a thread never reads the converter of one settings paired with another's.
    private sealed record Cached(JsonSerializerOptions Options, JsonConverter<TEnum> Converter);
}
