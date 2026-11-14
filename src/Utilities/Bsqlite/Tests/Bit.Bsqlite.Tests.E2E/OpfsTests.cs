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
              const admin = await import('/_content/Bit.Bsqlite/bit-bsqlite-opfs-admin.js');
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

    /// <summary>
    /// The VFS stores a database under the URL path of its name - percent-encoded - so the admin surface has
    /// to look in the same place: a name with a space or a non-ASCII letter the engine wrote is found,
    /// listed under its own name, exported, replaced and deleted.
    /// </summary>
    [TestMethod]
    public async Task Admin_finds_a_database_the_engine_wrote_under_a_name_the_VFS_encodes()
    {
        await SkipUnlessOpfsAsync(Page);

        var databaseName = $"My Notes é {Guid.NewGuid():N}.db";
        await OpenOpfsAsync(Page, databaseName);
        await ExecAsync(Page, "CREATE TABLE note (body TEXT)");
        await ExecAsync(Page, "INSERT INTO note VALUES ('written by the engine')");
        await Page.EvaluateAsync(
            """
            async () => {
              await globalThis.host.close();
              globalThis.host.dispose();
            }
            """);

        var outcome = await Page.EvaluateAsync<JsonElement>(
            """
            async name => {
              const admin = await import('/_content/Bit.Bsqlite/bit-bsqlite-opfs-admin.js');
              const existed = await admin.exists(name);
              const listed = (await admin.list()).includes(name);
              const image = await admin.exportDatabase(name);
              await admin.importDatabase(name, image);
              const reimported = await admin.exists(name);
              await admin.deleteDatabase(name);
              return {
                existed,
                listed,
                magic: new TextDecoder().decode(image.subarray(0, 15)),
                reimported,
                existsAfterDelete: await admin.exists(name),
                rawFileLeft: await admin.exists(encodeURIComponent(name)),
              };
            }
            """, databaseName);

        Assert.IsTrue(outcome.GetProperty("existed").GetBoolean());
        Assert.IsTrue(outcome.GetProperty("listed").GetBoolean());
        Assert.AreEqual("SQLite format 3", outcome.GetProperty("magic").GetString());
        Assert.IsTrue(outcome.GetProperty("reimported").GetBoolean());
        Assert.IsFalse(outcome.GetProperty("existsAfterDelete").GetBoolean());
        Assert.IsFalse(outcome.GetProperty("rawFileLeft").GetBoolean());
    }

    /// <summary>
    /// A snapshot of an open OPFS database - the backup <c>BsqliteDatabase.ExportAsync</c> takes - on a worker
    /// that has just opened it: OPFSCoopSyncVFS hands out its access handles cooperatively, so the first read
    /// answers busy and has to be retried, which <c>sqlite3_serialize</c> on its own cannot do.
    /// </summary>
    [TestMethod]
    public async Task Snapshot_of_an_open_database_is_the_file_byte_for_byte()
    {
        await SkipUnlessOpfsAsync(Page);

        var databaseName = UniqueName("opfs-snapshot");
        await OpenOpfsAsync(Page, databaseName);
        await ExecAsync(Page, "CREATE TABLE product (id INTEGER PRIMARY KEY, name TEXT)");
        await ExecAsync(Page, "WITH RECURSIVE n(i) AS (SELECT 1 UNION ALL SELECT i + 1 FROM n WHERE i < 500) INSERT INTO product (name) SELECT 'row ' || i FROM n");
        await Page.EvaluateAsync(
            """
            async () => {
              await globalThis.host.close();
              globalThis.host.dispose();
            }
            """);

        await OpenOpfsAsync(Page, databaseName);
        var outcome = await Page.EvaluateAsync<JsonElement>(
            """
            async name => {
              const snapshot = await globalThis.host.export();
              await globalThis.host.close();
              globalThis.host.dispose();
              const admin = await import('/_content/Bit.Bsqlite/bit-bsqlite-opfs-admin.js');
              const file = await admin.exportDatabase(name);
              return {
                length: snapshot.byteLength,
                same: snapshot.byteLength === file.byteLength && snapshot.every((b, i) => b === file[i]),
              };
            }
            """, databaseName);

        Assert.IsGreaterThan(4096, outcome.GetProperty("length").GetInt32());
        Assert.IsTrue(outcome.GetProperty("same").GetBoolean());
    }

    [TestMethod]
    public async Task Admin_probe_reports_available_in_a_secure_Chrome_context()
    {
        await SkipUnlessOpfsAsync(Page);

        await GotoHostPageAsync(Page);

        var probe = await Page.EvaluateAsync<JsonElement>(
            """
            async () => {
              const admin = await import('/_content/Bit.Bsqlite/bit-bsqlite-opfs-admin.js');
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

    /// <summary>
    /// A tab closed while it held the file. Firefox lets go of a closed tab's Web Lock at once but of the sync
    /// access handle its worker held only later, so the tab left behind can win the lock and still find the
    /// file taken: its next statement has to wait that out rather than fail with NoModificationAllowedError.
    /// </summary>
    [TestMethod]
    public async Task A_tab_closed_while_it_held_the_file_does_not_lock_the_others_out()
    {
        await SkipUnlessOpfsAsync(Page);

        var databaseName = UniqueName("opfs-closed");
        var context = await NewContextAsync();
        var staying = await context.NewPageAsync();
        var closing = await context.NewPageAsync();

        try
        {
            await OpenOpfsAsync(staying, databaseName);
            await ExecAsync(staying, "CREATE TABLE product (id INTEGER PRIMARY KEY, name TEXT)");

            // The closing tab takes the file over - its write needs the access handle - and goes away holding it.
            await OpenOpfsAsync(closing, databaseName);
            await ExecAsync(closing, "INSERT INTO product (name) VALUES ('Written before closing')");
            await closing.CloseAsync();

            AssertJson.AreEqual("""[["Written before closing"]]""", (await QueryAsync(staying, "SELECT name FROM product")).Rows);
        }
        finally
        {
            await context.CloseAsync();
        }
    }

    private static Task<HostVersion> OpenOpfsAsync(IPage page, string databaseName) => OpenHostAsync(page, Storages.Opfs(databaseName));
}
