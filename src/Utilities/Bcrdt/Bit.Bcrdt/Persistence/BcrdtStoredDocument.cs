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
