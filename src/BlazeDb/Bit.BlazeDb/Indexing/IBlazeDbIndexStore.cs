namespace Bit.BlazeDb;

/// <summary>Runtime state of one secondary index inside a table.</summary>
internal interface IBlazeDbIndexStore<TRow>
{
    /// <summary>The definition this store was created from.</summary>
    object Definition { get; }

    void Add(TRow row, long seq);

    void Remove(TRow row, long seq);

    void Clear();

    /// <summary>
    /// Throws <see cref="BlazeDbUniqueConstraintViolationException"/> if storing <paramref name="row"/>
    /// would duplicate an indexed value. <paramref name="replacedSeq"/> is the sequence number of
    /// the row being replaced (0 when inserting), whose entry is about to be removed and so must
    /// not count as a conflict. Called before any mutation is applied.
    /// </summary>
    void ValidateUnique(TRow row, long replacedSeq, string tableName);

    /// <summary>
    /// Whether the entry this store holds for <paramref name="seq"/> still matches the indexed
    /// value <paramref name="row"/> currently carries. False means the row instance was mutated
    /// after it was indexed, so the entry can no longer be found through the row's own values. A
    /// null value is recorded like any other, so a value changed to or from null is caught too.
    /// </summary>
    bool EntryMatches(TRow row, long seq);

    /// <summary>
    /// Equality lookup with the key passed as <see cref="object"/>. For callers that pick an index
    /// at runtime - the EF Core provider translating a LINQ predicate - and so cannot name the key
    /// type. The cast happens inside the already-instantiated generic store, so no reflection or
    /// runtime code generation is involved. A null key finds the rows whose indexed value is null;
    /// the caller has to know that the index's key type can hold a null.
    /// </summary>
    IEnumerable<TRow> LookupBoxed(object? key);

    /// <summary>
    /// Range scan with boxed bounds; see <see cref="LookupBoxed"/>. Only ordered indexes support
    /// it - a hash index throws.
    /// </summary>
    IEnumerable<TRow> RangeBoxed(bool hasFrom, object? from, bool hasTo, object? to, bool descending);
}
