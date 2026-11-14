using System.Data;
using Bit.Bsqlite.Data;
using Bit.Bsqlite.Storage;

namespace Bit.Bsqlite.Interop.Tests;

/// <summary>
/// The session factory is the only place that both chooses a backend and opens it, so the rule
/// "the binding is written only after the open succeeds" is asserted here rather than left as a
/// comment on two collaborating types.
/// </summary>
[TestClass]
public sealed class BsqliteSessionFactoryTests
{
    public TestContext TestContext { get; set; } = null!;

    private CancellationToken Ct => TestContext.CancellationToken;

    [TestMethod]
    public async Task Open_UsesTheResolvedProvider_AndCommitsTheBinding()
    {
        var opfs = Available("opfs", BsqliteEngineBuild.Synchronous);
        var store = new BsqliteInMemoryStorageBindingStore();
        var transports = new RecordingTransportFactory();
        var factory = Factory(store, transports, s => s.Prefer("opfs"), opfs);

        await using var session = await factory.OpenAsync("app.db", Ct);

        Assert.AreEqual("opfs", session.Resolution.Provider.Name);
        Assert.AreEqual("opfs", await store.GetProviderNameAsync("app.db", Ct));
        Assert.AreEqual("app.db", transports.Last.OpenedAs);
        Assert.AreEqual(opfs, transports.Created[0].Provider);
        Assert.AreEqual(ConnectionState.Open, session.Connection.State);
    }

    [TestMethod]
    public async Task Open_PassesTheProvidersBuildAndVfs_ThroughTheTransportFactory()
    {
        var indexed = Available(
            "indexeddb",
            BsqliteEngineBuild.AsyncCapable,
            vfs: new BsqliteJsModule("./_content/Bit.Bsqlite/bit-bsqlite-idb-vfs.js"));

        var capturing = new RecordingTransportFactory();
        var factory = Factory(
            new BsqliteInMemoryStorageBindingStore(),
            capturing,
            s => s.Prefer("indexeddb"),
            indexed);

        await using var session = await factory.OpenAsync("app.db", Ct);

        var seen = capturing.Created[0].Provider;
        Assert.AreSame(indexed, seen);
        Assert.AreEqual(BsqliteEngineBuild.AsyncCapable, seen.Capabilities.RequiredBuild);
        Assert.AreEqual("./_content/Bit.Bsqlite/bit-bsqlite-idb-vfs.js", seen.VfsModule!.ModuleUrl);
    }

    [TestMethod]
    public async Task FailedOpen_DoesNotCommitABinding_AndDisposesTheTransport()
    {
        var store = new BsqliteInMemoryStorageBindingStore();
        var transports = new RecordingTransportFactory(
            () => new ScriptedTransport
            {
                OpenThrows = new BsqliteException("disk I/O error", sqliteErrorCode: 10),
            });
        var factory = Factory(store, transports, s => s.Prefer("opfs"), Available("opfs"));

        var error = await Assert.ThrowsExactlyAsync<BsqliteException>(
            () => factory.OpenAsync("app.db", Ct));

        Assert.AreEqual(10, error.SqliteErrorCode);
        Assert.IsNull(await store.GetProviderNameAsync("app.db", Ct));
        Assert.IsTrue(transports.Last.Disposed);
    }

    [TestMethod]
    public async Task ExistingData_OpensOnTheBoundBackend_NotThePreferredOne()
    {
        var indexed = Available("indexeddb", BsqliteEngineBuild.AsyncCapable);
        var opfs = Available("opfs", BsqliteEngineBuild.Synchronous);
        var store = new BsqliteInMemoryStorageBindingStore();
        await store.SetProviderNameAsync("app.db", "indexeddb", Ct);

        var transports = new RecordingTransportFactory();
        var factory = Factory(
            store,
            transports,
            s => s.Prefer("opfs").Fallback("indexeddb"),
            opfs,
            indexed);

        await using var session = await factory.OpenAsync("app.db", Ct);

        Assert.AreEqual("indexeddb", session.Resolution.Provider.Name);
        Assert.IsTrue(session.Resolution.WasDecidedByExistingData);
        Assert.AreSame(indexed, transports.Created[0].Provider);
    }

