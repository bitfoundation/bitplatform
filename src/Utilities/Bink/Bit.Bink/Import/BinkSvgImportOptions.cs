namespace Bit.Bink;

/// <summary>What <see cref="BinkSvgImporter"/> reads, and how much of it.</summary>
public sealed record BinkSvgImportOptions
{
    /// <summary>Safe defaults for SVG from anywhere.</summary>
    public static readonly BinkSvgImportOptions Default = new();

    /// <summary>The longest SVG accepted, in characters.</summary>
    public int MaxLength { get; init; } = 16 * 1024 * 1024;

    /// <summary>The most elements an import may produce.</summary>
    public int MaxElements { get; init; } = 10_000;

    /// <summary>The most path segments all paths together may have.</summary>
    public int MaxSegments { get; init; } = 500_000;

    /// <summary>Whether <c>text</c> elements become text elements. Their layout is approximate: see the remarks of <see cref="BinkSvgImporter"/>.</summary>
    public bool ImportText { get; init; } = true;

    /// <summary>
    /// Whether <c>image</c> elements whose picture is embedded as a raster <c>data:</c> URI are imported. Images
    /// that refer to a file or a URL are never loaded, whatever this says.
    /// </summary>
    public bool ImportImages { get; init; } = true;
}
