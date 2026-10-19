using Bit.Bsqlite.Data;
using Bit.Bsqlite.Interop;
using Bit.Bsqlite.Storage;
using Bit.Bsqlite.Storage.InMemory;
using Bit.Bsqlite.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Bit.Bsqlite.EntityFrameworkCore.Tests;

/// <summary>
/// Every tab runs its own worker over the same file, so two tabs opening after a deploy race to apply
/// the same migration - and to seed the same empty database. Two in-process transports over one
/// shared-cache database stand in for the two tabs, their locks shared the way the worker's Web Locks
/// are.
/// </summary>
[TestClass]
public sealed class MigrationLockTests
{
    public TestContext TestContext { get; set; } = null!;

    private CancellationToken Ct => TestContext.CancellationToken;

    private static string SharedDatabase() => $"Data Source=lock-{Guid.NewGuid():N};Mode=Memory;Cache=Shared";

#if NET9_0_OR_GREATER
    [TestMethod]
    public async Task MigrateAsync_WaitsWhileAnotherTabIsMigrating()
    {
        var database = SharedDatabase();
        await using var otherTab = new BsqliteInProcessTransport(database);
        await otherTab.OpenAsync("app.db", Ct);
        await using var thisTab = new BsqliteInProcessTransport(database);
        await using var ctx = ContextFactory.Create(thisTab, "app.db");

        var held = await otherTab.AcquireLockAsync(BsqliteHistoryRepository.MigrationLockName, Ct);
        var migrating = ctx.Database.MigrateAsync(Ct);

        await Task.Delay(200, Ct);
        Assert.IsFalse(migrating.IsCompleted, "The migration ran while another tab held the lock.");

        // Waiting must not hold SQLite's own write lock, or the tab it waits for could not finish.
        Assert.DoesNotContain(sql => sql.StartsWith("BEGIN", StringComparison.Ordinal), thisTab.ExecutedCommands);

        await held.DisposeAsync();
        await migrating.WaitAsync(TimeSpan.FromSeconds(10), Ct);

        Assert.ContainsSingle(await ctx.Database.GetAppliedMigrationsAsync(Ct));
    }

    [TestMethod]
    public async Task MigrateAsync_ReleasesTheLockForTheNextTab()
    {
        var database = SharedDatabase();
        await using var first = new BsqliteInProcessTransport(database);
        await using var second = new BsqliteInProcessTransport(database);

        await using (var ctx = ContextFactory.Create(first, "app.db"))
        {
            await ctx.Database.MigrateAsync(Ct);
        }

        await using (var ctx = ContextFactory.Create(second, "app.db"))
        {
            await ctx.Database.MigrateAsync(Ct).WaitAsync(TimeSpan.FromSeconds(10), Ct);
            Assert.ContainsSingle(await ctx.Database.GetAppliedMigrationsAsync(Ct));
        }
    }
#endif

    /// <summary>
    /// The factory's first-use step - the schema step and the seed after it - runs under a lock of
    /// its own, which is also what keeps two tabs on EF Core 8, whose migrator takes no lock, from
    /// applying the same migration twice.
    /// </summary>
    [TestMethod]
    public async Task ContextFactory_FirstUse_WaitsWhileAnotherTabPreparesTheDatabase()
    {
        var database = SharedDatabase();
        await using var otherTab = new BsqliteInProcessTransport(database);
        await otherTab.OpenAsync("app.db", Ct);

        var seeded = 0;
        var services = new ServiceCollection();
        services.AddBitBsqliteServices(sqlite => sqlite
            .AddStorageProvider<BsqliteInMemoryStorageProvider>()
            .UseStorage(s => s.Prefer(BsqliteInMemoryStorageProvider.ProviderName))
            .UseBindingStore<BsqliteInMemoryStorageBindingStore>()
            .AddDatabase("app.db"));
        services.AddSingleton<IBsqliteTransportFactory>(new SharedTransportFactory(database));
        services.AddBitBsqliteDbContext<ProductContext>(options =>
        {
            options.Schema = BsqliteSchemaStrategy.EnsureCreated;
            options.OnFirstUse = (_, _) =>
            {
                seeded++;
                return Task.CompletedTask;
            };
        });

        await using var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IBsqliteDbContextFactory<ProductContext>>();

        var held = await otherTab.AcquireLockAsync(BsqliteDbContextFactory<ProductContext>.FirstUseLockName, Ct);
        var creating = factory.CreateDbContextAsync(Ct);

        await Task.Delay(200, Ct);
        Assert.IsFalse(creating.IsCompleted, "The first-use step ran while another tab held the lock.");
        Assert.AreEqual(0, seeded);

        await held.DisposeAsync();
        await using var ctx = await creating.WaitAsync(TimeSpan.FromSeconds(10), Ct);

        Assert.AreEqual(1, seeded);
    }

    private sealed class SharedTransportFactory(string connectionString) : IBsqliteTransportFactory
    {
        public IBsqliteTransport Create(IBsqliteStorageProvider provider) => new BsqliteInProcessTransport(connectionString);
    }
}
