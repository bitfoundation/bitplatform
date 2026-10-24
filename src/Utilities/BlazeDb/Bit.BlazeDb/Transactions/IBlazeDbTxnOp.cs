using Bit.BlazeDb.Serialization;

namespace Bit.BlazeDb;

/// <summary>
/// A single buffered write operation: applied to memory immediately, revertible on rollback,
/// and encodable into a WAL commit record.
/// </summary>
internal interface IBlazeDbTxnOp
{
    void Apply();

    void Revert();

    void EncodeRedo(BlazeDbBufferWriter payload, BlazeDbBufferWriter scratch);

    /// <summary>Whether the op's table has a <c>Changed</c> subscriber.</summary>
    bool IsObserved { get; }

    /// <summary>Whether the op writes the engine's own table, which database-wide notifications leave out.</summary>
    bool IsEngineTable { get; }

    /// <summary>Adds what the op did to the commit's changes, once it has committed.</summary>
    void CollectChange(BlazeDbChangeCollector collector);
}
