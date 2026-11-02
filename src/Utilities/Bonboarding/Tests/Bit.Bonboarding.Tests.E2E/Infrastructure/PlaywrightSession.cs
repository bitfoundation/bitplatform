using Microsoft.Playwright;

namespace Bit.Bonboarding.Tests.E2E.Infrastructure;

/// <summary>
/// One Playwright driver, one launched browser (Chromium unless <c>BONBOARDING_E2E_BROWSER</c> names another engine) and
/// one running demo for the whole run. Tests isolate
/// from each other with a browser context apiece (separate storage, cache and WebAssembly instance),
/// which is far cheaper than a browser apiece and isolates just as well.
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
        IPlaywright playwright;
        try
        {
            playwright = await Playwright.CreateAsync();
        }
        catch (Exception ex) when (ex is PlaywrightException or FileNotFoundException or DllNotFoundException)
        {
            return Session.Fail("Playwright is not available: " + ex.Message);
        }

        if (E2EEnvironment.Browser is not ("chromium" or "firefox" or "webkit"))
        {
            playwright.Dispose();
            return Session.Fail($"BONBOARDING_E2E_BROWSER is '{E2EEnvironment.Browser}'; it can be chromium, firefox or webkit.");
        }

        var browser = await LaunchAsync(playwright);
        if (browser is null)
        {
            playwright.Dispose();
            return Session.Fail(E2EEnvironment.IsChromium
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
        if (E2EEnvironment.IsChromium is false)
        {
            // Every context is created at a device scale factor of 1, which is what the Chromium flags below pin
            // for Chromium; the other engines take it from the context alone.
            var engine = E2EEnvironment.Browser == "firefox" ? playwright.Firefox : playwright.Webkit;
            try
            {
                return await engine.LaunchAsync(new BrowserTypeLaunchOptions
                {
                    ExecutablePath = E2EEnvironment.Executable,
                    Headless = E2EEnvironment.Headed is false,
                });
            }
            catch (PlaywrightException)
            {
                return null;
            }
        }

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
                    // Grayscale antialiasing and a fixed scale factor keep the pixel measurements the tests
                    // take independent of the machine's display settings.
                    Args = ["--disable-lcd-text", "--force-device-scale-factor=1"],
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
        public static Session Fail(string reason) => new(null, null, null, reason);
    }
}
