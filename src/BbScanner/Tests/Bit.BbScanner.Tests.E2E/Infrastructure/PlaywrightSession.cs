using Microsoft.Playwright;

namespace Bit.BbScanner.Tests.E2E.Infrastructure;

/// <summary>
/// One Playwright driver, one running demo and one set of camera clips for the whole run.
/// </summary>
/// <remarks>
/// <para>
/// Unlike most browser suites this one cannot share a browser between tests: the camera a page sees is a
/// launch argument of the browser process (<c>--use-file-for-fake-video-capture</c>), and so is whether the
/// permission prompt is accepted or denied. Every test therefore launches its own Chromium - see
/// <see cref="BbScannerTest.OpenAsync"/> - and what is shared is everything that is expensive and the same for
/// all of them: the driver, the demo server, the generated clips, and which browser could be launched at all.
/// </para>
/// <para>
/// Whatever stops the suite from running at all - no browser, a demo that does not start - is captured once as
/// <see cref="Session.Unavailable"/>, so every test reports it without waiting out the same timeout again.
/// </para>
/// </remarks>
public static class PlaywrightSession
{
    private static readonly Lazy<Task<Session>> _session = new(StartAsync);

    public static Task<Session> GetAsync() => _session.Value;

    public static async Task StopAsync()
    {
        if (_session.IsValueCreated is false) return;

        var session = await _session.Value;

        session.Playwright?.Dispose();

        if (session.Host is not null) await session.Host.DisposeAsync();

        session.Clips?.Dispose();
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

        var launch = await ResolveLaunchAsync(playwright);
        if (launch is null)
        {
            playwright.Dispose();
            return Session.Fail("no Chromium-based browser could be launched. Run 'pwsh bin/<configuration>/net10.0/playwright.ps1 install chromium', or install Chrome or Edge.");
        }

        var clips = CameraClips.Write();

        try
        {
            return new Session(playwright, launch, await DemoHost.StartAsync(), clips, Unavailable: null);
        }
        catch (Exception ex) when (ex is FileNotFoundException or InvalidOperationException or TimeoutException)
        {
            playwright.Dispose();
            clips.Dispose();
            return Session.Fail($"the demo application could not be started in the {E2EEnvironment.Configuration} configuration: {ex.Message}");
        }
    }

    /// <summary>
    /// Finds the browser this machine can launch, once. Preferred: the browser Playwright manages. Falling back
    /// to an installed one keeps the suite usable where that download is blocked. A channel or an executable
    /// asked for explicitly is the only candidate.
    /// </summary>
    /// <remarks>
    /// The managed browser is asked for as the <c>chromium</c> channel - the full Chromium build in its new
    /// headless mode - rather than as Playwright's default headless shell. The shell answers a camera request
    /// the page has no permission for with <c>NotSupportedError</c> where every real browser answers
    /// <c>NotAllowedError</c>, which would turn a denied permission into "no camera API" and make the suite test
    /// a browser nobody uses.
    /// </remarks>
    private static async Task<BrowserTypeLaunchOptions?> ResolveLaunchAsync(IPlaywright playwright)
    {
        string?[] channels = E2EEnvironment.Channel is { } channel ? [channel]
            : E2EEnvironment.Executable is not null ? [null]
            : ["chromium", "chrome", "msedge"];

        foreach (var candidate in channels)
        {
            var options = new BrowserTypeLaunchOptions
            {
                Channel = candidate,
                ExecutablePath = E2EEnvironment.Executable,
                Headless = E2EEnvironment.Headed is false,
            };

            try
            {
                var browser = await playwright.Chromium.LaunchAsync(options);
                await browser.CloseAsync();
                return options;
            }
            catch (PlaywrightException)
            {
                // Try the next candidate.
            }
        }

        return null;
    }

    /// <summary>The shared driver, demo and clips, or the reason the suite cannot run.</summary>
    public sealed record Session(IPlaywright? Playwright, BrowserTypeLaunchOptions? Launch, DemoHost? Host, CameraClips? Clips, string? Unavailable)
    {
        public static Session Fail(string reason) => new(null, null, null, null, reason);
    }
}
