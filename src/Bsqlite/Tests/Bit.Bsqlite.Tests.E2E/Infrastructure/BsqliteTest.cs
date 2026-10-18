using System.Text.Json;
using Microsoft.Playwright;

namespace Bit.Bsqlite.Tests.E2E.Infrastructure;

/// <summary>
/// Base of every library suite: a browser context and page of its own per test against the shared asset
/// host, and the helpers that drive the library from test code.
/// </summary>
/// <remarks>
/// <para>
/// Tests write plain values and read plain results. Batches go through the binary wire path - the one the
/// .NET transport uses - encoded and decoded in the page with the library's own codec, so every test here
/// exercises the real format rather than a parallel one. InteropTests covers the tagged JSON path
/// separately.
/// </para>
/// <para>
/// Values that JSON cannot carry are tagged again on the way out of the page, so that "came back as a 64-bit
/// integer" and "came back as a blob" stay observable instead of being silently coerced: a BigInt arrives as
/// <c>{ "bigint": "…" }</c>, a blob as <c>{ "blob": [ … ] }</c>, and a non-finite REAL - which Playwright cannot
/// hand to .NET as a <see cref="JsonElement"/> at all - as <c>{ "real": "Infinity" }</c>.
/// </para>
/// </remarks>
public abstract class BsqliteTest
{
    private readonly List<IBrowserContext> _contexts = [];
    private PlaywrightSession.Session? _session;
    private IPage? _page;

    public TestContext TestContext { get; set; } = default!;

    /// <summary>The origin the assets and test pages are served from, without a trailing slash.</summary>
    protected string BaseUrl => Session.Assets!.BaseUrl;

    protected IBrowser Browser => Session.Browser!;

    /// <summary><c>chromium</c>, <c>firefox</c> or <c>webkit</c>: see <see cref="E2EEnvironment.Browser"/>.</summary>
    protected static string BrowserName => E2EEnvironment.Browser;

    /// <summary>A page in a context of its own, opened for every test (Playwright Test's <c>page</c> fixture).</summary>
    protected IPage Page => _page ?? throw new InvalidOperationException("The page is opened in TestInitialize.");

    private PlaywrightSession.Session Session => _session ?? throw new InvalidOperationException("The session is opened in TestInitialize.");

    [TestInitialize]
    public async Task OpenSessionAsync()
    {
        _session = await PlaywrightSession.GetAsync();
        if (_session.Unavailable is { } reason)
        {
            // A build server must not report a suite that never ran as anything but a failure.
            if (E2EEnvironment.IsContinuousIntegration) Assert.Fail("The browser tests could not run: " + reason);
            Assert.Inconclusive("The browser tests could not run: " + reason);
        }

        _page = await NewPageAsync();
    }

    [TestCleanup]
    public async Task CloseContextsAsync()
    {
        foreach (var context in _contexts)
        {
            try { await context.CloseAsync(); }
            catch (PlaywrightException) { /* the browser went away first */ }
        }
        _contexts.Clear();
    }

    /// <summary>
    /// A fresh browser context, closed when the test ends. Relative URLs resolve against the asset host, so a
    /// page navigates with <c>GotoAsync("/index.html")</c>. Pages of one context share their origin's storage,
    /// which is what the multi-tab tests need; separate contexts do not.
    /// </summary>
    protected Task<IBrowserContext> NewContextAsync() => NewContextAsync(new BrowserNewContextOptions { BaseURL = BaseUrl });

    /// <summary>A fresh browser context with <paramref name="options"/>, closed when the test ends.</summary>
    protected async Task<IBrowserContext> NewContextAsync(BrowserNewContextOptions options)
    {
        var context = await Browser.NewContextAsync(options);
        _contexts.Add(context);
        return context;
    }

    /// <summary>A page in a fresh context of its own.</summary>
    protected async Task<IPage> NewPageAsync() => await (await NewContextAsync()).NewPageAsync();

    /// <summary>Reports the test as inconclusive - MSTest's skip - with <paramref name="reason"/> when <paramref name="condition"/> holds.</summary>
    protected static void SkipWhen(bool condition, string reason)
    {
        if (condition) Assert.Inconclusive(reason);
    }

    /// <summary>A database name no other test - and no earlier run on the same browser profile - has used.</summary>
    protected static string UniqueName(string prefix) => $"{prefix}-{Guid.NewGuid():N}.db";

    // ---------------------------------------------------------------------------------------------
    // The test pages and the host
    // ---------------------------------------------------------------------------------------------

    /// <summary>Loads a test page and waits until it has imported the library.</summary>
    protected static async Task GotoHostPageAsync(IPage page, string path = "/index.html")
    {
        await page.GotoAsync(path);
        await page.WaitForFunctionAsync("() => globalThis.BitBsqliteReady === true");
    }

    /// <summary>
    /// Loads the host page and opens a database, keeping the host as <c>globalThis.host</c> for the helpers
    /// below. Returns what the worker reports about itself.
    /// </summary>
    protected static async Task<HostVersion> OpenHostAsync(IPage page, HostOptions? options = null)
    {
        await GotoHostPageAsync(page);

        var version = await page.EvaluateAsync<JsonElement>(
            """
            async options => {
              globalThis.host = await globalThis.BitBsqlite.host.open(options);
              return await globalThis.host.version();
            }
            """, (options ?? new HostOptions()).ToArgument());

        return new HostVersion(version.GetProperty("version").GetString()!, version.GetProperty("build").GetString()!);
    }

