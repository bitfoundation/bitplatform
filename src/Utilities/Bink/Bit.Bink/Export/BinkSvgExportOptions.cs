namespace Bit.Bink;

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

    /// <summary>
    /// Maximum length of the output, in characters; past it the export throws <see cref="InvalidOperationException"/>
    /// instead of writing more. Unlimited by default. An embedded image is written in full wherever it is drawn, so a
    /// document that shows one picture many times exports to far more text than it holds; a server exporting
    /// documents it did not write sets this.
    /// </summary>
    public long MaxLength { get; init; } = long.MaxValue;
}
