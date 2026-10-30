using Microsoft.Playwright;

namespace Bit.Bheadless.Tests.E2E.Infrastructure;

/// <summary>
/// One Playwright driver, one launched Chromium and one running demo for the whole run. Tests isolate from each other
/// with a browser context apiece (separate storage, cache and WebAssembly instance or circuit), which is far cheaper
/// than a browser apiece and isolates just as well - and lets them run side by side.
/// </summary>
/// <remarks>
/// Whatever stops the suite from running at all - no browser, a demo that does not publish or start - is captured
/// once as <see cref="Session.Unavailable"/>, so every test reports it without waiting out the same timeout again.
/// </remarks>
public static class PlaywrightSession
{
    private static readonly Lazy<Task<Session>> _session = new(StartAsync);
    private static readonly SemaphoreSlim _relaunch = new(1, 1);

    public static Task<Session> GetAsync() => _session.Value;

    /// <summary>
    /// Launches the browser again when an earlier test took it down. A crash of the browser process is that test's
    /// failure; without this, every test after it would fail too, on a browser that is gone, and bury the one that
    /// matters. Serialized: the tests run in parallel, and only one of them should relaunch it.
    /// </summary>
    public static async Task<string?> EnsureBrowserAsync(Session session)
    {
        if (session.Browser is { IsConnected: true }) return null;

        await _relaunch.WaitAsync();
        try
        {
            if (session.Browser is { IsConnected: true }) return null;

            var browser = await LaunchAsync(session.Playwright!);
            if (browser is null) return "Chromium exited and could not be launched again.";

            session.Browser = browser;
            return null;
        }
        finally
        {
            _relaunch.Release();
        }
    }

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

        if (session.Host is not null) await session.Host.DisposeAsync();
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
            return Session.Fail($"no Chromium-based browser could be launched. Run 'pwsh {Path.Combine(AppContext.BaseDirectory, "playwright.ps1")} install chromium', or install Chrome or Edge.");
        }

        try
        {
            return new Session(playwright, browser, await DemoHost.StartAsync(), Unavailable: null);
        }
        catch (Exception ex) when (ex is FileNotFoundException or InvalidOperationException or TimeoutException or HttpRequestException)
        {
            await browser.CloseAsync();
            playwright.Dispose();
            return Session.Fail($"the demo could not be published or started ({E2EEnvironment.RenderMode}): {ex.Message}");
        }
    }

    private static async Task<IBrowser?> LaunchAsync(IPlaywright playwright)
    {
        // Preferred: the browser Playwright manages. Falling back to an installed one keeps the suite usable
        // where that download is blocked. A channel or an executable asked for explicitly is the only candidate.
        string?[] channels = E2EEnvironment.Channel is { } channel ? [channel]
            : E2EEnvironment.Executable is not null ? [null]
            : [null, "chrome", "msedge"];

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

    /// <summary>The shared browser and demo, or the reason the suite cannot run.</summary>
    public sealed record Session(IPlaywright? Playwright, IBrowser? Browser, DemoHost? Host, string? Unavailable)
    {
        /// <summary>The browser now: <see cref="EnsureBrowserAsync"/> replaces one that exited.</summary>
        public IBrowser? Browser { get; set; } = Browser;

        public static Session Fail(string reason) => new(null, null, null, reason);
    }
}
