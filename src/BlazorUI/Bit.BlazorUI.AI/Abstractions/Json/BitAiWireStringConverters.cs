using System.Text.Json.Serialization;

namespace Bit.BlazorUI;

/// <summary>
/// Serializes <see cref="BitAiRole"/> as its wire string (<c>"user"</c>) rather than as an object.
/// </summary>
public sealed class BitAiRoleJsonConverter : JsonConverter<BitAiRole>
{
    /// <inheritdoc />
    public override BitAiRole Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        return string.IsNullOrWhiteSpace(value) ? BitAiRole.User : new BitAiRole(value);
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, BitAiRole value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.Value);
}

/// <summary>
/// Serializes <see cref="BitAiArtifactKind"/> as its wire string (<c>"code"</c>) rather than as an object.
/// </summary>
public sealed class BitAiArtifactKindJsonConverter : JsonConverter<BitAiArtifactKind>
{
    /// <inheritdoc />
    public override BitAiArtifactKind Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        return string.IsNullOrWhiteSpace(value) ? BitAiArtifactKind.Document : new BitAiArtifactKind(value);
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, BitAiArtifactKind value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.Value);
}
