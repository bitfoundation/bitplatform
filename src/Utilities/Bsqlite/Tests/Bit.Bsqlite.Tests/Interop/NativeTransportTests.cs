using System.Diagnostics;
using Bit.Bsqlite.Data;
using Bit.Bsqlite.Storage;
using Bit.Bsqlite.Storage.FileSystem;
using Bit.Bsqlite.Storage.Tests;
using Microsoft.Data.Sqlite;

namespace Bit.Bsqlite.Interop.Tests;

/// <summary>The native transport's lifecycle, and which transport a backend gets on each host.</summary>
[TestClass]
public sealed class NativeTransportTests
{
    public TestContext TestContext { get; set; } = null!;

    private CancellationToken Ct => TestContext.CancellationToken;

    private readonly TemporaryDirectory _root = new();

    [TestCleanup]
    public void DeleteRoot() => _root.Dispose();

    /// <summary>
    /// A live query's refresh running when its window closes: the connection must outlive the batch, or the batch
    /// fails half way on a closed handle.
    /// </summary>
    [TestMethod]
    public async Task Dispose_WaitsForTheBatchInProgress()
    {
        using var entered = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        var transport = new BlockingTransport(entered, release);
        await transport.OpenAsync("blocking", Ct);

        var batch = Task.Run(() => transport.ExecuteAsync(
            [new BsqliteCommandRequest { CommandText = "SELECT 1", ResultKind = BsqliteResultKind.Scalar }],
            Ct), Ct);
        Assert.IsTrue(entered.Wait(TimeSpan.FromSeconds(10), Ct));

        var disposal = transport.DisposeAsync().AsTask();
        await Task.Delay(100, Ct);
        Assert.IsFalse(disposal.IsCompleted, "The connection was closed under the batch.");

        release.Set();

        Assert.AreEqual(1L, Assert.ContainsSingle(Assert.ContainsSingle(await batch).Rows)[0]);
        await disposal;
        Assert.IsNull(transport.Connection);
    }

    /// <summary>
    /// A window closing while its last write is still running: until the file is closed it is still that window's,
    /// so the admin surface must not delete or replace it under the write.
    /// </summary>
    [TestMethod]
    public async Task Dispose_KeepsTheFileFromTheAdmin_UntilItIsClosed()
    {
        var provider = new BsqliteFileSystemStorageProvider(new() { RootDirectory = _root.Path });
        using var entered = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        var transport = new BlockingTransport(entered, release, $"Data Source={provider.GetDatabasePath("app.db")};Pooling=False");
        await transport.OpenAsync("app.db", Ct);

        var batch = Task.Run(() => transport.ExecuteAsync(
            [new BsqliteCommandRequest { CommandText = "CREATE TABLE t (x)" }],
            Ct), Ct);
        Assert.IsTrue(entered.Wait(TimeSpan.FromSeconds(10), Ct));
        var disposal = transport.DisposeAsync().AsTask();

        var error = await Assert.ThrowsExactlyAsync<IOException>(async () => await provider.Admin.DeleteAsync("app.db", Ct));
        Assert.Contains("is open in 1 session", error.Message);

        release.Set();
        await batch;
        await disposal;

        await provider.Admin.DeleteAsync("app.db", Ct);
        Assert.IsFalse(await provider.Admin.ExistsAsync("app.db", Ct));
    }

    [TestMethod]
    public async Task Open_AfterDispose_Throws()
    {
        var transport = new BsqliteNativeTransport(new BsqliteFileSystemStorageProvider(new() { RootDirectory = _root.Path }));
        await transport.OpenAsync("app.db", Ct);
        await transport.DisposeAsync();

        await Assert.ThrowsExactlyAsync<ObjectDisposedException>(() => transport.OpenAsync("app.db", Ct));
    }

