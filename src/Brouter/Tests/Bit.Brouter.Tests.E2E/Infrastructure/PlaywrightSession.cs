using Microsoft.Playwright;

namespace Bit.Brouter.Tests.E2E.Infrastructure;

/// <summary>
/// One Playwright driver and one launched Chromium for the whole run. Tests isolate from each other
/// with a browser context apiece (separate cookies, storage, cache, circuit and WebAssembly
/// instance), which is far cheaper than a browser apiece and isolates just as well.
/// </summary>
public static class PlaywrightSession
{
    private static readonly Lazy<Task<IPlaywright>> _playwright = new(Playwright.CreateAsync);
    private static readonly Lazy<Task<IBrowser>> _browser = new(LaunchAsync);

    public static Task<IPlaywright> PlaywrightAsync() => _playwright.Value;

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
        var options = new BrowserTypeLaunchOptions { Headless = E2EEnvironment.Headed is false };
        if (E2EEnvironment.Channel is { } channel) options.Channel = channel;
        if (E2EEnvironment.Executable is { } executable) options.ExecutablePath = executable;

        return await (await PlaywrightAsync()).Chromium.LaunchAsync(options);
    }
}
