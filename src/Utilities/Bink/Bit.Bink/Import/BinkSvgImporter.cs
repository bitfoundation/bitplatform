using System.Globalization;
using System.Numerics;
using System.Xml;

namespace Bit.Bink;

/// <summary>What <see cref="BinkSvgImporter"/> reads, and how much of it.</summary>
public sealed record BinkSvgImportOptions
{
    /// <summary>Safe defaults for SVG from anywhere.</summary>
    public static readonly BinkSvgImportOptions Default = new();

    /// <summary>The longest SVG accepted, in characters.</summary>
    public int MaxLength { get; init; } = 16 * 1024 * 1024;

    /// <summary>The most elements an import may produce.</summary>
    public int MaxElements { get; init; } = 10_000;

    /// <summary>The most path segments all paths together may have.</summary>
    public int MaxSegments { get; init; } = 500_000;

    /// <summary>Whether <c>text</c> elements become text elements. Their layout is approximate: see the remarks of <see cref="BinkSvgImporter"/>.</summary>
    public bool ImportText { get; init; } = true;

    /// <summary>
    /// Whether <c>image</c> elements whose picture is embedded as a raster <c>data:</c> URI are imported. Images
    /// that refer to a file or a URL are never loaded, whatever this says.
    /// </summary>
    public bool ImportImages { get; init; } = true;
}

/// <summary>What <see cref="BinkSvgImporter"/> produced.</summary>
public sealed class BinkSvgImportResult
{
    internal BinkSvgImportResult(IReadOnlyList<BinkElement> elements, IReadOnlyList<string> warnings, float width, float height)
    {
        Elements = elements;
        Warnings = warnings;
        Width = width;
        Height = height;
        BinkRect bounds = BinkRect.Empty;
        foreach (BinkElement element in elements) bounds = bounds.Union(element.Bounds);
        Bounds = bounds;
    }

    /// <summary>The elements, bottom-most first, in the SVG's user units.</summary>
    public IReadOnlyList<BinkElement> Elements { get; }

    /// <summary>
    /// What the SVG used that could not be imported, one line per kind of thing (gradients, filters, a stylesheet,
    /// a remote image...). Empty when everything was imported.
    /// </summary>
    public IReadOnlyList<string> Warnings { get; }

    /// <summary>The SVG's own width, or 0 when it states none.</summary>
    public float Width { get; }

    /// <summary>The SVG's own height, or 0 when it states none.</summary>
    public float Height { get; }

    /// <summary>The union of the elements' bounds.</summary>
    public BinkRect Bounds { get; }
}

/// <summary>
/// Turns SVG into Bink elements: paths, rectangles, circles, ellipses, lines, polylines and polygons become editable
/// vector elements with their colours, widths, dashes, opacity and transforms, so an icon or a diagram from another
/// tool can be moved, recoloured and annotated like anything drawn here.
/// </summary>
/// <remarks>
/// <para>
/// SVG is untrusted input and is read defensively: DTDs are refused (so no entity expansion), nothing is fetched
/// (no external entities, stylesheets, fonts or images by URL), scripts, foreign objects and event handlers are
/// ignored, and the length, the number of elements and the number of path segments are bounded.
/// </para>
/// <para>
/// What has no Bink equivalent is left out and reported in <see cref="BinkSvgImportResult.Warnings"/> rather than
/// approximated silently: gradients and patterns (a shape filled with one is filled grey), filters, masks and clip
/// paths, <c>use</c> references, CSS stylesheets (presentation attributes and <c>style</c> attributes are applied),
/// and nested viewports' own view boxes. A group's opacity is applied to each of its elements, which differs where
/// they overlap. Text keeps its position, size, colour, weight and anchor, but the browser lays it out with the fonts it
/// has, and only the first position of a <c>text</c> element is used.
/// </para>
/// </remarks>
public static class BinkSvgImporter
{
    /// <summary>Reads SVG markup into elements.</summary>
    /// <exception cref="FormatException">The markup is not well-formed SVG, or exceeds the limits.</exception>
    public static BinkSvgImportResult Import(string svg, BinkSvgImportOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(svg);
        return TryImport(svg, out BinkSvgImportResult? result, out string? error, options) ? result! : throw new FormatException(error);
    }

