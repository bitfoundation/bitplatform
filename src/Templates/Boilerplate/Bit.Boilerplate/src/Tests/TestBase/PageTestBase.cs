using Microsoft.AspNetCore.Builder;

namespace Boilerplate.Tests.TestBase;

[TestClass]
public partial class PageTestBase : PageTest
{
    private readonly AppTestServer TestServer = new();
    public WebApplication WebApp => TestServer.WebApp;
    public Uri WebAppServerAddress => TestServer.WebAppServerAddress;

    [TestInitialize]
    public async Task InitializeTestServer()
    {
        await TestServer.Build(services =>
        {
            // Services registered in this test project will be used instead of the application's services, allowing you to fake certain behaviors during testing.
        }).Start();
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

        var currentTestMethod = GetType().GetMethod(TestContext.ManagedMethod!);
        var isAuthenticated = currentTestMethod!.GetCustomAttributes(typeof(AuthenticatedAttribute), false).Length > 0;
        if (isAuthenticated)
        {
            options.StorageState = TestInitializer.AuthenticationState.Replace("[ServerAddress]", WebAppServerAddress.OriginalString);
        }

        return options;
    }

    private static string GetVideoDirectory(TestContext testContext)
    {
        var testMethodFullName = $"{testContext.FullyQualifiedTestClassName}.{testContext.TestName}";
        return Path.Combine(testContext.TestResultsDirectory!, "..", "..", "Videos", testMethodFullName);
    }
}

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class AuthenticatedAttribute : Attribute;