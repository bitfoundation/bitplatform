using System.Globalization;
using System.Text;

namespace Bit.Bink;

/// <summary>What area of the document an export covers.</summary>
public enum BinkExportRegion : byte
{
    /// <summary>The page rectangle from the document settings, or the content bounds when the page is unbounded.</summary>
    Page = 0,
    /// <summary>The bounding box of the visible content, with padding.</summary>
    Content = 1,
    /// <summary>An explicit rectangle supplied by the caller.</summary>
    Custom = 2,
    /// <summary>The bounding box of the current selection.</summary>
    Selection = 3,
}

/// <summary>Options shared by the SVG and PNG exporters.</summary>
public record BinkExportOptions
{
    /// <summary>Which part of the document to export.</summary>
    public BinkExportRegion Region { get; init; } = BinkExportRegion.Page;

    /// <summary>The rectangle used when <see cref="Region"/> is <see cref="BinkExportRegion.Custom"/>.</summary>
    public BinkRect CustomRegion { get; init; }

    /// <summary>
    /// When set, only these elements are drawn (eraser strokes are always kept so erasing still applies). With
    /// <see cref="BinkExportRegion.Selection"/> the output is also cropped to them.
    /// </summary>
    public IReadOnlyCollection<Guid>? SelectedIds { get; init; }

    /// <summary>Padding added around the content in document units (used by content and selection regions).</summary>
    public float Padding { get; init; } = 8f;

    /// <summary>
    /// Background color. Null uses the document's background; use <see cref="BinkColor.Transparent"/> to export
    /// with no background at all.
    /// </summary>
    public BinkColor? Background { get; init; }

    /// <summary>Whether hidden layers are included.</summary>
    public bool IncludeHiddenLayers { get; init; }

    /// <summary>Computes the exported region for a document.</summary>
    public BinkRect ResolveRegion(BinkDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        switch (Region)
        {
            case BinkExportRegion.Custom:
                return CustomRegion.Normalize();
            case BinkExportRegion.Content:
            {
                BinkRect bounds = document.ContentBounds;
                return bounds.IsEmpty ? document.Settings.PageRect : bounds.Inflate(Padding);
            }
            case BinkExportRegion.Selection:
            {
                BinkRect bounds = BinkRect.Empty;
                if (SelectedIds is not null)
                {
                    foreach (Guid id in SelectedIds) if (document.TryGetElement(id, out BinkElement? e)) bounds = bounds.Union(e.Bounds);
                }
                return bounds.IsEmpty ? document.Settings.PageRect : bounds.Inflate(Padding);
            }
            default:
            {
                BinkRect page = document.Settings.PageRect;
                if (!page.IsEmpty) return page;
                BinkRect content = document.ContentBounds;
                return content.IsEmpty ? new BinkRect(0, 0, 1, 1) : content.Inflate(Padding);
            }
        }
    }
}

/// <summary>Options specific to SVG export.</summary>
public sealed record BinkSvgExportOptions : BinkExportOptions
{
    /// <summary>Default options.</summary>
    public static readonly BinkSvgExportOptions Default = new();

    /// <summary>Decimals kept for coordinates. Fewer decimals give smaller files.</summary>
    public int Decimals { get; init; } = 2;

    /// <summary>Whether to include the XML declaration (needed when writing a standalone <c>.svg</c> file).</summary>
    public bool IncludeXmlDeclaration { get; init; }

    /// <summary>Optional document title, written as <c>&lt;title&gt;</c> for accessibility.</summary>
    public string? Title { get; init; }

    /// <summary>Optional description, written as <c>&lt;desc&gt;</c> for accessibility.</summary>
    public string? Description { get; init; }

    /// <summary>
    /// Whether each element's accessible description is written as a <c>&lt;title&gt;</c> inside its group. Adds
    /// bytes but makes exported drawings navigable by screen readers.
    /// </summary>
    public bool IncludeElementDescriptions { get; init; }

    /// <summary>
    /// Prefix for generated element ids. Restricted to letters, digits, hyphen and underscore, because the value
    /// is written into an <c>id</c> attribute; anything else falls back to the default.
    /// </summary>
    public string IdPrefix
    {
        get => _idPrefix;
        init => _idPrefix = IsSafeIdPrefix(value) ? value : "bi";
    }

