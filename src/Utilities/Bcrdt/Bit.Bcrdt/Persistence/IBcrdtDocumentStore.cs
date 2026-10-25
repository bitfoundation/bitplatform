namespace Bit.Bcrdt;

/// <summary>What a store holds for one document.</summary>
/// <param name="Update">
/// Every persisted change, merged into a single update, or <see langword="null"/> if the document
/// has never been stored.
/// </param>
/// <param name="RecordCount">How many separate records the store holds, which drives compaction.</param>
public readonly record struct BcrdtStoredDocument(byte[]? Update, int RecordCount)
{
    /// <summary>Whether the store has nothing for this document.</summary>
    public bool IsEmpty => Update is null;
}

/// <summary>
/// Where documents are kept between sessions.
/// </summary>
/// <remarks>
/// <para>
/// The contract is append-only with periodic compaction, not read-modify-write. That choice matters
/// for more than performance: appending is atomic enough to be crash-safe on ordinary file systems
/// and cheap enough to do on every keystroke, whereas rewriting a whole document on every change
/// would be neither.
/// </para>
/// <para>
/// An implementation needs to understand nothing about CRDTs. Updates concatenate, so compaction is
/// just "merge these bytes", and correctness does not depend on the store preserving order,
/// deduplicating, or noticing anything about what it is storing. A store that loses the last record
/// after a crash is still correct - the peer will resend it.
/// </para>
/// </remarks>
public interface IBcrdtDocumentStore
{
    /// <summary>Reads everything stored for a document.</summary>
    ValueTask<BcrdtStoredDocument> LoadAsync(string documentId, CancellationToken cancellationToken = default);

    /// <summary>Appends one update. This is the hot path and should stay O(1).</summary>
    ValueTask AppendUpdateAsync(
        string documentId,
        ReadOnlyMemory<byte> update,
        CancellationToken cancellationToken = default);

    /// <summary>Collapses the stored records into one, losslessly.</summary>
    ValueTask CompactAsync(string documentId, CancellationToken cancellationToken = default);

    /// <summary>Removes a document entirely.</summary>
    ValueTask DeleteAsync(string documentId, CancellationToken cancellationToken = default);

    /// <summary>Lists the documents the store holds.</summary>
    ValueTask<IReadOnlyList<string>> ListAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// A store that needs to know which documents are attached to a <see cref="BcrdtDocumentPersistence"/>,
/// so it never forgets one that is still being written to.
/// </summary>
/// <remarks>
/// A document attached is held in memory by whoever attached it, and its later records are changes on
/// top of what the store holds: a store that dropped its log meanwhile would be left with changes
/// missing what they build on. Calls are paired; a document may be attached more than once at a time.
/// </remarks>
internal interface IBcrdtAttachedDocumentTracker
{
    /// <summary>Called before the document is first loaded through the binding.</summary>
    void OnAttached(string documentId);

    /// <summary>Called once the binding has stopped writing.</summary>
    void OnDetached(string documentId);
}
