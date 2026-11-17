using System.Text;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Boilerplate.Server.Api.Infrastructure.HealthChecks;
using Boilerplate.Tests.Features.Identity;

namespace Boilerplate.Tests.Features.Diagnostics;

[TestClass, TestCategory("UnitTest")]
public class AppInsightsHealthCheckTests
{
    private const string InstrumentationKey = "8a1f9d4c-2b3e-4f5a-9c8d-7e6f5a4b3c2d";

    public TestContext TestContext { get; set; } = default!;

    [TestMethod]
    [DataRow(HttpStatusCode.BadRequest, """{"itemsReceived":1,"itemsAccepted":0,"errors":[{"index":0,"statusCode":400,"message":"InvalidJsonObject: Not a valid JSON object"}]}""", HealthStatus.Healthy, "Application Insights accepts the instrumentation key", DisplayName = "Key accepted")]
    [DataRow(HttpStatusCode.BadRequest, """{"itemsReceived":1,"itemsAccepted":0,"errors":[{"index":0,"statusCode":400,"message":"Invalid instrumentation key"}]}""", HealthStatus.Degraded, "Application Insights answered 400 BadRequest: Invalid instrumentation key", DisplayName = "Unknown key")]
    [DataRow(HttpStatusCode.TooManyRequests, "", HealthStatus.Degraded, "Application Insights answered 429 TooManyRequests", DisplayName = "Throttled")]
    [DataRow(HttpStatusCode.Unauthorized, """{"itemsReceived":1,"itemsAccepted":0,"errors":[{"index":0,"statusCode":401,"message":"Authorization token is required"}]}""", HealthStatus.Degraded, "Application Insights answered 401 Unauthorized: Authorization token is required", DisplayName = "Local authentication off")]
    [DataRow(HttpStatusCode.ServiceUnavailable, "<html>Unavailable</html>", HealthStatus.Degraded, "Application Insights answered 503 ServiceUnavailable", DisplayName = "Outage")]
    public async Task Check_Should_JudgeTheIngestionEndpointsAnswer(HttpStatusCode status, string body, HealthStatus expected, string description)
    {
        string sentBody = string.Empty;
        var handler = new ExternalSignInHealthChecksTests.RoutingHandler(async (request, cancellationToken) =>
        {
            sentBody = await request.Content!.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
        });

        var result = await Run(handler, $"InstrumentationKey={InstrumentationKey};IngestionEndpoint=https://westeurope-5.in.applicationinsights.azure.com/");

        Assert.AreEqual(expected, result.Status, result.Description);
        Assert.AreEqual(description, result.Description);
        Assert.AreEqual("https://westeurope-5.in.applicationinsights.azure.com/v2.1/track", handler.Requests.Single().AbsoluteUri);

        var envelope = JsonNode.Parse(sentBody)!.AsArray().Single()!;
        Assert.AreEqual(InstrumentationKey, envelope["iKey"]!.ToString());
        Assert.IsNull(envelope["data"], "Without data nothing is ingested.");
    }

    [TestMethod]
    [DataRow($"InstrumentationKey={InstrumentationKey}", "https://dc.services.visualstudio.com/v2.1/track", DisplayName = "Default endpoint")]
    [DataRow($"InstrumentationKey={InstrumentationKey};EndpointSuffix=applicationinsights.azure.cn;Location=chinaeast2", "https://chinaeast2.dc.applicationinsights.azure.cn/v2.1/track", DisplayName = "Sovereign cloud")]
    public async Task Check_Should_PostWhereBelemetrySends(string connectionString, string trackUrl)
    {
        var handler = new ExternalSignInHealthChecksTests.RoutingHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("""{"errors":[{"message":"InvalidJsonObject: Not a valid JSON object"}]}""")
        }));

        var result = await Run(handler, connectionString);

        Assert.AreEqual(HealthStatus.Healthy, result.Status, result.Description);
        Assert.AreEqual(trackUrl, handler.Requests.Single().AbsoluteUri);
    }

    [TestMethod]
    [DataRow("IngestionEndpoint=https://westeurope-5.in.applicationinsights.azure.com/")]
    [DataRow("InstrumentationKey=not-a-guid")]
    public async Task Check_Should_SendNothing_WithoutAValidInstrumentationKey(string connectionString)
    {
        var handler = new ExternalSignInHealthChecksTests.RoutingHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));

        var result = await Run(handler, connectionString);

        Assert.AreEqual(HealthStatus.Degraded, result.Status);
        Assert.AreEqual("ApplicationInsights:ConnectionString has no valid InstrumentationKey", result.Description);
        Assert.IsEmpty(handler.Requests);
    }

    private async Task<HealthCheckResult> Run(HttpMessageHandler handler, string connectionString)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ApplicationInsights:ConnectionString"] = connectionString
        }).Build());
        services.AddSingleton(TimeProvider.System);
        services.AddHttpClient("ApplicationInsights").ConfigurePrimaryHttpMessageHandler(() => handler);

        await using var serviceProvider = services.BuildServiceProvider();

        var healthCheck = ActivatorUtilities.CreateInstance<AppInsightsHealthCheck>(serviceProvider);
        var registration = new HealthCheckRegistration("appInsights", healthCheck, HealthStatus.Degraded, tags: null);

        return await healthCheck.CheckHealthAsync(new HealthCheckContext { Registration = registration }, TestContext.CancellationToken);
    }
}
