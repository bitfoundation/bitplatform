using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace Bit.Blex.Tests.E2E.Infrastructure;

/// <summary>
/// Base of every suite: opens the page a test drives, fails the test on anything the page logged as an
/// error, and holds the helpers the suites share.
/// </summary>
/// <remarks>
/// The console check is load-bearing, not hygiene. Blex isolates JS interop failures on purpose - a
/// storage write that fails is reported to <c>OnError</c>, a DevTools bridge that cannot load is skipped -
/// so a broken integration rarely throws. What it does leave behind is an unhandled-exception report from
/// the Blazor runtime, or a failed module request, in the console.
/// </remarks>
public abstract class HarnessTest
{
    /// <summary>The localStorage key the harness persists the counter store under.</summary>
    protected const string CounterStorageKey = "blex-harness:counter";

    private readonly object _consoleLock = new();
    private readonly List<string> _consoleErrors = [];
    private readonly List<string> _allowedConsoleErrors = [];
    private IPage? _page;

    public TestContext TestContext { get; set; } = default!;

    protected IPage Page => _page ?? throw new InvalidOperationException("The page is opened in TestInitialize.");

    /// <summary>The origin the harness is served from, without a trailing slash.</summary>
    protected abstract string BaseUrl { get; }

    /// <summary>The target framework the harness runs on.</summary>
    protected abstract string Framework { get; }

    protected abstract Task<IPage> OpenPageAsync();

    protected abstract Task ClosePageAsync();

    [TestInitialize]
    public async Task OpenHarnessPageAsync()
    {
        _page = await OpenPageAsync();
        _page.Console += OnConsole;
        _page.PageError += OnPageError;
    }

    [TestCleanup]
    public async Task CloseHarnessPageAsync()
    {
        if (_page is not null)
        {
            _page.Console -= OnConsole;
            _page.PageError -= OnPageError;
        }

        await ClosePageAsync();

        string[] unexpected;
        lock (_consoleLock)
        {
            unexpected = [.. _consoleErrors.Where(error => _allowedConsoleErrors.Any(allowed => error.Contains(allowed, StringComparison.OrdinalIgnoreCase)) is false)];
        }

        if (unexpected.Length > 0)
            Assert.Fail($"The page logged {unexpected.Length} error(s):{Environment.NewLine}{string.Join(Environment.NewLine, unexpected)}");
    }

    /// <summary>Lets a test that provokes an error on purpose keep the console check for everything else.</summary>
    protected void AllowConsoleError(string fragment)
    {
        lock (_consoleLock) _allowedConsoleErrors.Add(fragment);
    }

    protected Task<IResponse?> GotoAsync(string path) =>
        Page.GotoAsync(BaseUrl + path, new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });

    /// <summary>
    /// Waits for the harness to have rendered interactively. Prerendered markup is on screen long before its
    /// event handlers are, and a click in between is silently lost.
    /// </summary>
    protected Task WaitForInteractiveAsync() => WaitForInteractiveAsync(Page);

    protected static Task WaitForInteractiveAsync(IPage page) =>
        Expect(page.Locator("#status")).ToHaveAttributeAsync("data-interactive", "true", new() { Timeout = 90_000 });

    protected Task ExpectCountAsync(int count) => ExpectCountAsync(Page, count);

    protected static Task ExpectCountAsync(IPage page, int count) =>
        Expect(page.Locator("#count")).ToHaveAttributeAsync("data-count", count.ToString(System.Globalization.CultureInfo.InvariantCulture));

    /// <summary>Clicks the counter's increment button <paramref name="times"/> times, waiting for each one to land.</summary>
    protected async Task IncrementAsync(int times, int startingAt = 0)
    {
        for (var i = 1; i <= times; i++)
        {
            await Page.Locator("#increment").ClickAsync();
            await ExpectCountAsync(startingAt + i);
        }
    }

    protected Task<string?> LocalStorageAsync(string key) => LocalStorageAsync(Page, key);

    protected static Task<string?> LocalStorageAsync(IPage page, string key) =>
        page.EvaluateAsync<string?>("key => localStorage.getItem(key)", key);

    /// <summary>The URLs of every Bit.Blex JS module request the page made, with their response statuses.</summary>
    protected async Task<ModuleRequest[]> BlexModuleRequestsAsync() =>
        await Page.EvaluateAsync<ModuleRequest[]>("""
            () => performance.getEntriesByType('resource')
                .filter(e => /bit-blex-[a-z]+(\.[a-z0-9]+)?\.js/i.test(e.name))
                .map(e => ({ url: e.name, status: e.responseStatus ?? 0 }))
            """);

    // A settable class rather than a positional record: Playwright materializes evaluate results through a
    // parameterless constructor.
    public sealed class ModuleRequest
    {
        public string Url { get; set; } = string.Empty;

        public int Status { get; set; }
    }

    private void OnConsole(object? sender, IConsoleMessage message)
    {
        if (message.Type is not "error") return;

        lock (_consoleLock) _consoleErrors.Add($"console: {message.Text} ({message.Location})");
    }

    private void OnPageError(object? sender, string error)
    {
        lock (_consoleLock) _consoleErrors.Add($"uncaught: {error}");
    }
}
