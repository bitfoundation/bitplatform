using System.Drawing;
using System.Numerics;
using System.Text.Json;
using Bit.Bmage.Annotations;
using Bit.Bmage.Layers;
using Bit.Bmage.Masks;
using Bit.Bmage.Operations;
using Bit.Bmage.Pipeline;

namespace Bit.Bmage.Serialization;

/// <summary>
/// Writes the properties of one object of a Bmage document. Handed to every codec, built-in or custom, positioned
/// inside the object after its <c>"type"</c>; the codec writes its own properties and nothing else.
/// </summary>
/// <remarks>
/// Geometry is written compactly: a point as <c>[x, y]</c>, a size as <c>[width, height]</c> and a rectangle as
/// <c>[x, y, width, height]</c>. Colours are <c>"#rrggbbaa"</c>. Numbers round-trip exactly.
/// </remarks>
public sealed class BmageJsonWriter
{
    internal BmageJsonWriter(Utf8JsonWriter json, BmageJsonOptions options)
    {
        Json = json;
        Options = options;
    }

    /// <summary>The underlying writer, for anything the helpers do not cover.</summary>
    public Utf8JsonWriter Json { get; }

    /// <summary>The options of the current serialisation.</summary>
    public BmageJsonOptions Options { get; }

    public BmageJsonWriter Write(string name, string? value)
    {
        if (value is null) Json.WriteNull(name);
        else Json.WriteString(name, value);
        return this;
    }

    public BmageJsonWriter Write(string name, bool value) { Json.WriteBoolean(name, value); return this; }

    public BmageJsonWriter Write(string name, int value) { Json.WriteNumber(name, value); return this; }

    public BmageJsonWriter Write(string name, int? value)
    {
        if (value is { } v) Json.WriteNumber(name, v);
        else Json.WriteNull(name);
        return this;
    }

    public BmageJsonWriter Write(string name, float value)
    {
        Json.WritePropertyName(name);
        WriteNumberValue(value);
        return this;
    }

    public BmageJsonWriter Write(string name, float? value)
    {
        if (value is { } v) return Write(name, v);
        Json.WriteNull(name);
        return this;
    }

    public BmageJsonWriter Write(string name, double value)
    {
        Json.WritePropertyName(name);
        WriteNumberValue(value);
        return this;
    }

    /// <summary>
    /// Writes a number as an array element, or after a property name. A value JSON has no number for, NaN or an
    /// infinity such as pointer arithmetic that divided by zero produces, is written as the string System.Text.Json's
    /// named literals use (<c>"NaN"</c>, <c>"Infinity"</c> or <c>"-Infinity"</c>), which <see cref="BmageJsonReader"/>
    /// reads back.
    /// </summary>
    public void WriteNumberValue(float value)
    {
        if (float.IsFinite(value)) Json.WriteNumberValue(value);
        else Json.WriteStringValue(float.IsNaN(value) ? "NaN" : value > 0 ? "Infinity" : "-Infinity");
    }

    /// <inheritdoc cref="WriteNumberValue(float)"/>
    public void WriteNumberValue(double value)
    {
        if (double.IsFinite(value)) Json.WriteNumberValue(value);
        else Json.WriteStringValue(double.IsNaN(value) ? "NaN" : value > 0 ? "Infinity" : "-Infinity");
    }

    public BmageJsonWriter Write(string name, Guid value) { Json.WriteString(name, value); return this; }

    /// <summary>Writes an enum by its name, or by its members' names for a set of flags.</summary>
    /// <exception cref="ArgumentException">The value is not one the enum names, so it could not be read back.</exception>
    public BmageJsonWriter WriteEnum<TEnum>(string name, TEnum value) where TEnum : struct, Enum
    {
        if (!BmageJsonReader.IsNamed(value))
            throw new ArgumentException($"{value} is not a {typeof(TEnum).Name}, so '{name}' cannot be written.", nameof(value));
        Json.WriteString(name, value.ToString());
        return this;
    }

    public BmageJsonWriter Write(string name, Rgba32 value) { Json.WriteString(name, value.ToHex()); return this; }

