using System.Numerics;
using System.Text;

namespace Bit.Bink;

/// <summary>
/// Draws into PDF content streams for <see cref="BinkPdfExporter"/>.
/// </summary>
/// <remarks>
/// <para>
/// Every painting operation is written self-contained, <c>q</c> clips <c>cm</c> paint <c>Q</c>, with the transform and
/// clips the context tracks itself rather than ones left in the stream by earlier calls. That is what lets the content
/// drawn so far be moved into a form at any moment, which is how pixel erasers work: PDF has no destination-out
/// blend, so when an eraser is drawn, everything drawn before it in the group becomes a form, and the erasers that
/// follow (until something else is drawn) are painted black on white into a soft mask the form is drawn through.
/// </para>
/// <para>All content is in document coordinates; the page maps them to points once, around the whole page.</para>
/// </remarks>
internal sealed class BinkPdfRenderContext : IBinkRenderContext, IBinkRenderContextCapabilities
{
    private sealed class Group(float opacity, BinkBlendMode blend)
    {
        public StringBuilder Content { get; set; } = new();

        /// <summary>The soft mask being built from consecutive erasers, and the form of what they erase.</summary>
        public StringBuilder? Mask { get; set; }

        public string? Erased { get; set; }

        public float Opacity { get; } = opacity;

        public BinkBlendMode Blend { get; } = blend;
    }

    private readonly BinkPdfResources _resources;
    private readonly BinkRect _region;
    private readonly Stack<Group> _groups = new();
    private readonly Stack<(Matrix3x2 Matrix, int Clips)> _saved = new();
    private readonly List<Vector2[]> _clips = new();
    private Matrix3x2 _matrix = Matrix3x2.Identity;

    public BinkPdfRenderContext(BinkPdfResources resources, BinkRect region)
    {
        _resources = resources;
        _region = region;
        _groups.Push(new Group(1f, BinkBlendMode.Normal));
    }

    /// <summary>Text elements left out because the standard fonts cannot show their characters.</summary>
    public int SkippedText { get; private set; }

    public bool SupportsText => true;

    public bool SupportsImages => true;

    private Group Current => _groups.Peek();

    public void Save() => _saved.Push((_matrix, _clips.Count));

    public void Restore()
    {
        if (_saved.Count == 0) return;
        (_matrix, int clips) = _saved.Pop();
        if (_clips.Count > clips) _clips.RemoveRange(clips, _clips.Count - clips);
    }

    public void Transform(Matrix3x2 matrix) => _matrix = matrix * _matrix;

    public void ClipRect(BinkRect rect)
    {
        rect = rect.Normalize();
        _clips.Add(
        [
            Vector2.Transform(rect.TopLeft, _matrix),
            Vector2.Transform(new Vector2(rect.Right, rect.Top), _matrix),
            Vector2.Transform(new Vector2(rect.Right, rect.Bottom), _matrix),
            Vector2.Transform(new Vector2(rect.Left, rect.Bottom), _matrix),
        ]);
    }

    public void FillPath(BinkPathGeometry path, in BinkFillPaint paint)
    {
        ArgumentNullException.ThrowIfNull(path);
        if (path.IsEmpty) return;
        string fill = paint.Rule == BinkFillRule.EvenOdd ? "f*" : "f";
        if (paint.Blend == BinkBlendMode.DestinationOut)
        {
            if (paint.EffectiveAlpha <= 0) return;
            StringBuilder mask = MaskOf(Current);
            Begin(mask);
            // Multiplied into what the mask already holds, so translucent erasers compound where they overlap, as
            // destination-out does on the canvas: two half erasers leave a quarter.
            SetState(mask, _resources.State(1f, 1f, BinkBlendMode.Multiply));
            Gray(mask, 1f - paint.EffectiveAlpha).Append(" g\n");
            WritePath(mask, path);
            mask.Append(fill).Append('\n');
            End(mask);
            return;
        }
        if (paint.EffectiveAlpha <= 0) return;
        StringBuilder content = Paint();
        Begin(content);
        SetState(content, _resources.State(paint.EffectiveAlpha, 1f, paint.Blend));
        Color(content, paint.Color).Append(" rg\n");
        WritePath(content, path);
        content.Append(fill).Append('\n');
        End(content);
    }

