namespace Bit.BlazeDb;

/// <summary>
/// <see cref="BlazeDbRowChange{TKey, TRow}"/> for a subscriber to every table at once
/// (<see cref="BlazeDbDatabase.Changed"/>), with the key and rows as objects and the table named.
/// </summary>
public sealed record BlazeDbRowChange(string Table, BlazeDbChangeKind Kind, object Key, object? OldRow, object? NewRow);
