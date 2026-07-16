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
    /// <summary>Parsed fonts, keyed by object identity (indirect ref or dict).</summary>
    internal Dictionary<object, BitPdfFont> Fonts { get; } = new();

    /// <summary>The <c>@font-face</c> family names already emitted.</summary>
    internal HashSet<string> EmittedFamilies { get; } = new();

    /// <summary>The accumulated <c>@font-face</c> CSS for the whole document.</summary>
    internal StringBuilder FontFaces { get; } = new();

    /// <summary>The document-wide <c>@font-face</c> stylesheet, or an empty string.</summary>
    public string FontFaceStyle => FontFaces.Length > 0 ? $"<style>{FontFaces}</style>" : string.Empty;
}
