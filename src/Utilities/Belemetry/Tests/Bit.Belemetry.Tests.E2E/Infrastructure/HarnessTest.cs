using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace Bit.Belemetry.Tests.E2E.Infrastructure;

/// <summary>
/// Base of every suite: opens the page a test drives, fails the test on anything the page logged as an error, and holds
/// the helpers the suites share.
/// </summary>
/// <remarks>
/// The console check is load-bearing, not hygiene. The browser script never throws into the page by design (a failed
/// export, a missing API, a broken instrumentation all degrade to "nothing was recorded"), so a broken integration
/// rarely fails loudly. What it does leave behind is an unhandled-exception report from the Blazor runtime, or a failed
/// module request, in the console.
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

    /// <summary>The receiver the host under test exports to.</summary>
    protected abstract CollectorClient Collector { get; }

    /// <summary>The <c>BELEMETRY_E2E_HOSTS</c> name of the host, to skip unselected suites.</summary>
    protected abstract string HostName { get; }

    protected abstract Task<IPage> OpenPageAsync();

    protected abstract Task ClosePageAsync();

    [TestInitialize]
    public async Task OpenHarnessPageAsync()
    {
        if (E2EEnvironment.Runs(HostName) is false)
            Assert.Inconclusive($"BELEMETRY_E2E_HOSTS does not include {HostName}.");

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

    /// <summary>A value unique to this test, put on the page's records (via <c>?marker=</c>) to find them.</summary>
    protected static string NewMarker() => Guid.NewGuid().ToString("N");

    protected Task<IResponse?> GotoAsync(string path) =>
        Page.GotoAsync(BaseUrl + path, new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });

    /// <summary>
    /// Waits for the harness to have rendered interactively. Prerendered markup is on screen long before its event
    /// handlers are, and a click in between is silently lost.
    /// </summary>
    protected Task WaitForInteractiveAsync() =>
        Expect(Page.Locator("#status")).ToHaveAttributeAsync("data-interactive", "true", new() { Timeout = 90_000 });

    /// <summary>Waits for <c>&lt;BelemetryInitializer /&gt;</c> to have connected the script to .NET.</summary>
    protected Task WaitForBridgeAsync() =>
        Expect(Page.Locator("#status")).ToHaveAttributeAsync("data-bridge", "true", new() { Timeout = 90_000 });

    /// <summary>Waits for the browser script to have started (from the rendered config, or from the bridge).</summary>
    protected Task WaitForScriptAsync() =>
        Page.WaitForFunctionAsync("() => !!(window.BitBelemetry && window.BitBelemetry.getContext().sessionId)", null, new() { Timeout = 90_000 });

    /// <summary>The script's current context: consent, session, pseudo id and user id.</summary>
    protected async Task<BrowserContextInfo> ContextAsync()
    {
        await WaitForScriptAsync();
        return await Page.EvaluateAsync<BrowserContextInfo>("() => window.BitBelemetry.getContext()");
    }

    protected async Task<string> SessionIdAsync() => (await ContextAsync()).SessionId!;

    protected Task<string?> LocalStorageAsync(string key) => Page.EvaluateAsync<string?>("k => localStorage.getItem(k)", key);

    protected Task<string?> SessionStorageAsync(string key) => Page.EvaluateAsync<string?>("k => sessionStorage.getItem(k)", key);

    /// <summary>Hands everything the script queued to the transport now, instead of waiting for the flush interval.</summary>
    protected Task FlushScriptAsync() => Page.EvaluateAsync("() => window.BitBelemetry.flush()");

    /// <summary>The page being hidden, as switching tabs or minimizing does: the script reports and flushes on it.</summary>
    protected Task HidePageAsync() => Page.EvaluateAsync("() => window.dispatchEvent(new Event('pagehide'))");

    // A settable class rather than a positional record: Playwright materializes evaluate results through a
    // parameterless constructor.
    public sealed class BrowserContextInfo
    {
        public int Consent { get; set; }

        public bool Decided { get; set; }

        public string? SessionId { get; set; }

        public string? PseudoId { get; set; }

        public string? UserId { get; set; }
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
