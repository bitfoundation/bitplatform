namespace Bit.BlazeDb;

/// <summary>
/// Ordered secondary index: keeps rows sorted by the indexed value, enabling O(log n) range
/// scans in both directions. Rows whose indexed value is null are recorded too: walking the index
/// in order returns them first (where <c>OrderBy</c> puts them), while a bounded range excludes
/// them, since null lies inside no interval.
/// </summary>
public sealed class BlazeDbOrderedIndexDefinition<TRow, TIndexKey> : BlazeDbIndexDefinition<TRow>
{
    public BlazeDbOrderedIndexDefinition(
        string name,
        Func<TRow, TIndexKey> selector,
        IComparer<TIndexKey>? comparer = null,
        bool unique = false,
        IReadOnlyList<string>? members = null)
        : base(name, unique, members)
    {
        Selector = selector;
        Comparer = comparer ?? DefaultComparer();
    }

    public Func<TRow, TIndexKey> Selector { get; }

    public IComparer<TIndexKey> Comparer { get; }

    internal override bool HasDefaultComparer => Equals(Comparer, DefaultComparer());

    /// <summary>
    /// <see cref="Comparer{T}.Default"/>, except for strings, which it compares by the current
    /// culture. An index has to answer the same question on every machine that opens the database,
    /// and the culture is neither fixed nor even constant within one process - changing it would
    /// reorder values already in the tree, stranding the entries a later write has to find. Ordinal
    /// also matches how the hash index and the primary-key dictionary compare strings, so the two
    /// index kinds agree on what counts as the same value. Pass a comparer explicitly
    /// (<see cref="StringComparer.OrdinalIgnoreCase"/>, a culture-aware one) to choose otherwise.
    /// </summary>
    private static IComparer<TIndexKey> DefaultComparer() =>
        typeof(TIndexKey) == typeof(string)
            ? (IComparer<TIndexKey>)(object)StringComparer.Ordinal
            : Comparer<TIndexKey>.Default;

    internal override IBlazeDbIndexStore<TRow> CreateStore() => new BlazeDbOrderedIndexStore<TRow, TIndexKey>(this);
}
