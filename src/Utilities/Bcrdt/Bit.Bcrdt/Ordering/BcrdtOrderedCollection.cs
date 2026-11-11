using System.Collections;

namespace Bit.Bcrdt;

/// <summary>One item of a <see cref="BcrdtOrderedCollection"/>, as it sorts.</summary>
/// <param name="Id">The item's key in <see cref="BcrdtOrderedCollection.Items"/>.</param>
/// <param name="Item">The item itself.</param>
/// <param name="OrderKey">
/// The item's order key, or <see langword="null"/> when it has none or a malformed one - such an item
/// sorts after every item with a valid key.
/// </param>
public readonly record struct BcrdtOrderedEntry(string Id, BcrdtMap Item, string? OrderKey);

/// <summary>
/// An ordered collection whose items can be inserted, moved and removed concurrently without ever being
/// duplicated or losing an edit.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="BcrdtArray.Move"/> is delete-then-insert: two people moving one element at the same time
/// end up with two copies of it, and an edit made to the element while it moved lands on the copy the
/// move deleted. A task board cannot live with either. This view keeps the order somewhere else
/// entirely: every item is a nested <see cref="BcrdtMap"/> under its own identifier in
/// <see cref="Items"/>, and its position is a <see cref="BcrdtOrderKey"/> in one of its fields,
/// <see cref="OrderField"/>. Reading the collection sorts the items by that key.
/// </para>
/// <para>
/// So a move is one write of one field of the item - no delete, no insert, no copy. Two concurrent
/// moves of one item are two writes of one map key, which resolve last-writer-wins: the item ends up at
/// one of the two positions, once, on every replica. An edit to any other field of the item, made while
/// it moved, is untouched by the move. Moving a card to another column is the same thing with one more
/// field: pass a <c>group</c> predicate that selects the target column and an <c>update</c> that writes
/// the card's column field, and both writes go out in one transaction.
/// </para>
/// <para>
/// Items sort by order key, then by identifier, both ordinally, so two items given the same key by two
/// replicas inserting into the same gap at once still sort identically everywhere. Inserting between two
/// such tied items rewrites the keys of the smaller side of the tie first - the fewest writes that make
/// room. An item with no key or a malformed one, which only a misbehaving peer or a hand-written field
/// produces, sorts after every valid key, by identifier, and gets a valid key the first time anything is
/// inserted or moved after it. An entry of <see cref="Items"/> that is not a map is not an item and is
/// ignored.
/// </para>
/// <para>
/// While updates are still arriving out of order, a field can briefly read as empty: an update whose
/// earlier changes have not arrived yet removes the value it overwrote at once, but must wait for them
/// to place its own. A card can then sort last for a moment, or belong to no column, until the rest
/// arrives - never twice, and never for good.
/// </para>
/// <para>
/// This is a view over existing types, not a type of its own: it adds nothing to the wire format, and
/// everything else - events, undo, persistence, synchronization - is the underlying map's. Observe
/// <see cref="Items"/> deeply to re-render on change. Every read sorts the items, which costs
/// <c>O(n log n)</c>: comfortable for boards and lists of hundreds or a few thousand items, not for a
/// sequence of characters.
/// </para>
/// </remarks>
public sealed class BcrdtOrderedCollection : IEnumerable<BcrdtOrderedEntry>
{
    /// <summary>The field that holds an item's order key unless another is given.</summary>
    public const string DefaultOrderField = "order";

    private readonly Random _random;

    /// <summary>Creates a view over <paramref name="items"/>.</summary>
    /// <param name="items">
    /// The map that holds the items: identifier to nested map. It must belong to a document before
    /// anything is written through the view.
    /// </param>
    /// <param name="orderField">The field of each item that holds its order key.</param>
    /// <param name="random">
    /// Where the position inside a gap is drawn from; <see cref="Random.Shared"/> by default. Pass a seeded
    /// one to make the keys this replica generates reproducible.
    /// </param>
    public BcrdtOrderedCollection(BcrdtMap items, string orderField = DefaultOrderField, Random? random = null)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentException.ThrowIfNullOrEmpty(orderField);

