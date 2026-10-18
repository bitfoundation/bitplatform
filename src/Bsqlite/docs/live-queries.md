# Live queries

A live query re-executes when any table it reads is written, by this tab or another one.

## With EF Core

```csharp
await using var live = db.Orders
    .Include(o => o.Customer)
    .Where(o => o.Status == OrderStatus.Open)
    .AsLiveQuery();

live.Changed += (_, rows) => InvokeAsync(StateHasChanged);
live.RefreshFailed += (_, ex) => logger.LogWarning(ex, "Live query failed");
var rows = await live.RefreshAsync();   // first run; later runs are automatic
```

`AsLiveQuery()` resolves the connection from the query's context and the tables from `ToQueryString()`. Pass a `BsqliteConnection` explicitly when the query's provider does not expose its context.

## With ADO.NET

```csharp
await using var live = new BsqliteLiveQuery<long>(
    connection,
    async ct => Convert.ToInt64(await countCommand.ExecuteScalarAsync(ct)),
    tables: ["product"]);
```

An empty table list means every write triggers a refresh.

## Async stream

```csharp
await foreach (var rows in live.WithCancellation(cancellationToken))
{
    Render(rows);
}
```

## How changes are detected

The worker installs `sqlite3_update_hook` and `sqlite3_commit_hook` on every connection. The update hook names the tables whose rows were inserted, updated, or deleted - including through triggers and `ON DELETE CASCADE` - and the statement text adds the two kinds of write the hook cannot see: DDL, and a `DELETE` without `WHERE`, which SQLite runs through its truncate optimisation. A request that changed no rows and is not DDL adds nothing, so a read or a no-op write never refreshes anything.

Nothing is reported while a transaction is open. Once the connection is back in autocommit, the accumulated tables go out if a commit happened and are dropped if it rolled back - a live query never shows data that may yet disappear, and a multi-statement `SaveChangesAsync` refreshes once rather than per statement.

The notification goes to this tab's .NET transport, which raises `TablesChanged`, and over a `BroadcastChannel` named `bit-bsqlite-changes` to every other tab, filtered there to the same database name.

A custom transport that cannot report its own writes leaves `ReportsLocalWrites` false, and the connection falls back to a regular-expression scan of the SQL it sends, holding the tables until an explicit `BsqliteTransaction` commits. That scan is deliberately coarse: it can produce extra refreshes, never a missed one.

## Coalescing

A burst of writes during a refresh produces exactly one further refresh after it, not one per write. Each `Changed` event therefore carries the latest state, and the engine is never asked to re-run a query it is about to re-run again.

## Lifetime

Dispose a live query when the component goes away, and before the session closes (`BsqliteDatabase.Closing` is the signal). A disposed query unsubscribes from the connection and cancels pending stream waiters.
