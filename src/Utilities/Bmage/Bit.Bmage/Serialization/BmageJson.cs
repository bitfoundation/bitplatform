using System.Buffers;
using System.Drawing;
using System.Text;
using System.Text.Json;

namespace Bit.Bmage;

/// <summary>
/// Converts edits to and from JSON: whole documents, pipelines, and the operations, layers, masks and annotations in
/// them. Every built-in type round-trips exactly, so an edit made in the browser can be stored, sent to a server and
/// replayed there on the original file at full resolution.
/// </summary>
/// <remarks>
/// <para>
/// The format is plain JSON with a <c>"type"</c> on every polymorphic object, versioned as a whole by
/// <see cref="FormatVersion"/>. It is read and written without reflection, so it is safe under trimming and native
/// AOT, and documents written by this version stay readable by later ones.
/// </para>
/// <para>
/// Pixels are never embedded. An image an annotation, layer or mask refers to is written as a reference that
/// <see cref="BmageJsonOptions.ImageReferences"/> chooses, and resolved on reading by
/// <see cref="BmageJsonOptions.ImageResolver"/>, so the images can be stored beside the document, deduplicated and
/// uploaded once.
/// </para>
/// <para>
/// A custom operation, annotation, layer or mask is written once a codec is registered for it with
/// <see cref="BmageJsonOptions.Add{T}"/>; until then serialising one throws, rather than silently losing a step.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // In the browser: the edit, not the pixels, goes to the server.
/// var json = BmageJson.Serialize(editor.Document);
///
/// // On the server: replay it on the original upload at full resolution.
/// var document = BmageJson.DeserializeDocument(json);
/// using var original = await processor.DecodeAsync(upload);
/// using var rendered = new BmageDocumentRenderer(fonts).Render(document, original);
/// </code>
/// </example>
public static class BmageJson
{
    /// <summary>The version of the format this library writes, and the newest it reads.</summary>
    public const int FormatVersion = 1;

    private const string DocumentFormat = "bmage-document";
    private const string PipelineFormat = "bmage-pipeline";

    /// <summary>Writes a document as JSON.</summary>
    public static string Serialize(BmageImageDocument document, BmageJsonOptions? options = null)
        => WriteToString(writer => Write(writer, document, options), options);

    /// <summary>Writes a pipeline as JSON.</summary>
    public static string Serialize(BmageImagePipeline pipeline, BmageJsonOptions? options = null)
        => WriteToString(writer => Write(writer, pipeline, options), options);

    /// <summary>Writes a document to a JSON writer.</summary>
    public static void Write(Utf8JsonWriter writer, BmageImageDocument document, BmageJsonOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(document);
        var w = new BmageJsonWriter(writer, options ?? BmageJsonOptions.Default);
        writer.WriteStartObject();
        writer.WriteString("format", DocumentFormat);
        writer.WriteNumber("version", FormatVersion);
        w.Write("sourceSize", document.SourceSize);
        w.Write("pipeline", document.Pipeline);
        writer.WriteStartArray("layers");
        foreach (var layer in document.Layers) w.WriteValue(layer);
        writer.WriteEndArray();
        writer.WriteStartArray("annotations");
        foreach (var annotation in document.Annotations) w.WriteValue(annotation);
        writer.WriteEndArray();
        writer.WriteEndObject();
    }

    /// <summary>Writes a pipeline to a JSON writer.</summary>
    public static void Write(Utf8JsonWriter writer, BmageImagePipeline pipeline, BmageJsonOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(pipeline);
        var w = new BmageJsonWriter(writer, options ?? BmageJsonOptions.Default);
        writer.WriteStartObject();
        writer.WriteString("format", PipelineFormat);
        writer.WriteNumber("version", FormatVersion);
        w.Write("operations", pipeline);
        writer.WriteEndObject();
    }

    /// <summary>Reads a document written by <see cref="Serialize(BmageImageDocument, BmageJsonOptions?)"/>.</summary>
    /// <exception cref="BmageImageDocumentFormatException">The JSON is not a document this version can read.</exception>
    public static BmageImageDocument DeserializeDocument(string json, BmageJsonOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(json);
        using var parsed = Parse(json);
        return ReadDocument(parsed.RootElement, options);
    }

