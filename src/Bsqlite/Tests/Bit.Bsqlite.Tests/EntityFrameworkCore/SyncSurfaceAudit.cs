using Bit.Bsqlite.Data;
using Bit.Bsqlite.Testing;
using Microsoft.EntityFrameworkCore;

namespace Bit.Bsqlite.EntityFrameworkCore.Tests;

/// <summary>
/// The S1 audit, now a regression suite: every entry point an application actually calls is marked
/// usable or blocked, so a new sync-only EF path shows up as a failing expectation naming the frame.
/// </summary>
[TestClass]
public sealed class SyncSurfaceAudit
{
    public TestContext TestContext { get; set; } = null!;

    private CancellationToken Ct => TestContext.CancellationToken;

    public enum Expectation
    {
        Works,
        SyncBound,
    }

    private async Task<ProductContext> CreateSeededAsync(BsqliteInProcessTransport transport)
    {
        var ctx = ContextFactory.Create(transport, "audit.db");
        await ctx.Database.EnsureCreatedAsync(Ct);
        ctx.Categories.Add(new Category { Name = "Tools" });
        ctx.Products.Add(new Product { Name = "Widget", Price = 9.99m, CategoryId = 1 });
        await ctx.SaveChangesAsync(Ct);
        ctx.ChangeTracker.Clear();
        return ctx;
    }

