using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore.Storage;
using Boilerplate.Server.Api.Infrastructure.DevMcp;

namespace Boilerplate.Tests.Features.DevMcp;

/// <summary>
/// Every database this template offers except SQLite configures EnableRetryOnFailure, and such a strategy refuses a
/// user-initiated transaction. The repo's own tree runs SQLite, so only a forced retrying strategy can prove the Dev
/// MCP's reads survive the configuration every real deployment - the demos included - actually runs.
/// </summary>
[TestClass, TestCategory("UnitTest")]
public class DevMcpReadOnlyStrategyTests
{
    public TestContext TestContext { get; set; } = default!;

    [TestMethod]
    public async Task AReadOnlyRead_Should_SucceedUnderARetryingExecutionStrategy()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync(TestContext.CancellationToken);

        var options = new DbContextOptionsBuilder<ProbeDbContext>()
            .UseSqlite(connection, sqlite => sqlite.ExecutionStrategy(dependencies => new RetryingExecutionStrategy(dependencies)))
            .Options;

        await using var db = new ProbeDbContext(options);

        Assert.IsTrue(db.Database.CreateExecutionStrategy().RetriesOnFailure,
            "This test is only meaningful while the strategy really is a retrying one.");

        var read = await DevMcpReadOnly.ReadAsync(db, async token =>
        {
            await db.Database.ExecuteSqlRawAsync("SELECT 1", token);
            return "read";
        }, TestContext.CancellationToken);

        Assert.AreEqual("read", read,
            "A Dev MCP read must open its transaction inside the execution strategy. Opening one around it throws " +
            "'does not support user-initiated transactions' on SQL Server, PostgreSQL and MySQL alike.");
    }

    private sealed class ProbeDbContext(DbContextOptions<ProbeDbContext> options) : DbContext(options);

    private sealed class RetryingExecutionStrategy(ExecutionStrategyDependencies dependencies)
        : ExecutionStrategy(dependencies, maxRetryCount: 1, maxRetryDelay: TimeSpan.FromMilliseconds(1))
    {
        protected override bool ShouldRetryOn(Exception exception) => false;
    }
}
