using Microsoft.Playwright.TestAdapter;

namespace Boilerplate.Tests.Infrastructure;

public class AppPageTest : PageTest
{
    /// <summary>
    /// How long anything in this suite waits before giving up. Set on the context rather than the page so that pages
    /// this test opens later - the external sign-in popup, for one - are as patient as the first one.
    /// </summary>
    private static readonly TimeSpan defaultTimeout = TimeSpan.FromMinutes(1);

    [TestInitialize]
    public async Task PageTimeoutSetup()
    {
        Context.SetDefaultTimeout((float)defaultTimeout.TotalMilliseconds);
        Assertions.SetDefaultExpectTimeout((float)defaultTimeout.TotalMilliseconds);
    }

    /// <summary>
    /// Opens a second browser - an isolated context, i.e. its own storage and its own session - already carrying this
    /// suite's timeout and pointed at <paramref name="serverAddress"/>. The caller owns the context and disposes it.
    /// <para>
    /// Both of those are easy to forget and neither fails loudly: a context left on Playwright's own 30 second default
    /// simply gives up sooner than the first browser on the very same step, and one that never had the server address
    /// injected talks to whatever the app was built against.
    /// </para>
    /// </summary>
    protected async Task<IBrowserContext> NewBrowserContext(Uri serverAddress)
    {
        var context = await Browser.NewContextAsync(ContextOptions());

        context.SetDefaultTimeout((float)defaultTimeout.TotalMilliseconds);

        await SetBlazorWebAssemblyServerAddress(serverAddress, context);

        return context;
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
    /// Runs the selected <c>BROWSER</c> on another computer when <c>PLAYWRIGHT_SERVER_ENDPOINT</c> (see .runsettings)
    /// points at a Playwright server there - most usefully a mac, where webkit runs against the actual Apple frameworks
    /// and is as close to Safari as automation gets, but chromium and firefox connect the same way.
    /// </summary>
    public override async Task<(string, BrowserTypeConnectOptions?)?> ConnectOptionsAsync()
    {
        if (Environment.GetEnvironmentVariable("PLAYWRIGHT_SERVER_ENDPOINT") is { Length: > 0 } playwrightServerEndpoint)
            return (playwrightServerEndpoint, new());

        return await base.ConnectOptionsAsync();
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
            || string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("PWDEBUG")) is false;

        // The channel names a chromium build, so it must not be handed to a firefox / webkit run (See .runsettings' BROWSER).
        // Qualified because the inherited BrowserType property (an IBrowserType) shadows the type of the same name.
        if (isHeadedRun is false && PlaywrightSettingsProvider.BrowserName is Microsoft.Playwright.BrowserType.Chromium)
        {
            options.Channel = "chromium-headless-shell";
        }

        if (PlaywrightSettingsProvider.BrowserName is Microsoft.Playwright.BrowserType.Firefox)
        {
            // An automated firefox neither grants nor refuses a notification permission: no doorhanger is ever shown
            // and Notification.requestPermission() simply stays pending, which hangs whatever awaited it. 2 is deny,
            // so the answer comes back right away - the same "denied" chromium reports here without being told to.
            options.FirefoxUserPrefs = new Dictionary<string, object> { ["permissions.default.desktop-notification"] = 2 };
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
