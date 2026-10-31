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
    /// <summary>
    /// The pixel bytes the cache may hold. Each bitmap is bounded only by <see cref="Limits"/>, so without this a few
    /// dozen large texts could keep gigabytes alive; past it the oldest entries go, and the newest always stays.
    /// </summary>
    private const long CacheByteLimit = 64L * 1024 * 1024;

    private readonly BrowserImageInterop _interop;
    // Ordered by insertion, so the first entry is always the oldest; a plain dictionary reuses a removed entry's slot.
    private readonly OrderedDictionary<CacheKey, CachedText> _cache = [];
    private readonly int _cacheLimit;
    private long _cachedBytes;
    private ImageLimits _limits = ImageLimits.Default;

    public BrowserTextRasterizer(BrowserImageInterop interop, int cacheLimit = 64)
    {
        _interop = interop ?? throw new ArgumentNullException(nameof(interop));
        _cacheLimit = Math.Max(1, cacheLimit);
    }

    /// <summary>
    /// Limits the bitmap of rendered text is held to. The browser checks the size before it allocates a canvas, so text
    /// that would need a larger one fails with an <see cref="ImageLimitExceededException"/> instead. Changing them
    /// empties the cache, so a bitmap rendered under looser limits is not handed out under tighter ones.
    /// </summary>
    public ImageLimits Limits
    {
        get => _limits;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            if (value == _limits) return;
            _limits = value;
            ClearCache();
        }
    }

    private readonly record struct CacheKey(string Text, string Font, string Color, float MaxWidth, float LetterSpacing, float LineHeight, string Align);

    private sealed class CachedText : IDisposable
    {
        public required ImageBuffer Pixels { get; init; }
        public required TextRenderInfo Info { get; init; }
        public long ByteCount => (long)Pixels.Width * Pixels.Height * 4;
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
            var limits = Limits;
            using var result = await _interop.InvokeForBytesAsync<TextRenderInfo>(
                "renderText", cancellationToken, text, font, color, style.MaxWidth, style.LetterSpacing, style.LineHeight, align,
                limits.MaxPixels, limits.MaxWidth, limits.MaxHeight).ConfigureAwait(false);
            // Text past the limits comes back as its size alone, with no canvas ever made for it.
            if (result.Info is { TooLarge: true } refused)
                throw new ImageLimitExceededException($"The text needs a {refused.Width}x{refused.Height} bitmap, more than the configured limits allow (max {limits.MaxWidth}x{limits.MaxHeight}, {limits.MaxPixels} pixels).");
            if (!result.HasValue || result.Info is not { Width: > 0, Height: > 0 } rendered)
                throw new ImageException("The browser produced no pixels for this text.");
            info = rendered;
            // Held to the limits as they are now, in case they were tightened while the browser rendered: the cache
            // never holds a bitmap they would refuse.
            buffer = ImageBuffer.Create(info.Width, info.Height, clear: false, limits: Limits);
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
        // Two renders of the same text in flight at once: the later one replaces the earlier.
        if (_cache.Remove(key, out var replaced))
        {
            _cachedBytes -= replaced.ByteCount;
            replaced.Dispose();
        }
        // Bounded by entries and by bytes, dropping the oldest first rather than growing without limit.
        while (_cache.Count > 0 && (_cache.Count >= _cacheLimit || _cachedBytes + entry.ByteCount > CacheByteLimit))
        {
            var evicted = _cache.GetAt(0).Value;
            _cachedBytes -= evicted.ByteCount;
            evicted.Dispose();
            _cache.RemoveAt(0);
        }
        _cache[key] = entry;
        _cachedBytes += entry.ByteCount;
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
        _cachedBytes = 0;
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

        /// <summary>Set when the text was refused for needing a bitmap past the limits; only the size is filled in then.</summary>
        [JsonPropertyName("tooLarge")] public bool TooLarge { get; set; }
    }
}
