using Bit.Bsqlite.Data;
using Bit.Bsqlite.Interop;
using Bit.Bsqlite.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Bit.Bsqlite.EntityFrameworkCore.Tests;

/// <summary>
/// SQL generation never touches the connection. These tests drive real reads and writes through
/// <c>UseBitBsqlite</c> so identical SQL cannot be mistaken for a working provider.
/// </summary>
[TestClass]
public sealed class ExecutionTests
{
    public TestContext TestContext { get; set; } = null!;

    private CancellationToken Ct => TestContext.CancellationToken;

    [TestMethod]
    public async Task SaveChanges_And_Query_RoundTrip()
    {
        await using var transport = new BsqliteInProcessTransport();
        await using var ctx = ContextFactory.Create(transport);
        await ctx.Database.EnsureCreatedAsync(Ct);

        ctx.Categories.Add(new Category { Name = "Tools" });
        ctx.Products.Add(new Product
        {
            Name = "Widget",
            Price = 19.99m,
            CreatedUtc = new DateTime(2026, 3, 1, 12, 0, 0, DateTimeKind.Utc),
            CategoryId = 1,
        });
        await ctx.SaveChangesAsync(Ct);
        ctx.ChangeTracker.Clear();

        var loaded = await ctx.Products.Include(p => p.Category).SingleAsync(Ct);

        Assert.AreEqual("Widget", loaded.Name);
        Assert.AreEqual(19.99m, loaded.Price);
        Assert.AreEqual("Tools", loaded.Category!.Name);
        Assert.AreEqual(new DateTime(2026, 3, 1, 12, 0, 0), loaded.CreatedUtc);
    }

    [TestMethod]
    public async Task SaveChanges_DateTime_SurvivesTheBrowserWireFormat()
    {
        await using var transport = new WireFormatAssertingTransport();
        await using var ctx = ContextFactory.Create(transport);
        await ctx.Database.EnsureCreatedAsync(Ct);

        ctx.Categories.Add(new Category { Name = "Tools" });
        ctx.Products.Add(new Product
        {
            Name = "Widget",
            Price = 19.99m,
            CreatedUtc = new DateTime(2026, 3, 1, 12, 0, 0, DateTimeKind.Utc),
            CategoryId = 1,
        });
        await ctx.SaveChangesAsync(Ct);
        ctx.ChangeTracker.Clear();

        var loaded = await ctx.Products.SingleAsync(Ct);
        Assert.AreEqual(new DateTime(2026, 3, 1, 12, 0, 0), loaded.CreatedUtc);
    }

    [TestMethod]
    public async Task DecimalComparison_RunsThroughEfCompare()
    {
        await using var transport = new BsqliteInProcessTransport();
        await using var ctx = ContextFactory.Create(transport);
        await ctx.Database.EnsureCreatedAsync(Ct);

        ctx.Categories.Add(new Category { Name = "Tools" });
        ctx.Products.AddRange(
            new Product { Name = "Cheap", Price = 5m, CategoryId = 1 },
            new Product { Name = "Pricey", Price = 500m, CategoryId = 1 });
        await ctx.SaveChangesAsync(Ct);
        ctx.ChangeTracker.Clear();

        var query = ctx.Products.Where(p => p.Price > 10m);

        Assert.Contains("ef_compare", query.ToQueryString(), StringComparison.Ordinal);
        AssertSequence.Equal(["Pricey"], await query.Select(p => p.Name).ToListAsync(Ct));
    }

#if NET9_0_OR_GREATER
    // EF Core translates SUM over a decimal from EF Core 9 on; before that the stock provider refuses it too.
    [TestMethod]
    public async Task DecimalSum_RunsThroughEfSum()
    {
        await using var transport = new BsqliteInProcessTransport();
        await using var ctx = ContextFactory.Create(transport);
        await ctx.Database.EnsureCreatedAsync(Ct);

        ctx.Categories.Add(new Category { Name = "Tools" });
        ctx.Products.AddRange(
            new Product { Name = "A", Price = 1.10m, CategoryId = 1 },
            new Product { Name = "B", Price = 2.20m, CategoryId = 1 });
        await ctx.SaveChangesAsync(Ct);
        ctx.ChangeTracker.Clear();

        var query = ctx.Products.GroupBy(p => p.CategoryId).Select(g => g.Sum(p => p.Price));

        Assert.Contains("ef_sum", query.ToQueryString(), StringComparison.Ordinal);
        Assert.AreEqual(3.30m, await query.SingleAsync(Ct));
    }
#endif

