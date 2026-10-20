using System.Text.Json;
using Bit.Bsqlite.Tests.E2E.Infrastructure;
using Microsoft.Playwright;

namespace Bit.Bsqlite.Tests.E2E;

/// <summary>
/// Soak coverage: many tabs writing at once, and a worker killed mid-transaction a thousand times over.
/// </summary>
/// <remarks>
/// <para>
/// Category <c>Soak</c>, and reported inconclusive by an ordinary run unless <c>BSQLITE_SOAK=1</c>, because both
/// tests are minutes of wall clock rather than seconds.
/// </para>
/// <para>
/// The loops live inside <c>page.evaluate</c> rather than being driven a statement at a time from here: a
/// thousand round trips through the Playwright protocol would measure the protocol, and tabs told to write "at
/// the same time" from .NET would in fact take turns.
/// </para>
/// <para>
/// Sizes are environment-tunable so a smaller run can be used while investigating a failure (see
/// <see cref="E2EEnvironment"/>): <c>BSQLITE_SOAK_TABS</c> - tabs in the concurrent-write test (default 8),
/// <c>BSQLITE_SOAK_ROWS</c> - rows each of those tabs commits (default 50), <c>BSQLITE_SOAK_KILLS</c> -
/// mid-transaction kills per storage (default 1000). The assembly runs one test at a time, which the soak tests
/// need: one already saturates a machine's storage layer, and running two at once would turn a genuine
/// contention failure into a scheduling artefact.
/// </para>
/// </remarks>
[TestClass]
public sealed class SoakTests : BsqliteTest
{
    private const string NotRequested = "the soak tests take minutes rather than seconds; set BSQLITE_SOAK=1 to run them";

    // Killed workers are reported in batches so a long run shows progress instead of going silent.
    private const int KillBatch = 25;

    private static readonly SoakStorage[] SoakStorages =
    [
        new("OPFS", "opfs", Storages.Opfs),
        new("IndexedDB", "idb", Storages.IndexedDb),
    ];

    // Both ends of the range in the reference targets, deduplicated in case they were tuned to meet.
    private static readonly int[] TabCounts = [.. new[] { 4, E2EEnvironment.SoakTabs }.Distinct()];

    public static IEnumerable<TestDataRow<string>> KillRuns =>
        SoakStorages.Select(storage => new TestDataRow<string>(storage.Label)
        {
            DisplayName = $"@soak {storage.Label} survives {E2EEnvironment.SoakKills} kills mid-transaction",
        });

    public static IEnumerable<TestDataRow<(string, int)>> TabRuns =>
        from storage in SoakStorages
        from tabs in TabCounts
        select new TestDataRow<(string, int)>((storage.Label, tabs))
        {
            DisplayName = $"@soak {storage.Label} loses no write across {tabs} tabs",
        };

    [TestMethod]
    [TestCategory("Soak")]
    [DynamicData(nameof(KillRuns))]
    public async Task Survives_kills_mid_transaction(string label)
    {
        SkipWhen(E2EEnvironment.Soak is false, NotRequested);

        var storage = SoakStorages.Single(s => s.Label == label);
        var kills = E2EEnvironment.SoakKills;

        var databaseName = UniqueName($"{storage.Prefix}-soak-kill");
        await PrepareAsync(Page, storage.Options(databaseName));

        var committed = 0;
        var openRetries = 0;

        for (var done = 0; done < kills; done += KillBatch)
        {
            var batch = Math.Min(KillBatch, kills - done);
            var outcome = await KillBatchAsync(Page, storage.Options(databaseName), iterations: batch, startIndex: done);

            committed += outcome.GetProperty("committed").GetInt32();
            openRetries += outcome.GetProperty("openRetries").GetInt32();
            TestContext.WriteLine($"progress: {done + batch}/{kills} kills, {openRetries} open retries");
        }

        Assert.AreEqual(kills, committed);

        await OpenHostAsync(Page, storage.Options(databaseName));

        var kept = await QueryAsync(Page, "SELECT count(*) FROM ledger WHERE label LIKE 'kept-%'");
        var ghosts = await QueryAsync(Page, "SELECT count(*) FROM ledger WHERE label LIKE 'ghost-%'");
        var distinct = await QueryAsync(Page, "SELECT count(DISTINCT label) FROM ledger");
        var integrity = await QueryAsync(Page, "PRAGMA integrity_check");

        // Every committed row is there exactly once, no row from an unfinished transaction survived, and the
        // file the kills left behind is still a valid SQLite database.
        AssertJson.AreEqual($"[[{kills}]]", kept.Rows);
        AssertJson.AreEqual("[[0]]", ghosts.Rows);
        AssertJson.AreEqual($"[[{kills}]]", distinct.Rows);
        AssertJson.AreEqual("""[["ok"]]""", integrity.Rows);
    }

