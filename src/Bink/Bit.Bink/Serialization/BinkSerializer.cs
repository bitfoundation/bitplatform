using System.Buffers;
using System.Numerics;
using System.Text;
using System.Text.Json;

namespace Bit.Bink;

/// <summary>
/// Reads and writes the Bink document format: versioned JSON with compact binary stroke points.
/// </summary>
/// <remarks>
/// <para>
/// The format is a stable contract, not a projection of the library's classes. Every element carries a
/// <c>type</c> discriminator and only documented fields; unknown fields are ignored on read, and unknown element
/// types are preserved verbatim, so documents survive round trips through other versions.
/// </para>
/// <para>
/// All input is treated as untrusted: sizes, counts and coordinates are validated against
/// <see cref="BinkDeserializerOptions.Limits"/> and malformed input produces an <see cref="BinkFormatException"/>
/// rather than an unbounded allocation.
/// </para>
/// </remarks>
public static class BinkSerializer
{
    /// <summary>The format identifier written into every document.</summary>
    public const string FormatName = "bink";

    /// <summary>The current schema version.</summary>
    public const int CurrentVersion = 1;

    /// <summary>The oldest schema version this library can read.</summary>
    public const int MinimumSupportedVersion = 1;

    /// <summary>Characters never allowed in a font family, because the value is emitted into CSS and SVG.</summary>
    private static readonly SearchValues<char> UnsafeFontCharacters = SearchValues.Create("<>\"'{}();\\");

    // ---- Writing ---------------------------------------------------------------------------------------------------

