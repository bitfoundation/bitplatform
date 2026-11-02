using Microsoft.Playwright;

namespace Bit.Bcrdt.Tests.E2E.Infrastructure;

/// <summary>
/// One Playwright driver, one launched browser (Chromium unless <c>BCRDT_E2E_BROWSER</c> names another engine) and
/// one running demo for the whole run. Tests isolate from each other with a browser context apiece (separate
/// storage, cache and WebAssembly instance), which is far cheaper than a browser apiece and isolates just as well.
/// </summary>
/// <remarks>
/// Whatever stops the suite from running at all - no browser, a demo that does not start - is captured
/// once as <see cref="Session.Unavailable"/>, so every test reports it without waiting out the same
/// timeout again.
/// </remarks>
public static class PlaywrightSession
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

        if (session.Host is not null) await session.Host.DisposeAsync();
    }

    private static async Task<Session> StartAsync()
    {
        if (E2EEnvironment.Browser is not ("chromium" or "firefox" or "webkit"))
            return Session.Fail($"BCRDT_E2E_BROWSER is '{E2EEnvironment.Browser}'; it can be chromium, firefox or webkit.");

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
            return Session.Fail(E2EEnvironment.Browser is "chromium"
                ? "no Chromium-based browser could be launched. Run 'pwsh bin/<configuration>/net11.0/playwright.ps1 install chromium', or install Chrome or Edge."
                : $"{E2EEnvironment.Browser} could not be launched. Run 'pwsh bin/<configuration>/net11.0/playwright.ps1 install {E2EEnvironment.Browser}'.");
        }

        try
        {
            return new Session(playwright, browser, await DemoHost.StartAsync(), Unavailable: null);
        }
        catch (Exception ex) when (ex is FileNotFoundException or InvalidOperationException or TimeoutException)
        {
            await browser.CloseAsync();
            playwright.Dispose();
            return Session.Fail($"the demo application could not be started in the {E2EEnvironment.Configuration} configuration: {ex.Message}");
        }
    }

    private static async Task<IBrowser?> LaunchAsync(IPlaywright playwright)
    {
        switch (E2EEnvironment.Browser)
        {
            case "firefox":
                return await TryLaunchAsync(playwright.Firefox, new BrowserTypeLaunchOptions
                {
                    ExecutablePath = E2EEnvironment.Executable,
                    Headless = E2EEnvironment.Headed is false,
                    // The same reason as Chromium's switches below: keep the timers of a window that is not in
                    // front running at full speed.
                    FirefoxUserPrefs = new Dictionary<string, object>
                    {
                        ["dom.min_background_timeout_value"] = 0,
                        ["dom.timeout.enable_budget_timer_throttling"] = false,
                    },
                });

            case "webkit":
                return await TryLaunchAsync(playwright.Webkit, new BrowserTypeLaunchOptions
                {
                    ExecutablePath = E2EEnvironment.Executable,
                    Headless = E2EEnvironment.Headed is false,
                });
        }

        // Chromium, the default (StartAsync has turned away any other name). The browser Playwright manages is
        // preferred; falling back to an installed one keeps the suite usable where that download is blocked. A
        // channel or an executable asked for explicitly is the only candidate.
        string?[] channels = E2EEnvironment.Channel is { } channel ? [channel]
            : E2EEnvironment.Executable is not null ? [null]
            : [null, "chrome", "msedge"];

        foreach (var candidate in channels)
        {
            var browser = await TryLaunchAsync(playwright.Chromium, new BrowserTypeLaunchOptions
            {
                Channel = candidate,
                ExecutablePath = E2EEnvironment.Executable,
                Headless = E2EEnvironment.Headed is false,
                // The live tests keep two pages busy at once, each in a context (a window) of its own, and in a
                // headed run only one of them is in front. Chromium throttles the timers of a page it considers
                // hidden, which would stretch the other tab's heartbeat and reconnect timers into the timeouts.
                Args =
                [
                    "--disable-background-timer-throttling",
                    "--disable-backgrounding-occluded-windows",
                    "--disable-renderer-backgrounding",
                ],
            });
            if (browser is not null) return browser;
        }

        return null;
    }

    private static async Task<IBrowser?> TryLaunchAsync(IBrowserType type, BrowserTypeLaunchOptions options)
    {
        try
        {
            return await type.LaunchAsync(options);
        }
        catch (PlaywrightException)
        {
            return null;
        }
    }

    /// <summary>The shared browser and demo, or the reason the suite cannot run.</summary>
    public sealed record Session(IPlaywright? Playwright, IBrowser? Browser, DemoHost? Host, string? Unavailable)
    {
        public static Session Fail(string reason) => new(null, null, null, reason);
    }
}