    [TestMethod]
    public async Task AutomaticOnOpen_WithoutABetterProvider_JustOpens()
    {
        var store = new BsqliteInMemoryStorageBindingStore();
        var factory = Factory(
            store,
            new RecordingTransportFactory(),
            s => s.Prefer("opfs").WithMigrationMode(BsqliteStorageMigrationMode.AutomaticOnOpen),
            Available("opfs"));

        await using var session = await factory.OpenAsync("app.db", Ct);

        Assert.AreEqual("opfs", session.Resolution.Provider.Name);
        Assert.AreEqual("opfs", await store.GetProviderNameAsync("app.db", Ct));
    }

    [TestMethod]
    public async Task AutomaticOnOpen_MovesTheDatabaseToTheBetterProvider_BeforeOpening()
    {
        var slow = new PersistentTestProvider("slow");
        var fast = new PersistentTestProvider("fast");
        var image = SqliteImages.Valid(fill: 3);
        await slow.Admin.ImportAsync("app.db", image, Ct);

        var store = new BsqliteInMemoryStorageBindingStore();
        await store.SetProviderNameAsync("app.db", "slow", Ct);

        var transports = new RecordingTransportFactory();
        var factory = Factory(
            store,
            transports,
            s => s.Prefer("fast").Fallback("slow").WithMigrationMode(BsqliteStorageMigrationMode.AutomaticOnOpen),
            fast,
            slow);

        await using var session = await factory.OpenAsync("app.db", Ct);

        Assert.AreEqual("fast", session.Resolution.Provider.Name);
        Assert.AreSame(fast, transports.Created[0].Provider);
        Assert.AreEqual("fast", await store.GetProviderNameAsync("app.db", Ct));
        CollectionAssert.AreEqual(image, await fast.Admin.ExportAsync("app.db", Ct));
        Assert.IsFalse(await slow.Admin.ExistsAsync("app.db", Ct));
    }

    /// <summary>
    /// An opportunistic move that fails is not a reason to keep the user from data that is still
    /// exactly where it was. The open proceeds on the bound backend, the binding is unchanged, and
    /// the resolution still says a better backend exists.
    /// </summary>
    [TestMethod]
    public async Task AutomaticOnOpen_WhenTheMoveFails_StillOpensWhereTheDataIs()
    {
        var slow = new PersistentTestProvider("slow");
        var fast = new PersistentTestProvider("fast", refusesImports: true);
        await slow.Admin.ImportAsync("app.db", SqliteImages.Valid(), Ct);

        var store = new BsqliteInMemoryStorageBindingStore();
        await store.SetProviderNameAsync("app.db", "slow", Ct);

        var transports = new RecordingTransportFactory();
        var factory = Factory(
            store,
            transports,
            s => s.Prefer("fast").Fallback("slow").WithMigrationMode(BsqliteStorageMigrationMode.AutomaticOnOpen),
            fast,
            slow);

        await using var session = await factory.OpenAsync("app.db", Ct);

        Assert.AreEqual("slow", session.Resolution.Provider.Name);
        Assert.AreSame(fast, session.Resolution.BetterProviderAvailable);
        Assert.AreEqual("slow", await store.GetProviderNameAsync("app.db", Ct));
        Assert.IsTrue(await slow.Admin.ExistsAsync("app.db", Ct));
    }

    /// <summary>
    /// The copy was verified and bound, then deleting the source failed with the source still whole -
    /// another tab holding it open, say. The flip is undone, so the database opens on the source and
    /// the binding agrees: writes made now are where the next load reads.
    /// </summary>
    [TestMethod]
    public async Task AutomaticOnOpen_WhenDeletingTheSourceFails_AndItSurvived_OpensOnTheSource_AndKeepsItBound()
    {
        var slow = new PersistentTestProvider("slow", deletes: DeleteBehavior.FailsLeavingIt);
        var fast = new PersistentTestProvider("fast");
        await slow.Admin.ImportAsync("app.db", SqliteImages.Valid(), Ct);

        var store = new BsqliteInMemoryStorageBindingStore();
        await store.SetProviderNameAsync("app.db", "slow", Ct);

        var factory = Factory(
            store,
            new RecordingTransportFactory(),
            s => s.Prefer("fast").Fallback("slow").WithMigrationMode(BsqliteStorageMigrationMode.AutomaticOnOpen),
            fast,
            slow);

        await using var session = await factory.OpenAsync("app.db", Ct);

        Assert.AreEqual("slow", session.Resolution.Provider.Name);
        Assert.AreEqual("slow", await store.GetProviderNameAsync("app.db", Ct));
        Assert.IsTrue(await slow.Admin.ExistsAsync("app.db", Ct));
        Assert.IsFalse(await fast.Admin.ExistsAsync("app.db", Ct), "The unbound copy is dropped.");
    }

