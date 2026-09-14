using System.Text.RegularExpressions;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace Bit.Bswup.Tests.E2E.Infrastructure;

/// <summary>
/// Base of every suite: gives each test an origin of its own on the harness host of its render mode, a
/// browser context of its own, and the helpers to read what Bswup did - the messages it raised, the caches
/// it wrote, the requests that reached the server. Fails the test on anything a page logged as an error.
/// </summary>
/// <remarks>
/// Bswup is built to degrade instead of throwing: a worker that fails to install leaves the app running from
/// the network, a failed update leaves the old version running. So a broken integration shows up as an
/// effect that did not happen - nothing cached, no update, a page that is not controlled - and the suites
/// assert those effects. The console check catches the rest: the page script logs every failure it recovers
/// from as an error.
/// </remarks>
public abstract partial class BswupTest
{
    private readonly Lock _consoleLock = new();
    private readonly List<string> _consoleErrors = [];
    private readonly List<string> _allowedConsoleErrors = [];

    private IBrowserContext? _context;
    private IPage? _page;
    private HarnessSession? _session;

    public TestContext TestContext { get; set; } = default!;

    /// <summary>The BswupHarness:Mode of the host the test runs against.</summary>
    protected abstract string Mode { get; }

    /// <summary>"/" for the Blazor Web App, "/standalone/" for the standalone WebAssembly app. Also the worker's scope.</summary>
    protected virtual string AppPath => "/";

    protected HarnessSession Session => _session ?? throw new InvalidOperationException("The session is opened in TestInitialize.");

    protected IBrowserContext Context => _context ?? throw new InvalidOperationException("The context is opened in TestInitialize.");

    protected IPage Page => _page ?? throw new InvalidOperationException("The page is opened in TestInitialize.");

    protected static string Framework => E2EEnvironment.Framework;

    [TestInitialize]
    public async Task OpenSessionAsync()
    {
        var host = await HarnessHosts.GetAsync(Mode);
        _session = new HarnessSession(host);

        var browser = await PlaywrightSession.BrowserAsync();
        _context = await browser.NewContextAsync(new() { ViewportSize = new() { Width = 1280, Height = 720 } });
        _page = await NewPageAsync();
    }

    [TestCleanup]
    public async Task CloseSessionAsync()
    {
        if (_context is not null)
        {
            await _context.CloseAsync();
        }

        string[] unexpected;
        lock (_consoleLock)
        {
            unexpected = [.. _consoleErrors.Where(error => _allowedConsoleErrors.Any(allowed => error.Contains(allowed, StringComparison.OrdinalIgnoreCase)) is false)];
        }

        if (unexpected.Length > 0)
            Assert.Fail($"The page logged {unexpected.Length} error(s):{Environment.NewLine}{string.Join(Environment.NewLine, unexpected)}");
    }

    /// <summary>A further tab of the test's browser context: same origin, same service worker, same caches.</summary>
    protected async Task<IPage> NewPageAsync()
    {
        var page = await Context.NewPageAsync();
        page.Console += (_, message) =>
        {
            if (message.Type is not "error") return;
            lock (_consoleLock) _consoleErrors.Add($"console: {message.Text} ({message.Location})");
        };
        page.PageError += (_, error) =>
        {
            lock (_consoleLock) _consoleErrors.Add($"uncaught: {error}");
        };
        return page;
    }

    /// <summary>Lets a test that provokes an error on purpose keep the console check for everything else.</summary>
    protected void AllowConsoleError(string fragment)
    {
        lock (_consoleLock) _allowedConsoleErrors.Add(fragment);
    }

    /// <summary>Allows the errors a browser logs for requests that fail at the network level (a dropped connection).</summary>
    protected void AllowNetworkErrors()
    {
        AllowConsoleError("net::ERR_");
        AllowConsoleError("Failed to fetch");
        AllowConsoleError("Failed to load resource");
        AllowConsoleError("An unknown error occurred when fetching the script");
    }

    /// <summary>The URL of <paramref name="path"/> relative to the app (no leading slash), on the test's origin.</summary>
    protected string AppUrl(string path = "") => Session.Origin + AppPath + path.TrimStart('/');

