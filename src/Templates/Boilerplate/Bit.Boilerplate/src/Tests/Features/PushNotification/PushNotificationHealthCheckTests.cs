using System.Buffers.Text;
using System.Security.Cryptography;
using AdsPush;
using AdsPush.Abstraction;
using AdsPush.Abstraction.Settings;
using Boilerplate.Server.Api.Features.PushNotification;
using Boilerplate.Tests.Features.Identity;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Boilerplate.Tests.Features.PushNotification;

/// <summary>
/// A token error is healthy; which errors count differs per provider (APNs also reports a wrong bundle id as InvalidArgument).
/// </summary>
[TestClass, TestCategory("UnitTest")]
public class PushNotificationHealthCheckTests
{
    public TestContext TestContext { get; set; } = default!;

    [TestMethod]
    [DataRow(AdsPushTarget.Ios, AdsPushErrorType.InvalidToken, HealthStatus.Healthy)]
    [DataRow(AdsPushTarget.Ios, AdsPushErrorType.InvalidArgument, HealthStatus.Degraded)]
    [DataRow(AdsPushTarget.Ios, AdsPushErrorType.InvalidAuthConfiguration, HealthStatus.Degraded)]
    [DataRow(AdsPushTarget.Android, AdsPushErrorType.InvalidToken, HealthStatus.Healthy)]
    [DataRow(AdsPushTarget.Android, AdsPushErrorType.InvalidArgument, HealthStatus.Healthy)]
    [DataRow(AdsPushTarget.Android, AdsPushErrorType.InvalidAuthConfiguration, HealthStatus.Degraded)]
    [DataRow(AdsPushTarget.Android, AdsPushErrorType.ServiceUnavailable, HealthStatus.Degraded)]
    public async Task Check_Should_TreatOnlyTokenErrorsAsHealthy(AdsPushTarget target, AdsPushErrorType errorType, HealthStatus expectedStatus)
    {
        var adsPushSender = A.Fake<IAdsPushSender>();
        A.CallTo(() => adsPushSender.BasicSendAsync(target, A<string>._, A<AdsPushBasicSendPayload>._, A<CancellationToken>._))
            .ThrowsAsync(new AdsPushException(errorType.ToString(), errorType, null!));

        var result = await Check(adsPushSender, target);

        Assert.AreEqual(expectedStatus, result.Status, result.Exception?.Message);
    }

    /// <summary>
    /// The sandbox answers a TestFlight or App Store install's token with BadDeviceToken, which the check would pass.
    /// </summary>
    [TestMethod]
    public async Task ApnsCheck_Should_ReportDegraded_OnTheSandboxOutsideDevelopment()
    {
        var adsPushSender = A.Fake<IAdsPushSender>();

        var result = await Check(adsPushSender, AdsPushTarget.Ios, apnsSandboxOutsideDevelopment: true);

        Assert.AreEqual(HealthStatus.Degraded, result.Status);
        Assert.Contains("EnvironmentType", result.Description!);
        A.CallTo(adsPushSender).MustNotHaveHappened();
    }

    [TestMethod]
    public async Task Check_Should_ReportTheRegisteredFailureStatus_OnAnUnexpectedException()
    {
        var adsPushSender = A.Fake<IAdsPushSender>();
        A.CallTo(() => adsPushSender.BasicSendAsync(A<AdsPushTarget>._, A<string>._, A<AdsPushBasicSendPayload>._, A<CancellationToken>._))
            .ThrowsAsync(new HttpRequestException("No route to host"));

        var result = await Check(adsPushSender, AdsPushTarget.Ios);

        Assert.AreEqual(HealthStatus.Degraded, result.Status);
    }

