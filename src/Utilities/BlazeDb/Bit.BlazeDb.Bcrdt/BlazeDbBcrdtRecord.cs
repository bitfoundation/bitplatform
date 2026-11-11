namespace Bit.BlazeDb.Bcrdt;

/// <summary>
/// One stored update of a Bcrdt document, as a row of the <c>bcrdt_records</c> table. Register the table with
/// <see cref="BlazeDbBcrdtExtensions.AddBcrdtDocuments"/>.
/// </summary>
[BlazeDbTable("bcrdt_records")]
[BlazeDbCompoundOrderedIndex("ByDocument", nameof(DocumentId), nameof(Sequence))]
public sealed partial class BlazeDbBcrdtRecord
{
    /// <summary>The record's own identity.</summary>
    [BlazeDbKey, BlazeDbField(1)]
    public Guid Id { get; init; }

    /// <summary>The document the update belongs to.</summary>
    [BlazeDbIndex, BlazeDbField(2)]
    public string DocumentId { get; init; } = "";

    /// <summary>The record's place in its document's log: one more than the record before it.</summary>
    [BlazeDbField(3)]
    public long Sequence { get; init; }

    /// <summary>The update, as Bcrdt encoded it. The store never looks inside.</summary>
    [BlazeDbField(4)]
    public byte[] Update { get; init; } = [];
}
