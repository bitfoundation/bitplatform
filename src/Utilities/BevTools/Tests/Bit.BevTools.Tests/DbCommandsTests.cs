using System.Text.Json;
using Bunit;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Bit.BevTools.Tests;

/// <summary>
/// Entity Framework Core's commands, read from what it logs: a real context on an in-memory SQLite database, so a
/// change in EF's log format breaks a test here rather than emptying the Database view.
/// </summary>
[TestClass]
public class DbCommandsTests
{
    public sealed class Order
    {
        public int Id { get; set; }

        public string Customer { get; set; } = "";
    }

    private sealed class ShopContext(DbContextOptions<ShopContext> options) : DbContext(options)
    {
        public DbSet<Order> Orders => Set<Order>();
    }

    private static (BevToolsSession Session, IServiceScope Scope, ShopContext Context, SqliteConnection Connection) Create(Action<BitBevToolsOptions>? configure = null)
    {
        var (session, scope) = TestHelpers.CreateSession(configure, services => services.AddLogging(logging => logging.SetMinimumLevel(LogLevel.Information)));
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        var context = new ShopContext(new DbContextOptionsBuilder<ShopContext>()
            .UseSqlite(connection)
            .UseLoggerFactory(scope.ServiceProvider.GetRequiredService<ILoggerFactory>())
            .Options);
        using (session.UseAmbient())
        {
            context.Database.EnsureCreated();
            context.Orders.AddRange(Enumerable.Range(1, 12).Select(i => new Order { Id = i, Customer = "c" + i }));
            context.SaveChanges();
        }

        session.Logs.Clear();
        return (session, scope, context, connection);
    }

    [TestMethod]
    public void Each_command_is_read_with_its_sql_and_time_and_one_per_row_is_a_finding()
    {
        var (session, scope, context, connection) = Create();
        using (scope)
        using (connection)
        using (context)
        {
            using (session.UseAmbient())
            {
                // One query per row: the N+1 shape.
                for (var id = 1; id <= 12; id++)
                {
                    _ = context.Orders.AsNoTracking().FirstOrDefault(o => o.Id == id);
                }
            }

            var commands = session.Logs.Snapshot().Where(l => l.DbCommand is not null).ToArray();
            Assert.HasCount(12, commands);
            var first = commands[0];
            Assert.AreEqual(BevDbCommands.Category, first.Category);
            Assert.AreEqual(BevDbCommands.ExecutedEventId, first.EventId);
            StringAssert.Contains(first.DbCommand!.CommandText, "SELECT");
            StringAssert.Contains(first.DbCommand.CommandText, "FROM \"Orders\"");
            Assert.IsNotNull(first.DbCommand.ElapsedMs);
            Assert.IsFalse(first.DbCommand.Failed);

            var summary = Assert.ContainsSingle(BevDbCommands.Summarize(session.Logs.Snapshot()));
            Assert.AreEqual(12, summary.Executions);

            session.Diagnostics.Evaluate(force: true);
            var finding = Assert.ContainsSingle(f => f.Diagnostic.RuleId == "db.per-item", session.Diagnostics.Findings).Diagnostic;
            StringAssert.Contains(finding.Title, "ran 12 times");
            StringAssert.Contains(finding.Evidence, "12 executions of one command");

            using var json = JsonDocument.Parse(BevSessionExporter.ToJson(session, new BevSessionExportOptions { Sections = new HashSet<string> { "logs" } }));
            Assert.AreEqual(12, json.RootElement.GetProperty("dbCommands")[0].GetProperty("executions").GetInt32());
            Assert.IsTrue(json.RootElement.GetProperty("logs")[0].GetProperty("dbCommand").TryGetProperty("elapsedMs", out _));
        }
    }

    [TestMethod]
    public void A_failed_command_is_marked_failed_and_is_an_error()
    {
        var (session, scope, context, connection) = Create();
        using (scope)
        using (connection)
        using (context)
        {
            using (session.UseAmbient())
            {
                Assert.ThrowsExactly<SqliteException>(() => context.Database.ExecuteSqlRaw("SELECT * FROM Missing"));
            }

            var failed = Assert.ContainsSingle(l => l.DbCommand is { Failed: true }, session.Logs.Snapshot());
            Assert.AreEqual(BevDbCommands.FailedEventId, failed.EventId);
            Assert.AreEqual("SELECT * FROM Missing", failed.DbCommand!.CommandText);
            Assert.Contains(e => e.Source == "log" && e.Message.StartsWith("Command:", StringComparison.Ordinal), session.Errors.Snapshot());
        }
    }

    [TestMethod]
    public void Value_access_none_keeps_the_sql_unread_but_counts_and_times_the_commands()
    {
        var (session, scope, context, connection) = Create(o => o.ValueAccess = BevValueAccess.None);
        using (scope)
        using (connection)
        using (context)
        {
            using (session.UseAmbient())
            {
                _ = context.Orders.Count();
            }

            var command = Assert.ContainsSingle(session.Logs.Snapshot().Where(l => l.DbCommand is not null)).DbCommand!;
            Assert.IsNull(command.CommandText);
            Assert.IsNotNull(command.ElapsedMs);
            var exported = BevSessionExporter.ToJson(session);
            Assert.DoesNotContain("SELECT", exported, StringComparison.Ordinal);

            // The unread commands' group is marked as such, not exported as a command whose SQL is "".
            using var json = JsonDocument.Parse(exported);
            var group = Assert.ContainsSingle(json.RootElement.GetProperty("dbCommands").EnumerateArray());
            Assert.AreEqual(JsonValueKind.Null, group.GetProperty("commandText").ValueKind);
            Assert.AreEqual(1, group.GetProperty("executions").GetInt32());
        }
    }

