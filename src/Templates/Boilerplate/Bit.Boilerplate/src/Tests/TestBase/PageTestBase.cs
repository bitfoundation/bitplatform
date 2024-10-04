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
        }).StartAsync();

        Services = TestServer.Services;
        ServerAddress = TestServer.ServerAddress;
        Configuration = TestServer.Configuration;
    }

    [TestCleanup]
    public ValueTask CleanupTestServer()
    {
        return TestServer.DisposeAsync();
    }
}
