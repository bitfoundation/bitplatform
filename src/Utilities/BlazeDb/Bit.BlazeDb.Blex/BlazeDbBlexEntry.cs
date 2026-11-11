namespace Bit.BlazeDb.Blex;

/// <summary>
/// One persisted Blex store - or any other key Blex writes - as a row of the <c>blex_state</c> table. Register the table
/// with <see cref="BlazeDbBlexExtensions.AddBlexState"/>.
/// </summary>
[BlazeDbTable("blex_state")]
public sealed partial class BlazeDbBlexEntry
{
    /// <summary>The storage key Blex chose: its key prefix and the store's name.</summary>
    [BlazeDbKey, BlazeDbField(1)]
    public string Key { get; init; } = "";

    /// <summary>The serialized (and, with a protector, protected) state, exactly as Blex handed it over.</summary>
    [BlazeDbField(2)]
    public string Value { get; init; } = "";

    /// <summary>When the value was last written, in UTC.</summary>
    [BlazeDbField(3)]
    public DateTime UpdatedAt { get; init; }
}