    public void StrokePath(BinkPathGeometry path, in BinkStrokePaint paint)
    {
        ArgumentNullException.ThrowIfNull(path);
        if (path.IsEmpty || !(paint.Width > 0) || paint.EffectiveAlpha <= 0) return;
        bool erases = paint.Blend == BinkBlendMode.DestinationOut;
        StringBuilder content = erases ? MaskOf(Current) : Paint();
        Begin(content);
        if (erases)
        {
            SetState(content, _resources.State(1f, 1f, BinkBlendMode.Multiply));
            Gray(content, 1f - paint.EffectiveAlpha).Append(" G\n");
        }
        else
        {
            SetState(content, _resources.State(1f, paint.EffectiveAlpha, paint.Blend));
            Color(content, paint.Color).Append(" RG\n");
        }
        // The cap and join enumerations have PDF's own numbering.
        BinkPdfFile.Number(content, paint.Width).Append(" w ").Append((int)paint.Cap).Append(" J ").Append((int)paint.Join).Append(" j 4 M\n");
        if (paint.Dash is { Length: > 0 } dash && Array.TrueForAll(dash, d => float.IsFinite(d) && d >= 0) && Array.Exists(dash, d => d > 0))
        {
            content.Append('[');
            // An odd pattern repeats to make on and off alternate, as SVG and the canvas do.
            int count = dash.Length % 2 == 1 ? dash.Length * 2 : dash.Length;
            for (int i = 0; i < count; i++) BinkPdfFile.Number(content.Append(i > 0 ? " " : ""), dash[i % dash.Length]);
            content.Append("] 0 d\n");
        }
        WritePath(content, path);
        content.Append("S\n");
        End(content);
    }

    public void DrawImage(BinkImageSource image, BinkRect destination, BinkRect? sourceRect, float opacity)
    {
        ArgumentNullException.ThrowIfNull(image);
        destination = destination.Normalize();
        if (destination.IsEmpty || !(opacity > 0)) return;
        string? name = _resources.Image(image, out int width, out int height, out int orientation);
        if (name is null)
        {
            _resources.SkippedImages++;
            return;
        }
        BinkRect source = sourceRect?.Normalize() ?? new BinkRect(0, 0, width, height);
        if (source.IsEmpty) return;

        StringBuilder content = Paint();
        Begin(content);
        SetState(content, _resources.State(opacity, 1f, BinkBlendMode.Normal));
        if (sourceRect is not null) Rect(content, destination).Append(" re W n\n");
        // The whole picture, as displayed, scaled so its crop fills the destination; the clip hides the rest.
        float sx = destination.Width / source.Width, sy = destination.Height / source.Height;
        float x = destination.X - source.X * sx, y = destination.Y - source.Y * sy;
        float w = width * sx, h = height * sy;
        // An image fills the unit square (u, v) with its first stored row at v = 1. Where that lands in the displayed
        // picture (p, q), q down, depends on the EXIF orientation; the picture then fills (x, y, w, h).
        (float pu, float pv, float p0, float qu, float qv, float q0) = orientation switch
        {
            2 => (-1f, 0f, 1f, 0f, -1f, 1f),
            3 => (-1f, 0f, 1f, 0f, 1f, 0f),
            4 => (1f, 0f, 0f, 0f, 1f, 0f),
            5 => (0f, -1f, 1f, 1f, 0f, 0f),
            6 => (0f, 1f, 0f, 1f, 0f, 0f),
            7 => (0f, 1f, 0f, -1f, 0f, 1f),
            8 => (0f, -1f, 1f, -1f, 0f, 1f),
            _ => (1f, 0f, 0f, 0f, -1f, 1f),
        };
        BinkPdfFile.Number(content, w * pu).Append(' ');
        BinkPdfFile.Number(content, h * qu).Append(' ');
        BinkPdfFile.Number(content, w * pv).Append(' ');
        BinkPdfFile.Number(content, h * qv).Append(' ');
        BinkPdfFile.Number(content, x + w * p0).Append(' ');
        BinkPdfFile.Number(content, y + h * q0).Append(" cm /").Append(name).Append(" Do\n");
        End(content);
    }

