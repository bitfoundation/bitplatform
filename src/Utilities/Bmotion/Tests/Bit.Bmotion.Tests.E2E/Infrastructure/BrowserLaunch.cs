using Microsoft.Playwright;

namespace Bit.Bmotion.Tests.E2E.Infrastructure;

/// <summary>
/// How the browser suites launch their browser, from environment variables rather than from runsettings
/// (which the Microsoft.Testing.Platform runner does not thread through reliably):
/// <list type="bullet">
///   <item><c>BMOTION_E2E_BROWSER</c> - <c>chromium</c> (the default), <c>firefox</c> or <c>webkit</c>.</item>
///   <item><c>BMOTION_E2E_CHANNEL</c> - e.g. <c>chrome</c> / <c>msedge</c> (uses an installed browser; Chromium only).</item>
///   <item><c>BMOTION_E2E_EXECUTABLE</c> - full path to an executable of the chosen browser family.</item>
///   <item><c>BMOTION_E2E_HEADED</c> - set to <c>1</c> to watch the run.</item>
/// </list>
/// </summary>
public static class BrowserLaunch
{
    /// <summary>The browser family the run uses: <c>chromium</c>, <c>firefox</c> or <c>webkit</c>.</summary>
    public static string Browser { get; } = (Environment.GetEnvironmentVariable("BMOTION_E2E_BROWSER") ?? "") switch
    {
        var b when b.Equals("firefox", StringComparison.OrdinalIgnoreCase) => "firefox",
        var b when b.Equals("webkit", StringComparison.OrdinalIgnoreCase) => "webkit",
        var b when string.IsNullOrWhiteSpace(b) || b.Equals("chromium", StringComparison.OrdinalIgnoreCase) => "chromium",
        var b => throw new InvalidOperationException($"BMOTION_E2E_BROWSER '{b}' is not chromium, firefox or webkit."),
    };

    /// <summary>The Playwright launcher of <see cref="Browser"/>.</summary>
    public static IBrowserType TypeOf(IPlaywright playwright) => Browser switch
    {
        "firefox" => playwright.Firefox,
        "webkit" => playwright.Webkit,
        _ => playwright.Chromium,
    };

    public static BrowserTypeLaunchOptions OptionsFromEnvironment()
    {
        var options = new BrowserTypeLaunchOptions
        {
            Headless = Environment.GetEnvironmentVariable("BMOTION_E2E_HEADED") != "1"
        };

        // A channel names an installed Chromium-family browser.
        var channel = Environment.GetEnvironmentVariable("BMOTION_E2E_CHANNEL");
        if (string.IsNullOrWhiteSpace(channel) is false && Browser == "chromium") options.Channel = channel;

        var executable = Environment.GetEnvironmentVariable("BMOTION_E2E_EXECUTABLE");
        if (string.IsNullOrWhiteSpace(executable) is false) options.ExecutablePath = executable;

        return options;
    }
}
