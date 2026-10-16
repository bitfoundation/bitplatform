using System.Globalization;
using System.Numerics;

namespace Bit.Bink;

/// <summary>
/// An <see cref="IBinkRenderContext"/> that writes SVG markup. Paths stay paths, so exported drawings remain
/// editable vector artwork rather than an embedded bitmap.
/// </summary>
/// <remarks>
/// All text that reaches the output (font families, text content, descriptions) is XML-escaped, and the context
/// never emits script, event handlers, external references or <c>foreignObject</c>, so the result is safe to serve.
/// </remarks>
public sealed class BinkSvgRenderContext : IBinkRenderContext, IBinkRenderContextCapabilities
{
    private readonly TextWriter _writer;
    private readonly int _decimals;

    // Save and PushGroup both open markup, but Restore and PopGroup must unwind their own kind. Tagging each
    // frame keeps them from consuming each other's, which would leave the document unbalanced.
    private readonly Stack<Frame> _frames = new();
    private int _clipCounter;
    private int _skippedErasers;

    private sealed class Frame(bool isGroup)
    {
        public bool IsGroup { get; } = isGroup;

        public string CloseTags { get; set; } = string.Empty;
    }

    /// <summary>Creates a context writing into <paramref name="writer"/>.</summary>
    /// <param name="writer">Destination for the SVG fragment.</param>
    /// <param name="decimals">Decimals kept for coordinates.</param>
    public BinkSvgRenderContext(TextWriter writer, int decimals = 2)
    {
        ArgumentNullException.ThrowIfNull(writer);
        _writer = writer;
        _decimals = Math.Clamp(decimals, 0, 6);
    }

    /// <inheritdoc />
    public bool SupportsText => true;

    /// <inheritdoc />
    public bool SupportsImages => true;

    /// <summary>
    /// Prefix for generated ids, so fragments from several documents can be combined without collisions.
    /// Restricted to characters that are safe inside an attribute value.
    /// </summary>
    public string IdPrefix
    {
        get => _idPrefix;
        init => _idPrefix = IsSafeIdPrefix(value) ? value : "bi";
    }

    private readonly string _idPrefix = "bi";

    private static bool IsSafeIdPrefix(string? value)
    {
        if (string.IsNullOrEmpty(value) || value.Length > 96 || !char.IsAsciiLetter(value[0])) return false;
        foreach (char c in value)
        {
            if (!char.IsAsciiLetterOrDigit(c) && c != '-' && c != '_') return false;
        }
        return true;
    }

    /// <summary>
    /// Number of erase operations that were skipped. Erasing cannot be expressed by a paint attribute in SVG; the
    /// document exporter turns eraser strokes into masks, and anything that reaches this context directly is
    /// counted here rather than being painted as solid colour.
    /// </summary>
    public int SkippedErasers => _skippedErasers;

    /// <inheritdoc />
    public void Save() => _frames.Push(new Frame(isGroup: false));

    /// <inheritdoc />
    public void Restore()
    {
        // Only unwind up to the nearest group boundary: a Restore must never close a group someone else opened.
        if (_frames.Count == 0 || _frames.Peek().IsGroup) return;
        _writer.Write(_frames.Pop().CloseTags);
    }

    /// <inheritdoc />
    public void Transform(Matrix3x2 matrix)
    {
        if (matrix.IsIdentity) return;
        _writer.Write("<g transform=\"matrix(");
        WriteNumber(matrix.M11, 6);
        _writer.Write(' ');
        WriteNumber(matrix.M12, 6);
        _writer.Write(' ');
        WriteNumber(matrix.M21, 6);
        _writer.Write(' ');
        WriteNumber(matrix.M22, 6);
        _writer.Write(' ');
        WriteNumber(matrix.M31, _decimals);
        _writer.Write(' ');
        WriteNumber(matrix.M32, _decimals);
        _writer.Write(")\">");
        PushClose("</g>");
    }

