using Bit.Bsqlite.Data;
using Bit.Bsqlite.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Bit.Bsqlite.EntityFrameworkCore.Tests;

/// <summary>
/// Every context of a session shares the session's one connection, and in the browser every
/// statement is a round trip to the worker, so two contexts' operations interleave. These pin down
/// that they take turns the way two SQLite connections over one file would: a transaction keeps
/// every other context's statements and transactions waiting until it ends, and a wait that would
/// never end fails as a locked database.
/// </summary>
[TestClass]
public sealed class ConcurrentContextsTests
{
    public TestContext TestContext { get; set; } = null!;

    private CancellationToken Ct => TestContext.CancellationToken;

    [TestMethod]
    public async Task SavesOfTwoContexts_AtOnce_BothCommit()
    {
        await using var inner = new BsqliteInProcessTransport();
        await using var connection = await OpenSharedAsync(new YieldingTransport(inner));

        for (var run = 0; run < 20; run++)
        {
            await using var first = Create(connection);
            await using var second = Create(connection);
            first.Categories.AddRange(new Category { Name = "a1" }, new Category { Name = "a2" });
            second.Categories.AddRange(new Category { Name = "b1" }, new Category { Name = "b2" });

            // Each multi-row save is a BEGIN IMMEDIATE ... COMMIT of its own, and the transport
            // yields on every statement, so without turns the second BEGIN lands inside the first.
            await Task.WhenAll(first.SaveChangesAsync(Ct), second.SaveChangesAsync(Ct));
        }

        await using var check = Create(connection);
        Assert.AreEqual(80, await check.Categories.CountAsync(Ct));
    }

    [TestMethod]
    public async Task AnotherContextsWrite_WaitsForTheOpenTransaction_AndOutlivesItsRollback()
    {
        await using var transport = new BsqliteInProcessTransport();
        await using var connection = await OpenSharedAsync(transport);
        await using var first = Create(connection);
        await using var second = Create(connection);

        await using (var transaction = await first.Database.BeginTransactionAsync(Ct))
        {
            first.Categories.Add(new Category { Name = "rolled back" });
            await first.SaveChangesAsync(Ct);

            second.Categories.Add(new Category { Name = "kept" });
            var save = second.SaveChangesAsync(Ct);
            await Task.Delay(100, Ct);
            Assert.IsFalse(save.IsCompleted, "The other context's write ran inside a transaction it never began.");

            await transaction.RollbackAsync(Ct);
            await save;
        }

        await using var check = Create(connection);
        CollectionAssert.AreEqual(new[] { "kept" }, await check.Categories.Select(c => c.Name).ToArrayAsync(Ct));
    }

    [TestMethod]
    public async Task AnotherContextsTransaction_BeginsOnceTheOpenOneEnds()
    {
        await using var transport = new BsqliteInProcessTransport();
        await using var connection = await OpenSharedAsync(transport);
        await using var first = Create(connection);
        await using var second = Create(connection);

        var transaction = await first.Database.BeginTransactionAsync(Ct);
        var begin = second.Database.BeginTransactionAsync(Ct);
        await Task.Delay(100, Ct);
        Assert.IsFalse(begin.IsCompleted, "A second transaction began inside the first.");

        await transaction.CommitAsync(Ct);
        await transaction.DisposeAsync();

        await using var next = await begin;
        second.Categories.Add(new Category { Name = "after" });
        await second.SaveChangesAsync(Ct);
        await next.CommitAsync(Ct);

        await using var check = Create(connection);
        Assert.AreEqual(1, await check.Categories.CountAsync(Ct));
    }

    [TestMethod]
    public async Task AContextWaitingOnAnotherWhileHoldingATransaction_FailsAsLocked()
    {
        await using var transport = new BsqliteInProcessTransport();
        await using var connection = await OpenSharedAsync(transport);
        await using var first = Create(connection);
        await using var second = Create(connection);
        second.Database.SetCommandTimeout(1);

        await using var transaction = await first.Database.BeginTransactionAsync(Ct);

        // The holder of the transaction waits on the other context, which waits on the transaction:
        // SQLite's busy timeout ends that with "database is locked", and so does this.
        var error = await Assert.ThrowsExactlyAsync<BsqliteConcurrencyException>(
            () => second.Categories.CountAsync(Ct));
        StringAssert.Contains(error.Message, "is locked");

        // The holder itself is unaffected.
        first.Categories.Add(new Category { Name = "kept" });
        await first.SaveChangesAsync(Ct);
        await transaction.CommitAsync(Ct);
        Assert.AreEqual(1, await second.Categories.CountAsync(Ct));
    }

    [TestMethod]
    public async Task ADbConnectionCommand_StillJoinsTheTransactionOfItsContext()
    {
        await using var transport = new BsqliteInProcessTransport();
        await using var connection = await OpenSharedAsync(transport);
        await using var context = Create(connection);

        // ADO.NET code inside an EF transaction, leaving Transaction unset: it runs inside, as it
        // always has, rather than waiting on the transaction it is part of.
        await using (var transaction = await context.Database.BeginTransactionAsync(Ct))
        {
            await using var command = context.Database.GetDbConnection().CreateCommand();
            command.CommandText = "INSERT INTO \"Categories\" (\"Name\") VALUES ('raw')";
            await command.ExecuteNonQueryAsync(Ct);
            await transaction.RollbackAsync(Ct);
        }

        Assert.AreEqual(0, await context.Categories.CountAsync(Ct));
    }

    private async Task<BsqliteConnection> OpenSharedAsync(IBsqliteTransport transport)
    {
        // One connection for every context, the way BsqliteDbContextFactory hands them out.
        var connection = new BsqliteConnection(transport, "concurrent.db");
        await connection.OpenAsync(Ct);

        await using var context = Create(connection);
        await context.Database.EnsureCreatedAsync(Ct);
        return connection;
    }

    private static ProductContext Create(BsqliteConnection connection)
        => new(new DbContextOptionsBuilder<ProductContext>()
            .UseBitBsqlite(connection)
#if NET9_0_OR_GREATER
            .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning))
#endif
            .Options);

    /// <summary>
    /// Yields before every round trip, as the worker transport does: the in-process transport
    /// returns synchronously, so on its own two contexts' statements never interleave.
    /// </summary>
    private sealed class YieldingTransport(IBsqliteTransport inner) : IBsqliteTransport
    {
        public Task OpenAsync(string databaseName, CancellationToken cancellationToken = default)
            => inner.OpenAsync(databaseName, cancellationToken);

        public Task CloseAsync(CancellationToken cancellationToken = default)
            => inner.CloseAsync(cancellationToken);

        public async Task<IReadOnlyList<BsqliteCommandResult>> ExecuteAsync(
            IReadOnlyList<BsqliteCommandRequest> batch,
            CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            return await inner.ExecuteAsync(batch, cancellationToken).ConfigureAwait(false);
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