    protected Task<IResponse?> GotoAsync(string path = "", IPage? page = null) =>
        (page ?? Page).GotoAsync(AppUrl(path), new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 120_000 });

    /// <summary>Waits for the harness root to have rendered interactively, i.e. for Blazor to have started.</summary>
    protected Task WaitForInteractiveAsync(IPage? page = null) =>
        Expect((page ?? Page).Locator("#status")).ToHaveAttributeAsync("data-interactive", "true", new() { Timeout = 120_000 });

    /// <summary>Waits for a service worker to control the page.</summary>
    protected Task WaitForControlledAsync(IPage? page = null) =>
        Expect((page ?? Page).Locator("html")).ToHaveAttributeAsync("data-harness-controlled", "true", new() { Timeout = 120_000 });

    /// <summary>Waits for the tab to have loaded exactly <paramref name="loads"/> documents (a reload counts).</summary>
    protected Task ExpectLoadsAsync(int loads, IPage? page = null) =>
        Expect((page ?? Page).Locator("html")).ToHaveAttributeAsync("data-harness-loads", loads.ToString(), new() { Timeout = 120_000 });

    /// <summary>
    /// Asserts the tab is still at <paramref name="loads"/> documents a few seconds later. Reloads race each other
    /// (a controllerchange, a worker broadcast), and a locator assertion alone passes at the right count on its way
    /// past it.
    /// </summary>
    protected async Task AssertNoFurtherReloadAsync(int loads, IPage? page = null)
    {
        page ??= Page;
        await Task.Delay(3000);
        await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
        Assert.AreEqual(loads.ToString(), await page.Locator("html").GetAttributeAsync("data-harness-loads"), "The page reloaded again.");
    }

    /// <summary>Opens the app and waits for the first install to complete: the page controlled and the app started, in the same document.</summary>
    protected async Task InstallAsync(string path = "", IPage? page = null)
    {
        await GotoAsync(path, page);
        await WaitForControlledAsync(page);
        await WaitForInteractiveAsync(page);
    }

    protected Task<bool> IsControlledAsync(IPage? page = null) =>
        (page ?? Page).EvaluateAsync<bool>("() => !!navigator.serviceWorker.controller");

    /// <summary>The Bswup messages the current document received, oldest first.</summary>
    protected Task<HarnessEvent[]> EventsAsync(IPage? page = null) =>
        (page ?? Page).EvaluateAsync<HarnessEvent[]>("() => window.harness ? window.harness.events : []");

    /// <summary>Waits until the current document received a <paramref name="type"/> message (matching <paramref name="match"/>).</summary>
    protected async Task<HarnessEvent> WaitForEventAsync(string type, Func<HarnessEvent, bool>? match = null, IPage? page = null, int timeoutMs = 120_000)
    {
        var events = await EventuallyAsync(() => EventsAsync(page), all => all.Any(e => e.Type == type && (match?.Invoke(e) ?? true)),
            $"a {type} message", timeoutMs);
        return events.First(e => e.Type == type && (match?.Invoke(e) ?? true));
    }

    /// <summary>The names of the Bswup cache buckets on the test's origin.</summary>
    protected async Task<string[]> BswupCacheNamesAsync(IPage? page = null) =>
        [.. (await (page ?? Page).EvaluateAsync<string[]>("() => caches.keys()")).Where(name => name.StartsWith("bit-bswup", StringComparison.Ordinal))];

    protected Task<string[]> CachedUrlsAsync(string cacheName, IPage? page = null) =>
        (page ?? Page).EvaluateAsync<string[]>("async name => (await caches.has(name)) ? (await (await caches.open(name)).keys()).map(r => r.url) : []", cacheName);

    /// <summary>The bucket this app's worker names for <paramref name="version"/>: <c>bit-bswup:&lt;scope-path&gt; - &lt;version&gt;</c>.</summary>
    protected string BucketName(string version) => $"bit-bswup:{AppPath} - {version}";

    /// <summary>
    /// The cache keys of the manifest assets the worker precaches with its default include/exclude lists (and
    /// the <paramref name="excluded"/> the test configured): the asset URL resolved against the worker, followed
    /// by <c>.&lt;hash&gt;</c> when the asset has one.
    /// </summary>
    protected string[] PrecacheKeys(HarnessSession.AssetsManifest manifest, params string[] excluded) =>
        [.. PrecachedAssets(manifest, excluded).Select(asset => AppUrl(asset.Url) + (string.IsNullOrEmpty(asset.Hash) ? string.Empty : "." + asset.Hash))];

    /// <summary>The manifest assets the worker precaches with its default include/exclude lists (and the <paramref name="excluded"/> the test configured).</summary>
    protected static HarnessSession.AssetsManifest.Asset[] PrecachedAssets(HarnessSession.AssetsManifest manifest, params string[] excluded) =>
        [.. manifest.Assets
            .Where(asset => DefaultIncludes.Any(pattern => pattern.IsMatch(asset.Url)))
            .Where(asset => DefaultExcludes.Any(pattern => pattern.IsMatch(asset.Url)) is false)
            .Where(asset => excluded.Any(pattern => Regex.IsMatch(asset.Url, pattern)) is false)];

    /// <summary>Runs fetch() inside the page (so through its service worker) and reports the outcome instead of throwing.</summary>
    protected Task<FetchOutcome> FetchAsync(string url, string? method = null, Dictionary<string, string>? headers = null, IPage? page = null) =>
        (page ?? Page).EvaluateAsync<FetchOutcome>("""
            async ([url, method, headers]) => {
                try {
                    const response = await fetch(url, { method: method || 'GET', headers: headers || {} });
                    const text = await response.text();
                    return { failed: false, status: response.status, text, contentRange: response.headers.get('content-range') || '' };
                } catch (error) {
                    return { failed: true, status: 0, text: String(error), contentRange: '' };
                }
            }
            """, new object?[] { url, method, headers });

    /// <summary>
    /// Polls <paramref name="probe"/> until <paramref name="done"/> holds. A probe that fails because the page is
    /// navigating (a reload Bswup triggered) is simply retried.
    /// </summary>
    protected static async Task<T> EventuallyAsync<T>(Func<Task<T>> probe, Func<T, bool> done, string what, int timeoutMs = 60_000)
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
        T last = default!;
        Exception? lastError = null;

        while (true)
        {
            try
            {
                last = await probe();
                lastError = null;
                if (done(last)) return last;
            }
            catch (PlaywrightException ex)
            {
                lastError = ex;
            }

            if (DateTime.UtcNow > deadline)
                Assert.Fail($"Timed out after {timeoutMs}ms waiting for {what}. Last value: {Describe(last)}{(lastError is null ? "" : $" (last error: {lastError.Message})")}");

            await Task.Delay(250);
        }
    }

    private static string Describe(object? value) => value switch
    {
        null => "null",
        string text => text,
        System.Collections.IEnumerable items => "[" + string.Join(", ", items.Cast<object?>().Select(item => item?.ToString())) + "]",
        _ => value.ToString() ?? string.Empty,
    };

    // bit-bswup.sw.ts DEFAULT_ASSETS_INCLUDE / DEFAULT_ASSETS_EXCLUDE (plus the running worker script).
    private static readonly Regex[] DefaultIncludes =
    [
        new(@"\.dll$"), new(@"\.wasm(\.br|\.gz)?$"), new(@"\.pdb(\.br|\.gz)?$"), new(@"\.html(\.br|\.gz)?$"), new(@"\.js$"), new(@"\.json$"),
        new(@"\.css$"), new(@"\.woff$"), new(@"\.png$"), new(@"\.jpe?g$"), new(@"\.gif$"), new(@"\.ico$"), new(@"\.blat$"), new(@"\.dat$"),
        new(@"\.svg$"), new(@"\.woff2$"), new(@"\.ttf$"), new(@"\.webp$"),
    ];

    private static readonly Regex[] DefaultExcludes =
    [
        new(@"^_content/Bit\.Bswup/bit-bswup\.sw(-cleanup)?(\.min)?\.js$"), new(@"^service-worker\.js$"),
    ];
}