    /// <summary>
    /// Deleting the source failed part-way, so the verified copy is the only whole database left. It
    /// stays bound, and the session opens on it rather than on what remains of the source.
    /// </summary>
    [TestMethod]
    public async Task AutomaticOnOpen_WhenDeletingTheSourceFailsPartway_OpensOnTheTarget()
    {
        var slow = new PersistentTestProvider("slow", deletes: DeleteBehavior.FailsAfterDeleting);
        var fast = new PersistentTestProvider("fast");
        var image = SqliteImages.Valid();
        await slow.Admin.ImportAsync("app.db", image, Ct);

        var store = new BsqliteInMemoryStorageBindingStore();
        await store.SetProviderNameAsync("app.db", "slow", Ct);

        var transports = new RecordingTransportFactory();
        var factory = Factory(
            store,
            transports,
            s => s.Prefer("fast").Fallback("slow").WithMigrationMode(BsqliteStorageMigrationMode.AutomaticOnOpen),
            fast,
            slow);

        await using var session = await factory.OpenAsync("app.db", Ct);

        Assert.AreEqual("fast", session.Resolution.Provider.Name);
        Assert.AreSame(fast, transports.Created[0].Provider);
        Assert.AreEqual("fast", await store.GetProviderNameAsync("app.db", Ct));
        CollectionAssert.AreEqual(image, await fast.Admin.ExportAsync("app.db", Ct));
    }

    [TestMethod]
    public async Task Dispose_TearsDownTheTransport()
    {
        var transports = new RecordingTransportFactory();
        var factory = Factory(
            new BsqliteInMemoryStorageBindingStore(),
            transports,
            s => s.Prefer("opfs"),
            Available("opfs"));

        var session = await factory.OpenAsync("app.db", Ct);
        await session.DisposeAsync();

        Assert.IsTrue(transports.Last.Disposed);
    }

    // ---------------------------------------------------------------------------------------------
    // BsqliteDatabase: restore, delete, disposal
    // ---------------------------------------------------------------------------------------------

    [TestMethod]
    public async Task Restore_ImportsTheImageWhereTheDatabaseLives_AndReopens()
    {
        var home = new PersistentTestProvider("home");
        var store = new BsqliteInMemoryStorageBindingStore();
        var transports = new RecordingTransportFactory();
        await using var database = new BsqliteDatabase(Factory(store, transports, s => s.Prefer("home"), home), "app.db");
        var first = await database.OpenAsync(Ct);
        var events = new List<string>();
        database.Closing += () => { events.Add("closing"); return Task.CompletedTask; };
        database.Opened += () => { events.Add("opened"); return Task.CompletedTask; };
        var backup = SqliteImages.Valid(fill: 9, pages: 3);

        await database.RestoreAsync(backup, Ct);

        CollectionAssert.AreEqual(backup, await home.Admin.ExportAsync("app.db", Ct));
        CollectionAssert.AreEqual(new[] { "closing", "opened" }, events);
        Assert.IsTrue(database.IsOpen);
        Assert.AreNotSame(first, database.Session);
        Assert.IsTrue(transports.Created[0].Transport.Disposed, "The session open during the import was closed.");
    }

    [TestMethod]
    public async Task Restore_OfADatabaseNotYetOpen_FindsWhereItLives_AndLeavesItOpen()
    {
        var home = new PersistentTestProvider("home");
        var store = new BsqliteInMemoryStorageBindingStore();
        await using var database = new BsqliteDatabase(
            Factory(store, new RecordingTransportFactory(), s => s.Prefer("home"), home),
            "app.db");
        var backup = SqliteImages.Valid(fill: 4);

        await database.RestoreAsync(backup, Ct);

        CollectionAssert.AreEqual(backup, await home.Admin.ExportAsync("app.db", Ct));
        Assert.AreEqual("home", await store.GetProviderNameAsync("app.db", Ct));
        Assert.IsTrue(database.IsOpen);
    }

