using Microsoft.Playwright;

namespace Bit.Bswup.Tests.E2E.Infrastructure;

/// <summary>
/// One Playwright driver and one launched Chromium for the whole run. Tests isolate from each other with a
/// browser context apiece and an origin apiece, which is far cheaper than a browser apiece and isolates just
/// as well: service worker registrations and CacheStorage are keyed by origin.
/// </summary>
public static class PlaywrightSession
{
    private static readonly Lazy<Task<IPlaywright>> _playwright = new(Playwright.CreateAsync);
    private static readonly Lazy<Task<IBrowser>> _browser = new(LaunchAsync);

    public static Task<IBrowser> BrowserAsync() => _browser.Value;

    public static async Task StopAsync()
    {
        if (_browser.IsValueCreated)
        {
            try { await (await _browser.Value).CloseAsync(); }
            catch (Exception) { /* a browser that failed to launch already failed the tests */ }
        }

        if (_playwright.IsValueCreated)
        {
            try { (await _playwright.Value).Dispose(); }
            catch (Exception) { }
        }
    }

    private static async Task<IBrowser> LaunchAsync()
    {
        var options = new BrowserTypeLaunchOptions
        {
            Headless = E2EEnvironment.Headed is false,
            // Chromium already resolves *.localhost to the loopback address and treats it as a secure context
            // (service workers require one); the rule only makes that independent of the resolver in use.
            Args = ["--host-resolver-rules=MAP *.localhost 127.0.0.1"],
        };
        if (E2EEnvironment.Channel is { } channel) options.Channel = channel;
        if (E2EEnvironment.Executable is { } executable) options.ExecutablePath = executable;

        return await (await _playwright.Value).Chromium.LaunchAsync(options);
    }
}
