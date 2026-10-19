using Microsoft.Playwright;

namespace Bit.Bsqlite.Tests.E2E.Infrastructure;

/// <summary>
/// One Playwright driver, one launched browser and one asset host for the whole run, plus the demo server
/// the first demo test asks for. Tests isolate from each other with a browser context apiece (separate
/// storage - OPFS, IndexedDB, Cache Storage, localStorage - and separate workers), which is far cheaper than
/// a browser apiece and isolates just as well.
/// </summary>
/// <remarks>
/// Whatever stops the suite from running at all - no browser, no compiled assets, a demo that does not
/// start - is captured once as an <c>Unavailable</c> reason, so every test reports it without waiting out
/// the same timeout again.
/// </remarks>
public static class PlaywrightSession
{
    private static readonly Lazy<Task<Session>> _session = new(StartAsync);
    private static readonly Lazy<Task<DemoSession>> _demo = new(StartDemoAsync);

    public static Task<Session> GetAsync() => _session.Value;

    /// <summary>The demo server, started on first use: the library tests never need it.</summary>
    public static Task<DemoSession> GetDemoAsync() => _demo.Value;

    public static async Task StopAsync()
    {
        if (_demo.IsValueCreated && (await _demo.Value).Host is { } demo) await demo.DisposeAsync();

        if (_session.IsValueCreated is false) return;

        var session = await _session.Value;

        if (session.Browser is not null)
        {
            try { await session.Browser.CloseAsync(); }
            catch (PlaywrightException) { /* the browser is gone already */ }
        }

        session.Playwright?.Dispose();

        if (session.Assets is not null) await session.Assets.DisposeAsync();
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
            return Session.Fail($"no {E2EEnvironment.Browser} browser could be launched. Run 'pwsh bin/<configuration>/net10.0/playwright.ps1 install {E2EEnvironment.Browser}'"
                + (E2EEnvironment.Browser == "chromium" ? ", or install Chrome or Edge." : "."));
        }

        try
        {
            return new Session(playwright, browser, await AssetHost.StartAsync(), Unavailable: null);
        }
        catch (Exception ex) when (ex is DirectoryNotFoundException or FileNotFoundException or IOException)
        {
            await browser.CloseAsync();
            playwright.Dispose();
            return Session.Fail("the library assets could not be served: " + ex.Message);
        }
    }

    private static async Task<DemoSession> StartDemoAsync()
    {
        try
        {
            return new DemoSession(await DemoHost.StartAsync(), Unavailable: null);
        }
        catch (Exception ex) when (ex is FileNotFoundException or InvalidOperationException or TimeoutException)
        {
            return new DemoSession(null, $"the demo application could not be started in the {E2EEnvironment.Configuration} configuration: {ex.Message}");
        }
    }

    private static async Task<IBrowser?> LaunchAsync(IPlaywright playwright)
    {
        var type = E2EEnvironment.Browser switch
        {
            "firefox" => playwright.Firefox,
            "webkit" => playwright.Webkit,
            _ => playwright.Chromium,
        };

        // Preferred: the browser Playwright manages. Falling back to an installed Chrome or Edge keeps the
        // suite usable where that download is blocked. A channel or an executable asked for explicitly is the
        // only candidate, and only Chromium has installed channels to fall back to.
        string?[] channels = E2EEnvironment.Channel is { } channel ? [channel]
            : E2EEnvironment.Executable is not null || type != playwright.Chromium ? [null]
            : [null, "chrome", "msedge"];

        foreach (var candidate in channels)
        {
            try
            {
                return await type.LaunchAsync(new BrowserTypeLaunchOptions
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

    /// <summary>The shared browser and asset host, or the reason the suite cannot run.</summary>
    public sealed record Session(IPlaywright? Playwright, IBrowser? Browser, AssetHost? Assets, string? Unavailable)
    {
        public static Session Fail(string reason) => new(null, null, null, reason);
    }

    /// <summary>The shared demo server, or the reason it could not start.</summary>
    public sealed record DemoSession(DemoHost? Host, string? Unavailable);
}
