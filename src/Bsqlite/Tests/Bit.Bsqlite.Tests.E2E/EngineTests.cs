using System.Text.Json;
using Bit.Bsqlite.Tests.E2E.Infrastructure;

namespace Bit.Bsqlite.Tests.E2E;

/// <summary>Engine loading and build negotiation.</summary>
[TestClass]
public sealed class EngineTests : BsqliteTest
{
    [TestMethod]
    public async Task Loads_the_synchronous_build_and_reports_the_pinned_SQLite_version()
    {
        var (version, build) = await OpenHostAsync(Page);

        Assert.AreEqual("synchronous", build);

        // Pinned through the vendored artifacts, so a silent engine change fails here rather than showing
        // up as different SQL behaviour much later.
        Assert.AreEqual("3.53.0", version);
    }

    /// <summary>
    /// EF Core gates SQL translations on the engine's version, and it reads that version from
    /// <c>BsqlitePclProvider.sqlite3_libversion()</c> - a stub, because WebAssembly has no e_sqlite3 to ask. A
    /// stub that claims more than the vendored engine delivers is how EF ends up emitting SQL the engine cannot
    /// run, at query time, in the browser only.
    /// </summary>
    [TestMethod]
    public async Task The_engine_reports_the_version_the_dotnet_stub_claims()
    {
        var reported = await OpenHostAsync(Page);

        // Keep in step with BsqlitePclProvider.EngineVersion.
        Assert.AreEqual("3.53.0", reported.Version);
    }

    [TestMethod]
    public async Task Picks_an_async_capable_build_only_when_the_VFS_needs_one()
    {
        await GotoHostPageAsync(Page);

        var selection = await Page.EvaluateAsync<JsonElement>(
            """
            () => {
              const { selectBuild, supportsJspi } = globalThis.BitBsqlite.engine;
              return {
                jspi: supportsJspi(),
                synchronous: selectBuild('synchronous').id,
                asyncCapable: selectBuild('asyncCapable').id,
              };
            }
            """);

        Assert.AreEqual("synchronous", selection.GetProperty("synchronous").GetString());

        // The point of vendoring three builds: whichever async build this browser gets, it must be the
        // cheaper one when the browser can take it.
        Assert.AreEqual(selection.GetProperty("jspi").GetBoolean() ? "jspi" : "asyncify", selection.GetProperty("asyncCapable").GetString());
    }

    /// <summary>
    /// Loading the async build is not hypothetical work: it is what the IndexedDB and Cache Storage providers
    /// will run on, and a build that cannot execute a plain query is worth discovering now rather than behind a
    /// VFS.
    /// </summary>
    [TestMethod]
    public async Task The_async_capable_build_loads_and_executes()
    {
        var (version, build) = await OpenHostAsync(Page, new HostOptions { DatabaseName = "async.db", RequiredBuild = "asyncCapable" });

        CollectionAssert.Contains(new[] { "jspi", "asyncify" }, build);
        Assert.AreEqual("3.53.0", version);

        AssertJson.AreEqual("[[42]]", (await QueryAsync(Page, "SELECT 6 * 7")).Rows);
    }

    [TestMethod]
    public async Task Rejects_an_unknown_build_requirement_instead_of_guessing()
    {
        await GotoHostPageAsync(Page);

        var error = await Page.EvaluateAsync<string?>(
            """
            () => {
              try {
                globalThis.BitBsqlite.engine.selectBuild('whatever');
                return null;
              } catch (e) {
                return e.message;
              }
            }
            """);

        Assert.IsNotNull(error);
        Assert.Contains("whatever", error);
    }

    [TestMethod]
    public async Task A_worker_owns_one_database_and_says_so_when_asked_for_another()
    {
        await OpenHostAsync(Page, new HostOptions { DatabaseName = "first.db" });

        var error = await Page.EvaluateAsync<string?>(
            """
            async () => {
              try {
                await globalThis.host.send({
                  kind: 'open',
                  databaseName: 'second.db',
                  requiredBuild: 'synchronous',
                });
                return null;
              } catch (e) {
                return e.message;
              }
            }
            """);

        Assert.IsNotNull(error);
        Assert.Contains("first.db", error);
        Assert.Contains("own worker", error);
    }

    [TestMethod]
    public async Task Reopening_the_same_database_reuses_the_session()
    {
        await OpenHostAsync(Page, new HostOptions { DatabaseName = "same.db" });

        var result = await Page.EvaluateAsync<JsonElement>(
            """
            () => globalThis.host.send({
              kind: 'open',
              databaseName: 'same.db',
              requiredBuild: 'synchronous',
            })
            """);

        Assert.IsTrue(result.GetProperty("reused").GetBoolean());
    }
}
