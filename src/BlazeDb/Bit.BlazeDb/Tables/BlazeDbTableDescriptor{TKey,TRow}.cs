namespace Bit.BlazeDb;

public sealed class BlazeDbTableDescriptor<TKey, TRow> : BlazeDbTableDescriptor
    where TKey : notnull
{
    public BlazeDbTableDescriptor(
        string name,
        Func<TRow, TKey> keySelector,
        BlazeDbRowWriter<TRow> rowWriter,
        BlazeDbRowReader<TRow> rowReader,
        BlazeDbKeyWriter<TKey> keyWriter,
        BlazeDbKeyReader<TKey> keyReader,
        IReadOnlyList<BlazeDbIndexDefinition<TRow>>? indexes = null,
        string? keyMember = null)
        : base(name)
    {
        KeySelector = keySelector;
        RowWriter = rowWriter;
        RowReader = rowReader;
        KeyWriter = keyWriter;
        KeyReader = keyReader;
        Indexes = indexes ?? [];
        KeyMember = keyMember;
    }

    public Func<TRow, TKey> KeySelector { get; }
    public BlazeDbRowWriter<TRow> RowWriter { get; }

    public BlazeDbRowReader<TRow> RowReader { get; }

    public BlazeDbKeyWriter<TKey> KeyWriter { get; }

    public BlazeDbKeyReader<TKey> KeyReader { get; }

    public IReadOnlyList<BlazeDbIndexDefinition<TRow>> Indexes { get; }

    internal override IBlazeDbTableInternal CreateTable(BlazeDbDatabase database) => new BlazeDbTable<TKey, TRow>(database, this);
}
