using System.Text.Json;
using Bit.Bsqlite.Tests.E2E.Infrastructure;

namespace Bit.Bsqlite.Tests.E2E;

/// <summary>
/// A Blazor application is routinely published under a sub-path - GitHub Pages, an IIS virtual directory -
/// where <c>&lt;base href="/app/"&gt;</c> puts every static asset under <c>/app/_content/...</c>. The .NET
/// providers therefore name their modules relative to the document, and the host resolves those names
/// against the document base before the worker imports them. This page lives under /nested/ and serves the
/// library from /nested/_content/, so a root-relative URL anywhere in the chain - the VFS module, the engine's
/// base classes the vendored VFS imports, the worker itself - fails here.
/// </summary>
[TestClass]
public sealed class SubPathTests : BsqliteTest
{
    [TestMethod]
    [DataRow("opfs", "opfs-coop-sync", "synchronous", "./_content/Bit.Bsqlite.Storage.Opfs/bit-bsqlite-opfs-vfs.js",
        DisplayName = "opfs opens through a document-relative module URL")]
    [DataRow("indexeddb", "idb-batch-atomic", "asyncCapable", "./_content/Bit.Bsqlite.Storage.IndexedDb/bit-bsqlite-idb-vfs.js",
        DisplayName = "indexeddb opens through a document-relative module URL")]
    [DataRow("cache-storage", "cache-storage", "asyncCapable", "./_content/Bit.Bsqlite.Storage.CacheStorage/bit-bsqlite-cache-register.js",
        DisplayName = "cache-storage opens through a document-relative module URL")]
    public async Task Opens_through_a_document_relative_module_URL(string name, string vfsName, string requiredBuild, string moduleUrl)
    {
        if (name == "opfs")
        {
            await SkipUnlessOpfsAsync(Page, "/nested/index.html");
        }

        await GotoHostPageAsync(Page, "/nested/index.html");

        var result = await Page.EvaluateAsync<JsonElement>(
            """
            async ({ requiredBuild, moduleUrl }) => {
              const host = globalThis.BitBsqlite.host.createHost();
              try {
                const opened = await host.call({
                  kind: 'open',
                  databaseName: `sub-path-${Date.now()}.db`,
                  requiredBuild,
                  vfs: { moduleUrl, registerExport: 'register' },
                });

                const queried = await host.call({
                  kind: 'execute',
                  batch: [{ commandText: 'SELECT 6 * 7', resultKind: 'reader', parameters: [] }],
                });

                return { opened, queried };
              } finally {
                host.dispose();
              }
            }
            """, new { name, vfsName, requiredBuild, moduleUrl });

        // The build is whichever one this browser got (expect.any(String)); everything else is exact.
        var opened = result.GetProperty("opened");
        var build = opened.GetProperty("result").GetProperty("build");
        Assert.AreEqual(JsonValueKind.String, build.ValueKind);
        AssertJson.AreEqual(
            JsonSerializer.Serialize(new { ok = true, result = new { build = build.GetString(), vfsName, reused = false } }),
            opened);

        var queried = result.GetProperty("queried");
        Assert.IsTrue(queried.GetProperty("ok").GetBoolean());
        AssertJson.AreEqual("[42]", queried.GetProperty("result")[0].GetProperty("rows")[0].GetProperty("v"));
    }
}