    [TestMethod]
    public async Task Restore_RefusesAFileThatIsNotADatabase_BeforeClosingAnything()
    {
        var home = new PersistentTestProvider("home");
        var original = SqliteImages.Valid(fill: 1);
        await home.Admin.ImportAsync("app.db", original, Ct);
        var store = new BsqliteInMemoryStorageBindingStore();
        await store.SetProviderNameAsync("app.db", "home", Ct);
        await using var database = new BsqliteDatabase(
            Factory(store, new RecordingTransportFactory(), s => s.Prefer("home"), home),
            "app.db");
        var session = await database.OpenAsync(Ct);
        var closed = false;
        database.Closing += () => { closed = true; return Task.CompletedTask; };

        await Assert.ThrowsExactlyAsync<BsqliteCorruptDatabaseException>(
            () => database.RestoreAsync("PK\u0003\u0004 a zip, not a database"u8.ToArray(), Ct));
        var truncated = SqliteImages.Valid(pages: 4)[..2048];
        await Assert.ThrowsExactlyAsync<BsqliteCorruptDatabaseException>(() => database.RestoreAsync(truncated, Ct));

        Assert.IsFalse(closed);
        Assert.AreSame(session, database.Session);
        CollectionAssert.AreEqual(original, await home.Admin.ExportAsync("app.db", Ct));
    }

    /// <summary>
    /// Every database EF Core creates on a server or a desktop is in WAL mode, and its header says so. A web
    /// VFS has no shared memory for the log SQLite would then look for, so the image is marked for a
    /// rollback journal on the way in, as <c>PRAGMA journal_mode = DELETE</c> would after a checkpoint.
    /// </summary>
    [TestMethod]
    public async Task Restore_MarksAWalModeImage_ForARollbackJournal()
    {
        var home = new PersistentTestProvider("home");
        await using var database = new BsqliteDatabase(
            Factory(new BsqliteInMemoryStorageBindingStore(), new RecordingTransportFactory(), s => s.Prefer("home"), home),
            "app.db");

        await database.RestoreAsync(SqliteImages.Valid(walMode: true), Ct);

        var restored = await home.Admin.ExportAsync("app.db", Ct);
        Assert.AreEqual(1, restored[18]);
        Assert.AreEqual(1, restored[19]);
        CollectionAssert.AreEqual(SqliteImages.Valid(), restored);
    }

    [TestMethod]
    public async Task Restore_OnABackendThatDoesNotPersist_IsRefused()
    {
        var memory = new ConfigurableProvider { Name = "memory", RequiredBuild = BsqliteEngineBuild.Synchronous, IsPersistent = false };
        await using var database = new BsqliteDatabase(
            Factory(
                new BsqliteInMemoryStorageBindingStore(),
                new RecordingTransportFactory(),
                s => s.Prefer("memory").AllowNonPersistentFallback(),
                memory),
            "app.db");

        await Assert.ThrowsExactlyAsync<NotSupportedException>(() => database.RestoreAsync(SqliteImages.Valid(), Ct));
    }

    /// <summary>
    /// Closing a database that does not persist is what loses its rows, so an open one is refused before it
    /// closes - not wiped and then refused.
    /// </summary>
    [TestMethod]
    public async Task Restore_OfAnOpenDatabaseThatDoesNotPersist_IsRefusedBeforeItCloses()
    {
        var memory = new ConfigurableProvider { Name = "memory", RequiredBuild = BsqliteEngineBuild.Synchronous, IsPersistent = false };
        var transports = new RecordingTransportFactory();
        await using var database = new BsqliteDatabase(
            Factory(new BsqliteInMemoryStorageBindingStore(), transports, s => s.Prefer("memory").AllowNonPersistentFallback(), memory),
            "app.db");
        var session = await database.OpenAsync(Ct);
        var closed = false;
        database.Closing += () => { closed = true; return Task.CompletedTask; };

        await Assert.ThrowsExactlyAsync<NotSupportedException>(() => database.RestoreAsync(SqliteImages.Valid(), Ct));

        Assert.IsFalse(closed);
        Assert.AreSame(session, database.Session);
        Assert.IsFalse(transports.Last.Disposed);
    }

