namespace Boilerplate.Tests.Infrastructure;

// Every UI test drives a real browser, so running them concurrently starves weak CI runners and shows up as Playwright timeouts.
// MSTest runs non-parallelizable tests serially after the parallel ones, so the rest of the suite still uses all of the assembly's workers.
[DoNotParallelize]
// Browser tests are inherently timing sensitive, so give each one a couple of extra attempts before failing the run.
[Retry(2)]
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
    /// Video recording costs CPU on every test, which is exactly what makes tests flaky on constrained CI runners.
    /// <c>TestRunCount</c> is 1 on the first attempt and increments per retry, so the first attempt runs without a video
    /// and only the retries of an already failing test are recorded - a failure still ends up with a video to look at.
    /// </summary>
    public override BrowserNewContextOptions ContextOptions()
    {
        var options = base.ContextOptions();
        return TestContext.TestRunCount > 1 ? options.EnableVideoRecording(TestContext) : options;
    }

    [TestCleanup]
    public virtual async ValueTask Cleanup() => await Context.FinalizeVideoRecording(TestContext);
}