    public BmageJsonWriter Write(string name, Rgba32? value)
    {
        if (value is { } v) Json.WriteString(name, v.ToHex());
        else Json.WriteNull(name);
        return this;
    }

    public BmageJsonWriter Write(string name, Vector2 value) => Numbers(name, value.X, value.Y);

    public BmageJsonWriter Write(string name, PointF value) => Numbers(name, value.X, value.Y);

    public BmageJsonWriter Write(string name, Size value)
    {
        Json.WriteStartArray(name);
        Json.WriteNumberValue(value.Width);
        Json.WriteNumberValue(value.Height);
        Json.WriteEndArray();
        return this;
    }

    public BmageJsonWriter Write(string name, Size? value)
    {
        if (value is { } v) return Write(name, v);
        Json.WriteNull(name);
        return this;
    }

    public BmageJsonWriter Write(string name, SizeF value) => Numbers(name, value.Width, value.Height);

    public BmageJsonWriter Write(string name, Rectangle value)
    {
        Json.WriteStartArray(name);
        Json.WriteNumberValue(value.X);
        Json.WriteNumberValue(value.Y);
        Json.WriteNumberValue(value.Width);
        Json.WriteNumberValue(value.Height);
        Json.WriteEndArray();
        return this;
    }

    public BmageJsonWriter Write(string name, Rectangle? value)
    {
        if (value is { } v) return Write(name, v);
        Json.WriteNull(name);
        return this;
    }

    public BmageJsonWriter Write(string name, RectangleF value) => Numbers(name, value.X, value.Y, value.Width, value.Height);

    /// <summary>Writes a list of numbers, or null.</summary>
    public BmageJsonWriter Write(string name, IReadOnlyList<float>? values)
    {
        if (values is null)
        {
            Json.WriteNull(name);
            return this;
        }
        Json.WriteStartArray(name);
        foreach (var v in values) WriteNumberValue(v);
        Json.WriteEndArray();
        return this;
    }

    /// <summary>Writes a list of points as <c>[[x, y], ...]</c>.</summary>
    public BmageJsonWriter Write(string name, IReadOnlyList<Vector2> points)
    {
        Json.WriteStartArray(name);
        foreach (var p in points)
        {
            Json.WriteStartArray();
            WriteNumberValue(p.X);
            WriteNumberValue(p.Y);
            Json.WriteEndArray();
        }
        Json.WriteEndArray();
        return this;
    }

    /// <summary>
    /// Writes a reference to an image through <see cref="BmageJsonOptions.ImageReferences"/>: images are stored beside
    /// the document, never inside it.
    /// </summary>
    public BmageJsonWriter WriteImage(string name, ImageBuffer image)
    {
        ArgumentNullException.ThrowIfNull(image);
        var reference = Options.ImageReferences?.Invoke(image)
            ?? throw new NotSupportedException("The document refers to an image (an image annotation, an image layer or a bitmap mask). Set BmageJsonOptions.ImageReferences to say how each image is referred to, and store the images beside the document.");
        Json.WriteString(name, reference);
        return this;
    }

    /// <summary>Writes an operation, or null.</summary>
    public BmageJsonWriter Write(string name, IImageOperation? operation)
    {
        Json.WritePropertyName(name);
        WriteValue(operation);
        return this;
    }

    /// <summary>Writes the operations of a pipeline as an array.</summary>
    public BmageJsonWriter Write(string name, ImagePipeline pipeline)
    {
        ArgumentNullException.ThrowIfNull(pipeline);
        Json.WriteStartArray(name);
        foreach (var operation in pipeline) WriteValue(operation);
        Json.WriteEndArray();
        return this;
    }

    /// <summary>Writes a mask, or null.</summary>
    public BmageJsonWriter Write(string name, Mask? mask)
    {
        Json.WritePropertyName(name);
        WriteValue(mask);
        return this;
    }

    /// <summary>Writes a layer.</summary>
    public BmageJsonWriter Write(string name, Layer layer)
    {
        Json.WritePropertyName(name);
        WriteValue(layer);
        return this;
    }

