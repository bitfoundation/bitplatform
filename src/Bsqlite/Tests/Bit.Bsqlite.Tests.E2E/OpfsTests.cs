using System.Text.Json;
using System.Text.RegularExpressions;
using Bit.Bsqlite.Tests.E2E.Infrastructure;
using Microsoft.Playwright;

namespace Bit.Bsqlite.Tests.E2E;

/// <summary>The OPFS storage provider: persistence, the admin module, and several tabs on one file.</summary>
/// <remarks>
/// Every test here needs OPFS, including the multi-tab ones that open their own pages, so each starts with
/// <see cref="BsqliteTest.SkipUnlessOpfsAsync"/>.
/// </remarks>
[TestClass]
public sealed class OpfsTests : BsqliteTest
{
    // ---------------------------------------------------------------------------------------------
    // OPFS persistence
    // ---------------------------------------------------------------------------------------------

    [TestMethod]
    public async Task Persistence_survives_a_worker_restart()
    {
        await SkipUnlessOpfsAsync(Page);

        var databaseName = UniqueName("opfs-persist");
        await OpenOpfsAsync(Page, databaseName);
        await ExecAsync(Page, "CREATE TABLE product (id INTEGER PRIMARY KEY, name TEXT)");
        await ExecAsync(Page, "INSERT INTO product (name) VALUES ('Kept')");

        await Page.EvaluateAsync(
            """
            async () => {
              await globalThis.host.close();
              globalThis.host.dispose();
            }
            """);

        await OpenOpfsAsync(Page, databaseName);
        AssertJson.AreEqual("""[["Kept"]]""", (await QueryAsync(Page, "SELECT name FROM product")).Rows);
    }

    [TestMethod]
    public async Task Persistence_an_uncommitted_write_does_not_survive_a_killed_worker()
    {
        await SkipUnlessOpfsAsync(Page);

        var databaseName = UniqueName("opfs-crash");
        await OpenOpfsAsync(Page, databaseName);
        await ExecAsync(Page, "CREATE TABLE product (id INTEGER PRIMARY KEY, name TEXT)");
        await ExecAsync(Page, "INSERT INTO product (name) VALUES ('Committed')");
        await ExecAsync(Page, "BEGIN");
        await ExecAsync(Page, "INSERT INTO product (name) VALUES ('Ghost')");

        await Page.EvaluateAsync("() => globalThis.host.dispose()");

        await OpenOpfsAsync(Page, databaseName);
        AssertJson.AreEqual("""[["Committed"]]""", (await QueryAsync(Page, "SELECT name FROM product ORDER BY id")).Rows);
    }

    [TestMethod]
    public async Task Persistence_two_workers_can_open_the_same_database()
    {
        await SkipUnlessOpfsAsync(Page);

        var databaseName = UniqueName("opfs-shared");
        await OpenOpfsAsync(Page, databaseName);
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
            """, Storages.Opfs(databaseName).ToArgument());

        AssertJson.AreEqual("""[["FromFirst"]]""", second);
    }

    [TestMethod]
    public async Task Persistence_ATTACH_is_rejected_rather_than_corrupting_silently()
    {
        await SkipUnlessOpfsAsync(Page);

        await OpenOpfsAsync(Page, UniqueName("opfs-attach"));

        var error = await ExecuteExpectingFailureAsync(Page, new WireRequest("ATTACH 'other.db' AS other", ResultKind: "nonQuery"));

        Assert.IsNotNull(error);
        Assert.MatchesRegex(new Regex("ATTACH", RegexOptions.IgnoreCase), error.Message);
        Assert.MatchesRegex(new Regex("transaction", RegexOptions.IgnoreCase), error.Message);
    }

    // ---------------------------------------------------------------------------------------------
    // OPFS admin
    // ---------------------------------------------------------------------------------------------

    [TestMethod]
    public async Task Admin_import_export_list_and_delete()
    {
        await SkipUnlessOpfsAsync(Page);

        await GotoHostPageAsync(Page);

        var name = UniqueName("opfs-admin");
        var image = Enumerable.Range(0, 64).Select(i => i * 7 % 251).ToArray();

        var outcome = await Page.EvaluateAsync<JsonElement>(
            """
            async ({ name, image }) => {
              const admin = await import('/_content/Bit.Bsqlite.Storage.Opfs/bit-bsqlite-opfs-admin.js');
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
    public async Task Admin_probe_reports_available_in_a_secure_Chrome_context()
    {
        await SkipUnlessOpfsAsync(Page);

        await GotoHostPageAsync(Page);

        var probe = await Page.EvaluateAsync<JsonElement>(
            """
            async () => {
              const admin = await import('/_content/Bit.Bsqlite.Storage.Opfs/bit-bsqlite-opfs-admin.js');
              return await admin.probe();
            }
            """);

        Assert.IsTrue(probe.GetProperty("available").GetBoolean());
        Assert.AreEqual("true", probe.GetProperty("environment").GetProperty("hasGetDirectory").GetString());
        Assert.AreEqual("true", probe.GetProperty("environment").GetProperty("secureContext").GetString());
    }

    // ---------------------------------------------------------------------------------------------
    // OPFS multi-tab
    // ---------------------------------------------------------------------------------------------

    [TestMethod]
    public async Task Multi_tab_a_second_tab_sees_a_committed_write()
    {
        await SkipUnlessOpfsAsync(Page);

        var databaseName = UniqueName("opfs-tabs");
        var context = await NewContextAsync();
        var first = await context.NewPageAsync();
        var second = await context.NewPageAsync();

        try
        {
            await OpenOpfsAsync(first, databaseName);
            await ExecAsync(first, "CREATE TABLE product (id INTEGER PRIMARY KEY, name TEXT)");
            await ExecAsync(first, "INSERT INTO product (name) VALUES ('Shared')");

            await OpenOpfsAsync(second, databaseName);
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
        await SkipUnlessOpfsAsync(Page);

        var databaseName = UniqueName("opfs-grow");
        var context = await NewContextAsync();
        var reader = await context.NewPageAsync();
        var writer = await context.NewPageAsync();

        try
        {
            await OpenOpfsAsync(reader, databaseName);
            AssertJson.AreEqual("[]", (await QueryAsync(reader, "SELECT name FROM sqlite_master WHERE type = 'table'")).Rows);

            await OpenOpfsAsync(writer, databaseName);
            await ExecAsync(writer, "CREATE TABLE product (id INTEGER PRIMARY KEY, name TEXT)");
            await ExecAsync(writer, "INSERT INTO product (name) VALUES ('Grown')");

            AssertJson.AreEqual("""[["Grown"]]""", (await QueryAsync(reader, "SELECT name FROM product")).Rows);
        }
        finally
        {
            await context.CloseAsync();
        }
    }

    private static Task<HostVersion> OpenOpfsAsync(IPage page, string databaseName) => OpenHostAsync(page, Storages.Opfs(databaseName));
}