/// <summary>One Bswup message as harness.js recorded it.</summary>
public sealed class HarnessEvent
{
    public string Type { get; set; } = string.Empty;

    public bool? FirstInstall { get; set; }

    public double? Percent { get; set; }

    public string? Reason { get; set; }

    public bool? Fatal { get; set; }

    public string? Version { get; set; }

    public string? Url { get; set; }

    public string? Message { get; set; }

    public int? Status { get; set; }

    public int? Index { get; set; }

    public string? Asset { get; set; }

    public override string ToString() =>
        $"{Type}{(FirstInstall is null ? "" : $" firstInstall={FirstInstall}")}{(Percent is null ? "" : $" {Percent:0}%")}{(Reason is null ? "" : $" reason={Reason} fatal={Fatal}")}{(Version is null ? "" : $" version={Version}")}{(Url is null ? "" : $" url={Url}")}";
}

public sealed class FetchOutcome
{
    /// <summary>fetch() rejected: a network error, which is what a request the worker cannot serve gets offline.</summary>
    public bool Failed { get; set; }

    public int Status { get; set; }

    public string Text { get; set; } = string.Empty;

    public string ContentRange { get; set; } = string.Empty;

    public override string ToString() => Failed ? $"failed: {Text}" : $"{Status}: {Text[..Math.Min(Text.Length, 80)]}";
}