    private readonly string _idPrefix = "bi";

    private static bool IsSafeIdPrefix(string? value)
    {
        if (string.IsNullOrEmpty(value) || value.Length > 64 || !char.IsAsciiLetter(value[0])) return false;
        foreach (char c in value)
        {
            if (!char.IsAsciiLetterOrDigit(c) && c != '-' && c != '_') return false;
        }
        return true;
    }

    /// <summary>Scale factor applied through the <c>width</c>/<c>height</c> attributes; the view box is unchanged.</summary>
    public float Scale { get; init; } = 1f;

    public long MaxLength { get; init; } = long.MaxValue;
}

/// <summary>
/// Exports documents as SVG. Strokes are exported as filled outline paths, shapes as native SVG shapes, text as
/// <c>&lt;text&gt;</c> and images as <c>&lt;image&gt;</c>: the output stays fully vector and editable in any SVG tool.
/// </summary>
/// <remarks>
/// Eraser strokes become SVG masks that apply to exactly the content they covered, so an erased drawing exports
/// the way it looks on screen without flattening anything to pixels.
/// </remarks>
public static class BinkSvgExporter
{
    /// <summary>Exports a document to an SVG string.</summary>
    public static string Export(BinkDocument document, BinkSvgExportOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(document);
        // Every stroke point becomes about a dozen characters of path data; guessing low makes the builder
        // repeatedly copy itself, which is what dominates the cost of exporting a large drawing.
        var builder = new StringBuilder((int)Math.Clamp(1024 + document.ElementCount * 200L + document.TotalPointCount * 26L, 1024, 512L * 1024 * 1024));
        using (var writer = new StringWriter(builder, CultureInfo.InvariantCulture))
        {
            Export(document, writer, options);
        }
        return builder.ToString();
    }

    /// <summary>Exports a document as UTF-8 bytes, suitable for writing a <c>.svg</c> file.</summary>
    public static byte[] ExportToUtf8Bytes(BinkDocument document, BinkSvgExportOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(document);
        using var stream = new MemoryStream((int)Math.Clamp(1024 + document.TotalPointCount * 26L, 1024, int.MaxValue));
        Export(document, stream, options);
        return stream.ToArray();
    }

    /// <summary>Exports a document into a stream as UTF-8.</summary>
    public static void Export(BinkDocument document, Stream stream, BinkSvgExportOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(stream);
        using var writer = new StreamWriter(stream, new UTF8Encoding(false), 16384, leaveOpen: true);
        Export(document, writer, options ?? BinkSvgExportOptions.Default with { IncludeXmlDeclaration = true });
    }

