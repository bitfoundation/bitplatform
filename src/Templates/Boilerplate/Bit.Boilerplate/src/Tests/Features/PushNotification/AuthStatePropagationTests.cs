using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Boilerplate.Client.Core.Components;

namespace Boilerplate.Tests.Features.PushNotification;

[TestClass, TestCategory("UITest")]
public partial class AuthStatePropagationTests
{
    public Microsoft.VisualStudio.TestTools.UnitTesting.TestContext TestContext { get; set; } = default!;

    [TestMethod]
    public async Task ASignOut_Should_Propagate_AfterASignInWhosePushSubscriptionWasRateLimited()
    {
        var userId = Guid.NewGuid();
        var subscribeCalls = 0;

        var pushNotificationService = A.Fake<IPushNotificationService>();
        A.CallTo(() => pushNotificationService.Subscribe(A<CancellationToken>._))
            .ReturnsLazily(() => ++subscribeCalls is 2 ? Task.FromException(new TooManyRequestsException()) : Task.CompletedTask);

        await using var server = new AppTestServer();
        await server.Build().Start(TestContext.CancellationToken);

        await using var ctx = server.CreateBunitContext();

        ctx.Services.AddScoped(_ => pushNotificationService);
        ctx.Services.AddScoped(_ => A.Fake<IUserController>());
        ctx.SetRendererInfo(new RendererInfo("Static", isInteractive: false));

        var cut = ctx.Render<AppClientCoordinator>();
        var telemetryContext = ctx.Services.GetRequiredService<ITelemetryContext>();

        Task Propagate(ClaimsPrincipal user, bool firstRun = false) =>
            cut.InvokeAsync(() => cut.Instance.PropagateAuthState(firstRun, Task.FromResult(new AuthenticationState(user))));

        await Propagate(IAuthTokenProvider.Anonymous(), firstRun: true);

        await Propagate(new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, userId.ToString())], "Bearer")));

        Assert.AreEqual(2, subscribeCalls, "The sign-in did not try to subscribe, so the rest of this test cannot mean anything.");
        Assert.AreEqual(userId, telemetryContext.UserId);

        await Propagate(IAuthTokenProvider.Anonymous());

        Assert.AreEqual(3, subscribeCalls, "The sign-out was skipped, so the push subscription was not moved to the anonymous state.");
        Assert.IsNull(telemetryContext.UserId, "The sign-out was skipped, so the telemetry still carries the signed out user.");
    }
}
