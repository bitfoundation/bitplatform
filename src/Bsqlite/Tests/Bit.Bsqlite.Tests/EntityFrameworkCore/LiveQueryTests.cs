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

        // Each write is followed by a wait for its refresh: a DbContext allows one operation at a
        // time, so the next SaveChanges must not start while the live query is still reading. That
        // is the caller's side of the contract; the gate covers the library's side.
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
