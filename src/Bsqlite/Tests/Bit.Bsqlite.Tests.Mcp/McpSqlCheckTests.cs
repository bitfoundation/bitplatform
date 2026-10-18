using Bit.Bsqlite.Data;
using Bit.Bsqlite.Demo.Server.Dtos;
using Bit.Bsqlite.Demo.Server.Services;

namespace Bit.Bsqlite.Tests.Mcp;

/// <summary>
/// CheckBsqliteSql on the statements the guards exist for, on the ones they let through, and on input
/// that is too much to check - every verdict held to what the library's own guard says for the same
/// text under the same backend's limits.
/// </summary>
[TestClass]
public class McpSqlCheckTests
{
    [TestMethod]
    public async Task Without_sql_it_judges_every_guarded_statement_on_every_backend()
    {
        var check = await CheckAsync(null);

        CollectionAssert.AreEqual(BsqliteStorageCatalog.GuardedStatements, check.Statements.Select(statement => statement.Sql).ToArray());

        foreach (var statement in check.Statements)
        {
            CollectionAssert.AreEquivalent(new[] { "opfs", "indexeddb", "cache-storage", "in-memory" }, statement.Backends.Select(verdict => verdict.Name).ToArray());
        }

        Assert.IsTrue(check.Notes.Any(note => note.Contains("Pass sql", StringComparison.Ordinal)));
    }

    [TestMethod]
    public async Task Wal_is_refused_everywhere()
    {
        var verdicts = Verdicts(await CheckAsync("PRAGMA journal_mode=WAL"));

        Assert.IsTrue(verdicts.Values.All(verdict => verdict.Accepted is false));
        Assert.IsTrue(verdicts.Values.All(verdict => verdict.Error!.StartsWith("WAL mode is not available in the browser", StringComparison.Ordinal)));
    }

    [TestMethod]
    public async Task Attach_is_refused_only_where_the_backend_declares_it_cannot_span_databases()
    {
        var verdicts = Verdicts(await CheckAsync("ATTACH DATABASE 'archive.db' AS archive"));

        Assert.IsFalse(verdicts["opfs"].Accepted);
        Assert.Contains("ATTACH is not available on this storage backend", verdicts["opfs"].Error!);
        Assert.IsTrue(verdicts["indexeddb"].Accepted);
        Assert.IsTrue(verdicts["cache-storage"].Accepted);
        Assert.IsTrue(verdicts["in-memory"].Accepted);
    }

    [TestMethod]
    public async Task Page_size_is_refused_only_where_the_backend_fixes_it()
    {
        var verdicts = Verdicts(await CheckAsync("PRAGMA page_size = 1024"));

        Assert.IsTrue(verdicts["opfs"].Accepted);
        Assert.IsFalse(verdicts["indexeddb"].Accepted);
        Assert.IsFalse(verdicts["cache-storage"].Accepted);
        Assert.IsTrue(verdicts["in-memory"].Accepted);
        Assert.Contains("PRAGMA page_size cannot be changed", verdicts["indexeddb"].Error!);
    }

    [TestMethod]
    public async Task A_query_is_accepted_and_reports_the_tables_a_live_query_over_it_watches()
    {
        var check = await CheckAsync("SELECT o.Number, c.DisplayName FROM \"Orders\" AS o INNER JOIN \"Customers\" AS c ON c.Id = o.CustomerId");
        var statement = check.Statements.Single();

        Assert.IsTrue(statement.Backends.All(verdict => verdict.Accepted));
        CollectionAssert.AreEquivalent(new[] { "Customers", "Orders" }, statement.Tables);
        Assert.IsFalse(statement.LooksLikeWrite);
        Assert.IsFalse(statement.LooksLikeSchemaChange);
        Assert.IsTrue(check.Notes.Any(note => note.Contains("re-runs when", StringComparison.Ordinal)));
    }