    /// <summary>Writes an annotation.</summary>
    public BmageJsonWriter Write(string name, Annotation annotation)
    {
        Json.WritePropertyName(name);
        WriteValue(annotation);
        return this;
    }

    /// <summary>Writes a nested object without a type, such as a style, whose properties <paramref name="body"/> writes.</summary>
    public BmageJsonWriter WriteObject(string name, Action<BmageJsonWriter> body)
    {
        ArgumentNullException.ThrowIfNull(body);
        Json.WriteStartObject(name);
        body(this);
        Json.WriteEndObject();
        return this;
    }

    /// <summary>Writes an array of nested objects without a type, each written by <paramref name="body"/>.</summary>
    public BmageJsonWriter WriteObjects<T>(string name, IEnumerable<T> items, Action<BmageJsonWriter, T> body)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(body);
        Json.WriteStartArray(name);
        foreach (var item in items)
        {
            Json.WriteStartObject();
            body(this, item);
            Json.WriteEndObject();
        }
        Json.WriteEndArray();
        return this;
    }

    internal void WriteValue(object? value)
    {
        if (value is null)
        {
            Json.WriteNullValue();
            return;
        }
        var codec = Options.Codecs.Find(value);
        Json.WriteStartObject();
        Json.WriteString("type", codec.TypeName);
        codec.Write(this, value);
        Json.WriteEndObject();
    }

    private BmageJsonWriter Numbers(string name, params ReadOnlySpan<float> values)
    {
        Json.WriteStartArray(name);
        foreach (var v in values) WriteNumberValue(v);
        Json.WriteEndArray();
        return this;
    }
}

/// <summary>
/// Reads the properties of one object of a Bmage document. Handed to every codec, built-in or custom. Optional
/// properties take their default when absent, so documents written by an older version keep reading; a required one
/// that is missing or malformed throws <see cref="ImageDocumentFormatException"/> naming it.
/// </summary>
public sealed class BmageJsonReader
{
    private readonly JsonElement _element;

    internal BmageJsonReader(JsonElement element, BmageJsonOptions options, string path)
    {
        _element = element;
        Options = options;
        Path = path;
    }

    /// <summary>The object being read, for anything the helpers do not cover.</summary>
    public JsonElement Element => _element;

    /// <summary>The options of the current deserialisation.</summary>
    public BmageJsonOptions Options { get; }

    /// <summary>Where in the document the object is, for error messages: <c>$.pipeline[2]</c>.</summary>
    public string Path { get; }

    /// <summary>True when the object has the property and it is not null.</summary>
    public bool Has(string name) => _element.TryGetProperty(name, out var value) && value.ValueKind != JsonValueKind.Null;

    public string GetString(string name) => Optional(name) is { } v ? Convert(name, () => v.GetString()!) : throw Missing(name);

    public string? GetString(string name, string? fallback) => Optional(name) is { } v ? Convert(name, () => v.GetString()) : fallback;

    public bool GetBoolean(string name, bool fallback = false) => Optional(name) is { } v ? Convert(name, v.GetBoolean) : fallback;

    public int GetInt32(string name) => Optional(name) is { } v ? Convert(name, v.GetInt32) : throw Missing(name);

    public int GetInt32(string name, int fallback) => Optional(name) is { } v ? Convert(name, v.GetInt32) : fallback;

    public int? GetNullableInt32(string name) => Optional(name) is { } v ? Convert(name, v.GetInt32) : null;

    public float GetSingle(string name) => Optional(name) is { } v ? Convert(name, () => ReadSingle(v)) : throw Missing(name);

    public float GetSingle(string name, float fallback) => Optional(name) is { } v ? Convert(name, () => ReadSingle(v)) : fallback;

    public float? GetNullableSingle(string name) => Optional(name) is { } v ? Convert(name, () => ReadSingle(v)) : null;

    public double GetDouble(string name) => Optional(name) is { } v ? Convert(name, () => ReadDouble(v)) : throw Missing(name);