    public static IEnumerable<(string Name, Expectation Expected, Func<ProductContext, CancellationToken, Task> Operation)> DataOperations =>
    [
        ("ToListAsync", Expectation.Works, (ctx, ct) => ctx.Products.ToListAsync(ct)),
        ("FirstOrDefaultAsync", Expectation.Works, (ctx, ct) => ctx.Products.FirstOrDefaultAsync(ct)),
        ("CountAsync", Expectation.Works, (ctx, ct) => ctx.Products.CountAsync(ct)),
        ("AnyAsync", Expectation.Works, (ctx, ct) => ctx.Products.AnyAsync(ct)),
        ("Include + ToListAsync", Expectation.Works, (ctx, ct) => ctx.Products.Include(p => p.Category).ToListAsync(ct)),
        (
            "AsAsyncEnumerable",
            Expectation.Works,
            async (ctx, ct) =>
            {
                await foreach (var _ in ctx.Products.AsAsyncEnumerable().WithCancellation(ct))
                {
                }
            }
        ),
        (
            "SaveChangesAsync (insert)",
            Expectation.Works,
            (ctx, ct) =>
            {
                ctx.Products.Add(new Product { Name = "New", Price = 1m, CategoryId = 1 });
                return ctx.SaveChangesAsync(ct);
            }
        ),
        (
            "SaveChangesAsync (update)",
            Expectation.Works,
            async (ctx, ct) =>
            {
                var product = await ctx.Products.FirstAsync(ct);
                product.Name = "Renamed";
                await ctx.SaveChangesAsync(ct);
            }
        ),
        (
            "SaveChangesAsync (delete)",
            Expectation.Works,
            async (ctx, ct) =>
            {
                ctx.Products.Remove(await ctx.Products.FirstAsync(ct));
                await ctx.SaveChangesAsync(ct);
            }
        ),
        (
            "ExecuteUpdateAsync",
            Expectation.Works,
            (ctx, ct) => ctx.Products.ExecuteUpdateAsync(s => s.SetProperty(p => p.Name, "Bulk"), ct)
        ),
        ("ExecuteDeleteAsync", Expectation.Works, (ctx, ct) => ctx.Products.ExecuteDeleteAsync(ct)),
        (
            "FromSqlRaw + ToListAsync",
            Expectation.Works,
            (ctx, ct) => ctx.Products.FromSqlRaw("SELECT * FROM Products").ToListAsync(ct)
        ),
        (
            "ExecuteSqlRawAsync",
            Expectation.Works,
            (ctx, ct) => ctx.Database.ExecuteSqlRawAsync("UPDATE Products SET Name = 'raw'", ct)
        ),
        ("CanConnectAsync", Expectation.Works, (ctx, ct) => ctx.Database.CanConnectAsync(ct)),
        (
            "OpenConnectionAsync + CloseConnectionAsync",
            Expectation.Works,
            async (ctx, ct) =>
            {
                await ctx.Database.OpenConnectionAsync(ct);
                await ctx.Database.CloseConnectionAsync();
            }
        ),
        (
            "BeginTransactionAsync + CommitAsync",
            Expectation.Works,
            async (ctx, ct) =>
            {
                await using var tx = await ctx.Database.BeginTransactionAsync(ct);
                ctx.Products.Add(new Product { Name = "Tx", Price = 1m, CategoryId = 1 });
                await ctx.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);
            }
        ),
        (
            "BeginTransactionAsync + RollbackAsync",
            Expectation.Works,
            async (ctx, ct) =>
            {
                await using var tx = await ctx.Database.BeginTransactionAsync(ct);
                ctx.Products.Add(new Product { Name = "Tx", Price = 1m, CategoryId = 1 });
                await ctx.SaveChangesAsync(ct);
                await tx.RollbackAsync(ct);
            }
        ),
        (
            "Entry.ReloadAsync",
            Expectation.Works,
            async (ctx, ct) =>
            {
                var product = await ctx.Products.FirstAsync(ct);
                await ctx.Entry(product).ReloadAsync(ct);
            }
        ),
        (
            "Collection.LoadAsync",
            Expectation.Works,
            async (ctx, ct) =>
            {
                var category = await ctx.Categories.FirstAsync(ct);
                await ctx.Entry(category).Collection(c => c.Products).LoadAsync(ct);
            }
        ),
        (
            "GetAppliedMigrationsAsync",
            Expectation.Works,
            (ctx, ct) => ctx.Database.GetAppliedMigrationsAsync(ct)
        ),
        (
            "GetPendingMigrationsAsync",
            Expectation.Works,
            (ctx, ct) => ctx.Database.GetPendingMigrationsAsync(ct)
        ),
        ("ToList (sync)", Expectation.SyncBound, (ctx, ct) => Task.FromResult(ctx.Products.ToList())),
        ("Count (sync)", Expectation.SyncBound, (ctx, ct) => Task.FromResult(ctx.Products.Count())),
        (
            "SaveChanges (sync)",
            Expectation.SyncBound,
            (ctx, ct) =>
            {
                ctx.Products.Add(new Product { Name = "Sync", Price = 1m, CategoryId = 1 });
                return Task.FromResult(ctx.SaveChanges());
            }
        ),
    ];

    public static IEnumerable<(string Name, Expectation Expected, Func<ProductContext, CancellationToken, Task> Operation)> SchemaOperations =>
    [
        ("EnsureCreatedAsync", Expectation.Works, (ctx, ct) => ctx.Database.EnsureCreatedAsync(ct)),
        ("EnsureDeletedAsync", Expectation.Works, (ctx, ct) => ctx.Database.EnsureDeletedAsync(ct)),
        ("MigrateAsync", Expectation.Works, (ctx, ct) => ctx.Database.MigrateAsync(ct)),
        ("CanConnectAsync (virgin database)", Expectation.Works, (ctx, ct) => ctx.Database.CanConnectAsync(ct)),
        ("EnsureCreated (sync)", Expectation.SyncBound, (ctx, ct) => Task.FromResult(ctx.Database.EnsureCreated())),
    ];

    [TestMethod]
    [DynamicData(nameof(DataOperations))]
    public async Task DataOperation_MatchesExpectation(
        string name,
        Expectation expected,
        Func<ProductContext, CancellationToken, Task> operation)
    {
        await using var transport = new BsqliteInProcessTransport();
        await using var ctx = await CreateSeededAsync(transport);
        await AssertExpectationAsync(name, expected, ctx, operation);
    }

    [TestMethod]
    [DynamicData(nameof(SchemaOperations))]
    public async Task SchemaOperation_MatchesExpectation(
        string name,
        Expectation expected,
        Func<ProductContext, CancellationToken, Task> operation)
    {
        await using var transport = new BsqliteInProcessTransport();
        await using var ctx = ContextFactory.Create(transport, "audit-schema.db");
        await AssertExpectationAsync(name, expected, ctx, operation);
    }

    private async Task AssertExpectationAsync(
        string name,
        Expectation expected,
        ProductContext ctx,
        Func<ProductContext, CancellationToken, Task> operation)
    {
        Exception? failure = null;
        try
        {
            await operation(ctx, Ct);
        }
        catch (Exception exception)
        {
            failure = exception;
        }

        if (expected == Expectation.SyncBound)
        {
            Assert.IsInstanceOfType<BsqliteSynchronousApiNotSupportedException>(failure);

            TestContext.WriteLine($"{name} is sync-bound via:");
            foreach (var frame in EfFrames(failure!))
            {
                TestContext.WriteLine($"    {frame}");
            }

            return;
        }

        if (failure is not null)
        {
            Assert.Fail($"'{name}' was expected to work on the async tier but threw:{Environment.NewLine}{failure}");
        }
    }

    private static IEnumerable<string> EfFrames(Exception failure)
        => (failure.StackTrace ?? string.Empty)
            .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Trim())
            .Where(line => line.Contains("Microsoft.EntityFrameworkCore", StringComparison.Ordinal))
            .Take(4);
}
