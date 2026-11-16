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

    /// <summary>
    /// A write is reported by the base table it changed; a query over a view names only the view, so it never
    /// matched a notification and the live query never re-ran.
    /// </summary>
    [TestMethod]
    public async Task AsLiveQuery_OverAView_RerunsWhenATableBehindItIsWritten()
    {
        var transport = new BsqliteInProcessTransport();
        await using var connection = new BsqliteConnection(transport, "live-view.db");
        await connection.OpenAsync(Ct);
        await using var ctx = new LineContext(new DbContextOptionsBuilder<LineContext>()
            .UseBitBsqlite(connection)
            .Options);

        await ctx.Database.EnsureCreatedAsync(Ct);
        await ctx.Database.ExecuteSqlRawAsync("CREATE VIEW \"OpenLines\" AS SELECT \"Id\", \"Name\" FROM \"Lines\"", Ct);

        await using var live = ctx.OpenLines.AsLiveQuery(connection);
        Assert.IsEmpty(await live.RefreshAsync(Ct));

        ctx.Lines.Add(new Line { Name = "New" });
        await ctx.SaveChangesAsync(Ct);

        var deadline = DateTime.UtcNow.AddSeconds(2);
        while ((live.Current?.Count ?? 0) == 0 && DateTime.UtcNow < deadline)
        {
            await Task.Delay(20, Ct);
        }

        Assert.AreEqual("New", Assert.ContainsSingle(live.Current!).Name);
    }

    /// <summary>
    /// What the extractor picks up that is no table - a table-valued function, a word in a string literal or a
    /// comment, a common table expression - must not turn a query over mapped tables into one that re-runs on
    /// every write; a view still must.
    /// </summary>
    [TestMethod]
    public async Task TablesOf_SetsAsideWhatIsNoTable_ButNotAView()
    {
        var transport = new BsqliteInProcessTransport();
        await using var connection = new BsqliteConnection(transport, "live-tables-of.db");
        await connection.OpenAsync(Ct);
        await using var ctx = new LineContext(new DbContextOptionsBuilder<LineContext>()
            .UseBitBsqlite(connection)
            .Options);

        int[] ids = [1, 2];
        // One JSON parameter, which EF reads with json_each - EF 10 expands a collection into a parameter per element
        // by default, and EF.Parameter asks for the single one.
        var primitiveCollection = ctx.Lines.Where(line => EF.Parameter(ids).Contains(line.Id)).ToQueryString();
        StringAssert.Contains(primitiveCollection, "json_each", StringComparison.OrdinalIgnoreCase);
        CollectionAssert.AreEquivalent(new[] { "Lines" }, BsqliteLiveQueryExtensions.TablesOf(primitiveCollection, ctx).ToArray());

        var literal = ctx.Lines.Where(line => line.Name == "picked up from store").ToQueryString();
        CollectionAssert.AreEquivalent(new[] { "Lines" }, BsqliteLiveQueryExtensions.TablesOf(literal, ctx).ToArray());

        CollectionAssert.AreEquivalent(
            new[] { "Lines" },
            BsqliteLiveQueryExtensions.TablesOf(
                "-- read from audit\nWITH recent (Id) AS (SELECT \"Id\" FROM \"Lines\") SELECT * FROM recent /* join history */ WHERE 'it''s from store' <> ''",
                ctx).ToArray());

        Assert.IsEmpty(BsqliteLiveQueryExtensions.TablesOf(ctx.OpenLines.ToQueryString(), ctx));
        Assert.IsEmpty(BsqliteLiveQueryExtensions.TablesOf("SELECT * FROM \"Lines\" JOIN \"Unmapped\" ON 1", ctx));
    }

    public sealed class Line
    {
        public int Id { get; set; }

        public string Name { get; set; } = "";
    }

    public sealed class OpenLine
    {
        public int Id { get; set; }

        public string Name { get; set; } = "";
    }

    public sealed class LineContext(DbContextOptions<LineContext> options) : DbContext(options)
    {
        public DbSet<Line> Lines => Set<Line>();

        public DbSet<OpenLine> OpenLines => Set<OpenLine>();

        protected override void OnModelCreating(ModelBuilder modelBuilder) =>
            modelBuilder.Entity<OpenLine>().HasNoKey().ToView("OpenLines");
    }
}
