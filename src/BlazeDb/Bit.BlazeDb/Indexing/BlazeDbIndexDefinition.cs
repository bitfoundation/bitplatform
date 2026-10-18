namespace Bit.BlazeDb;

/// <summary>
/// Definition of a secondary index over rows of type <typeparamref name="TRow"/>.
/// Concrete implementations: <c>BlazeDbHashIndexDefinition</c> (equality lookups) and
/// <c>BlazeDbOrderedIndexDefinition</c> (range scans). Stores are maintained automatically by the
/// owning table on every mutation.
/// </summary>
public abstract class BlazeDbIndexDefinition<TRow>
{
    protected BlazeDbIndexDefinition(string name, bool unique = false, IReadOnlyList<string>? members = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Index name must be non-empty.", nameof(name));
        }
        Name = name;
        IsUnique = unique;
        Members = members is { Count: > 0 } ? members : [name];
    }

    public string Name { get; }

    /// <summary>
    /// Row properties this index covers, in order. Query layers use it to decide whether a
    /// predicate can be served by the index. It defaults to the index name, which is the name the
    /// source generator gives an index over a single property.
    /// </summary>
    public IReadOnlyList<string> Members { get; }

    /// <summary>
    /// When true, no two rows may share an indexed value. Rows whose indexed value is null are
    /// exempt, matching the treatment of nulls everywhere else in the indexing layer. A compound
    /// index is keyed on a value tuple, which is never itself null, so its members being null does
    /// not exempt a row - see the compound index attributes.
    /// </summary>
    public bool IsUnique { get; }

    /// <summary>
    /// Whether the index compares keys the way the type itself does - by default equality and order
    /// (ordinal for strings) - rather than through a comparer of the definer's choosing. A query
    /// layer can only treat a lookup as the answer to <c>==</c> or <c>&lt;</c> when it is.
    /// </summary>
    internal abstract bool HasDefaultComparer { get; }

    internal abstract IBlazeDbIndexStore<TRow> CreateStore();
}
