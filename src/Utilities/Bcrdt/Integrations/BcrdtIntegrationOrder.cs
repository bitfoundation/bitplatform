namespace Bit.Bcrdt.Integrations;

/// <summary>
/// Order for objects kept in a collaborative map: each entry carries a <see cref="BcrdtOrderKey"/>, and the objects
/// are listed in the order of that key, ties broken by identifier.
/// </summary>
/// <remarks>
/// <para>
/// An ordered collection of objects could be a <see cref="BcrdtArray"/>, but moving an element of an array is a
/// delete and an insert of a copy, so a concurrent edit of the moved object lands on the deleted copy and is lost,
/// and two people moving the same object end up with two of it. Keeping the objects in a map by identifier and the
/// order in a field of each makes a move one field write: concurrent moves of different objects never conflict, and
/// two moves of the same object resolve like any other field - one wins, on every replica.
/// </para>
/// <para>
/// The keys are the fractional order keys <see cref="BcrdtOrderedCollection"/> uses, so there is always room for
/// another key between two different ones and nothing is ever renumbered. Two people inserting at the same place
/// concurrently mostly get different keys - a single insertion takes a random point of the gap - and when they do
/// tie they are ordered by identifier, the same way everywhere, until the next write near them gives one of them a
/// key of its own. An entry without a valid key - written by something else, or by a misbehaving peer - sorts after
/// every entry with one, and gets one the next time its list is written.
/// </para>
/// </remarks>
internal static class BcrdtIntegrationOrder
{
    /// <summary>The key each entry keeps its position in.</summary>
    public const string Key = "$order";

    /// <summary>Reads an entry's position, or null when it has none a replica can use.</summary>
    public static string? Read(BcrdtMap? map) => map?.Get(Key) is string key && BcrdtOrderKey.IsValid(key) ? key : null;

    /// <summary>
    /// Compares two entries the way every replica sorts them: by position, then by identifier. An entry without a
    /// position sorts after every entry with one.
    /// </summary>
    public static int Compare(string? leftOrder, string leftId, string? rightOrder, string rightId)
    {
        int byOrder = CompareOrder(leftOrder, rightOrder);
        return byOrder != 0 ? byOrder : string.CompareOrdinal(leftId, rightId);
    }

    /// <summary>Compares two positions alone; a missing one sorts last.</summary>
    private static int CompareOrder(string? left, string? right)
    {
        if ((left is null) != (right is null)) return left is null ? 1 : -1;
        return string.CompareOrdinal(left, right);
    }

    /// <summary>
    /// Works out which entries need a new position so that sorting reproduces <paramref name="ids"/> in the order
    /// given, changing as few as it can.
    /// </summary>
    /// <param name="ids">The identifiers in the order the local model has them.</param>
    /// <param name="orders">Each one's current position, or null when it has none yet.</param>
    /// <returns>The index into <paramref name="ids"/> and the new position of each entry that has to move.</returns>
    /// <remarks>
    /// The entries whose keys already increase in the order given - the longest such run - keep their positions, and
    /// only the others get keys between them. Bringing one object to the front therefore writes one position, not
    /// one per object above it. Of entries that tie on a key only one can be kept, so the others get keys of their
    /// own, and every gap the rest are placed in is a real one.
    /// </remarks>
    public static List<(int Index, string Order)> Assign(IReadOnlyList<string> ids, IReadOnlyList<string?> orders)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(orders.Count, ids.Count);

        int count = ids.Count;
        bool[] keep = LongestIncreasingRun(count, orders);

        var result = new List<(int Index, string Order)>();
        int previous = -1;
        for (int index = 0; index <= count; index++)
        {
            if (index < count && !keep[index]) continue;

            int start = previous + 1;
            int gap = index - start;
            if (gap > 0)
            {
                string? low = previous >= 0 ? orders[previous] : null;
                string? high = index < count ? orders[index] : null;

                // A single move or insertion takes a random point of its gap, so two replicas doing the same at
                // once rarely tie; a run of them is spread evenly, which keeps the keys short.
                if (gap == 1) result.Add((start, BcrdtOrderKey.Between(low, high, Random.Shared)));
                else
                {
                    IReadOnlyList<string> values = BcrdtOrderKey.BetweenMany(low, high, gap);
                    for (int k = 0; k < gap; k++) result.Add((start + k, values[k]));
                }
            }
            previous = index;
        }
        return result;
    }

    /// <summary>
    /// Marks the longest run of entries whose positions strictly increase in the order given, by patience sorting.
    /// </summary>
    private static bool[] LongestIncreasingRun(int count, IReadOnlyList<string?> orders)
    {
        var keep = new bool[count];
        var tails = new List<int>();
        var previous = new int[count];

        for (int index = 0; index < count; index++)
        {
            if (orders[index] is not { } order) continue;

            int low = 0, high = tails.Count;
            while (low < high)
            {
                int middle = (low + high) / 2;
                if (string.CompareOrdinal(orders[tails[middle]], order) < 0) low = middle + 1;
                else high = middle;
            }

            previous[index] = low > 0 ? tails[low - 1] : -1;
            if (low == tails.Count) tails.Add(index);
            else tails[low] = index;
        }

        for (int index = tails.Count > 0 ? tails[^1] : -1; index >= 0; index = previous[index]) keep[index] = true;
        return keep;
    }

    /// <summary>
    /// Rearranges a sequence so the entries the shared state knows about are in shared order, leaving the others -
    /// local changes not yet written - where they are.
    /// </summary>
    /// <typeparam name="T">The item type.</typeparam>
    /// <param name="items">The local sequence.</param>
    /// <param name="key">The shared position and identifier of an item, or null for one the shared state does not order.</param>
    /// <returns>The sequence in its new order.</returns>
    public static List<T> Arrange<T>(IReadOnlyList<T> items, Func<T, (string? Order, string Id)?> key)
    {
        var slots = new List<int>();
        var known = new List<(T Item, string? Order, string Id)>();
        for (int index = 0; index < items.Count; index++)
        {
            if (key(items[index]) is not { } position) continue;
            slots.Add(index);
            known.Add((items[index], position.Order, position.Id));
        }

        known.Sort(static (left, right) => Compare(left.Order, left.Id, right.Order, right.Id));

        var result = new List<T>(items);
        for (int k = 0; k < slots.Count; k++) result[slots[k]] = known[k].Item;
        return result;
    }
}