    [TestMethod]
    public async Task WithoutEfFunctions_DecimalQueries_Fail()
    {
        await using var transport = new BsqliteInProcessTransport(registerEfFunctions: false);
        await using var ctx = ContextFactory.Create(transport);
        await ctx.Database.EnsureCreatedAsync(Ct);

        ctx.Categories.Add(new Category { Name = "Tools" });
        ctx.Products.Add(new Product { Name = "A", Price = 1.10m, CategoryId = 1 });
        await ctx.SaveChangesAsync(Ct);
        ctx.ChangeTracker.Clear();

        var failure = await Assert.ThrowsExactlyAsync<SqliteException>(
            () => ctx.Products.Where(p => p.Price > 10m).ToListAsync(Ct));

        Assert.Contains("no such function: ef_compare", failure.Message, StringComparison.Ordinal);
    }

    [TestMethod]
    public async Task SynchronousDispose_DoesNotThrow_AndLeavesTransportUsable()
    {
        await using var transport = new BsqliteInProcessTransport();
        var ctx = ContextFactory.Create(transport);
        await ctx.Database.EnsureCreatedAsync(Ct);
        ctx.Categories.Add(new Category { Name = "Tools" });
        ctx.Products.Add(new Product { Name = "Widget", Price = 1m, CategoryId = 1 });
        await ctx.SaveChangesAsync(Ct);

        ctx.Dispose();

        await using var second = ContextFactory.Create(transport);
        Assert.AreEqual(1, await second.Products.CountAsync(Ct));
    }

    [TestMethod]
    public async Task Connection_SurvivesOpenCloseCycles()
    {
        await using var transport = new BsqliteInProcessTransport();
        await using var ctx = ContextFactory.Create(transport);
        await ctx.Database.EnsureCreatedAsync(Ct);

        for (var i = 0; i < 3; i++)
        {
            await ctx.Database.OpenConnectionAsync(Ct);
            await ctx.Database.CloseConnectionAsync();
        }

        ctx.Categories.Add(new Category { Name = "Tools" });
        ctx.Products.Add(new Product { Name = "Widget", Price = 1m, CategoryId = 1 });
        await ctx.SaveChangesAsync(Ct);

        Assert.AreEqual(1, await ctx.Products.CountAsync(Ct));
    }

    /// <summary>
    /// Desktop tests bind through Microsoft.Data.Sqlite, which converts DateTime itself. The browser
    /// transport encodes with <see cref="BsqliteWireFormat"/> first, so this wrapper makes a leaking
    /// DateTime fail here the same way it fails in WASM.
    /// </summary>
    private sealed class WireFormatAssertingTransport : IBsqliteTransport
    {
        private readonly BsqliteInProcessTransport _inner = new();

        public Task OpenAsync(string databaseName, CancellationToken cancellationToken = default)
            => _inner.OpenAsync(databaseName, cancellationToken);

        public Task CloseAsync(CancellationToken cancellationToken = default)
            => _inner.CloseAsync(cancellationToken);

        public Task<IReadOnlyList<BsqliteCommandResult>> ExecuteAsync(
            IReadOnlyList<BsqliteCommandRequest> batch,
            CancellationToken cancellationToken = default)
        {
            _ = BsqliteWireFormat.EncodeBatch(batch);
            return _inner.ExecuteAsync(batch, cancellationToken);
        }

        public ValueTask DisposeAsync() => _inner.DisposeAsync();
    }
}
