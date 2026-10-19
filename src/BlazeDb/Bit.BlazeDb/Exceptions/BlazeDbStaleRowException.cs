namespace Bit.BlazeDb;

/// <summary>
/// Thrown when a write states what a row used to hold but the table's indexes describe something
/// else, so the entries it would retract are not the entries that exist.
/// <para>
/// It means the row changed after the caller took its earlier values. The usual cause is two EF Core
/// contexts sharing one store: queries hand back the row itself, so both track the same object while
/// each keeps its own snapshot of the originals, and the second save describes a state the first one
/// has already moved past. Applying it would leave the row indexed under a value it no longer has and
/// listed twice under the value it does, so the write is refused instead.
/// </para>
/// </summary>
public sealed class BlazeDbStaleRowException : BlazeDbException
{
    public BlazeDbStaleRowException(string tableName, string indexName)
        : base($"The values given as the earlier state of a row in table '{tableName}' do not match what " +
               $"index '{indexName}' holds for it, so that entry cannot be retracted. The row was changed " +
               "after those values were taken - another context or caller has written it since. Re-read the " +
               "row and apply the change again.")
    {
        TableName = tableName;
        IndexName = indexName;
    }

    public string TableName { get; }

    public string IndexName { get; }
}
