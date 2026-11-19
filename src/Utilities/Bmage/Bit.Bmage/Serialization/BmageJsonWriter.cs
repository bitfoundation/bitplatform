using System.Drawing;
using System.Numerics;
using System.Text.Json;

namespace Bit.Bmage;

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

    public BmageJsonWriter Write(string name, BmageRgba32 value) { Json.WriteString(name, value.ToHex()); return this; }

    public BmageJsonWriter Write(string name, BmageRgba32? value)
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
    public BmageJsonWriter WriteImage(string name, BmageImageBuffer image)
    {
        ArgumentNullException.ThrowIfNull(image);
        var reference = Options.ImageReferences?.Invoke(image)
            ?? throw new NotSupportedException("The document refers to an image (an image annotation, an image layer or a bitmap mask). Set BmageJsonOptions.ImageReferences to say how each image is referred to, and store the images beside the document.");
        Json.WriteString(name, reference);
        return this;
    }

    /// <summary>Writes an operation, or null.</summary>
    public BmageJsonWriter Write(string name, IBmageImageOperation? operation)
    {
        Json.WritePropertyName(name);
        WriteValue(operation);
        return this;
    }

    /// <summary>Writes the operations of a pipeline as an array.</summary>
    public BmageJsonWriter Write(string name, BmageImagePipeline pipeline)
    {
        ArgumentNullException.ThrowIfNull(pipeline);
        Json.WriteStartArray(name);
        foreach (var operation in pipeline) WriteValue(operation);
        Json.WriteEndArray();
        return this;
    }

    /// <summary>Writes a mask, or null.</summary>
    public BmageJsonWriter Write(string name, BmageMask? mask)
    {
        Json.WritePropertyName(name);
        WriteValue(mask);
        return this;
    }

    /// <summary>Writes a layer.</summary>
    public BmageJsonWriter Write(string name, BmageLayer layer)
    {
        Json.WritePropertyName(name);
        WriteValue(layer);
        return this;
    }

    /// <summary>Writes layers as an array, bottom first.</summary>
    public BmageJsonWriter Write(string name, IEnumerable<BmageLayer> layers)
    {
        ArgumentNullException.ThrowIfNull(layers);
        Json.WriteStartArray(name);
        foreach (var layer in layers) WriteValue(layer);
        Json.WriteEndArray();
        return this;
    }

    /// <summary>Writes an annotation.</summary>
    public BmageJsonWriter Write(string name, BmageAnnotation annotation)
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
