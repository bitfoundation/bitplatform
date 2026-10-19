using System.Buffers;
using System.Text;
using System.Text.Json;

namespace Bit.Bink;

/// <summary>
/// Serializes <see cref="BinkDrawingOperation"/> values so they can travel between participants.
/// </summary>
/// <remarks>
/// Operations are encoded with the same element format the document serializer uses, so the wire format is
/// versioned and validated exactly like a saved file. Only the changed elements are sent: drawing a stroke
/// transmits that stroke, never the document.
/// </remarks>
public static class BinkOperationCodec
{
    /// <summary>Serializes an operation to JSON.</summary>
    public static string Serialize(BinkDrawingOperation operation, BinkSerializerOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(operation);
        var buffer = new ArrayBufferWriter<byte>(512);
        using (var writer = new Utf8JsonWriter(buffer, new JsonWriterOptions { SkipValidation = true }))
        {
            Write(writer, operation, options ?? BinkSerializerOptions.Default);
            writer.Flush();
        }
        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    private static void Write(Utf8JsonWriter writer, BinkDrawingOperation operation, BinkSerializerOptions options)
    {
        writer.WriteStartObject();
        switch (operation)
        {
            case BinkAddElementOperation add:
                writer.WriteString("op", "add");
                writer.WriteString("layer", add.LayerId);
                if (add.Index is { } index) writer.WriteNumber("index", index);
                writer.WritePropertyName("element");
                BinkSerializer.WriteElementTo(writer, add.Element, options);
                break;
            case BinkRemoveElementOperation remove:
                writer.WriteString("op", "remove");
                writer.WriteString("element", remove.ElementId);
                break;
            case BinkReplaceElementOperation replace:
                writer.WriteString("op", "replace");
                writer.WritePropertyName("element");
                BinkSerializer.WriteElementTo(writer, replace.Element, options);
                break;
            case BinkReorderElementOperation reorder:
                writer.WriteString("op", "reorder");
                writer.WriteString("element", reorder.ElementId);
                writer.WriteString("layer", reorder.TargetLayerId);
                if (reorder.Index is { } target) writer.WriteNumber("index", target);
                break;
            case BinkAddLayerOperation addLayer:
                writer.WriteString("op", "addLayer");
                writer.WriteString("layer", addLayer.LayerId);
                if (addLayer.Index is { } layerIndex) writer.WriteNumber("index", layerIndex);
                WriteLayerProperties(writer, addLayer.Properties);
                if (addLayer.Elements is { Count: > 0 })
                {
                    writer.WritePropertyName("elements");
                    writer.WriteStartArray();
                    foreach (BinkElement element in addLayer.Elements) BinkSerializer.WriteElementTo(writer, element, options);
                    writer.WriteEndArray();
                }
                break;
            case BinkRemoveLayerOperation removeLayer:
                writer.WriteString("op", "removeLayer");
                writer.WriteString("layer", removeLayer.LayerId);
                break;
            case BinkUpdateLayerOperation updateLayer:
                writer.WriteString("op", "updateLayer");
                writer.WriteString("layer", updateLayer.LayerId);
                WriteLayerProperties(writer, updateLayer.Properties);
                break;
            case BinkReorderLayerOperation reorderLayer:
                writer.WriteString("op", "reorderLayer");
                writer.WriteString("layer", reorderLayer.LayerId);
                writer.WriteNumber("index", reorderLayer.Index);
                break;
            case BinkUpdateSettingsOperation settings:
                writer.WriteString("op", "settings");
                writer.WriteNumber("width", settings.Settings.Width);
                writer.WriteNumber("height", settings.Settings.Height);
                writer.WriteString("background", settings.Settings.Background.ToCss());
                break;
            case BinkSetMetadataOperation metadata:
                writer.WriteString("op", "metadata");
                writer.WriteString("key", metadata.Key);
                if (metadata.Value is null) writer.WriteNull("value");
                else writer.WriteString("value", metadata.Value);
                break;
            case BinkBatchOperation batch:
                writer.WriteString("op", "batch");
                if (batch.Label is { Length: > 0 }) writer.WriteString("label", batch.Label);
                writer.WritePropertyName("operations");
                writer.WriteStartArray();
                foreach (BinkDrawingOperation inner in batch.Operations) Write(writer, inner, options);
                writer.WriteEndArray();
                break;
            default:
                throw new InvalidOperationException($"{operation.GetType().Name} cannot be sent to other participants. Custom operations must be translated by the application.");
        }
        writer.WriteEndObject();
    }

    private static void WriteLayerProperties(Utf8JsonWriter writer, BinkLayerProperties properties)
    {
        writer.WritePropertyName("properties");
        writer.WriteStartObject();
        writer.WriteString("name", properties.Name);
        writer.WriteBoolean("visible", properties.IsVisible);
        writer.WriteBoolean("locked", properties.IsLocked);
        writer.WriteNumber("opacity", properties.Opacity);
        // A layer update replaces the whole property set, so metadata left off the wire would be erased on every
        // other replica by something as small as renaming the layer.
        if (properties.Metadata is { Count: > 0 } metadata)
        {
            writer.WritePropertyName("metadata");
            writer.WriteStartObject();
            foreach ((string key, string value) in metadata.OrderBy(static kv => kv.Key, StringComparer.Ordinal)) writer.WriteString(key, value);
            writer.WriteEndObject();
        }
        writer.WriteEndObject();
    }

    /// <summary>Parses an operation previously produced by <see cref="Serialize"/>.</summary>
    /// <exception cref="BinkFormatException">The JSON is not a valid operation.</exception>
    public static BinkDrawingOperation Deserialize(string json, BinkDeserializerOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(json);
        options ??= BinkDeserializerOptions.Default;
        try
        {
            using JsonDocument document = JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = 32 });
            return Read(document.RootElement, options);
        }
        catch (JsonException ex)
        {
            throw new BinkFormatException("The operation is not valid JSON.", ex);
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException or FormatException or OverflowException)
        {
            // A value of the wrong JSON kind, such as a number where a string belongs, makes JsonElement throw
            // InvalidOperationException. The session only expects InkFormatException from an untrusted payload,
            // so anything else would escape the receive callback and take the transport down with it.
            throw new BinkFormatException("The operation is malformed: " + ex.Message, ex);
        }
    }

    private static BinkDrawingOperation Read(JsonElement element, BinkDeserializerOptions options)
    {
        if (element.ValueKind != JsonValueKind.Object) throw new BinkFormatException("An operation must be a JSON object.");
        string kind = element.TryGetProperty("op", out JsonElement op) ? op.GetString() ?? string.Empty : string.Empty;

        switch (kind)
        {
            case "add":
            {
                BinkElement item = ReadElement(element, "element", options);
                return new BinkAddElementOperation(ReadGuid(element, "layer"), item, ReadInt(element, "index"));
            }
            case "remove":
                return new BinkRemoveElementOperation(ReadGuid(element, "element"));
            case "replace":
                return new BinkReplaceElementOperation(ReadElement(element, "element", options));
            case "reorder":
                return new BinkReorderElementOperation(ReadGuid(element, "element"), ReadGuid(element, "layer"), ReadInt(element, "index"));
            case "addLayer":
            {
                List<BinkElement>? elements = null;
                if (element.TryGetProperty("elements", out JsonElement array) && array.ValueKind == JsonValueKind.Array)
                {
                    elements = new List<BinkElement>();
                    foreach (JsonElement e in array.EnumerateArray())
                    {
                        BinkElement? parsed = BinkSerializer.ReadElementFrom(e, options);
                        if (parsed is not null) elements.Add(parsed);
                    }
                }
                return new BinkAddLayerOperation(ReadGuid(element, "layer"), ReadLayerProperties(element), ReadInt(element, "index"), elements);
            }
            case "removeLayer":
                return new BinkRemoveLayerOperation(ReadGuid(element, "layer"));
            case "updateLayer":
                return new BinkUpdateLayerOperation(ReadGuid(element, "layer"), ReadLayerProperties(element));
            case "reorderLayer":
                return new BinkReorderLayerOperation(ReadGuid(element, "layer"), ReadInt(element, "index") ?? 0);
            case "settings":
                return new BinkUpdateSettingsOperation(new BinkDocumentSettings
                {
                    Width = ReadFloat(element, "width", 800f),
                    Height = ReadFloat(element, "height", 600f),
                    Background = element.TryGetProperty("background", out JsonElement bg) && BinkColor.TryParse(bg.GetString(), out BinkColor color) ? color : BinkColor.White,
                });
            case "metadata":
            {
                string key = element.TryGetProperty("key", out JsonElement k) ? k.GetString() ?? string.Empty : string.Empty;
                string? value = element.TryGetProperty("value", out JsonElement v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
                if (key.Length == 0) throw new BinkFormatException("A metadata operation must have a key.");
                return new BinkSetMetadataOperation(key, value);
            }
            case "batch":
            {
                var operations = new List<BinkDrawingOperation>();
                if (element.TryGetProperty("operations", out JsonElement array) && array.ValueKind == JsonValueKind.Array)
                {
                    foreach (JsonElement e in array.EnumerateArray()) operations.Add(Read(e, options));
                }
                string? label = element.TryGetProperty("label", out JsonElement l) ? l.GetString() : null;
                return new BinkBatchOperation(operations, label);
            }
            default:
                throw new BinkFormatException($"Unknown operation \"{kind}\".");
        }
    }

    private static BinkElement ReadElement(JsonElement element, string name, BinkDeserializerOptions options)
    {
        if (!element.TryGetProperty(name, out JsonElement value)) throw new BinkFormatException($"The operation is missing its \"{name}\".");
        return BinkSerializer.ReadElementFrom(value, options) ?? throw new BinkFormatException("The operation carries an element this version cannot read.");
    }

    private static BinkLayerProperties ReadLayerProperties(JsonElement element)
    {
        if (!element.TryGetProperty("properties", out JsonElement properties) || properties.ValueKind != JsonValueKind.Object) return new BinkLayerProperties();
        return new BinkLayerProperties
        {
            Name = properties.TryGetProperty("name", out JsonElement n) ? n.GetString() ?? "Layer" : "Layer",
            IsVisible = !properties.TryGetProperty("visible", out JsonElement v) || v.ValueKind != JsonValueKind.False,
            IsLocked = properties.TryGetProperty("locked", out JsonElement l) && l.ValueKind == JsonValueKind.True,
            Opacity = properties.TryGetProperty("opacity", out JsonElement o) && o.TryGetSingle(out float opacity) ? Math.Clamp(opacity, 0f, 1f) : 1f,
            Metadata = ReadMetadata(properties),
        };
    }

    private static Dictionary<string, string>? ReadMetadata(JsonElement element)
    {
        if (!element.TryGetProperty("metadata", out JsonElement metadata) || metadata.ValueKind != JsonValueKind.Object) return null;
        Dictionary<string, string>? result = null;
        foreach (JsonProperty property in metadata.EnumerateObject())
        {
            if (property.Name.Length == 0 || property.Value.ValueKind != JsonValueKind.String) continue;
            result ??= new Dictionary<string, string>(StringComparer.Ordinal);
            result[property.Name] = property.Value.GetString()!;
        }
        return result;
    }

    private static Guid ReadGuid(JsonElement element, string name)
        => element.TryGetProperty(name, out JsonElement value) && value.ValueKind == JsonValueKind.String && value.TryGetGuid(out Guid guid)
            ? guid
            : throw new BinkFormatException($"The operation is missing a valid \"{name}\" identifier.");

    private static int? ReadInt(JsonElement element, string name)
        => element.TryGetProperty(name, out JsonElement value) && value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out int result) ? result : null;

    private static float ReadFloat(JsonElement element, string name, float fallback)
        => element.TryGetProperty(name, out JsonElement value) && value.ValueKind == JsonValueKind.Number && value.TryGetSingle(out float result) && float.IsFinite(result) ? result : fallback;
}