    public void DrawText(string text, Vector2 origin, in BinkTextStyle style, float opacity)
    {
        if (string.IsNullOrEmpty(text) || !(opacity > 0) || !(style.FontSize > 0)) return;
        // Lines break at line feeds only, as in the SVG export and on the canvas; a carriage return draws nothing.
        string[] lines = text.Replace("\r", "", StringComparison.Ordinal).Split('\n');
        var encoded = new byte[lines.Length][];
        for (int i = 0; i < lines.Length; i++)
        {
            if (!BinkPdfFont.TryEncode(lines[i], out encoded[i]))
            {
                SkippedText++;
                return;
            }
        }
        BinkPdfFace face = BinkPdfFont.FaceOf(style.FontFamily);
        string font = _resources.Font(face, style.Bold, style.Italic);

        StringBuilder content = Paint();
        Begin(content);
        SetState(content, _resources.State(style.Color.Alpha * opacity, 1f, BinkBlendMode.Normal));
        Color(content, style.Color).Append(" rg\nBT /").Append(font).Append(' ');
        BinkPdfFile.Number(content, style.FontSize).Append(" Tf\n");
        float lineHeight = style.FontSize * style.LineHeight;
        for (int i = 0; i < lines.Length; i++)
        {
            if (encoded[i].Length == 0) continue;
            float width = BinkPdfFont.Measure(encoded[i], face, style.Bold) * style.FontSize;
            float x = style.Align switch
            {
                BinkTextAlign.Center => origin.X - width / 2f,
                BinkTextAlign.Right => origin.X - width,
                _ => origin.X,
            };
            // The origin is the top of the first line, as in the SVG export; the text matrix flips the glyphs upright
            // in these y-down coordinates.
            float baseline = origin.Y + style.FontSize * 0.82f + i * lineHeight;
            content.Append("1 0 0 -1 ");
            BinkPdfFile.Number(content, x).Append(' ');
            BinkPdfFile.Number(content, baseline).Append(" Tm ");
            BinkPdfFont.WriteString(content, encoded[i]).Append(" Tj\n");
        }
        content.Append("ET\n");
        End(content);
    }

    public void PushGroup(float opacity, BinkBlendMode blend) => _groups.Push(new Group(Math.Clamp(float.IsFinite(opacity) ? opacity : 1f, 0f, 1f), blend));

    public void PopGroup()
    {
        if (_groups.Count <= 1) return;
        Group group = _groups.Pop();
        CloseMask(group);
        if (group.Content.Length == 0) return;
        (string form, _) = _resources.Form(Ascii(group.Content), _region, "<< /S /Transparency /I true /K false /CS /DeviceRGB >>");
        StringBuilder parent = Paint();
        parent.Append("q\n");
        SetState(parent, _resources.State(group.Opacity, group.Opacity, group.Blend));
        parent.Append('/').Append(form).Append(" Do\nQ\n");
    }

    /// <summary>Closes what is still open and returns the page's content, mapped from document units to points.</summary>
    public byte[] Finish(float scale)
    {
        while (_groups.Count > 1) PopGroup();
        Group page = _groups.Peek();
        CloseMask(page);
        var content = new StringBuilder(page.Content.Length + 64);
        content.Append("q\n");
        content.Append(BinkPdfFile.Precise(scale)).Append(" 0 0 ").Append(BinkPdfFile.Precise(-scale)).Append(' ')
            .Append(BinkPdfFile.Precise(-_region.X * scale)).Append(' ').Append(BinkPdfFile.Precise(_region.Bottom * scale)).Append(" cm\n");
        content.Append(page.Content);
        content.Append("Q\n");
        return Ascii(content);
    }

