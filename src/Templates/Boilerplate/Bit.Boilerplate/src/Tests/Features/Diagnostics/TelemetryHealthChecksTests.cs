using System.Text;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Boilerplate.Server.Api;
using Boilerplate.Server.Api.Infrastructure.DevMcp;
using Boilerplate.Server.Api.Infrastructure.HealthChecks;
using Boilerplate.Tests.Features.Identity;

namespace Boilerplate.Tests.Features.Diagnostics;

[TestClass, TestCategory("UnitTest")]
public class TelemetryHealthChecksTests
{
    public TestContext TestContext { get; set; } = default!;

    [TestMethod]
    [DataRow("http/protobuf", "http://collector:4318/v1/logs", "application/x-protobuf", 0, DisplayName = "HTTP protobuf")]
    [DataRow("http/json", "http://collector:4318/v1/logs", "application/json", 2, DisplayName = "HTTP JSON")]
    [DataRow("grpc", "http://collector:4318/opentelemetry.proto.collector.logs.v1.LogsService/Export", "application/grpc", 5, DisplayName = "gRPC")]
    public async Task OtlpCheck_Should_SendAnEmptyExport_WithTheConfiguredProtocolAndHeaders(string protocol, string url, string contentType, int bodyLength)
    {
        HttpRequestMessage? sent = null;
        byte[] body = [];
        var handler = new ExternalSignInHealthChecksTests.RoutingHandler(async (request, cancellationToken) =>
        {
            sent = request;
            body = await request.Content!.ReadAsByteArrayAsync(cancellationToken);
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            if (protocol is "grpc")
            {
                response.TrailingHeaders.Add("grpc-status", "0");
            }
            return response;
        });

        var result = await RunOtlpCheck(handler, new()
        {
            ["OTEL_EXPORTER_OTLP_ENDPOINT"] = "http://collector:4318/",
            ["OTEL_EXPORTER_OTLP_PROTOCOL"] = protocol,
            ["OTEL_EXPORTER_OTLP_HEADERS"] = "x-otlp-api-key=key%3D1,x-tenant = blue"
        });

        Assert.AreEqual(HealthStatus.Healthy, result.Status, result.Description);
        Assert.AreEqual(url, sent!.RequestUri!.AbsoluteUri);
        Assert.AreEqual(contentType, sent.Content!.Headers.ContentType!.MediaType);
        Assert.HasCount(bodyLength, body);
        Assert.IsTrue(body.All(b => b is 0 or (byte)'{' or (byte)'}'), "The export carries no telemetry.");
        Assert.AreEqual("key=1", sent.Headers.GetValues("x-otlp-api-key").Single());
        Assert.AreEqual("blue", sent.Headers.GetValues("x-tenant").Single());
        if (protocol is "grpc")
        {
            Assert.AreEqual(HttpVersion.Version20, sent.Version);
            Assert.AreEqual(HttpVersionPolicy.RequestVersionExact, sent.VersionPolicy, "Without it HttpClient falls back to HTTP/1.1, which gRPC can't use.");
        }
    }

    [TestMethod]
    [DataRow(HttpStatusCode.Unauthorized, null, "The OTLP endpoint answered 401 Unauthorized", DisplayName = "Wrong key over HTTP")]
    [DataRow(HttpStatusCode.ServiceUnavailable, null, "The OTLP endpoint answered 503 ServiceUnavailable", DisplayName = "Collector down")]
    [DataRow(HttpStatusCode.OK, "16", "The OTLP endpoint answered grpc-status 16 (Unauthenticated key)", DisplayName = "Wrong key over gRPC")]
    public async Task OtlpCheck_Should_ReportWhatTheEndpointRefused(HttpStatusCode status, string? grpcStatus, string description)
    {
        var handler = new ExternalSignInHealthChecksTests.RoutingHandler((_, _) =>
        {
            var response = new HttpResponseMessage(status);
            if (grpcStatus is not null)
            {
                response.Headers.Add("grpc-status", grpcStatus);
                response.Headers.Add("grpc-message", "Unauthenticated%20key");
            }
            return Task.FromResult(response);
        });

        var result = await RunOtlpCheck(handler, new()
        {
            ["OTEL_EXPORTER_OTLP_ENDPOINT"] = "http://collector:4317",
            ["OTEL_EXPORTER_OTLP_PROTOCOL"] = grpcStatus is null ? "http/protobuf" : "grpc"
        });

        Assert.AreEqual(HealthStatus.Degraded, result.Status);
        Assert.AreEqual(description, result.Description);
    }

