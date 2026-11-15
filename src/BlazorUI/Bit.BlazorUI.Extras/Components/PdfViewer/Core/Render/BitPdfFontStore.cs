using System.Text;

namespace Bit.BlazorUI;

/// <summary>
/// A per-document store shared across page renders so an embedded font's parsed
/// model and its base64 <c>@font-face</c> rule are produced once, not once per
/// page. Without it, a 100-page document repeats every font's base64 payload 100
/// times in the emitted HTML.
/// </summary>
public sealed class BitPdfFontStore
{
    // Set on an overlay: reads fall through to it, writes stay local until Commit.
    private readonly BitPdfFontStore? _base;

    /// <summary>Creates an empty store.</summary>
    public BitPdfFontStore()
    {
    }

    private BitPdfFontStore(BitPdfFontStore @base) => _base = @base;

    /// <summary>Parsed fonts, keyed by object identity (indirect ref or dict). An
    /// overlay holds only the fonts it registered itself.</summary>
    internal Dictionary<object, BitPdfFont> Fonts { get; } = new();

    /// <summary>The <c>@font-face</c> family names already emitted (an overlay: by itself).</summary>
    internal HashSet<string> EmittedFamilies { get; } = new();

    /// <summary>The accumulated <c>@font-face</c> CSS (an overlay: its own faces).</summary>
    internal StringBuilder FontFaces { get; } = new();

    internal bool TryGetFont(object key, out BitPdfFont font)
        => Fonts.TryGetValue(key, out font!) || (_base is not null && _base.Fonts.TryGetValue(key, out font!));

    internal void AddFont(object key, BitPdfFont font) => Fonts[key] = font;

    /// <summary>Registers <paramref name="family"/> and returns <c>true</c> the first
    /// time it is seen, in this store or its base.</summary>
    internal bool TryAddFamily(string family)
        => _base?.EmittedFamilies.Contains(family) is not true && EmittedFamilies.Add(family);

    // A Range render registers fonts in an attempt-local overlay that reads the
    // shared store without copying it. Only a complete page commits the fonts and
    // faces it added; an abandoned attempt (missing bytes) is simply dropped, so
    // it cannot suppress CSS on the next attempt.
    internal BitPdfFontStore BeginOverlay()
    {
        if (_base is not null)
        {
            throw new InvalidOperationException("A font-store overlay cannot be overlaid again.");
        }

        return new(this);
    }

    internal void Commit()
    {
        var target = _base ?? throw new InvalidOperationException("Only a font-store overlay can be committed.");
        foreach (var pair in Fonts)
        {
            target.Fonts.TryAdd(pair.Key, pair.Value);
        }

        foreach (string family in EmittedFamilies)
        {
            target.EmittedFamilies.Add(family);
        }

        target.AppendFace(FontFaces.ToString());
        Fonts.Clear();
        EmittedFamilies.Clear();
        FontFaces.Clear();
    }

    // Builds append under the engine gate while the UI thread may read the style,
    // so the face text is guarded by its own lock.
    internal void AppendFace(string face)
    {
        lock (FontFaces)
        {
            FontFaces.Append(face);
        }
    }

    /// <summary>The document-wide <c>@font-face</c> stylesheet, or an empty string.</summary>
    public string FontFaceStyle
    {
        get
        {
            lock (FontFaces)
            {
                return FontFaces.Length > 0 ? $"<style>{FontFaces}</style>" : string.Empty;
            }
        }
    }
}
