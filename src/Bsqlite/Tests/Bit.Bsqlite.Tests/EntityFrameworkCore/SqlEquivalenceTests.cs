using Bit.Bsqlite.Testing;
using Microsoft.EntityFrameworkCore;

namespace Bit.Bsqlite.EntityFrameworkCore.Tests;

/// <summary>
/// SQL generation never inspects the connection type, so <c>UseBitBsqlite</c> must produce
/// SQL byte-identical to the stock provider. That is the §2 "same model both sides" guardrail.
/// </summary>
[TestClass]
public sealed class SqlEquivalenceTests
{
    public static IEnumerable<(string Name, Func<ProductContext, IQueryable<object>> Query)> Queries =>
    [
        ("simple filter", ctx => ctx.Products.Where(p => p.Name == "widget")),
        ("decimal comparison", ctx => ctx.Products.Where(p => p.Price > 10m)),
        ("decimal arithmetic", ctx => ctx.Products.Where(p => p.Price * 2m < 100m)),
#if NET10_0_OR_GREATER
        // EF Core translates ORDER BY a decimal from EF Core 10 on; before that the stock provider refuses it too.
        ("order by decimal", ctx => ctx.Products.OrderBy(p => p.Price)),
#endif
        ("include navigation", ctx => ctx.Products.Include(p => p.Category)),
#if NET9_0_OR_GREATER
        // EF Core translates SUM over a decimal from EF Core 9 on; before that the stock provider refuses it too.
        (
            "group by with aggregate",
            ctx => ctx.Products
                .GroupBy(p => p.CategoryId)
                .Select(g => new { g.Key, Total = g.Sum(p => p.Price), Count = g.Count() })
        ),
#endif
        (
            "projection with join",
            ctx => ctx.Products.Select(p => new { p.Name, Category = p.Category!.Name })
        ),
        ("skip take", ctx => ctx.Products.OrderBy(p => p.Id).Skip(10).Take(5)),
        ("string contains", ctx => ctx.Products.Where(p => p.Name.Contains("wid"))),
        ("date filter", ctx => ctx.Products.Where(p => p.CreatedUtc > new DateTime(2026, 1, 1))),
    ];

    [TestMethod]
    [DynamicData(nameof(Queries))]
    public async Task GeneratedSql_IsIdenticalToStockProvider(
        string name,
        Func<ProductContext, IQueryable<object>> query)
    {
        await using var transport = new BsqliteInProcessTransport();
        using var stock = ContextFactory.CreateStock();
        using var ours = ContextFactory.Create(transport);

        var expected = query(stock).ToQueryString();
        var actual = query(ours).ToQueryString();

        Assert.AreEqual(expected, actual);
        Assert.IsFalse(string.IsNullOrWhiteSpace(actual), $"Query '{name}' produced no SQL.");
    }
}
