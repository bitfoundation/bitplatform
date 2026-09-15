using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace Bit.Brouter.Tests.E2E.Infrastructure;

/// <summary>
/// Base of every suite: opens the page a test drives, fails the test on anything the page logged as an
/// error, and holds the helpers the suites share.
/// </summary>
/// <remarks>
/// The console check is load-bearing, not hygiene. Brouter swallows every JS interop failure on purpose
/// (a missing module, a host without the API, a disconnected circuit all degrade to "the effect did not
/// happen"), so a broken integration rarely throws. What it does leave behind is an unhandled-exception
/// report from the Blazor runtime, or a failed module request, in the console.
/// </remarks>
public abstract class HarnessTest
{
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
    /// Waits for the harness to have rendered interactively. Prerendered markup is on screen long before
    /// its event handlers are, and a click in between is silently lost.
    /// </summary>
    protected Task WaitForInteractiveAsync() =>
        Expect(Page.Locator("#status")).ToHaveAttributeAsync("data-interactive", "true", new() { Timeout = 90_000 });

    /// <summary>
    /// Waits for Brouter to have applied the initial navigation's DOM effects, signalled by focus landing
    /// on the page heading (the harness sets FocusOnNavigateSelector to "h1"). The initial load scrolls to
    /// the top too, and without a prerendered page that can land well after the first interactive render -
    /// undoing any scroll a test made in between.
    /// </summary>
    protected async Task WaitForInitialNavigationEffectsAsync(string headingSelector)
    {
        await WaitForInteractiveAsync();
        await Expect(Page.Locator(headingSelector)).ToBeFocusedAsync();
    }

    protected Task ExpectUrlAsync(string pathAndQuery) => Expect(Page).ToHaveURLAsync(BaseUrl + pathAndQuery);

    protected async Task ClickAndExpectAsync(string selector, string arrivedSelector)
    {
        await Page.Locator(selector).ClickAsync();
        await Expect(Page.Locator(arrivedSelector)).ToBeVisibleAsync();
    }

    protected Task<double> ScrollYAsync() => Page.EvaluateAsync<double>("() => window.scrollY");

    protected async Task WaitForScrollYAsync(double expected, double tolerance = 5)
    {
        try
        {
            await Page.WaitForFunctionAsync("([y, t]) => Math.abs(window.scrollY - y) <= t", new object[] { expected, tolerance }, new() { Timeout = 10_000 });
        }
        // Playwright reports a wait that ran out as System.TimeoutException, not as a PlaywrightException.
        catch (Exception ex) when (ex is TimeoutException or PlaywrightException)
        {
            Assert.Fail($"window.scrollY is {await ScrollYAsync()}, expected {expected}.");
        }
    }

    /// <summary>The URLs of every Bit.Brouter JS module request the page made, with their response statuses.</summary>
    protected async Task<ModuleRequest[]> BrouterModuleRequestsAsync() =>
        await Page.EvaluateAsync<ModuleRequest[]>("""
            () => performance.getEntriesByType('resource')
                .filter(e => /bit-brouter(\.[a-z0-9]+)?\.js/i.test(e.name))
                .map(e => ({ url: e.name, status: e.responseStatus ?? 0 }))
            """);

    // A settable class rather than a positional record: Playwright materializes evaluate results
    // through a parameterless constructor.
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