    /// <summary>Runs a batch through the binary path, encoding parameters and decoding rows with the library's wire helpers.</summary>
    protected static async Task<WireResult[]> ExecuteAsync(IPage page, params WireRequest[] batch)
    {
        var results = await page.EvaluateAsync<JsonElement>(
            """
            async batch => {
              const { encodeValue, decodeValue, encodeBatchBinary, decodeReplyBinary } = globalThis.BitBsqlite.host;

              const naturalValue = p => {
                if (p.bytes) return new Uint8Array(p.bytes);
                if (p.big) return BigInt(p.big);
                return p.value;
              };

              const wire = batch.map(request => ({
                commandText: request.commandText,
                resultKind: request.resultKind ?? 'nonQuery',
                maxRows: request.maxRows ?? undefined,
                parameters: (request.parameters ?? []).map(p => ({
                  name: p.name,
                  ...encodeValue(naturalValue(p)),
                })),
              }));

              const results = decodeReplyBinary(await globalThis.host.executeBinary(encodeBatchBinary(wire)));

              // Re-tagged for the trip to the test runner: JSON has no BigInt, no bytes and no Infinity.
              const forTest = value => {
                if (typeof value === 'bigint') return { bigint: String(value) };
                if (value instanceof Uint8Array) return { blob: Array.from(value) };
                if (typeof value === 'number' && !Number.isFinite(value)) return { real: String(value) };
                return value;
              };

              return results.map(result => ({
                columnNames: result.columnNames,
                columnTypes: result.columnTypes,
                recordsAffected: result.recordsAffected,
                lastInsertRowId: Number(result.lastInsertRowId),
                rows: result.rows.map(row => row.v.map((value, i) => forTest(decodeValue(row.t[i], value)))),
              }));
            }
            """, batch.Select(r => r.ToArgument()).ToArray());

        return [.. results.EnumerateArray().Select(WireResult.From)];
    }

    /// <summary>Convenience for the common case of one statement whose rows are wanted.</summary>
    protected static async Task<WireResult> QueryAsync(IPage page, string commandText, params WireParameter[] parameters)
        => (await ExecuteAsync(page, new WireRequest(commandText, parameters, "reader")))[0];

    /// <summary>Convenience for one statement whose rows are not wanted.</summary>
    protected static async Task<WireResult> ExecAsync(IPage page, string commandText, params WireParameter[] parameters)
        => (await ExecuteAsync(page, new WireRequest(commandText, parameters, "nonQuery")))[0];

    /// <summary>
    /// Runs a batch expected to fail, returning the error as data - or null when it did not fail. A captured
    /// rejection rather than an exception from Playwright, so the SQLite result code survives the boundary: a
    /// test asserting only on the message would pass for the wrong error.
    /// </summary>
    protected static async Task<WireFailure?> ExecuteExpectingFailureAsync(IPage page, params WireRequest[] batch)
    {
        var failure = await page.EvaluateAsync<JsonElement>(
            """
            async batch => {
              const { encodeValue, encodeBatchBinary, decodeReplyBinary } = globalThis.BitBsqlite.host;

              const wire = batch.map(request => ({
                commandText: request.commandText,
                resultKind: request.resultKind ?? 'nonQuery',
                parameters: (request.parameters ?? []).map(p => ({ name: p.name, ...encodeValue(p.value) })),
              }));

              try {
                decodeReplyBinary(await globalThis.host.executeBinary(encodeBatchBinary(wire)));
                return null;
              } catch (error) {
                return { message: error.message, sqliteCode: error.sqliteCode ?? null, name: error.name };
              }
            }
            """, batch.Select(r => r.ToArgument()).ToArray());

        return WireFailure.From(failure);
    }

    /// <summary>
    /// Starts recording the host's change notifications. Each later call to <see cref="NotificationsAsync"/>
    /// returns the table lists received since the previous one, each sorted.
    /// </summary>
    /// <remarks>
    /// The worker posts a notification before the reply to the batch that caused it, and the host handles
    /// messages in order, so a notification is recorded by the time that batch's promise settles.
    /// </remarks>
    protected static Task RecordNotificationsAsync(IPage page) => page.EvaluateAsync(
        """
        () => {
          globalThis.notified = [];
          globalThis.host.onNotify(payload => globalThis.notified.push([...payload.tables].sort()));
        }
        """);

    /// <summary>The notifications recorded since the last call; see <see cref="RecordNotificationsAsync"/>.</summary>
    protected static async Task<string[][]> NotificationsAsync(IPage page)
    {
        var notified = await page.EvaluateAsync<JsonElement>("() => globalThis.notified.splice(0)");
        return [.. notified.EnumerateArray().Select(tables => tables.EnumerateArray().Select(t => t.GetString()!).ToArray())];
    }

    /// <summary>
    /// Skips the current test where the browser has no OPFS.
    /// </summary>
    /// <remarks>
    /// Playwright's own WebKit builds expose no <c>navigator.storage</c> at all - not in the window, not in a
    /// worker, secure context notwithstanding - so anything that opens an OPFS database there fails before
    /// SQLite is involved. Safari proper has OPFS; the tests are skipped rather than failed so the suite still
    /// reports on what that WebKit can exercise (IndexedDB, Cache Storage, the engine itself).
    /// </remarks>
    protected static async Task SkipUnlessOpfsAsync(IPage page, string path = "/index.html")
    {
        await page.GotoAsync(path);
        var available = await page.EvaluateAsync<bool>("() => typeof navigator.storage?.getDirectory === 'function'");
        SkipWhen(available is false, "this browser has no navigator.storage.getDirectory, so OPFS cannot be tested");
    }
}
