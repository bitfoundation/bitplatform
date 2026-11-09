namespace Boilerplate.Tests.Features.ForceUpdate;

[TestClass, TestCategory("IntegrationTest")]
public partial class ForceUpdateTests
{
    public TestContext TestContext { get; set; } = default!;

    [TestMethod]
    public async Task AClientWhoseMajorVersionIsBehindTheServers_Should_BeRefused()
    {
        await using var server = new AppTestServer();

        // The app starts at 0.0.1, where no client can be behind, so the server is given a version with a major.
        await server.Build(configureTestConfiguration: configuration => configuration["ServerVersion"] = "2.5.1")
            .Start(TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.BadRequest, await CallAs(server, "1.9.9"));
        Assert.AreEqual(HttpStatusCode.OK, await CallAs(server, "2.0.0"));
    }

    [TestMethod]
    public async Task AClientOnTheStartingVersion_Should_BeAccepted()
    {
        await using var server = new AppTestServer();

        await server.Build().Start(TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.OK, await CallAs(server, "0.0.1"));
    }

    [TestMethod]
    public async Task AMinimumVersionSetForThePlatform_Should_WinOverTheMajorVersionRule()
    {
        await using var server = new AppTestServer();

        await server.Build(configureTestConfiguration: configuration => configuration["SupportedAppVersions:MinimumSupportedWebAppVersion"] = "0.5.0")
            .Start(TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.OK, await CallAs(server, "0.9.0"));
        Assert.AreEqual(HttpStatusCode.BadRequest, await CallAs(server, "0.4.0"));
    }

    private async Task<HttpStatusCode> CallAs(AppTestServer server, string appVersion)
    {
        using var client = server.CreateRawHttpClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "alive");
        request.Headers.Add("X-App-Version", appVersion);
        request.Headers.Add("X-App-Platform", "Web");

        using var response = await client.SendAsync(request, TestContext.CancellationToken);

        return response.StatusCode;
    }
}
