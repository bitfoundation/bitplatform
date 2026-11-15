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

    // Guards a shared store's Fonts, EmittedFamilies and FontFaces: a build reads
    // them through its overlay under the engine gate while the UI thread commits
    // a published render's overlay or reads the style.
    private readonly object _sync = new();

    // An overlay's faces, by family, in the order they were first emitted.
    private readonly List<(string Family, string Face)> _pendingFaces = [];

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

    /// <summary>The accumulated <c>@font-face</c> CSS of a shared store.</summary>
    internal StringBuilder FontFaces { get; } = new();

    internal bool TryGetFont(object key, out BitPdfFont font)
    {
        if (_base is not null)
        {
            if (Fonts.TryGetValue(key, out font!))
            {
                return true;
            }

            lock (_base._sync)
            {
                return _base.Fonts.TryGetValue(key, out font!);
            }
        }

        lock (_sync)
        {
            return Fonts.TryGetValue(key, out font!);
        }
    }

    internal void AddFont(object key, BitPdfFont font)
    {
        if (_base is not null)
        {
            Fonts[key] = font;
            return;
        }

        lock (_sync)
        {
            Fonts[key] = font;
        }
    }

    /// <summary>Registers <paramref name="family"/> and returns <c>true</c> the first
    /// time it is seen, in this store or its base.</summary>
    internal bool TryAddFamily(string family)
    {
        if (_base is not null)
        {
            lock (_base._sync)
            {
                if (_base.EmittedFamilies.Contains(family))
                {
                    return false;
                }
            }

            return EmittedFamilies.Add(family);
        }

        lock (_sync)
        {
            return EmittedFamilies.Add(family);
        }
    }

    /// <summary>Adds the <c>@font-face</c> rule of <paramref name="family"/>, once
    /// <see cref="TryAddFamily"/> registered it.</summary>
    internal void AppendFace(string family, string face)
    {
        if (_base is not null)
        {
            _pendingFaces.Add((family, face));
            return;
        }

        lock (_sync)
        {
            FontFaces.Append(face);
        }
    }

    // A Range render registers fonts in an attempt-local overlay that reads the
    // shared store without copying it. Only a render the viewer publishes commits
    // the fonts and faces it added; an abandoned attempt (missing bytes, a
    // deferred miss, a superseded epoch) is simply dropped, so it cannot suppress
    // CSS on the next attempt.
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
        lock (target._sync)
        {
            foreach (var pair in Fonts)
            {
                target.Fonts.TryAdd(pair.Key, pair.Value);
            }

            // Another render may have committed a family since this one started:
            // its face is emitted once.
            foreach (var (family, face) in _pendingFaces)
            {
                if (target.EmittedFamilies.Add(family))
                {
                    target.FontFaces.Append(face);
                }
            }
        }

        Fonts.Clear();
        EmittedFamilies.Clear();
        _pendingFaces.Clear();
    }

    /// <summary>The document-wide <c>@font-face</c> stylesheet, or an empty string.</summary>
    public string FontFaceStyle
    {
        get
        {
            lock (_sync)
            {
                return FontFaces.Length > 0 ? $"<style>{FontFaces}</style>" : string.Empty;
            }
        }
    }
}