    [TestMethod]
    [DataRow(HttpStatusCode.Gone, "push subscription has unsubscribed or expired.", HealthStatus.Healthy, "FCM accepts the VAPID signature", DisplayName = "Signature accepted")]
    [DataRow(HttpStatusCode.NotFound, "", HealthStatus.Healthy, "FCM accepts the VAPID signature", DisplayName = "Unknown subscription")]
    [DataRow(HttpStatusCode.Forbidden, "permission denied: invalid JWT provided", HealthStatus.Degraded, "FCM answered 403 Forbidden: permission denied: invalid JWT provided", DisplayName = "Keys of different pairs")]
    [DataRow(HttpStatusCode.ServiceUnavailable, "<html>Unavailable</html>", HealthStatus.Degraded, "FCM answered 503 ServiceUnavailable", DisplayName = "Outage")]
    public async Task WebPushCheck_Should_JudgeFcmsAnswer(HttpStatusCode status, string body, HealthStatus expected, string description)
    {
        string? authorization = null;
        var handler = new ExternalSignInHealthChecksTests.RoutingHandler((request, _) =>
        {
            authorization = request.Headers.Authorization?.ToString();
            return Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(body) });
        });

        var result = await CheckWebPush(handler, "mailto:ops@example.com");

        Assert.AreEqual(expected, result.Status, result.Description);
        Assert.AreEqual(description, result.Description);
        Assert.AreEqual("fcm.googleapis.com", handler.Requests.Single().Host);
        Assert.StartsWith("WebPush ", authorization!, "The push is signed with the VAPID private key.");
    }

    [TestMethod]
    public async Task WebPushCheck_Should_SendNothing_WithASubjectThatIsNoAddress()
    {
        var handler = new ExternalSignInHealthChecksTests.RoutingHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.Gone)));

        var result = await CheckWebPush(handler, "https://");

        Assert.AreEqual(HealthStatus.Degraded, result.Status);
        Assert.AreEqual("AdsPushVapid is invalid: Subject is not a valid URL or mailto address", result.Description);
        Assert.IsEmpty(handler.Requests);
    }

    [TestMethod]
    public async Task WebPushCheck_Should_ReportDegraded_WhenFcmIsUnreachable()
    {
        var handler = new ExternalSignInHealthChecksTests.RoutingHandler((_, _) => throw new HttpRequestException("No route to host"));

        var result = await CheckWebPush(handler, "mailto:ops@example.com");

        Assert.AreEqual(HealthStatus.Degraded, result.Status);
        Assert.AreEqual("FCM is unreachable", result.Description);
    }

    private async Task<HealthCheckResult> CheckWebPush(HttpMessageHandler handler, string subject)
    {
        using var vapidKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var keyParameters = vapidKey.ExportParameters(includePrivateParameters: true);
        using var httpClient = new HttpClient(handler);

        var services = new ServiceCollection();
        services.AddSingleton(new AdsPushSenderBuilder().ConfigureVapid(new AdsPushVapidSettings
        {
            PublicKey = Base64Url.EncodeToString([0x04, .. keyParameters.Q.X!, .. keyParameters.Q.Y!]),
            PrivateKey = Base64Url.EncodeToString(keyParameters.D!),
            Subject = subject
        }, httpClient).BuildSender());

        await using var serviceProvider = services.BuildServiceProvider();

        var healthCheck = ActivatorUtilities.CreateInstance<WebPushHealthCheck>(serviceProvider);
        var registration = new HealthCheckRegistration("webPush", healthCheck, failureStatus: HealthStatus.Degraded, tags: null);

        return await healthCheck.CheckHealthAsync(new HealthCheckContext { Registration = registration }, TestContext.CancellationToken);
    }

    private async Task<HealthCheckResult> Check(IAdsPushSender adsPushSender, AdsPushTarget target, bool apnsSandboxOutsideDevelopment = false)
    {
        var healthCheck = new PushNotificationHealthCheck(adsPushSender, target, apnsSandboxOutsideDevelopment);
        var registration = new HealthCheckRegistration(target is AdsPushTarget.Ios ? "apns" : "firebase", healthCheck, failureStatus: HealthStatus.Degraded, tags: null);

        return await healthCheck.CheckHealthAsync(new HealthCheckContext { Registration = registration }, TestContext.CancellationToken);
    }
}
