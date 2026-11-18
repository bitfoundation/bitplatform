namespace Bit.Bink;

/// <summary>What <see cref="BinkSvgImporter"/> produced.</summary>
public sealed class BinkSvgImportResult
{
    internal BinkSvgImportResult(IReadOnlyList<BinkElement> elements, IReadOnlyList<string> warnings, float width, float height)
    {
        Elements = elements;
        Warnings = warnings;
        Width = width;
        Height = height;
        BinkRect bounds = BinkRect.Empty;
        foreach (BinkElement element in elements) bounds = bounds.Union(element.Bounds);
        Bounds = bounds;
    }

    /// <summary>The elements, bottom-most first, in the SVG's user units.</summary>
    public IReadOnlyList<BinkElement> Elements { get; }

    /// <summary>
    /// What the SVG used that could not be imported, one line per kind of thing (gradients, filters, a stylesheet,
    /// a remote image...). Empty when everything was imported.
    /// </summary>
    public IReadOnlyList<string> Warnings { get; }

    /// <summary>The SVG's own width, or 0 when it states none.</summary>
    public float Width { get; }

    /// <summary>The SVG's own height, or 0 when it states none.</summary>
    public float Height { get; }

    /// <summary>The union of the elements' bounds.</summary>
    public BinkRect Bounds { get; }
}