    public double GetDouble(string name, double fallback) => Optional(name) is { } v ? Convert(name, () => ReadDouble(v)) : fallback;

    /// <summary>
    /// Reads a number, or one of the strings <see cref="BmageJsonWriter.WriteNumberValue(float)"/> writes for a value
    /// JSON has no number for.
    /// </summary>
    /// <exception cref="InvalidOperationException">The element is neither a number nor such a string.</exception>
    /// <exception cref="FormatException">The number does not fit.</exception>
    public static float ReadSingle(JsonElement element) => element.ValueKind == JsonValueKind.String ? (float)NamedNumber(element) : element.GetSingle();

    /// <inheritdoc cref="ReadSingle"/>
    public static double ReadDouble(JsonElement element) => element.ValueKind == JsonValueKind.String ? NamedNumber(element) : element.GetDouble();

    private static double NamedNumber(JsonElement element) => element.GetString() switch
    {
        "NaN" => double.NaN,
        "Infinity" => double.PositiveInfinity,
        "-Infinity" => double.NegativeInfinity,
        var text => throw new InvalidOperationException($"'{text}' is not a number"),
    };

    public Guid GetGuid(string name, Guid fallback) => Optional(name) is { } v ? Convert(name, v.GetGuid) : fallback;

    /// <summary>Reads an enum by name, ignoring case.</summary>
    public TEnum GetEnum<TEnum>(string name, TEnum fallback) where TEnum : struct, Enum
    {
        if (Optional(name) is not { } v) return fallback;
        var text = Convert(name, () => v.GetString()) ?? string.Empty;
        // Names only. Enum.TryParse alone would also take any number, accept values no member has, and combine the
        // members of an enum that is not a set of flags: "Copy, Erase" would read as Multiply. A set of flags may combine
        // its members; anything else must be exactly one of them.
        var flags = typeof(TEnum).IsDefined(typeof(FlagsAttribute), inherit: false);
        var valid = Enum.TryParse<TEnum>(text, ignoreCase: true, out var value)
            && text.Trim().Length > 0 && !char.IsDigit(text.Trim()[0]) && text.Trim()[0] is not ('-' or '+')
            && IsNamed(value)
            && (flags || string.Equals(value.ToString(), text.Trim(), StringComparison.OrdinalIgnoreCase));
        return valid ? value : throw Invalid(name, $"'{text}' is not a {typeof(TEnum).Name}");
    }

    /// <summary>True when the value prints as its members' names rather than as a number.</summary>
    internal static bool IsNamed<TEnum>(TEnum value) where TEnum : struct, Enum
    {
        var text = value.ToString();
        return text.Length > 0 && !char.IsDigit(text[0]) && text[0] != '-';
    }

    public Rgba32 GetColor(string name) => Optional(name) is { } v ? ParseColor(name, v) : throw Missing(name);

    public Rgba32 GetColor(string name, Rgba32 fallback) => Optional(name) is { } v ? ParseColor(name, v) : fallback;

    public Rgba32? GetNullableColor(string name, Rgba32? fallback = null)
    {
        if (!_element.TryGetProperty(name, out var value)) return fallback;
        return value.ValueKind == JsonValueKind.Null ? null : ParseColor(name, value);
    }

    public Vector2 GetVector2(string name) => Numbers(name, 2) is { } n ? new Vector2(n[0], n[1]) : throw Missing(name);

    public Vector2 GetVector2(string name, Vector2 fallback) => Numbers(name, 2) is { } n ? new Vector2(n[0], n[1]) : fallback;

    public PointF GetPointF(string name) => Numbers(name, 2) is { } n ? new PointF(n[0], n[1]) : throw Missing(name);

    public Size GetSize(string name) => GetNullableSize(name) ?? throw Missing(name);

    public Size? GetNullableSize(string name) => Integers(name, 2) is { } n ? new Size(n[0], n[1]) : null;

    public SizeF? GetNullableSizeF(string name) => Numbers(name, 2) is { } n ? new SizeF(n[0], n[1]) : null;

    public Rectangle GetRectangle(string name) => GetNullableRectangle(name) ?? throw Missing(name);

