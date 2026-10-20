using System.Text.RegularExpressions;
using Bit.Bsqlite.Tests.E2E.Infrastructure;

namespace Bit.Bsqlite.Tests.E2E;

/// <summary>
/// The engine-layer rules, run against the worker's built-in memory VFS - the InMemory provider's actual
/// engine path. The C# kit runs the same rules against the in-process transport; a divergence between the two
/// is a bug in one of them.
/// </summary>
[TestClass]
public sealed class EngineConformanceTests : BsqliteTest
{
    [TestMethod]
    public async Task A_rolled_back_write_is_invisible()
    {
        await OpenAsync();

        await ExecAsync(Page, "BEGIN; INSERT INTO product (name) VALUES ('Ghost'); ROLLBACK;");

        AssertJson.AreEqual("[[0]]", (await QueryAsync(Page, "SELECT COUNT(*) FROM product")).Rows);
    }

    [TestMethod]
    public async Task A_committed_write_is_visible()
    {
        await OpenAsync();

        await ExecAsync(Page, "BEGIN; INSERT INTO product (name) VALUES ('Kept'); COMMIT;");

        AssertJson.AreEqual("""[["Kept"]]""", (await QueryAsync(Page, "SELECT name FROM product")).Rows);
    }

    [TestMethod]
    public async Task Journal_mode_WAL_is_rejected_rather_than_silently_ignored()
    {
        await OpenAsync();

        var error = await ExecuteExpectingFailureAsync(Page, new WireRequest("PRAGMA journal_mode=WAL", ResultKind: "nonQuery"));

        Assert.IsNotNull(error);
        Assert.MatchesRegex(new Regex("WAL", RegexOptions.IgnoreCase), error.Message);
        Assert.MatchesRegex(new Regex("shared-memory", RegexOptions.IgnoreCase), error.Message);
    }

    // The describe block's beforeEach: the in-memory database every test here starts from.
    private async Task OpenAsync()
    {
        await OpenHostAsync(Page, new HostOptions { DatabaseName = "conformance.db" });
        await ExecAsync(Page, "CREATE TABLE product (id INTEGER PRIMARY KEY, name TEXT)");
    }
}