    /// <summary>The content of the current group, ready for something to be painted on it.</summary>
    private StringBuilder Paint()
    {
        Group group = Current;
        CloseMask(group);
        return group.Content;
    }

    /// <summary>
    /// The soft mask the next eraser is painted into. The first eraser after other content moves that content into a
    /// form and starts a mask that keeps all of it (white); erasers paint it darker.
    /// </summary>
    private StringBuilder MaskOf(Group group)
    {
        if (group.Mask is not null) return group.Mask;
        (group.Erased, _) = group.Content.Length > 0 ? _resources.Form(Ascii(group.Content), _region, "<< /S /Transparency >>") : (null, 0);
        group.Content = new StringBuilder();
        group.Mask = new StringBuilder();
        group.Mask.Append("1 g\n");
        Rect(group.Mask, _region.Inflate(1f)).Append(" re f\n");
        return group.Mask;
    }

    /// <summary>Draws the erased content through the mask its erasers made, and returns the group to plain drawing.</summary>
    private void CloseMask(Group group)
    {
        if (group.Mask is null) return;
        if (group.Erased is { } erased)
        {
            (_, int maskForm) = _resources.Form(Ascii(group.Mask), _region.Inflate(1f), "<< /S /Transparency /CS /DeviceGray >>");
            group.Content.Append("q /").Append(_resources.MaskState(maskForm)).Append(" gs /").Append(erased).Append(" Do Q\n");
        }
        group.Mask = null;
        group.Erased = null;
    }

    /// <summary>Opens a self-contained painting operation: the clips, then the transform.</summary>
    private void Begin(StringBuilder content)
    {
        content.Append("q\n");
        foreach (Vector2[] clip in _clips)
        {
            for (int i = 0; i < clip.Length; i++)
            {
                BinkPdfFile.Number(content, clip[i].X).Append(' ');
                BinkPdfFile.Number(content, clip[i].Y).Append(i == 0 ? " m " : " l ");
            }
            content.Append("h W n\n");
        }
        if (!_matrix.IsIdentity)
        {
            BinkPdfFile.Number(content, _matrix.M11).Append(' ');
            BinkPdfFile.Number(content, _matrix.M12).Append(' ');
            BinkPdfFile.Number(content, _matrix.M21).Append(' ');
            BinkPdfFile.Number(content, _matrix.M22).Append(' ');
            BinkPdfFile.Number(content, _matrix.M31).Append(' ');
            BinkPdfFile.Number(content, _matrix.M32).Append(" cm\n");
        }
    }

    private static void End(StringBuilder content) => content.Append("Q\n");

    private static void SetState(StringBuilder content, string? state)
    {
        if (state is not null) content.Append('/').Append(state).Append(" gs\n");
    }

    private static StringBuilder Color(StringBuilder content, BinkColor color)
    {
        BinkPdfFile.Number(content, color.R / 255f).Append(' ');
        BinkPdfFile.Number(content, color.G / 255f).Append(' ');
        return BinkPdfFile.Number(content, color.B / 255f);
    }

    private static StringBuilder Gray(StringBuilder content, float level) => BinkPdfFile.Number(content, Math.Clamp(level, 0f, 1f));

    private static StringBuilder Rect(StringBuilder content, BinkRect rect)
    {
        BinkPdfFile.Number(content, rect.X).Append(' ');
        BinkPdfFile.Number(content, rect.Y).Append(' ');
        BinkPdfFile.Number(content, rect.Width).Append(' ');
        return BinkPdfFile.Number(content, rect.Height);
    }

