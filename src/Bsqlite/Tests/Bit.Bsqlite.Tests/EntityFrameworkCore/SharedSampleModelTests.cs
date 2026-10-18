using Bit.Bsqlite.Demo.Data;
using Bit.Bsqlite.Demo.Domain;
using Bit.Bsqlite.Data;
using Bit.Bsqlite.Testing;
using Microsoft.EntityFrameworkCore;

namespace Bit.Bsqlite.EntityFrameworkCore.Tests;

/// <summary>
/// The sample's load-bearing claim: one migration set applies through <c>UseBitBsqlite</c>.
/// </summary>
[TestClass]
public sealed class SharedSampleModelTests
{
    public TestContext TestContext { get; set; } = null!;

    private CancellationToken Ct => TestContext.CancellationToken;

    [TestMethod]
    public async Task SampleMigrations_ApplyAndRoundTrip()
    {
        await using var transport = new BsqliteInProcessTransport();
        await using var connection = new BsqliteConnection(transport, "sample.db");
        await connection.OpenAsync(Ct);
        await using var ctx = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseBitBsqlite(connection)
            .Options);

        await ctx.Database.MigrateAsync(Ct);
        await DemoData.SeedIfEmptyAsync(ctx, Ct);

        var product = await ctx.Products.Include(p => p.Category).SingleAsync(p => p.Sku == "TL-100", Ct);
        Assert.AreEqual("Digital caliper", product.Name);
        Assert.AreEqual(42.50m, product.Price);
        Assert.AreEqual(TimeSpan.FromDays(2), product.LeadTime);
        Assert.AreEqual("Tools", product.Category!.Name);

        var customer = await ctx.Customers.SingleAsync(c => c.Email == "stores@northwind.example", Ct);
        Assert.AreEqual(new DateOnly(1978, 4, 12), customer.DateOfBirth);
        Assert.IsTrue(customer.IsVip);
        Assert.AreEqual(25_000m, customer.CreditLimit);

        var order = await ctx.Orders
            .Include(o => o.Lines)
            .ThenInclude(l => l.Product)
            .SingleAsync(o => o.Number == "SO-1042", Ct);
        Assert.AreEqual(OrderStatus.Shipped, order.Status);
        Assert.AreEqual(2, order.Lines.Count);
        Assert.Contains(l => l.Product!.Sku == "TL-100", order.Lines);
    }
}