    /// <inheritdoc />
    public void ClipRect(BinkRect rect)
    {
        string id = IdPrefix + "-clip-" + _clipCounter++.ToString(CultureInfo.InvariantCulture);
        _writer.Write("<clipPath id=\"");
        _writer.Write(id);
        _writer.Write("\"><rect x=\"");
        WriteNumber(rect.X, _decimals);
        _writer.Write("\" y=\"");
        WriteNumber(rect.Y, _decimals);
        _writer.Write("\" width=\"");
        WriteNumber(MathF.Max(rect.Width, 0), _decimals);
        _writer.Write("\" height=\"");
        WriteNumber(MathF.Max(rect.Height, 0), _decimals);
        _writer.Write("\"/></clipPath><g clip-path=\"url(#");
        _writer.Write(id);
        _writer.Write(")\">");
        PushClose("</g>");
    }

    /// <inheritdoc />
    public void FillPath(BinkPathGeometry path, in BinkFillPaint paint)
    {
        ArgumentNullException.ThrowIfNull(path);
        if (path.IsEmpty || paint.EffectiveAlpha <= 0) return;
        if (paint.Blend == BinkBlendMode.DestinationOut)
        {
            // Painting this would draw solid colour where the user expects an erasure, which is worse than
            // leaving it out. SvgExporter handles erasers properly, as masks.
            _skippedErasers++;
            return;
        }
        _writer.Write("<path d=\"");
        path.WriteSvgPathData(_writer, _decimals);
        _writer.Write("\" fill=\"");
        _writer.Write(RgbOnly(paint.Color));
        _writer.Write('"');
        WriteOpacity("fill-opacity", paint.EffectiveAlpha);
        if (paint.Rule == BinkFillRule.EvenOdd) _writer.Write(" fill-rule=\"evenodd\"");
        WriteBlend(paint.Blend);
        _writer.Write("/>");
    }

    /// <inheritdoc />
    public void StrokePath(BinkPathGeometry path, in BinkStrokePaint paint)
    {
        ArgumentNullException.ThrowIfNull(path);
        if (path.IsEmpty || paint.EffectiveAlpha <= 0 || paint.Width <= 0) return;
        if (paint.Blend == BinkBlendMode.DestinationOut)
        {
            _skippedErasers++;
            return;
        }
        _writer.Write("<path d=\"");
        path.WriteSvgPathData(_writer, _decimals);
        _writer.Write("\" fill=\"none\" stroke=\"");
        _writer.Write(RgbOnly(paint.Color));
        _writer.Write("\" stroke-width=\"");
        WriteNumber(paint.Width, _decimals);
        _writer.Write('"');
        WriteOpacity("stroke-opacity", paint.EffectiveAlpha);
        if (paint.Cap != BinkLineCap.Butt) _writer.Write(paint.Cap == BinkLineCap.Round ? " stroke-linecap=\"round\"" : " stroke-linecap=\"square\"");
        if (paint.Join != BinkLineJoin.Miter) _writer.Write(paint.Join == BinkLineJoin.Round ? " stroke-linejoin=\"round\"" : " stroke-linejoin=\"bevel\"");
        if (paint.Dash is { Length: > 0 } dash)
        {
            _writer.Write(" stroke-dasharray=\"");
            for (int i = 0; i < dash.Length; i++)
            {
                if (i > 0) _writer.Write(' ');
                WriteNumber(MathF.Max(dash[i], 0), _decimals);
            }
            _writer.Write('"');
        }
        WriteBlend(paint.Blend);
        _writer.Write("/>");
    }

