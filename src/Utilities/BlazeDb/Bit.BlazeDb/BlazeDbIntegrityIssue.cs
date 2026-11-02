namespace Bit.BlazeDb;

/// <summary>
/// One disagreement <see cref="BlazeDbDatabase.VerifyIntegrity"/> found between a table's rows and its
/// indexes. Every one so far traces back to a row object that was changed in place after it was
/// written - see "Rows are live objects" in the guide.
/// </summary>
/// <param name="Table">The table the issue is in.</param>
/// <param name="Index">The index that disagrees with the rows, or null when it is the primary key - or when the issue is a write a rollback could not undo.</param>
/// <param name="Description">What was found, naming the row's key.</param>
public sealed record BlazeDbIntegrityIssue(string Table, string? Index, string Description);