    /// <summary>Reads a document from a parsed JSON element.</summary>
    /// <exception cref="BmageImageDocumentFormatException">The JSON is not a document this version can read.</exception>
    public static BmageImageDocument ReadDocument(JsonElement element, BmageJsonOptions? options = null)
    {
        var r = Root(element, DocumentFormat, options);
        var size = r.GetSize("sourceSize");
        if (size.Width <= 0 || size.Height <= 0) throw new BmageImageDocumentFormatException($"$.sourceSize must be positive, not {size.Width}x{size.Height}.");
        var document = BmageImageDocument.Create(size).WithPipeline(r.GetPipeline("pipeline"));
        foreach (var layer in r.GetArray<BmageLayer>("layers")) document = document.AddLayer(layer);
        return document.AddAnnotations(r.GetArray<BmageAnnotation>("annotations"));
    }

    /// <summary>Reads a pipeline written by <see cref="Serialize(BmageImagePipeline, BmageJsonOptions?)"/>.</summary>
    /// <exception cref="BmageImageDocumentFormatException">The JSON is not a pipeline this version can read.</exception>
    public static BmageImagePipeline DeserializePipeline(string json, BmageJsonOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(json);
        using var parsed = Parse(json);
        return ReadPipeline(parsed.RootElement, options);
    }

    /// <summary>Reads a pipeline from a parsed JSON element.</summary>
    /// <exception cref="BmageImageDocumentFormatException">The JSON is not a pipeline this version can read.</exception>
    public static BmageImagePipeline ReadPipeline(JsonElement element, BmageJsonOptions? options = null)
        => Root(element, PipelineFormat, options).GetPipeline("operations");

    /// <summary>Writes one operation, layer, mask or annotation as JSON, with its <c>"type"</c>.</summary>
    public static string SerializeObject(object value, BmageJsonOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(value);
        return WriteToString(writer => new BmageJsonWriter(writer, options ?? BmageJsonOptions.Default).WriteValue(value), options);
    }

    /// <summary>
    /// Reads one object written by <see cref="SerializeObject"/>. <typeparamref name="T"/> is
    /// <see cref="IBmageImageOperation"/>, <see cref="BmageLayer"/>, <see cref="BmageMask"/>, <see cref="BmageAnnotation"/> or a type a
    /// codec was registered for.
    /// </summary>
    public static T DeserializeObject<T>(string json, BmageJsonOptions? options = null) where T : class
    {
        ArgumentNullException.ThrowIfNull(json);
        using var parsed = Parse(json);
        return new BmageJsonReader(parsed.RootElement, options ?? BmageJsonOptions.Default, "$").ReadValue<T>(parsed.RootElement, "$");
    }

    private static BmageJsonReader Root(JsonElement element, string format, BmageJsonOptions? options)
    {
        if (element.ValueKind != JsonValueKind.Object) throw new BmageImageDocumentFormatException("The JSON is not an object.");
        var r = new BmageJsonReader(element, options ?? BmageJsonOptions.Default, "$");
        var actual = r.GetString("format", null);
        if (actual != format)
            throw new BmageImageDocumentFormatException(actual is null ? $"The JSON has no \"format\"; expected \"{format}\"." : $"The JSON is a \"{actual}\", not a \"{format}\".");
        var version = r.GetInt32("version", 0);
        if (version is < 1 or > FormatVersion)
            throw new BmageImageDocumentFormatException($"The JSON is version {version} of the format; this version of Bmage reads versions 1 to {FormatVersion}.");
        return r;
    }

    private static JsonDocument Parse(string json)
    {
        try
        {
            return JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = 128 });
        }
        catch (JsonException ex)
        {
            throw new BmageImageDocumentFormatException($"The text is not valid JSON: {ex.Message}", ex);
        }
    }

    private static string WriteToString(Action<Utf8JsonWriter> write, BmageJsonOptions? options)
    {
        var buffer = new ArrayBufferWriter<byte>();
        using (var writer = new Utf8JsonWriter(buffer, new JsonWriterOptions { Indented = options?.Indented ?? false }))
            write(writer);
        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }
}
