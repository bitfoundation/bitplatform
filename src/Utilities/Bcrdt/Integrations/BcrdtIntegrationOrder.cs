namespace Bit.Bcrdt.Integrations;

/// <summary>
/// Stacking order for objects kept in a collaborative map: each entry carries a number, and the objects are drawn in
/// the order of that number, ties broken by identifier.
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
/// The numbers are doubles placed between their neighbours. Two people inserting at the same place concurrently get
/// the same number and are ordered by identifier, the same way everywhere. Repeated insertion at one place halves
/// the gap each time, and when the gap runs out the whole sequence is renumbered - a write to every object in it,
/// which is the price of a representation this simple. A dense, unbounded key (a fractional index over strings) would
/// never need renumbering; this is deliberately the simplest deterministic scheme.
/// </para>
/// </remarks>
internal static class BcrdtIntegrationOrder
{
    /// <summary>The key each entry keeps its position in.</summary>
    public const string Key = "$order";

    /// <summary>Reads an entry's position, or null when it has none a replica can use.</summary>
    public static double? Read(BcrdtMap? map) => map?.Get(Key) switch
    {
        double number when double.IsFinite(number) => number,
        float number when float.IsFinite(number) => number,
        long number => number,
        _ => null,
    };

    /// <summary>
    /// Compares two entries the way every replica sorts them: by position, then by identifier. An entry without a
    /// position sorts after every entry with one.
    /// </summary>
    public static int Compare(double? leftOrder, string leftId, double? rightOrder, string rightId)
    {
        int byOrder = (leftOrder ?? double.PositiveInfinity).CompareTo(rightOrder ?? double.PositiveInfinity);
        return byOrder != 0 ? byOrder : string.CompareOrdinal(leftId, rightId);
    }

    /// <summary>
    /// Works out which entries need a new position so that sorting reproduces <paramref name="ids"/> in the order
    /// given, changing as few as it can.
    /// </summary>
    /// <param name="ids">The identifiers in the order the local model has them.</param>
    /// <param name="orders">Each one's current position, or null when it has none yet.</param>
    /// <returns>The index into <paramref name="ids"/> and the new position of each entry that has to move.</returns>
    /// <remarks>
    /// The entries already in sorted order relative to each other - the longest such run - keep their positions, and
    /// only the others are placed between them. Bringing one object to the front therefore writes one position, not
    /// one per object above it.
    /// </remarks>
    public static List<(int Index, double Order)> Assign(IReadOnlyList<string> ids, IReadOnlyList<double?> orders)
    {
        int count = ids.Count;
        bool[] keep = LongestSortedRun(ids, orders);

        var result = new List<(int Index, double Order)>();
        int previous = -1;
        for (int index = 0; index <= count; index++)
        {
            if (index < count && !keep[index]) continue;

            int start = previous + 1;
            int gap = index - start;
            if (gap > 0)
            {
                double? low = previous >= 0 ? orders[previous] : null;
                double? high = index < count ? orders[index] : null;
                if (!TryFill(low, high, gap, out double[] values)) return Renumber(orders);
                for (int k = 0; k < gap; k++) result.Add((start + k, values[k]));
            }
            previous = index;
        }
        return result;
    }

    /// <summary>Marks the longest run of entries whose positions are already in sorted order.</summary>
    private static bool[] LongestSortedRun(IReadOnlyList<string> ids, IReadOnlyList<double?> orders)
    {
        int count = ids.Count;
        var keep = new bool[count];
        var tails = new List<int>();
        var previous = new int[count];

        for (int index = 0; index < count; index++)
        {
            if (orders[index] is null) continue;

            int low = 0, high = tails.Count;
            while (low < high)
            {
                int middle = (low + high) / 2;
                if (Compare(orders[tails[middle]], ids[tails[middle]], orders[index], ids[index]) < 0) low = middle + 1;
                else high = middle;
            }

            previous[index] = low > 0 ? tails[low - 1] : -1;
            if (low == tails.Count) tails.Add(index);
            else tails[low] = index;
        }

        for (int index = tails.Count > 0 ? tails[^1] : -1; index >= 0; index = previous[index]) keep[index] = true;
        return keep;
    }

    /// <summary>Spreads <paramref name="count"/> positions strictly between two neighbours, either of which may be missing.</summary>
    private static bool TryFill(double? low, double? high, int count, out double[] values)
    {
        values = new double[count];
        for (int k = 0; k < count; k++)
        {
            values[k] = (low, high) switch
            {
                (null, null) => k + 1,
                (null, { } h) => h - count + k,
                ({ } l, null) => l + k + 1,
                ({ } l, { } h) => l + ((h - l) * (k + 1) / (count + 1)),
            };
        }

        // Halving a gap often enough exhausts a double's precision, and the values collapse onto their neighbours.
        double floor = low ?? double.NegativeInfinity;
        for (int k = 0; k < count; k++)
        {
            if (!double.IsFinite(values[k]) || values[k] <= floor) return false;
            floor = values[k];
        }
        return high is null || floor < high.Value;
    }

    private static List<(int Index, double Order)> Renumber(IReadOnlyList<double?> orders)
    {
        var result = new List<(int Index, double Order)>();
        for (int index = 0; index < orders.Count; index++)
        {
            if (orders[index] != index + 1) result.Add((index, index + 1));
        }
        return result;
    }

    /// <summary>
    /// Rearranges a sequence so the entries the shared state knows about are in shared order, leaving the others -
    /// local changes not yet written - where they are.
    /// </summary>
    /// <typeparam name="T">The item type.</typeparam>
    /// <param name="items">The local sequence.</param>
    /// <param name="key">The shared position and identifier of an item, or null for one the shared state does not order.</param>
    /// <returns>The sequence in its new order.</returns>
    public static List<T> Arrange<T>(IReadOnlyList<T> items, Func<T, (double? Order, string Id)?> key)
    {
        var slots = new List<int>();
        var known = new List<(T Item, double? Order, string Id)>();
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
