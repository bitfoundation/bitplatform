namespace Bit.BlazeDb;

/// <summary>
/// Gathers the net change one commit made to each row, for delivery to <c>Changed</c> subscribers once the
/// commit is complete and the write lock released. Only built when somebody is listening: a commit with no
/// subscriber for any table it touched allocates nothing.
/// </summary>
internal sealed class BlazeDbChangeCollector
{
    private readonly List<IBlazeDbTableChanges> _tables = [];
    private readonly Dictionary<object, IBlazeDbTableChanges> _byTable = new(ReferenceEqualityComparer.Instance);

    public BlazeDbChangeCollector(bool isReset, bool reportsDatabase)
    {
        IsReset = isReset;
        ReportsDatabase = reportsDatabase;
    }

    /// <summary>The whole database was replaced: every table is reported reset rather than row by row.</summary>
    public bool IsReset { get; }

    /// <summary>
    /// Whether the database's own <c>Changed</c> had subscribers when the commit was collected, and so every
    /// table's rows were. Only then is that event raised for it: one collected for a few watched tables
    /// alone would hand a database subscriber that arrived since a commit missing the rest of its rows.
    /// </summary>
    public bool ReportsDatabase { get; }

    public BlazeDbTableChanges<TKey, TRow> For<TKey, TRow>(BlazeDbTable<TKey, TRow> table)
        where TKey : notnull
    {
        if (!_byTable.TryGetValue(table, out var changes))
        {
            _byTable[table] = changes = new BlazeDbTableChanges<TKey, TRow>(table);
            _tables.Add(changes);
        }
        return (BlazeDbTableChanges<TKey, TRow>)changes;
    }

    /// <summary>
    /// Raises each table's <c>Changed</c>, in the order the commit first touched them, then the
    /// database's. A handler that throws does not stop the others; its exception is collected.
    /// </summary>
    public void Deliver(BlazeDbDatabase database, IReadOnlyList<IBlazeDbTableInternal> resetTables, ref List<Exception>? errors)
    {
        List<BlazeDbRowChange>? all = ReportsDatabase && database.HasChangedSubscribers ? [] : null;
        if (IsReset)
        {
            foreach (var table in resetTables)
            {
                table.RaiseReset(ref errors);
            }
        }
        else
        {
            foreach (var table in _tables)
            {
                table.Deliver(all, ref errors);
            }
        }
        if (all is null || (!IsReset && all.Count == 0))
        {
            return;
        }
        database.RaiseChanged(new BlazeDbChangedEventArgs(all, IsReset), ref errors);
    }

    /// <summary>
    /// Calls every subscriber of <paramref name="handler"/> in turn, so one that throws cannot keep the
    /// rest from hearing about a commit, as it would through the delegate's own invocation.
    /// </summary>
    public static void Raise<TArgs>(EventHandler<TArgs>? handler, object sender, TArgs args, ref List<Exception>? errors)
    {
        if (handler is null)
        {
            return;
        }
        foreach (var subscriber in handler.GetInvocationList())
        {
            try
            {
                ((EventHandler<TArgs>)subscriber)(sender, args);
            }
            catch (Exception ex)
            {
                (errors ??= []).Add(ex);
            }
        }
    }
}