    /// <inheritdoc />
    public void DrawImage(BinkImageSource image, BinkRect destination, BinkRect? sourceRect, float opacity)
    {
        ArgumentNullException.ThrowIfNull(image);
        if (destination.IsEmpty || opacity <= 0) return;

        // A crop is expressed as an oversized image behind a clip rectangle, which keeps the pixels untouched.
        if (sourceRect is { } crop && crop.Width > 0 && crop.Height > 0 && image.Width > 0 && image.Height > 0)
        {
            float scaleX = destination.Width / crop.Width;
            float scaleY = destination.Height / crop.Height;
            var full = new BinkRect(destination.X - crop.X * scaleX, destination.Y - crop.Y * scaleY, image.Width * scaleX, image.Height * scaleY);

            // The clip needs a scope of its own. Without the Save, the matching Restore would close whatever
            // scope the caller had open, and every later element would fall outside its layer group.
            Save();
            ClipRect(destination);
            WriteImageTag(image, full, opacity);
            Restore();
            return;
        }
        WriteImageTag(image, destination, opacity);
    }

    private void WriteImageTag(BinkImageSource image, BinkRect rect, float opacity)
    {
        _writer.Write("<image x=\"");
        WriteNumber(rect.X, _decimals);
        _writer.Write("\" y=\"");
        WriteNumber(rect.Y, _decimals);
        _writer.Write("\" width=\"");
        WriteNumber(MathF.Max(rect.Width, 0), _decimals);
        _writer.Write("\" height=\"");
        WriteNumber(MathF.Max(rect.Height, 0), _decimals);
        _writer.Write("\" preserveAspectRatio=\"none\"");
        WriteOpacity("opacity", opacity);
        _writer.Write(" href=\"");
        BinkSvgText.WriteEscaped(_writer, image.Uri);
        _writer.Write("\"/>");
    }

    /// <inheritdoc />
    public void DrawText(string text, Vector2 origin, in BinkTextStyle style, float opacity)
    {
        if (string.IsNullOrEmpty(text) || opacity <= 0) return;
        float lineHeight = style.FontSize * style.LineHeight;
        _writer.Write("<text x=\"");
        WriteNumber(origin.X, _decimals);
        _writer.Write("\" y=\"");
        // SVG positions text on its baseline; the element's origin is the top of the first line.
        WriteNumber(origin.Y + style.FontSize * 0.82f, _decimals);
        _writer.Write("\" font-family=\"");
        BinkSvgText.WriteEscaped(_writer, style.FontFamily);
        _writer.Write("\" font-size=\"");
        WriteNumber(style.FontSize, _decimals);
        _writer.Write("\" fill=\"");
        _writer.Write(RgbOnly(style.Color));
        _writer.Write('"');
        WriteOpacity("fill-opacity", style.Color.Alpha * opacity);
        if (style.Bold) _writer.Write(" font-weight=\"bold\"");
        if (style.Italic) _writer.Write(" font-style=\"italic\"");
        if (style.Align != BinkTextAlign.Left) _writer.Write(style.Align == BinkTextAlign.Center ? " text-anchor=\"middle\"" : " text-anchor=\"end\"");
        _writer.Write(" xml:space=\"preserve\">");

        int line = 0;
        foreach (ReadOnlySpan<char> segment in text.AsSpan().EnumerateLines())
        {
            _writer.Write("<tspan x=\"");
            WriteNumber(origin.X, _decimals);
            _writer.Write("\" dy=\"");
            WriteNumber(line == 0 ? 0 : lineHeight, _decimals);
            _writer.Write("\">");
            BinkSvgText.WriteEscaped(_writer, segment);
            _writer.Write("</tspan>");
            line++;
        }
        _writer.Write("</text>");
    }

    /// <inheritdoc />
    public void PushGroup(float opacity, BinkBlendMode blend)
    {
        _writer.Write("<g");
        if (opacity < 1f) WriteOpacity("opacity", opacity);
        if (blend != BinkBlendMode.Normal)
        {
            _writer.Write(" style=\"mix-blend-mode:");
            _writer.Write(BlendName(blend));
            _writer.Write(";isolation:isolate\"");
        }
        else
        {
            _writer.Write(" style=\"isolation:isolate\"");
        }
        _writer.Write('>');
        _frames.Push(new Frame(isGroup: true) { CloseTags = "</g>" });
    }