    /// <summary>Exports a document into a text writer.</summary>
    public static void Export(BinkDocument document, TextWriter writer, BinkSvgExportOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(writer);
        options ??= BinkSvgExportOptions.Default;
        if (options.MaxLength < long.MaxValue) writer = new LimitedWriter(writer, options.MaxLength);

        BinkRect region = options.ResolveRegion(document);
        if (region.IsEmpty) region = new BinkRect(region.X, region.Y, MathF.Max(region.Width, 1), MathF.Max(region.Height, 1));
        int decimals = Math.Clamp(options.Decimals, 0, 6);

        if (options.IncludeXmlDeclaration) writer.Write("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");

        float scale = options.Scale > 0 && float.IsFinite(options.Scale) ? options.Scale : 1f;
        // The file declares SVG 1.1, whose consumers only know xlink:href, so the prefix is declared for images.
        writer.Write("<svg xmlns=\"http://www.w3.org/2000/svg\" xmlns:xlink=\"http://www.w3.org/1999/xlink\" version=\"1.1\" width=\"");
        WriteNumber(writer, region.Width * scale, decimals);
        writer.Write("\" height=\"");
        WriteNumber(writer, region.Height * scale, decimals);
        writer.Write("\" viewBox=\"");
        WriteNumber(writer, region.X, decimals);
        writer.Write(' ');
        WriteNumber(writer, region.Y, decimals);
        writer.Write(' ');
        WriteNumber(writer, region.Width, decimals);
        writer.Write(' ');
        WriteNumber(writer, region.Height, decimals);
        writer.Write("\">");

        if (options.Title is { Length: > 0 } title)
        {
            writer.Write("<title>");
            BinkSvgText.WriteEscaped(writer, title);
            writer.Write("</title>");
        }
        if (options.Description is { Length: > 0 } description)
        {
            writer.Write("<desc>");
            BinkSvgText.WriteEscaped(writer, description);
            writer.Write("</desc>");
        }

        BinkColor background = options.Background ?? document.Settings.Background;
        if (!background.IsTransparent)
        {
            writer.Write("<rect x=\"");
            WriteNumber(writer, region.X, decimals);
            writer.Write("\" y=\"");
            WriteNumber(writer, region.Y, decimals);
            writer.Write("\" width=\"");
            WriteNumber(writer, region.Width, decimals);
            writer.Write("\" height=\"");
            WriteNumber(writer, region.Height, decimals);
            writer.Write("\" fill=\"");
            writer.Write((background with { A = 255 }).ToCss());
            writer.Write('"');
            if (background.A < 255)
            {
                writer.Write(" fill-opacity=\"");
                WriteNumber(writer, background.Alpha, 3);
                writer.Write('"');
            }
            writer.Write("/>");
        }

        var context = new BinkSvgRenderContext(writer, decimals) { IdPrefix = options.IdPrefix, XlinkDeclared = true };
        var state = new ExportState(options, region, decimals);
        foreach (BinkLayer layer in document.Layers)
        {
            if (!layer.IsVisible && !options.IncludeHiddenLayers) continue;
            WriteLayer(writer, context, layer, state);
        }

        // An element that opened a scope without closing it must not leave the document unbalanced.
        context.CloseAll();
        writer.Write("</svg>");
    }

    private sealed class ExportState(BinkSvgExportOptions options, BinkRect region, int decimals)
    {
        public BinkSvgExportOptions Options => options;
        public BinkRect Region => region;
        public int Decimals => decimals;
        public int MaskCounter;
    }

    private static void WriteLayer(TextWriter writer, BinkSvgRenderContext context, BinkLayer layer, ExportState state)
    {
        List<BinkElement> elements = FilterElements(layer, state.Options);
        if (elements.Count == 0) return;

        writer.Write("<g");
        if (layer.Name is { Length: > 0 })
        {
            writer.Write(" data-layer=\"");
            BinkSvgText.WriteEscaped(writer, layer.Name);
            writer.Write('"');
        }
        if (layer.Opacity < 1f)
        {
            writer.Write(" opacity=\"");
            WriteNumber(writer, layer.Opacity, 3);
            writer.Write('"');
        }
        // Isolated exactly when BinkDocumentRenderer isolates the layer on canvas and in the rasterizer. An
        // isolated group is what keeps a multiply highlight from reaching the layers below, so isolating every
        // layer made a highlight over a page image composite normally in SVG while it multiplied everywhere else.
        if (layer.BlendMode != BinkBlendMode.Normal)
        {
            writer.Write(" style=\"mix-blend-mode:");
            writer.Write(layer.BlendMode == BinkBlendMode.Multiply ? "multiply" : "screen");
            writer.Write(";isolation:isolate\"");
        }
        else if (layer.Opacity < 1f || HasEraser(layer)) writer.Write(" style=\"isolation:isolate\"");
        writer.Write('>');

        WriteRun(writer, context, elements, elements.Count, state);

        writer.Write("</g>");
    }

    private static bool HasEraser(BinkLayer layer)
    {
        foreach (BinkElement element in layer.Elements)
        {
            if (element is BinkStroke { IsEraser: true }) return true;
        }
        return false;
    }

    /// <summary>
    /// Most masks nested inside one another in a layer. XML parsers limit element depth (libxml2 refuses a
    /// document nested deeper than 256 elements unless told otherwise), so nesting deeper makes the file
    /// unreadable rather than wrong. The margin leaves room for the root, the layer and an element's own groups.
    /// </summary>
    private const int MaxMaskNesting = 240;

