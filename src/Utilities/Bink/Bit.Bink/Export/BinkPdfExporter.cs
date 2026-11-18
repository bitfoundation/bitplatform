namespace Bit.Bink;

/// <summary>
/// Writes documents as PDF in pure .NET, with no native dependencies: on a server, in WebAssembly or in a test.
/// </summary>
/// <remarks>
/// <para>
/// The file is vector, like the SVG export, and draws through the same <see cref="IBinkRenderContext"/> as the canvas,
/// so it matches the screen. Strokes, shapes and paths stay paths; opacity, multiply and screen blending and layer
/// opacity use PDF transparency; and pixel-eraser strokes become soft masks over exactly what they erased, nothing
/// flattened to pixels. Embedded JPEGs are copied in as they are, embedded PNGs are decoded and stored losslessly
/// with their transparency. Each document is one page, so a notebook of documents is one file.
/// </para>
/// <para>
/// Text is real text, selectable and searchable, set in the standard Helvetica, Times or Courier faces (by the
/// style's font family) that every PDF viewer has, which cover Western European languages. Text with other characters
/// is left out and counted in <see cref="BinkPdfExportResult.SkippedTextElements"/> rather than drawn wrongly.
/// Output is deterministic: the same document always produces the same bytes.
/// </para>
/// </remarks>
public static class BinkPdfExporter
{
    /// <summary>The largest page side, in points, that PDF viewers display: 200 inches. Larger pages are scaled down to it.</summary>
    public const float MaxPageSize = 14_400f;

    /// <summary>Exports a document as a one-page PDF.</summary>
    public static byte[] Export(BinkDocument document, BinkPdfExportOptions? options = null) => ExportWithDetails([document], options).Bytes;

    /// <summary>Exports a document as a one-page PDF into a stream.</summary>
    public static BinkPdfExportResult Export(BinkDocument document, Stream stream, BinkPdfExportOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(stream);
        BinkPdfExportResult result = ExportWithDetails([document], options);
        stream.Write(result.Bytes);
        return result;
    }

    /// <summary>Exports a document as a one-page PDF and reports what could not be drawn.</summary>
    public static BinkPdfExportResult ExportWithDetails(BinkDocument document, BinkPdfExportOptions? options = null)
        => ExportWithDetails([document], options);

    /// <summary>
    /// Exports documents as the pages of one PDF, in order, and reports what could not be drawn. Each page is the
    /// size of its own document's export region.
    /// </summary>
    /// <exception cref="ArgumentException">There are no documents, or one of them is null.</exception>
    public static BinkPdfExportResult ExportWithDetails(IEnumerable<BinkDocument> documents, BinkPdfExportOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(documents);
        options ??= BinkPdfExportOptions.Default;
        float scale = options.Scale;
        if (!float.IsFinite(scale) || scale <= 0) throw new ArgumentOutOfRangeException(nameof(options), "The export scale must be a positive, finite number.");

        var file = new BinkPdfFile(options.Compress);
        var resources = new BinkPdfResources(file, options.MaxImagePixels);
        int pagesId = file.Reserve();
        var pageIds = new List<int>();
        int skippedText = 0;

        foreach (BinkDocument document in documents)
        {
            if (document is null) throw new ArgumentException("A document to export is null.", nameof(documents));
            BinkRect region = options.ResolveRegion(document);
            if (region.IsEmpty) region = new BinkRect(region.X, region.Y, MathF.Max(region.Width, 1f), MathF.Max(region.Height, 1f));

            if (!float.IsFinite(region.X) || !float.IsFinite(region.Y) || !float.IsFinite(region.Width) || !float.IsFinite(region.Height))
                throw new InvalidOperationException("The export region is not a finite rectangle.");
            // A page larger than PDF viewers show (14,400 points, 200 inches) is scaled down to fit rather than
            // written at a size nothing opens, or overflowing to a size of zero.
            double largest = Math.Max(region.Width, region.Height) * (double)scale;
            float pageScale = largest > MaxPageSize ? (float)(scale * (MaxPageSize / largest)) : scale;

            var context = new BinkPdfRenderContext(resources, region);
            BinkColor background = options.Background ?? document.Settings.Background;
            BinkDocumentRenderer.RenderBackground(context, background, region);
            BinkDocumentRenderer.Render(document, context, new BinkRenderOptions
            {
                ClipBounds = region,
                IncludeHiddenLayers = options.IncludeHiddenLayers,
                ElementFilter = options.SelectedIds is { Count: > 0 } ? options.SelectedIds : null,
            });
            // The page size is written to a thousandth of a point; a side that would round to nothing makes a page no
            // viewer can show, so such a scale is refused rather than written.
            string width = BinkPdfFile.Number(region.Width * pageScale), height = BinkPdfFile.Number(region.Height * pageScale);
            if (width == "0" || height == "0")
                throw new ArgumentOutOfRangeException(nameof(options), "The export scale makes a page side smaller than a thousandth of a point.");

            byte[] content = context.Finish(pageScale);
            skippedText += context.SkippedText;

            int contentId = file.AddStream("", content);
            int pageId = file.Add($"<< /Type /Page /Parent {pagesId} 0 R /MediaBox [0 0 {width} {height}] /Resources {resources.Id} 0 R /Contents {contentId} 0 R >>");
            pageIds.Add(pageId);
        }
        if (pageIds.Count == 0) throw new ArgumentException("There is no document to export.", nameof(documents));

        file.Set(pagesId, $"<< /Type /Pages /Kids [{string.Join(' ', pageIds.Select(id => id + " 0 R"))}] /Count {pageIds.Count} >>");
        resources.Write();

        var catalog = new System.Text.StringBuilder($"<< /Type /Catalog /Pages {pagesId} 0 R");
        if (options.Title is { Length: > 0 }) catalog.Append(" /ViewerPreferences << /DisplayDocTitle true >>");
        if (options.Language is { Length: > 0 } language) catalog.Append(" /Lang ").Append(BinkPdfFile.TextString(language));
        catalog.Append(" >>");
        int catalogId = file.Add(catalog.ToString());

        var info = new System.Text.StringBuilder("<< /Producer (Bit.Bink)");
        if (options.Title is { Length: > 0 } title) info.Append(" /Title ").Append(BinkPdfFile.TextString(title));
        if (options.Description is { Length: > 0 } description) info.Append(" /Subject ").Append(BinkPdfFile.TextString(description));
        if (options.Author is { Length: > 0 } author) info.Append(" /Author ").Append(BinkPdfFile.TextString(author));
        info.Append(" >>");
        int infoId = file.Add(info.ToString());

        return new BinkPdfExportResult(file.ToBytes(catalogId, infoId), pageIds.Count, skippedText, resources.SkippedImages);
    }
}
