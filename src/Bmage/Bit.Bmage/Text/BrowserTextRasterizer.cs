using System.Globalization;
using System.Numerics;
using System.Text.Json.Serialization;
using Bit.Bmage.Drawing;
using Bit.Bmage.Interop;
using Microsoft.JSInterop;

namespace Bit.Bmage.Text;

/// <summary>
/// Renders text with the browser's own text engine, so system fonts, emoji, right-to-left scripts and complex shaping
/// all work without shipping a font file.
/// </summary>
/// <remarks>
/// <para>
/// The browser produces pixels rather than outlines, so this rasteriser draws a bitmap instead of a vector path. That
/// means <see cref="ITextRasterizer.GetTextPath"/> is not supported here; use <see cref="DrawTextAsync"/> on a canvas.
/// Where true outlines matter (stroked or transformed text at arbitrary scale), register a font with
/// <see cref="FontTextRasterizer"/> instead.
/// </para>
/// <para>
/// Text rendering is asynchronous because it crosses the JavaScript boundary. Results are cached by content and style,
/// which matters when a text annotation is redrawn on every pointer move.
/// </para>
/// </remarks>
public sealed class BrowserTextRasterizer : IDisposable
{
    private readonly BrowserImageInterop _interop;
    private readonly Dictionary<CacheKey, CachedText> _cache = [];
    private readonly int _cacheLimit;

    public BrowserTextRasterizer(BrowserImageInterop interop, int cacheLimit = 64)
    {
        _interop = interop ?? throw new ArgumentNullException(nameof(interop));
        _cacheLimit = Math.Max(1, cacheLimit);
    }

    private readonly record struct CacheKey(string Text, string Font, string Color, float MaxWidth, float LetterSpacing, float LineHeight, string Align);

    private sealed class CachedText : IDisposable
    {
        public required ImageBuffer Pixels { get; init; }
        public required TextRenderInfo Info { get; init; }
        public void Dispose() => Pixels.Dispose();
    }

    /// <summary>Measures text using the browser's font metrics.</summary>
    public async ValueTask<TextMetrics> MeasureAsync(string text, TextStyle style, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(style);
        if (string.IsNullOrEmpty(text)) return new TextMetrics(0, 0, 0, 0, 0);
        var rendered = await RenderAsync(text, style, cancellationToken).ConfigureAwait(false);
        var info = rendered.Info;
        return new TextMetrics((float)info.TextWidth, (float)(info.LineCount * style.FontSize * style.LineHeight), (float)info.Ascent, (float)info.Descent, info.LineCount);
    }

    /// <summary>
    /// Draws text onto a canvas at the given position, honouring the style's alignment and baseline.
    /// </summary>
    public async ValueTask DrawTextAsync(ImageCanvas canvas, string text, Vector2 position, TextStyle style, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(canvas);
        ArgumentNullException.ThrowIfNull(style);
        if (string.IsNullOrEmpty(text)) return;
        var rendered = await RenderAsync(text, style, cancellationToken).ConfigureAwait(false);
        var info = rendered.Info;
        // The browser drew the text with its origin at (originX, originY) inside the bitmap; align that with the
        // requested position, adjusting for the requested baseline.
        var baselineOffset = style.Baseline switch
        {
            TextBaseline.Top => (float)info.Ascent,
            TextBaseline.Middle => (float)info.Ascent - (float)(info.LineCount * style.FontSize * style.LineHeight) / 2,
            TextBaseline.Bottom => (float)info.Ascent - (float)(info.LineCount * style.FontSize * style.LineHeight),
            _ => 0f,
        };
        var origin = new Vector2(position.X - (float)info.OriginX, position.Y - (float)info.OriginY + baselineOffset);
        canvas.DrawImage(rendered.Pixels, origin);
    }

    private async ValueTask<CachedText> RenderAsync(string text, TextStyle style, CancellationToken cancellationToken)
    {
        var font = BuildFontString(style);
        var color = (style.FillColor ?? Rgba32.Black).ToCss();
        var align = style.Alignment switch { TextAlignment.Center => "center", TextAlignment.Right => "right", _ => "left" };
        var key = new CacheKey(text, font, color, style.MaxWidth ?? 0, style.LetterSpacing, style.LineHeight, align);
        if (_cache.TryGetValue(key, out var cached)) return cached;

        TextRenderInfo info;
        ImageBuffer buffer;
        try
        {
            using var result = await _interop.InvokeForBytesAsync<TextRenderInfo>(
                "renderText", cancellationToken, text, font, color, style.MaxWidth, style.LetterSpacing, style.LineHeight, align).ConfigureAwait(false);
            if (!result.HasValue || result.Info is not { Width: > 0, Height: > 0 } rendered)
                throw new ImageException("The browser produced no pixels for this text.");
            info = rendered;
            buffer = ImageBuffer.Create(info.Width, info.Height, clear: false);
            try
            {
                result.Bytes.Span.CopyTo(buffer.Bytes);
            }
            catch
            {
                buffer.Dispose();
                throw;
            }
        }
        catch (JSException ex)
        {
            throw new ImageException($"The browser failed to render text: {ex.Message}", ex);
        }

        var entry = new CachedText { Pixels = buffer, Info = info };
        if (_cache.Count >= _cacheLimit)
        {
            // Simple bounded cache: drop the oldest entry rather than growing without limit.
            var oldest = _cache.Keys.First();
            _cache[oldest].Dispose();
            _cache.Remove(oldest);
        }
        _cache[key] = entry;
        return entry;
    }

    /// <summary>Builds a CSS font shorthand string from a style.</summary>
    public static string BuildFontString(TextStyle style)
    {
        ArgumentNullException.ThrowIfNull(style);
        var weight = style.Bold ? "bold " : string.Empty;
        var italic = style.Italic ? "italic " : string.Empty;
        var size = style.FontSize.ToString("0.##", CultureInfo.InvariantCulture);
        return $"{italic}{weight}{size}px {style.FontFamily}";
    }

    /// <summary>Empties the cache and releases the pixel buffers it holds.</summary>
    public void ClearCache()
    {
        foreach (var entry in _cache.Values) entry.Dispose();
        _cache.Clear();
    }

    public void Dispose() => ClearCache();

    /// <summary>Layout information returned by the browser after rendering text.</summary>
    public sealed class TextRenderInfo
    {
        [JsonPropertyName("width")] public int Width { get; set; }
        [JsonPropertyName("height")] public int Height { get; set; }
        [JsonPropertyName("originX")] public double OriginX { get; set; }
        [JsonPropertyName("originY")] public double OriginY { get; set; }
        [JsonPropertyName("lineCount")] public int LineCount { get; set; }
        [JsonPropertyName("textWidth")] public double TextWidth { get; set; }
        [JsonPropertyName("ascent")] public double Ascent { get; set; }
        [JsonPropertyName("descent")] public double Descent { get; set; }
        [JsonPropertyName("byteLength")] public long ByteLength { get; set; }
    }
}