    /// <summary>An empty file - a failed upload looks like one - would wipe the database it replaced.</summary>
    [TestMethod]
    public async Task Restore_OfAnEmptyFile_IsRefused()
    {
        var home = new PersistentTestProvider("home");
        var original = SqliteImages.Valid(fill: 5);
        await home.Admin.ImportAsync("app.db", original, Ct);
        var store = new BsqliteInMemoryStorageBindingStore();
        await store.SetProviderNameAsync("app.db", "home", Ct);
        await using var database = new BsqliteDatabase(
            Factory(store, new RecordingTransportFactory(), s => s.Prefer("home"), home),
            "app.db");

        await Assert.ThrowsExactlyAsync<BsqliteCorruptDatabaseException>(() => database.RestoreAsync(ReadOnlyMemory<byte>.Empty, Ct));

        CollectionAssert.AreEqual(original, await home.Admin.ExportAsync("app.db", Ct));
    }

    /// <summary>
    /// A copy an interrupted migration left on another backend is how a database whose binding is lost gets
    /// found again - so a delete that cleared only the bound backend and the binding would see the old rows
    /// come back on the next open.
    /// </summary>
    [TestMethod]
    public async Task Delete_RemovesEveryCopy_AndTheBinding_AndLeavesTheDatabaseClosed()
    {
        var home = new PersistentTestProvider("home");
        var stale = new PersistentTestProvider("stale");
        await home.Admin.ImportAsync("app.db", SqliteImages.Valid(fill: 1), Ct);
        await stale.Admin.ImportAsync("app.db", SqliteImages.Valid(fill: 2), Ct);
        await home.Admin.ImportAsync("other.db", SqliteImages.Valid(fill: 3), Ct);
        var store = new BsqliteInMemoryStorageBindingStore();
        await store.SetProviderNameAsync("app.db", "home", Ct);
        await using var database = new BsqliteDatabase(
            Factory(store, new RecordingTransportFactory(), s => s.Prefer("home").Fallback("stale"), home, stale),
            "app.db");
        await database.OpenAsync(Ct);

        await database.DeleteAsync(Ct);

        Assert.IsFalse(database.IsOpen);
        Assert.IsFalse(await home.Admin.ExistsAsync("app.db", Ct));
        Assert.IsFalse(await stale.Admin.ExistsAsync("app.db", Ct));
        Assert.IsTrue(await home.Admin.ExistsAsync("other.db", Ct), "Only the one database is deleted.");
        Assert.IsNull(await store.GetProviderNameAsync("app.db", Ct));
    }

    [TestMethod]
    public async Task Delete_WhenTheBackendHoldingTheDatabaseIsUnavailable_FailsAndKeepsTheBinding()
    {
        var home = new ConfigurableProvider { Name = "home", RequiredBuild = BsqliteEngineBuild.Synchronous };
        var store = new BsqliteInMemoryStorageBindingStore();
        await store.SetProviderNameAsync("app.db", "home", Ct);
        await using var database = new BsqliteDatabase(
            Factory(store, new RecordingTransportFactory(), s => s.Prefer("home"), home),
            "app.db");
        await database.OpenAsync(Ct);
        home.IsAvailable = false;

        await Assert.ThrowsExactlyAsync<BsqliteStorageUnavailableException>(() => database.DeleteAsync(Ct));

        Assert.AreEqual("home", await store.GetProviderNameAsync("app.db", Ct));
    }

    /// <summary>
    /// Every backend is asked before any is touched: a copy on a backend registered ahead of the unreachable
    /// one that holds the database is still there after the delete fails.
    /// </summary>
    [TestMethod]
    public async Task Delete_ThatFailsOnTheBoundBackend_TouchesNoOtherCopyFirst()
    {
        var elsewhere = new PersistentTestProvider("elsewhere");
        var home = new ConfigurableProvider { Name = "home", RequiredBuild = BsqliteEngineBuild.Synchronous, IsAvailable = false };
        await elsewhere.Admin.ImportAsync("app.db", SqliteImages.Valid(), Ct);
        var store = new BsqliteInMemoryStorageBindingStore();
        await store.SetProviderNameAsync("app.db", "home", Ct);
        await using var database = new BsqliteDatabase(
            Factory(store, new RecordingTransportFactory(), s => s.Prefer("home"), elsewhere, home),
            "app.db");

        await Assert.ThrowsExactlyAsync<BsqliteStorageUnavailableException>(() => database.DeleteAsync(Ct));

        Assert.IsTrue(await elsewhere.Admin.ExistsAsync("app.db", Ct));
        Assert.AreEqual("home", await store.GetProviderNameAsync("app.db", Ct));
    }

