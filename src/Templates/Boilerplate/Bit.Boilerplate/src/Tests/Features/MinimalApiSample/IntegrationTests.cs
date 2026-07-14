using Boilerplate.Shared.Features.MinimalApiSample;

namespace Boilerplate.Tests.Features.MinimalApiSample;

[TestClass, TestCategory("IntegrationTest")]
public partial class IntegrationTests
{
    public TestContext TestContext { get; set; } = default!;

    /// <summary>
    /// Exercises the sample minimal API end-to-end through its typed HttpClient proxy (See
    /// <c>IMinimalApiSampleController</c> and its <c>app.MapGet("/api/minimal-api-sample/...")</c> registration):
    /// the endpoint simply echoes back the route and query-string parameters it received, so a round-trip proves
    /// both the route parameter and the (optional) query-string parameter are bound and returned correctly.
    /// It needs no sign-in because the endpoint is anonymous.
    /// </summary>
    [TestMethod]
    public async Task MinimalApiSample_Should_EchoRouteAndQueryStringParameters()
    {
        await using var server = new AppTestServer();

        await server.Build(s => s.AddIntegrationApiOnlyTestsServices()).Start(TestContext.CancellationToken);

        await using var scope = server.WebApp.Services.CreateAsyncScope();

        var minimalApiSampleController = scope.ServiceProvider.GetRequiredService<IMinimalApiSampleController>();

        var routeParameter = "sample-route";
        var queryStringParameter = "sample-query";

        var result = await minimalApiSampleController
            .MinimalApiSample(routeParameter, queryStringParameter, TestContext.CancellationToken);

        // The endpoint returns an anonymous object { RouteParameter, QueryStringParameter } serialized with the
        // app's camelCase policy (See AppJsonContext), so the echoed values must match what we sent.
        Assert.AreEqual(routeParameter, result.GetProperty("routeParameter").GetString());
        Assert.AreEqual(queryStringParameter, result.GetProperty("queryStringParameter").GetString());
    }
}
