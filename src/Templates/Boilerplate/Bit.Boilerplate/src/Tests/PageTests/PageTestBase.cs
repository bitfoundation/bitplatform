using Microsoft.AspNetCore.Builder;

namespace Boilerplate.Tests.PageTests;

[TestClass]
public partial class PageTestBase : PageTest
{
    protected AppTestServer TestServer { get; set; } = new();
    public WebApplication WebApp => TestServer.WebApp;
    public Uri WebAppServerAddress => TestServer.WebAppServerAddress;

    protected virtual bool AutoStartTestServer(string method) => true;

    [TestInitialize]
    public async Task InitializeTestServer()
    {
        if (AutoStartTestServer(TestContext.ManagedMethod!))
        {
            await TestServer.Build().Start();
        }
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