    private static void WritePath(StringBuilder content, BinkPathGeometry path)
    {
        ReadOnlySpan<BinkPathVerb> verbs = path.Verbs;
        ReadOnlySpan<float> c = path.Coordinates;
        int k = 0;
        Vector2 current = Vector2.Zero, start = Vector2.Zero;
        foreach (BinkPathVerb verb in verbs)
        {
            switch (verb)
            {
                case BinkPathVerb.MoveTo:
                    current = start = new Vector2(c[k], c[k + 1]);
                    Point(content, current).Append(" m\n");
                    k += 2;
                    break;
                case BinkPathVerb.LineTo:
                    current = new Vector2(c[k], c[k + 1]);
                    Point(content, current).Append(" l\n");
                    k += 2;
                    break;
                case BinkPathVerb.QuadTo:
                {
                    // PDF has no quadratic curve; the cubic with control points two thirds of the way to the
                    // quadratic's control point is the same curve.
                    var q = new Vector2(c[k], c[k + 1]);
                    var end = new Vector2(c[k + 2], c[k + 3]);
                    Point(content, current + (q - current) * (2f / 3f)).Append(' ');
                    Point(content, end + (q - end) * (2f / 3f)).Append(' ');
                    Point(content, end).Append(" c\n");
                    current = end;
                    k += 4;
                    break;
                }
                case BinkPathVerb.CubicTo:
                    Point(content, new Vector2(c[k], c[k + 1])).Append(' ');
                    Point(content, new Vector2(c[k + 2], c[k + 3])).Append(' ');
                    current = new Vector2(c[k + 4], c[k + 5]);
                    Point(content, current).Append(" c\n");
                    k += 6;
                    break;
                case BinkPathVerb.Close:
                    content.Append("h\n");
                    current = start;
                    break;
            }
        }
    }

    private static StringBuilder Point(StringBuilder content, Vector2 p) => BinkPdfFile.Number(BinkPdfFile.Number(content, p.X).Append(' '), p.Y);

    private static byte[] Ascii(StringBuilder content)
    {
        var bytes = new byte[content.Length];
        int i = 0;
        foreach (ReadOnlyMemory<char> chunk in content.GetChunks())
        {
            foreach (char ch in chunk.Span) bytes[i++] = (byte)ch;
        }
        return bytes;
    }
}

/// <summary>The standard fonts' encoding and widths, for setting text in a PDF without embedding a font.</summary>
internal static class BinkPdfFont
{
    // Advance widths in thousandths of the font size, from the Adobe font metrics, for ' ' to '~'.
    private static readonly short[] Helvetica =
    [
        278, 278, 355, 556, 556, 889, 667, 191, 333, 333, 389, 584, 278, 333, 278, 278,
        556, 556, 556, 556, 556, 556, 556, 556, 556, 556, 278, 278, 584, 584, 584, 556,
        1015, 667, 667, 722, 722, 667, 611, 778, 722, 278, 500, 667, 556, 833, 722, 778,
        667, 778, 722, 667, 611, 722, 667, 944, 667, 667, 611, 278, 278, 278, 469, 556,
        333, 556, 556, 500, 556, 556, 278, 556, 556, 222, 222, 500, 222, 833, 556, 556,
        556, 556, 333, 500, 278, 556, 500, 722, 500, 500, 500, 334, 260, 334, 584,
    ];

    private static readonly short[] HelveticaBold =
    [
        278, 333, 474, 556, 556, 889, 722, 238, 333, 333, 389, 584, 278, 333, 278, 278,
        556, 556, 556, 556, 556, 556, 556, 556, 556, 556, 333, 333, 584, 584, 584, 611,
        975, 722, 722, 722, 722, 667, 611, 778, 722, 278, 556, 722, 611, 833, 722, 778,
        667, 778, 722, 667, 611, 722, 667, 944, 667, 667, 611, 333, 278, 333, 584, 556,
        333, 556, 611, 556, 611, 556, 333, 611, 611, 278, 278, 556, 278, 889, 611, 611,
        611, 611, 389, 556, 333, 611, 556, 778, 556, 556, 500, 389, 280, 389, 584,
    ];

