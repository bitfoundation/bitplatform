namespace Bit.Bdnd;

/// <summary>
/// The index arithmetic of dropping an item into a list - before or after the item it was dropped on, in the same
/// list or another - done once and right, so a drop handler only says what moved where.
/// </summary>
/// <remarks>
/// A target with <see cref="BdndDropTarget{TItem}.Edges"/> reports the edge a drop was closest to; with the index of the
/// item that target shows, <see cref="InsertionIndex"/> gives where the dropped item goes in that list, and
/// <see cref="Move{T}"/> moves it there - out of the list it came from, which may be the same one.
/// </remarks>
public static class BdndList
{
    /// <summary>
    /// The index to insert an item at, in a list as it is now, when it is dropped on the item at
    /// <paramref name="targetIndex"/> against <paramref name="edge"/>: before it for <see cref="BdndEdge.Top"/> and
    /// <see cref="BdndEdge.Left"/> (or no edge), after it for <see cref="BdndEdge.Bottom"/> and <see cref="BdndEdge.Right"/>.
    /// Edges are where they are on screen, so a list laid out against its order - right to left (<c>dir="rtl"</c>),
    /// <c>row-reverse</c> or <c>column-reverse</c> - passes <paramref name="reversed"/>: there the left (or bottom) edge
    /// is after the item.
    /// </summary>
    public static int InsertionIndex(int targetIndex, BdndEdge? edge, bool reversed = false) =>
        edge is null ? targetIndex
        : (edge is BdndEdge.Bottom or BdndEdge.Right) != reversed ? targetIndex + 1
        : targetIndex;

    /// <summary>
    /// The index an item at <paramref name="startIndex"/> ends up at when it is dropped on the item at
    /// <paramref name="targetIndex"/> of the same list against <paramref name="edge"/> - one less than the insertion
    /// index when the item came from before it, since taking it out shifts the rest up. <paramref name="reversed"/> as
    /// for <see cref="InsertionIndex"/>.
    /// </summary>
    public static int ReorderIndex(int startIndex, int targetIndex, BdndEdge? edge, bool reversed = false)
    {
        var insertion = InsertionIndex(targetIndex, edge, reversed);
        return startIndex < insertion ? insertion - 1 : insertion;
    }

    /// <summary>
    /// Moves the item at <paramref name="startIndex"/> of <paramref name="list"/> to <paramref name="finishIndex"/>.
    /// Answers false, changing nothing, when the indexes are out of range or the same.
    /// </summary>
    public static bool Reorder<T>(IList<T> list, int startIndex, int finishIndex)
    {
        ArgumentNullException.ThrowIfNull(list);
        if (startIndex == finishIndex || startIndex < 0 || startIndex >= list.Count || finishIndex < 0 || finishIndex >= list.Count)
            return false;
        var item = list[startIndex];
        list.RemoveAt(startIndex);
        list.Insert(finishIndex, item);
        return true;
    }

    /// <summary>
    /// Moves the item at <paramref name="sourceIndex"/> of <paramref name="source"/> to
    /// <paramref name="insertionIndex"/> of <paramref name="destination"/> - an index in the destination as it is
    /// before the move (what <see cref="InsertionIndex"/> answers), whether or not the two are the same list. Answers
    /// false, changing nothing, when the indexes are out of range or the item would stay where it is.
    /// </summary>
    public static bool Move<T>(IList<T> source, int sourceIndex, IList<T> destination, int insertionIndex)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(destination);
        if (sourceIndex < 0 || sourceIndex >= source.Count || insertionIndex < 0 || insertionIndex > destination.Count) return false;
        if (ReferenceEquals(source, destination))
            return Reorder(source, sourceIndex, sourceIndex < insertionIndex ? insertionIndex - 1 : insertionIndex);
        var item = source[sourceIndex];
        source.RemoveAt(sourceIndex);
        destination.Insert(insertionIndex, item);
        return true;
    }
}
