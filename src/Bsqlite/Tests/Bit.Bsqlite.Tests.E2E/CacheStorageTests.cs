using System.Text.Json;
using Bit.Bsqlite.Tests.E2E.Infrastructure;
using Microsoft.Playwright;

namespace Bit.Bsqlite.Tests.E2E;

/// <summary>The Cache Storage provider: persistence, the admin module, and several tabs on one database.</summary>
[TestClass]
public sealed class CacheStorageTests : BsqliteTest
{
    // ---------------------------------------------------------------------------------------------
    // Cache Storage persistence
    //
    // Playwright's WebKit runs with an ephemeral data store, and its Cache Storage keeps a cache's entries
    // only while some context holds that cache open: when the worker that wrote them is the sole holder and
    // is terminated, the entries are gone (the cache name survives, its contents do not). Every test here
    // restarts the worker and expects to read back what it wrote, which is exactly what that store cannot
    // do. The multi-tab tests below, where another tab holds the cache open the whole time, do run there.
    // ---------------------------------------------------------------------------------------------

    private const string WebKitDiscardsEntries = "Playwright's WebKit discards Cache Storage entries once no context holds the cache open";

    [TestMethod]
    public async Task Persistence_survives_a_worker_restart()
    {
        SkipWhen(BrowserName == "webkit", WebKitDiscardsEntries);

        var databaseName = UniqueName("cache-persist");
        await OpenCacheAsync(Page, databaseName);
        await ExecAsync(Page, "CREATE TABLE product (id INTEGER PRIMARY KEY, name TEXT)");
        await ExecAsync(Page, "INSERT INTO product (name) VALUES ('Kept')");
        await Page.EvaluateAsync(
            """
            async () => {
              await globalThis.host.close();
              globalThis.host.dispose();
            }
            """);

        await OpenCacheAsync(Page, databaseName);
        AssertJson.AreEqual("""[["Kept"]]""", (await QueryAsync(Page, "SELECT name FROM product")).Rows);
    }

    [TestMethod]
    public async Task Persistence_an_uncommitted_write_does_not_survive_a_killed_worker()
    {
        SkipWhen(BrowserName == "webkit", WebKitDiscardsEntries);

        var databaseName = UniqueName("cache-crash");
        await OpenCacheAsync(Page, databaseName);
        await ExecAsync(Page, "CREATE TABLE product (id INTEGER PRIMARY KEY, name TEXT)");
        await ExecAsync(Page, "INSERT INTO product (name) VALUES ('Committed')");
        await ExecAsync(Page, "BEGIN");
        await ExecAsync(Page, "INSERT INTO product (name) VALUES ('Ghost')");
        await Page.EvaluateAsync("() => globalThis.host.dispose()");

        await OpenCacheAsync(Page, databaseName);
        AssertJson.AreEqual("""[["Committed"]]""", (await QueryAsync(Page, "SELECT name FROM product ORDER BY id")).Rows);
    }

    [TestMethod]
    public async Task Persistence_opens_a_database_that_besql_left_in_bit_Besql()
    {
        SkipWhen(BrowserName == "webkit", WebKitDiscardsEntries);

        var databaseName = UniqueName("cache-besql");
        await OpenCacheAsync(Page, databaseName);
        await ExecAsync(Page, "CREATE TABLE product (id INTEGER PRIMARY KEY, name TEXT)");
        await ExecAsync(Page, "INSERT INTO product (name) VALUES ('FromBesql')");
        await Page.EvaluateAsync(
            """
            async () => {
              await globalThis.host.close();
              globalThis.host.dispose();
            }
            """);

        await Page.EvaluateAsync(
            """
            async name => {
              const admin = await import('/_content/Bit.Bsqlite.Storage.CacheStorage/bit-bsqlite-cache-admin.js');
              const image = await admin.exportDatabase(name);
              await admin.deleteDatabase(name);
              const besql = await caches.open('bit-Besql');
              await besql.put(`/data/cache/${name}`, new Response(image));
            }
            """, databaseName);

        await OpenCacheAsync(Page, databaseName);
        AssertJson.AreEqual("""[["FromBesql"]]""", (await QueryAsync(Page, "SELECT name FROM product")).Rows);
    }

    // ---------------------------------------------------------------------------------------------
    // Cache Storage admin
    // ---------------------------------------------------------------------------------------------

    [TestMethod]
    public async Task Admin_import_export_list_and_delete()
    {
        await GotoHostPageAsync(Page);

        var name = UniqueName("cache-admin");
        var image = Enumerable.Range(0, 64).Select(i => i * 7 % 251).ToArray();
        "SQLite format 3\0".Select(c => (int)c).ToArray().CopyTo(image, 0);

        var outcome = await Page.EvaluateAsync<JsonElement>(
            """
            async ({ name, image }) => {
              const admin = await import('/_content/Bit.Bsqlite.Storage.CacheStorage/bit-bsqlite-cache-admin.js');
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

    // ---------------------------------------------------------------------------------------------
    // Cache Storage multi-tab
    // ---------------------------------------------------------------------------------------------

    [TestMethod]
    public async Task Multi_tab_a_second_tab_sees_a_committed_write()
    {
        var databaseName = UniqueName("cache-tabs");
        var context = await NewContextAsync();
        var first = await context.NewPageAsync();
        var second = await context.NewPageAsync();

        try
        {
            await OpenCacheAsync(first, databaseName);
            await ExecAsync(first, "CREATE TABLE product (id INTEGER PRIMARY KEY, name TEXT)");
            await ExecAsync(first, "INSERT INTO product (name) VALUES ('Shared')");

            await OpenCacheAsync(second, databaseName);
            AssertJson.AreEqual("""[["Shared"]]""", (await QueryAsync(second, "SELECT name FROM product")).Rows);
        }
        finally
        {
            await context.CloseAsync();
        }
    }

    /// <summary>
    /// The harder order: the reader opened while the file was still empty. Its VFS cached a size of zero, and
    /// every read transaction since has to notice that the other tab grew the file.
    /// </summary>
    [TestMethod]
    public async Task Multi_tab_a_tab_that_opened_first_sees_a_write_that_grew_the_file()
    {
        var databaseName = UniqueName("cache-grow");
        var context = await NewContextAsync();
        var reader = await context.NewPageAsync();
        var writer = await context.NewPageAsync();

        try
        {
            await OpenCacheAsync(reader, databaseName);
            AssertJson.AreEqual("[]", (await QueryAsync(reader, "SELECT name FROM sqlite_master WHERE type = 'table'")).Rows);

            await OpenCacheAsync(writer, databaseName);
            await ExecAsync(writer, "CREATE TABLE product (id INTEGER PRIMARY KEY, name TEXT)");
            await ExecAsync(writer, "INSERT INTO product (name) VALUES ('Grown')");

            AssertJson.AreEqual("""[["Grown"]]""", (await QueryAsync(reader, "SELECT name FROM product")).Rows);
        }
        finally
        {
            await context.CloseAsync();
        }
    }

    private static Task<HostVersion> OpenCacheAsync(IPage page, string databaseName) => OpenHostAsync(page, Storages.CacheStorage(databaseName));
}
