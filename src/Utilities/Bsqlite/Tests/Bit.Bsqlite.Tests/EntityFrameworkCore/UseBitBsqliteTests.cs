using Bit.Bsqlite.Data;
using Bit.Bsqlite.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage;

namespace Bit.Bsqlite.EntityFrameworkCore.Tests;

/// <summary>
/// <c>UseBitBsqlite</c> is the stock SQLite provider plus two service replacements. These tests
/// pin that shape so a future change cannot quietly become a custom provider.
/// </summary>
[TestClass]
public sealed class UseBitBsqliteTests
{
    public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    public void ProviderName_IsStockSqlite()
    {
        using var stock = ContextFactory.CreateStock();
        using var ours = ContextFactory.Create(new BsqliteInProcessTransport());

        Assert.AreEqual("Microsoft.EntityFrameworkCore.Sqlite", stock.Database.ProviderName);
        Assert.AreEqual(stock.Database.ProviderName, ours.Database.ProviderName);
    }

    [TestMethod]
    public void ReplacesTheTwoSyncBoundServices()
    {
        using var ctx = ContextFactory.Create(new BsqliteInProcessTransport());

        Assert.IsInstanceOfType<BsqliteDatabaseCreator>(ctx.GetService<IRelationalDatabaseCreator>());
        Assert.IsInstanceOfType<BsqliteHistoryRepository>(ctx.GetService<IHistoryRepository>());
    }

    [TestMethod]
    public async Task StockUseSqlite_EnsureCreatedAsync_StillThrows()
    {
        // The reason the veneer exists: UseSqlite(connection) alone reaches Open() through
        // RelationalDatabaseCreator.CreateAsync. If that ever becomes async in EF, this test
        // fails and the replacements can be re-evaluated.
        await using var transport = new BsqliteInProcessTransport();
        await using var ctx = new ProductContext(new DbContextOptionsBuilder<ProductContext>()
            .UseSqlite(new BsqliteConnection(transport, "stock.db"))
            .Options);

        var failure = await Assert.ThrowsExactlyAsync<BsqliteSynchronousApiNotSupportedException>(
            () => ctx.Database.EnsureCreatedAsync(TestContext.CancellationToken));

        Assert.Contains("Open", failure.Message, StringComparison.Ordinal);
    }

    [TestMethod]
    public void StockSqliteConnection_ServerVersion_IsAtLeastJsonFunctions()
    {
        // EF Core 10's query translator does this before compiling any query. On WASM there is
        // no e_sqlite3; UseBitBsqlite installs a PCL stub so the call does not throw.
        var version = new Version(new Microsoft.Data.Sqlite.SqliteConnection().ServerVersion);
        Assert.IsTrue(version >= new Version(3, 38));
    }
}
