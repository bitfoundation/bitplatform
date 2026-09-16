using AdsPush;
using AdsPush.Abstraction;
using Boilerplate.Server.Api.Features.PushNotification;
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

    [TestMethod]
    public async Task Check_Should_ReportTheRegisteredFailureStatus_OnAnUnexpectedException()
    {
        var adsPushSender = A.Fake<IAdsPushSender>();
        A.CallTo(() => adsPushSender.BasicSendAsync(A<AdsPushTarget>._, A<string>._, A<AdsPushBasicSendPayload>._, A<CancellationToken>._))
            .ThrowsAsync(new HttpRequestException("No route to host"));

        var result = await Check(adsPushSender, AdsPushTarget.Ios);

        Assert.AreEqual(HealthStatus.Degraded, result.Status);
    }

    private async Task<HealthCheckResult> Check(IAdsPushSender adsPushSender, AdsPushTarget target)
    {
        var healthCheck = new PushNotificationHealthCheck(adsPushSender, target);
        var registration = new HealthCheckRegistration(target is AdsPushTarget.Ios ? "apns" : "firebase", healthCheck, failureStatus: HealthStatus.Degraded, tags: null);

        return await healthCheck.CheckHealthAsync(new HealthCheckContext { Registration = registration }, TestContext.CancellationToken);
    }
}
