using Boilerplate.Server.Api;
using Boilerplate.Server.Api.Infrastructure.Services;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Diagnostics.HealthChecks;

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

    /// <summary>
    /// <c>/health</c> is the readiness contract - it runs every registered check and answers 503 as soon as one of
    /// them reports Unhealthy. The two checks that reach a remote dependency (Twilio, the profile-images object
    /// store) must therefore neither hang it nor fail it: an SMS provider incident is not a reason to pull every
    /// replica of a perfectly serving API out of the load balancer.
    /// <para>
    /// The registration's <c>Timeout</c> and <c>FailureStatus</c> are asserted together with the "live" tagging,
    /// because they are three halves of the same guarantee and each is a one-word edit away from being lost. Note
    /// <c>FailureStatus</c> only applies to a check that <em>throws</em>; a check that returns
    /// <c>HealthCheckResult.Unhealthy</c> itself overrides it, which is why the behavioural assertion below exists
    /// as well as the registration one.
    /// </para>
    /// </summary>
    [TestMethod]
    public async Task ExternalHealthChecks_Should_BeBoundedAndNonFatal()
    {
        await using var server = new AppTestServer();

        await server.Build(s => s.AddIntegrationApiOnlyTestsServices()).Start(TestContext.CancellationToken);

        var registrations = server.WebApp.Services.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value.Registrations;

        foreach (var name in new[] { "sms", "userProfileImages" })
        {
            var registration = registrations.SingleOrDefault(r => r.Name == name)
                ?? throw new InvalidOperationException($"No '{name}' health check is registered. See AddServerApiHealthChecks.");

            Assert.AreNotEqual(Timeout.InfiniteTimeSpan, registration.Timeout,
                $"The '{name}' check calls out to a third party, so it needs a timeout - without one a hung socket hangs /health with it.");

            Assert.AreEqual(HealthStatus.Degraded, registration.FailureStatus,
                $"A failing '{name}' check must not make /health answer 503 and drain the instance from rotation.");

            Assert.DoesNotContain("live", registration.Tags,
                $"The '{name}' check must stay out of /alive, which is the liveness probe and must not depend on anything remote.");
        }

        // The control: the one check that *is* meant to gate liveness still does.
        Assert.Contains("live", registrations.Single(r => r.Name == "binStorage").Tags);
    }

    /// <summary>
    /// The behavioural half of the test above: both external checks must report the status their registration asked
    /// for rather than hard-coding Unhealthy, otherwise the <c>failureStatus</c> argument is decoration.
    /// <c>TwilioHealthCheck</c> is driven with an Sms section that looks configured while <c>TwilioClient.Init</c> was
    /// never called, so the SDK throws immediately and no network request is made.
    /// </summary>
    [TestMethod]
    public async Task TwilioHealthCheck_Should_ReportTheRegisteredFailureStatus()
    {
        await using var server = new AppTestServer();

        await server.Build(s => s.AddIntegrationApiOnlyTestsServices()).Start(TestContext.CancellationToken);

        await using var scope = server.WebApp.Services.CreateAsyncScope();

        var settings = new ServerApiSettings
        {
            Sms = new SmsOptions { FromPhoneNumber = "+15550000000", TwilioAccountSid = "AC-not-a-real-sid", TwilioAutoToken = "not-a-real-token" }
        };

        var healthCheck = ActivatorUtilities.CreateInstance<TwilioHealthCheck>(scope.ServiceProvider, settings);

        var registration = new HealthCheckRegistration("sms", healthCheck, failureStatus: HealthStatus.Degraded, tags: null);

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext { Registration = registration }, TestContext.CancellationToken);

        Assert.AreEqual(HealthStatus.Degraded, result.Status,
            $"The check must honour its registration's FailureStatus. Description: '{result.Description}'.");
    }
}
