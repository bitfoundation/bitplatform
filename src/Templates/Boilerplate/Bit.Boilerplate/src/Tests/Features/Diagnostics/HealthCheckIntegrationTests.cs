using System.Net;

namespace Boilerplate.Tests.Features.Diagnostics;

[TestClass, TestCategory("IntegrationTest")]
public partial class HealthCheckIntegrationTests
{
    public TestContext TestContext { get; set; } = default!;

    /// <summary>
    /// Verifies the application exposes a working liveness health-check endpoint. <c>MapAppHealthChecks</c>
    /// (See <c>WebApplicationExtensions.MapAppHealthChecks</c>) maps <c>GET /alive</c> with a predicate that runs
    /// only the checks tagged <c>"live"</c> - which, per <c>AddDefaultHealthChecks</c>, is exactly the single
    /// <c>binStorage</c> disk-storage check. That check is deterministically healthy in any test/CI environment
    /// (it only requires free disk space), so the endpoint returns HTTP 200 with the default plain-text body
    /// <c>"Healthy"</c>. <c>/alive</c> is deliberately preferred over <c>/health</c> here: <c>/health</c> also runs
    /// the Hangfire check (which is racy right after the host starts, before the Hangfire server has written its
    /// first heartbeat) plus the DbContext and blob-storage checks, none of which are deterministic at this instant.
    /// The endpoint is anonymous, so no sign-in is required; the request is issued through the DI <see cref="HttpClient"/>
    /// resolved from a request scope (BaseAddress is the test server address).
    /// </summary>
    [TestMethod]
    public async Task Health_Should_ReportHealthy()
    {
        await using var server = new AppTestServer();

        await server.Build(s => s.AddIntegrationApiOnlyTestsServices()).Start(TestContext.CancellationToken);

        await using var scope = server.WebApp.Services.CreateAsyncScope();
        var httpClient = scope.ServiceProvider.GetRequiredService<HttpClient>();

        using var response = await httpClient.GetAsync("alive", TestContext.CancellationToken);

        var body = await response.Content.ReadAsStringAsync(TestContext.CancellationToken);

        // The default MapHealthChecks response writer emits the overall HealthStatus name as plain text.
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, $"Unexpected status. Body: '{body}'.");
        Assert.AreEqual("Healthy", body.Trim());
    }
}
