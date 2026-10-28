using Bit.Bsqlite.Data;
using Bit.Bsqlite.EntityFrameworkCore;
using Bit.Bsqlite.Interop;
using Bit.Bsqlite.Storage;
using Bit.Bsqlite.Storage.InMemory;
using Bit.Bsqlite.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Bit.Bsqlite.EntityFrameworkCore.Tests;

/// <summary>
/// The registration surface an application actually calls: <c>AddBitBsqliteServices</c>,
/// <c>AddDatabase</c>, and <c>AddBitBsqliteDbContext</c>. The worker transport is replaced by
/// the in-process one so the whole composition runs on desktop .NET.
/// </summary>
[TestClass]
public sealed class DependencyInjectionTests
{
    public TestContext TestContext { get; set; } = null!;

    private CancellationToken Ct => TestContext.CancellationToken;

    private static ServiceProvider Build(
        Action<BsqliteDbContextOptions<ProductContext>>? configure = null,
        Action<BsqliteBuilder>? configureSqlite = null)
    {
        var services = new ServiceCollection();
        services.AddBitBsqliteServices(sqlite =>
        {
            sqlite
                .AddStorageProvider<BsqliteInMemoryStorageProvider>()
                .UseStorage(s => s.Prefer(BsqliteInMemoryStorageProvider.ProviderName))
                .UseBindingStore<BsqliteInMemoryStorageBindingStore>()
                .AddDatabase("di.db");
            configureSqlite?.Invoke(sqlite);
        });

        // Desktop has no worker; the in-process engine stands in for it.
        services.AddSingleton<IBsqliteTransportFactory>(new InProcessTransportFactory());

        services.AddBitBsqliteDbContext<ProductContext>(options =>
        {
            options.Schema = BsqliteSchemaStrategy.EnsureCreated;
            // EF Core 9 started refusing to migrate a model with pending changes.
            options.Configure = (_, builder) => builder.ConfigureWarnings(w =>
                w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
            configure?.Invoke(options);
        });

        return services.BuildServiceProvider();
    }

    [TestMethod]
    public async Task AddBitBsqliteServices_RegistersTheCompositionRoot()
    {
        await using var provider = Build();

        Assert.IsNotNull(provider.GetRequiredService<IBsqliteSessionFactory>());
        Assert.IsNotNull(provider.GetRequiredService<BsqliteStorageProviderResolver>());
        Assert.IsNotNull(provider.GetRequiredService<BsqliteStorageSelection>());
        Assert.AreEqual("di.db", provider.GetRequiredService<BsqliteDatabase>().Name);
        Assert.ContainsSingle(provider.GetServices<IBsqliteStorageProvider>());
    }

    [TestMethod]
    public void AddBitBsqliteServices_WithoutAPreference_FailsAtRegistration()
    {
        var services = new ServiceCollection();

        var error = Assert.ThrowsExactly<InvalidOperationException>(() =>
            services.AddBitBsqliteServices(sqlite => sqlite.AddStorageProvider<BsqliteInMemoryStorageProvider>()));

        Assert.Contains("UseStorage", error.Message);
    }

    [TestMethod]
    public async Task ContextFactory_OpensTheDatabase_PreparesTheSchemaOnce_AndHandsOutContexts()
    {
        var firstUse = 0;
        await using var provider = Build(o => o.OnFirstUse = (_, _) =>
        {
            firstUse++;
            return Task.CompletedTask;
        });

        var factory = provider.GetRequiredService<IBsqliteDbContextFactory<ProductContext>>();
        Assert.IsFalse(factory.Database.IsOpen);

        await using (var first = await factory.CreateDbContextAsync(Ct))
        {
            first.Categories.Add(new Category { Name = "Tools" });
            await first.SaveChangesAsync(Ct);
        }

        await using (var second = await factory.CreateDbContextAsync(Ct))
        {
            Assert.AreEqual(1, await second.Categories.CountAsync(Ct));
        }

        Assert.IsTrue(factory.Database.IsOpen);
        Assert.AreEqual(1, firstUse);

        // The generic EF factory interface resolves to the same instance.
        Assert.AreSame(factory, provider.GetRequiredService<IDbContextFactory<ProductContext>>());
    }

    [TestMethod]
    public async Task ContextFactory_SynchronousCreate_WorksOnceOpen_AndExplainsOtherwise()
    {
        await using var provider = Build();
        var factory = provider.GetRequiredService<IBsqliteDbContextFactory<ProductContext>>();

        var error = Assert.ThrowsExactly<InvalidOperationException>(() => factory.CreateDbContext());
        Assert.Contains("CreateDbContextAsync", error.Message);

        await using var _ = await factory.CreateDbContextAsync(Ct);
        using var sync = factory.CreateDbContext();
        Assert.IsNotNull(sync);
    }

    /// <summary>
    /// A session opened some other way has not had its schema step run, so a synchronous context on
    /// it would query tables that are not there yet - or a schema one deploy out of date.
    /// </summary>
    [TestMethod]
    public async Task ContextFactory_SynchronousCreate_RefusesASessionItHasNotPrepared()
    {
        await using var provider = Build();
        var factory = provider.GetRequiredService<IBsqliteDbContextFactory<ProductContext>>();
        await provider.GetRequiredService<BsqliteDatabase>().OpenAsync(Ct);

        var error = Assert.ThrowsExactly<InvalidOperationException>(() => factory.CreateDbContext());
        Assert.Contains("CreateDbContextAsync", error.Message);
        Assert.DoesNotContain("BsqliteDatabase.OpenAsync", error.Message);
    }

    /// <summary>
    /// What the steps load - a lazy-loaded assembly, say - has to be there before the first connection
    /// is created, and stays loaded when the database closes and reopens. A context factory opens
    /// through the same session factory, so it waits for them too.
    /// </summary>
    [TestMethod]
    public async Task BeforeFirstOpen_RunsItsStepsInOrder_OncePerScope_BeforeAnyDatabaseOpens()
    {
        var runs = new List<string>();
        var openWhileRunning = true;
        await using var provider = Build(configureSqlite: sqlite => sqlite
            .BeforeFirstOpen((sp, _) =>
            {
                runs.Add("first");
                openWhileRunning = sp.GetRequiredService<BsqliteDatabase>().IsOpen;
                return Task.CompletedTask;
            })
            .BeforeFirstOpen((_, _) =>
            {
                runs.Add("second");
                return Task.CompletedTask;
            }));

        var factory = provider.GetRequiredService<IBsqliteDbContextFactory<ProductContext>>();

        await using (var _ = await factory.CreateDbContextAsync(Ct))
        {
        }

        await factory.Database.CloseAsync(Ct);
        await factory.Database.OpenAsync(Ct);

        CollectionAssert.AreEqual(new[] { "first", "second" }, runs);
        Assert.IsFalse(openWhileRunning);

        // Another scope - another circuit, on a server - has services of its own, and runs them again.
        await using var scope = provider.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<BsqliteDatabase>().OpenAsync(Ct);

        CollectionAssert.AreEqual(new[] { "first", "second", "first", "second" }, runs);
    }

    [TestMethod]
    public async Task BeforeFirstOpen_AFailureLeavesTheDatabaseClosed_AndTheNextOpenRunsItAgain()
    {
        var runs = 0;
        await using var provider = Build(configureSqlite: sqlite => sqlite.BeforeFirstOpen((_, _) =>
        {
            if (++runs == 1)
            {
                throw new IOException("The download was cut short.");
            }

            return Task.CompletedTask;
        }));

        var database = provider.GetRequiredService<BsqliteDatabase>();

        await Assert.ThrowsExactlyAsync<IOException>(() => database.OpenAsync(Ct).AsTask());
        Assert.IsFalse(database.IsOpen);

        await database.OpenAsync(Ct);
        Assert.IsTrue(database.IsOpen);
        Assert.AreEqual(2, runs);
    }

    [TestMethod]
    public async Task TwoDatabases_TheContextIsBoundToTheOneItNames()
    {
        await using var provider = BuildTwoDatabases(o => o.DatabaseName = "app.db");

        var factory = provider.GetRequiredService<IBsqliteDbContextFactory<ProductContext>>();

        Assert.AreEqual("app.db", factory.Database.Name);
        Assert.AreSame(provider.GetRequiredKeyedService<BsqliteDatabase>("app.db"), factory.Database);
        Assert.AreEqual("cache.db", provider.GetRequiredKeyedService<BsqliteDatabase>("cache.db").Name);
    }

    /// <summary>
    /// The last registration would win, binding the context - and running its migrations - against
    /// a database that is not its own.
    /// </summary>
    [TestMethod]
    public async Task TwoDatabases_AContextThatNamesNeither_IsRefused()
    {
        await using var provider = BuildTwoDatabases(configure: null);

        var error = Assert.ThrowsExactly<InvalidOperationException>(
            () => provider.GetRequiredService<IBsqliteDbContextFactory<ProductContext>>());

        Assert.Contains("DatabaseName", error.Message);
        Assert.Contains("'cache.db'", error.Message);
    }

    [TestMethod]
    public void AddDatabase_TheSameNameTwice_IsRefused()
    {
        var services = new ServiceCollection();

        var error = Assert.ThrowsExactly<InvalidOperationException>(() => services.AddBitBsqliteServices(sqlite => sqlite
            .AddStorageProvider<BsqliteInMemoryStorageProvider>()
            .UseStorage(s => s.Prefer(BsqliteInMemoryStorageProvider.ProviderName))
            .AddDatabase("app.db")
            .AddDatabase("app.db")));

        Assert.Contains("already registered", error.Message);
    }

    private static ServiceProvider BuildTwoDatabases(Action<BsqliteDbContextOptions<ProductContext>>? configure)
    {
        var services = new ServiceCollection();
        services.AddBitBsqliteServices(sqlite => sqlite
            .AddStorageProvider<BsqliteInMemoryStorageProvider>()
            .UseStorage(s => s.Prefer(BsqliteInMemoryStorageProvider.ProviderName))
            .UseBindingStore<BsqliteInMemoryStorageBindingStore>()
            .AddDatabase("app.db")
            .AddDatabase("cache.db"));
        services.AddSingleton<IBsqliteTransportFactory>(new InProcessTransportFactory());
        services.AddBitBsqliteDbContext<ProductContext>(options => configure?.Invoke(options));
        return services.BuildServiceProvider();
    }

    [TestMethod]
    public async Task Database_Close_RaisesClosing_AndReopenPreparesAgain()
    {
        var firstUse = 0;
        await using var provider = Build(o => o.OnFirstUse = (_, _) =>
        {
            firstUse++;
            return Task.CompletedTask;
        });

        var database = provider.GetRequiredService<BsqliteDatabase>();
        var factory = provider.GetRequiredService<IBsqliteDbContextFactory<ProductContext>>();
        var closing = 0;
        var opened = 0;
        database.Closing += () => { closing++; return Task.CompletedTask; };
        database.Opened += () => { opened++; return Task.CompletedTask; };

        await using (var _ = await factory.CreateDbContextAsync(Ct))
        {
        }

        await database.CloseAsync(Ct);
        Assert.IsFalse(database.IsOpen);

        await using (var _ = await factory.CreateDbContextAsync(Ct))
        {
        }

        Assert.AreEqual(1, closing);
        Assert.AreEqual(2, opened);
        Assert.AreEqual(2, firstUse);
    }

    [TestMethod]
    public async Task Database_WhileClosed_RunsWorkWithTheSessionGone_ThenReopens()
    {
        await using var provider = Build();
        var database = provider.GetRequiredService<BsqliteDatabase>();
        await database.OpenAsync(Ct);

        var sawClosed = false;
        await database.WhileClosedAsync(_ =>
        {
            sawClosed = !database.IsOpen;
            return Task.CompletedTask;
        }, Ct);

        Assert.IsTrue(sawClosed);
        Assert.IsTrue(database.IsOpen);
    }

    /// <summary>
    /// The database stays closed for the whole of the work: an open from anywhere else - a page loading,
    /// an Opened handler still running from the last open - waits for the work, instead of reopening the
    /// database on the binding the work is about to change and leaving the session and the binding on
    /// two different backends.
    /// </summary>
    [TestMethod]
    public async Task Database_WhileClosed_HoldsAnOpenFromElsewhere_UntilTheWorkIsDone()
    {
        await using var provider = Build();
        var database = provider.GetRequiredService<BsqliteDatabase>();
        var before = await database.OpenAsync(Ct);

        var inWork = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var finishWork = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var whileClosed = database.WhileClosedAsync(async _ =>
        {
            inWork.SetResult();
            await finishWork.Task;
        }, Ct);

        await inWork.Task;
        var elsewhere = database.OpenAsync(Ct).AsTask();
        await Task.Delay(100, Ct);

        Assert.IsFalse(elsewhere.IsCompleted, "An open went ahead while the work was running.");
        Assert.IsFalse(database.IsOpen);

        finishWork.SetResult();
        await whileClosed;
        var after = await elsewhere;

        Assert.AreSame(database.Session, after);
        Assert.AreNotSame(before, after);
    }

    /// <summary>
    /// The work holds the database closed, so it cannot open it itself: that would wait on the work
    /// forever. It is told so instead.
    /// </summary>
    [TestMethod]
    public async Task Database_WhileClosed_RefusesAnOpenFromInsideTheWork()
    {
        await using var provider = Build();
        var database = provider.GetRequiredService<BsqliteDatabase>();
        await database.OpenAsync(Ct);

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() =>
            database.WhileClosedAsync(async ct => await database.OpenAsync(ct), Ct));

        // A failed work leaves the database closed, and the next open is an ordinary one.
        Assert.IsFalse(database.IsOpen);
        await database.OpenAsync(Ct);
        Assert.IsTrue(database.IsOpen);
    }

    private sealed class InProcessTransportFactory : IBsqliteTransportFactory
    {
        public IBsqliteTransport Create(IBsqliteStorageProvider provider) => new BsqliteInProcessTransport();
    }
}
