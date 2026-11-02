namespace Bit.BlazeDb.Samples.Core.Data;

/// <summary>
/// The samples' one table. The source generator turns it into <c>TodoItem.Table</c>, the descriptor a database
/// registers, and <c>TodoItem.Indexes</c>, the typed indexes every read in the pages goes through. It is a record,
/// so a change is written as a new row (<c>todo with { Done = true }</c>): a read hands out the instance the table
/// holds, and changing that instance in place would leave its index entries behind.
/// </summary>
[BlazeDbTable("todos")]
[BlazeDbCompoundIndex("CategoryDone", nameof(Category), nameof(Done))]
public partial record TodoItem
{
    // The field numbers are pinned: the binary format knows a property by its number, so one added later
    // cannot shift the numbers of those already on disk.

    [BlazeDbKey, BlazeDbField(1)] public Guid Id { get; init; }

    /// <summary>Ordered (ordinally), so the queries page can look titles up by prefix.</summary>
    [BlazeDbOrderedIndex, BlazeDbField(2)] public string Title { get; init; } = "";

    [BlazeDbIndex, BlazeDbField(3)] public string Category { get; init; } = "";

    [BlazeDbIndex, BlazeDbField(4)] public bool Done { get; init; }

    [BlazeDbOrderedIndex, BlazeDbField(5)] public DateTime CreatedAt { get; init; }
}
