using System.Buffers;
using System.Drawing;
using System.Text;
using System.Text.Json;
using Bit.Bmage.Annotations;
using Bit.Bmage.Editing;
using Bit.Bmage.Layers;
using Bit.Bmage.Masks;
using Bit.Bmage.Operations;
using Bit.Bmage.Pipeline;

namespace Bit.Bmage.Serialization;

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
/// using var rendered = new DocumentRenderer(fonts).Render(document, original);
/// </code>
/// </example>
public static class BmageJson
{
    /// <summary>The version of the format this library writes, and the newest it reads.</summary>
    public const int FormatVersion = 1;

    private const string DocumentFormat = "bmage-document";
    private const string PipelineFormat = "bmage-pipeline";

    /// <summary>Writes a document as JSON.</summary>
    public static string Serialize(ImageDocument document, BmageJsonOptions? options = null)
        => WriteToString(writer => Write(writer, document, options), options);

    /// <summary>Writes a pipeline as JSON.</summary>
    public static string Serialize(ImagePipeline pipeline, BmageJsonOptions? options = null)
        => WriteToString(writer => Write(writer, pipeline, options), options);

    /// <summary>Writes a document to a JSON writer.</summary>
    public static void Write(Utf8JsonWriter writer, ImageDocument document, BmageJsonOptions? options = null)
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
    public static void Write(Utf8JsonWriter writer, ImagePipeline pipeline, BmageJsonOptions? options = null)
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

    /// <summary>Reads a document written by <see cref="Serialize(ImageDocument, BmageJsonOptions?)"/>.</summary>
    /// <exception cref="ImageDocumentFormatException">The JSON is not a document this version can read.</exception>
    public static ImageDocument DeserializeDocument(string json, BmageJsonOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(json);
        using var parsed = Parse(json);
        return ReadDocument(parsed.RootElement, options);
    }

    /// <summary>Reads a document from a parsed JSON element.</summary>
    /// <exception cref="ImageDocumentFormatException">The JSON is not a document this version can read.</exception>
    public static ImageDocument ReadDocument(JsonElement element, BmageJsonOptions? options = null)
    {
        var r = Root(element, DocumentFormat, options);
        var size = r.GetSize("sourceSize");
        if (size.Width <= 0 || size.Height <= 0) throw new ImageDocumentFormatException($"$.sourceSize must be positive, not {size.Width}x{size.Height}.");
        var document = ImageDocument.Create(size).WithPipeline(r.GetPipeline("pipeline"));
        foreach (var layer in r.GetArray<Layer>("layers")) document = document.AddLayer(layer);
        return document.AddAnnotations(r.GetArray<Annotation>("annotations"));
    }

    /// <summary>Reads a pipeline written by <see cref="Serialize(ImagePipeline, BmageJsonOptions?)"/>.</summary>
    /// <exception cref="ImageDocumentFormatException">The JSON is not a pipeline this version can read.</exception>
    public static ImagePipeline DeserializePipeline(string json, BmageJsonOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(json);
        using var parsed = Parse(json);
        return ReadPipeline(parsed.RootElement, options);
    }

    /// <summary>Reads a pipeline from a parsed JSON element.</summary>
    /// <exception cref="ImageDocumentFormatException">The JSON is not a pipeline this version can read.</exception>
    public static ImagePipeline ReadPipeline(JsonElement element, BmageJsonOptions? options = null)
        => Root(element, PipelineFormat, options).GetPipeline("operations");

    /// <summary>Writes one operation, layer, mask or annotation as JSON, with its <c>"type"</c>.</summary>
    public static string SerializeObject(object value, BmageJsonOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(value);
        return WriteToString(writer => new BmageJsonWriter(writer, options ?? BmageJsonOptions.Default).WriteValue(value), options);
    }

    /// <summary>
    /// Reads one object written by <see cref="SerializeObject"/>. <typeparamref name="T"/> is
    /// <see cref="IImageOperation"/>, <see cref="Layer"/>, <see cref="Mask"/>, <see cref="Annotation"/> or a type a
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
        if (element.ValueKind != JsonValueKind.Object) throw new ImageDocumentFormatException("The JSON is not an object.");
        var r = new BmageJsonReader(element, options ?? BmageJsonOptions.Default, "$");
        var actual = r.GetString("format", null);
        if (actual != format)
            throw new ImageDocumentFormatException(actual is null ? $"The JSON has no \"format\"; expected \"{format}\"." : $"The JSON is a \"{actual}\", not a \"{format}\".");
        var version = r.GetInt32("version", 0);
        if (version is < 1 or > FormatVersion)
            throw new ImageDocumentFormatException($"The JSON is version {version} of the format; this version of Bmage reads versions 1 to {FormatVersion}.");
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
            throw new ImageDocumentFormatException($"The text is not valid JSON: {ex.Message}", ex);
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

/// <summary>Options for <see cref="BmageJson"/>: how images are referred to, layout, and codecs for custom types.</summary>
public sealed class BmageJsonOptions
{
    /// <summary>The built-in codecs, with no image handling: documents without images round-trip with these.</summary>
    public static BmageJsonOptions Default { get; } = new() { _frozen = true };

    private bool _frozen;

    public BmageJsonOptions() => Codecs = new CodecRegistry(BuiltInCodecs.All);

    private BmageJsonOptions(CodecRegistry codecs) => Codecs = codecs;