    [TestMethod]
    public async Task OtlpCheck_Should_PostToBelemetrysEndpoint_WhenTheServersHaveNoneOfTheirOwn()
    {
        var handler = new ExternalSignInHealthChecksTests.RoutingHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));

        var result = await RunOtlpCheck(handler, new()
        {
            ["Belemetry:OtlpEndpoint"] = "https://www.example.com",
            ["OTEL_EXPORTER_OTLP_PROTOCOL"] = "grpc"
        });

        Assert.AreEqual(HealthStatus.Healthy, result.Status, result.Description);
        Assert.AreEqual("https://www.example.com/v1/logs", handler.Requests.Single().AbsoluteUri, "Belemetry posts there over HTTP protobuf, whatever OTEL_EXPORTER_OTLP_PROTOCOL says.");
    }

    [TestMethod]
    [DataRow(null, "http/protobuf", "http://logs:4318/custom/logs", "http://logs:4318/custom/logs", DisplayName = "Logs endpoint only")]
    [DataRow("http://collector:4318", "http/json", "http://logs:4318/custom/logs", "http://logs:4318/custom/logs", DisplayName = "Logs endpoint wins")]
    [DataRow(null, "grpc", "http://logs:4317", "http://logs:4317/opentelemetry.proto.collector.logs.v1.LogsService/Export", DisplayName = "Logs endpoint over gRPC")]
    public async Task OtlpCheck_Should_PostToTheLogsEndpoint_WhenOneIsSet(string? endpoint, string protocol, string logsEndpoint, string url)
    {
        var handler = new ExternalSignInHealthChecksTests.RoutingHandler((_, _) =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.TrailingHeaders.Add("grpc-status", "0");
            return Task.FromResult(response);
        });

        var result = await RunOtlpCheck(handler, new()
        {
            ["OTEL_EXPORTER_OTLP_ENDPOINT"] = endpoint,
            ["OTEL_EXPORTER_OTLP_LOGS_ENDPOINT"] = logsEndpoint,
            ["OTEL_EXPORTER_OTLP_PROTOCOL"] = protocol
        });

        Assert.AreEqual(HealthStatus.Healthy, result.Status, result.Description);
        Assert.AreEqual(url, handler.Requests.Single().AbsoluteUri);
    }

    [TestMethod]
    public async Task OtlpCheck_Should_ReportDegraded_WhenAGrpcAnswerHasNoStatus()
    {
        var handler = new ExternalSignInHealthChecksTests.RoutingHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("<html>Welcome</html>", Encoding.UTF8, "text/html")
        }));

        var result = await RunOtlpCheck(handler, new()
        {
            ["OTEL_EXPORTER_OTLP_ENDPOINT"] = "http://collector:4317",
            ["OTEL_EXPORTER_OTLP_PROTOCOL"] = "grpc"
        });

        Assert.AreEqual(HealthStatus.Degraded, result.Status);
        Assert.AreEqual("The OTLP endpoint answered without a grpc-status", result.Description);
    }

    [TestMethod]
    public async Task OtlpCheck_Should_ReportDegraded_WhenTheEndpointIsUnreachable()
    {
        var handler = new ExternalSignInHealthChecksTests.RoutingHandler((_, _) => throw new HttpRequestException("Connection refused"));

        var result = await RunOtlpCheck(handler, new() { ["OTEL_EXPORTER_OTLP_ENDPOINT"] = "http://collector:4318" });

        Assert.AreEqual(HealthStatus.Degraded, result.Status);
        Assert.AreEqual("The OTLP endpoint is unreachable", result.Description);
    }

    [TestMethod]
    [DataRow(HttpStatusCode.OK, """[{"name":"Boilerplate.Server.Api"},{"name":"Boilerplate.Server.Web"}]""", HealthStatus.Healthy, "The Aspire dashboard accepts the key and keeps telemetry of 2 resources", DisplayName = "Key accepted")]
    [DataRow(HttpStatusCode.Unauthorized, "", HealthStatus.Degraded, "The Aspire dashboard refused the key in AspireDashboard:ApiKey, which has to match its Dashboard:Api:PrimaryApiKey.", DisplayName = "Wrong key")]
    [DataRow(HttpStatusCode.NotFound, "", HealthStatus.Degraded, "The Aspire dashboard at AspireDashboard:Url has no telemetry API", DisplayName = "API turned off")]
    [DataRow(HttpStatusCode.BadGateway, "", HealthStatus.Degraded, "The Aspire dashboard is unreachable", DisplayName = "Dashboard down")]
    public async Task AspireDashboardCheck_Should_JudgeTheTelemetryApisAnswer(HttpStatusCode status, string body, HealthStatus expected, string description)
    {
        HttpRequestMessage? sent = null;
        var handler = new ExternalSignInHealthChecksTests.RoutingHandler((request, _) =>
        {
            sent = request;
            return Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") });
        });

        var services = new ServiceCollection();
        services.AddSingleton(new ServerApiSettings { AspireDashboard = new() { Url = new Uri("http://dashboard:18888"), ApiKey = "test-key" } });
        services.AddHttpClient<DevMcpTelemetryReader>(httpClient =>
        {
            httpClient.BaseAddress = new Uri("http://dashboard:18888/");
            httpClient.DefaultRequestHeaders.Add("X-API-Key", "test-key");
        }).ConfigurePrimaryHttpMessageHandler(() => handler);

        await using var serviceProvider = services.BuildServiceProvider();

        var result = await Run(ActivatorUtilities.CreateInstance<AspireDashboardHealthCheck>(serviceProvider));

        Assert.AreEqual(expected, result.Status, result.Description);
        Assert.AreEqual(description, result.Description);
        Assert.AreEqual("http://dashboard:18888/api/telemetry/resources", sent!.RequestUri!.AbsoluteUri);
        Assert.AreEqual("test-key", sent.Headers.GetValues("X-API-Key").Single());
    }

    private async Task<HealthCheckResult> RunOtlpCheck(HttpMessageHandler handler, Dictionary<string, string?> settings)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection(settings).Build());
        services.AddHttpClient("Otlp").ConfigurePrimaryHttpMessageHandler(() => handler);

        await using var serviceProvider = services.BuildServiceProvider();

        return await Run(ActivatorUtilities.CreateInstance<OtlpHealthCheck>(serviceProvider));
    }

    private async Task<HealthCheckResult> Run(IHealthCheck healthCheck)
    {
        var registration = new HealthCheckRegistration("test", healthCheck, HealthStatus.Degraded, tags: null);

        return await healthCheck.CheckHealthAsync(new HealthCheckContext { Registration = registration }, TestContext.CancellationToken);
    }
}
