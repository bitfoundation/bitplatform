using Microsoft.Playwright.TestAdapter;

namespace Boilerplate.Tests.Infrastructure;

public class AppPageTest : PageTest
{
    [TestInitialize]
    public async Task PageTimeoutSetup()
    {
        Page.SetDefaultTimeout((float)TimeSpan.FromMinutes(1).TotalMilliseconds);
    }

    /// <summary>
    /// Passes the server address to the Blazor WebAssembly app through a <c>startupParams</c> JS function that
    /// Client.Web/Program.cs reads on startup (see its advancedTests block), overriding the app's configured
    /// ServerAddress so the browser app talks to our test server rather than a hard-coded address.
    /// More info: https://stackoverflow.com/questions/60831359/how-are-string-args-passed-to-program-main-in-a-blazor-webassembly-app
    /// </summary>
    protected async Task SetBlazorWebAssemblyServerAddress(Uri serverAddress, IBrowserContext context)
    {
        await context.AddInitScriptAsync($"window.startupParams = function() {{ return [ 'ServerAddress={serverAddress}' ]; }};");
    }

    /// <summary>
    /// Runs the headless tests on <c>chromium-headless-shell</c> - the small headless-only build that plain
    /// <c>Headless = true</c> used to mean before Playwright 1.49 made the <c>chromium</c> channel launch the real Chrome
    /// browser in its new headless mode. The shell is what the suite actually needs and is markedly cheaper.
    /// </summary>
    public override async Task<BrowserTypeLaunchOptions?> LaunchOptionsAsync()
    {
        var options = new BrowserTypeLaunchOptions(PlaywrightSettingsProvider.LaunchOptions);

        // The shell has no UI at all, so a debugging run must keep the full browser. .runsettings documents both switches.
        var isHeadedRun = options.Headless is false
            || Environment.GetEnvironmentVariable("HEADED") is "1"
            || string.IsNullOrEmpty(Environment.GetEnvironmentVariable("PWDEBUG")) is false;

        // The channel names a chromium build, so it must not be handed to a firefox / webkit run (See .runsettings' BROWSER).
        // Qualified because the inherited BrowserType property (an IBrowserType) shadows the type of the same name.
        if (isHeadedRun is false && PlaywrightSettingsProvider.BrowserName is Microsoft.Playwright.BrowserType.Chromium)
        {
            options.Channel = "chromium-headless-shell";
        }

        return options;
    }

    /// <summary>
    /// Video recording costs CPU on every test, which is exactly what makes tests flaky on constrained CI runners.
    /// <c>TestRunCount</c> is 1 on the first attempt and increments per retry, so the first attempt runs without a video
    /// and only the retries of an already failing test are recorded - a failure still ends up with a video to look at.
    /// </summary>
    public override BrowserNewContextOptions ContextOptions()
    {
        var options = base.ContextOptions();

        options.IgnoreHTTPSErrors = true;

        return TestContext.TestRunCount > 1 ? options.EnableVideoRecording(TestContext) : options;
    }

    [TestCleanup]
    public virtual async ValueTask Cleanup() => await Context.FinalizeVideoRecording(TestContext);
}
