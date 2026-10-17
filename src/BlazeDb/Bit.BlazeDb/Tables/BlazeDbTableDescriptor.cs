namespace Bit.BlazeDb;

/// <summary>
/// Static metadata for a table: name, key extraction, binary serialization and index
/// definitions. Descriptors are immutable and shared; typically produced by the BlazeDb
/// source generator for <c>[BlazeDbTable]</c>-annotated types, but they can be hand-written.
/// </summary>
public abstract class BlazeDbTableDescriptor
{
    protected BlazeDbTableDescriptor(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Table name must be non-empty.", nameof(name));
        }
        Name = name;
    }

    public string Name { get; }

    /// <summary>
    /// The row property the key selector reads, when known - the source generator always records it.
    /// Query layers use it to send <c>row.Id == value</c> to the primary-key dictionary; null means a
    /// hand-written descriptor did not say, and such predicates are filtered instead.
    /// </summary>
    public string? KeyMember { get; protected init; }

    internal abstract IBlazeDbTableInternal CreateTable(BlazeDbDatabase database);
}
