//+:cnd:noEmit
using FluentEmail.Core;
using FluentEmail.Core.Models;
using Boilerplate.Shared.Features.Diagnostic;
using Microsoft.Extensions.DependencyInjection.Extensions;
//#if (notification == true)
using Boilerplate.Shared.Features.PushNotification;
//#endif

namespace Boilerplate.Tests.Features.Diagnostics;

/// <summary>
/// The test messages the health checks page sends: to the caller's email, phone number and push subscription.
/// </summary>
[TestClass, TestCategory("IntegrationTest")]
public partial class DiagnosticTestMessagesTests
{
    public TestContext TestContext { get; set; } = default!;

    [TestMethod]
    [DataRow("SendTestEmail")]
    [DataRow("SendTestSms")]
    public async Task EmailAndSms_Should_RejectAnonymousCallers(string action)
    {
        await using var server = new AppTestServer();
        await server.Build().Start(TestContext.CancellationToken);

        using var anonymousClient = server.CreateRawHttpClient();
        using var response = await anonymousClient.PostAsync($"api/v1/Diagnostic/{action}", null, TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task TestEmailAndSms_Should_NeedTheFeature_AndUseTheCallersOwnContacts()
    {
        var fluentEmail = A.Fake<IFluentEmail>();
        A.CallTo(fluentEmail).WithReturnType<IFluentEmail>().Returns(fluentEmail);
        A.CallTo(() => fluentEmail.SendAsync(A<CancellationToken?>._)).Returns(new SendResponse());

        await using var server = new AppTestServer();
        await server.Build(
            configureTestServices: services =>
            {
                services.Replace(ServiceDescriptor.Transient(_ => fluentEmail));
            }).Start(TestContext.CancellationToken);

        await using var client = server.CreateAppClient();
        var (email, userId) = await TestAccountUtils.CreateAndSignIn(client, TestContext.CancellationToken);
        var diagnosticController = client.GetController<IDiagnosticController>();

        await Assert.ThrowsExactlyAsync<ForbiddenException>(() => diagnosticController.SendTestEmail(TestContext.CancellationToken),
            "Sending needs the health checks feature.");
        A.CallTo(() => fluentEmail.SendAsync(A<CancellationToken?>._)).MustNotHaveHappened();

        await using var grant = await TestAccountUtils.MakeGlobalAdmin(client, userId, TestContext.CancellationToken);

        Assert.IsTrue(await diagnosticController.SendTestEmail(TestContext.CancellationToken));
        A.CallTo(() => fluentEmail.To(email, A<string>._)).MustHaveHappenedOnceExactly();
        A.CallTo(() => fluentEmail.SendAsync(A<CancellationToken?>._)).MustHaveHappenedOnceExactly();

        Assert.IsFalse(await diagnosticController.SendTestSms(TestContext.CancellationToken), "The account has no phone number.");
    }
    //#if (notification == true)

    [TestMethod]
    public async Task TestPushNotification_Should_ReportWhetherTheDeviceIsSubscribed_AndKeepToTheCallersOwnDevice()
    {
        await using var server = new AppTestServer();
        await server.Build().Start(TestContext.CancellationToken);

        var deviceId = $"push-test-{Guid.NewGuid():N}";

        try
        {
            await using (var ownerClient = server.CreateAppClient())
            {
                await TestAccountUtils.CreateAndSignIn(ownerClient, TestContext.CancellationToken);
                var diagnosticController = ownerClient.GetController<IDiagnosticController>();

                Assert.IsFalse(await diagnosticController.SendTestPushNotification(deviceId, TestContext.CancellationToken));

                await ownerClient.GetController<IPushNotificationController>()
                    .Subscribe(new() { DeviceId = deviceId, Platform = "fcmV1", PushChannel = "test-channel" }, TestContext.CancellationToken);

                Assert.IsTrue(await diagnosticController.SendTestPushNotification(deviceId, TestContext.CancellationToken));
            }

            await using (var otherClient = server.CreateAppClient())
            {
                await TestAccountUtils.CreateAndSignIn(otherClient, TestContext.CancellationToken);

                await Assert.ThrowsExactlyAsync<ResourceNotFoundException>(() => otherClient.GetController<IDiagnosticController>()
                    .SendTestPushNotification(deviceId, TestContext.CancellationToken), "Another session's device must not be reachable.");
            }
        }
        finally
        {
            await using var scopeApiApp = server.ApiApp.Services.CreateAsyncScope();
            await scopeApiApp.ServiceProvider.GetRequiredService<AppDbContext>().PushNotificationSubscriptions
                .Where(s => s.DeviceId == deviceId).ExecuteDeleteAsync(TestContext.CancellationToken);
        }
    }
    //#endif
}