        Items = items;
        OrderField = orderField;
        _random = random ?? Random.Shared;
    }

    /// <summary>The map that holds the items, keyed by identifier.</summary>
    public BcrdtMap Items { get; }

    /// <summary>The field of each item that holds its order key.</summary>
    public string OrderField { get; }

    /// <summary>The number of items.</summary>
    public int Count
    {
        get
        {
            int count = 0;
            foreach (KeyValuePair<string, object?> entry in Items)
            {
                if (entry.Value is BcrdtMap) count++;
            }
            return count;
        }
    }

    /// <summary>Reads an item by identifier, or <see langword="null"/> if there is none.</summary>
    public BcrdtMap? Get(string id) => Items.Get(id) as BcrdtMap;

    /// <summary>
    /// The identifier of <paramref name="item"/>, or <see langword="null"/> if it is not a current item of
    /// this collection.
    /// </summary>
    /// <remarks>
    /// For code that holds on to an item - the payload of a drag, the map <see cref="Insert"/> returned -
    /// and needs its identifier to <see cref="Move"/> or <see cref="Remove"/> it.
    /// </remarks>
    public string? IdOf(BcrdtMap item)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (item.ContainingItem is not { Deleted: false, ParentKey: { } id } entry || !ReferenceEquals(entry.Parent, Items)) return null;
        return id;
    }

    /// <summary>The items in order, optionally only those of one group.</summary>
    /// <param name="group">
    /// Which items to list - a kanban column, say - or <see langword="null"/> for all of them. The
    /// order within a group is the order of the whole collection with the other items left out.
    /// </param>
    public IReadOnlyList<BcrdtOrderedEntry> GetOrdered(Func<BcrdtMap, bool>? group = null) => Snapshot(group, excludedId: null);

    /// <summary>
    /// The position of an item among the ordered items of <paramref name="group"/>, or -1 if it is not
    /// one of them.
    /// </summary>
    public int IndexOf(string id, Func<BcrdtMap, bool>? group = null)
    {
        ArgumentNullException.ThrowIfNull(id);

        List<BcrdtOrderedEntry> ordered = Snapshot(group, excludedId: null);
        for (int i = 0; i < ordered.Count; i++)
        {
            if (string.Equals(ordered[i].Id, id, StringComparison.Ordinal)) return i;
        }
        return -1;
    }

    /// <summary>Creates an item at <paramref name="index"/>, as a single transaction.</summary>
    /// <param name="index">
    /// Where the item goes among the items of <paramref name="group"/>: it ends up at exactly this
    /// position, and at the end when it equals their count.
    /// </param>
    /// <param name="initialize">
    /// Writes the item's fields - including whatever puts it in <paramref name="group"/>. It runs on the
    /// attached item inside the same transaction, so peers never see the item without them.
    /// </param>
    /// <param name="group">Which items <paramref name="index"/> counts among, or <see langword="null"/> for all.</param>
    /// <param name="id">
    /// The new item's identifier; a new GUID by default. Two replicas creating the same identifier
    /// concurrently create one item, last-writer-wins, so supply one only when it is unique by
    /// construction.
    /// </param>
    /// <returns>The new item.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is outside the group.</exception>
    /// <exception cref="ArgumentException">An entry with <paramref name="id"/> already exists.</exception>
    public BcrdtMap Insert(int index, Action<BcrdtMap>? initialize = null, Func<BcrdtMap, bool>? group = null, string? id = null)
    {
        id ??= Guid.NewGuid().ToString("N");
        if (Items.ContainsKey(id)) throw new ArgumentException($"An entry '{id}' already exists.", nameof(id));

        return Items.Doc.Transact(_ =>
        {
            List<BcrdtOrderedEntry> ordered = Snapshot(group, excludedId: null);
            ArgumentOutOfRangeException.ThrowIfNegative(index);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(index, ordered.Count);

            return Create(ordered, index, initialize, id);
        });
    }

    /// <summary>Creates an item after every item of <paramref name="group"/>, as a single transaction.</summary>
    /// <inheritdoc cref="Insert(int, Action{BcrdtMap}?, Func{BcrdtMap, bool}?, string?)"/>
    public BcrdtMap Add(Action<BcrdtMap>? initialize = null, Func<BcrdtMap, bool>? group = null, string? id = null)
    {
        id ??= Guid.NewGuid().ToString("N");
        if (Items.ContainsKey(id)) throw new ArgumentException($"An entry '{id}' already exists.", nameof(id));

        return Items.Doc.Transact(_ =>
        {
            List<BcrdtOrderedEntry> ordered = Snapshot(group, excludedId: null);
            return Create(ordered, ordered.Count, initialize, id);
        });
    }

    /// <summary>Creates an item at <paramref name="index"/> of <paramref name="ordered"/>. Call inside a transaction.</summary>
    private BcrdtMap Create(List<BcrdtOrderedEntry> ordered, int index, Action<BcrdtMap>? initialize, string id)
    {
        string key = MakeRoom(ordered, index);

        var item = new BcrdtMap();
        item.Set(OrderField, key);
        Items.Set(id, item);
        initialize?.Invoke(item);
        return item;
    }

    /// <summary>Moves an item to <paramref name="index"/>, as a single transaction.</summary>
    /// <param name="id">The item to move.</param>
    /// <param name="index">
    /// Where the item ends up among the items of <paramref name="group"/>, counted without the item
    /// itself: 0 is first, and their count is last.
    /// </param>
    /// <param name="group">
    /// Which items <paramref name="index"/> counts among, or <see langword="null"/> for all. To move an
    /// item to another group, select the target group here and write the item's group field in
    /// <paramref name="update"/>.
    /// </param>
    /// <param name="update">
    /// Writes to the item in the same transaction - typically its group field.
    /// </param>
    /// <remarks>
    /// Only the order key is written - never the item itself - and only when the item does not already
    /// sort at the target position. Two replicas moving one item concurrently each write its key; one
    /// write wins, so the item ends up at one of the two positions, exactly once.
    /// </remarks>
    /// <exception cref="ArgumentException">There is no item <paramref name="id"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is outside the group.</exception>
    public void Move(string id, int index, Func<BcrdtMap, bool>? group = null, Action<BcrdtMap>? update = null)
    {
        ArgumentNullException.ThrowIfNull(id);
        BcrdtMap item = Get(id) ?? throw new ArgumentException($"There is no item '{id}'.", nameof(id));

        Items.Doc.Transact(_ =>
        {
            List<BcrdtOrderedEntry> ordered = Snapshot(group, excludedId: id);
            ArgumentOutOfRangeException.ThrowIfNegative(index);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(index, ordered.Count);

            var current = new BcrdtOrderedEntry(id, item, ReadKey(item));
            bool alreadyThere = (index == 0 || Compare(ordered[index - 1], current) < 0)
                && (index == ordered.Count || Compare(current, ordered[index]) < 0);

            if (!alreadyThere) item.Set(OrderField, MakeRoom(ordered, index));
            update?.Invoke(item);
        });
    }

    /// <summary>Removes an item, as a single transaction.</summary>
    /// <returns>Whether there was an item to remove.</returns>
    /// <remarks>
    /// Removing wins over a concurrent move or edit of the item: those write inside the item, and do
    /// not bring back the entry that held it.
    /// </remarks>
    public bool Remove(string id)
    {
        ArgumentNullException.ThrowIfNull(id);
        if (Get(id) is null) return false;

        Items.Delete(id);
        return true;
    }

    /// <inheritdoc/>
    public IEnumerator<BcrdtOrderedEntry> GetEnumerator() => Snapshot(group: null, excludedId: null).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    /// The key for a new position at <paramref name="index"/> of <paramref name="ordered"/>, after
    /// rewriting whichever neighbours' keys leave no room there.
    /// </summary>
    private string MakeRoom(List<BcrdtOrderedEntry> ordered, int index)
    {
        // Items without a valid key sort last; everything from the first of them on is "unkeyed".
        int firstUnkeyed = ordered.FindIndex(entry => entry.OrderKey is null);
        if (firstUnkeyed < 0) firstUnkeyed = ordered.Count;

        if (index > firstUnkeyed)
        {
            // A valid key sorts before every unkeyed item, so the unkeyed items ahead of the position
            // must get valid keys first, in the order they already have.
            string? lowest = firstUnkeyed > 0 ? ordered[firstUnkeyed - 1].OrderKey : null;
            IReadOnlyList<string> keys = BcrdtOrderKey.BetweenMany(lowest, null, index - firstUnkeyed + 1);
            for (int i = firstUnkeyed; i < index; i++) Rekey(ordered[i], keys[i - firstUnkeyed]);
            return keys[^1];
        }

        string? before = index > 0 ? ordered[index - 1].OrderKey : null;
        string? after = index < firstUnkeyed ? ordered[index].OrderKey : null;

        if (before is null || after is null || string.CompareOrdinal(before, after) < 0)
        {
            return BcrdtOrderKey.Between(before, after, _random);
        }

        // The neighbours tie, so no key fits between them. Find the run of items sharing that key and
        // rewrite whichever side of the position is shorter, inside the gap that side has.
        int runStart = index - 1;
        while (runStart > 0 && string.Equals(ordered[runStart - 1].OrderKey, before, StringComparison.Ordinal)) runStart--;
        int runEnd = index + 1;
        while (runEnd < firstUnkeyed && string.Equals(ordered[runEnd].OrderKey, before, StringComparison.Ordinal)) runEnd++;

        int leftCount = index - runStart;
        int rightCount = runEnd - index;

        if (leftCount <= rightCount)
        {
            string? lowest = runStart > 0 ? ordered[runStart - 1].OrderKey : null;
            IReadOnlyList<string> keys = BcrdtOrderKey.BetweenMany(lowest, before, leftCount + 1);
            for (int i = 0; i < leftCount; i++) Rekey(ordered[runStart + i], keys[i]);
            return keys[^1];
        }
        else
        {
            string? highest = runEnd < firstUnkeyed ? ordered[runEnd].OrderKey : null;
            IReadOnlyList<string> keys = BcrdtOrderKey.BetweenMany(before, highest, rightCount + 1);
            for (int i = 0; i < rightCount; i++) Rekey(ordered[index + i], keys[i + 1]);
            return keys[0];
        }
    }

    private void Rekey(BcrdtOrderedEntry entry, string key) => entry.Item.Set(OrderField, key);

    private List<BcrdtOrderedEntry> Snapshot(Func<BcrdtMap, bool>? group, string? excludedId)
    {
        List<BcrdtOrderedEntry> ordered = [];
        foreach ((string id, object? value) in Items)
        {
            if (value is not BcrdtMap item) continue;
            if (excludedId is not null && string.Equals(id, excludedId, StringComparison.Ordinal)) continue;
            if (group is not null && !group(item)) continue;
            ordered.Add(new BcrdtOrderedEntry(id, item, ReadKey(item)));
        }

        ordered.Sort(Compare);
        return ordered;
    }

    private string? ReadKey(BcrdtMap item) => item.Get(OrderField) is string key && BcrdtOrderKey.IsValid(key) ? key : null;

    /// <summary>By key, then by identifier; an item without a valid key sorts after every item with one.</summary>
    private static int Compare(BcrdtOrderedEntry x, BcrdtOrderedEntry y)
    {
        if ((x.OrderKey is null) != (y.OrderKey is null)) return x.OrderKey is null ? 1 : -1;

        int byKey = string.CompareOrdinal(x.OrderKey, y.OrderKey);
        return byKey != 0 ? byKey : string.CompareOrdinal(x.Id, y.Id);
    }
}
