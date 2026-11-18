namespace Bit.Bink;

/// <summary>Options shared by the SVG and PNG exporters.</summary>
public record BinkExportOptions
{
    /// <summary>Which part of the document to export.</summary>
    public BinkExportRegion Region { get; init; } = BinkExportRegion.Page;

    /// <summary>The rectangle used when <see cref="Region"/> is <see cref="BinkExportRegion.Custom"/>.</summary>
    public BinkRect CustomRegion { get; init; }

    /// <summary>
    /// When set, only these elements are drawn (eraser strokes are always kept so erasing still applies). With
    /// <see cref="BinkExportRegion.Selection"/> the output is also cropped to them.
    /// </summary>
    public IReadOnlyCollection<Guid>? SelectedIds { get; init; }

    /// <summary>Padding added around the content in document units (used by content and selection regions).</summary>
    public float Padding { get; init; } = 8f;

    /// <summary>
    /// Background color. Null uses the document's background; use <see cref="BinkColor.Transparent"/> to export
    /// with no background at all.
    /// </summary>
    public BinkColor? Background { get; init; }

    /// <summary>Whether hidden layers are included.</summary>
    public bool IncludeHiddenLayers { get; init; }

    /// <summary>Computes the exported region for a document.</summary>
    public BinkRect ResolveRegion(BinkDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        switch (Region)
        {
            case BinkExportRegion.Custom:
                return CustomRegion.Normalize();
            case BinkExportRegion.Content:
            {
                BinkRect bounds = document.ContentBounds;
                return bounds.IsEmpty ? document.Settings.PageRect : bounds.Inflate(Padding);
            }
            case BinkExportRegion.Selection:
            {
                BinkRect bounds = BinkRect.Empty;
                if (SelectedIds is not null)
                {
                    foreach (Guid id in SelectedIds) if (document.TryGetElement(id, out BinkElement? e)) bounds = bounds.Union(e.Bounds);
                }
                return bounds.IsEmpty ? document.Settings.PageRect : bounds.Inflate(Padding);
            }
            default:
            {
                BinkRect page = document.Settings.PageRect;
                if (!page.IsEmpty) return page;
                BinkRect content = document.ContentBounds;
                return content.IsEmpty ? new BinkRect(0, 0, 1, 1) : content.Inflate(Padding);
            }
        }
    }
}
