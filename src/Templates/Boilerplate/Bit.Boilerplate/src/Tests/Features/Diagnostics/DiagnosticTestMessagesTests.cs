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
        await server.Build(services => services.AddIntegrationApiOnlyTestsServices()).Start(TestContext.CancellationToken);

        using var anonymousClient = new HttpClient { BaseAddress = server.WebAppServerAddress };
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
        await server.Build(services =>
        {
            services.AddIntegrationApiOnlyTestsServices();
            services.Replace(ServiceDescriptor.Transient(_ => fluentEmail));
        }).Start(TestContext.CancellationToken);

        await using var scope = server.WebApp.Services.CreateAsyncScope();
        var (email, userId) = await TestAccountUtils.CreateAndSignIn(server, scope, TestContext.CancellationToken);
        var diagnosticController = scope.ServiceProvider.GetRequiredService<IDiagnosticController>();

        await Assert.ThrowsExactlyAsync<ForbiddenException>(() => diagnosticController.SendTestEmail(TestContext.CancellationToken),
            "Sending needs the health checks feature.");
        A.CallTo(() => fluentEmail.SendAsync(A<CancellationToken?>._)).MustNotHaveHappened();

        await using var grant = await TestAccountUtils.MakeGlobalAdmin(server, scope, userId, TestContext.CancellationToken);

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
        await server.Build(services => services.AddIntegrationApiOnlyTestsServices()).Start(TestContext.CancellationToken);

        var deviceId = $"push-test-{Guid.NewGuid():N}";

        try
        {
            await using (var ownerScope = server.WebApp.Services.CreateAsyncScope())
            {
                await TestAccountUtils.CreateAndSignIn(server, ownerScope, TestContext.CancellationToken);
                var diagnosticController = ownerScope.ServiceProvider.GetRequiredService<IDiagnosticController>();

                Assert.IsFalse(await diagnosticController.SendTestPushNotification(deviceId, TestContext.CancellationToken));

                await ownerScope.ServiceProvider.GetRequiredService<IPushNotificationController>()
                    .Subscribe(new() { DeviceId = deviceId, Platform = "fcmV1", PushChannel = "test-channel" }, TestContext.CancellationToken);

                Assert.IsTrue(await diagnosticController.SendTestPushNotification(deviceId, TestContext.CancellationToken));
            }

            await using (var otherScope = server.WebApp.Services.CreateAsyncScope())
            {
                await TestAccountUtils.CreateAndSignIn(server, otherScope, TestContext.CancellationToken);

                await Assert.ThrowsExactlyAsync<ResourceNotFoundException>(() => otherScope.ServiceProvider.GetRequiredService<IDiagnosticController>()
                    .SendTestPushNotification(deviceId, TestContext.CancellationToken), "Another session's device must not be reachable.");
            }
        }
        finally
        {
            await using var cleanupScope = server.WebApp.Services.CreateAsyncScope();
            await cleanupScope.ServiceProvider.GetRequiredService<AppDbContext>().PushNotificationSubscriptions
                .Where(s => s.DeviceId == deviceId).ExecuteDeleteAsync(TestContext.CancellationToken);
        }
    }
    //#endif
}