    private static readonly short[] Times =
    [
        250, 333, 408, 500, 500, 833, 778, 180, 333, 333, 500, 564, 250, 333, 250, 278,
        500, 500, 500, 500, 500, 500, 500, 500, 500, 500, 278, 278, 564, 564, 564, 444,
        921, 722, 667, 667, 722, 611, 556, 722, 722, 333, 389, 722, 611, 889, 722, 722,
        556, 722, 667, 556, 611, 722, 722, 944, 722, 722, 611, 333, 278, 333, 469, 500,
        333, 444, 500, 444, 500, 444, 333, 500, 500, 278, 278, 500, 278, 778, 500, 500,
        500, 500, 333, 389, 278, 500, 500, 722, 500, 500, 444, 480, 200, 480, 541,
    ];

    // The same metrics for WinAnsiEncoding's bytes 0x80 to 0xFF; zero where the encoding has no character.
    private static readonly short[] HelveticaHigh =
    [
        556, 0, 222, 556, 333, 1000, 556, 556, 333, 1000, 667, 333, 1000, 0, 611, 0,
        0, 222, 222, 333, 333, 350, 556, 1000, 333, 1000, 500, 333, 944, 0, 500, 667,
        278, 333, 556, 556, 556, 556, 260, 556, 333, 737, 370, 556, 584, 333, 737, 333,
        400, 584, 333, 333, 333, 556, 537, 278, 333, 333, 365, 556, 834, 834, 834, 611,
        667, 667, 667, 667, 667, 667, 1000, 722, 667, 667, 667, 667, 278, 278, 278, 278,
        722, 722, 778, 778, 778, 778, 778, 584, 778, 722, 722, 722, 722, 667, 667, 611,
        556, 556, 556, 556, 556, 556, 889, 500, 556, 556, 556, 556, 278, 278, 278, 278,
        556, 556, 556, 556, 556, 556, 556, 584, 611, 556, 556, 556, 556, 500, 556, 500,
    ];

    private static readonly short[] HelveticaBoldHigh =
    [
        556, 0, 278, 556, 500, 1000, 556, 556, 333, 1000, 667, 333, 1000, 0, 611, 0,
        0, 278, 278, 500, 500, 350, 556, 1000, 333, 1000, 556, 333, 944, 0, 500, 667,
        278, 333, 556, 556, 556, 556, 280, 556, 333, 737, 370, 556, 584, 333, 737, 333,
        400, 584, 333, 333, 333, 611, 556, 278, 333, 333, 365, 556, 834, 834, 834, 611,
        722, 722, 722, 722, 722, 722, 1000, 722, 667, 667, 667, 667, 278, 278, 278, 278,
        722, 722, 778, 778, 778, 778, 778, 584, 778, 722, 722, 722, 722, 667, 667, 611,
        556, 556, 556, 556, 556, 556, 889, 556, 556, 556, 556, 556, 278, 278, 278, 278,
        611, 611, 611, 611, 611, 611, 611, 584, 611, 611, 611, 611, 611, 556, 611, 556,
    ];

    private static readonly short[] TimesHigh =
    [
        500, 0, 333, 500, 444, 1000, 500, 500, 333, 1000, 556, 333, 889, 0, 611, 0,
        0, 333, 333, 444, 444, 350, 500, 1000, 333, 980, 389, 333, 722, 0, 444, 722,
        250, 333, 500, 500, 500, 500, 200, 500, 333, 760, 276, 500, 564, 333, 760, 333,
        400, 564, 300, 300, 333, 500, 453, 250, 333, 300, 310, 500, 750, 750, 750, 444,
        722, 722, 722, 722, 722, 722, 889, 667, 611, 611, 611, 611, 333, 333, 333, 333,
        722, 722, 722, 722, 722, 722, 722, 564, 722, 722, 722, 722, 722, 722, 556, 500,
        444, 444, 444, 444, 444, 444, 667, 444, 444, 444, 444, 444, 278, 278, 278, 278,
        500, 500, 500, 500, 500, 500, 500, 564, 500, 500, 500, 500, 500, 500, 500, 500,
    ];

