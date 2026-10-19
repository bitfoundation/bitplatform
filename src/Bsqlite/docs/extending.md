# Extending

## SQL functions from JavaScript

User-defined functions must live where the engine lives, which is the worker. An extension module is an ES module exporting `register`:

```js
// wwwroot/my-functions.js
export function register({ module, sqlite3, db, databaseName }) {
  sqlite3.create_function(db, 'slugify', 1, 1 /* SQLITE_UTF8 */, 0, (ctx, values) => {
    const text = sqlite3.value_text(values[0]);
    sqlite3.result(ctx, text.toLowerCase().replace(/[^a-z0-9]+/g, '-'));
  });
}
```

```csharp
builder.Services.AddBitBsqliteServices(sqlite => sqlite
    // ...
    .AddExtensionModule("/my-functions.js"));
```

```csharp
modelBuilder.HasDbFunction(() => Sql.Slugify(default!)).HasName("slugify");
```

The module receives the Emscripten `module`, the wa-sqlite `sqlite3` API, the open `db` handle, and the database name - the same handles the built-in `ef_*` set uses (`bit-bsqlite-functions.js` is a worked example, including aggregates and a collation). A relative URL is resolved against the document, as every other Blazor asset is, so `./my-functions.js` and `./_content/MyPackage/functions.js` work under a sub-path too.

Functions cannot call back into .NET: the worker has no access to the Blazor runtime, and a synchronous SQL callback could not await it if it did.

## Open-time statements

```csharp
.OnOpen("PRAGMA cache_size = -16000")
.OnOpen("PRAGMA temp_store = MEMORY")
```

These run after the provider's own statements and after `PRAGMA foreign_keys`, outside the SQL guards. They cannot enable WAL.

## A custom transport

`IBsqliteTransport` is the seam between the ADO.NET layer and wherever the engine runs. `Bit.Bsqlite.Testing.BsqliteInProcessTransport` runs Microsoft.Data.Sqlite in-process for desktop tests; `BsqliteWorkerTransport` drives the worker. A transport that can also:

- report committed writes - raise `TablesChanged` once a write commits, for its own writes and other writers' alike, and return `true` from `ReportsLocalWrites`; otherwise leave both at their defaults and the connection infers tables from the SQL;
- snapshot an open database - implement `IBsqliteSnapshotSource`.

Register it through `IBsqliteTransportFactory` to replace the worker for every session, which is how the EF test suite runs the whole composition on desktop .NET.

## A custom storage backend

See [Storage](storage.md#writing-a-provider).
