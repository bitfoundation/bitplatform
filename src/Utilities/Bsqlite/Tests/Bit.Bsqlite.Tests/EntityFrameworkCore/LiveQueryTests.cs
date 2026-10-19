using Bit.Bsqlite.Data;
using Bit.Bsqlite.Testing;
using Microsoft.EntityFrameworkCore;

namespace Bit.Bsqlite.EntityFrameworkCore.Tests;

[TestClass]
public sealed class LiveQueryTests
{
    public TestContext TestContext { get; set; } = null!;

    private CancellationToken Ct => TestContext.CancellationToken;

    [TestMethod]
    public async Task AsLiveQuery_RerunsAfterSaveChanges()
    {
        var transport = new BsqliteInProcessTransport();
        await using var connection = new BsqliteConnection(transport, "live-ef.db");
        await connection.OpenAsync(Ct);
        await using var ctx = new ProductContext(new DbContextOptionsBuilder<ProductContext>()
            .UseBitBsqlite(connection)
            .Options);

        await ctx.Database.EnsureCreatedAsync(Ct);
        ctx.Categories.Add(new Category { Name = "Tools" });
        await ctx.SaveChangesAsync(Ct);

        await using var live = ctx.Products.AsLiveQuery(connection);
        Assert.IsEmpty(await live.RefreshAsync(Ct));

        ctx.Products.Add(new Product { Name = "Live", Price = 1m, CategoryId = 1 });
        await ctx.SaveChangesAsync(Ct);

        var deadline = DateTime.UtcNow.AddSeconds(2);
        while ((live.Current?.Count ?? 0) == 0 && DateTime.UtcNow < deadline)
        {
            await Task.Delay(20, Ct);
        }

        Assert.AreEqual("Live", Assert.ContainsSingle(live.Current!).Name);
    }

    [TestMethod]
    public async Task AsLiveQuery_RefreshWaitsForSaveChangesToFinish()
    {
        // The in-process transport raises TablesChanged before EF has consumed the insert result, and
        // on the thread pool the refresh runs concurrently with the rest of SaveChanges. Without the
        // save gate the re-read tracked the new row while EF was still assigning its key, and the
        // *write* failed with "another instance with the same key value is already being tracked".
        var transport = new BsqliteInProcessTransport();
        await using var connection = new BsqliteConnection(transport, "live-ef-race.db");
        await connection.OpenAsync(Ct);
        await using var ctx = new ProductContext(new DbContextOptionsBuilder<ProductContext>()
            .UseBitBsqlite(connection)
            .Options);

        await ctx.Database.EnsureCreatedAsync(Ct);
        ctx.Categories.Add(new Category { Name = "Tools" });
        await ctx.SaveChangesAsync(Ct);

        await using var live = ctx.Products.AsLiveQuery(connection);
        await live.RefreshAsync(Ct);

        // Each write is followed by a wait for its refresh, so every refresh is checked - not to keep
        // the two apart: the refresh runs on a context of its own.
        const int writes = 50;
        for (var i = 0; i < writes; i++)
        {
            ctx.Products.Add(new Product { Name = $"Item {i}", Price = i, CategoryId = 1 });
            await ctx.SaveChangesAsync(Ct);

            var deadline = DateTime.UtcNow.AddSeconds(5);
            while ((live.Current?.Count ?? 0) < i + 1 && DateTime.UtcNow < deadline)
            {
                await Task.Delay(1, Ct);
            }

            Assert.AreEqual(i + 1, live.Current!.Count);
        }
    }

