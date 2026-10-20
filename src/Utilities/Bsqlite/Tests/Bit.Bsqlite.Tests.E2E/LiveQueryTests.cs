using System.Text.Json;
using Bit.Bsqlite.Tests.E2E.Infrastructure;

namespace Bit.Bsqlite.Tests.E2E;

/// <summary>Live queries.</summary>
[TestClass]
public sealed class LiveQueryTests : BsqliteTest
{
    [TestMethod]
    public async Task A_write_notifies_the_host_with_the_table_name()
    {
        await OpenHostAsync(Page, new HostOptions { DatabaseName = $"live-{Now()}" });
        await ExecAsync(Page, "CREATE TABLE product (id INTEGER PRIMARY KEY, name TEXT)");

        var tables = await Page.EvaluateAsync<string[]>(
            """
            async () => {
              const seen = new Promise(resolve => {
                globalThis.host.onNotify(payload => resolve(payload.tables));
              });
              await globalThis.host.execute([{
                commandText: "INSERT INTO product (name) VALUES ('Live')",
                resultKind: 'nonQuery',
              }]);
              return await seen;
            }
            """);

        CollectionAssert.Contains(tables, "product");
    }

    /// <summary>
    /// Nothing goes out while a transaction is open: another tab that re-ran on the INSERT could not see the
    /// row and would never hear about the commit. The COMMIT batch is what carries the name.
    /// </summary>
    [TestMethod]
    public async Task A_transaction_is_reported_once_at_commit_with_every_table_it_wrote()
    {
        await OpenHostAsync(Page, new HostOptions { DatabaseName = $"live-tx-{Now()}" });
        await ExecAsync(Page, "CREATE TABLE product (id INTEGER PRIMARY KEY, name TEXT)");
        await ExecAsync(Page, "CREATE TABLE customer (id INTEGER PRIMARY KEY, name TEXT)");

        var notifications = await Page.EvaluateAsync<JsonElement>(
            """
            async () => {
              const seen = [];
              globalThis.host.onNotify(payload => seen.push([...payload.tables].sort()));

              const run = sql => globalThis.host.execute([{ commandText: sql, resultKind: 'nonQuery' }]);
              await run('BEGIN');
              await run("INSERT INTO product (name) VALUES ('Live')");
              await run("INSERT INTO customer (name) VALUES ('Live')");
              const beforeCommit = seen.length;
              await run('COMMIT');
              return { beforeCommit, seen };
            }
            """);

        Assert.AreEqual(0, notifications.GetProperty("beforeCommit").GetInt32());
        AssertJson.AreEqual("""[["customer", "product"]]""", notifications.GetProperty("seen"));
    }

    [TestMethod]
    public async Task A_rolled_back_transaction_is_not_reported()
    {
        await OpenHostAsync(Page, new HostOptions { DatabaseName = $"live-rollback-{Now()}" });
        await ExecAsync(Page, "CREATE TABLE product (id INTEGER PRIMARY KEY, name TEXT)");

        var count = await Page.EvaluateAsync<int>(
            """
            async () => {
              let seen = 0;
              globalThis.host.onNotify(() => seen++);

              const run = sql => globalThis.host.execute([{ commandText: sql, resultKind: 'nonQuery' }]);
              await run('BEGIN');
              await run("INSERT INTO product (name) VALUES ('Ghost')");
              await run('ROLLBACK');
              return seen;
            }
            """);

        Assert.AreEqual(0, count);
    }

    /// <summary>
    /// The update hook sees what the SQL text cannot: nothing in this DELETE names the child table, yet its
    /// rows are gone and a live query over it has to know.
    /// </summary>
    [TestMethod]
    public async Task A_cascading_delete_names_the_child_table()
    {
        await OpenHostAsync(Page, new HostOptions { DatabaseName = $"live-cascade-{Now()}" });
        await ExecAsync(Page, "CREATE TABLE parent (id INTEGER PRIMARY KEY)");
        await ExecAsync(Page, "CREATE TABLE child (id INTEGER PRIMARY KEY, parent_id INTEGER REFERENCES parent(id) ON DELETE CASCADE)");
        await ExecAsync(Page, "INSERT INTO parent (id) VALUES (1)");
        await ExecAsync(Page, "INSERT INTO child (id, parent_id) VALUES (1, 1)");

        var tables = await Page.EvaluateAsync<string[]>(
            """
            async () => {
              const seen = new Promise(resolve => {
                globalThis.host.onNotify(payload => resolve([...payload.tables].sort()));
              });
              await globalThis.host.execute([{ commandText: 'DELETE FROM parent WHERE id = 1', resultKind: 'nonQuery' }]);
              return await seen;
            }
            """);

        CollectionAssert.AreEqual(new[] { "child", "parent" }, tables);
    }

    [TestMethod]
    public async Task A_second_tab_hears_the_write_over_BroadcastChannel()
    {
        var databaseName = $"live-tab-{Now()}";
        var context = await NewContextAsync();
        var first = await context.NewPageAsync();
        var second = await context.NewPageAsync();

        try
        {
            await OpenHostAsync(first, new HostOptions { DatabaseName = databaseName });
            await OpenHostAsync(second, new HostOptions { DatabaseName = $"{databaseName}-other" });
            await ExecAsync(first, "CREATE TABLE product (id INTEGER PRIMARY KEY, name TEXT)");

            var notified = second.EvaluateAsync<string[]>(
                """
                () => new Promise(resolve => {
                  globalThis.host.onNotify(payload => resolve(payload.tables));
                })
                """);

            await ExecAsync(first, "INSERT INTO product (name) VALUES ('CrossTab')");
            CollectionAssert.Contains(await notified, "product");
        }
        finally
        {
            await context.CloseAsync();
        }
    }

    /// <summary><c>Date.now()</c>, which the spec put in its database names.</summary>
    private static long Now() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
}
