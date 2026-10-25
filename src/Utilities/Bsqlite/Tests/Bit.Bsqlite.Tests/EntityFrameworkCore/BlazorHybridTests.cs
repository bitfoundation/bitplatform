using Bit.Bsqlite.Data;
using Bit.Bsqlite.Interop;
using Bit.Bsqlite.Storage;
using Bit.Bsqlite.Storage.FileSystem;
using Bit.Bsqlite.Storage.IndexedDb;
using Bit.Bsqlite.Storage.Opfs;
using Bit.Bsqlite.Storage.Tests;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Bit.Bsqlite.EntityFrameworkCore.Tests;

/// <summary>
/// A Blazor Hybrid app registers Bsqlite exactly as its WebAssembly client does - browser backends, a
/// preference among them, a database, a context - and its databases land on the device. Composed here on
/// desktop .NET, which runs natively the way a Hybrid app does, with the host set to Blazor Hybrid.
/// </summary>
[TestClass]
public sealed class BlazorHybridTests
{
    public TestContext TestContext { get; set; } = null!;

    private CancellationToken Ct => TestContext.CancellationToken;

    private readonly TemporaryDirectory _root = new();

    [TestCleanup]
    public void DeleteRoot() => _root.Dispose();

    /// <summary>The registration a WebAssembly client uses, word for word, plus where the test keeps its files.</summary>
    private ServiceProvider BuildApp(string databaseName = "app.db")
    {
        var services = new ServiceCollection();
        services.AddBitBsqliteServices(sqlite =>
        {
            sqlite.Host = BsqliteHostKind.BlazorHybrid;
            sqlite
                .AddStorageProvider<BsqliteOpfsStorageProvider>()
                .AddStorageProvider<BsqliteIndexedDbStorageProvider>()
                .UseStorage(s => s
                    .Prefer(BsqliteOpfsStorageProvider.ProviderName)
                    .Fallback(BsqliteIndexedDbStorageProvider.ProviderName))
                .ConfigureFileSystemStorage(o => o.RootDirectory = _root.Path)
                .AddDatabase(databaseName);
        });

        services.AddBitBsqliteDbContext<ProductContext>(options =>
        {
            options.Schema = BsqliteSchemaStrategy.EnsureCreated;
#if NET9_0_OR_GREATER
            options.Configure = (_, builder) => builder.ConfigureWarnings(w =>
                w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
#endif
        });

        return services.BuildServiceProvider();
    }

    [TestMethod]
    public async Task SameRegistration_KeepsTheDatabaseOnTheDevice_WithoutAJavaScriptRuntime()
    {
        await using var app = BuildApp();
        var factory = app.GetRequiredService<IBsqliteDbContextFactory<ProductContext>>();

        await using (var db = await factory.CreateDbContextAsync(Ct))
        {
            db.Categories.Add(new Category { Name = "Tools" });
            await db.SaveChangesAsync(Ct);
        }

        var resolution = factory.Database.Session!.Resolution;
        Assert.AreEqual(BsqliteFileSystemStorageProvider.ProviderName, resolution.Provider.Name);
        Assert.IsTrue(resolution.IsFirstChoice);
        Assert.IsTrue(File.Exists(Path.Combine(_root.Path, "app.db")));
    }

    [TestMethod]
    public async Task Data_SurvivesTheAppRestarting()
    {
        await using (var first = BuildApp())
        {
            await using var db = await first.GetRequiredService<IBsqliteDbContextFactory<ProductContext>>().CreateDbContextAsync(Ct);
            db.Categories.Add(new Category { Name = "Kept" });
            await db.SaveChangesAsync(Ct);
        }

        await using var second = BuildApp();
        var factory = second.GetRequiredService<IBsqliteDbContextFactory<ProductContext>>();
        await using var reopened = await factory.CreateDbContextAsync(Ct);

        Assert.AreEqual("Kept", (await reopened.Categories.SingleAsync(Ct)).Name);

        // Bound on the device, so the restart did not have to go looking for it.
        Assert.IsTrue(factory.Database.Session!.Resolution.WasDecidedByExistingData);
        Assert.IsFalse(factory.Database.Session.Resolution.WasFoundWithoutBinding);
    }

    [TestMethod]
    public async Task FileSystemBackend_IsRegisteredOnce_NextToTheBrowserOnes()
    {
        await using var app = BuildApp();

        var names = app.GetServices<IBsqliteStorageProvider>().Select(provider => provider.Name).ToArray();

        CollectionAssert.AreEquivalent(new[] { "opfs", "indexeddb", "file-system" }, names);
    }

    /// <summary>Each module of an app may register its own database, each with its own AddBitBsqliteServices call.</summary>
    [TestMethod]
    public async Task RegisteringTwice_KeepsOneFileSystemBackend()
    {
        var services = new ServiceCollection();
        services.AddBitBsqliteServices(sqlite =>
        {
            sqlite.Host = BsqliteHostKind.BlazorHybrid;
            sqlite
                .AddStorageProvider<BsqliteOpfsStorageProvider>()
                .UseStorage(s => s.Prefer(BsqliteOpfsStorageProvider.ProviderName))
                .ConfigureFileSystemStorage(o => o.RootDirectory = _root.Path)
                .AddDatabase("first.db");
        });
        services.AddBitBsqliteServices(sqlite =>
        {
            sqlite.Host = BsqliteHostKind.BlazorHybrid;
            sqlite
                .UseStorage(s => s.Prefer(BsqliteOpfsStorageProvider.ProviderName))
                .AddDatabase("second.db");
        });

        await using var app = services.BuildServiceProvider();

        Assert.AreEqual(1, app.GetServices<IBsqliteStorageProvider>().Count(provider => provider.Name == "file-system"));
        foreach (var databaseName in new[] { "first.db", "second.db" })
        {
            var session = await app.GetRequiredKeyedService<BsqliteDatabase>(databaseName).OpenAsync(Ct);
            Assert.AreEqual("file-system", session.Resolution.Provider.Name);
        }
    }

    /// <summary>
    /// A file system backend the app registers itself - through a factory, whose product registration cannot see -
    /// takes the place of the one a Blazor Hybrid app gets on its own, rather than colliding with it.
    /// </summary>
    [TestMethod]
    public async Task FileSystemBackendRegisteredThroughAFactory_TakesTheDefaultsPlace()
    {
        using var ownRoot = new TemporaryDirectory();
        var services = new ServiceCollection();
        services.AddBitBsqliteServices(sqlite =>
        {
            sqlite.Host = BsqliteHostKind.BlazorHybrid;
            sqlite
                .AddStorageProvider(_ => new BsqliteFileSystemStorageProvider(new() { RootDirectory = ownRoot.Path }))
                .UseStorage(s => s.Prefer(BsqliteFileSystemStorageProvider.ProviderName))
                .ConfigureFileSystemStorage(o => o.RootDirectory = _root.Path)
                .AddDatabase("app.db");
        });

        await using var app = services.BuildServiceProvider();
        var session = await app.GetRequiredService<BsqliteDatabase>().OpenAsync(Ct);

        Assert.AreEqual("file-system", session.Resolution.Provider.Name);
        Assert.IsTrue(File.Exists(Path.Combine(ownRoot.Path, "app.db")));
        Assert.IsFalse(File.Exists(Path.Combine(_root.Path, "app.db")));
        await app.GetRequiredService<BsqliteDatabase>().CloseAsync(Ct);
    }

    /// <summary>
    /// What makes a desktop process a Blazor Hybrid app is its web view's JavaScript runtime, not an assembly it
    /// ships: a server that references a library built on the web view keeps its users' databases in their browsers.
    /// </summary>
    [TestMethod]
    public void DesktopProcess_WithoutTheWebViewsJSRuntime_IsNotBlazorHybrid()
    {
        var services = new ServiceCollection()
            .AddSingleton<Microsoft.JSInterop.IJSRuntime>(new Bit.Bsqlite.Interop.Tests.ScriptedJsRuntime())
            .BuildServiceProvider();

        Assert.AreEqual(BsqliteHostKind.Other, BsqliteHost.Of(services));
    }

    /// <summary>
    /// A Blazor Hybrid app that ran Bsqlite in its web view before kept its database on a browser backend there.
    /// Its data is opened where it is - not abandoned for an empty file - and the device ranks above, to move to.
    /// </summary>
    [TestMethod]
    public async Task DatabaseKeptInTheWebViewBefore_IsStillOpenedThere()
    {
        var browser = new FakeStorageProvider("browser-backend");
        var bindings = new BsqliteInMemoryStorageBindingStore();
        await bindings.SetProviderNameAsync("app.db", "browser-backend", Ct);
        var resolver = new BsqliteStorageProviderResolver(
            [browser, new BsqliteFileSystemStorageProvider(new() { RootDirectory = _root.Path })],
            bindings,
            BsqliteHostKind.BlazorHybrid);

        var resolution = await resolver.ResolveAsync(
            "app.db",
            BsqliteStorageSelectionBuilder.Create(s => s.Prefer("browser-backend")),
            Ct);

        Assert.AreEqual("browser-backend", resolution.Provider.Name);
        Assert.IsTrue(resolution.WasDecidedByExistingData);
        Assert.AreEqual("file-system", resolution.BetterProviderAvailable?.Name);
    }

    [TestMethod]
    public async Task BrowserBackends_AreNotProbed_AndSayWhy()
    {
        var browser = new FakeStorageProvider("browser-backend");
        var resolver = new BsqliteStorageProviderResolver(
            [browser, new BsqliteFileSystemStorageProvider(new() { RootDirectory = _root.Path })],
            new BsqliteInMemoryStorageBindingStore(),
            BsqliteHostKind.BlazorHybrid);

        var resolution = await resolver.ResolveAsync(
            "app.db",
            BsqliteStorageSelectionBuilder.Create(s => s.Prefer("browser-backend")),
            Ct);

        Assert.AreEqual("file-system", resolution.Provider.Name);
        Assert.AreEqual(0, browser.ProbeCount);
        Assert.AreEqual(BsqliteStorageCandidateStatus.Selected, resolution.Attempts[0].Status);
        Assert.AreEqual(BsqliteStorageCandidateStatus.NotProbed, resolution.Attempts[1].Status);
    }

    [TestMethod]
    public async Task OnlyBrowserBackends_InAHybridApp_FailWithTheReason()
    {
        var resolver = new BsqliteStorageProviderResolver(
            [new FakeStorageProvider("browser-backend")],
            new BsqliteInMemoryStorageBindingStore(),
            BsqliteHostKind.BlazorHybrid);

        var failure = await Assert.ThrowsExactlyAsync<BsqliteStorageUnavailableException>(() => resolver
            .ResolveAsync("app.db", BsqliteStorageSelectionBuilder.Create(s => s.Prefer("browser-backend")), Ct)
            .AsTask());

        Assert.Contains("Blazor Hybrid", Assert.ContainsSingle(failure.Attempts).Probe!.UnavailableReason!);
    }

    /// <summary>A server must not keep its users' databases: they live in their browsers.</summary>
    [TestMethod]
    public async Task FileSystemBackend_IsNotOffered_OutsideBlazorHybrid()
    {
        var resolver = new BsqliteStorageProviderResolver(
            [new BsqliteFileSystemStorageProvider(new() { RootDirectory = _root.Path })],
            new BsqliteInMemoryStorageBindingStore(),
            BsqliteHostKind.Other);

        var failure = await Assert.ThrowsExactlyAsync<BsqliteStorageUnavailableException>(() => resolver
            .ResolveAsync("app.db", BsqliteStorageSelectionBuilder.Create(s => s.Prefer("file-system")), Ct)
            .AsTask());

        Assert.Contains("Blazor Hybrid", Assert.ContainsSingle(failure.Attempts).Probe!.UnavailableReason!);
        Assert.IsFalse(Directory.Exists(_root.Path), "The backend was probed on a host it cannot serve.");
    }

    [TestMethod]
    public async Task FileSystemBackend_IsNotOffered_InTheBrowser()
    {
        var resolver = new BsqliteStorageProviderResolver(
            [new BsqliteFileSystemStorageProvider(new() { RootDirectory = _root.Path })],
            new BsqliteInMemoryStorageBindingStore(),
            BsqliteHostKind.Browser);

        var failure = await Assert.ThrowsExactlyAsync<BsqliteStorageUnavailableException>(() => resolver
            .ResolveAsync("app.db", BsqliteStorageSelectionBuilder.Create(s => s.Prefer("file-system")), Ct)
            .AsTask());

        Assert.Contains("in the browser", Assert.ContainsSingle(failure.Attempts).Probe!.UnavailableReason!);
    }

    /// <summary>Two windows of one app are two scopes on one file - two tabs, in the browser's terms.</summary>
    [TestMethod]
    public async Task WriteInOneWindow_RerunsALiveQueryInTheOther()
    {
        await using var app = BuildApp();
        await using var firstWindow = app.CreateAsyncScope();
        await using var secondWindow = app.CreateAsyncScope();

        var first = firstWindow.ServiceProvider.GetRequiredService<IBsqliteDbContextFactory<ProductContext>>();
        var second = secondWindow.ServiceProvider.GetRequiredService<IBsqliteDbContextFactory<ProductContext>>();

        await using var watching = await second.CreateDbContextAsync(Ct);
        await using var live = watching.Categories.AsLiveQuery(second.Database.Session!.Connection);
        Assert.IsEmpty(await live.RefreshAsync(Ct));

        await using (var writing = await first.CreateDbContextAsync(Ct))
        {
            writing.Categories.Add(new Category { Name = "From the other window" });
            await writing.SaveChangesAsync(Ct);
        }

        var deadline = DateTime.UtcNow.AddSeconds(5);
        while ((live.Current?.Count ?? 0) == 0 && DateTime.UtcNow < deadline)
        {
            await Task.Delay(20, Ct);
        }

        Assert.AreEqual("From the other window", Assert.ContainsSingle(live.Current!).Name);
    }

    /// <summary>
    /// A Bit.Besql app kept its database on the device as a plain SQLite file - in WAL mode, since stock EF Core
    /// created it. Pointing the backend at that folder, under the same name, opens the users' data as it is, and
    /// takes the file back to the rollback journal Bsqlite runs every database in.
    /// </summary>
    [TestMethod]
    public async Task ExistingSqliteFile_OpensInPlace()
    {
        var existing = Path.Combine(_root.Path, "App_Data", "AppOffline.db");
        Directory.CreateDirectory(Path.GetDirectoryName(existing)!);
        await using (var connection = new SqliteConnection($"Data Source={existing};Pooling=False"))
        {
            await connection.OpenAsync(Ct);
            await using var command = connection.CreateCommand();
            command.CommandText = "PRAGMA journal_mode = WAL; CREATE TABLE notes (text TEXT NOT NULL); INSERT INTO notes VALUES ('written by Besql');";
            await command.ExecuteNonQueryAsync(Ct);
        }

        await using var app = BuildApp("App_Data/AppOffline.db");
        var database = app.GetRequiredService<BsqliteDatabase>();
        var session = await database.OpenAsync(Ct);

        await using var query = session.Connection.CreateCommand();
        query.CommandText = "SELECT text FROM notes";
        Assert.AreEqual("written by Besql", await query.ExecuteScalarAsync(Ct));
        Assert.IsTrue(session.Resolution.WasFoundWithoutBinding);

        query.CommandText = "PRAGMA journal_mode";
        Assert.AreEqual("delete", await query.ExecuteScalarAsync(Ct));
    }
}