    public Rectangle? GetNullableRectangle(string name)
        => Integers(name, 4) is { } n ? new Rectangle(n[0], n[1], n[2], n[3]) : null;

    public RectangleF GetRectangleF(string name) => Numbers(name, 4) is { } n ? new RectangleF(n[0], n[1], n[2], n[3]) : throw Missing(name);

    /// <summary>Reads a list of numbers, or null when absent.</summary>
    public float[]? GetSingles(string name)
    {
        if (Optional(name) is not { } v) return null;
        if (v.ValueKind != JsonValueKind.Array) throw Invalid(name, "expected an array of numbers");
        var values = new float[v.GetArrayLength()];
        var i = 0;
        foreach (var item in v.EnumerateArray()) values[i++] = Convert(name, () => ReadSingle(item));
        return values;
    }

    /// <summary>Reads a list of points written as <c>[[x, y], ...]</c>.</summary>
    public Vector2[] GetPoints(string name)
    {
        if (Optional(name) is not { } v) throw Missing(name);
        if (v.ValueKind != JsonValueKind.Array) throw Invalid(name, "expected an array of points");
        var points = new Vector2[v.GetArrayLength()];
        var i = 0;
        foreach (var item in v.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Array || item.GetArrayLength() != 2) throw Invalid($"{name}[{i}]", "expected [x, y]");
            points[i++] = new Vector2(Convert(name, () => ReadSingle(item[0])), Convert(name, () => ReadSingle(item[1])));
        }
        return points;
    }

    /// <summary>Resolves an image reference through <see cref="BmageJsonOptions.ImageResolver"/>.</summary>
    public ImageBuffer GetImage(string name)
    {
        var reference = GetString(name);
        var resolver = Options.ImageResolver
            ?? throw new ImageDocumentFormatException($"{Path}.{name} refers to the image '{reference}'. Set BmageJsonOptions.ImageResolver to supply the images a document refers to.");
        return resolver(reference) ?? throw new ImageDocumentFormatException($"{Path}.{name} refers to the image '{reference}', which the resolver did not supply.");
    }

    /// <summary>Reads an operation, or null when the property is absent or null.</summary>
    public IImageOperation? GetOperation(string name) => Optional(name) is { } v ? ReadValue<IImageOperation>(v, $"{Path}.{name}") : null;

    /// <summary>Reads a pipeline written as an array of operations; an absent property is the empty pipeline.</summary>
    public ImagePipeline GetPipeline(string name)
    {
        if (Optional(name) is not { } v) return ImagePipeline.Empty;
        return ImagePipeline.From(ReadArray<IImageOperation>(v, $"{Path}.{name}"));
    }

    /// <summary>Reads a mask, or null when the property is absent or null.</summary>
    public Mask? GetMask(string name) => Optional(name) is { } v ? ReadValue<Mask>(v, $"{Path}.{name}") : null;

    /// <summary>A reader for a nested object without a type, or null when the property is absent or null.</summary>
    public BmageJsonReader? GetObject(string name)
    {
        if (Optional(name) is not { } v) return null;
        if (v.ValueKind != JsonValueKind.Object) throw Invalid(name, "expected an object");
        return new BmageJsonReader(v, Options, $"{Path}.{name}");
    }

    /// <summary>Readers for an array of nested objects without a type; an absent property is an empty array.</summary>
    public List<BmageJsonReader> GetObjects(string name)
    {
        if (Optional(name) is not { } v) return [];
        if (v.ValueKind != JsonValueKind.Array) throw Invalid(name, "expected an array of objects");
        var list = new List<BmageJsonReader>(v.GetArrayLength());
        var i = 0;
        foreach (var item in v.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object) throw Invalid($"{name}[{i}]", "expected an object");
            list.Add(new BmageJsonReader(item, Options, $"{Path}.{name}[{i++}]"));
        }
        return list;
    }

    /// <summary>Reads a nested object with the codec registered for its type.</summary>
    public T Get<T>(string name) where T : class
        => Optional(name) is { } v ? ReadValue<T>(v, $"{Path}.{name}") : throw Missing(name);

    internal T ReadValue<T>(JsonElement element, string path) where T : class
    {
        if (element.ValueKind != JsonValueKind.Object) throw new ImageDocumentFormatException($"{path} should be an object.");
        if (!element.TryGetProperty("type", out var typeElement) || typeElement.ValueKind != JsonValueKind.String)
            throw new ImageDocumentFormatException($"{path} has no \"type\".");
        var type = typeElement.GetString()!;
        var codec = Options.Codecs.Find(typeof(T), type)
            ?? throw new ImageDocumentFormatException($"{path} is a \"{type}\", which is not a known {Kind(typeof(T))}. Register a codec for it in BmageJsonOptions.");
        try
        {
            return (T)codec.Read(new BmageJsonReader(element, Options, path));
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or FormatException)
        {
            throw new ImageDocumentFormatException($"{path} (a \"{type}\") is invalid: {ex.Message}", ex);
        }
    }

    internal List<T> ReadArray<T>(JsonElement element, string path) where T : class
    {
        if (element.ValueKind != JsonValueKind.Array) throw new ImageDocumentFormatException($"{path} should be an array.");
        var list = new List<T>(element.GetArrayLength());
        var i = 0;
        foreach (var item in element.EnumerateArray()) list.Add(ReadValue<T>(item, $"{path}[{i++}]"));
        return list;
    }

    /// <summary>Reads an array of nested objects of one kind.</summary>
    public List<T> GetArray<T>(string name) where T : class
        => Optional(name) is { } v ? ReadArray<T>(v, $"{Path}.{name}") : [];

    private static string Kind(Type type)
        => type == typeof(IImageOperation) ? "operation" : type == typeof(Annotation) ? "annotation" : type == typeof(Layer) ? "layer" : type == typeof(Mask) ? "mask" : type.Name;

    private JsonElement? Optional(string name)
        => _element.TryGetProperty(name, out var value) && value.ValueKind != JsonValueKind.Null ? value : null;

    private float[]? Numbers(string name, int count)
    {
        if (Optional(name) is not { } v) return null;
        if (v.ValueKind != JsonValueKind.Array || v.GetArrayLength() != count) throw Invalid(name, $"expected {count} numbers");
        var values = new float[count];
        for (var i = 0; i < count; i++) values[i] = Convert(name, () => ReadSingle(v[i]));
        return values;
    }

    /// <summary>Whole numbers read as such, never through a float, which holds integers exactly only up to 2^24.</summary>
    private int[]? Integers(string name, int count)
    {
        if (Optional(name) is not { } v) return null;
        if (v.ValueKind != JsonValueKind.Array || v.GetArrayLength() != count) throw Invalid(name, $"expected {count} whole numbers");
        var values = new int[count];
        for (var i = 0; i < count; i++)
            if (v[i].ValueKind != JsonValueKind.Number || !v[i].TryGetInt32(out values[i])) throw Invalid(name, "expected whole numbers");
        return values;
    }

    private Rgba32 ParseColor(string name, JsonElement value)
        => value.ValueKind == JsonValueKind.String && Rgba32.TryParseHex(value.GetString(), out var color) ? color : throw Invalid(name, "expected a colour such as \"#ff8800ff\"");

    private T Convert<T>(string name, Func<T> read)
    {
        try { return read(); }
        catch (Exception ex) when (ex is InvalidOperationException or FormatException) { throw Invalid(name, ex.Message); }
    }

    private ImageDocumentFormatException Missing(string name) => new($"{Path} has no \"{name}\".");

    private ImageDocumentFormatException Invalid(string name, string problem) => new($"{Path}.{name} is invalid: {problem}.");
}

/// <summary>Thrown when JSON is not a document, pipeline or object Bmage can read.</summary>
public sealed class ImageDocumentFormatException : ImageException
{
    public ImageDocumentFormatException(string message) : base(message) { }
    public ImageDocumentFormatException(string message, Exception? innerException) : base(message, innerException) { }
}
