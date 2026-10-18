using System.Text.Json;
using System.Text.RegularExpressions;
using Bit.Bsqlite.Tests.E2E.Infrastructure;
using Microsoft.Playwright;

namespace Bit.Bsqlite.Tests.E2E;

/// <summary>The IndexedDB storage provider: persistence, the admin module, and several tabs on one database.</summary>
[TestClass]
public sealed class IndexedDbTests : BsqliteTest
{
    // ---------------------------------------------------------------------------------------------
    // IndexedDB persistence
    // ---------------------------------------------------------------------------------------------

    [TestMethod]
    public async Task Persistence_opens_on_an_async_capable_build_not_the_synchronous_one()
    {
        var (_, build) = await OpenIdbAsync(Page, UniqueName("idb-build"));
        CollectionAssert.Contains(new[] { "jspi", "asyncify" }, build);
    }

    [TestMethod]
    public async Task Persistence_survives_a_worker_restart()
    {
        var databaseName = UniqueName("idb-persist");
        await OpenIdbAsync(Page, databaseName);
        await ExecAsync(Page, "CREATE TABLE product (id INTEGER PRIMARY KEY, name TEXT)");
        await ExecAsync(Page, "INSERT INTO product (name) VALUES ('Kept')");

        await Page.EvaluateAsync(
            """
            async () => {
              await globalThis.host.close();
              globalThis.host.dispose();
            }
            """);

        await OpenIdbAsync(Page, databaseName);
        AssertJson.AreEqual("""[["Kept"]]""", (await QueryAsync(Page, "SELECT name FROM product")).Rows);
    }

    [TestMethod]
    public async Task Persistence_an_uncommitted_write_does_not_survive_a_killed_worker()
    {
        var databaseName = UniqueName("idb-crash");
        await OpenIdbAsync(Page, databaseName);
        await ExecAsync(Page, "CREATE TABLE product (id INTEGER PRIMARY KEY, name TEXT)");
        await ExecAsync(Page, "INSERT INTO product (name) VALUES ('Committed')");
        await ExecAsync(Page, "BEGIN");
        await ExecAsync(Page, "INSERT INTO product (name) VALUES ('Ghost')");

        await Page.EvaluateAsync("() => globalThis.host.dispose()");

        await OpenIdbAsync(Page, databaseName);
        AssertJson.AreEqual("""[["Committed"]]""", (await QueryAsync(Page, "SELECT name FROM product ORDER BY id")).Rows);
    }

    [TestMethod]
    public async Task Persistence_two_workers_can_open_the_same_database()
    {
        var databaseName = UniqueName("idb-shared");
        await OpenIdbAsync(Page, databaseName);
        await ExecAsync(Page, "CREATE TABLE product (id INTEGER PRIMARY KEY, name TEXT)");
        await ExecAsync(Page, "INSERT INTO product (name) VALUES ('FromFirst')");

        var second = await Page.EvaluateAsync<JsonElement>(
            """
            async options => {
              const { decodeValue } = globalThis.BitBsqlite.host;
              const host = await globalThis.BitBsqlite.host.open(options);
              try {
                const result = await host.execute([{
                  commandText: 'SELECT name FROM product',
                  resultKind: 'reader',
                }]);
                return result[0].rows.map(row =>
                  row.v.map((value, i) => decodeValue(row.t[i], value)));
              } finally {
                host.dispose();
              }
            }
            """, Storages.IndexedDb(databaseName).ToArgument());

        AssertJson.AreEqual("""[["FromFirst"]]""", second);
    }

    [TestMethod]
    public async Task Persistence_PRAGMA_page_size_assignment_is_rejected()
    {
        await OpenIdbAsync(Page, UniqueName("idb-pagesize"));

        var error = await ExecuteExpectingFailureAsync(Page, new WireRequest("PRAGMA page_size=8192", ResultKind: "nonQuery"));

        Assert.IsNotNull(error);
        Assert.MatchesRegex(new Regex("page_size", RegexOptions.IgnoreCase), error.Message);
        Assert.MatchesRegex(new Regex("fixed", RegexOptions.IgnoreCase), error.Message);
    }

    [TestMethod]
    public async Task Persistence_the_default_page_size_is_4096()
    {
        await OpenIdbAsync(Page, UniqueName("idb-defaultpage"));
        AssertJson.AreEqual("[[4096]]", (await QueryAsync(Page, "PRAGMA page_size")).Rows);
    }

    [TestMethod]
    public async Task Persistence_cache_size_is_raised_so_batch_atomic_writes_can_keep_the_journal_in_cache()
    {
        await OpenIdbAsync(Page, UniqueName("idb-cache"));
        var cacheSize = (await QueryAsync(Page, "PRAGMA cache_size")).Rows[0][0];
        Assert.AreEqual(-8000, cacheSize.GetInt32());
    }

    [TestMethod]
    public async Task Persistence_the_synchronous_build_cannot_host_this_VFS()
    {
        await GotoHostPageAsync(Page);

        var error = await Page.EvaluateAsync<string?>(
            """
            async options => {
              try {
                await globalThis.BitBsqlite.host.open(options);
                return null;
              } catch (e) {
                return e.message;
              }
            }
            """, new HostOptions
            {
                DatabaseName = UniqueName("idb-sync"),
                RequiredBuild = "synchronous",
                Vfs = Storages.IndexedDbVfs,
            }.ToArgument());

        Assert.IsNotNull(error);
    }

