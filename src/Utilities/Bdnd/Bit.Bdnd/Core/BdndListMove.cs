namespace Bit.Bdnd;

/// <summary>
/// A move of one or more items of a list to an insertion index of a list - another, or the same one - worked out
/// before it is made (<see cref="BdndList.PlanMove{T}(IList{T}, IEnumerable{int}, IList{T}, int)"/>): where every item
/// comes from and where they all end up, so a drop handler can check it, show it, make it with <see cref="Apply"/> and
/// take it back with <see cref="Revert"/> - an undo history keeps the moves it made.
/// </summary>
/// <remarks>
/// The items moved keep their order and land next to each other, the first at <see cref="FinishIndex"/>. A plan holds
/// on to the two lists: it is made against them as they are, and <see cref="Apply"/> and <see cref="Revert"/> change
/// nothing - answering false - when the items are no longer where the plan expects them, as when the lists changed in
/// between in a way that undoing the later changes first does not put back. An item is where the plan expects it when
/// the very instance it moved is there (for a reference type; for a value type, an equal value), so an equal item put
/// in its place - a duplicate, or a copy of a record - does not pass for it. Both lists must take inserts and removals:
/// <see cref="Apply"/> and <see cref="Revert"/> refuse a read-only or fixed-size list (an array) before they change
/// anything.
/// </remarks>
/// <typeparam name="T">The type of the items of the lists.</typeparam>
public sealed class BdndListMove<T>
{
    private readonly int[] _sourceIndexes;
    private readonly T[] _items;

    internal BdndListMove(IList<T> source, int[] sourceIndexes, IList<T> destination, int insertionIndex, int finishIndex)
    {
        Source = source;
        _sourceIndexes = sourceIndexes;
        _items = [.. sourceIndexes.Select(i => source[i])];
        Destination = destination;
        InsertionIndex = insertionIndex;
        FinishIndex = finishIndex;
    }

    /// <summary>The list the items come from.</summary>
    public IList<T> Source { get; }

    /// <summary>The list the items go to: <see cref="Source"/> itself for a reorder.</summary>
    public IList<T> Destination { get; }

    /// <summary>Whether the items stay in their list, which is then only reordered.</summary>
    public bool IsReorder => ReferenceEquals(Source, Destination);

    /// <summary>The indexes the items have in <see cref="Source"/> before the move, in ascending order.</summary>
    public IReadOnlyList<int> SourceIndexes => _sourceIndexes;

    /// <summary>The items moved, in the order they have in <see cref="Source"/> - and will have in <see cref="Destination"/>.</summary>
    public IReadOnlyList<T> Items => _items;

    /// <summary>The insertion index the plan was made for: an index in <see cref="Destination"/> as it is before the move.</summary>
    public int InsertionIndex { get; }

    /// <summary>
    /// The index the first item has in <see cref="Destination"/> after the move; the others follow it. In a reorder it
    /// is less than <see cref="InsertionIndex"/> by the number of items that came from before it.
    /// </summary>
    public int FinishIndex { get; }

    /// <summary>Whether the move has been made: <see cref="Apply"/> succeeded and no <see cref="Revert"/> followed.</summary>
    public bool IsApplied { get; private set; }

    /// <summary>
    /// Makes the move: takes the items out of <see cref="Source"/> and inserts them at <see cref="FinishIndex"/> of
    /// <see cref="Destination"/>. Answers false, changing nothing, when the move has been made already or the items are
    /// no longer at <see cref="SourceIndexes"/>. A move taken back with <see cref="Revert"/> can be made again (redo).
    /// </summary>
    /// <exception cref="NotSupportedException">One of the lists is read-only or of a fixed size; neither is changed.</exception>
    public bool Apply()
    {
        EnsureChangeable();
        if (IsApplied || Holds(Source, _sourceIndexes) is false) return false;
        // checked before anything changes: a list half moved is worse than a move refused
        if (FinishIndex > Destination.Count - (IsReorder ? _items.Length : 0)) return false;
        if (Source is List<T> source) RemoveAt(source, _sourceIndexes);
        else for (var i = _sourceIndexes.Length - 1; i >= 0; i--) Source.RemoveAt(_sourceIndexes[i]);
        if (Destination is List<T> destination) destination.InsertRange(FinishIndex, _items);
        else for (var i = 0; i < _items.Length; i++) Destination.Insert(FinishIndex + i, _items[i]);
        IsApplied = true;
        return true;
    }

