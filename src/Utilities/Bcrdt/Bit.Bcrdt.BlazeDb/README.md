# Bit.Bcrdt.BlazeDb

A [bit Bcrdt](https://www.nuget.org/packages/Bit.Bcrdt) document store on top of
[bit BlazeDb](https://www.nuget.org/packages/Bit.BlazeDb), so an application already on BlazeDb keeps its collaborative
documents in the same database as the rest of its data - persisted to OPFS or IndexedDB in Blazor WebAssembly, or to
files on the server, the desktop and MAUI - with no second storage to open, quota to watch or export to forget.

```bash
dotnet add package Bit.Bcrdt.BlazeDb
```

The application keeps its own reference to `Bit.BlazeDb`: that is what carries BlazeDb's source generator and browser
modules to the project, as BlazeDb's own README explains.

Register the store's table where the database is opened, next to the application's own:

```csharp
using Bit.Bcrdt.BlazeDb;

BlazeDbDatabase database = await BlazeDbDatabase.OpenAsync(new BlazeDbDatabaseOptions
{
    Storage = await BlazeDbOpfsStorage.CreateAsync("app"),
}.AddTable(TodoItem.Table)
 .AddTable(BcrdtBlazeDbDocumentStore.Table));
```

Then hand the database to bit Bcrdt. A WebAssembly application usually opens its database once, asynchronously, in a
service it shares; give the store that service and it asks for the database when it is first used:

```csharp
builder.Services.AddSingleton<AppDatabase>();
builder.Services.AddBitBcrdtServices(collaboration => collaboration
    .UseTransport(new BcrdtSignalRTransportFactory(hubUrl))
    .UseBlazeDbStore((services, _) => new(services.GetRequiredService<AppDatabase>().GetAsync())));
```

`UseBlazeDbStore(database)` takes an open database directly, and `new BcrdtBlazeDbDocumentStore(...)` builds one for
`BcrdtDocumentPersistence` or a document host.

## What it guarantees

- **Appends are cheap.** Each update is one row - keyed by a sequence number, indexed by document - committed in one
  in-memory step and appended to BlazeDb's write-ahead log.
- **An update the store accepted survives a crash.** Every write waits for the database to flush it
  (`WaitForDurability`, on by default). Turn it off to leave durability to BlazeDb's background flusher, within its
  `FlushInterval`; a crash then costs at most that interval's updates, which a peer or server still holds and resends.
- **Torn and damaged records** are BlazeDb's to handle, and it does: every log record is checksummed, a torn tail is
  cut off on recovery, and damage anywhere else makes the open fail rather than cut away what follows it. A row that
  is intact but is no update this build can use is skipped, left in place and reported through
  `UnreadableRecordSkipped`.
- **Compaction is atomic.** The merged update is inserted and exactly the rows it replaces are deleted in one BlazeDb
  transaction, which commits as one log record: a crash leaves the old rows or the merged one, never both or neither.
- **Writes never join the application's transaction.** BlazeDb has one ambient transaction; a store write made while
  the application had one open would roll back with it. The store refuses that case as a transient failure instead,
  and the persistence layer retries the update once the transaction ends.
- **Document identifiers are data.** They are stored as values and found through an index, so one taken from a URL
  needs no sanitizing; an identifier that is not well-formed Unicode is refused, because it could not be read back as
  the same string.

## Things to know

- **BlazeDb is memory-first.** Every stored document's bytes stay in memory for as long as the database is open,
  whether or not the document is - compaction keeps that to about one merged update per document.
- **One writer.** A read-only BlazeDb replica - the second tab, under BlazeDb's single-writer model - loads documents
  and refuses to store their changes (`NotSupportedException`, reported once through `WriteFailed`). Where several tabs
  must each persist their own edits, `BcrdtIndexedDbDocumentStore` from Bit.Bcrdt is the better fit.
- **Threading.** BlazeDb assumes one logical writer and no concurrent readers, which WebAssembly gives by construction.
  On a multi-threaded host - Blazor Server, MAUI, the desktop - pass the lock the application serializes its own
  database access with as `BcrdtBlazeDbDocumentStoreOptions.Gate`, and the store takes it around everything it does.
- **Change notifications.** The store's rows are ordinary rows: a database-wide `Changed` subscriber sees them, and can
  tell them apart by the table name, `BcrdtBlazeDbDocumentStore.TableName`. A handler that throws for one of the
  store's writes does not fail it - the write committed - and is reported through `ChangeHandlerFailed`.

The full guide is in the [Bcrdt README](https://github.com/bitfoundation/bitplatform/tree/develop/src/Utilities/Bcrdt).
