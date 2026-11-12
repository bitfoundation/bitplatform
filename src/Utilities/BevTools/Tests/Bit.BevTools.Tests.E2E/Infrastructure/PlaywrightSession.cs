using Microsoft.Playwright;

namespace Bit.BevTools.Tests.E2E.Infrastructure;

/// <summary>
/// One Playwright driver and one launched browser for the whole run. Tests isolate from each other with a browser
/// context apiece (separate storage, cache and WebAssembly instance), which is far cheaper than a browser apiece and
/// isolates just as well. The app they drive is started by the suite that drives it (the lazy loading sample).
/// </summary>
/// <remarks>
/// Whatever stops the suite from running at all - no Playwright, no browser - is captured once as
/// <see cref="Session.Unavailable"/>, so every test reports it without trying the same launch again.
/// </remarks>
internal static class PlaywrightSession
{
    private static readonly Lazy<Task<Session>> _session = new(StartAsync);

    public static Task<Session> GetAsync() => _session.Value;

    public static async Task StopAsync()
    {
        if (_session.IsValueCreated is false) return;

        var session = await _session.Value;

        if (session.Browser is not null)
        {
            try { await session.Browser.CloseAsync(); }
            catch (PlaywrightException) { /* the browser is gone already */ }
        }

        session.Playwright?.Dispose();
    }

    private static async Task<Session> StartAsync()
    {
        IPlaywright playwright;
        try
        {
            playwright = await Playwright.CreateAsync();
        }
        catch (Exception ex) when (ex is PlaywrightException or FileNotFoundException or DllNotFoundException)
        {
            return Session.Fail("Playwright is not available: " + ex.Message);
        }

        var browser = await LaunchAsync(playwright);
        if (browser is null)
        {
            playwright.Dispose();
            return Session.Fail(E2EEnvironment.BrowserEngine is "firefox" or "webkit"
                ? $"{E2EEnvironment.BrowserEngine} could not be launched. Run 'pwsh bin/<configuration>/net11.0/playwright.ps1 install {E2EEnvironment.BrowserEngine}'."
                : "no Chromium-based browser could be launched. Run 'pwsh bin/<configuration>/net11.0/playwright.ps1 install chromium', or install Chrome or Edge.");
        }

        return new Session(playwright, browser, Unavailable: null);
    }

    private static async Task<IBrowser?> LaunchAsync(IPlaywright playwright)
    {
        if (E2EEnvironment.BrowserEngine is "firefox" or "webkit")
        {
            var type = E2EEnvironment.BrowserEngine == "firefox" ? playwright.Firefox : playwright.Webkit;
            try
            {
                return await type.LaunchAsync(new BrowserTypeLaunchOptions { Headless = E2EEnvironment.Headed is false });
            }
            catch (PlaywrightException)
            {
                return null;
            }
        }

        // Preferred: the full Chromium build Playwright manages, in its new headless mode - the code a person's Chrome
        // runs - then its headless shell. Falling back to an installed browser keeps the suite usable where that
        // download is blocked. A channel or an executable asked for explicitly is the only candidate.
        string?[] channels = E2EEnvironment.Channel is { } channel ? [channel]
            : E2EEnvironment.Executable is not null ? [null]
            : ["chromium", null, "chrome", "msedge"];

        foreach (var candidate in channels)
        {
            try
            {
                return await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
                {
                    Channel = candidate,
                    ExecutablePath = E2EEnvironment.Executable,
                    Headless = E2EEnvironment.Headed is false,
                });
            }
            catch (PlaywrightException)
            {
                // Try the next candidate.
            }
        }

        return null;
    }

    /// <summary>The shared browser, or the reason the suite cannot run.</summary>
    internal sealed record Session(IPlaywright? Playwright, IBrowser? Browser, string? Unavailable)
    {
        public static Session Fail(string reason) => new(null, null, reason);
    }
}