    /// <summary>Reads SVG markup into elements, reporting a problem instead of throwing.</summary>
    public static bool TryImport(string? svg, out BinkSvgImportResult? result, out string? error, BinkSvgImportOptions? options = null)
    {
        result = null;
        options ??= BinkSvgImportOptions.Default;
        if (string.IsNullOrWhiteSpace(svg))
        {
            error = "There is no SVG.";
            return false;
        }
        if (svg.Length > options.MaxLength)
        {
            error = $"The SVG is {svg.Length} characters long; the limit is {options.MaxLength}.";
            return false;
        }
        try
        {
            result = new Reader(options).Read(svg);
            error = null;
            return true;
        }
        catch (XmlException ex)
        {
            error = "The SVG is not well-formed XML: " + ex.Message;
        }
        catch (FormatException ex)
        {
            error = ex.Message;
        }
        catch (ArgumentException ex)
        {
            error = "The SVG contains a value that cannot be used: " + ex.Message;
        }
        return false;
    }

    /// <summary>The presentation properties in effect for an element, inherited down the tree.</summary>
    private sealed record Paint
    {
        public string Fill { get; init; } = "black";
        public string Stroke { get; init; } = "none";
        public float StrokeWidth { get; init; } = 1f;
        public float FillOpacity { get; init; } = 1f;
        public float StrokeOpacity { get; init; } = 1f;
        public BinkFillRule FillRule { get; init; } = BinkFillRule.NonZero;
        public float[]? Dash { get; init; }
        public BinkLineCap Cap { get; init; } = BinkLineCap.Butt;
        public BinkLineJoin Join { get; init; } = BinkLineJoin.Miter;
        public string? FontFamily { get; init; }
        public float FontSize { get; init; } = 16f;
        public bool Bold { get; init; }
        public bool Italic { get; init; }
        public string Anchor { get; init; } = "start";
        public string Color { get; init; } = "black";
        public bool Hidden { get; init; }
        // Not inherited in SVG, but a group's opacity applies to everything in it, so it is carried down multiplied.
        public float Opacity { get; init; } = 1f;
    }

    private sealed class Reader(BinkSvgImportOptions options)
    {
        private static readonly HashSet<string> Skipped = new(StringComparer.Ordinal)
        {
            "defs", "symbol", "marker", "title", "desc", "metadata", "linearGradient", "radialGradient", "pattern",
            "clipPath", "mask", "filter", "style", "script", "foreignObject",
        };

        private readonly List<BinkElement> _elements = [];
        private readonly SortedSet<string> _warnings = new(StringComparer.Ordinal);
        private int _segments;
        private float _width;
        private float _height;

        public BinkSvgImportResult Read(string svg)
        {
            var settings = new XmlReaderSettings
            {
                // A DOCTYPE is skipped, not refused: Illustrator and older Inkscape write one. Nothing in it is
                // expanded and, with no resolver, nothing it names is fetched.
                DtdProcessing = DtdProcessing.Ignore,
                XmlResolver = null,
                IgnoreComments = true,
                IgnoreProcessingInstructions = true,
                MaxCharactersInDocument = options.MaxLength,
            };
            using var reader = XmlReader.Create(new StringReader(svg), settings);
            reader.MoveToContent();
            if (reader.NodeType != XmlNodeType.Element || reader.LocalName != "svg") throw new FormatException("The document's root element is not svg.");
            ReadRoot(reader);
            return new BinkSvgImportResult(_elements, [.. _warnings], _width, _height);
        }

