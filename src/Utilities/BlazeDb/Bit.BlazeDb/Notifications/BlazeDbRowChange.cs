namespace Bit.BlazeDb;

/// <summary>What a committed write did to a row.</summary>
public enum BlazeDbChangeKind
{
    /// <summary>The row did not exist before the commit and does after it.</summary>
    Inserted,

    /// <summary>The row existed before the commit and still does, with new values.</summary>
    Updated,

    /// <summary>The row existed before the commit and does not after it.</summary>
    Deleted,
}

/// <summary>
/// The net effect of one commit on one row of a table: several writes to the same key inside a transaction
/// are reported as the single change from the row before the commit to the row after it, so a subscriber
/// never sees a state that only existed part-way through.
/// </summary>
/// <param name="Kind">Inserted, updated or deleted.</param>
/// <param name="Key">The row's primary key.</param>
/// <param name="OldRow">The row as it was before the commit; default for an insert.</param>
/// <param name="NewRow">The row as it is after the commit - the live object the table holds; default for a delete.</param>
public readonly record struct BlazeDbRowChange<TKey, TRow>(BlazeDbChangeKind Kind, TKey Key, TRow? OldRow, TRow? NewRow);

/// <summary>
/// <see cref="BlazeDbRowChange{TKey, TRow}"/> for a subscriber to every table at once
/// (<see cref="BlazeDbDatabase.Changed"/>), with the key and rows as objects and the table named.
/// </summary>
public sealed record BlazeDbRowChange(string Table, BlazeDbChangeKind Kind, object Key, object? OldRow, object? NewRow);