    /// <summary>
    /// Opens racing on one transport open the file once: a second connection would leak, and stay among the file's
    /// sessions after disposal, so the admin surface would refuse the file for the life of the process.
    /// </summary>
    [TestMethod]
    public async Task ConcurrentOpens_OpenTheFileOnce()
    {
        var provider = new BsqliteFileSystemStorageProvider(new() { RootDirectory = _root.Path });
        var transport = new BsqliteNativeTransport(provider);

        await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => Task.Run(() => transport.OpenAsync("app.db", Ct), Ct)));
        await transport.DisposeAsync();

        await provider.Admin.DeleteAsync("app.db", Ct);
        Assert.IsFalse(await provider.Admin.ExistsAsync("app.db", Ct));
    }

    /// <summary>
    /// The writes have committed by the time a handler hears of them: a handler that throws must not turn the batch
    /// into a failure the caller would retry. In the browser a handler's exception does not reach the write either.
    /// </summary>
    [TestMethod]
    public async Task ThrowingTablesChangedHandler_DoesNotFailTheCommittedWrite()
    {
        await using var transport = new BsqliteNativeTransport(new BsqliteFileSystemStorageProvider(new() { RootDirectory = _root.Path }));
        await transport.OpenAsync("app.db", Ct);
        var heard = new List<string>();
        transport.TablesChanged += (_, _) => throw new InvalidOperationException("A handler's bug.");
        transport.TablesChanged += (_, e) => heard.AddRange(e.Tables);

        await transport.ExecuteAsync(
            [
                new BsqliteCommandRequest { CommandText = "CREATE TABLE t (x)" },
                new BsqliteCommandRequest { CommandText = "INSERT INTO t VALUES (1)" },
            ],
            Ct);

        var count = await transport.ExecuteAsync(
            [new BsqliteCommandRequest { CommandText = "SELECT COUNT(*) FROM t", ResultKind = BsqliteResultKind.Scalar }],
            Ct);
        Assert.AreEqual(1L, Assert.ContainsSingle(Assert.ContainsSingle(count).Rows)[0]);
        Assert.Contains("t", heard);
    }

    /// <summary>
    /// While another connection holds a file in WAL mode it cannot leave it, and waiting does not change that:
    /// the open goes ahead at once instead of retrying for a command's whole timeout.
    /// </summary>
    [TestMethod]
    public async Task Open_DoesNotWaitOnAFileHeldInWalModeElsewhere()
    {
        var provider = new BsqliteFileSystemStorageProvider(new() { RootDirectory = _root.Path });
        var path = provider.GetDatabasePath("app.db");
        Directory.CreateDirectory(_root.Path);

        await using var holder = new SqliteConnection($"Data Source={path};Pooling=False");
        await holder.OpenAsync(Ct);
        await using (var command = holder.CreateCommand())
        {
            command.CommandText = "PRAGMA journal_mode = WAL; CREATE TABLE t (x); INSERT INTO t VALUES (1);";
            await command.ExecuteNonQueryAsync(Ct);
        }

        await using var transport = new BsqliteNativeTransport(provider);
        var clock = Stopwatch.StartNew();
        await transport.OpenAsync("app.db", Ct);

        Assert.IsLessThan(TimeSpan.FromSeconds(5), clock.Elapsed);
        var mode = await transport.ExecuteAsync(
            [new BsqliteCommandRequest { CommandText = "PRAGMA journal_mode", ResultKind = BsqliteResultKind.Scalar }],
            Ct);
        Assert.AreEqual("wal", Assert.ContainsSingle(Assert.ContainsSingle(mode).Rows)[0]);
    }

    /// <summary>
    /// The transport follows where the backend runs on this host - the rule selection serves it by - not whether
    /// it implements <see cref="IBsqliteFileStorageProvider"/>: in the browser the native engine is not there.
    /// </summary>
    [TestMethod]
    public void BackendThatRunsInBothPlaces_GetsTheWorker_InTheBrowser()
    {
        var workers = new RecordingTransportFactory();
        var provider = new EverywhereProvider(_root.Path);

        var transport = new BsqliteTransportFactory(() => workers, new(), BsqliteHostKind.Browser).Create(provider);

        Assert.AreSame(workers.Last, transport);
    }

    [TestMethod]
    public async Task BackendThatRunsInBothPlaces_GetsTheNativeTransport_InBlazorHybrid()
    {
        var workers = new RecordingTransportFactory();
        var provider = new EverywhereProvider(_root.Path);

        await using var transport = new BsqliteTransportFactory(() => workers, new(), BsqliteHostKind.BlazorHybrid).Create(provider);

        Assert.IsInstanceOfType<BsqliteNativeTransport>(transport);
        Assert.IsEmpty(workers.Created);
    }

    /// <summary>A database a Blazor Hybrid app kept in its web view before is still opened there, by the worker.</summary>
    [TestMethod]
    public void BrowserBackend_GetsTheWorker_InBlazorHybrid()
    {
        var workers = new RecordingTransportFactory();

        var transport = new BsqliteTransportFactory(() => workers, new(), BsqliteHostKind.BlazorHybrid)
            .Create(new FakeStorageProvider("browser-backend"));

        Assert.AreSame(workers.Last, transport);
    }

    [TestMethod]
    public void NativeOnlyBackend_CannotBeOpened_InTheBrowser()
    {
        var factory = new BsqliteTransportFactory(() => new RecordingTransportFactory(), new(), BsqliteHostKind.Browser);

        var error = Assert.ThrowsExactly<InvalidOperationException>(() => factory.Create(
            new BsqliteFileSystemStorageProvider(new() { RootDirectory = _root.Path })));

        Assert.Contains("in the browser", error.Message);
    }

    /// <summary>
    /// A Blazor Hybrid app's database still on a browser backend runs in the web view's worker, where another web
    /// view may be moving it to the device at the same time - so its open takes the lock the browser takes.
    /// </summary>
    [TestMethod]
    public async Task DatabaseBoundToABrowserBackend_TakesTheOpenLock_InBlazorHybrid()
    {
        var workers = new LockingWorkers();
        var factory = new BsqliteTransportFactory(() => workers, new(), BsqliteHostKind.BlazorHybrid);
        var browser = new FakeStorageProvider("browser-backend");

        await using var held = await factory.AcquireOpenLockAsync("app.db", _ => ValueTask.FromResult<IBsqliteStorageProvider?>(browser), Ct);

        Assert.IsNotNull(held);
        AssertSequence.Equal(["app.db"], workers.Locked);
    }

    /// <summary>
    /// A database on the device, or a new one - which goes there - has nothing in the web view to take turns with,
    /// and its open never builds the worker factory, so it does not wait for the web view's <c>IJSRuntime</c>.
    /// </summary>
    [TestMethod]
    public async Task DatabaseOnTheDevice_TakesNoOpenLock_InBlazorHybrid()
    {
        var built = false;
        var factory = new BsqliteTransportFactory(() => { built = true; return new LockingWorkers(); }, new(), BsqliteHostKind.BlazorHybrid);
        var device = new BsqliteFileSystemStorageProvider(new() { RootDirectory = _root.Path });
        var everywhere = new EverywhereProvider(_root.Path);

        Assert.IsNull(await factory.AcquireOpenLockAsync("app.db", _ => ValueTask.FromResult<IBsqliteStorageProvider?>(device), Ct));
        Assert.IsNull(await factory.AcquireOpenLockAsync("app.db", _ => ValueTask.FromResult<IBsqliteStorageProvider?>(everywhere), Ct));
        Assert.IsNull(await factory.AcquireOpenLockAsync("app.db", _ => default, Ct));
        Assert.IsFalse(built);
    }

    /// <summary>In the browser every backend is a browser one, so the lock is taken without reading the binding.</summary>
    [TestMethod]
    public async Task EveryOpen_TakesTheOpenLock_InTheBrowser()
    {
        var workers = new LockingWorkers();
        var factory = new BsqliteTransportFactory(() => workers, new(), BsqliteHostKind.Browser);
        var asked = false;

        await using var held = await factory.AcquireOpenLockAsync("app.db", _ => { asked = true; return default; }, Ct);

        Assert.IsNotNull(held);
        Assert.IsFalse(asked);
        AssertSequence.Equal(["app.db"], workers.Locked);
    }

    /// <summary>A worker factory that records the open locks taken through it.</summary>
    private sealed class LockingWorkers : IBsqliteTransportFactory, IBsqliteOpenLock
    {
        public List<string> Locked { get; } = [];

        public IBsqliteTransport Create(IBsqliteStorageProvider provider) => throw new NotSupportedException();

        public Task<IAsyncDisposable?> AcquireOpenLockAsync(
            string databaseName,
            Func<CancellationToken, ValueTask<IBsqliteStorageProvider?>> boundProvider,
            CancellationToken cancellationToken)
        {
            Locked.Add(databaseName);
            return Task.FromResult<IAsyncDisposable?>(new Released());
        }

        private sealed class Released : IAsyncDisposable
        {
            public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        }
    }

    /// <summary>Holds its batch at the first statement until told to go on.</summary>
    private sealed class BlockingTransport(
        ManualResetEventSlim entered,
        ManualResetEventSlim release,
        string connectionString = "Data Source=:memory:")
        : BsqliteNativeTransport(connectionString, onOpenSql: null, registerEfFunctions: false)
    {
        protected override void OnExecuting(BsqliteCommandRequest request)
        {
            entered.Set();
            release.Wait();
        }
    }

    /// <summary>A backend that runs both in the browser's worker and where .NET runs natively.</summary>
    private sealed class EverywhereProvider(string root) : IBsqliteFileStorageProvider
    {
        public string Name => "everywhere";

        public BsqliteStorageCapabilities Capabilities { get; } = new()
        {
            RequiredBuild = BsqliteEngineBuild.Synchronous,
            IsPersistent = true,
            SupportedContexts = BsqliteExecutionContexts.DedicatedWorker | BsqliteExecutionContexts.NativeProcess,
        };

        public BsqliteJsModule? VfsModule => null;

        public IBsqliteStorageAdmin Admin => throw new NotSupportedException();

        public string GetDatabasePath(string databaseName) => Path.Combine(root, databaseName);

        public ValueTask<BsqliteProbeResult> ProbeAsync(CancellationToken cancellationToken = default)
            => ValueTask.FromResult(BsqliteProbeResult.Available());
    }
}