        private void ReadRoot(XmlReader reader)
        {
            Dictionary<string, string> attributes = Attributes(reader);
            float width = Length(attributes.GetValueOrDefault("width"), 0f, 16f);
            float height = Length(attributes.GetValueOrDefault("height"), 0f, 16f);
            Matrix3x2 transform = Matrix3x2.Identity;
            if (attributes.TryGetValue("viewBox", out string? viewBox) && Numbers(viewBox) is [float minX, float minY, float vbWidth, float vbHeight] && vbWidth > 0 && vbHeight > 0)
            {
                // Mapped onto the stated size, centred and uniformly scaled (xMidYMid meet) unless told not to keep the
                // proportions. With no size, one view box unit is one document unit.
                if (width <= 0) width = vbWidth;
                if (height <= 0) height = vbHeight;
                float sx = width / vbWidth;
                float sy = height / vbHeight;
                bool stretch = attributes.GetValueOrDefault("preserveAspectRatio")?.Trim().StartsWith("none", StringComparison.Ordinal) == true;
                if (!stretch) sx = sy = MathF.Min(sx, sy);
                float dx = stretch ? 0f : (width - vbWidth * sx) / 2f;
                float dy = stretch ? 0f : (height - vbHeight * sy) / 2f;
                transform = Matrix3x2.CreateTranslation(-minX, -minY) * Matrix3x2.CreateScale(sx, sy) * Matrix3x2.CreateTranslation(dx, dy);
            }
            _width = width;
            _height = height;
            Paint paint = Inherit(new Paint(), attributes);
            if (reader.IsEmptyElement) return;
            ReadChildren(reader, paint, transform);
        }