    /// <summary>
    /// Writes a layer's elements. Eraser strokes become masks that apply to everything drawn before them, which
    /// is exactly the sequential destination-out semantics used on canvas.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Consecutive eraser strokes share one mask: erasing twice with nothing drawn in between multiplies what is
    /// left by both, which is exactly what a mask holding both strokes does. Nesting therefore counts how often
    /// drawing and erasing alternate, not how many eraser strokes there are, and the pen's eraser end, which makes
    /// one eraser stroke per pointer-up, adds nesting only when ink is drawn between erasures.
    /// </para>
    /// <para>
    /// Past <see cref="MaxMaskNesting"/> alternations, every later eraser goes into one outermost mask applied to
    /// everything drawn before the last of them. Ink drawn between those later erasers is then also masked by the
    /// ones before it, so part of it can be hidden; erased ink can never reappear.
    /// </para>
    /// <para>
    /// Nesting is built iteratively. A recursive formulation would nest once per eraser stroke, and a document
    /// can contain hundreds of thousands of those, which would overflow the stack in a way .NET cannot catch.
    /// </para>
    /// </remarks>
    private static void WriteRun(TextWriter writer, BinkSvgRenderContext context, List<BinkElement> elements, int count, ExportState state)
    {
        // Each run of consecutive erasers, as [Start, End) in elements.
        var runs = new List<(int Start, int End)>();
        for (int i = 0; i < count; i++)
        {
            if (elements[i] is not BinkStroke { IsEraser: true }) continue;
            int start = i;
            while (i + 1 < count && elements[i + 1] is BinkStroke { IsEraser: true }) i++;
            runs.Add((start, i + 1));
        }

        if (runs.Count == 0)
        {
            for (int i = 0; i < count; i++) WriteElement(writer, context, elements[i], state);
            return;
        }

        // One mask per run, opened from the outside in. Past the limit the outermost mask holds every eraser from
        // its run to the last one; below the limit it is just the last run's.
        int outermost = Math.Min(runs.Count, MaxMaskNesting) - 1;
        for (int m = outermost; m >= 0; m--)
        {
            WriteMask(writer, elements, runs[m].Start, m == outermost ? runs[^1].End : runs[m].End, state, out string maskId);
            writer.Write("<g mask=\"url(#");
            writer.Write(maskId);
            writer.Write(")\">");
        }

        // Innermost first: everything before the first eraser, then the content after each run as its group
        // closes. The outermost group stays open until the last run, so the content between the runs it holds
        // stays under it.
        for (int i = 0; i < runs[0].Start; i++) WriteElement(writer, context, elements[i], state);
        for (int r = 0; r < runs.Count; r++)
        {
            if (r < outermost || r == runs.Count - 1) writer.Write("</g>");
            int from = runs[r].End;
            int to = r + 1 < runs.Count ? runs[r + 1].Start : count;
            for (int i = from; i < to; i++) WriteElement(writer, context, elements[i], state);
        }
    }

    private static void WriteMask(TextWriter writer, List<BinkElement> elements, int start, int end, ExportState state, out string maskId)
    {
        maskId = state.Options.IdPrefix + "-mask-" + state.MaskCounter++.ToString(CultureInfo.InvariantCulture);

        writer.Write("<mask id=\"");
        writer.Write(maskId);
        writer.Write("\" maskUnits=\"userSpaceOnUse\" x=\"");
        WriteNumber(writer, state.Region.X, state.Decimals);
        writer.Write("\" y=\"");
        WriteNumber(writer, state.Region.Y, state.Decimals);
        writer.Write("\" width=\"");
        WriteNumber(writer, state.Region.Width, state.Decimals);
        writer.Write("\" height=\"");
        WriteNumber(writer, state.Region.Height, state.Decimals);
        writer.Write("\"><rect x=\"");
        WriteNumber(writer, state.Region.X, state.Decimals);
        writer.Write("\" y=\"");
        WriteNumber(writer, state.Region.Y, state.Decimals);
        writer.Write("\" width=\"");
        WriteNumber(writer, state.Region.Width, state.Decimals);
        writer.Write("\" height=\"");
        WriteNumber(writer, state.Region.Height, state.Decimals);
        writer.Write("\" fill=\"#fff\"/>");

        // Black where an eraser passed: luminance 0 makes the masked content transparent there. Each eraser is
        // painted over the ones before it, so where two overlap the mask keeps the product of what each leaves.
        var maskContext = new BinkSvgRenderContext(writer, state.Decimals) { IdPrefix = state.Options.IdPrefix + "-m" + state.MaskCounter.ToString(CultureInfo.InvariantCulture), XlinkDeclared = true };
        for (int i = start; i < end; i++)
        {
            if (elements[i] is not BinkStroke { IsEraser: true } eraser) continue;
            maskContext.Save();
            if (!eraser.Transform.IsIdentity) maskContext.Transform(eraser.Transform);
            StrokeGeometryToMask(maskContext, eraser);
            maskContext.Restore();
        }
        writer.Write("</mask>");
    }

