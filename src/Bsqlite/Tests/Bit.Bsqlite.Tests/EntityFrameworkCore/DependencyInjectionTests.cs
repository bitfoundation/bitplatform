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

    private static ServiceProvider Build(Action<BsqliteDbContextOptions<ProductContext>>? configure = null)
    {
        var services = new ServiceCollection();
        services.AddBitBsqliteServices(sqlite => sqlite
            .AddStorageProvider<BsqliteInMemoryStorageProvider>()
            .UseStorage(s => s.Prefer(BsqliteInMemoryStorageProvider.ProviderName))
            .UseBindingStore<BsqliteInMemoryStorageBindingStore>()
            .AddDatabase("di.db"));

        // Desktop has no worker; the in-process engine stands in for it.
        services.AddSingleton<IBsqliteTransportFactory>(new InProcessTransportFactory());

        services.AddBitBsqliteDbContext<ProductContext>(options =>
        {
            options.Schema = BsqliteSchemaStrategy.EnsureCreated;
#if NET9_0_OR_GREATER
            // EF Core 9 started refusing to migrate a model with pending changes; EF Core 8 has no such warning.
            options.Configure = (_, builder) => builder.ConfigureWarnings(w =>
                w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
#endif
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

    private sealed class InProcessTransportFactory : IBsqliteTransportFactory
    {
        public IBsqliteTransport Create(IBsqliteStorageProvider provider) => new BsqliteInProcessTransport();
    }
}
