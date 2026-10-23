using Microsoft.Playwright;

namespace Bit.Bmage.Tests.E2E.Infrastructure;

/// <summary>
/// One Playwright driver and one launched browser for the whole run. Tests isolate from each other
/// with a browser context apiece (separate storage, cache and WebAssembly instance), which is far
/// cheaper than a browser apiece and isolates just as well.
/// </summary>
/// <remarks>
/// Playwright's bundled Chromium is used when installed. When it is not, a Chrome or Edge installed on
/// the machine is used instead, and when neither launches the tests report Inconclusive with the
/// reason - so a checkout without browsers still builds and runs every other suite.
/// <c>BMAGE_E2E_BROWSER</c> picks Playwright's Firefox or WebKit instead (see <see cref="E2EEnvironment.Browser"/>).
/// </remarks>
public static class PlaywrightSession
{
    private static readonly Lazy<Task<IPlaywright>> _playwright = new(Playwright.CreateAsync);
    private static readonly Lazy<Task<(IBrowser? Browser, string? Failure)>> _browser = new(LaunchAsync);

    /// <summary>The shared browser, or null with the reason when no browser could be launched.</summary>
    public static Task<(IBrowser? Browser, string? Failure)> BrowserAsync() => _browser.Value;

    public static async Task StopAsync()
    {
        if (_browser.IsValueCreated)
        {
            try
            {
                if ((await _browser.Value).Browser is { } browser) await browser.CloseAsync();
            }
            catch (Exception) { /* a browser that failed to launch already failed the tests */ }
        }

        if (_playwright.IsValueCreated)
        {
            try { (await _playwright.Value).Dispose(); }
            catch (Exception) { }
        }
    }

    private static async Task<(IBrowser?, string?)> LaunchAsync()
    {
        IPlaywright playwright;
        try
        {
            playwright = await _playwright.Value;
        }
        catch (Exception ex)
        {
            return (null, $"Playwright could not start: {ex.Message}");
        }

        var headless = E2EEnvironment.Headed is false;
        if (E2EEnvironment.Browser is "firefox" or "webkit")
        {
            var type = E2EEnvironment.Browser == "firefox" ? playwright.Firefox : playwright.Webkit;
            try
            {
                return (await type.LaunchAsync(new() { Headless = headless }), null);
            }
            catch (PlaywrightException ex)
            {
                return (null, $"Playwright's {E2EEnvironment.Browser} could not be launched. Run 'pwsh bin/Debug/net10.0/playwright.ps1 install {E2EEnvironment.Browser}'. {ex.Message.Split('\n')[0]}");
            }
        }
        if (E2EEnvironment.Browser != "chromium")
            return (null, $"BMAGE_E2E_BROWSER is '{E2EEnvironment.Browser}'; it must be chromium, firefox or webkit.");

        BrowserTypeLaunchOptions[] candidates = E2EEnvironment.Executable is { } executable
            ? [new() { Headless = headless, ExecutablePath = executable }]
            : E2EEnvironment.Channel is { } channel
                ? [new() { Headless = headless, Channel = channel }]
                // Playwright's own build first, then a system Chrome, then Edge.
                : [new() { Headless = headless }, new() { Headless = headless, Channel = "chrome" }, new() { Headless = headless, Channel = "msedge" }];

        var failures = new List<string>();
        foreach (var options in candidates)
        {
            try
            {
                return (await playwright.Chromium.LaunchAsync(options), null);
            }
            catch (PlaywrightException ex)
            {
                failures.Add($"{options.Channel ?? options.ExecutablePath ?? "bundled chromium"}: {ex.Message.Split('\n')[0]}");
            }
        }

        return (null, "No browser is available. Run 'pwsh bin/Debug/net10.0/playwright.ps1 install chromium', or install Chrome or Edge. "
            + string.Join(" | ", failures));
    }
}
