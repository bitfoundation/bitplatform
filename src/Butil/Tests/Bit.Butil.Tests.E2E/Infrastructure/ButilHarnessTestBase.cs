using Microsoft.Playwright;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Butil.Tests.E2E.Infrastructure;

/// <summary>
/// Base of every fixture: opens the harness route on the host the run drives (see <see cref="HarnessTarget"/>),
/// waits until the page is interactive, and fails the test on anything the Blazor runtime reported as fatal.
/// </summary>
/// <remarks>
/// Manages Playwright itself rather than deriving from <c>Microsoft.Playwright.MSTest.PageTest</c>, because
/// runsettings are not threaded through the Microsoft.Testing.Platform runner reliably; the browser is configured
/// from <c>BUTIL_E2E_*</c> environment variables instead (see <see cref="BrowserLaunch"/> and
/// <see cref="E2EEnvironment"/>).
/// <br/>
/// The fatal-error check is load-bearing. An unhandled exception in a Server circuit terminates the circuit, and
/// in WebAssembly it takes the renderer down; either way every later click on the page is silently dropped and the
/// test only ever sees a status that never arrives. The console message that reports the exception is the one
/// place the actual cause is visible, so it is what the failure quotes.
/// </remarks>
public abstract class ButilHarnessTestBase
{
    // What the Blazor runtimes write to the console when the page stops being interactive: WebAssembly's
    // renderer logs "Unhandled exception rendering component", blazor.server.js/blazor.web.js report a circuit
    // an exception terminated. Anything a test provokes on purpose - a failed fetch, a 404 - is not on this list.
    private static readonly string[] FatalConsoleFragments =
    [
        "Unhandled exception rendering component",
        "unhandled exception on the current circuit",
        "Circuit has closed due to an error",
    ];

    private readonly object _errorsLock = new();
    private readonly List<string> _fatalErrors = [];
    private IHarnessLease? _lease;

    protected IPage Page { get; private set; } = default!;

    /// <summary>The harness route a derived fixture drives, e.g. "/e2e".</summary>
    protected abstract string HarnessRoute { get; }

    /// <summary>Whether the fixture needs Bit.Butil's lazy script loading instead of the bundle.</summary>
    protected virtual bool LazyScripts => false;

    /// <summary>The host this run drives - one of <see cref="HarnessHostKinds.All"/>.</summary>
    protected static string Host => E2EEnvironment.Host;

    [TestInitialize]
    public async Task SetUp()
    {
        if (Host is HarnessHostKinds.Hybrid && OperatingSystem.IsWindows() is false)
            Assert.Inconclusive("BlazorWebView renders through WebView2, which needs Windows.");

        _lease = await HarnessTarget.AcquireAsync(LazyScripts);
        Page = _lease.Page;
        Page.Console += OnConsole;
        Page.PageError += OnPageError;

        // Generous: a Debug WebAssembly boot, or a prerender followed by a circuit handshake, routinely takes
        // longer than a click on a CI runner without anything being wrong.
        await Page.GotoAsync(_lease.BaseUrl + HarnessRoute, new() { Timeout = 90_000 });
        await Assertions.Expect(Page.Locator("#status")).ToHaveTextAsync("ready", new() { Timeout = 90_000 });
    }

    [TestCleanup]
    public async Task TearDown()
    {
        if (_lease is not null)
        {
            Page.Console -= OnConsole;
            Page.PageError -= OnPageError;
            await _lease.DisposeAsync();
            _lease = null;
        }

        string[] fatal;
        lock (_errorsLock) fatal = [.. _fatalErrors];

        if (fatal.Length > 0)
            Assert.Fail($"The page reported {fatal.Length} fatal error(s) on {Host}:{Environment.NewLine}{string.Join(Environment.NewLine, fatal)}");
    }

    /// <summary>Clicks an element by id and waits for #status to contain the expected prefix.</summary>
    protected async Task ClickAndExpectAsync(string id, string statusPrefix, int timeoutMs = 15_000)
    {
        await Page.Locator($"#{id}").ClickAsync();
        await Assertions.Expect(Page.Locator("#status")).ToContainTextAsync(statusPrefix, new() { Timeout = timeoutMs });
    }

    protected async Task<string> CurrentStatusAsync()
        => (await Page.Locator("#status").TextContentAsync())?.Trim() ?? string.Empty;

    private void OnConsole(object? sender, IConsoleMessage message)
    {
        if (message.Type is not "error") return;
        if (FatalConsoleFragments.Any(fragment => message.Text.Contains(fragment, StringComparison.OrdinalIgnoreCase)) is false) return;

        lock (_errorsLock) _fatalErrors.Add($"console: {message.Text}");
    }

    private void OnPageError(object? sender, string error)
    {
        if (FatalConsoleFragments.Any(fragment => error.Contains(fragment, StringComparison.OrdinalIgnoreCase)) is false) return;

        lock (_errorsLock) _fatalErrors.Add($"uncaught: {error}");
    }
}
