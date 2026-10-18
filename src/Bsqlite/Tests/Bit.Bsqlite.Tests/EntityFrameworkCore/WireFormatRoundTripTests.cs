using Microsoft.EntityFrameworkCore;

namespace Bit.Bsqlite.EntityFrameworkCore.Tests;

/// <summary>
/// EF over the real wire format, which is the combination the browser runs and the one the rest of
/// the desktop suite skips past.
/// </summary>
[TestClass]
public sealed class WireFormatRoundTripTests
{
    public TestContext TestContext { get; set; } = null!;

    private CancellationToken Ct => TestContext.CancellationToken;

    /// <summary>
    /// The bug this exists to prevent: the wire format wrote <c>10</c> where the rest of the SQLite
    /// stack writes <c>10.0</c>, EF compiled <c>== 10m</c> to <c>WHERE "Price" = '10.0'</c>, and a
    /// row the application had just saved could not be found again. Nothing threw.
    /// </summary>
    /// <remarks>
    /// The comparisons are written as literals on purpose. A captured variable is parameterised, so
    /// both sides of the comparison go through the same encoder and agree even when that encoder is
    /// wrong - which is exactly how this defect stayed invisible. An inlined constant is rendered by
    /// EF's own type mapping, and only matches if what was stored agrees with it.
    /// </remarks>
    [TestMethod]
    public async Task ADecimalWrittenThroughTheWire_IsFoundByAConstantComparison()
    {
        await using var transport = new WireLoopbackTransport();
        await using var ctx = ContextFactory.Create(transport);
        await ctx.Database.EnsureCreatedAsync(Ct);

        ctx.Categories.Add(new Category { Name = "Tools" });
        foreach (var price in new[] { 10m, 1.50m, 0m, 12.34m, 1234.5678m })
        {
            ctx.Products.Add(new Product { Name = $"P{price}", Price = price, CategoryId = 1 });
        }

        await ctx.SaveChangesAsync(Ct);
        ctx.ChangeTracker.Clear();

        Assert.AreEqual(1, await ctx.Products.CountAsync(p => p.Price == 10m, Ct));
        Assert.AreEqual(1, await ctx.Products.CountAsync(p => p.Price == 1.50m, Ct));
        Assert.AreEqual(1, await ctx.Products.CountAsync(p => p.Price == 0m, Ct));
        Assert.AreEqual(1, await ctx.Products.CountAsync(p => p.Price == 12.34m, Ct));
        Assert.AreEqual(1, await ctx.Products.CountAsync(p => p.Price == 1234.5678m, Ct));
    }

    /// <summary>
    /// The same row, read back as a CLR value rather than matched in SQL.
    /// </summary>
    [TestMethod]
    [DataRow("10")]
    [DataRow("1.50")]
    [DataRow("0")]
    [DataRow("12.34")]
    [DataRow("-3")]
    [DataRow("1234.5678")]
    public async Task ADecimalWrittenThroughTheWire_MaterializesBackToItself(string literal)
    {
        var price = decimal.Parse(literal, System.Globalization.CultureInfo.InvariantCulture);

        await using var transport = new WireLoopbackTransport();
        await using var ctx = ContextFactory.Create(transport);
        await ctx.Database.EnsureCreatedAsync(Ct);

        ctx.Categories.Add(new Category { Name = "Tools" });
        ctx.Products.Add(new Product { Name = "Widget", Price = price, CategoryId = 1 });
        await ctx.SaveChangesAsync(Ct);
        ctx.ChangeTracker.Clear();

        Assert.AreEqual(price, (await ctx.Products.SingleAsync(Ct)).Price);
    }

    /// <summary>
    /// The stored text is compared against the stock provider's, because "both halves agree" is the
    /// whole claim - a database written in the browser has to be one the server can read.
    /// </summary>
    [TestMethod]
    public async Task TheStoredTextMatchesWhatTheStockProviderWrites()
    {
        await using var transport = new WireLoopbackTransport();
        await using var ctx = ContextFactory.Create(transport);
        await ctx.Database.EnsureCreatedAsync(Ct);

        ctx.Categories.Add(new Category { Name = "Tools" });
        ctx.Products.Add(new Product { Name = "Widget", Price = 10m, CategoryId = 1 });
        await ctx.SaveChangesAsync(Ct);

        var stored = await ctx.Database
            .SqlQueryRaw<string>("SELECT CAST(\"Price\" AS TEXT) AS \"Value\" FROM \"Products\"")
            .SingleAsync(Ct);

        Assert.AreEqual("10.0", stored);
    }

    /// <summary>
    /// Ordering and comparison go through <c>ef_compare</c> and the <c>EF_DECIMAL</c> collation, so
    /// they have to keep working whatever the stored text looks like.
    /// </summary>
#if NET10_0_OR_GREATER
    // EF Core translates ORDER BY a decimal from EF Core 10 on; before that the stock provider refuses it too.
    [TestMethod]
    public async Task DecimalsWrittenThroughTheWire_StillCompareNumerically()
    {
        await using var transport = new WireLoopbackTransport();
        await using var ctx = ContextFactory.Create(transport);
        await ctx.Database.EnsureCreatedAsync(Ct);

        ctx.Categories.Add(new Category { Name = "Tools" });
        foreach (var price in new[] { 9m, 10m, 2.5m, 100m, -0.25m })
        {
            ctx.Products.Add(new Product { Name = $"P{price}", Price = price, CategoryId = 1 });
        }

        await ctx.SaveChangesAsync(Ct);
        ctx.ChangeTracker.Clear();

        AssertSequence.Equal(
            [-0.25m, 2.5m, 9m, 10m, 100m],
            await ctx.Products.OrderBy(p => p.Price).Select(p => p.Price).ToListAsync(Ct));

        AssertSequence.Equal(
            [10m, 100m],
            await ctx.Products
                .Where(p => p.Price > 9m)
                .OrderBy(p => p.Price)
                .Select(p => p.Price)
                .ToListAsync(Ct));
    }
#endif

    /// <summary>
    /// Every other type the binder converts, checked through the same path - a regression in any of
    /// them is the same silent class of failure the decimal one was.
    /// </summary>
    [TestMethod]
    public async Task NonDecimalValues_SurviveTheWireUnchanged()
    {
        var created = new DateTime(2026, 8, 17, 9, 30, 15, DateTimeKind.Utc);

        await using var transport = new WireLoopbackTransport();
        await using var ctx = ContextFactory.Create(transport);
        await ctx.Database.EnsureCreatedAsync(Ct);

        ctx.Categories.Add(new Category { Name = "Tools" });
        ctx.Products.Add(new Product
        {
            Name = "Widget",
            Price = 1m,
            CreatedUtc = created,
            CategoryId = 1,
        });
        await ctx.SaveChangesAsync(Ct);
        ctx.ChangeTracker.Clear();

        var loaded = await ctx.Products.SingleAsync(Ct);

        Assert.AreEqual("Widget", loaded.Name);
        Assert.AreEqual(created, loaded.CreatedUtc);
        Assert.AreEqual(1, loaded.CategoryId);
        Assert.AreEqual(1, await ctx.Products.CountAsync(p => p.CreatedUtc == created, Ct));
    }
}