    // WinAnsiEncoding's bytes 0x80..0x9F, which are not Latin-1: the character each stands for, 0 where none.
    private static readonly char[] WinAnsiHigh =
    [
        '€', '\0', '‚', 'ƒ', '„', '…', '†', '‡', 'ˆ', '‰', 'Š', '‹', 'Œ', '\0', 'Ž', '\0',
        '\0', '‘', '’', '“', '”', '•', '–', '—', '˜', '™', 'š', '›', 'œ', '\0', 'ž', 'Ÿ',
    ];

    /// <summary>The standard face nearest a CSS font family list.</summary>
    public static BinkPdfFace FaceOf(string? family)
    {
        string f = (family ?? string.Empty).ToLowerInvariant();
        if (f.Contains("mono", StringComparison.Ordinal) || f.Contains("courier", StringComparison.Ordinal) || f.Contains("consol", StringComparison.Ordinal)) return BinkPdfFace.Courier;
        if (f.Contains("times", StringComparison.Ordinal) || f.Contains("georgia", StringComparison.Ordinal)
            || (f.Contains("serif", StringComparison.Ordinal) && !f.Contains("sans-serif", StringComparison.Ordinal))) return BinkPdfFace.Times;
        return BinkPdfFace.Helvetica;
    }

    /// <summary>Encodes a line in WinAnsiEncoding; false when it has a character the encoding lacks.</summary>
    public static bool TryEncode(string line, out byte[] bytes)
    {
        bytes = new byte[line.Length];
        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (c == '\t') c = ' ';
            if (c is (>= ' ' and <= '~') or (>= ' ' and <= 'ÿ'))
            {
                bytes[i] = (byte)c;
                continue;
            }
            int index = c == '\0' ? -1 : Array.IndexOf(WinAnsiHigh, c);
            if (index < 0) return false;
            bytes[i] = (byte)(0x80 + index);
        }
        return true;
    }

    /// <summary>The width of encoded text in units of the font size.</summary>
    public static float Measure(ReadOnlySpan<byte> text, BinkPdfFace face, bool bold)
    {
        if (face == BinkPdfFace.Courier) return text.Length * 0.6f;
        short[] widths = face == BinkPdfFace.Times ? Times : bold ? HelveticaBold : Helvetica;
        short[] high = face == BinkPdfFace.Times ? TimesHigh : bold ? HelveticaBoldHigh : HelveticaHigh;
        int total = 0;
        foreach (byte b in text)
        {
            int width = b switch
            {
                >= 32 and <= 126 => widths[b - 32],
                >= 0x80 => high[b - 0x80],
                _ => 0,
            };
            // A byte the encoding has no character for is measured as a lower-case letter.
            total += width > 0 ? width : widths['a' - 32];
        }
        // Times bold is a little wider than Times; its own metrics are not carried, so it is estimated.
        return total / 1000f * (face == BinkPdfFace.Times && bold ? 1.04f : 1f);
    }

    /// <summary>Writes encoded text as a PDF string, escaping what a literal string cannot hold.</summary>
    public static StringBuilder WriteString(StringBuilder content, ReadOnlySpan<byte> text)
    {
        content.Append('(');
        foreach (byte b in text)
        {
            if (b is (byte)'(' or (byte)')' or (byte)'\\') content.Append('\\').Append((char)b);
            else if (b is < 32 or > 126) content.Append('\\').Append(Convert.ToString(b, 8).PadLeft(3, '0'));
            else content.Append((char)b);
        }
        return content.Append(')');
    }
}
