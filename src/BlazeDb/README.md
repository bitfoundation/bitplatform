# Bit.BlazeDb

A memory-first, SQL-free embedded database engine for .NET, designed for Blazor WebAssembly running
client-side in the browser.

```csharp
[BlazeDbTable("todos")]
public partial class TodoItem
{
    [BlazeDbKey] public Guid Id { get; set; }
    public string Title { get; set; } = "";
    [BlazeDbIndex] public bool Done { get; set; }
}

await using var db = await BlazeDbDatabase.OpenAsync(new BlazeDbDatabaseOptions
{
    Storage = await BlazeDbOpfsStorage.CreateAsync("mydb"),
}.AddTable(TodoItem.Table));

var todos = db.GetTable(TodoItem.Table);
todos.Insert(new TodoItem { Id = Guid.NewGuid(), Title = "hello" });
var open = todos.Lookup(TodoItem.Indexes.Done, false); // a hash index probe - no I/O, no SQL, no interop
```

## Why

Existing client-side database options for Blazor WebAssembly (sqlite-wasm bridges, IndexedDB wrappers)
pay heavy costs on every operation: JS interop marshalling, worker round-trips, and SQL parsing.
BlazeDb eliminates all three:

- **All live data resides in managed memory** as strongly-typed .NET objects. Reads are plain
  method calls into hash/ordered indexes - no I/O, no interop, no query language.
- **Durability is a background concern**: commits apply to memory synchronously and are appended
  to a write-ahead log (WAL) that is flushed asynchronously to OPFS (Origin Private File System).
  Periodic snapshots compact the log.
- **No SQL**: the engine exposes low-level, typed query primitives (`Get`, `Range`, `Scan`,
  `BlazeDbQuery`). The EF Core provider translates LINQ directly into those primitives - there is no
  intermediate query language to generate or parse.
- **AOT/trimming friendly**: serialization and table metadata are produced by a Roslyn source
  generator - zero runtime reflection.
- **Built for the browser's constraints**: cooperative yielding so long scans do not freeze the
  UI, storage-quota awareness instead of surprise write failures, encryption through WebCrypto,
  and a single-writer model with read-only replica tabs.

## Install

```bash
dotnet add package Bit.BlazeDb                     # the engine
dotnet add package Bit.BlazeDb.Generators          # the source generator for [BlazeDbTable] types
dotnet add package Bit.BlazeDb.Browser             # OPFS, IndexedDB, WebCrypto, tabs
dotnet add package Bit.BlazeDb.EntityFrameworkCore # UseBlazeDb()
```

`Bit.BlazeDb.Generators` is what turns a `[BlazeDbTable]` class into a table: it emits the `Table`
descriptor and the `Indexes` class the rest of this guide uses. Add it to every project that declares
table types - a `PackageReference` to the engine alone compiles the attributes, but generates nothing,
and `TodoItem.Table` then does not exist. The generator ships as an analyzer, so it adds nothing to the
application's output.

Add the namespaces you use to `_Imports.razor` or a global using:

```csharp
global using Bit.BlazeDb;
global using Bit.BlazeDb.Browser;   // the browser backends
global using Bit.BlazeDb.Storage;   // IBlazeDbStorage, the file and in-memory backends, encryption
global using Bit.BlazeDb.Querying;  // BlazeDbQuery, BlazeDbBound
```

## Requirements

- .NET 8, .NET 9 or .NET 10. `Bit.BlazeDb.EntityFrameworkCore` targets EF Core 8, 9 and 10 respectively.
- Blazor WebAssembly - standalone, or a Blazor Web App's `.Client` project - for the browser backends.
  The engine itself has no browser dependency: it runs in any .NET process, with the file or in-memory
  backend.

## Packages

| Package | Purpose |
| --- | --- |
| `Bit.BlazeDb` | The engine: tables, indexes, transactions, WAL/snapshot persistence, the storage abstraction with in-memory, file and encrypted backends. No browser dependency. |
| `Bit.BlazeDb.Generators` | The Roslyn incremental source generator: binary serializers, key extractors and typed index definitions for `[BlazeDbTable]` types. |
| `Bit.BlazeDb.Browser` | OPFS and IndexedDB storage backends (`[JSImport]` ES modules), Web Locks single-writer election, a WebCrypto cipher and cross-tab sync. |
| `Bit.BlazeDb.EntityFrameworkCore` | The EF Core provider: `UseBlazeDb`, change tracking over live rows, LINQ translated to query plans. |

