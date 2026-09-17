using System.Text;
using Microsoft.IdentityModel.Tokens;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Boilerplate.Server.Api.Infrastructure.SignalR;
using Boilerplate.Tests.Features.Identity;

namespace Boilerplate.Tests.Features.SignalR;

/// <summary>
/// The health api, then a user lookup signed as the data plane REST API expects (HS256, aud = the url without its query).
/// </summary>
[TestClass, TestCategory("UnitTest")]
public class AzureSignalRHealthCheckTests
{
    private const string AccessKey = "dGVzdC1hY2Nlc3Mta2V5LXRoYXQtaXMtbG9uZy1lbm91Z2g=";

    public TestContext TestContext { get; set; } = default!;

    [TestMethod]
    [DataRow(HttpStatusCode.OK, HttpStatusCode.NotFound, HealthStatus.Healthy, DisplayName = "Key accepted")]
    [DataRow(HttpStatusCode.OK, HttpStatusCode.Unauthorized, HealthStatus.Degraded, DisplayName = "Key rejected")]
    [DataRow(HttpStatusCode.OK, HttpStatusCode.Forbidden, HealthStatus.Degraded, DisplayName = "Key forbidden")]
    [DataRow(HttpStatusCode.OK, HttpStatusCode.BadRequest, HealthStatus.Degraded, DisplayName = "Bad request")]
    [DataRow(HttpStatusCode.OK, HttpStatusCode.TooManyRequests, HealthStatus.Degraded, DisplayName = "Throttled")]
    [DataRow(HttpStatusCode.OK, HttpStatusCode.InternalServerError, HealthStatus.Degraded, DisplayName = "Service error")]
    [DataRow(HttpStatusCode.ServiceUnavailable, HttpStatusCode.NotFound, HealthStatus.Degraded, DisplayName = "Unhealthy service")]
    public async Task Check_Should_JudgeTheServicesAnswers(HttpStatusCode healthStatus, HttpStatusCode userStatus, HealthStatus expected)
    {
        var handler = new ExternalSignInHealthChecksTests.RoutingHandler((request, _) => Task.FromResult(new HttpResponseMessage(
            request.RequestUri!.AbsolutePath is "/api/health" ? healthStatus : userStatus)));

        var result = await Run(handler, $"Endpoint=https://test.service.signalr.net;AccessKey={AccessKey};Version=1.0;");

        Assert.AreEqual(expected, result.Status, result.Exception?.Message);
        if (result.Exception is not null)
        {
            Assert.DoesNotContain(AccessKey, result.Exception.Message);
        }
    }

    [TestMethod]
    public async Task Check_Should_SignTheUserLookup_ForItsOwnUrl()
    {
        HttpRequestMessage? userRequest = null;
        var handler = new ExternalSignInHealthChecksTests.RoutingHandler((request, _) =>
        {
            if (request.RequestUri!.AbsolutePath is not "/api/health")
            {
                userRequest = request;
            }
            return Task.FromResult(new HttpResponseMessage(userRequest is null ? HttpStatusCode.OK : HttpStatusCode.NotFound));
        });

        var result = await Run(handler, $"Endpoint=https://test.service.signalr.net;Port=8443;AccessKey={AccessKey};Version=1.0;");

        Assert.AreEqual(HealthStatus.Healthy, result.Status, result.Exception?.Message);
        Assert.AreEqual("https://test.service.signalr.net:8443/api/health?api-version=2024-12-01", handler.Requests[0].AbsoluteUri);

        Assert.IsNotNull(userRequest);
        Assert.AreEqual(HttpMethod.Head, userRequest.Method);
        Assert.StartsWith("https://test.service.signalr.net:8443/api/hubs/healthcheck/users/", userRequest.RequestUri!.AbsoluteUri);
        Assert.EndsWith("?api-version=2024-12-01", userRequest.RequestUri.AbsoluteUri);
        Assert.AreEqual("Bearer", userRequest.Headers.Authorization!.Scheme);

        var validation = await new JsonWebTokenHandler().ValidateTokenAsync(userRequest.Headers.Authorization.Parameter, new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidAudience = userRequest.RequestUri.GetLeftPart(UriPartial.Path),
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(AccessKey))
        });
        Assert.IsTrue(validation.IsValid, validation.Exception?.Message);
    }

    [TestMethod]
    public async Task Check_Should_OnlyAskTheHealthApi_WithoutAnAccessKey()
    {
        var handler = new ExternalSignInHealthChecksTests.RoutingHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));

        var result = await Run(handler, "Endpoint=https://test.service.signalr.net;AuthType=azure.msi;Version=1.0;");

        Assert.AreEqual(HealthStatus.Healthy, result.Status, result.Exception?.Message);
        Assert.HasCount(1, handler.Requests);
    }

    private async Task<HealthCheckResult> Run(HttpMessageHandler handler, string connectionString)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Azure:SignalR:ConnectionString"] = connectionString
        }).Build());
        services.AddSingleton(TimeProvider.System);
        services.AddHttpClient("AzureSignalR").ConfigurePrimaryHttpMessageHandler(() => handler);

        await using var serviceProvider = services.BuildServiceProvider();

        var healthCheck = ActivatorUtilities.CreateInstance<AzureSignalRHealthCheck>(serviceProvider);
        var registration = new HealthCheckRegistration("azureSignalR", healthCheck, HealthStatus.Degraded, tags: null);

        return await healthCheck.CheckHealthAsync(new HealthCheckContext { Registration = registration }, TestContext.CancellationToken);
    }
}
