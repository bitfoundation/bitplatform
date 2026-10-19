using Bit.Bsqlite.Data;
using Bit.Bsqlite.Testing;
using Microsoft.EntityFrameworkCore;

namespace Bit.Bsqlite.EntityFrameworkCore.Tests;

/// <summary>
/// The schema paths the S1 audit found blocked - now the exit criterion for <c>UseBitBsqlite</c>.
/// </summary>
[TestClass]
public sealed class SchemaTests
{
    public TestContext TestContext { get; set; } = null!;

    private CancellationToken Ct => TestContext.CancellationToken;

    [TestMethod]
    public async Task EnsureCreatedAsync_CreatesAUsableSchema()
    {
        await using var transport = new BsqliteInProcessTransport();
        await using var ctx = ContextFactory.Create(transport);

        Assert.IsTrue(await ctx.Database.EnsureCreatedAsync(Ct));

        ctx.Categories.Add(new Category { Name = "Tools" });
        ctx.Products.Add(new Product { Name = "Widget", Price = 9.99m, CategoryId = 1 });
        await ctx.SaveChangesAsync(Ct);
        ctx.ChangeTracker.Clear();

        var loaded = await ctx.Products.Include(p => p.Category).SingleAsync(Ct);
        Assert.AreEqual("Tools", loaded.Category!.Name);
    }

    [TestMethod]
    public async Task EnsureCreatedAsync_IsIdempotent()
    {
        await using var transport = new BsqliteInProcessTransport();
        await using var ctx = ContextFactory.Create(transport);

        Assert.IsTrue(await ctx.Database.EnsureCreatedAsync(Ct));
        Assert.IsFalse(await ctx.Database.EnsureCreatedAsync(Ct));
    }

    [TestMethod]
    public async Task MigrateAsync_AppliesMigrations_AndRecordsHistory()
    {
        await using var transport = new BsqliteInProcessTransport();
        await using var ctx = ContextFactory.Create(transport);

        AssertSequence.Equal(["20260101000000_InitialCreate"], await ctx.Database.GetPendingMigrationsAsync(Ct));

        await ctx.Database.MigrateAsync(Ct);

        AssertSequence.Equal(["20260101000000_InitialCreate"], await ctx.Database.GetAppliedMigrationsAsync(Ct));
        Assert.IsEmpty(await ctx.Database.GetPendingMigrationsAsync(Ct));

        ctx.Categories.Add(new Category { Name = "Tools" });
        ctx.Products.Add(new Product { Name = "Widget", Price = 12.34m, CategoryId = 1 });
        await ctx.SaveChangesAsync(Ct);
        ctx.ChangeTracker.Clear();

        var loaded = await ctx.Products.SingleAsync(Ct);
        Assert.AreEqual(12.34m, loaded.Price);
    }

    [TestMethod]
    public async Task MigrateAsync_IsIdempotent()
    {
        await using var transport = new BsqliteInProcessTransport();
        await using var ctx = ContextFactory.Create(transport);

        await ctx.Database.MigrateAsync(Ct);
        await ctx.Database.MigrateAsync(Ct);

        Assert.IsEmpty(await ctx.Database.GetPendingMigrationsAsync(Ct));
    }

    [TestMethod]
    public async Task EnsureDeletedAsync_DropsUserTables_NotSqliteBookkeeping()
    {
        await using var transport = new BsqliteInProcessTransport();
        await using var ctx = ContextFactory.Create(transport);
        await ctx.Database.EnsureCreatedAsync(Ct);

        var deleted = await ctx.Database.EnsureDeletedAsync(Ct);

        Assert.IsTrue(deleted);
        Assert.IsTrue(await ctx.Database.EnsureCreatedAsync(Ct));
    }

    /// <summary>
    /// With foreign keys on, DROP TABLE runs an implicit DELETE that a referencing table can veto,
    /// and sqlite_master lists parents first. A view left behind would break the next
    /// EnsureCreated. Neither may stop the delete, and enforcement has to be back on afterwards
    /// because the connection lives on.
    /// </summary>
    [TestMethod]
    public async Task EnsureDeletedAsync_CopesWithReferencedRowsAndViews_AndLeavesForeignKeysOn()
    {
        await using var transport = new BsqliteInProcessTransport();
        await using var ctx = ContextFactory.Create(transport);
        await ctx.Database.EnsureCreatedAsync(Ct);

        await ctx.Database.ExecuteSqlRawAsync("CREATE TABLE parent (id INTEGER PRIMARY KEY)", Ct);
        await ctx.Database.ExecuteSqlRawAsync(
            "CREATE TABLE child (id INTEGER PRIMARY KEY, parent_id INTEGER REFERENCES parent(id))", Ct);
        await ctx.Database.ExecuteSqlRawAsync("INSERT INTO parent (id) VALUES (1)", Ct);
        await ctx.Database.ExecuteSqlRawAsync("INSERT INTO child (id, parent_id) VALUES (1, 1)", Ct);
        await ctx.Database.ExecuteSqlRawAsync("CREATE VIEW parents AS SELECT id FROM parent", Ct);

        Assert.IsTrue(await ctx.Database.EnsureDeletedAsync(Ct));

        Assert.IsTrue(await ctx.Database.EnsureCreatedAsync(Ct));
        Assert.AreEqual(0L, await ScalarAsync(ctx, "SELECT COUNT(*) FROM sqlite_master WHERE type = 'view'"));
        Assert.AreEqual(1L, await ScalarAsync(ctx, "PRAGMA foreign_keys"));
    }

    private async Task<long> ScalarAsync(DbContext ctx, string sql)
    {
        var connection = ctx.Database.GetDbConnection();
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        return Convert.ToInt64(await command.ExecuteScalarAsync(Ct));
    }

    [TestMethod]
    public async Task SynchronousSchemaApis_StillThrow()
    {
        await using var transport = new BsqliteInProcessTransport();
        await using var ctx = ContextFactory.Create(transport);

        Assert.ThrowsExactly<BsqliteSynchronousApiNotSupportedException>(
            () => ctx.Database.EnsureCreated());
    }
}