    [TestMethod]
    [TestCategory("Soak")]
    [DynamicData(nameof(TabRuns))]
    public async Task Loses_no_write_across_tabs(string label, int tabs)
    {
        SkipWhen(E2EEnvironment.Soak is false, NotRequested);

        var storage = SoakStorages.Single(s => s.Label == label);
        var rowsPerTab = E2EEnvironment.SoakRows;

        var databaseName = UniqueName($"{storage.Prefix}-soak-tabs-{tabs}");
        var context = await NewContextAsync();

        try
        {
            var pages = new List<IPage>();
            for (var index = 0; index < tabs; index++)
            {
                pages.Add(await context.NewPageAsync());
            }

            await PrepareAsync(pages[0], storage.Options(databaseName));

            var outcomes = await Task.WhenAll(pages.Select((tab, index) => WriteBatchAsync(
                tab,
                storage.Options(databaseName),
                tab: index,
                rows: rowsPerTab)));

            CollectionAssert.AreEqual(
                pages.Select(_ => rowsPerTab).ToArray(),
                outcomes.Select(outcome => outcome.GetProperty("written").GetInt32()).ToArray());
            TestContext.WriteLine($"progress: {outcomes.Sum(o => o.GetProperty("retries").GetInt32())} write retries");

            var reader = pages[0];
            await OpenHostAsync(reader, storage.Options(databaseName));

            var total = await QueryAsync(reader, "SELECT count(*) FROM ledger");
            var perTab = await QueryAsync(
                reader,
                "SELECT tab, count(*) FROM ledger GROUP BY tab ORDER BY tab");
            var integrity = await QueryAsync(reader, "PRAGMA integrity_check");

            // A lost write shows up as a short count; a write applied twice shows up as a long one. Both are
            // visible per tab, so a failure names the tab that lost the race.
            AssertJson.AreEqual($"[[{tabs * rowsPerTab}]]", total.Rows);
            AssertJson.AreEqual(
                JsonSerializer.Serialize(Enumerable.Range(0, tabs).Select(index => new[] { index, rowsPerTab })),
                perTab.Rows);
            AssertJson.AreEqual("""[["ok"]]""", integrity.Rows);
        }
        finally
        {
            await context.CloseAsync();
        }
    }

    /// <summary>Creates the table the soak tests write to, then closes cleanly so the loops start from a good file.</summary>
    private static async Task PrepareAsync(IPage page, HostOptions options)
    {
        await OpenHostAsync(page, options);
        await ExecAsync(
            page,
            "CREATE TABLE ledger (id INTEGER PRIMARY KEY, tab INTEGER NOT NULL, label TEXT NOT NULL)");
        await page.EvaluateAsync(
            """
            async () => {
              await globalThis.host.close();
              globalThis.host.dispose();
            }
            """);
    }

    /// <summary>
    /// Commits one row, opens a transaction, writes a row into it, then kills the worker without committing or
    /// closing - the closest a test can get to the tab being killed by the browser or the user.
    /// </summary>
    private static Task<JsonElement> KillBatchAsync(IPage page, HostOptions options, int iterations, int startIndex)
    {
        return page.EvaluateAsync<JsonElement>(
            """
            async ({ databaseName, options, iterations, startIndex }) => {
              const sleep = ms => new Promise(resolve => setTimeout(resolve, ms));
              const outcome = { committed: 0, openRetries: 0 };

              // A killed worker releases its lock on the file asynchronously, so the next open can arrive while
              // the previous one is still letting go. That is a retry, not a failure.
              const openWithRetry = async () => {
                let lastError;
                for (let attempt = 0; attempt < 60; attempt++) {
                  try {
                    return await globalThis.BitBsqlite.host.open({ databaseName, ...options });
                  } catch (error) {
                    lastError = error;
                    outcome.openRetries++;
                    await sleep(25 + attempt * 5);
                  }
                }

                throw lastError;
              };

              const run = (host, commandText) =>
                host.execute([{ commandText, resultKind: 'nonQuery' }]);

              for (let index = 0; index < iterations; index++) {
                const iteration = startIndex + index;
                const host = await openWithRetry();

                try {
                  await run(host, `INSERT INTO ledger (tab, label) VALUES (0, 'kept-${iteration}')`);
                  outcome.committed++;

                  await run(host, 'BEGIN');
                  await run(host, `INSERT INTO ledger (tab, label) VALUES (0, 'ghost-${iteration}')`);
                } finally {
                  host.dispose();
                }
              }

              return outcome;
            }
            """, new { databaseName = options.DatabaseName, options = options.ToArgument(), iterations, startIndex });
    }

    /// <summary>
    /// Commits <paramref name="rows"/> rows from one tab, retrying the statements another tab is currently
    /// holding the database against. Contention is expected here; losing a row to it is not.
    /// </summary>
    private static async Task<JsonElement> WriteBatchAsync(IPage page, HostOptions options, int tab, int rows)
    {
        await GotoHostPageAsync(page);

        return await page.EvaluateAsync<JsonElement>(
            """
            async ({ databaseName, options, tab, rows }) => {
              const sleep = ms => new Promise(resolve => setTimeout(resolve, ms));
              const outcome = { written: 0, retries: 0 };

              const withRetry = async action => {
                let lastError;
                for (let attempt = 0; attempt < 60; attempt++) {
                  try {
                    return await action();
                  } catch (error) {
                    lastError = error;
                    outcome.retries++;
                    await sleep(25 + attempt * 5);
                  }
                }

                throw lastError;
              };

              const host = await withRetry(() => globalThis.BitBsqlite.host.open({ databaseName, ...options }));

              try {
                for (let row = 0; row < rows; row++) {
                  await withRetry(() => host.execute([{
                    commandText: `INSERT INTO ledger (tab, label) VALUES (${tab}, 'tab-${tab}-row-${row}')`,
                    resultKind: 'nonQuery',
                  }]));
                  outcome.written++;
                }
              } finally {
                await host.close();
                host.dispose();
              }

              return outcome;
            }
            """, new { databaseName = options.DatabaseName, options = options.ToArgument(), tab, rows });
    }

    /// <summary>A storage the soak runs against: its label, the prefix of its database names, and how it opens.</summary>
    private sealed record SoakStorage(string Label, string Prefix, Func<string, HostOptions> Options);
}
