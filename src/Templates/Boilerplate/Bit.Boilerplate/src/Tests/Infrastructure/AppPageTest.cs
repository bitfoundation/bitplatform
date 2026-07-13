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

    public override BrowserNewContextOptions ContextOptions() => base.ContextOptions().EnableVideoRecording(TestContext);

    [TestCleanup]
    public virtual async ValueTask Cleanup() => await Context.FinalizeVideoRecording(TestContext);
}