    // ---------------------------------------------------------------------------------------------
    // IndexedDB admin
    // ---------------------------------------------------------------------------------------------

    [TestMethod]
    public async Task Admin_import_export_list_and_delete()
    {
        await GotoHostPageAsync(Page);

        var name = UniqueName("idb-admin");
        var image = Enumerable.Range(0, 64).Select(i => i * 7 % 251).ToArray();

        var outcome = await Page.EvaluateAsync<JsonElement>(
            """
            async ({ name, image }) => {
              const admin = await import('/_content/Bit.Bsqlite.Storage.IndexedDb/bit-bsqlite-idb-admin.js');
              await admin.importDatabase(name, new Uint8Array(image));
              const exported = Array.from(await admin.exportDatabase(name));
              const listed = await admin.list();
              const existed = await admin.exists(name);
              await admin.deleteDatabase(name);
              return {
                exported,
                listed: listed.includes(name),
                existed,
                existsAfterDelete: await admin.exists(name),
              };
            }
            """, new { name, image });

        AssertJson.AreEqual(JsonSerializer.Serialize(image), outcome.GetProperty("exported"));
        Assert.IsTrue(outcome.GetProperty("listed").GetBoolean());
        Assert.IsTrue(outcome.GetProperty("existed").GetBoolean());
        Assert.IsFalse(outcome.GetProperty("existsAfterDelete").GetBoolean());
    }

    [TestMethod]
    public async Task Admin_export_of_an_engine_created_database_is_a_SQLite_file_image()
    {
        var databaseName = UniqueName("idb-export");
        await OpenIdbAsync(Page, databaseName);
        await ExecAsync(Page, "CREATE TABLE product (id INTEGER PRIMARY KEY, name TEXT)");
        await ExecAsync(Page, "INSERT INTO product (name) VALUES ('Migrated')");
        await Page.EvaluateAsync(
            """
            async () => {
              await globalThis.host.close();
              globalThis.host.dispose();
            }
            """);

        var header = await Page.EvaluateAsync<string>(
            """
            async name => {
              const admin = await import('/_content/Bit.Bsqlite.Storage.IndexedDb/bit-bsqlite-idb-admin.js');
              const bytes = await admin.exportDatabase(name);
              return String.fromCharCode(...bytes.subarray(0, 15));
            }
            """, databaseName);

        Assert.AreEqual("SQLite format 3", header);
    }

    [TestMethod]
    public async Task Admin_probe_reports_available_when_indexedDB_exists()
    {
        await GotoHostPageAsync(Page);

        var probe = await Page.EvaluateAsync<JsonElement>(
            """
            async () => {
              const admin = await import('/_content/Bit.Bsqlite.Storage.IndexedDb/bit-bsqlite-idb-admin.js');
              return await admin.probe();
            }
            """);

        Assert.IsTrue(probe.GetProperty("available").GetBoolean());
        Assert.AreEqual("true", probe.GetProperty("environment").GetProperty("hasIndexedDB").GetString());
    }

    // ---------------------------------------------------------------------------------------------
    // IndexedDB multi-tab
    // ---------------------------------------------------------------------------------------------

    [TestMethod]
    public async Task Multi_tab_a_second_tab_sees_a_committed_write()
    {
        var databaseName = UniqueName("idb-tabs");
        var context = await NewContextAsync();
        var first = await context.NewPageAsync();
        var second = await context.NewPageAsync();

        try
        {
            await OpenIdbAsync(first, databaseName);
            await ExecAsync(first, "CREATE TABLE product (id INTEGER PRIMARY KEY, name TEXT)");
            await ExecAsync(first, "INSERT INTO product (name) VALUES ('Shared')");

            await OpenIdbAsync(second, databaseName);
            AssertJson.AreEqual("""[["Shared"]]""", (await QueryAsync(second, "SELECT name FROM product")).Rows);
        }
        finally
        {
            await context.CloseAsync();
        }
    }

    /// <summary>
    /// The harder order: the reader opened while the file was still empty and has to notice, at each read
    /// transaction, that the other tab grew it.
    /// </summary>
    [TestMethod]
    public async Task A_tab_that_opened_first_sees_a_write_that_grew_the_file()
    {
        var databaseName = UniqueName("idb-grow");
        var context = await NewContextAsync();
        var reader = await context.NewPageAsync();
        var writer = await context.NewPageAsync();

        try
        {
            await OpenIdbAsync(reader, databaseName);
            AssertJson.AreEqual("[]", (await QueryAsync(reader, "SELECT name FROM sqlite_master WHERE type = 'table'")).Rows);

            await OpenIdbAsync(writer, databaseName);
            await ExecAsync(writer, "CREATE TABLE product (id INTEGER PRIMARY KEY, name TEXT)");
            await ExecAsync(writer, "INSERT INTO product (name) VALUES ('Grown')");

            AssertJson.AreEqual("""[["Grown"]]""", (await QueryAsync(reader, "SELECT name FROM product")).Rows);
        }
        finally
        {
            await context.CloseAsync();
        }
    }

    private static Task<HostVersion> OpenIdbAsync(IPage page, string databaseName) => OpenHostAsync(page, Storages.IndexedDb(databaseName));
}