    /// <summary>Serializes a document to a JSON string.</summary>
    public static string Serialize(BinkDocument document, BinkSerializerOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(document);
        var buffer = new ArrayBufferWriter<byte>(EstimateSize(document, options));
        Serialize(document, buffer, options);
        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    /// <summary>Serializes a document as UTF-8 bytes.</summary>
    public static byte[] SerializeToUtf8Bytes(BinkDocument document, BinkSerializerOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(document);
        var buffer = new ArrayBufferWriter<byte>(EstimateSize(document, options));
        Serialize(document, buffer, options);
        return buffer.WrittenSpan.ToArray();
    }

    /// <summary>Serializes a document into a stream.</summary>
    public static void Serialize(BinkDocument document, Stream stream, BinkSerializerOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(stream);
        options ??= BinkSerializerOptions.Default;
        using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = options.Indented, SkipValidation = true });
        Write(writer, document, options);
    }

    /// <summary>Serializes a document into a buffer writer.</summary>
    public static void Serialize(BinkDocument document, IBufferWriter<byte> buffer, BinkSerializerOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(buffer);
        options ??= BinkSerializerOptions.Default;
        using var writer = new Utf8JsonWriter(buffer, new JsonWriterOptions { Indented = options.Indented, SkipValidation = true });
        Write(writer, document, options);
    }

    /// <summary>Serializes a document into a stream asynchronously.</summary>
    public static async Task SerializeAsync(BinkDocument document, Stream stream, BinkSerializerOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(stream);
        byte[] bytes = SerializeToUtf8Bytes(document, options);
        await stream.WriteAsync(bytes, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Estimates the encoded size so the output buffer rarely has to grow. Growing an <see cref="ArrayBufferWriter{T}"/>
    /// copies everything written so far, which dominates the cost of saving a large document.
    /// </summary>
    private static int EstimateSize(BinkDocument document, BinkSerializerOptions? options)
    {
        // Binary points cost roughly 6 bytes each before base64 (4/3 expansion); JSON points cost far more.
        double perPoint = (options ?? BinkSerializerOptions.Default).PointEncoding == BinkPointEncoding.Binary ? 8.5 : 34;
        long estimate = 1024 + document.ElementCount * 320L + (long)(document.TotalPointCount * perPoint);
        return (int)Math.Clamp(estimate, 1024, 256L * 1024 * 1024);
    }

    private static void Write(Utf8JsonWriter writer, BinkDocument document, BinkSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("format", FormatName);
        writer.WriteNumber("version", CurrentVersion);
        writer.WriteString("id", document.Id);

        writer.WritePropertyName("settings");
        writer.WriteStartObject();
        writer.WriteNumber("width", Round(document.Settings.Width, options.Decimals));
        writer.WriteNumber("height", Round(document.Settings.Height, options.Decimals));
        writer.WriteString("background", document.Settings.Background.ToCss());
        writer.WriteEndObject();

        if (document.Metadata.Count > 0)
        {
            writer.WritePropertyName("metadata");
            writer.WriteStartObject();
            foreach ((string key, string value) in document.Metadata.OrderBy(static kv => kv.Key, StringComparer.Ordinal)) writer.WriteString(key, value);
            writer.WriteEndObject();
        }

        writer.WritePropertyName("layers");
        writer.WriteStartArray();
        foreach (BinkLayer layer in document.Layers) WriteLayer(writer, layer, options);
        writer.WriteEndArray();

        writer.WriteEndObject();
        writer.Flush();
    }

    private static void WriteLayer(Utf8JsonWriter writer, BinkLayer layer, BinkSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("id", layer.Id);
        writer.WriteString("name", layer.Name);
        if (!layer.IsVisible) writer.WriteBoolean("visible", false);
        if (layer.IsLocked) writer.WriteBoolean("locked", true);
        if (layer.Opacity < 1f) writer.WriteNumber("opacity", Round(layer.Opacity, 3));
        WriteMetadata(writer, layer.Properties.Metadata);

        writer.WritePropertyName("elements");
        writer.WriteStartArray();
        foreach (BinkElement element in layer.Elements) WriteElement(writer, element, options);
        writer.WriteEndArray();
        writer.WriteEndObject();
    }

    /// <summary>
    /// Writes a single element in the document format. Exposed so features that transmit individual elements,
    /// such as collaboration, use exactly the same encoding as a saved document.
    /// </summary>
    /// <param name="writer">Destination. The element is written as one JSON object.</param>
    /// <param name="element">The element to write.</param>
    /// <param name="options">Serialization options.</param>
    public static void WriteElementTo(Utf8JsonWriter writer, BinkElement element, BinkSerializerOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(element);
        WriteElement(writer, element, options ?? BinkSerializerOptions.Default);
    }

    /// <summary>
    /// Reads a single element written by <see cref="WriteElementTo"/>. Returns null when the element type is
    /// unknown and <see cref="BinkDeserializerOptions.PreserveUnknownElements"/> is disabled.
    /// </summary>
    /// <param name="element">The JSON object.</param>
    /// <param name="options">Deserialization options, including the limits applied to untrusted input.</param>
    /// <exception cref="BinkFormatException">The JSON is not a valid element.</exception>
    public static BinkElement? ReadElementFrom(JsonElement element, BinkDeserializerOptions? options = null)
    {
        try
        {
            return ReadElement(element, options ?? BinkDeserializerOptions.Default);
        }
        catch (BinkFormatException)
        {
            throw;
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException or FormatException or OverflowException)
        {
            throw new BinkFormatException("The element is malformed: " + ex.Message, ex);
        }
    }

    private static void WriteElement(Utf8JsonWriter writer, BinkElement element, BinkSerializerOptions options)
    {
        if (element is BinkUnknownElement unknown)
        {
            // Preserved verbatim so a load/save round trip never destroys data written by a newer version.
            using var document = JsonDocument.Parse(unknown.RawJson);
            document.RootElement.WriteTo(writer);
            return;
        }

        writer.WriteStartObject();
        switch (element)
        {
            case BinkStroke stroke: WriteStroke(writer, stroke, options); break;
            case BinkRectangleShape rect: WriteRectangle(writer, rect, options); break;
            case BinkEllipseShape ellipse: WriteEllipse(writer, ellipse, options); break;
            case BinkLineShape line: WriteLine(writer, line, options); break;
            case BinkTextElement text: WriteText(writer, text, options); break;
            case BinkImageElement image: WriteImage(writer, image, options); break;
            case BinkHighlightElement highlight: WriteHighlight(writer, highlight, options); break;
            case BinkCalloutElement callout: WriteCallout(writer, callout, options); break;
            default: throw new InvalidOperationException($"{element.GetType().Name} cannot be serialized. Custom element types must be handled by the application before saving.");
        }
        writer.WriteEndObject();
    }

    private static void WriteCommon(Utf8JsonWriter writer, string type, BinkElement element, BinkSerializerOptions options)
    {
        writer.WriteString("type", type);
        writer.WriteString("id", element.Id);
        if (element.Version != 0) writer.WriteNumber("v", element.Version);
        if (!element.Transform.IsIdentity)
        {
            writer.WritePropertyName("transform");
            writer.WriteStartArray();
            Matrix3x2 m = element.Transform;
            writer.WriteNumberValue(Round(m.M11, 6));
            writer.WriteNumberValue(Round(m.M12, 6));
            writer.WriteNumberValue(Round(m.M21, 6));
            writer.WriteNumberValue(Round(m.M22, 6));
            writer.WriteNumberValue(Round(m.M31, options.Decimals));
            writer.WriteNumberValue(Round(m.M32, options.Decimals));
            writer.WriteEndArray();
        }
        if (element.Description is { Length: > 0 }) writer.WriteString("description", element.Description);
        WriteMetadata(writer, element.Metadata);
    }

    private static void WriteMetadata(Utf8JsonWriter writer, IReadOnlyDictionary<string, string>? metadata)
    {
        if (metadata is null || metadata.Count == 0) return;
        writer.WritePropertyName("metadata");
        writer.WriteStartObject();
        foreach ((string key, string value) in metadata.OrderBy(static kv => kv.Key, StringComparer.Ordinal)) writer.WriteString(key, value);
        writer.WriteEndObject();
    }

    private static void WriteStroke(Utf8JsonWriter writer, BinkStroke stroke, BinkSerializerOptions options)
    {
        WriteCommon(writer, "stroke", stroke, options);
        WriteBrush(writer, stroke.Brush, options);
        if (stroke.Input.Kind != BinkPointerKind.Unknown)
        {
            writer.WritePropertyName("input");
            writer.WriteStartObject();
            writer.WriteString("device", stroke.Input.Kind.ToString().ToLowerInvariant());
            if (stroke.Input.HasRealPressure) writer.WriteBoolean("pressure", true);
            writer.WriteEndObject();
        }

        writer.WritePropertyName("points");
        writer.WriteStartObject();
        writer.WriteNumber("count", stroke.Points.Count);
        if (options.PointEncoding == BinkPointEncoding.Binary)
        {
            writer.WriteString("encoding", "b64");
            writer.WriteBase64String("data", BinkPointCodec.Encode(stroke.Points, options.Quantization));
        }
        else
        {
            writer.WriteString("encoding", "json");
            WriteFloatArray(writer, "x", stroke.Points.X, options.Decimals);
            WriteFloatArray(writer, "y", stroke.Points.Y, options.Decimals);
            WriteFloatArray(writer, "p", stroke.Points.Pressure, 3);
            WriteFloatArray(writer, "t", stroke.Points.Time, 1);
            if (!stroke.Points.TiltX.IsEmpty)
            {
                WriteFloatArray(writer, "tx", stroke.Points.TiltX, 1);
                WriteFloatArray(writer, "ty", stroke.Points.TiltY, 1);
            }
            if (!stroke.Points.Twist.IsEmpty) WriteFloatArray(writer, "tw", stroke.Points.Twist, 1);
        }
        writer.WriteEndObject();
    }

    private static void WriteFloatArray(Utf8JsonWriter writer, string name, ReadOnlySpan<float> values, int decimals)
    {
        writer.WritePropertyName(name);
        writer.WriteStartArray();
        foreach (float v in values) writer.WriteNumberValue(Round(v, decimals));
        writer.WriteEndArray();
    }

    private static void WriteBrush(Utf8JsonWriter writer, BinkBrush brush, BinkSerializerOptions options)
    {
        writer.WritePropertyName("brush");
        writer.WriteStartObject();
        writer.WriteString("kind", brush.Kind);
        writer.WriteString("color", brush.Color.ToCss());
        writer.WriteNumber("width", Round(brush.Width, 3));
        if (brush.Opacity < 1f) writer.WriteNumber("opacity", Round(brush.Opacity, 3));
        if (brush.BlendMode != BinkBlendMode.Normal) writer.WriteString("blend", BlendName(brush.BlendMode));
        if (brush.Smoothing != 0.5f) writer.WriteNumber("smoothing", Round(brush.Smoothing, 3));
        if (brush.Spacing != 0.25f) writer.WriteNumber("spacing", Round(brush.Spacing, 3));
        if (!brush.SimulatePressure) writer.WriteBoolean("simulatePressure", false);
        if (brush.TaperStart != 0) writer.WriteNumber("taperStart", Round(brush.TaperStart, 3));
        if (brush.TaperEnd != 0) writer.WriteNumber("taperEnd", Round(brush.TaperEnd, 3));

        BinkPressureCurve curve = brush.PressureCurve;
        writer.WritePropertyName("pressure");
        writer.WriteStartObject();
        writer.WriteString("curve", curve.Kind.ToString().ToLowerInvariant());
        writer.WriteNumber("min", Round(curve.MinFactor, 3));
        writer.WriteNumber("max", Round(curve.MaxFactor, 3));
        if (curve.Kind == BinkPressureCurveKind.Custom) WriteFloatArray(writer, "samples", curve.Samples, 4);
        writer.WriteEndObject();
        writer.WriteEndObject();
    }

    private static void WriteShapeStyle(Utf8JsonWriter writer, BinkShapeStyle style, BinkSerializerOptions options)
    {
        writer.WritePropertyName("style");
        writer.WriteStartObject();
        writer.WriteString("stroke", style.StrokeColor.ToCss());
        writer.WriteNumber("strokeWidth", Round(style.StrokeWidth, 3));
        if (style.FillColor is { } fill) writer.WriteString("fill", fill.ToCss());
        if (style.Opacity < 1f) writer.WriteNumber("opacity", Round(style.Opacity, 3));
        if (style.BlendMode != BinkBlendMode.Normal) writer.WriteString("blend", BlendName(style.BlendMode));
        if (style.Dash is { Length: > 0 } dash) WriteFloatArray(writer, "dash", dash, 2);
        writer.WriteEndObject();
    }

    private static void WriteTextStyle(Utf8JsonWriter writer, BinkTextStyle style, BinkSerializerOptions options)
    {
        writer.WritePropertyName("textStyle");
        writer.WriteStartObject();
        writer.WriteString("font", style.FontFamily);
        writer.WriteNumber("size", Round(style.FontSize, 2));
        writer.WriteString("color", style.Color.ToCss());
        if (style.Bold) writer.WriteBoolean("bold", true);
        if (style.Italic) writer.WriteBoolean("italic", true);
        if (style.Align != BinkTextAlign.Left) writer.WriteString("align", style.Align.ToString().ToLowerInvariant());
        if (style.LineHeight != 1.25f) writer.WriteNumber("lineHeight", Round(style.LineHeight, 3));
        writer.WriteEndObject();
    }

    private static void WriteRect(Utf8JsonWriter writer, string name, BinkRect rect, int decimals)
    {
        writer.WritePropertyName(name);
        writer.WriteStartArray();
        writer.WriteNumberValue(Round(rect.X, decimals));
        writer.WriteNumberValue(Round(rect.Y, decimals));
        writer.WriteNumberValue(Round(rect.Width, decimals));
        writer.WriteNumberValue(Round(rect.Height, decimals));
        writer.WriteEndArray();
    }

    private static void WritePoint(Utf8JsonWriter writer, string name, Vector2 point, int decimals)
    {
        writer.WritePropertyName(name);
        writer.WriteStartArray();
        writer.WriteNumberValue(Round(point.X, decimals));
        writer.WriteNumberValue(Round(point.Y, decimals));
        writer.WriteEndArray();
    }

    private static void WriteRectangle(Utf8JsonWriter writer, BinkRectangleShape shape, BinkSerializerOptions options)
    {
        WriteCommon(writer, "rect", shape, options);
        WriteRect(writer, "rect", shape.Rect, options.Decimals);
        if (shape.CornerRadius > 0) writer.WriteNumber("radius", Round(shape.CornerRadius, 3));
        WriteShapeStyle(writer, shape.Style, options);
    }

    private static void WriteEllipse(Utf8JsonWriter writer, BinkEllipseShape shape, BinkSerializerOptions options)
    {
        WriteCommon(writer, "ellipse", shape, options);
        WriteRect(writer, "rect", shape.Rect, options.Decimals);
        WriteShapeStyle(writer, shape.Style, options);
    }

    private static void WriteLine(Utf8JsonWriter writer, BinkLineShape shape, BinkSerializerOptions options)
    {
        WriteCommon(writer, "line", shape, options);
        WritePoint(writer, "start", shape.Start, options.Decimals);
        WritePoint(writer, "end", shape.End, options.Decimals);
        if (shape.StartArrow) writer.WriteBoolean("startArrow", true);
        if (shape.EndArrow) writer.WriteBoolean("endArrow", true);
        if (shape.ArrowSize != 5f) writer.WriteNumber("arrowSize", Round(shape.ArrowSize, 3));
        WriteShapeStyle(writer, shape.Style, options);
    }

    private static void WriteText(Utf8JsonWriter writer, BinkTextElement text, BinkSerializerOptions options)
    {
        WriteCommon(writer, "text", text, options);
        WritePoint(writer, "position", text.Position, options.Decimals);
        writer.WriteString("text", text.Text);
        if (text.Size is { } size) WritePoint(writer, "size", size, options.Decimals);
        if (text.Opacity < 1f) writer.WriteNumber("opacity", Round(text.Opacity, 3));
        if (text.Background is { } bg) writer.WriteString("background", bg.ToCss());
        if (text.Padding != 4f) writer.WriteNumber("padding", Round(text.Padding, 3));
        WriteTextStyle(writer, text.Style, options);
    }

    private static void WriteImage(Utf8JsonWriter writer, BinkImageElement image, BinkSerializerOptions options)
    {
        WriteCommon(writer, "image", image, options);
        WriteRect(writer, "rect", image.Rect, options.Decimals);
        bool omit = image.Source.IsOmitted || (image.Source.IsEmbedded && !options.IncludeEmbeddedImages);
        writer.WriteString("src", omit ? string.Empty : image.Source.Uri);
        if (omit) writer.WriteBoolean("srcOmitted", true);
        if (image.Source.Width > 0) writer.WriteNumber("naturalWidth", image.Source.Width);
        if (image.Source.Height > 0) writer.WriteNumber("naturalHeight", image.Source.Height);
        if (image.Crop is { } crop) WriteRect(writer, "crop", crop, options.Decimals);
        if (image.Opacity < 1f) writer.WriteNumber("opacity", Round(image.Opacity, 3));
    }

    private static void WriteHighlight(Utf8JsonWriter writer, BinkHighlightElement highlight, BinkSerializerOptions options)
    {
        WriteCommon(writer, "highlight", highlight, options);
        writer.WriteString("color", highlight.Color.ToCss());
        if (highlight.Opacity != 0.5f) writer.WriteNumber("opacity", Round(highlight.Opacity, 3));
        writer.WritePropertyName("rects");
        writer.WriteStartArray();
        foreach (BinkRect r in highlight.Rects)
        {
            writer.WriteStartArray();
            writer.WriteNumberValue(Round(r.X, options.Decimals));
            writer.WriteNumberValue(Round(r.Y, options.Decimals));
            writer.WriteNumberValue(Round(r.Width, options.Decimals));
            writer.WriteNumberValue(Round(r.Height, options.Decimals));
            writer.WriteEndArray();
        }
        writer.WriteEndArray();
    }

    private static void WriteCallout(Utf8JsonWriter writer, BinkCalloutElement callout, BinkSerializerOptions options)
    {
        WriteCommon(writer, "callout", callout, options);
        WriteRect(writer, "box", callout.Box, options.Decimals);
        WritePoint(writer, "anchor", callout.Anchor, options.Decimals);
        writer.WriteString("text", callout.Text);
        if (callout.Padding != 6f) writer.WriteNumber("padding", Round(callout.Padding, 3));
        WriteShapeStyle(writer, callout.Style, options);
        WriteTextStyle(writer, callout.TextStyle, options);
    }

    private static string BlendName(BinkBlendMode mode) => mode switch
    {
        BinkBlendMode.Multiply => "multiply",
        BinkBlendMode.Screen => "screen",
        BinkBlendMode.DestinationOut => "erase",
        _ => "normal",
    };

    private static double Round(float value, int decimals)
    {
        if (!float.IsFinite(value)) return 0;
        return Math.Round(value, Math.Clamp(decimals, 0, 8), MidpointRounding.AwayFromZero);
    }

    // ---- Reading ---------------------------------------------------------------------------------------------------

    /// <summary>Parses a document from a JSON string.</summary>
    /// <exception cref="BinkFormatException">The input is not a valid Bink document.</exception>
    public static BinkDocument Deserialize(string json, BinkDeserializerOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(json);
        return Deserialize(Encoding.UTF8.GetBytes(json), options);
    }

    /// <summary>Parses a document from UTF-8 bytes.</summary>
    /// <exception cref="BinkFormatException">The input is not a valid Bink document.</exception>
    public static BinkDocument Deserialize(ReadOnlySpan<byte> utf8Json, BinkDeserializerOptions? options = null)
    {
        options ??= BinkDeserializerOptions.Default;
        if (utf8Json.Length > options.MaxInputBytes) throw new BinkFormatException($"The document is {utf8Json.Length} bytes; the limit is {options.MaxInputBytes}.");
        JsonDocument json;
        try
        {
            // Parsing from a sequence reader avoids copying the whole document before reading it.
            var reader = new Utf8JsonReader(utf8Json, new JsonReaderOptions { AllowTrailingCommas = false, CommentHandling = JsonCommentHandling.Disallow, MaxDepth = 32 });
            json = JsonDocument.ParseValue(ref reader);
        }
        catch (JsonException ex)
        {
            throw new BinkFormatException("The document is not valid JSON.", ex);
        }
        using (json)
        {
            try
            {
                return ReadDocument(json.RootElement, options);
            }
            catch (BinkFormatException)
            {
                throw;
            }
            catch (JsonException ex)
            {
                throw new BinkFormatException("The document is not valid JSON.", ex);
            }
            catch (Exception ex) when (ex is InvalidOperationException or ArgumentException or FormatException or OverflowException)
            {
                // Reading untrusted JSON reaches library code that reports malformed values in its own currency:
                // a string that is not valid UTF-8 throws InvalidOperationException from JsonElement.GetString,
                // an empty metadata key throws ArgumentException. Callers were promised one exception type, and
                // TryDeserialize only catches that one, so anything else here would escape as an unhandled crash.
                throw new BinkFormatException("The document is malformed: " + ex.Message, ex);
            }
        }
    }

    /// <summary>Parses a document from a stream.</summary>
    /// <exception cref="BinkFormatException">The input is not a valid Bink document.</exception>
    public static async Task<BinkDocument> DeserializeAsync(Stream stream, BinkDeserializerOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        options ??= BinkDeserializerOptions.Default;
        using var memory = new MemoryStream();
        var buffer = new byte[81920];
        int read;
        while ((read = await stream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) > 0)
        {
            if (memory.Length + read > options.MaxInputBytes) throw new BinkFormatException($"The document exceeds the size limit of {options.MaxInputBytes} bytes.");
            memory.Write(buffer, 0, read);
        }
        return Deserialize(memory.GetBuffer().AsSpan(0, (int)memory.Length), options);
    }

    /// <summary>Tries to parse a document, returning false instead of throwing on malformed input.</summary>
    public static bool TryDeserialize(string json, out BinkDocument? document, out string? error, BinkDeserializerOptions? options = null)
    {
        document = null;
        error = null;
        if (json is null)
        {
            error = "The document is null.";
            return false;
        }
        try
        {
            document = Deserialize(json, options);
            return true;
        }
        catch (BinkFormatException ex)
        {
            error = ex.Message;
            return false;
        }
    }

    /// <summary>
    /// Tries to parse a document from UTF-8 bytes, returning false instead of throwing on malformed input. This is
    /// the overload to use when loading a file: bytes read from disk or from a request body may not be valid UTF-8
    /// at all, and decoding them to a string first would either throw or silently replace the invalid sequences.
    /// </summary>
    public static bool TryDeserialize(ReadOnlySpan<byte> utf8Json, out BinkDocument? document, out string? error, BinkDeserializerOptions? options = null)
    {
        document = null;
        error = null;
        try
        {
            document = Deserialize(utf8Json, options);
            return true;
        }
        catch (BinkFormatException ex)
        {
            error = ex.Message;
            return false;
        }
    }

    private static BinkDocument ReadDocument(JsonElement root, BinkDeserializerOptions options)
    {
        if (root.ValueKind != JsonValueKind.Object) throw new BinkFormatException("The document must be a JSON object.");
        if (!root.TryGetProperty("format", out JsonElement format) || format.GetString() != FormatName)
            throw new BinkFormatException($"Not a Bink document (the \"format\" property must be \"{FormatName}\").");
        int version = root.TryGetProperty("version", out JsonElement versionElement) && versionElement.TryGetInt32(out int v) ? v : 0;
        if (version < MinimumSupportedVersion) throw new BinkFormatException($"Document version {version} is older than the minimum supported version {MinimumSupportedVersion}.");
        if (version > CurrentVersion) throw new BinkFormatException($"Document version {version} was written by a newer version of Bink (this build supports up to {CurrentVersion}). Upgrade the Bit.Bink package to open it.");

        Guid id = ReadGuid(root, "id") ?? Guid.NewGuid();
        BinkDocumentSettings settings = BinkDocumentSettings.Default;
        if (root.TryGetProperty("settings", out JsonElement settingsElement) && settingsElement.ValueKind == JsonValueKind.Object)
        {
            // Clamped exactly as UpdateSettings clamps them: a page size is a coordinate like any other, and a
            // file must not be able to declare a page no edit could ever produce.
            settings = new BinkDocumentSettings
            {
                Width = Math.Clamp(ReadFloat(settingsElement, "width", 800f), 0f, options.Limits.MaxCoordinate),
                Height = Math.Clamp(ReadFloat(settingsElement, "height", 600f), 0f, options.Limits.MaxCoordinate),
                Background = ReadColor(settingsElement, "background", BinkColor.White),
            };
        }

        var document = new BinkDocument(id, settings, options.Limits, withoutLayers: true);

        if (root.TryGetProperty("metadata", out JsonElement metadata) && metadata.ValueKind == JsonValueKind.Object)
        {
            foreach (JsonProperty property in metadata.EnumerateObject())
            {
                // An empty key cannot be looked up again, so it is dropped rather than failing the whole load.
                if (property.Name.Length > 0 && property.Value.ValueKind == JsonValueKind.String) document.SetMetadataCore(property.Name, property.Value.GetString());
            }
        }

        if (!root.TryGetProperty("layers", out JsonElement layers) || layers.ValueKind != JsonValueKind.Array)
            throw new BinkFormatException("The document must contain a \"layers\" array.");

        int layerCount = 0;
        var seenIds = new HashSet<Guid>();
        foreach (JsonElement layerElement in layers.EnumerateArray())
        {
            if (++layerCount > options.Limits.MaxLayers) throw new BinkFormatException($"The document declares more than {options.Limits.MaxLayers} layers.");
            ReadLayer(document, layerElement, options, seenIds);
        }
        if (document.Layers.Count == 0) document.InsertLayerCore(Guid.NewGuid(), new BinkLayerProperties { Name = "Drawing" }, null, null);
        return document;
    }

    private static void ReadLayer(BinkDocument document, JsonElement element, BinkDeserializerOptions options, HashSet<Guid> seenIds)
    {
        if (element.ValueKind != JsonValueKind.Object) throw new BinkFormatException("Each layer must be a JSON object.");
        var properties = new BinkLayerProperties
        {
            Name = ReadString(element, "name") ?? "Layer",
            IsVisible = ReadBool(element, "visible", true),
            IsLocked = ReadBool(element, "locked", false),
            Opacity = Math.Clamp(ReadFloat(element, "opacity", 1f), 0f, 1f),
            Metadata = ReadMetadata(element),
        };
        Guid id = ReadGuid(element, "id") ?? Guid.NewGuid();
        if (document.TryGetLayer(id, out _)) id = Guid.NewGuid();

        var elements = new List<BinkElement>();
        if (element.TryGetProperty("elements", out JsonElement elementArray) && elementArray.ValueKind == JsonValueKind.Array)
        {
            foreach (JsonElement e in elementArray.EnumerateArray())
            {
                BinkElement? parsed = ReadElement(e, options);
                if (parsed is null) continue;
                // A duplicate id would make the element unaddressable. Give it a fresh one rather than
                // rejecting the whole document: the drawing is still perfectly good.
                if (document.ContainsElement(parsed.Id) || seenIds.Contains(parsed.Id)) parsed = parsed with { Id = Guid.NewGuid() };
                seenIds.Add(parsed.Id);
                elements.Add(parsed);
            }
        }
        try
        {
            document.InsertLayerCore(id, properties, null, elements);
        }
        catch (InvalidOperationException ex)
        {
            // Document limits and validation throw; to a caller loading a file this is a malformed document,
            // which is what TryDeserialize promises to report rather than throw.
            throw new BinkFormatException(ex.Message, ex);
        }
    }

    private static BinkElement? ReadElement(JsonElement element, BinkDeserializerOptions options)
    {
        if (element.ValueKind != JsonValueKind.Object) throw new BinkFormatException("Each element must be a JSON object.");
        string type = ReadString(element, "type") ?? throw new BinkFormatException("Each element must have a \"type\".");

        BinkElement? result = type switch
        {
            "stroke" => ReadStroke(element, options),
            "rect" => ReadRectangle(element),
            "ellipse" => ReadEllipse(element),
            "line" => ReadLine(element),
            "text" => ReadText(element),
            "image" => ReadImage(element, options),
            "highlight" => ReadHighlight(element, options.Limits),
            "callout" => ReadCallout(element),
            _ => options.PreserveUnknownElements ? new BinkUnknownElement(type, element.GetRawText(), ReadRect(element, "bounds") ?? BinkRect.Empty) : null,
        };
        if (result is null) return null;

        Guid id = ReadGuid(element, "id") ?? Guid.NewGuid();
        int version = element.TryGetProperty("v", out JsonElement v) && v.TryGetInt32(out int parsed) ? Math.Max(parsed, 0) : 0;
        Matrix3x2 transform = ReadTransform(element, options.Limits);
        return result with
        {
            Id = id,
            Version = version,
            Transform = transform,
            Description = ReadString(element, "description"),
            Metadata = ReadMetadata(element),
        };
    }

    private static BinkStroke ReadStroke(JsonElement element, BinkDeserializerOptions options)
    {
        BinkBrush brush = ReadBrush(element, options);
        if (!element.TryGetProperty("points", out JsonElement pointsElement) || pointsElement.ValueKind != JsonValueKind.Object)
            throw new BinkFormatException("A stroke must have a \"points\" object.");

        BinkStrokePoints points;
        string encoding = ReadString(pointsElement, "encoding") ?? "json";
        if (encoding == "b64")
        {
            if (!pointsElement.TryGetProperty("data", out JsonElement data) || data.ValueKind != JsonValueKind.String)
                throw new BinkFormatException("Binary points require a \"data\" string.");
            byte[] bytes;
            try
            {
                bytes = data.GetBytesFromBase64();
            }
            catch (FormatException ex)
            {
                throw new BinkFormatException("Stroke point data is not valid base64.", ex);
            }
            try
            {
                points = BinkPointCodec.Decode(bytes, options.Limits.MaxPointsPerStroke);
            }
            catch (FormatException ex)
            {
                throw new BinkFormatException("Stroke point data is malformed.", ex);
            }
        }
        else
        {
            float[] xs = ReadFloatArray(pointsElement, "x", options.Limits.MaxPointsPerStroke);
            float[] ys = ReadFloatArray(pointsElement, "y", options.Limits.MaxPointsPerStroke);
            if (xs.Length != ys.Length) throw new BinkFormatException("Stroke x and y arrays must have the same length.");
            float[] ps = ReadFloatArray(pointsElement, "p", options.Limits.MaxPointsPerStroke);
            float[] ts = ReadFloatArray(pointsElement, "t", options.Limits.MaxPointsPerStroke);
            float[] tx = ReadFloatArray(pointsElement, "tx", options.Limits.MaxPointsPerStroke);
            float[] ty = ReadFloatArray(pointsElement, "ty", options.Limits.MaxPointsPerStroke);
            float[] tw = ReadFloatArray(pointsElement, "tw", options.Limits.MaxPointsPerStroke);
            if (ps.Length != 0 && ps.Length != xs.Length) throw new BinkFormatException("Stroke pressure array length must match the point count.");
            if (ts.Length != 0 && ts.Length != xs.Length) throw new BinkFormatException("Stroke time array length must match the point count.");
            if (tx.Length != ty.Length || (tx.Length != 0 && tx.Length != xs.Length)) throw new BinkFormatException("Stroke tilt array lengths must match the point count.");
            if (tw.Length != 0 && tw.Length != xs.Length) throw new BinkFormatException("Stroke twist array length must match the point count.");
            points = BinkStrokePoints.FromArrays(xs, ys, ps, ts, tx, ty, tw);
        }

        ValidateCoordinates(points, options.Limits);

        var input = BinkStrokeInputInfo.Unknown;
        if (element.TryGetProperty("input", out JsonElement inputElement) && inputElement.ValueKind == JsonValueKind.Object)
        {
            string? device = ReadString(inputElement, "device");
            var kind = device switch
            {
                "mouse" => BinkPointerKind.Mouse,
                "touch" => BinkPointerKind.Touch,
                "pen" => BinkPointerKind.Pen,
                _ => BinkPointerKind.Unknown,
            };
            input = new BinkStrokeInputInfo(kind, ReadBool(inputElement, "pressure", false));
        }
        return new BinkStroke(points, brush) { Input = input };
    }

    private static void ValidateCoordinates(BinkStrokePoints points, BinkDocumentLimits limits)
    {
        ReadOnlySpan<float> xs = points.X;
        ReadOnlySpan<float> ys = points.Y;
        float max = limits.MaxCoordinate;
        for (int i = 0; i < xs.Length; i++)
        {
            if (!float.IsFinite(xs[i]) || !float.IsFinite(ys[i]) || MathF.Abs(xs[i]) > max || MathF.Abs(ys[i]) > max)
                throw new BinkFormatException($"A stroke point is outside the allowed coordinate range (+/-{max}).");
        }
    }

    private static BinkBrush ReadBrush(JsonElement element, BinkDeserializerOptions options)
    {
        if (!element.TryGetProperty("brush", out JsonElement brushElement) || brushElement.ValueKind != JsonValueKind.Object)
            return new BinkPenBrush();

        string kind = ReadString(brushElement, "kind") ?? BinkPenBrush.KindName;
        if (!options.BrushRegistry.TryCreate(kind, out BinkBrush? brush) || brush is null)
        {
            // An unknown brush still renders with its stored appearance rather than being dropped.
            brush = new BinkPenBrush();
        }

        BinkPressureCurve curve = brush.PressureCurve;
        if (brushElement.TryGetProperty("pressure", out JsonElement pressure) && pressure.ValueKind == JsonValueKind.Object)
        {
            string curveName = ReadString(pressure, "curve") ?? "linear";
            float min = Math.Clamp(ReadFloat(pressure, "min", curve.MinFactor), 0f, 4f);
            float max = Math.Clamp(ReadFloat(pressure, "max", curve.MaxFactor), 0f, 4f);
            if (curveName == "custom")
            {
                float[] samples = ReadFloatArray(pressure, "samples", 1024);
                curve = samples.Length >= 2 ? BinkPressureCurve.FromSamples(samples, min, max) : new BinkPressureCurve(BinkPressureCurveKind.Linear, min, max);
            }
            else
            {
                var kindValue = curveName switch
                {
                    "easein" => BinkPressureCurveKind.EaseIn,
                    "easeout" => BinkPressureCurveKind.EaseOut,
                    "easeinout" => BinkPressureCurveKind.EaseInOut,
                    "constant" => BinkPressureCurveKind.Constant,
                    _ => BinkPressureCurveKind.Linear,
                };
                curve = new BinkPressureCurve(kindValue, min, max);
            }
        }

        float width = ReadFloat(brushElement, "width", brush.Width);
        if (!float.IsFinite(width) || width <= 0) width = 1f;
        width = MathF.Min(width, options.Limits.MaxStrokeWidth);

        return brush with
        {
            Color = ReadColor(brushElement, "color", brush.Color),
            Width = width,
            Opacity = Math.Clamp(ReadFloat(brushElement, "opacity", brush.Opacity), 0f, 1f),
            BlendMode = ReadBlend(brushElement, "blend", brush.BlendMode),
            Smoothing = Math.Clamp(ReadFloat(brushElement, "smoothing", brush.Smoothing), 0f, 1f),
            Spacing = Math.Clamp(ReadFloat(brushElement, "spacing", brush.Spacing), 0f, 10f),
            SimulatePressure = ReadBool(brushElement, "simulatePressure", brush.SimulatePressure),
            TaperStart = MathF.Max(ReadFloat(brushElement, "taperStart", brush.TaperStart), 0f),
            TaperEnd = MathF.Max(ReadFloat(brushElement, "taperEnd", brush.TaperEnd), 0f),
            PressureCurve = curve,
        };
    }

    private static BinkShapeStyle ReadShapeStyle(JsonElement element)
    {
        if (!element.TryGetProperty("style", out JsonElement style) || style.ValueKind != JsonValueKind.Object) return BinkShapeStyle.Default;
        float[] dash = ReadFloatArray(style, "dash", 32);
        return new BinkShapeStyle
        {
            StrokeColor = ReadColor(style, "stroke", BinkColor.Black),
            StrokeWidth = Math.Clamp(ReadFloat(style, "strokeWidth", 2f), 0f, 10000f),
            FillColor = style.TryGetProperty("fill", out JsonElement fill) && fill.ValueKind == JsonValueKind.String && BinkColor.TryParse(fill.GetString(), out BinkColor c) ? c : null,
            Opacity = Math.Clamp(ReadFloat(style, "opacity", 1f), 0f, 1f),
            BlendMode = ReadBlend(style, "blend", BinkBlendMode.Normal),
            Dash = dash.Length > 0 ? dash : null,
        };
    }

    private static BinkTextStyle ReadTextStyle(JsonElement element)
    {
        if (!element.TryGetProperty("textStyle", out JsonElement style) || style.ValueKind != JsonValueKind.Object) return BinkTextStyle.Default;
        string font = ReadString(style, "font") ?? BinkTextStyle.Default.FontFamily;
        // Font families are emitted into CSS and SVG; keep them to a safe character set.
        if (font.AsSpan().IndexOfAny(UnsafeFontCharacters) >= 0 || font.Length > 200) font = BinkTextStyle.Default.FontFamily;
        return new BinkTextStyle(
            font,
            Math.Clamp(ReadFloat(style, "size", 16f), 0.1f, 10000f),
            ReadColor(style, "color", BinkColor.Black),
            ReadBool(style, "bold", false),
            ReadBool(style, "italic", false),
            ReadString(style, "align") switch { "center" => BinkTextAlign.Center, "right" => BinkTextAlign.Right, _ => BinkTextAlign.Left },
            Math.Clamp(ReadFloat(style, "lineHeight", 1.25f), 0.5f, 10f));
    }

    private static BinkRectangleShape ReadRectangle(JsonElement element)
        => new(ReadRect(element, "rect") ?? BinkRect.Empty) { Style = ReadShapeStyle(element), CornerRadius = MathF.Max(ReadFloat(element, "radius", 0f), 0f) };

    private static BinkEllipseShape ReadEllipse(JsonElement element)
        => new(ReadRect(element, "rect") ?? BinkRect.Empty) { Style = ReadShapeStyle(element) };

    private static BinkLineShape ReadLine(JsonElement element)
        => new(ReadVector(element, "start") ?? Vector2.Zero, ReadVector(element, "end") ?? Vector2.Zero)
        {
            Style = ReadShapeStyle(element),
            StartArrow = ReadBool(element, "startArrow", false),
            EndArrow = ReadBool(element, "endArrow", false),
            ArrowSize = Math.Clamp(ReadFloat(element, "arrowSize", 5f), 0f, 100f),
        };

    private static BinkTextElement ReadText(JsonElement element)
    {
        string text = ReadString(element, "text") ?? string.Empty;
        if (text.Length > BinkTextElement.MaxLength) throw new BinkFormatException($"A text element exceeds the maximum length of {BinkTextElement.MaxLength} characters.");
        return new BinkTextElement(ReadVector(element, "position") ?? Vector2.Zero, text)
        {
            Style = ReadTextStyle(element),
            Size = ReadVector(element, "size"),
            Opacity = Math.Clamp(ReadFloat(element, "opacity", 1f), 0f, 1f),
            Background = element.TryGetProperty("background", out JsonElement bg) && bg.ValueKind == JsonValueKind.String && BinkColor.TryParse(bg.GetString(), out BinkColor c) ? c : null,
            Padding = Math.Clamp(ReadFloat(element, "padding", 4f), 0f, 1000f),
        };
    }

    private static BinkImageElement? ReadImage(JsonElement element, BinkDeserializerOptions options)
    {
        string uri = ReadString(element, "src") ?? string.Empty;
        BinkRect rect = ReadRect(element, "rect") ?? BinkRect.Empty;
        int naturalWidth = element.TryGetProperty("naturalWidth", out JsonElement nw) && nw.TryGetInt32(out int w) ? w : 0;
        int naturalHeight = element.TryGetProperty("naturalHeight", out JsonElement nh) && nh.TryGetInt32(out int h) ? h : 0;
        BinkImageSource? source;
        if (uri.Length == 0 && ReadBool(element, "srcOmitted", false))
        {
            // The payload was stripped on save. The element is kept without pixels so the application can
            // re-attach what it stored elsewhere; dropping it would lose the rect, crop and metadata with it.
            source = BinkImageSource.Omitted.WithSize(naturalWidth, naturalHeight);
        }
        else
        {
            if (!BinkImageSource.TryCreate(uri, naturalWidth, naturalHeight, options.AllowSvgImages, out source) || source is null) return null;
            if (!options.AllowRemoteImages && !source.IsEmbedded) return null;
        }
        return new BinkImageElement(source, rect)
        {
            Crop = ReadRect(element, "crop"),
            Opacity = Math.Clamp(ReadFloat(element, "opacity", 1f), 0f, 1f),
        };
    }

    private static BinkHighlightElement ReadHighlight(JsonElement element, BinkDocumentLimits limits)
    {
        var rects = new List<BinkRect>();
        if (element.TryGetProperty("rects", out JsonElement array) && array.ValueKind == JsonValueKind.Array)
        {
            // A highlight is a list of rectangles, so it needs its own bound: without one it would be the only
            // element type that can grow without limit.
            int maxRects = Math.Max(limits.MaxPointsPerStroke / 4, 64);
            if (array.GetArrayLength() > maxRects)
            {
                throw new BinkFormatException($"A highlight declares {array.GetArrayLength()} rectangles; the limit is {maxRects}.");
            }

            Span<float> values = stackalloc float[4];
            foreach (JsonElement r in array.EnumerateArray())
            {
                if (r.ValueKind != JsonValueKind.Array) continue;
                int i = 0;
                foreach (JsonElement value in r.EnumerateArray())
                {
                    if (i >= 4) break;
                    values[i++] = value.TryGetSingle(out float f) && float.IsFinite(f) ? f : 0f;
                }
                if (i == 4) rects.Add(new BinkRect(values[0], values[1], values[2], values[3]));
            }
        }
        return new BinkHighlightElement(rects)
        {
            Color = ReadColor(element, "color", new BinkColor(255, 235, 59)),
            Opacity = Math.Clamp(ReadFloat(element, "opacity", 0.5f), 0f, 1f),
        };
    }

    private static BinkCalloutElement ReadCallout(JsonElement element)
    {
        string text = ReadString(element, "text") ?? string.Empty;
        if (text.Length > BinkTextElement.MaxLength) throw new BinkFormatException($"A callout exceeds the maximum text length of {BinkTextElement.MaxLength} characters.");
        return new BinkCalloutElement(ReadRect(element, "box") ?? BinkRect.Empty, ReadVector(element, "anchor") ?? Vector2.Zero, text)
        {
            Style = ReadShapeStyle(element),
            TextStyle = ReadTextStyle(element),
            Padding = Math.Clamp(ReadFloat(element, "padding", 6f), 0f, 1000f),
        };
    }

    // ---- JSON helpers ----------------------------------------------------------------------------------------------

    private static string? ReadString(JsonElement element, string name)
        => element.TryGetProperty(name, out JsonElement value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    private static bool ReadBool(JsonElement element, string name, bool fallback)
        => element.TryGetProperty(name, out JsonElement value) && value.ValueKind is JsonValueKind.True or JsonValueKind.False ? value.GetBoolean() : fallback;

    private static float ReadFloat(JsonElement element, string name, float fallback)
    {
        if (!element.TryGetProperty(name, out JsonElement value) || value.ValueKind != JsonValueKind.Number) return fallback;
        return value.TryGetSingle(out float f) && float.IsFinite(f) ? f : fallback;
    }

    private static Guid? ReadGuid(JsonElement element, string name)
        => element.TryGetProperty(name, out JsonElement value) && value.ValueKind == JsonValueKind.String && value.TryGetGuid(out Guid guid) ? guid : null;

    private static BinkColor ReadColor(JsonElement element, string name, BinkColor fallback)
        => element.TryGetProperty(name, out JsonElement value) && value.ValueKind == JsonValueKind.String && BinkColor.TryParse(value.GetString(), out BinkColor color) ? color : fallback;

    private static BinkBlendMode ReadBlend(JsonElement element, string name, BinkBlendMode fallback)
        => ReadString(element, name) switch
        {
            "normal" => BinkBlendMode.Normal,
            "multiply" => BinkBlendMode.Multiply,
            "screen" => BinkBlendMode.Screen,
            "erase" => BinkBlendMode.DestinationOut,
            _ => fallback,
        };

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

    private static BinkRect? ReadRect(JsonElement element, string name)
    {
        if (!element.TryGetProperty(name, out JsonElement value) || value.ValueKind != JsonValueKind.Array) return null;
        Span<float> values = stackalloc float[4];
        int i = 0;
        foreach (JsonElement v in value.EnumerateArray())
        {
            if (i >= 4) break;
            values[i++] = v.TryGetSingle(out float f) && float.IsFinite(f) ? f : 0f;
        }
        return i == 4 ? new BinkRect(values[0], values[1], values[2], values[3]) : null;
    }

    private static Vector2? ReadVector(JsonElement element, string name)
    {
        if (!element.TryGetProperty(name, out JsonElement value) || value.ValueKind != JsonValueKind.Array) return null;
        Span<float> values = stackalloc float[2];
        int i = 0;
        foreach (JsonElement v in value.EnumerateArray())
        {
            if (i >= 2) break;
            values[i++] = v.TryGetSingle(out float f) && float.IsFinite(f) ? f : 0f;
        }
        return i == 2 ? new Vector2(values[0], values[1]) : null;
    }

    private static Matrix3x2 ReadTransform(JsonElement element, BinkDocumentLimits limits)
    {
        if (!element.TryGetProperty("transform", out JsonElement value) || value.ValueKind != JsonValueKind.Array) return Matrix3x2.Identity;
        Span<float> m = stackalloc float[6];
        int i = 0;
        foreach (JsonElement v in value.EnumerateArray())
        {
            if (i >= 6) break;
            m[i++] = v.TryGetSingle(out float f) && float.IsFinite(f) ? f : 0f;
        }
        if (i != 6) return Matrix3x2.Identity;
        var matrix = new Matrix3x2(m[0], m[1], m[2], m[3], m[4], m[5]);
        // A singular or wildly scaled transform would make elements un-selectable or explode their bounds.
        float determinant = matrix.GetDeterminant();
        if (!float.IsFinite(determinant) || MathF.Abs(determinant) < 1e-9f) return Matrix3x2.Identity;
        if (MathF.Abs(m[4]) > limits.MaxCoordinate || MathF.Abs(m[5]) > limits.MaxCoordinate) return Matrix3x2.Identity;
        return matrix;
    }

    private static float[] ReadFloatArray(JsonElement element, string name, int maxLength)
    {
        if (!element.TryGetProperty(name, out JsonElement value) || value.ValueKind != JsonValueKind.Array) return [];
        int length = value.GetArrayLength();
        if (length > maxLength) throw new BinkFormatException($"The \"{name}\" array has {length} entries; the limit is {maxLength}.");
        float[] result = new float[length];
        int i = 0;
        foreach (JsonElement v in value.EnumerateArray())
        {
            result[i++] = v.TryGetSingle(out float f) && float.IsFinite(f) ? f : 0f;
        }
        return result;
    }
}

/// <summary>Thrown when a document cannot be read because it is malformed, too large or written by a newer version.</summary>
public sealed class BinkFormatException : Exception
{
    /// <summary>Creates the exception.</summary>
    public BinkFormatException() : base("The ink document is not valid.") { }

    /// <summary>Creates the exception with a message.</summary>
    public BinkFormatException(string message) : base(message) { }

    /// <summary>Creates the exception with a message and inner exception.</summary>
    public BinkFormatException(string message, Exception innerException) : base(message, innerException) { }
}