    /// <summary>
    /// A tracking query re-run on a context that already tracks its rows gets the tracked instances
    /// back, old values and all. The refresh runs on a fresh context, so an update - here from raw
    /// SQL, as another tab's would arrive - shows up, not just inserts and deletes.
    /// </summary>
    [TestMethod]
    public async Task AsLiveQuery_TrackingQuery_SeesUpdatesToRowsItAlreadyLoaded()
    {
        var transport = new BsqliteInProcessTransport();
        await using var connection = new BsqliteConnection(transport, "live-update.db");
        await connection.OpenAsync(Ct);
        await using var ctx = new ProductContext(new DbContextOptionsBuilder<ProductContext>()
            .UseBitBsqlite(connection)
            .Options);

        await ctx.Database.EnsureCreatedAsync(Ct);
        ctx.Categories.Add(new Category { Name = "Tools" });
        ctx.Products.Add(new Product { Name = "Before", Price = 1m, CategoryId = 1 });
        await ctx.SaveChangesAsync(Ct);

        await using var live = ctx.Products.Include(p => p.Category).AsLiveQuery();
        Assert.AreEqual("Before", Assert.ContainsSingle(await live.RefreshAsync(Ct)).Name);

        await ctx.Database.ExecuteSqlRawAsync("UPDATE \"Products\" SET \"Name\" = 'After'", Ct);

        var deadline = DateTime.UtcNow.AddSeconds(2);
        while (live.Current?.SingleOrDefault()?.Name != "After" && DateTime.UtcNow < deadline)
        {
            await Task.Delay(20, Ct);
        }

        var row = Assert.ContainsSingle(live.Current!);
        Assert.AreEqual("After", row.Name);
        Assert.AreEqual("Tools", row.Category!.Name);
    }

    /// <summary>
    /// Refreshes arrive whenever another tab writes, so running them on the application's context
    /// would collide with whatever the application starts there next. They run elsewhere, and the
    /// application's context neither tracks their rows nor is kept busy by them.
    /// </summary>
    [TestMethod]
    public async Task AsLiveQuery_RunsOutsideTheApplicationsContext()
    {
        var transport = new BsqliteInProcessTransport();
        await using var connection = new BsqliteConnection(transport, "live-own-context.db");
        await connection.OpenAsync(Ct);
        await using var ctx = new ProductContext(new DbContextOptionsBuilder<ProductContext>()
            .UseBitBsqlite(connection)
            .Options);

        await ctx.Database.EnsureCreatedAsync(Ct);
        ctx.Categories.Add(new Category { Name = "Tools" });
        await ctx.SaveChangesAsync(Ct);
        ctx.ChangeTracker.Clear();

        await using var live = ctx.Categories.AsLiveQuery();
        Assert.ContainsSingle(await live.RefreshAsync(Ct));
        Assert.IsEmpty(ctx.ChangeTracker.Entries());

        ctx.Categories.Add(new Category { Name = "Paint" });
        await ctx.SaveChangesAsync(Ct);

        var deadline = DateTime.UtcNow.AddSeconds(2);
        while (live.Current?.Count != 2 && DateTime.UtcNow < deadline)
        {
            await Task.Delay(20, Ct);
        }

        Assert.HasCount(2, live.Current!);

        // Only what the application added itself.
        Assert.AreEqual("Paint", ((Category)Assert.ContainsSingle(ctx.ChangeTracker.Entries()).Entity).Name);
    }

    [TestMethod]
    public async Task AsLiveQuery_ResolvesConnectionFromComposedQuery()
    {
        // The sample calls OrderBy(...).AsLiveQuery() with no connection argument. DbSet exposes
        // the context through IInfrastructure; EntityQueryable after OrderBy/Where does not.
        var transport = new BsqliteInProcessTransport();
        await using var connection = new BsqliteConnection(transport, "live-composed.db");
        await connection.OpenAsync(Ct);
        await using var ctx = new ProductContext(new DbContextOptionsBuilder<ProductContext>()
            .UseBitBsqlite(connection)
            .Options);

        await ctx.Database.EnsureCreatedAsync(Ct);
        ctx.Categories.Add(new Category { Name = "Tools" });
        await ctx.SaveChangesAsync(Ct);

        await using var live = ctx.Products.OrderBy(p => p.Id).AsLiveQuery();
        Assert.IsEmpty(await live.RefreshAsync(Ct));
    }
}