    /// <summary>
    /// A copy with the same codecs, the custom ones included, and the given image handling, for a reader or writer that
    /// stores the images itself, such as <see cref="BmageProject"/>. The caller's options are left as they are.
    /// </summary>
    internal BmageJsonOptions WithImages(Func<ImageBuffer, string>? references, Func<string, ImageBuffer?>? resolver) => new(Codecs)
    {
        ImageReferences = references,
        ImageResolver = resolver,
        Indented = Indented,
    };

    /// <summary>
    /// Chooses the reference written for an image an annotation, layer or mask uses: a file name, an upload id, a
    /// content hash. Called once per occurrence; return the same reference for the same image to store it once.
    /// </summary>
    public Func<ImageBuffer, string>? ImageReferences
    {
        get;
        set { ThrowIfFrozen(); field = value; }
    }

    /// <summary>Supplies the image for a reference written by <see cref="ImageReferences"/>, or null when it is unknown.</summary>
    public Func<string, ImageBuffer?>? ImageResolver
    {
        get;
        set { ThrowIfFrozen(); field = value; }
    }

    /// <summary>Indent the JSON for people to read. Default false.</summary>
    public bool Indented
    {
        get;
        set { ThrowIfFrozen(); field = value; }
    }

    internal CodecRegistry Codecs { get; }

    /// <summary>
    /// Registers how a custom operation, annotation, layer or mask is written and read. <paramref name="type"/> is the
    /// <c>"type"</c> it is stored under; namespacing it, as in <c>"acme.duotone"</c>, keeps it clear of built-in names.
    /// </summary>
    /// <example>
    /// <code>
    /// options.Add&lt;DuotoneOperation&gt;("acme.duotone",
    ///     (writer, duotone) => writer.Write("shadow", duotone.Shadow).Write("highlight", duotone.Highlight),
    ///     reader => new DuotoneOperation(reader.GetColor("shadow"), reader.GetColor("highlight")));
    /// </code>
    /// </example>
    public BmageJsonOptions Add<T>(string type, Action<BmageJsonWriter, T> write, Func<BmageJsonReader, T> read) where T : class
    {
        ThrowIfFrozen();
        ArgumentException.ThrowIfNullOrWhiteSpace(type);
        ArgumentNullException.ThrowIfNull(write);
        ArgumentNullException.ThrowIfNull(read);
        var kind = typeof(IImageOperation).IsAssignableFrom(typeof(T)) ? typeof(IImageOperation)
            : typeof(Annotation).IsAssignableFrom(typeof(T)) ? typeof(Annotation)
            : typeof(Layer).IsAssignableFrom(typeof(T)) ? typeof(Layer)
            : typeof(Mask).IsAssignableFrom(typeof(T)) ? typeof(Mask)
            : throw new ArgumentException($"{typeof(T).Name} is not an operation, annotation, layer or mask.", nameof(T));
        Codecs.Add(new Codec(kind, typeof(T), type, (w, value) => write(w, (T)value), r => read(r)));
        return this;
    }

    private void ThrowIfFrozen()
    {
        if (_frozen) throw new InvalidOperationException("BmageJsonOptions.Default is shared and cannot be changed. Create new BmageJsonOptions instead.");
    }
}

/// <summary>How one type is written and read.</summary>
internal sealed record Codec(Type Kind, Type Type, string TypeName, Action<BmageJsonWriter, object> Write, Func<BmageJsonReader, object> Read);

/// <summary>The codecs in use, by CLR type for writing and by kind and name for reading.</summary>
internal sealed class CodecRegistry
{
    private readonly Dictionary<Type, Codec> _byType = [];
    private readonly Dictionary<(Type Kind, string Name), Codec> _byName = [];

    public CodecRegistry(IEnumerable<Codec> codecs)
    {
        foreach (var codec in codecs) Add(codec);
    }

    /// <summary>Adds a codec, replacing any for the same type and any for the same name of the same kind.</summary>
    public void Add(Codec codec)
    {
        // Whatever the new codec displaces, by name or by type, stops answering for both.
        if (_byName.TryGetValue((codec.Kind, codec.TypeName), out var sameName) && _byType.TryGetValue(sameName.Type, out var mapped) && ReferenceEquals(mapped, sameName))
            _byType.Remove(sameName.Type);
        if (_byType.TryGetValue(codec.Type, out var sameType) && _byName.TryGetValue((sameType.Kind, sameType.TypeName), out var named) && ReferenceEquals(named, sameType))
            _byName.Remove((sameType.Kind, sameType.TypeName));
        _byType[codec.Type] = codec;
        _byName[(codec.Kind, codec.TypeName)] = codec;
    }

    /// <summary>
    /// The codec for exactly the value's type. A subclass is not written as its base type, even of a matrix or curve
    /// operation: it may draw different pixels, and reading it back as the base would change the edit without a word.
    /// </summary>
    public Codec Find(object value)
    {
        var type = value.GetType();
        if (_byType.TryGetValue(type, out var codec)) return codec;
        throw new NotSupportedException($"{type.Name} has no JSON codec. Register one with BmageJsonOptions.Add<{type.Name}>(...) so the document can be written and read back.");
    }

    /// <summary>True when a codec is registered for exactly this type.</summary>
    public bool Supports(Type type) => _byType.ContainsKey(type);

    public Codec? Find(Type kind, string name)
    {
        if (_byName.TryGetValue((kind, name), out var codec)) return codec;
        // A codec registered for a concrete type answers for its own kind when asked for that type directly.
        return _byName.Values.FirstOrDefault(c => c.TypeName == name && kind.IsAssignableFrom(c.Type));
    }
}