    /// <summary>
    /// A window closed while a page still waits for its database: the open finishes after disposal began,
    /// and the session it leaves - on a device, one of the sessions every other window's admin operations
    /// wait for - must not outlive the database it was opened for.
    /// </summary>
    [TestMethod]
    public async Task Dispose_WhileAnOpenIsUnderWay_DisposesTheSessionThatOpenLeaves()
    {
        var finishOpen = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var transports = new RecordingTransportFactory(() => new ScriptedTransport { OpenWaitsFor = finishOpen.Task });
        var database = new BsqliteDatabase(
            Factory(new BsqliteInMemoryStorageBindingStore(), transports, s => s.Prefer("opfs"), Available("opfs")),
            "app.db");

        var opening = database.OpenAsync(Ct).AsTask();
        var disposing = database.DisposeAsync().AsTask();
        finishOpen.SetResult();
        await Task.WhenAll(opening.ContinueWith(_ => { }, TaskScheduler.Default), disposing);

        Assert.IsTrue(transports.Last.Disposed);
        Assert.IsFalse(database.IsOpen);
        await Assert.ThrowsExactlyAsync<ObjectDisposedException>(() => database.OpenAsync(Ct).AsTask());
    }

    /// <summary>
    /// An import cut short has already overwritten the database: it is put back as it was, and the session the
    /// application had open is reopened on it - not left closed with nothing announcing it.
    /// </summary>
    [TestMethod]
    public async Task Restore_ThatFailsPartWay_PutsTheDatabaseBack_AndReopensIt()
    {
        var home = new PersistentTestProvider("home");
        var original = SqliteImages.Valid(fill: 6);
        await home.Admin.ImportAsync("app.db", original, Ct);
        var store = new BsqliteInMemoryStorageBindingStore();
        await store.SetProviderNameAsync("app.db", "home", Ct);
        await using var database = new BsqliteDatabase(
            Factory(store, new RecordingTransportFactory(), s => s.Prefer("home"), home),
            "app.db");
        await database.OpenAsync(Ct);
        var opened = 0;
        database.Opened += () => { opened++; return Task.CompletedTask; };
        home.TearNextImport = true;

        await Assert.ThrowsExactlyAsync<BsqliteQuotaExceededException>(
            () => database.RestoreAsync(SqliteImages.Valid(fill: 7, pages: 2), Ct));

        CollectionAssert.AreEqual(original, await home.Admin.ExportAsync("app.db", Ct));
        Assert.IsTrue(database.IsOpen);
        Assert.AreEqual(1, opened);
    }

    /// <summary>A restore that fails where there was no database yet leaves none - not half of the backup.</summary>
    [TestMethod]
    public async Task Restore_ThatFailsWhereNoDatabaseWas_LeavesNone()
    {
        var home = new PersistentTestProvider("home") { TearNextImport = true };
        await using var database = new BsqliteDatabase(
            Factory(new BsqliteInMemoryStorageBindingStore(), new RecordingTransportFactory(), s => s.Prefer("home"), home),
            "app.db");

        await Assert.ThrowsExactlyAsync<BsqliteQuotaExceededException>(() => database.RestoreAsync(SqliteImages.Valid(), Ct));

        Assert.IsFalse(await home.Admin.ExistsAsync("app.db", Ct));
        Assert.IsFalse(database.IsOpen, "A database that was not open is not opened.");
    }

    /// <summary>
    /// Where a closed database lives is resolved, not learned by opening it - no worker, engine or binding -
    /// so the only session the restore opens is the one it leaves open.
    /// </summary>
    [TestMethod]
    public async Task Restore_OfADatabaseNotYetOpen_OpensOnlyTheSessionItLeaves()
    {
        var home = new PersistentTestProvider("home");
        var transports = new RecordingTransportFactory();
        await using var database = new BsqliteDatabase(
            Factory(new BsqliteInMemoryStorageBindingStore(), transports, s => s.Prefer("home"), home),
            "app.db");

        await database.RestoreAsync(SqliteImages.Valid(), Ct);

        Assert.HasCount(1, transports.Created);
        Assert.IsFalse(transports.Last.Disposed);
    }

