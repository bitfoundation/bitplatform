namespace Bit.Bcrdt.BlazeDb;

/// <summary>One stored update of one document: a row of <see cref="BcrdtBlazeDbDocumentStore.Table"/>.</summary>
/// <remarks>
/// <para>
/// BlazeDb keys a table by one scalar, so the natural key - the document and its position in the log - is
/// split in two: a sequence number unique across the table, which also orders a document's records the way
/// they were written, and a hash index on the document, which finds them without a scan.
/// </para>
/// <para>
/// The field numbers are pinned rather than left to declaration order: these rows outlive the build that
/// wrote them, and an automatic number that moved would make every stored update decode into the wrong
/// property without an error.
/// </para>
/// </remarks>
[BlazeDbTable(BcrdtBlazeDbDocumentStore.TableName)]
internal sealed partial class BcrdtBlazeDbUpdateRecord
{
    /// <summary>The record's position in the table, unique across every document.</summary>
    [BlazeDbKey]
    [BlazeDbField(1)]
    public long Id { get; set; }

    /// <summary>The document the update belongs to, exactly as the application named it.</summary>
    [BlazeDbIndex]
    [BlazeDbField(2)]
    public string DocumentId { get; set; } = "";

    /// <summary>The update, as the persistence layer handed it over.</summary>
    [BlazeDbField(3)]
    public byte[] Update { get; set; } = [];
}
