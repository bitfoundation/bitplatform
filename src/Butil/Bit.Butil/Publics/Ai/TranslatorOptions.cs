namespace Bit.Butil;

/// <summary>
/// The language pair a <see cref="Translator"/> session translates between. Both are required - a
/// translator is created per pair, and each pair is its own downloadable model.
/// </summary>
public class TranslatorOptions
{
    /// <summary>The language being translated from, as a BCP 47 tag (<c>"en"</c>).</summary>
    public string SourceLanguage { get; set; } = string.Empty;

    /// <summary>The language being translated to, as a BCP 47 tag (<c>"fr"</c>).</summary>
    public string TargetLanguage { get; set; } = string.Empty;
}
