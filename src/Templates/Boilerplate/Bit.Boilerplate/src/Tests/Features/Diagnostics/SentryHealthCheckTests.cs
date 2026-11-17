using Microsoft.Extensions.Diagnostics.HealthChecks;
using Boilerplate.Server.Api.Infrastructure.HealthChecks;
using Boilerplate.Tests.Features.Identity;

namespace Boilerplate.Tests.Features.Diagnostics;

[TestClass, TestCategory("UnitTest")]
public class SentryHealthCheckTests
{
    private const string Dsn = "https://0123456789abcdef0123456789abcdef@o1.ingest.de.sentry.io/42";

    public TestContext TestContext { get; set; } = default!;

    [TestMethod]
    [DataRow(HttpStatusCode.OK, null, HealthStatus.Healthy, "Sentry accepts the app's errors", DisplayName = "Accepted")]
    [DataRow(HttpStatusCode.OK, "60:transaction:organization:quota_exceeded", HealthStatus.Healthy, "Sentry accepts the app's errors", DisplayName = "Only transactions limited")]
    [DataRow(HttpStatusCode.OK, "60:error;transaction:organization:quota_exceeded", HealthStatus.Degraded, "Sentry drops the app's errors for 60 seconds (quota_exceeded)", DisplayName = "Errors over quota")]
    [DataRow(HttpStatusCode.OK, "30::key:", HealthStatus.Degraded, "Sentry drops the app's errors for 30 seconds (rate limited)", DisplayName = "Every category limited")]
    [DataRow(HttpStatusCode.Forbidden, null, HealthStatus.Degraded, "Sentry answered 403 Forbidden: event submission rejected with_reason: ProjectId", DisplayName = "Project rejected")]
    [DataRow(HttpStatusCode.TooManyRequests, "60::organization:quota_exceeded", HealthStatus.Degraded, "Sentry drops the app's errors for 60 seconds (quota_exceeded)", DisplayName = "Throttled")]
    [DataRow(HttpStatusCode.ServiceUnavailable, null, HealthStatus.Degraded, "Sentry answered 503 ServiceUnavailable: event submission rejected with_reason: ProjectId", DisplayName = "Sentry down")]
    public async Task Check_Should_JudgeSentrysAnswer(HttpStatusCode status, string? rateLimits, HealthStatus expected, string description)
    {
        var handler = new ExternalSignInHealthChecksTests.RoutingHandler((_, _) =>
        {
            var response = new HttpResponseMessage(status) { Content = new StringContent(status is HttpStatusCode.OK ? "{}" : """{"detail":"event submission rejected with_reason: ProjectId"}""") };
            if (rateLimits is not null)
            {
                response.Headers.Add("X-Sentry-Rate-Limits", rateLimits);
            }
            return Task.FromResult(response);
        });

        var result = await Run(handler, Dsn);

        Assert.AreEqual(expected, result.Status, result.Description);
        Assert.AreEqual(description, result.Description);
    }

    [TestMethod]
    public async Task Check_Should_LeaveTheReasonOut_WhenTheBodyHasNone()
    {
        var handler = new ExternalSignInHealthChecksTests.RoutingHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadGateway) { Content = new StringContent("<html>Bad gateway</html>") }));

        var result = await Run(handler, Dsn);

        Assert.AreEqual(HealthStatus.Degraded, result.Status);
        Assert.AreEqual("Sentry answered 502 BadGateway", result.Description);
    }

    [TestMethod]
    public async Task Check_Should_PostAClientReport_ToTheDsnsEnvelopeEndpoint()
    {
        HttpRequestMessage? sent = null;
        string? body = null;
        var handler = new ExternalSignInHealthChecksTests.RoutingHandler(async (request, cancellationToken) =>
        {
            sent = request;
            body = await request.Content!.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{}") };
        });

        var result = await Run(handler, "https://abc:legacy-secret@sentry.example.com/sentry/42/");

        Assert.AreEqual(HealthStatus.Healthy, result.Status, result.Description);
        Assert.AreEqual(HttpMethod.Post, sent!.Method);
        Assert.AreEqual("https://sentry.example.com/sentry/api/42/envelope/?sentry_key=abc&sentry_version=7", sent.RequestUri!.AbsoluteUri);
        Assert.AreEqual("application/x-sentry-envelope", sent.Content!.Headers.ContentType!.MediaType);

        var lines = body!.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.HasCount(3, lines);
        Assert.AreEqual("""{"type":"client_report"}""", lines[1], "A client report creates no event and counts against no quota.");
        Assert.Contains("\"discarded_events\":[]", lines[2]);
    }

    [TestMethod]
    public async Task Check_Should_ReportDegraded_WhenSentryIsUnreachable()
    {
        var handler = new ExternalSignInHealthChecksTests.RoutingHandler((_, _) => throw new HttpRequestException("No such host is known."));

        var result = await Run(handler, Dsn);

        Assert.AreEqual(HealthStatus.Degraded, result.Status);
        Assert.AreEqual("Sentry is unreachable", result.Description);
    }

    [TestMethod]
    [DataRow("not a url")]
    [DataRow("ftp://key@sentry.example.com/42")]
    [DataRow("https://sentry.example.com/42")]
    [DataRow("https://key@sentry.example.com/")]
    public async Task Check_Should_SendNothing_ForAnInvalidDsn(string dsn)
    {
        var handler = new ExternalSignInHealthChecksTests.RoutingHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));

        var result = await Run(handler, dsn);

        Assert.AreEqual(HealthStatus.Degraded, result.Status);
        Assert.AreEqual("Sentry:Dsn is not a Sentry DSN", result.Description);
        Assert.IsEmpty(handler.Requests);
    }

    private async Task<HealthCheckResult> Run(HttpMessageHandler handler, string dsn)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Sentry:Dsn"] = dsn
        }).Build());
        services.AddSingleton(TimeProvider.System);
        services.AddHttpClient("Sentry").ConfigurePrimaryHttpMessageHandler(() => handler);

        await using var serviceProvider = services.BuildServiceProvider();

        var healthCheck = ActivatorUtilities.CreateInstance<SentryHealthCheck>(serviceProvider);
        var registration = new HealthCheckRegistration("sentry", healthCheck, HealthStatus.Degraded, tags: null);

        return await healthCheck.CheckHealthAsync(new HealthCheckContext { Registration = registration }, TestContext.CancellationToken);
    }
}