    /// <summary>
    /// Work that fails still finds the database reopened, and a token cancelled meanwhile does not stop that
    /// reopen - the database was open before the call, and the application expects it open after.
    /// </summary>
    [TestMethod]
    public async Task WhileClosed_WorkThatFails_StillReopensTheDatabase()
    {
        await using var database = new BsqliteDatabase(
            Factory(new BsqliteInMemoryStorageBindingStore(), new RecordingTransportFactory(), s => s.Prefer("opfs"), Available("opfs")),
            "app.db");
        await database.OpenAsync(Ct);
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(Ct);

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => database.WhileClosedAsync(_ =>
        {
            cancellation.Cancel();
            throw new InvalidOperationException("The work failed.");
        }, cancellation.Token));

        Assert.IsTrue(database.IsOpen);
    }

    /// <summary>
    /// Disposing from a Closing handler - or from WhileClosedAsync's work - is disposing from code that runs
    /// holding the gate disposal would wait for: it returns, and the database ends closed and disposed.
    /// </summary>
    [TestMethod]
    public async Task Dispose_FromAClosingHandler_OrFromWorkWhileClosed_DoesNotDeadlock()
    {
        var transports = new RecordingTransportFactory();
        var fromClosing = new BsqliteDatabase(
            Factory(new BsqliteInMemoryStorageBindingStore(), transports, s => s.Prefer("opfs"), Available("opfs")),
            "app.db");
        await fromClosing.OpenAsync(Ct);
        fromClosing.Closing += async () => await fromClosing.DisposeAsync();

        await fromClosing.CloseAsync(Ct).WaitAsync(TimeSpan.FromSeconds(10), Ct);

        Assert.IsTrue(transports.Last.Disposed);
        await Assert.ThrowsExactlyAsync<ObjectDisposedException>(() => fromClosing.OpenAsync(Ct).AsTask());

        var fromWork = new BsqliteDatabase(
            Factory(new BsqliteInMemoryStorageBindingStore(), transports, s => s.Prefer("opfs"), Available("opfs")),
            "other.db");
        await fromWork.OpenAsync(Ct);

        await fromWork.WhileClosedAsync(async _ => await fromWork.DisposeAsync(), Ct).WaitAsync(TimeSpan.FromSeconds(10), Ct);

        Assert.IsFalse(fromWork.IsOpen, "A disposed database is not reopened.");
        Assert.IsTrue(transports.Created.TrueForAll(created => created.Transport.Disposed));
    }

    /// <summary>
    /// A backend that cannot take the name at all - the file system refuses 'CON.db', a browser backend keeps
    /// it - holds no copy: the delete goes on everywhere else instead of stopping part way.
    /// </summary>
    [TestMethod]
    public async Task Delete_SkipsABackendThatCannotTakeTheName_AndFinishes()
    {
        var home = new PersistentTestProvider("home");
        var picky = new PersistentTestProvider("picky", refusesNames: true);
        var stale = new PersistentTestProvider("stale");
        await home.Admin.ImportAsync("app.db", SqliteImages.Valid(fill: 1), Ct);
        await stale.Admin.ImportAsync("app.db", SqliteImages.Valid(fill: 2), Ct);
        var store = new BsqliteInMemoryStorageBindingStore();
        await store.SetProviderNameAsync("app.db", "home", Ct);
        await using var database = new BsqliteDatabase(
            Factory(store, new RecordingTransportFactory(), s => s.Prefer("home").Fallback("picky").Fallback("stale"), home, picky, stale),
            "app.db");

        await database.DeleteAsync(Ct);

        Assert.IsFalse(await home.Admin.ExistsAsync("app.db", Ct));
        Assert.IsFalse(await stale.Admin.ExistsAsync("app.db", Ct));
        Assert.IsNull(await store.GetProviderNameAsync("app.db", Ct));
    }

    private enum DeleteBehavior
    {
        Deletes,

        /// <summary>Throws with the database untouched - an OPFS file another tab holds open.</summary>
        FailsLeavingIt,

        /// <summary>Removes the database, then throws - a related file that could not go.</summary>
        FailsAfterDeleting,
    }

    /// <summary>
    /// A persistent, available backend with a real (in-memory) admin surface, so a migration can be
    /// run against it. Optionally refuses every import, which is what an origin out of quota does,
    /// and fails deletes the ways <see cref="DeleteBehavior"/> lists.
    /// </summary>
    private sealed class PersistentTestProvider(
        string name,
        bool refusesImports = false,
        DeleteBehavior deletes = DeleteBehavior.Deletes,
        bool refusesNames = false)
        : IBsqliteStorageProvider
    {
        public string Name { get; } = name;

        public BsqliteStorageCapabilities Capabilities { get; } = new()
        {
            RequiredBuild = BsqliteEngineBuild.Synchronous,
            IsPersistent = true,
        };

        public BsqliteJsModule? VfsModule => null;

        public IBsqliteStorageAdmin Admin { get; } = new MemoryAdmin(refusesImports, deletes, refusesNames);

        /// <summary>The next import writes half the image and then fails - a quota hit part way.</summary>
        public bool TearNextImport
        {
            get => ((MemoryAdmin)Admin).TearNextImport;
            set => ((MemoryAdmin)Admin).TearNextImport = value;
        }

        public ValueTask<BsqliteProbeResult> ProbeAsync(CancellationToken cancellationToken = default)
            => ValueTask.FromResult(BsqliteProbeResult.Available());

        private sealed class MemoryAdmin(bool refusesImports, DeleteBehavior deletes, bool refusesNames) : IBsqliteStorageAdmin
        {
            private readonly Dictionary<string, byte[]> _images = new(StringComparer.Ordinal);

            public bool TearNextImport { get; set; }

            public ValueTask<bool> ExistsAsync(string databaseName, CancellationToken cancellationToken = default)
                => refusesNames
                    ? throw new ArgumentException($"'{databaseName}' is not a name this backend can keep.", nameof(databaseName))
                    : ValueTask.FromResult(_images.ContainsKey(databaseName));

            public ValueTask<IReadOnlyList<string>> ListAsync(CancellationToken cancellationToken = default)
                => ValueTask.FromResult<IReadOnlyList<string>>([.. _images.Keys]);

            public ValueTask DeleteAsync(string databaseName, CancellationToken cancellationToken = default)
            {
                if (refusesNames)
                {
                    throw new ArgumentException($"'{databaseName}' is not a name this backend can keep.", nameof(databaseName));
                }

                if (deletes is DeleteBehavior.FailsLeavingIt)
                {
                    throw new IOException("The file is in use.");
                }

                _images.Remove(databaseName);

                return deletes is DeleteBehavior.FailsAfterDeleting
                    ? throw new IOException("The journal is in use.")
                    : ValueTask.CompletedTask;
            }

            public ValueTask<byte[]> ExportAsync(string databaseName, CancellationToken cancellationToken = default)
                => _images.TryGetValue(databaseName, out var image)
                    ? ValueTask.FromResult(image.ToArray())
                    : throw new FileNotFoundException(databaseName);

            public ValueTask ImportAsync(
                string databaseName,
                ReadOnlyMemory<byte> contents,
                CancellationToken cancellationToken = default)
            {
                if (refusesImports)
                {
                    throw new BsqliteQuotaExceededException("The origin is out of storage.");
                }

                if (TearNextImport)
                {
                    TearNextImport = false;
                    _images[databaseName] = contents[..(contents.Length / 2)].ToArray();
                    throw new BsqliteQuotaExceededException("The origin ran out of storage part way.");
                }

                _images[databaseName] = contents.ToArray();
                return ValueTask.CompletedTask;
            }
        }
    }

    private static ConfigurableProvider Available(
        string name,
        BsqliteEngineBuild build = BsqliteEngineBuild.Synchronous,
        BsqliteJsModule? vfs = null)
        => new()
        {
            Name = name,
            RequiredBuild = build,
            Vfs = vfs,
        };

    private static BsqliteSessionFactory Factory(
        BsqliteInMemoryStorageBindingStore store,
        IBsqliteTransportFactory transports,
        Action<BsqliteStorageSelectionBuilder> configure,
        params IBsqliteStorageProvider[] providers)
        => new(
            new BsqliteStorageProviderResolver(providers, store),
            transports,
            BsqliteStorageSelectionBuilder.Create(configure),
            store);
}