        private void ReadChildren(XmlReader reader, Paint parent, Matrix3x2 parentTransform)
        {
            int depth = reader.Depth;
            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.EndElement && reader.Depth == depth) return;
                if (reader.NodeType != XmlNodeType.Element) continue;
                if (reader.NamespaceURI is not ("http://www.w3.org/2000/svg" or ""))
                {
                    // Not XmlReader.Skip, which moves on to the next node, so this loop's Read would step over it.
                    SkipElement(reader);
                    continue;
                }
                ReadElement(reader, parent, parentTransform);
            }
        }

        private void ReadElement(XmlReader reader, Paint parent, Matrix3x2 parentTransform)
        {
            string name = reader.LocalName;
            if (Skipped.Contains(name))
            {
                switch (name)
                {
                    case "style": Warn("CSS stylesheets are not applied; presentation attributes and style attributes are."); break;
                    case "script" or "foreignObject": Warn($"{name} elements are ignored."); break;
                }
                SkipElement(reader);
                return;
            }
            if (reader.Depth > 64) throw new FormatException("The SVG is nested more than 64 levels deep.");

            Dictionary<string, string> attributes = Attributes(reader);
            Paint paint = Inherit(parent, attributes);
            Matrix3x2 transform = Transform(attributes.GetValueOrDefault("transform")) * parentTransform;
            if (paint.Hidden)
            {
                SkipElement(reader);
                return;
            }

            switch (name)
            {
                case "g" or "a" or "switch":
                    if (!reader.IsEmptyElement) ReadChildren(reader, paint, transform);
                    return;
                case "svg":
                {
                    // A nested viewport is placed by its position; its own view box is not mapped.
                    if (attributes.ContainsKey("viewBox")) Warn("A nested svg element's viewBox is not applied.");
                    Matrix3x2 offset = Matrix3x2.CreateTranslation(Length(attributes.GetValueOrDefault("x"), 0f, paint.FontSize), Length(attributes.GetValueOrDefault("y"), 0f, paint.FontSize));
                    if (!reader.IsEmptyElement) ReadChildren(reader, paint, offset * transform);
                    return;
                }
                case "text":
                {
                    string text = reader.IsEmptyElement ? string.Empty : TextContent(reader);
                    if (options.ImportText) AddText(attributes, paint, transform, text);
                    else Warn("Text was left out.");
                    return;
                }
                case "use":
                    Warn("use elements, which repeat other parts of the drawing, are not imported.");
                    break;
                default:
                    AddShape(name, attributes, paint, transform);
                    break;
            }
            if (!reader.IsEmptyElement) SkipElement(reader);
        }

        private static void SkipElement(XmlReader reader)
        {
            // Skip moves past the element's end; the caller's loop then reads the next node.
            if (reader.IsEmptyElement) return;
            int depth = reader.Depth;
            while (reader.Read() && !(reader.NodeType == XmlNodeType.EndElement && reader.Depth == depth)) { }
        }

        private static string TextContent(XmlReader reader)
        {
            var text = new System.Text.StringBuilder();
            int depth = reader.Depth;
            while (reader.Read() && !(reader.NodeType == XmlNodeType.EndElement && reader.Depth == depth))
            {
                if (reader.NodeType is XmlNodeType.Text or XmlNodeType.CDATA or XmlNodeType.SignificantWhitespace or XmlNodeType.Whitespace) text.Append(reader.Value);
            }
            // SVG collapses runs of white space into one space by default.
            return string.Join(' ', text.ToString().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        }

        private void AddShape(string name, Dictionary<string, string> attributes, Paint paint, Matrix3x2 transform)
        {
            float F(string key) => Length(attributes.GetValueOrDefault(key), 0f, paint.FontSize);
            BinkElement? element;
            switch (name)
            {
                case "rect":
                {
                    float w = F("width"), h = F("height");
                    if (w <= 0 || h <= 0) return;
                    float rx = F("rx"), ry = F("ry");
                    float radius = MathF.Min(MathF.Max(rx, ry), MathF.Min(w, h) / 2f);
                    element = new BinkRectangleShape(new BinkRect(F("x"), F("y"), w, h)) { CornerRadius = MathF.Max(radius, 0f), Style = Style(paint, closed: true) };
                    break;
                }
                case "circle":
                {
                    float r = F("r");
                    if (r <= 0) return;
                    element = new BinkEllipseShape(BinkRect.FromCenter(new Vector2(F("cx"), F("cy")), r * 2, r * 2)) { Style = Style(paint, closed: true) };
                    break;
                }
                case "ellipse":
                {
                    float rx = F("rx"), ry = F("ry");
                    if (rx <= 0 || ry <= 0) return;
                    element = new BinkEllipseShape(BinkRect.FromCenter(new Vector2(F("cx"), F("cy")), rx * 2, ry * 2)) { Style = Style(paint, closed: true) };
                    break;
                }
                case "line":
                    element = new BinkLineShape(new Vector2(F("x1"), F("y1")), new Vector2(F("x2"), F("y2"))) { Style = Style(paint, closed: false) };
                    break;
                case "polyline" or "polygon":
                {
                    float[] numbers = Numbers(attributes.GetValueOrDefault("points") ?? string.Empty);
                    int count = Math.Min(numbers.Length / 2, BinkPolygonShape.MaxPoints);
                    if (count < 2) return;
                    var points = new Vector2[count];
                    for (int i = 0; i < count; i++) points[i] = new Vector2(numbers[2 * i], numbers[2 * i + 1]);
                    bool closed = name == "polygon";
                    element = new BinkPolygonShape(points, closed) { Style = Style(paint, closed) };
                    break;
                }
                case "path":
                {
                    string data = attributes.GetValueOrDefault("d") ?? string.Empty;
                    int budget = options.MaxSegments - _segments;
                    if (!BinkSvgPathData.TryParse(data, out BinkPathGeometry? geometry, out string? error, Math.Min(Math.Max(budget, 1), BinkPathElement.MaxSegments)))
                    {
                        bool tooLong = budget <= 0 || error?.Contains("segments", StringComparison.Ordinal) == true;
                        // One path over what a path element holds is left out; only running past the total stops the import.
                        if (tooLong && budget > BinkPathElement.MaxSegments) Warn($"A path with more than {BinkPathElement.MaxSegments} segments was left out.");
                        else if (tooLong) throw new FormatException($"The SVG's paths have more than {options.MaxSegments} segments.");
                        else Warn("A path with malformed data was left out: " + error);
                        return;
                    }
                    if (geometry!.IsEmpty) return;
                    _segments += geometry.VerbCount;
                    element = new BinkPathElement(geometry) { Style = Style(paint, closed: true), FillRule = paint.FillRule };
                    break;
                }
                case "image":
                    element = Image(attributes, paint);
                    if (element is null) return;
                    break;
                default:
                    Warn($"{name} elements are not imported.");
                    return;
            }
            if (TryClean(transform, out Matrix3x2 cleaned)) Add(element with { Transform = cleaned });
        }

        private BinkElement? Image(Dictionary<string, string> attributes, Paint paint)
        {
            string? href = attributes.GetValueOrDefault("href") ?? attributes.GetValueOrDefault("xlink:href");
            float w = Length(attributes.GetValueOrDefault("width"), 0f, paint.FontSize);
            float h = Length(attributes.GetValueOrDefault("height"), 0f, paint.FontSize);
            if (!options.ImportImages || string.IsNullOrEmpty(href) || w <= 0 || h <= 0) return null;
            // Only a picture carried inside the SVG: following a reference would make importing a file fetch a URL.
            if (!href.StartsWith("data:", StringComparison.OrdinalIgnoreCase) || !BinkImageSource.TryCreate(href, 0, 0, allowSvg: false, out BinkImageSource? source) || source is null)
            {
                Warn("Images that are not embedded raster pictures are not imported.");
                return null;
            }
            return new BinkImageElement(source, new BinkRect(Length(attributes.GetValueOrDefault("x"), 0f, paint.FontSize), Length(attributes.GetValueOrDefault("y"), 0f, paint.FontSize), w, h)) { Opacity = paint.Opacity };
        }

        private void AddText(Dictionary<string, string> attributes, Paint paint, Matrix3x2 transform, string text)
        {
            if (text.Length == 0) return;
            if (text.Length > BinkTextElement.MaxLength) text = text[..BinkTextElement.MaxLength];
            float[] xs = Numbers(attributes.GetValueOrDefault("x") ?? "0");
            float[] ys = Numbers(attributes.GetValueOrDefault("y") ?? "0");
            float x = xs.Length > 0 ? xs[0] : 0f;
            float y = ys.Length > 0 ? ys[0] : 0f;
            BinkColor color = Color(paint.Fill, paint) ?? BinkColor.Black;
            var style = BinkTextStyle.Default with
            {
                FontSize = Math.Clamp(paint.FontSize, 0.1f, 10000f),
                Color = color.WithAlpha(color.Alpha * paint.FillOpacity * paint.Opacity),
                Bold = paint.Bold,
                Italic = paint.Italic,
                FontFamily = SafeFont(paint.FontFamily) ?? BinkTextStyle.Default.FontFamily,
                Align = paint.Anchor switch { "middle" => BinkTextAlign.Center, "end" => BinkTextAlign.Right, _ => BinkTextAlign.Left },
            };
            // SVG places text on its baseline; a text element's position is the top of the line, about an ascent above.
            var element = new BinkTextElement(new Vector2(x, y - style.FontSize * 0.8f), text) { Style = style, Padding = 0f };
            if (TryClean(transform, out Matrix3x2 cleaned)) Add(element with { Transform = cleaned });
        }

        private void Add(BinkElement element)
        {
            if (_elements.Count >= options.MaxElements) throw new FormatException($"The SVG has more than {options.MaxElements} elements.");
            _elements.Add(element);
        }

        private BinkShapeStyle Style(Paint paint, bool closed)
        {
            BinkColor? fill = closed ? Color(paint.Fill, paint) : null;
            BinkColor? stroke = Color(paint.Stroke, paint);
            return new BinkShapeStyle
            {
                FillColor = fill is { } f ? f.WithAlpha(f.Alpha * paint.FillOpacity) : null,
                StrokeColor = stroke is { } s ? s.WithAlpha(s.Alpha * paint.StrokeOpacity) : BinkColor.Black,
                StrokeWidth = stroke is null ? 0f : Math.Clamp(paint.StrokeWidth, 0f, 10000f),
                Opacity = Math.Clamp(paint.Opacity, 0f, 1f),
                Dash = paint.Dash,
                LineCap = paint.Cap,
                LineJoin = paint.Join,
            };
        }

        /// <summary>A paint value as a colour; null for none. Gradients and patterns become grey, with a warning.</summary>
        private BinkColor? Color(string value, Paint paint)
        {
            string v = value.Trim();
            if (v.Length == 0 || v == "none" || v == "transparent") return null;
            if (v.StartsWith("url(", StringComparison.OrdinalIgnoreCase))
            {
                Warn("Gradients and patterns are not imported; shapes painted with one are painted grey.");
                // "url(#gradient) red" names the colour to use when the reference cannot be: use it when given.
                int close = v.IndexOf(')');
                string fallback = close >= 0 ? v[(close + 1)..].Trim() : string.Empty;
                return fallback.Length > 0 && fallback != "none" && BinkColor.TryParse(Named(fallback), out BinkColor parsed) ? parsed : new BinkColor(128, 128, 128);
            }
            if (v == "currentColor") v = paint.Color;
            if (BinkColor.TryParse(Named(v), out BinkColor color)) return color;
            Warn($"The colour \"{(v.Length > 40 ? v[..40] : v)}\" is not understood and is drawn black.");
            return BinkColor.Black;
        }

        private static string Named(string value) => value.ToLowerInvariant() switch
        {
            "black" => "#000000",
            "white" => "#ffffff",
            "red" => "#ff0000",
            "lime" => "#00ff00",
            "green" => "#008000",
            "blue" => "#0000ff",
            "yellow" => "#ffff00",
            "orange" => "#ffa500",
            "purple" => "#800080",
            "gray" or "grey" => "#808080",
            "silver" => "#c0c0c0",
            "maroon" => "#800000",
            "navy" => "#000080",
            "teal" => "#008080",
            "olive" => "#808000",
            "fuchsia" or "magenta" => "#ff00ff",
            "aqua" or "cyan" => "#00ffff",
            "pink" => "#ffc0cb",
            "brown" => "#a52a2a",
            "gold" => "#ffd700",
            "darkgray" or "darkgrey" => "#a9a9a9",
            "lightgray" or "lightgrey" => "#d3d3d3",
            _ => value,
        };

        private static string? SafeFont(string? family)
        {
            if (string.IsNullOrWhiteSpace(family) || family.Length > 200) return null;
            // The value reaches CSS and SVG again on export; the same characters the document format refuses are refused.
            return family.AsSpan().IndexOfAny("<>\"'{}();\\") >= 0 ? null : family.Trim();
        }

        private Paint Inherit(Paint parent, Dictionary<string, string> attributes)
        {
            // Presentation attributes first, then the style attribute, which overrides them as CSS does.
            var properties = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach ((string key, string value) in attributes) properties[key] = value;
            if (attributes.TryGetValue("style", out string? style))
            {
                foreach (string declaration in style.Split(';', StringSplitOptions.RemoveEmptyEntries))
                {
                    int colon = declaration.IndexOf(':');
                    if (colon <= 0) continue;
                    string value = declaration[(colon + 1)..].Replace("!important", string.Empty, StringComparison.OrdinalIgnoreCase).Trim();
                    properties[declaration[..colon].Trim()] = value;
                }
            }
            if (properties.ContainsKey("filter") && properties["filter"] != "none") Warn("Filters are not imported.");
            if (properties.TryGetValue("mask", out string? mask) && mask != "none") Warn("Masks are not imported.");
            if (properties.TryGetValue("clip-path", out string? clip) && clip != "none") Warn("Clip paths are not imported; clipped shapes are drawn whole.");

            Paint paint = parent with { Opacity = 1f };
            foreach ((string key, string value) in properties)
            {
                string v = value.Trim();
                if (v == "inherit") continue;
                paint = key switch
                {
                    "fill" => paint with { Fill = v },
                    "stroke" => paint with { Stroke = v },
                    "color" => paint with { Color = v },
                    "stroke-width" => paint with { StrokeWidth = MathF.Max(Length(v, paint.StrokeWidth, paint.FontSize), 0f) },
                    "fill-opacity" => paint with { FillOpacity = Fraction(v, paint.FillOpacity) },
                    "stroke-opacity" => paint with { StrokeOpacity = Fraction(v, paint.StrokeOpacity) },
                    "opacity" => paint with { Opacity = Fraction(v, 1f) },
                    "fill-rule" => paint with { FillRule = v == "evenodd" ? BinkFillRule.EvenOdd : BinkFillRule.NonZero },
                    "stroke-dasharray" => paint with { Dash = DashArray(v, paint.FontSize) },
                    "stroke-linecap" => paint with { Cap = v switch { "round" => BinkLineCap.Round, "square" => BinkLineCap.Square, _ => BinkLineCap.Butt } },
                    "stroke-linejoin" => paint with { Join = v switch { "round" => BinkLineJoin.Round, "bevel" => BinkLineJoin.Bevel, _ => BinkLineJoin.Miter } },
                    "font-family" => paint with { FontFamily = v },
                    "font-size" => paint with { FontSize = Length(v, paint.FontSize, paint.FontSize) },
                    "font-weight" => paint with { Bold = v is "bold" or "bolder" || (int.TryParse(v, NumberStyles.Integer, CultureInfo.InvariantCulture, out int weight) && weight >= 600) },
                    "font-style" => paint with { Italic = v is "italic" or "oblique" },
                    "text-anchor" => paint with { Anchor = v },
                    "display" when v == "none" => paint with { Hidden = true },
                    "visibility" => paint with { Hidden = v is "hidden" or "collapse" },
                    _ => paint,
                };
            }
            return paint with { Opacity = parent.Opacity * paint.Opacity };
        }

        private static float Fraction(string value, float fallback)
        {
            string v = value.Trim();
            bool percent = v.EndsWith('%');
            if (!float.TryParse(percent ? v[..^1] : v, NumberStyles.Float, CultureInfo.InvariantCulture, out float f) || !float.IsFinite(f)) return fallback;
            return Math.Clamp(percent ? f / 100f : f, 0f, 1f);
        }

        private float[]? DashArray(string value, float fontSize)
        {
            if (value.Trim() == "none") return null;
            var dashes = new List<float>();
            foreach (string part in value.Split([',', ' ', '\t', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries))
            {
                float d = Length(part, -1f, fontSize);
                if (d < 0) return null;
                dashes.Add(d);
                if (dashes.Count > 32) return null;
            }
            if (dashes.Count == 0 || dashes.All(static d => d == 0)) return null;
            // An odd list is repeated to make it even, as SVG does.
            if (dashes.Count % 2 == 1) dashes.AddRange(dashes);
            return [.. dashes];
        }

        /// <summary>A length in user units: plain numbers, px, pt, pc, mm, cm, in and em. Percentages are not resolved.</summary>
        private float Length(string? value, float fallback, float fontSize)
        {
            if (string.IsNullOrWhiteSpace(value)) return fallback;
            ReadOnlySpan<char> v = value.AsSpan().Trim();
            float scale = 1f;
            if (v.EndsWith("%"))
            {
                Warn("Percentage lengths are not resolved.");
                return fallback;
            }
            foreach ((string unit, float factor) in Units)
            {
                if (!v.EndsWith(unit, StringComparison.Ordinal)) continue;
                v = v[..^unit.Length];
                scale = unit == "em" ? fontSize : factor;
                break;
            }
            return float.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out float f) && float.IsFinite(f * scale) ? f * scale : fallback;
        }

        private static readonly (string Unit, float Factor)[] Units = [("px", 1f), ("pt", 96f / 72f), ("pc", 16f), ("mm", 96f / 25.4f), ("cm", 96f / 2.54f), ("in", 96f), ("em", 0f)];

        /// <summary>A list of numbers separated by white space and commas, as points and view boxes are written.</summary>
        private static float[] Numbers(string value)
        {
            var numbers = new List<float>();
            foreach (string part in value.Split([',', ' ', '\t', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries))
            {
                // "10-5" and "1.5.5" are two numbers each, as path data allows: a number ends at a sign that does not
                // follow an exponent, and at a point when it already has one or is in its exponent.
                int start = 0;
                bool point = part[0] == '.';
                bool exponent = false;
                for (int i = 1; i <= part.Length; i++)
                {
                    if (i < part.Length)
                    {
                        char c = part[i];
                        bool sign = c is '-' or '+' && part[i - 1] is not ('e' or 'E');
                        if (!sign && !(c == '.' && (point || exponent)))
                        {
                            point |= c == '.';
                            exponent |= c is 'e' or 'E';
                            continue;
                        }
                    }
                    if (float.TryParse(part.AsSpan(start, i - start), NumberStyles.Float, CultureInfo.InvariantCulture, out float f) && float.IsFinite(f)) numbers.Add(f);
                    start = i;
                    point = i < part.Length && part[i] == '.';
                    exponent = false;
                    if (numbers.Count > BinkPolygonShape.MaxPoints * 2) return [.. numbers];
                }
            }
            return [.. numbers];
        }

        /// <summary>An SVG transform list: matrix, translate, scale, rotate (about a point too), skewX and skewY.</summary>
        private Matrix3x2 Transform(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return Matrix3x2.Identity;
            Matrix3x2 result = Matrix3x2.Identity;
            ReadOnlySpan<char> rest = value.AsSpan();
            int guard = 0;
            while (!rest.IsEmpty && guard++ < 64)
            {
                int open = rest.IndexOf('(');
                int close = rest.IndexOf(')');
                if (open < 0 || close < open) break;
                string name = rest[..open].Trim().TrimStart(',').Trim().ToString();
                float[] a = Numbers(rest[(open + 1)..close].ToString());
                Matrix3x2 m = name switch
                {
                    "matrix" when a.Length == 6 => new Matrix3x2(a[0], a[1], a[2], a[3], a[4], a[5]),
                    "translate" when a.Length >= 1 => Matrix3x2.CreateTranslation(a[0], a.Length > 1 ? a[1] : 0f),
                    "scale" when a.Length >= 1 => Matrix3x2.CreateScale(a[0], a.Length > 1 ? a[1] : a[0]),
                    "rotate" when a.Length >= 3 => Matrix3x2.CreateRotation(a[0] * MathF.PI / 180f, new Vector2(a[1], a[2])),
                    "rotate" when a.Length >= 1 => Matrix3x2.CreateRotation(a[0] * MathF.PI / 180f),
                    "skewX" when a.Length >= 1 => Matrix3x2.CreateSkew(a[0] * MathF.PI / 180f, 0f),
                    "skewY" when a.Length >= 1 => Matrix3x2.CreateSkew(0f, a[0] * MathF.PI / 180f),
                    _ => Unknown(name),
                };
                // A transform list applies its last item first, so each item goes before what is already there.
                result = m * result;
                rest = rest[(close + 1)..];
            }
            return result;
        }

        private Matrix3x2 Unknown(string name)
        {
            Warn($"The transform \"{(name.Length > 20 ? name[..20] : name)}\" is not understood and is ignored.");
            return Matrix3x2.Identity;
        }

        /// <summary>
        /// The identity stays the identity, so untransformed shapes keep their fast paths and save without one. False
        /// for a transform that cannot be inverted: SVG does not render such an element, and scale(0) is a common way
        /// to hide one.
        /// </summary>
        private static bool TryClean(Matrix3x2 m, out Matrix3x2 cleaned)
        {
            cleaned = m.IsIdentity ? Matrix3x2.Identity : m;
            return float.IsFinite(m.M11) && float.IsFinite(m.M12) && float.IsFinite(m.M21) && float.IsFinite(m.M22)
                && float.IsFinite(m.M31) && float.IsFinite(m.M32) && float.IsFinite(m.GetDeterminant()) && Matrix3x2.Invert(m, out _);
        }

        private static Dictionary<string, string> Attributes(XmlReader reader)
        {
            var attributes = new Dictionary<string, string>(StringComparer.Ordinal);
            if (!reader.MoveToFirstAttribute()) return attributes;
            do
            {
                // Event handlers and anything else not listed below are read and then simply never used.
                string key = reader.Prefix == "xlink" ? "xlink:" + reader.LocalName : reader.LocalName;
                attributes[key] = reader.Value;
            }
            while (reader.MoveToNextAttribute());
            reader.MoveToElement();
            return attributes;
        }

        private void Warn(string message)
        {
            if (_warnings.Count < 32) _warnings.Add(message);
        }
    }
}
