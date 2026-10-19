using Bit.BlazeDb.EntityFrameworkCore.Query;
using Bit.BlazeDb.Querying;

namespace Bit.BlazeDb.EntityFrameworkCore.Metadata;

internal sealed class BlazeDbTableBinding<TKey, TRow> : IBlazeDbTableBinding
    where TKey : notnull
    where TRow : class
{
    private readonly BlazeDbTable<TKey, TRow> _table;

    public BlazeDbTableBinding(BlazeDbTable<TKey, TRow> table, string? keyMember)
    {
        _table = table;
        KeyMember = keyMember;
        Indexes = table.Descriptor.Indexes
            .Select(i => new BlazeDbIndex(
                i.Name,
                i.Members,
                Ordered: i.GetType().GetGenericTypeDefinition() == typeof(BlazeDbOrderedIndexDefinition<,>),
                KeyType: i.GetType().GetGenericArguments()[1],
                Definition: i,
                DefaultComparer: i.HasDefaultComparer))
            .ToArray();
    }

    public Type RowType => typeof(TRow);

    public string TableName => _table.Name;

    public Type KeyType => typeof(TKey);

    public string? KeyMember { get; }

    public IReadOnlyList<BlazeDbIndex> Indexes { get; }

    public BlazeDbQueryPlan CreatePlan() => new BlazeDbQueryPlan<TRow>();

    public IEnumerable<object> Execute(BlazeDbQueryPlan plan) =>
        ((BlazeDbQueryPlan<TRow>)plan).Build(BlazeDbQuery<TKey, TRow>.From(_table)).Execute();

    public IQueryable AsQueryable(IEnumerable<object> rows) => rows.Cast<TRow>().AsQueryable();

    public void Insert(object row) => _table.Insert((TRow)row);

    public void UpdateInPlace(object row, object previousValues) =>
        _table.UpdateInPlace((TRow)row, (TRow)previousValues);

    public void Delete(object row, object previousValues)
    {
        var key = _table.Descriptor.KeySelector((TRow)row);
        if (!_table.DeleteInPlace(key, (TRow)previousValues, (TRow)row))
        {
            // Nothing to delete means someone else already did: the same "0 rows affected" an
            // update of a vanished row reports, and for the same reason.
            throw new KeyNotFoundException($"Table '{TableName}' has no row with key '{key}'.");
        }
    }
}
