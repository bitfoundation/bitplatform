using Bit.Bsqlite.Tests.E2E.Infrastructure;

namespace Bit.Bsqlite.Tests.E2E;

/// <summary>
/// The reason sticky binding exists, shown at the storage layer: the same filename on a different VFS is a
/// different, empty database. Switching backends without the binding would look like success and lose the
/// rows.
/// </summary>
[TestClass]
public sealed class StorageIsolationTests : BsqliteTest
{
    [TestMethod]
    public async Task An_IndexedDB_write_is_invisible_on_OPFS_under_the_same_name()
    {
        // Both directions involve OPFS, which Playwright's WebKit does not have.
        await SkipUnlessOpfsAsync(Page);

        var databaseName = UniqueName("iso-idb");

        await OpenHostAsync(Page, Idb(databaseName));
        await ExecAsync(Page, "CREATE TABLE product (id INTEGER PRIMARY KEY, name TEXT)");
        await ExecAsync(Page, "INSERT INTO product (name) VALUES ('OnlyInIndexedDb')");
        await Page.EvaluateAsync(
            """
            async () => {
              await globalThis.host.close();
              globalThis.host.dispose();
            }
            """);

        await OpenHostAsync(Page, Opfs(databaseName));
        var tables = await QueryAsync(
            Page,
            "SELECT name FROM sqlite_master WHERE type='table' AND name='product'");
        AssertJson.AreEqual("[]", tables.Rows);
    }

    [TestMethod]
    public async Task An_OPFS_write_is_invisible_on_IndexedDB_under_the_same_name()
    {
        // Both directions involve OPFS, which Playwright's WebKit does not have.
        await SkipUnlessOpfsAsync(Page);

        var databaseName = UniqueName("iso-opfs");

        await OpenHostAsync(Page, Opfs(databaseName));
        await ExecAsync(Page, "CREATE TABLE product (id INTEGER PRIMARY KEY, name TEXT)");
        await ExecAsync(Page, "INSERT INTO product (name) VALUES ('OnlyInOpfs')");
        await Page.EvaluateAsync(
            """
            async () => {
              await globalThis.host.close();
              globalThis.host.dispose();
            }
            """);

        await OpenHostAsync(Page, Idb(databaseName));
        var tables = await QueryAsync(
            Page,
            "SELECT name FROM sqlite_master WHERE type='table' AND name='product'");
        AssertJson.AreEqual("[]", tables.Rows);
    }

    // The two storages as the spec opened them: the VFS module and the build it needs, without limits.
    private static HostOptions Opfs(string databaseName) => new() { DatabaseName = databaseName, RequiredBuild = "synchronous", Vfs = Storages.OpfsVfs };

    private static HostOptions Idb(string databaseName) => new() { DatabaseName = databaseName, RequiredBuild = "asyncCapable", Vfs = Storages.IndexedDbVfs };
}