    private static void StrokeGeometryToMask(BinkSvgRenderContext context, BinkStroke eraser)
    {
        BinkStrokeGeometry geometry = eraser.Geometry;
        if (geometry.IsEmpty) return;
        // Over the white mask background, black at the eraser's alpha leaves 1 - alpha of the content: the same
        // partial erase the canvas and the rasterizer apply for a translucent eraser.
        var black = new BinkColor(0, 0, 0, eraser.Brush.Color.A);
        float opacity = eraser.Brush.Opacity;
        if (geometry.Mode == BinkStrokeGeometryMode.Fill) context.FillPath(geometry.Path, new BinkFillPaint(black, opacity));
        else context.StrokePath(geometry.Path, new BinkStrokePaint(black, geometry.LineWidth, opacity, BinkBlendMode.Normal, geometry.Cap, geometry.Join));
    }

    private static void WriteElement(TextWriter writer, BinkSvgRenderContext context, BinkElement element, ExportState state)
    {
        if (element is BinkStroke { IsEraser: true }) return;
        bool describe = state.Options.IncludeElementDescriptions;
        if (describe)
        {
            writer.Write("<g role=\"img\"><title>");
            BinkSvgText.WriteEscaped(writer, element.GetAccessibleDescription());
            writer.Write("</title>");
        }
        element.Draw(context);
        if (describe) writer.Write("</g>");
    }

    private static List<BinkElement> FilterElements(BinkLayer layer, BinkSvgExportOptions options)
    {
        var result = new List<BinkElement>(layer.Count);
        HashSet<Guid>? selection = options.SelectedIds is { Count: > 0 } ? [.. options.SelectedIds] : null;
        foreach (BinkElement element in layer.Elements)
        {
            if (selection is not null && !selection.Contains(element.Id) && element is not BinkStroke { IsEraser: true }) continue;
            result.Add(element);
        }
        // Leading erasers have nothing below them to erase and would only bloat the output.
        int leading = 0;
        while (leading < result.Count && result[leading] is BinkStroke { IsEraser: true }) leading++;
        result.RemoveRange(0, leading);
        return result;
    }

    private static void WriteNumber(TextWriter writer, float value, int decimals) => BinkSvgNumber.Write(writer, value, decimals);

    private sealed class LimitedWriter(TextWriter inner, long maxLength) : TextWriter(inner.FormatProvider)
    {
        private long _length;

        public override Encoding Encoding => inner.Encoding;

        public override void Write(char value) => Write(new ReadOnlySpan<char>(in value));

        public override void Write(char[] buffer, int index, int count) => Write(buffer.AsSpan(index, count));

        public override void Write(string? value) => Write(value.AsSpan());

        public override void Write(ReadOnlySpan<char> buffer)
        {
            if (buffer.Length > maxLength - _length)
            {
                inner.Write(buffer[..(int)Math.Max(maxLength - _length, 0)]);
                _length = Math.Max(_length, maxLength);
                throw new InvalidOperationException($"The SVG is longer than the limit of {maxLength} characters.");
            }
            _length += buffer.Length;
            inner.Write(buffer);
        }
    }
}
