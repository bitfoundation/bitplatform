using System.Collections.Generic;

namespace Bit.Butil;

/// <summary>What a drop was carrying.</summary>
/// <param name="Files">
/// The files, if any. Their contents are readable afterwards through
/// <see cref="Bit.Butil.DataTransfer.ReadFile"/> - the drop handler does not have to do the reading.
/// </param>
/// <param name="Items">
/// Everything that is not a file, keyed by MIME type: <c>"text/plain"</c> for dragged text,
/// <c>"text/uri-list"</c> for a dragged link, <c>"text/html"</c> for a rich selection, plus whatever
/// custom type the drag source set. Read here rather than left for later, because
/// <c>getData</c> outside the drop event answers with an empty string.
/// </param>
public record DropPayload(DroppedFile[] Files, Dictionary<string, string> Items)
{
    /// <summary>The dragged text, or null when the drag carried none.</summary>
    public string? Text => Items.TryGetValue("text/plain", out var text) ? text : null;

    /// <summary>
    /// The dragged URL, or null. This is what arrives when a link, an image or a tab is dragged in
    /// from another window - a page can accept a link without the file behind it.
    /// </summary>
    public string? Uri => Items.TryGetValue("text/uri-list", out var uri) ? uri : null;
}
