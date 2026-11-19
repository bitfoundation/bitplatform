namespace Bit.Bcrdt;

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