Everything public is prefixed `BlazeDb` (`BlazeDbDatabase`, `BlazeDbTable<TKey, TRow>`,
`BlazeDbOpfsStorage`...) and lives under the `Bit.BlazeDb` namespace and its children.

## The demo

The demo site, [blazedb.bitplatform.dev](https://blazedb.bitplatform.dev), is an interactive tour of the
engine with no external dependencies - no CSS framework, no icon font, no JavaScript library, no web
fonts. Every page drives the real engine in your browser rather than describing it:

| Page | What it demonstrates |
| --- | --- |
| `/` | Architecture, benchmarks and a quick start, with live stats from the demo database. |
| `/todos` | A working CRUD app: index-backed filters, sorting, paging and persisted preferences. |
| `/schema` | Attributes, the generated descriptor inspected at runtime, supported types, diagnostics. |
| `/indexes` | Hash lookups, ordered ranges, `BlazeDbBound<T>`, and indexed versus scanned counting. |
| `/queries` | A query composer that shows the generated C#, the plan and the results. |
| `/efcore` | The EF Core provider: `UseBlazeDb`, live LINQ, SaveChanges, and what the translator absorbs. |
| `/transactions` | Commit, rollback, dispose-without-commit, nesting errors and crash-recovery proof. |
| `/durability` | WAL, snapshots, checkpoints, and simulated crashes including torn tails and corruption. |
| `/storage` | The `IBlazeDbStorage` contract, a live trace of engine I/O, OPFS and Web Locks notes. |
| `/serialization` | Build a row and inspect the exact bytes, field by field, with a hex dump. |
| `/errors` | Every exception the engine raises, triggered live against throwaway databases. |
| `/bench` | Read, write and encoding benchmarks measured in your own browser. |
| `/roadmap` | What ships, what never will, and how the repository is laid out. |
| `/mcp-server` | The MCP server this site hosts for AI agents. |

## MCP server

The demo hosts a [Model Context Protocol](https://modelcontextprotocol.io) server at
`https://blazedb.bitplatform.dev/mcp`, so an AI agent writing database code reads the library itself -
its API, this guide, the source generator and the demo's working pages - instead of guessing at an API it
half remembers. It is also served, next to the other bit libraries, through the aggregated
`https://bitplatform.dev/mcp` endpoint.

```json
{
  "mcpServers": {
    "blazedb": { "type": "http", "url": "https://blazedb.bitplatform.dev/mcp" }
  }
}
```

Start with `SearchBlazeDb`, which searches this guide, every public type and member, the generator's
diagnostics, the setup guides and the demo's pages at once, and points each hit at the exact follow-up
call. `GetBlazeDbSetupGuide` gives the wiring for one hosting model, `GetBlazeDbGuideSection` one section
of this guide, `GetBlazeDbApi` one public type with its members, real defaults and documentation,
`GetBlazeDbDiagnostics` one `BLZ` diagnostic with its cause and fix, and `GetBlazeDbSourceFile` one of the
demo's working files; each returns its index when called without a key. `InspectBlazeDbTable` runs the
real source generator over a `[BlazeDbTable]` class and reports its diagnostics, the field number and wire
type of every property, the key, the indexes and the members it generates. Every tool is read-only.

## Quick start

```csharp
[BlazeDbTable("todos")]
[BlazeDbCompoundIndex("CategoryDone", nameof(Category), nameof(Done))]
public partial class TodoItem
{
    [BlazeDbKey] public Guid Id { get; set; }
    [BlazeDbIndex(Unique = true)] public string Slug { get; set; } = "";
    public string Title { get; set; } = "";
    public string? Category { get; set; }
    [BlazeDbIndex] public bool Done { get; set; }
    [BlazeDbOrderedIndex] public DateTime CreatedAt { get; set; }
}

var db = await BlazeDbDatabase.OpenAsync(new BlazeDbDatabaseOptions
{
    Storage = await BlazeDbOpfsStorage.CreateAsync("mydb"), // or BlazeDbFileStorage / BlazeDbInMemoryStorage / null
}.AddTable(TodoItem.Table));

var todos = db.GetTable(TodoItem.Table);

var id = Guid.NewGuid();
todos.Insert(new TodoItem { Id = id, Slug = "hello", Title = "hello", CreatedAt = DateTime.UtcNow });
var one = todos.Get(id);                                  // O(1), no I/O
var open = todos.Lookup(TodoItem.Indexes.Done, false);    // hash index
var latest = todos.OrderBy(TodoItem.Indexes.CreatedAt, descending: true);
var range = todos.Range(TodoItem.Indexes.CreatedAt, from, to);
var both = todos.Lookup(TodoItem.Indexes.CategoryDone, ("work", false)); // one compound lookup

using (var txn = db.BeginTransaction())
{
    todos.Update(new TodoItem { Id = id, Slug = "hello", Title = "hello", Done = true, CreatedAt = one!.CreatedAt });
    txn.Commit();
}

await db.FlushAsync(); // force durability now (otherwise flushed in the background)
```

### Rows are live objects

Rows are live objects - a read hands back the instance the table holds. `Update` therefore takes a
*new* instance (as above): the table retracts the old index entries through the values of the row it
currently holds, so mutating that instance's indexed properties and passing it back is refused
rather than left to strand index entries. To write a row that was changed in place, hand the table
the values it had: `todos.UpdateInPlace(row, previousValues)` / `todos.DeleteInPlace(key,
previousValues)` - which is what the EF Core provider does with the change tracker's originals.
`previousValues` must carry every indexed property as the table last saw it; if the row has been
written since those values were taken, the call throws `BlazeDbStaleRowException` instead of
leaving the row indexed twice.

### Null values

Null values are indexed like any other: `todos.Lookup(TodoItem.Indexes.Category, null)` returns
the uncategorized rows, ordering by an index puts them first (as `OrderBy` does), and a bounded
`Range` excludes them. A unique index still allows any number of nulls.

### Files on the desktop

On desktop, `BlazeDbFileStorage` takes an exclusive lock on its directory for as long as it lives -
dispose it when the database is closed - and `BlazeDbFileStorage.OpenReadOnly(dir)` opens the same
directory for a follower that only reads. `BlazeDbDatabase.DeleteAsync(storage)` removes a database
from any backend.

### Threading model

BlazeDb is built for Blazor WebAssembly, which has one thread, and its guarantees are those of a
**single logical writer with no concurrent readers**. Writes are serialized by a lock so the
background flusher can snapshot consistently, but reads - `Get`, `Scan`, index lookups, every LINQ
query through EF Core - take no lock at all, because on the platform the engine is designed for
there is nothing to lock against. A read that overlaps a write on another thread is a data race on
the underlying dictionaries and may throw or return garbage.

That matters as soon as the engine leaves the browser. On ASP.NET Core or Blazor Server, `UseBlazeDb`
shares one engine between every context of a type, and requests run on different threads; that is
not a supported configuration unless the application serializes all access itself (one request at
a time, or a lock around every query and save). The desktop file backend is for tools, tests and
single-threaded hosts, not for a multi-threaded server.

### Opening with part of the model

A database may hold tables the current options do not register - a context type that owns some of
a shared store's tables, or an application that has dropped a table type. Such tables are still
opened: their rows are carried as raw bytes through recovery and written back out by every
checkpoint, so nothing is lost, and `db.UnregisteredTables` names them. They cannot be queried until
a descriptor for them is registered, which the EF Core provider does automatically when a context
that knows the table shares the engine. Snapshots written by versions before this behavior (format 1)
still open, but only with every table registered.

### Streaming results

Streaming a large result set without blocking the browser's only thread:

```csharp
await foreach (var todo in BlazeDbQuery<Guid, TodoItem>.From(todos)
    .UseIndex(TodoItem.Indexes.CreatedAt, from, to)
    .Where(t => !t.Done)
    .ExecuteAsync(batchSize: 256))
{
    Render(todo);
}
```

## Hosting models

Where a database can live follows from two facts: the browser backends need the WebAssembly runtime,
and the engine assumes one logical writer with no concurrent readers. Everything else - the tables, the
queries, the EF Core provider - is the same wherever it runs.

### Blazor WebAssembly

The model the engine is designed for. The database lives in the tab, in managed memory, and persists to
OPFS through `Bit.BlazeDb.Browser` (or to IndexedDB where OPFS cannot be written - see
[Multiple tabs](#multiple-tabs)). Open it once per tab and share it: register a singleton service that
opens the database on first use, the way the demo's `DemoDbService` does, rather than opening one per
component. WebAssembly has one thread, so the threading model holds by construction.

```csharp
public sealed class AppDatabase : IAsyncDisposable
{
    private Task<BlazeDbDatabase>? _opening;
    private BlazeDbOpfsStorage? _storage;

    public Task<BlazeDbDatabase> GetAsync() => _opening ??= OpenAsync();

    private async Task<BlazeDbDatabase> OpenAsync()
    {
        _storage = await BlazeDbOpfsStorage.CreateAsync("app"); // takes this tab's writer lock
        return await BlazeDbDatabase.OpenAsync(new BlazeDbDatabaseOptions { Storage = _storage }.AddTable(TodoItem.Table));
    }

    public async ValueTask DisposeAsync()
    {
        if (_opening is not null) await (await _opening).DisposeAsync(); // flushes what is still pending
        _storage?.Dispose();                                             // and only then releases the lock
    }
}

// Program.cs
builder.Services.AddSingleton<AppDatabase>();
```

The database does not own its storage: dispose the storage after the database, which is what releases
the writer lock for the next tab. `CreateAsync` throws `BlazeDbDatabaseLockedException` when another tab
already holds it - open a read-only replica there instead, or fall back to `BlazeDbInMemoryStorage` the
way the demo does.

### Blazor Web App

The database still lives in the browser, so the code that opens it belongs in the `.Client` project and
runs under `InteractiveWebAssembly`. What changes is prerendering: a prerendered component runs on the
server first, where there is no OPFS, no IndexedDB and no Web Locks - `Bit.BlazeDb.Browser` is
`[SupportedOSPlatform("browser")]` and its backends throw outside the browser. Either turn prerendering
off for the pages that use the database (`@rendermode="new InteractiveWebAssemblyRenderMode(prerender: false)"`,
which is what the demo does for the whole app, since every one of its pages opens the database), or keep
it and open the database only once the component runs in the browser - in `OnAfterRenderAsync`, or behind
`OperatingSystem.IsBrowser()` - rendering a placeholder until then. Under `InteractiveAuto` the same
applies to the first visit, which renders on the server while the runtime downloads.

### Blazor Server and ASP.NET Core

The engine runs in the server process, persisting through `BlazeDbFileStorage`. Every circuit and every
request runs on its own thread, which the engine does not guard against (see
[Threading model](#threading-model)): serialize all access to a shared database yourself, or give each
user a database of their own in a directory of their own. For a multi-user server store, a database
server is the right tool; BlazeDb is the local store of one user's application.

### Blazor Hybrid and desktop

MAUI, WPF and Windows Forms apps run .NET in-process, so the browser backends do not apply even though
the UI is rendered in a WebView. Persist through `BlazeDbFileStorage` in the app's data directory
(`FileSystem.AppDataDirectory` on MAUI), opened once for the app's lifetime and disposed when it closes.
Components render on the UI thread, which serializes their access; marshal work from background threads
back to it. Console tools and tests use `BlazeDbFileStorage` or `BlazeDbInMemoryStorage`, which also makes
crash and corruption scenarios reproducible.

## Encryption at rest

`BlazeDbEncryptedStorage` wraps any backend and seals each write into its own AES-GCM frame, so WAL
appends stay appends. In the browser, use the WebCrypto cipher - the managed `AesGcm` throws
inside the browser sandbox:

```csharp
var key = BlazeDbEncryptedStorage.DeriveKey(passphrase, salt);   // PBKDF2, works everywhere
var cipher = await BlazeDbWebCryptoCipher.CreateAsync(key);      // BlazeDbAesGcmCipher on desktop
var storage = new BlazeDbEncryptedStorage(await BlazeDbOpfsStorage.CreateAsync("mydb"), cipher, ownsCipher: true);
```

What this does and does not protect: every frame is sealed with a fresh random 96-bit nonce and the
file name as associated data, so contents cannot be read or altered without the key and a frame
cannot be moved between files. It does not prevent *rollback* - someone with access to the files
can put back an older, internally consistent manifest, snapshot and log - and there is no key
rotation: to change the key, open the database, checkpoint, and write it out through a storage
wrapped with the new cipher. Random nonces are safe for about 2^32 frames per key; at the default
flush interval that is decades of continuous writing, but a long-lived database should rotate
before then.

## Storage quota

When the backend can report the origin's allowance, the engine checks it before flushing,
compacts if space is short, and refuses deliberately rather than failing mid-append. The data
stays intact in memory and the flush can be retried once space is freed:

```csharp
var options = new BlazeDbDatabaseOptions
{
    Storage = storage,
    QuotaReserveBytes = 4 * 1024 * 1024,
    OnQuotaPressure = quota => ShowStorageWarning(quota.UsedFraction),
};

var quota = await db.GetStorageQuotaAsync();   // null when the backend cannot say
await BlazeDbOpfsStorage.RequestPersistenceAsync();   // ask not to be evicted
```

Wrapping a browser backend in `BlazeDbEncryptedStorage` keeps its quota awareness. When a background
flush is refused (or fails for any other reason) the data stays committed in memory and the flusher
keeps retrying; `db.LastBackgroundError` reports the standing failure so a UI can say durability is
lagging, and clears again once a flush succeeds.

## Multiple tabs

One tab holds the writer lock; the others open the same database read-only and reload when the
writer checkpoints. Only the notification crosses tabs - the data always travels through storage,
so a replica can never observe an uncommitted state:

```csharp
// Writer tab
var sync = await BlazeDbTabSync.CreatePublisherAsync("mydb");
var db = await BlazeDbDatabase.OpenAsync(new BlazeDbDatabaseOptions
{
    Storage = await BlazeDbOpfsStorage.CreateAsync("mydb"),
    OnCheckpoint = sync.PublishCheckpoint,
}.AddTable(TodoItem.Table));

// Replica tab
var replica = await BlazeDbDatabase.OpenAsync(new BlazeDbDatabaseOptions
{
    Storage = await BlazeDbOpfsStorage.CreateReadOnlyAsync("mydb"),
    ReadOnly = true,
}.AddTable(TodoItem.Table));
await BlazeDbTabSync.CreateReplicaAsync("mydb", replica, onChanged: RefreshUiAsync);
```

`CreateReadOnlyAsync` returns `null` when the database does not exist yet, rather than creating an
empty one: a replica that opens before the writer has written anything should wait for it, not
invent a database of its own. The storage it returns refuses every write, so forgetting
`ReadOnly = true` cannot turn a replica into a second, unelected writer on the same files.

A reload reads the whole generation - manifest, snapshot and log - before it touches a single row,
and re-reads the manifest at the end. A checkpoint that lands halfway through is noticed and the
read starts again; a reload that fails leaves the replica serving what it had.

### Without OPFS

Where OPFS is unavailable or cannot be written from the main thread (Firefox private windows;
Safari before 26, which can read OPFS but lacks `createWritable`), swap in `BlazeDbIndexedDbStorage`,
which has the same `CreateAsync` / `CreateReadOnlyAsync` pair. `IsOpfsAvailableAsync` checks for
the write API, not just the directory, so it says no on exactly those browsers:

```csharp
IBlazeDbStorage storage = await BlazeDbIndexedDbStorage.IsOpfsAvailableAsync()
    ? await BlazeDbOpfsStorage.CreateAsync("mydb")
    : await BlazeDbIndexedDbStorage.CreateAsync("mydb");
```

Both backends need the Web Locks API to elect the writer. On a browser without it `CreateAsync`
throws rather than guess; pass `allowWithoutWebLocks: true` if the application guarantees a single
tab. A tab restored from the back/forward cache re-checks that it still holds the lock and stops
writing - the data stays in memory and `db.LastBackgroundError` says why - if another tab has
taken over in the meantime.

## EF Core

The provider maps entity types onto the same `[BlazeDbTable]` types the engine uses - the descriptor the
source generator emits is the model - and is configured the same way as any other EF Core provider:

```csharp
public sealed class AppContext(DbContextOptions<AppContext> options) : DbContext(options)
{
    public DbSet<TodoItem> Todos => Set<TodoItem>();
}

builder.Services.AddDbContext<AppContext>(options =>
    options.UseBlazeDb());                 // in-memory
    // options.UseBlazeDb(storage);        // OPFS, files, encrypted storage, …

var open = context.Todos.Where(t => !t.Done).OrderBy(t => t.CreatedAt).Take(20).ToList();
open[0].Done = true;
context.SaveChanges();
```

`UseBlazeDb()` with no arguments names the store after the context type, so every `AppContext`
shares one engine. Pass a string to choose the name (`UseInMemoryDatabase` style), or an
`IBlazeDbStorage` to persist. Tables are discovered from `DbSet` properties; there is no
`OpenAsync` and no `AddTable` on this path. A store is identified by its name or its storage
instance across the whole process - two contexts configured alike share one engine even when EF
gives them separate internal service providers - so a persistent store never ends up with two
writers.

`OnConfiguring` works too:

```csharp
protected override void OnConfiguring(DbContextOptionsBuilder options)
    => options.UseBlazeDb();
```

### Opening the store

When the storage backend is asynchronous (OPFS in the browser), call
`await context.Database.EnsureCreatedAsync()` once before the first synchronous query.
In-memory stores open synchronously and need no extra step. `EnsureCreated` returns `true` for
the call that opened the store and `false` after that, so `if (EnsureCreated()) Seed();` works
as it does with other providers. `EnsureDeleted` removes the store's files as well as closing it.

### What follows from memory-first

Three things are worth knowing, because they follow from the engine being memory-first rather than
from anything EF does:

- **A query returns the row itself, not a copy.** Mutating a tracked entity changes the database in
  memory immediately; `SaveChanges` is what commits it as one transaction and one WAL record, and
  what moves the secondary index entries onto the new values. Reading a value you have modified but
  not yet saved therefore gives you the modified one - from a query as well: while a tracked entity
  of that type has unsaved changes in *any* live context on the engine, queries read the rows
  instead of the indexes so both agree. (Contexts on one engine register with it for exactly this
  purpose; the check costs a change-detection pass over the other contexts' trackers, paid only by
  queries that would otherwise use an index.)
- **`SaveChanges` is a memory operation.** It returns as soon as the transaction commits; the log
  reaches storage on the flush interval, or immediately if you call `context.Database.GetBlazeDb().FlushAsync()`.
  A duplicate key or unique-index violation fails the save with `DbUpdateException` (the engine's
  exception is the `InnerException`); the whole save is rolled back. When the save runs inside a
  transaction you opened - `Database.BeginTransaction()` or one on the engine itself - it joins that
  transaction, so a failure part-way leaves the entries already applied in it for your rollback to
  undo, as with a relational provider.
- **Two contexts on one store track the same objects.** Each keeps its own original values, so if
  both load a row and both save it, the second save describes a state the row has moved past and
  fails with `DbUpdateConcurrencyException`; reload and retry, as with any provider. Updating or
  deleting a row another context has since deleted is a `DbUpdateConcurrencyException` too. A save
  cannot join a transaction another context has open. And after a rollback, the entities whose
  saves were undone are detached - the objects still hold the undone values, and the next query
  loads the rows as the database actually has them.

The model comes from the same attributes the engine uses: `[BlazeDbKey]` names the primary key whatever it
is called, and `[BlazeDbIgnore]` keeps a property out of the entity type as well as out of the row bytes.

### How LINQ is translated

LINQ is translated as far as the engine's plan model reaches: a source chosen from the predicates -
the primary-key dictionary for `Where(t => t.Id == id)`, `ids.Contains(t.Id)` and `Find(id)`, a
compound or single-property index for equalities (including `t.Category == null`) and `Contains`
lists, an ordered index for ranges and `OrderBy` - then filters, ordering and paging.
Everything else - projections, grouping, aggregates, joins and subqueries - runs as LINQ to Objects
over the rows the plan returned, which costs nothing extra because those rows are already objects.
A query may refer to other sets (`context.People.Where(p => context.Orders.Any(o => o.PersonId ==
p.Id))`, a `join` between two sets); those are read as scans of their tables. Two things to know:
an `OrderBy` on a string property that an ordered index answers comes back in ordinal order, since
that is how the index sorts, where LINQ to Objects would use the current culture; and `Include` is
refused, because BlazeDb rows have no navigations - relationships are modelled as key properties
and queried against the other table directly.

### Compiled queries

Because the provider replaces EF's query pipeline, it also replaces EF's compiled-query cache. The
predicates and orderings a plan runs are compiled once per query *shape* and re-bound to the values
each execution captures, so repeating a query does not compile anything; the expression walk that
chooses the plan is repeated each time and is a few microseconds. Operators left to LINQ to Objects
(a `Select`, a `GroupBy`) are evaluated by `System.Linq`'s own enumerable query provider, which does
compile them per execution - keep the heavy lifting in the `Where`/`OrderBy`/`Skip`/`Take` prefix
the plan absorbs when a query is hot.

The demo site walks through all of this against a live database at `/efcore`.

## Schema evolution

Field numbers identify properties in the binary format. Without `[BlazeDbField(n)]` they follow
declaration order, so adding, removing or reordering a property renumbers the ones after it and
rows already on disk decode into the wrong properties without an error. Pin every property's number
(1 to 536,870,911) before a type persists real data; the generator warns (`BLZ013`) when a type pins
some numbers but not others, which is the case where an automatic number moves most easily.

Collections (`List<T>`, `T[]`) and `byte[]` can be stored but not indexed, and a collection's element
type has to be non-nullable: the wire format has no encoding for a null element, only for an absent
field. The generator reports each of these as an error on the property rather than letting the
generated code fail to compile or the index throw on its first insert.

## Compile-time diagnostics

`Bit.BlazeDb.Generators` reports every table shape it cannot generate as a build diagnostic on the type
or property at fault, rather than emitting code that fails to compile or a descriptor that throws at
runtime. A type with an error gets no generated partial at all, so its own diagnostic arrives together
with "does not contain a definition for 'Table'" errors wherever the type is used - fix the `BLZ` one first.

| Id | Severity | Title |
| --- | --- | --- |
| BLZ001 | Error | Table type must be partial |
| BLZ002 | Error | Table type must have exactly one [BlazeDbKey] property |
| BLZ003 | Error | Unsupported property type |
| BLZ004 | Error | Table type needs a parameterless constructor |
| BLZ005 | Error | Duplicate field number |
| BLZ006 | Error | Unsupported key type |
| BLZ007 | Error | Unsupported table type shape |
| BLZ008 | Error | Compound index references an unknown property |
| BLZ009 | Error | Compound index needs at least two properties |
| BLZ010 | Error | Duplicate index name |
| BLZ011 | Error | Index name must be a valid identifier |
| BLZ012 | Error | Table type inherits properties |
| BLZ013 | Warning | Table type mixes explicit and automatic field numbers |
| BLZ014 | Error | Field number out of range |
| BLZ015 | Error | Property cannot be indexed |
| BLZ016 | Error | Compound index lists a property twice |

## Performance

BenchmarkDotNet, .NET 10, 10,000-row table, vs native SQLite (in-memory, prepared statements).
Native SQLite is a *favorable* proxy for sqlite-wasm solutions, which additionally pay JS
interop, worker round-trips and wasm execution overhead on every query.

| Operation | BlazeDb | SQLite | Notes |
| --- | ---: | ---: | --- |
| Point get by key | 2.6 ns (0 alloc) | 876 ns | ~340x |
| Count by hash index | 2.7 ns (0 alloc) | 67,649 ns | O(1) vs table scan |
| Ordered range scan (~5,100 rows) | 55.6 us | 69.3 us | comparable; both index-driven |
| Upsert (autocommit) | 462 ns (with WAL) | 752 ns | memory-only: 328 ns |
| Upsert in 1,000-op transaction | 487 ns/op | 682 ns/op | with WAL journaling |

Reproduce with the suite in `Tests/Bit.BlazeDb.Benchmarks` (see its README). The demo app also has an
in-browser micro-benchmark page (`/bench`) that measures the same operations in the WebAssembly
runtime, including per-row storage cost.

## Building

From the repository's `src` folder:

```bash
dotnet build BlazeDb/Bit.BlazeDb.slnx
dotnet test --project BlazeDb/Tests/Bit.BlazeDb.Tests        # engine and EF Core provider, net8.0/net9.0/net10.0
dotnet test --project BlazeDb/Tests/Bit.BlazeDb.Tests.Mcp    # the demo's MCP server
dotnet run --project BlazeDb/Bit.BlazeDb.Demo/Server         # the demo site
```

The browser suite (`Tests/Bit.BlazeDb.Tests.E2E`) runs the demo in Chromium; its README says how.
