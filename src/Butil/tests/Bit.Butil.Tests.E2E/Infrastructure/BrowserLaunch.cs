using Microsoft.Playwright;

namespace Bit.Butil.Tests.E2E.Infrastructure;

/// <summary>
/// How the browser suites launch Chromium, from environment variables rather than from runsettings
/// (which the Microsoft.Testing.Platform runner does not thread through reliably):
/// <list type="bullet">
///   <item><c>BUTIL_E2E_CHANNEL</c> - e.g. <c>chrome</c> / <c>msedge</c> (uses an installed browser).</item>
///   <item><c>BUTIL_E2E_EXECUTABLE</c> - full path to a chromium-family executable.</item>
///   <item><c>BUTIL_E2E_HEADED</c> - set to <c>1</c> to watch the run.</item>
/// </list>
/// </summary>
/// <remarks>
/// Shared by the E2E suite and, as a linked source file, by the benchmark suite, so one setup serves
/// both and a variable added here reaches both.
/// </remarks>
public static class BrowserLaunch
{
    public static BrowserTypeLaunchOptions OptionsFromEnvironment()
    {
        var options = new BrowserTypeLaunchOptions
        {
            Headless = Environment.GetEnvironmentVariable("BUTIL_E2E_HEADED") != "1"
        };

        var channel = Environment.GetEnvironmentVariable("BUTIL_E2E_CHANNEL");
        if (string.IsNullOrWhiteSpace(channel) is false) options.Channel = channel;

        var executable = Environment.GetEnvironmentVariable("BUTIL_E2E_EXECUTABLE");
        if (string.IsNullOrWhiteSpace(executable) is false) options.ExecutablePath = executable;

        return options;
    }
}
