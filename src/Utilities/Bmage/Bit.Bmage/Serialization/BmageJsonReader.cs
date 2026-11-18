using System.Drawing;
using System.Numerics;
using System.Text.Json;

namespace Bit.Bmage;

/// <summary>
/// Reads the properties of one object of a Bmage document. Handed to every codec, built-in or custom. Optional
/// properties take their default when absent, so documents written by an older version keep reading; a required one
/// that is missing or malformed throws <see cref="BmageImageDocumentFormatException"/> naming it.
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

    public BmageRgba32 GetColor(string name) => Optional(name) is { } v ? ParseColor(name, v) : throw Missing(name);

    public BmageRgba32 GetColor(string name, BmageRgba32 fallback) => Optional(name) is { } v ? ParseColor(name, v) : fallback;

    public BmageRgba32? GetNullableColor(string name, BmageRgba32? fallback = null)
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
    public BmageImageBuffer GetImage(string name)
    {
        var reference = GetString(name);
        var resolver = Options.ImageResolver
            ?? throw new BmageImageDocumentFormatException($"{Path}.{name} refers to the image '{reference}'. Set BmageJsonOptions.ImageResolver to supply the images a document refers to.");
        return resolver(reference) ?? throw new BmageImageDocumentFormatException($"{Path}.{name} refers to the image '{reference}', which the resolver did not supply.");
    }

    /// <summary>Reads an operation, or null when the property is absent or null.</summary>
    public IBmageImageOperation? GetOperation(string name) => Optional(name) is { } v ? ReadValue<IBmageImageOperation>(v, $"{Path}.{name}") : null;

    /// <summary>Reads a pipeline written as an array of operations; an absent property is the empty pipeline.</summary>
    public BmageImagePipeline GetPipeline(string name)
    {
        if (Optional(name) is not { } v) return BmageImagePipeline.Empty;
        return BmageImagePipeline.From(ReadArray<IBmageImageOperation>(v, $"{Path}.{name}"));
    }

    /// <summary>Reads a mask, or null when the property is absent or null.</summary>
    public BmageMask? GetMask(string name) => Optional(name) is { } v ? ReadValue<BmageMask>(v, $"{Path}.{name}") : null;

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
        if (element.ValueKind != JsonValueKind.Object) throw new BmageImageDocumentFormatException($"{path} should be an object.");
        if (!element.TryGetProperty("type", out var typeElement) || typeElement.ValueKind != JsonValueKind.String)
            throw new BmageImageDocumentFormatException($"{path} has no \"type\".");
        var type = typeElement.GetString()!;
        var codec = Options.Codecs.Find(typeof(T), type)
            ?? throw new BmageImageDocumentFormatException($"{path} is a \"{type}\", which is not a known {Kind(typeof(T))}. Register a codec for it in BmageJsonOptions.");
        try
        {
            return (T)codec.Read(new BmageJsonReader(element, Options, path));
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or FormatException)
        {
            throw new BmageImageDocumentFormatException($"{path} (a \"{type}\") is invalid: {ex.Message}", ex);
        }
    }

    internal List<T> ReadArray<T>(JsonElement element, string path) where T : class
    {
        if (element.ValueKind != JsonValueKind.Array) throw new BmageImageDocumentFormatException($"{path} should be an array.");
        var list = new List<T>(element.GetArrayLength());
        var i = 0;
        foreach (var item in element.EnumerateArray()) list.Add(ReadValue<T>(item, $"{path}[{i++}]"));
        return list;
    }

    /// <summary>Reads an array of nested objects of one kind.</summary>
    public List<T> GetArray<T>(string name) where T : class
        => Optional(name) is { } v ? ReadArray<T>(v, $"{Path}.{name}") : [];

    private static string Kind(Type type)
        => type == typeof(IBmageImageOperation) ? "operation" : type == typeof(BmageAnnotation) ? "annotation" : type == typeof(BmageLayer) ? "layer" : type == typeof(BmageMask) ? "mask" : type.Name;

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

    private BmageRgba32 ParseColor(string name, JsonElement value)
        => value.ValueKind == JsonValueKind.String && BmageRgba32.TryParseHex(value.GetString(), out var color) ? color : throw Invalid(name, "expected a colour such as \"#ff8800ff\"");

    private T Convert<T>(string name, Func<T> read)
    {
        try { return read(); }
        catch (Exception ex) when (ex is InvalidOperationException or FormatException) { throw Invalid(name, ex.Message); }
    }

    private BmageImageDocumentFormatException Missing(string name) => new($"{Path} has no \"{name}\".");

    private BmageImageDocumentFormatException Invalid(string name, string problem) => new($"{Path}.{name} is invalid: {problem}.");
}
