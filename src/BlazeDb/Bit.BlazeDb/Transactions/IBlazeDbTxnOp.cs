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
}
