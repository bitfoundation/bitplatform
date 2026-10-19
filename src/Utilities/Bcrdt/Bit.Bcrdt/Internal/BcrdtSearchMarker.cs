namespace Bit.Bcrdt;

/// <summary>A cached "index N is at this block" association.</summary>
internal sealed class BcrdtSearchMarker(BcrdtItem item, int index)
{
    public BcrdtItem Item = item;
    public int Index = index;
    public long Timestamp = BcrdtSearchMarkerCache.NextTimestamp();
}

/// <summary>
/// Caches recently used index positions so sequential editing does not rescan the document.
/// </summary>
/// <remarks>
/// <para>
/// Reaching index <c>n</c> in a linked list costs a walk from the head. For an editor that is
/// disastrous: every keystroke near the end of a long document would traverse the entire document,
/// making typing cost grow linearly with what has already been typed.
/// </para>
/// <para>
/// Editing is overwhelmingly local, so a handful of remembered positions turns that walk into a few
/// steps from the nearest marker. The markers are pure cache - losing or invalidating one costs
/// time, never correctness - which is why every operation that cannot cheaply prove a marker is
/// still accurate simply drops it.
/// </para>
/// </remarks>
internal static class BcrdtSearchMarkerCache
{
    private const int MaxMarkers = 40;
    private static long _timestamp;

    public static long NextTimestamp() => Interlocked.Increment(ref _timestamp);

    /// <summary>
    /// Finds the block containing <paramref name="index"/>, using and refreshing the cache.
    /// </summary>
    /// <returns>
    /// A marker positioned at or before the index, or <see langword="null"/> when the caller should
    /// just walk from the start.
    /// </returns>
    /// <remarks>
    /// Rich text never uses the cache. A marker jumps straight to a block and skips the formatting
    /// marks before it, so a text operation starting there would not know which attributes are in
    /// force - and inserting, formatting and deleting all depend on knowing that exactly. Once a type
    /// carries formatting, every lookup walks from the start.
    /// </remarks>
    public static BcrdtSearchMarker? Find(BcrdtAbstractType type, int index)
    {
        if (type.Start is null || index == 0) return null;

        if (type.HasFormatting)
        {
            type.InvalidateSearchMarkers();
            return null;
        }

        List<BcrdtSearchMarker> markers = type.SearchMarkers ??= [];

        BcrdtSearchMarker? closest = null;
        foreach (BcrdtSearchMarker marker in markers)
        {
            if (closest is null || Math.Abs(index - marker.Index) < Math.Abs(index - closest.Index))
            {
                closest = marker;
            }
        }

        BcrdtItem? cursor = type.Start;
        int cursorIndex = 0;
        if (closest is not null)
        {
            cursor = closest.Item;
            cursorIndex = closest.Index;
            closest.Timestamp = NextTimestamp();
        }

        while (cursor?.Right is not null && cursorIndex < index)
        {
            if (!cursor.Deleted && cursor.Countable)
            {
                if (index < cursorIndex + cursor.Length) break;
                cursorIndex += cursor.Length;
            }
            cursor = cursor.Right;
        }

        while (cursor?.Left is not null && cursorIndex > index)
        {
            cursor = cursor.Left;
            if (!cursor.Deleted && cursor.Countable) cursorIndex -= cursor.Length;
        }

        // Never point a marker at a block that could be absorbed by its left neighbour: the merge
        // would leave the marker addressing the interior of a larger block, and its index would be
        // silently wrong from then on.
        while (cursor?.Left is not null &&
               cursor.Left.Id.Client == cursor.Id.Client &&
               cursor.Left.Id.Clock + cursor.Left.Length == cursor.Id.Clock)
        {
            cursor = cursor.Left;
            if (!cursor.Deleted && cursor.Countable) cursorIndex -= cursor.Length;
        }

        if (cursor is null) return null;

        if (closest is not null && Math.Abs(closest.Index - cursorIndex) < type.Length / MaxMarkers)
        {
            Overwrite(closest, cursor, cursorIndex);
            return closest;
        }

        return Add(markers, cursor, cursorIndex);
    }

    private static BcrdtSearchMarker Add(List<BcrdtSearchMarker> markers, BcrdtItem item, int index)
    {
        if (markers.Count >= MaxMarkers)
        {
            // Evict the least recently used marker rather than growing without bound.
            BcrdtSearchMarker oldest = markers[0];
            foreach (BcrdtSearchMarker marker in markers)
            {
                if (marker.Timestamp < oldest.Timestamp) oldest = marker;
            }
            Overwrite(oldest, item, index);
            return oldest;
        }

        var created = new BcrdtSearchMarker(item, index);
        markers.Add(created);
        item.HasMarker = true;
        return created;
    }

    private static void Overwrite(BcrdtSearchMarker marker, BcrdtItem item, int index)
    {
        marker.Item.HasMarker = false;
        marker.Item = item;
        marker.Index = index;
        marker.Timestamp = NextTimestamp();
        item.HasMarker = true;
    }

    /// <summary>
    /// Shifts cached indices after content was inserted or removed at <paramref name="index"/>.
    /// </summary>
    /// <param name="markers">The cache to adjust, if there is one.</param>
    /// <param name="index">Where the change happened.</param>
    /// <param name="length">Positive for an insertion, negative for a removal.</param>
    public static void ApplyChange(List<BcrdtSearchMarker>? markers, int index, int length)
    {
        if (markers is null) return;

        for (int i = markers.Count - 1; i >= 0; i--)
        {
            BcrdtSearchMarker marker = markers[i];

            if (length > 0)
            {
                // An insertion can leave a marker pointing at a block that is now deleted or
                // uncountable. Walk back to the nearest real position so the stored index still
                // means something; if there is none, or another marker already owns that position,
                // drop this one.
                BcrdtItem? cursor = marker.Item;
                cursor.HasMarker = false;

                while (cursor is not null && (cursor.Deleted || !cursor.Countable))
                {
                    cursor = cursor.Left;
                    if (cursor is not null && !cursor.Deleted && cursor.Countable) marker.Index -= cursor.Length;
                }

                if (cursor is null || cursor.HasMarker)
                {
                    markers.RemoveAt(i);
                    continue;
                }

                marker.Item = cursor;
                cursor.HasMarker = true;
            }

            if (index < marker.Index || (length > 0 && index == marker.Index))
            {
                marker.Index = Math.Max(index, marker.Index + length);
            }
        }
    }
}