    [TestMethod]
    public void A_slow_command_is_a_finding()
    {
        var (session, scope, context, connection) = Create(o => o.Diagnostics.SlowDbCommandMs = 0);
        using (scope)
        using (connection)
        using (context)
        {
            using (session.UseAmbient())
            {
                _ = context.Orders.Count();
            }

            session.Diagnostics.Evaluate(force: true);
            var finding = Assert.ContainsSingle(f => f.Diagnostic.RuleId == "db.slow", session.Diagnostics.Findings).Diagnostic;
            StringAssert.StartsWith(finding.Title, "A database command took");
            StringAssert.Contains(finding.Evidence, "COUNT(*)");
        }
    }

    [TestMethod]
    public void Elapsed_times_are_read_whatever_the_group_separator_and_sql_is_compared_without_its_layout()
    {
        IReadOnlyList<KeyValuePair<string, object?>> Pairs(string elapsed) =>
            [new("elapsed", elapsed), new("commandText", "SELECT 1"), new("{OriginalFormat}", "Executed DbCommand ({elapsed}ms)")];

        Assert.AreEqual(1234, BevDbCommands.Read(20101, Pairs("1,234"), BevValueAccess.Full).ElapsedMs);
        Assert.AreEqual(1234, BevDbCommands.Read(20101, Pairs("1.234"), BevValueAccess.Full).ElapsedMs);
        Assert.IsNull(BevDbCommands.Read(20101, Pairs("n/a"), BevValueAccess.Full).ElapsedMs);
        Assert.AreEqual("SELECT a FROM b WHERE c = @p0", BevDbCommands.Normalize("SELECT a\r\n  FROM b\n\tWHERE c = @p0  "));
        // A literal's whitespace is its value: two commands that differ only in it are two commands.
        Assert.AreEqual("SELECT \"a  b\" FROM t WHERE n = 'x  y' AND m = 'it''s  z'", BevDbCommands.Normalize("SELECT  \"a  b\"\n FROM t WHERE n = 'x  y'  AND m = 'it''s  z'"));
        Assert.AreNotEqual(BevDbCommands.Normalize("WHERE n = 'a  b'"), BevDbCommands.Normalize("WHERE n = 'a b'"));
    }

    [TestMethod]
    public void Failed_executions_are_no_query_per_item()
    {
        var (session, scope, context, connection) = Create();
        using (scope)
        using (connection)
        using (context)
        {
            using (session.UseAmbient())
            {
                for (var i = 0; i < 12; i++)
                {
                    Assert.ThrowsExactly<SqliteException>(() => context.Database.ExecuteSqlRaw("SELECT * FROM Missing"));
                }
            }

            Assert.HasCount(12, session.Logs.Snapshot().Where(l => l.DbCommand is { Failed: true }));
            session.Diagnostics.Evaluate(force: true);
            Assert.DoesNotContain(f => f.Diagnostic.RuleId == "db.per-item", session.Diagnostics.Findings, "a retried failure is an error of its own");
        }
    }
}

[TestClass]
public class DatabaseViewTests : BunitContext
{
    private static readonly Action<ILogger, string, string, Exception?> Executed = LoggerMessage.Define<string, string>(
        LogLevel.Information, new EventId(BevDbCommands.ExecutedEventId, "Microsoft.EntityFrameworkCore.Database.Command.CommandExecuted"), "Executed DbCommand ({elapsed}ms){commandText}");

    public DatabaseViewTests()
    {
        Services.AddBitBevToolsServices(o =>
        {
            o.Enabled = true;
            o.Ui.OpenByDefault = true;
        });
        Services.AddLogging();
        Services.Replace(ServiceDescriptor.Singleton<ILoggerFactory, LoggerFactory>());
        JSInterop.Mode = JSRuntimeMode.Loose;
        this.UseRenderer("Server");
    }

    [TestMethod]
    public void The_database_view_groups_commands_by_sql_and_opens_one_s_executions()
    {
        var session = Services.GetRequiredService<BevToolsSession>();
        var logger = Services.GetRequiredService<ILoggerFactory>().CreateLogger(BevDbCommands.Category);
        using (session.UseAmbient())
        {
            for (var i = 0; i < 3; i++)
            {
                Executed(logger, "4", "SELECT \"o\".\"Id\" FROM \"Orders\" AS \"o\" WHERE \"o\".\"Id\" = @p0", null);
            }

            Executed(logger, "900", "SELECT COUNT(*) FROM \"Orders\"", null);
        }

        var panel = Render<BevToolsPanel>();
        panel.FindAll(".bev-tab").First(t => t.TextContent.Trim().StartsWith("Logs", StringComparison.Ordinal)).Click();
        panel.FindAll(".bev-segmented button").First(b => b.TextContent == "Database").Click();
        var rows = panel.FindAll(".bev-body tbody tr.bev-row");
        Assert.HasCount(2, rows);
        StringAssert.Contains(rows[0].TextContent, "WHERE");
        StringAssert.Contains(rows[0].TextContent, "3");

        rows[0].QuerySelector("button")!.Click();
        Assert.HasCount(3, panel.FindAll(".bev-body tbody tr.bev-row"), "the command's executions");
        StringAssert.Contains(panel.Markup, "Show every entry");
    }
}