    /// <inheritdoc />
    public void PopGroup()
    {
        // Close any scopes an element left open before closing the group itself, so the markup stays balanced
        // even when a caller's Save and Restore are not perfectly paired.
        while (_frames.Count > 0 && !_frames.Peek().IsGroup) _writer.Write(_frames.Pop().CloseTags);
        if (_frames.Count == 0) return;
        _writer.Write(_frames.Pop().CloseTags);
    }

    /// <summary>Closes every scope still open, so a partially written fragment is still well formed.</summary>
    public void CloseAll()
    {
        while (_frames.Count > 0) _writer.Write(_frames.Pop().CloseTags);
    }

    /// <summary>Writes a raw group open tag with the given attributes. Used by the document exporter for layers and masks.</summary>
    internal void WriteRawOpen(string tag) => _writer.Write(tag);

    /// <summary>Writes raw markup. Callers are responsible for escaping.</summary>
    internal void WriteRaw(string markup) => _writer.Write(markup);

    /// <summary>
    /// Records markup that must be closed. It attaches to the innermost scope, so the matching Restore or
    /// PopGroup closes everything opened after it, in the right order.
    /// </summary>
    private void PushClose(string tag)
    {
        if (_frames.Count > 0 && !_frames.Peek().IsGroup)
        {
            Frame frame = _frames.Peek();
            frame.CloseTags = tag + frame.CloseTags;
            return;
        }
        // No open Save scope: the tag becomes a scope of its own so it is still closed eventually.
        _frames.Push(new Frame(isGroup: false) { CloseTags = tag });
    }

    private void WriteOpacity(string attribute, float value)
    {
        if (value >= 1f) return;
        _writer.Write(' ');
        _writer.Write(attribute);
        _writer.Write("=\"");
        WriteNumber(Math.Clamp(value, 0f, 1f), 3);
        _writer.Write('"');
    }

    private void WriteBlend(BinkBlendMode blend)
    {
        if (blend == BinkBlendMode.Normal) return;
        _writer.Write(" style=\"mix-blend-mode:");
        _writer.Write(BlendName(blend));
        _writer.Write('"');
    }

    private static string BlendName(BinkBlendMode blend) => blend switch
    {
        BinkBlendMode.Multiply => "multiply",
        BinkBlendMode.Screen => "screen",
        _ => "normal",
    };

    /// <summary>Returns <c>#rrggbb</c>; alpha is emitted separately as an opacity attribute for maximum viewer compatibility.</summary>
    private static string RgbOnly(BinkColor color) => (color with { A = 255 }).ToCss();

    private void WriteNumber(float value, int decimals) => BinkSvgNumber.Write(_writer, value, decimals);
}

/// <summary>XML escaping helpers shared by the SVG writers.</summary>
internal static class BinkSvgText
{
    /// <summary>Writes text with XML special characters escaped.</summary>
    public static void WriteEscaped(TextWriter writer, ReadOnlySpan<char> text)
    {
        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            switch (c)
            {
                case '<': writer.Write("&lt;"); break;
                case '>': writer.Write("&gt;"); break;
                case '&': writer.Write("&amp;"); break;
                case '"': writer.Write("&quot;"); break;
                case '\'': writer.Write("&#39;"); break;
                default:
                    // Control characters are not legal in XML 1.0 and would make the output unparseable.
                    if (c < 0x20 && c is not ('\t' or '\n' or '\r')) writer.Write(' ');
                    // Neither are the noncharacters U+FFFE and U+FFFF, nor a surrogate that is not part of a pair.
                    else if (c is '￾' or '￿') writer.Write(' ');
                    else if (char.IsHighSurrogate(c) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1])) { writer.Write(c); writer.Write(text[++i]); }
                    else if (char.IsSurrogate(c)) writer.Write('�');
                    else writer.Write(c);
                    break;
            }
        }
    }

    /// <summary>Returns the text with XML special characters escaped.</summary>
    public static string Escape(string text)
    {
        using var writer = new StringWriter(CultureInfo.InvariantCulture);
        WriteEscaped(writer, text);
        return writer.ToString();
    }
}
