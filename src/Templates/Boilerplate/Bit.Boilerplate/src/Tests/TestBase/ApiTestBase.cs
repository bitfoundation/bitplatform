namespace Boilerplate.Tests.TestBase;

[TestClass]
public partial class ApiTestBase
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
    public ValueTask CleanupTestServer()
    {
        return TestServer.DisposeAsync();
    }
}
