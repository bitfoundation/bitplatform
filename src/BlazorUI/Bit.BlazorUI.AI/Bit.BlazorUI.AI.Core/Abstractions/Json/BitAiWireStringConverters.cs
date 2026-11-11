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

/// <summary>
/// Serializes <see cref="BitAiToolChoice"/> as <c>{"mode":"auto"}</c>, or <c>{"mode":"specific","toolName":"search"}</c>
/// for a specific tool; a bare mode string (<c>"auto"</c>) reads too.
/// </summary>
/// <remarks>
/// The choice has no public constructor, so it is read back through its own factories: an unknown mode reads as
/// <see cref="BitAiToolChoice.Auto"/>, and <c>specific</c> without a tool name fails.
/// </remarks>
public sealed class BitAiToolChoiceJsonConverter : JsonConverter<BitAiToolChoice>
{
    /// <inheritdoc />
    public override BitAiToolChoice Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        string? mode;
        string? toolName = null;

        if (reader.TokenType is JsonTokenType.String)
        {
            mode = reader.GetString();
        }
        else
        {
            var json = JsonElement.ParseValue(ref reader);
            if (json.ValueKind is not JsonValueKind.Object) throw new JsonException("A BitAiToolChoice is a string or an object.");
            mode = json.TryGetProperty("mode", out var m) && m.ValueKind is JsonValueKind.String ? m.GetString() : null;
            toolName = json.TryGetProperty("toolName", out var t) && t.ValueKind is JsonValueKind.String ? t.GetString() : null;
        }

        return mode switch
        {
            "none" => BitAiToolChoice.None,
            "required" => BitAiToolChoice.Required,
            "specific" when !string.IsNullOrWhiteSpace(toolName) => BitAiToolChoice.Specific(toolName),
            "specific" => throw new JsonException("A specific BitAiToolChoice names its tool in 'toolName'."),
            _ => BitAiToolChoice.Auto,
        };
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, BitAiToolChoice value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("mode", value.Mode ?? BitAiToolChoice.Auto.Mode);
        if (value.ToolName is { } toolName) writer.WriteString("toolName", toolName);
        writer.WriteEndObject();
    }
}