    [TestMethod]
    public async Task A_write_and_a_schema_change_are_told_apart()
    {
        var write = (await CheckAsync("UPDATE Products SET Price = Price * 1.1 WHERE CategoryId = 2")).Statements.Single();
        var ddl = (await CheckAsync("CREATE INDEX IX_Products_Sku ON Products (Sku)")).Statements.Single();

        Assert.IsTrue(write.LooksLikeWrite);
        Assert.IsFalse(write.LooksLikeSchemaChange);
        Assert.IsTrue(ddl.LooksLikeSchemaChange);
        CollectionAssert.Contains(ddl.Tables, "Products");
    }

    [TestMethod]
    public async Task Sql_longer_than_the_bound_is_not_half_checked()
    {
        var check = await CheckAsync(new string('x', BsqliteSqlCheck.MaxSqlLength + 1));

        Assert.IsEmpty(check.Statements);
        Assert.Contains($"{BsqliteSqlCheck.MaxSqlLength}", check.Error!);
    }

    [TestMethod]
    public async Task The_longest_sql_it_takes_is_checked_quickly_whatever_it_holds()
    {
        // The library's patterns scan from every statement boundary, and the input is a stranger's: the bound
        // on its length is what keeps the worst case - a boundary every few characters, each opening a scan -
        // a fraction of a second rather than a stalled request.
        static string Fill(string unit) => string.Concat(Enumerable.Repeat(unit, BsqliteSqlCheck.MaxSqlLength / unit.Length + 1))[..BsqliteSqlCheck.MaxSqlLength];

        string[] worst = [Fill("; with "), Fill(";/*"), Fill(";-- x\n"), Fill("pragma attach from ")];

        foreach (var sql in worst)
        {
            var clock = System.Diagnostics.Stopwatch.StartNew();

            var check = await CheckAsync(sql);

            Assert.IsNotEmpty(check.Statements);
            Assert.IsLessThan(TimeSpan.FromSeconds(2), clock.Elapsed, $"{sql[..20]}... took {clock.Elapsed}.");
        }
    }

    [TestMethod]
    public async Task The_verdicts_are_the_guards_own()
    {
        // The library's guard, run here under the limits a session on each backend gets, must say exactly
        // what the tool said - message for message.
        string[] statements =
        [
            "PRAGMA journal_mode = 'wal'",
            "attach database 'x.db' as x",
            "PRAGMA page_size=65536",
            "BEGIN; INSERT INTO t VALUES (1); ATTACH 'y.db' AS y; COMMIT;",
            "SELECT 'attach' AS word",
        ];

        foreach (var sql in statements)
        {
            var verdicts = Verdicts(await CheckAsync(sql));

            foreach (var provider in BsqliteStorageCatalog.Providers)
            {
                var capabilities = BsqliteStorageCatalog.Instance(provider.Name)!.Capabilities;
                var limits = new BsqliteRuntimeLimits
                {
                    SupportsMultiDatabaseTransactions = capabilities.SupportsMultiDatabaseTransactions,
                    CanChangePageSize = capabilities.CanChangePageSize,
                };

                string? expected = null;
                try
                {
                    BsqliteFeatureGuards.EnsureSupported(sql, limits);
                }
                catch (BsqliteException exception)
                {
                    expected = exception.Message;
                }

                Assert.AreEqual(expected is null, verdicts[provider.Name].Accepted, $"{sql} on {provider.Name}");
                Assert.AreEqual(expected, verdicts[provider.Name].Error, $"{sql} on {provider.Name}");
            }
        }
    }

    internal static Task<BsqliteSqlCheckDto> CheckAsync(string? sql)
        => McpCall.StructuredAsync<BsqliteSqlCheckDto>("CheckBsqliteSql", sql is null ? null : new() { ["sql"] = sql });

    private static Dictionary<string, BsqliteSqlVerdictDto> Verdicts(BsqliteSqlCheckDto check)
        => check.Statements.Single().Backends.ToDictionary(verdict => verdict.Name, StringComparer.Ordinal);
}
