namespace Bit.Bink;

/// <summary>The outcome of a PDF export.</summary>
/// <param name="Bytes">The PDF file.</param>
/// <param name="PageCount">Pages written: one per document.</param>
/// <param name="SkippedTextElements">
/// Text left out because it uses characters the PDF's standard fonts cannot show (they cover Western European
/// languages). Export those drawings as SVG, or as PNG from the browser, where the viewer's fonts draw them.
/// </param>
/// <param name="SkippedImages">
/// Images left out: remote images (only embedded ones are in the document), formats other than PNG and JPEG, CMYK
/// JPEGs, unreadable data, and pictures past <see cref="BinkPdfExportOptions.MaxImagePixels"/>.
/// </param>
public readonly record struct BinkPdfExportResult(byte[] Bytes, int PageCount, int SkippedTextElements, int SkippedImages)
{
    /// <summary>True when everything in the document was drawn.</summary>
    public bool IsComplete => SkippedTextElements == 0 && SkippedImages == 0;
}