    /// <summary>
    /// Takes the move back: the items leave <see cref="Destination"/> and return to <see cref="SourceIndexes"/> of
    /// <see cref="Source"/>, which is then as it was before <see cref="Apply"/>. Answers false, changing nothing, when
    /// the move has not been made or the items are no longer where it put them.
    /// </summary>
    /// <exception cref="NotSupportedException">One of the lists is read-only or of a fixed size; neither is changed.</exception>
    public bool Revert()
    {
        EnsureChangeable();
        if (IsApplied is false || Holds(Destination, FinishIndex) is false) return false;
        // each item goes back to its index of the source as it fills up again
        var count = Source.Count - (IsReorder ? _items.Length : 0);
        for (var i = 0; i < _sourceIndexes.Length; i++)
        {
            if (_sourceIndexes[i] > count + i) return false;
        }
        if (Destination is List<T> destination) destination.RemoveRange(FinishIndex, _items.Length);
        else for (var i = 0; i < _items.Length; i++) Destination.RemoveAt(FinishIndex);
        if (Source is List<T> source) InsertAt(source, _sourceIndexes, _items);
        else for (var i = 0; i < _items.Length; i++) Source.Insert(_sourceIndexes[i], _items[i]);
        IsApplied = false;
        return true;
    }

    // checked before anything changes: a list half moved is worse than a move refused - the items taken out of one
    // list and then refused by the other would be in neither
    private void EnsureChangeable()
    {
        if (Changeable(Source) is false || Changeable(Destination) is false)
            throw new NotSupportedException("A move needs lists that take inserts and removals; one of them is read-only or of a fixed size (an array).");
    }

    private static bool Changeable(IList<T> list) => list.IsReadOnly is false && list is not System.Collections.IList { IsFixedSize: true };

    // the very item the plan moved: the same instance, not an equal one (a duplicate elsewhere in the list, a copy of a
    // record); a value type has no instance, and an equal value is the item
    private static bool Same(T a, T b) => typeof(T).IsValueType ? EqualityComparer<T>.Default.Equals(a, b) : ReferenceEquals(a, b);

    // A List<T> takes a selection out, or puts it back, in one pass over the elements after the first index: removing
    // or inserting the items one at a time shifts the rest of the list once per item.

    // takes out the elements at these ascending indexes, closing up the rest
    private static void RemoveAt(List<T> list, int[] indexes)
    {
        var write = indexes[0];
        for (int read = indexes[0], next = 0; read < list.Count; read++)
        {
            if (next < indexes.Length && read == indexes[next])
            {
                next++;
                continue;
            }
            list[write++] = list[read];
        }
        list.RemoveRange(write, list.Count - write);
    }

    // puts the items at these ascending indexes of the list they leave once inserted, moving the rest up
    private static void InsertAt(List<T> list, int[] indexes, T[] items)
    {
        var read = list.Count - 1;
        list.AddRange(items); // room for them at the end
        for (int write = list.Count - 1, next = indexes.Length - 1; next >= 0; write--)
        {
            if (write == indexes[next]) list[write] = items[next--];
            else list[write] = list[read--];
        }
    }

    // whether the items sit at these indexes of the list
    private bool Holds(IList<T> list, int[] indexes)
    {
        for (var i = 0; i < indexes.Length; i++)
        {
            if (indexes[i] >= list.Count || Same(list[indexes[i]], _items[i]) is false) return false;
        }
        return true;
    }

    // whether the items sit next to each other from this index of the list
    private bool Holds(IList<T> list, int start)
    {
        if (start + _items.Length > list.Count) return false;
        for (var i = 0; i < _items.Length; i++)
        {
            if (Same(list[start + i], _items[i]) is false) return false;
        }
        return true;
    }
}
