namespace Boilerplate.Tests.TestBase;

[TestClass]
public partial class PageTestBase : PageTest
{
    private AppTestServer TestServer = null!;

    public Uri ServerAddress { get; private set; } = null!;
    public IServiceProvider Services { get; private set; } = null!;
    public IConfiguration Configuration { get; private set; } = null!;

    [TestInitialize]
    public async Task InitializeTestServer()
    {
        TestServer = new AppTestServer();

        await TestServer.Build(services =>
        {
            // Services registered in this test project will be used instead of the application's services, allowing you to fake certain behaviors during testing.
        }).Start();

        ServerAddress = TestServer.WebAppServerAddress;
        Services = TestServer.WebApp.Services;
        Configuration = TestServer.WebApp.Configuration;
    }

    [TestCleanup]
    public async ValueTask CleanupTestServer()
    {
        await Context.CloseAsync();
        if (TestContext.CurrentTestOutcome is not UnitTestOutcome.Failed)
        {
            var directory = GetVideoDirectory(TestContext);
            if (Directory.Exists(directory))
                Directory.Delete(directory, true);
        }

        if (TestServer is not null)
        {
            await TestServer.DisposeAsync();
        }
    }

    public override BrowserNewContextOptions ContextOptions()
    {
        var options = base.ContextOptions();
        options.RecordVideoDir = GetVideoDirectory(TestContext);
        return options;
    }

    private static string GetVideoDirectory(TestContext testContext)
    {
        var testMethodFullName = $"{testContext.FullyQualifiedTestClassName}.{testContext.TestName}";
        return Path.Combine(testContext.TestResultsDirectory!, "..", "..", "Videos", testMethodFullName);
    }
}
