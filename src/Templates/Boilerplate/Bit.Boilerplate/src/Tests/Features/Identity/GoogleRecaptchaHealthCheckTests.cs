using System.Text;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Boilerplate.Server.Api.Features.Identity.Services;
using Boilerplate.Server.Api.Features.Identity.HealthChecks;

namespace Boilerplate.Tests.Features.Identity;

/// <summary>
/// Google checks the response before the secret, so reCaptcha only proves siteverify answers as expected.
/// </summary>
[TestClass, TestCategory("IntegrationTest")]
public class GoogleRecaptchaHealthCheckTests
{
    public TestContext TestContext { get; set; } = default!;

    [TestMethod]
    [DataRow(HttpStatusCode.OK, """{"success":false,"error-codes":["invalid-input-response"]}""", HealthStatus.Healthy, DisplayName = "siteverify answers")]
    [DataRow(HttpStatusCode.OK, """{"success":false,"error-codes":["invalid-input-secret"]}""", HealthStatus.Degraded, DisplayName = "Unexpected error")]
    [DataRow(HttpStatusCode.OK, """{"success":false}""", HealthStatus.Degraded, DisplayName = "No error codes")]
    [DataRow(HttpStatusCode.BadRequest, "", HealthStatus.Degraded, DisplayName = "Refused")] // Not a transient status, so the resilience handler does not retry it.
    public async Task Check_Should_JudgeSiteverifysAnswer(HttpStatusCode status, string body, HealthStatus expected)
    {
        var handler = new ExternalSignInHealthChecksTests.RoutingHandler((_, _) => Task.FromResult(new HttpResponseMessage(status)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        }));

        await using var server = new AppTestServer();

        await server.Build(services =>
        {
            services.AddIntegrationApiOnlyTestsServices();
            services.AddHttpClient<GoogleRecaptchaService>().ConfigurePrimaryHttpMessageHandler(() => handler);
        }).Start(TestContext.CancellationToken);

        var report = await server.WebApp.Services.GetRequiredService<HealthCheckService>()
            .CheckHealthAsync(r => r.Name is "reCaptcha", TestContext.CancellationToken);

        Assert.AreEqual(expected, report.Entries["reCaptcha"].Status, report.Entries["reCaptcha"].Exception?.Message);
        Assert.AreEqual("https://www.google.com/recaptcha/api/siteverify", handler.Requests.Distinct().Single().AbsoluteUri);
    }
}
