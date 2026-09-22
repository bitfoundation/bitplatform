//+:cnd:noEmit
using Microsoft.AspNetCore.Identity;
//#if (advancedTests == true)
using Boilerplate.Tests.Features.DevMcp;
//#endif

namespace Boilerplate.Tests.Features.Identity;

/// <summary>
/// <c>UserSession.Trusted</c> decides whether a secret may be pushed to a device, so what writes it is as much a
/// security boundary as the policies that read it: the elevated-access code, the second factor and the password-reset
/// token all push one. It is per-SESSION, which is the point - the account-level answer only looks the same because
/// enabling 2fa rotates the security stamp.
/// </summary>
[TestClass, TestCategory("IntegrationTest")]
public class TrustedSessionTests
{
    /// <summary>
    /// A mailed code proves the inbox, which is where the next code goes anyway - so the push would add no proof.
    /// </summary>
    [TestMethod]
    public async Task ASessionOpenedWithAnEmailedCode_Should_NotBeTrusted()
    {
        await using var server = await StartServer();
        await using var scope = server.WebApp.Services.CreateAsyncScope();
        await TestAccountUtils.CreateAndSignIn(server, scope, TestContext.CancellationToken);

        var session = await ReadCurrentSession(server, scope);

        Assert.AreEqual("Email", session.AuthenticationMethod,
            "The magic-link sign-in confirms the account through an e-mailed code, which is what the column has to record.");

        Assert.IsFalse(session.Trusted,
            "A code sent to the account's own e-mail is not a second factor, so the session it opens must not be trusted " +
            "with the next one.");
    }

    /// <summary>
    /// The positive case. The sign-in really runs the authenticator: flipping <c>TwoFactorEnabled</c> would prove
    /// nothing, since the column records what this sign-in did, not what the account demands.
    /// </summary>
    [TestMethod]
    public async Task ASessionThatCompletedTheSecondFactor_Should_BeTrusted()
    {
        await using var server = await StartServer();
        await using var scope = server.WebApp.Services.CreateAsyncScope();
        var (email, userId) = await TestAccountUtils.CreateAndSignIn(server, scope, TestContext.CancellationToken);

        await DevMcpTestUtils.EnableTwoFactorAndSignInWithIt(server, scope, email, userId, TestContext.CancellationToken);

        var session = await ReadCurrentSession(server, scope);

        Assert.AreEqual("Password", session.AuthenticationMethod,
            "The first step was the password; the second factor is recorded by Trusted, not by overwriting the method.");

        Assert.IsTrue(session.Trusted, "A sign-in that passed the authenticator is the case this column exists for.");
    }

    /// <summary>
    /// The untrusted session an account starts with must not survive as a device the next code could be pushed to.
    /// </summary>
    [TestMethod]
    public async Task EnablingTwoFactor_Should_LeaveNoUntrustedSessionBehind()
    {
        await using var server = await StartServer();
        await using var scope = server.WebApp.Services.CreateAsyncScope();
        var (email, userId) = await TestAccountUtils.CreateAndSignIn(server, scope, TestContext.CancellationToken);

        var beforeTwoFactor = await ReadCurrentSession(server, scope);

        await DevMcpTestUtils.EnableTwoFactorAndSignInWithIt(server, scope, email, userId, TestContext.CancellationToken);

        // The stamp check runs on refresh, so the row survives until that session asks for a token. What matters is that
        // it can never receive anything: the push filters on Trusted, and this one is false.
        Assert.IsFalse(beforeTwoFactor.Trusted,
            "The pre-2fa session was opened with an e-mailed code. Were it trusted, turning 2fa on would leave a device " +
            "behind that still receives codes with no second factor of its own.");
    }

    /// <summary>
    /// With no trusted session there is nowhere to push, and claiming one sends the user to a device never notified.
    /// </summary>
    [TestMethod]
    public async Task SendElevatedAccessToken_Should_NotClaimOtherDevices_WhenNoSessionIsTrusted()
    {
        await using var server = await StartServer();
        await using var scope = server.WebApp.Services.CreateAsyncScope();
        await TestAccountUtils.CreateAndSignIn(server, scope, TestContext.CancellationToken);

        var sentTo = await scope.ServiceProvider.GetRequiredService<IUserController>()
                                                .SendElevatedAccessToken(TestContext.CancellationToken);

        Assert.IsFalse(sentTo.SentToOtherDevices,
            "This account's only session was opened with an e-mailed code, so there is no trusted device to push to.");

        Assert.IsTrue(sentTo.SentToEmail,
            "The e-mail is still confirmed, so the code did go somewhere - the push is what was skipped, not the send.");
    }

    /// <summary>
    /// And the positive. The current session is excluded from the push - a code is useless on the device that asked
    /// for it - which is why this needs two two-factor sign-ins rather than one.
    /// </summary>
    [TestMethod]
    public async Task SendElevatedAccessToken_Should_ClaimOtherDevices_WhenATrustedOneExists()
    {
        await using var server = await StartServer();
        await using var scope = server.WebApp.Services.CreateAsyncScope();
        var (email, userId) = await TestAccountUtils.CreateAndSignIn(server, scope, TestContext.CancellationToken);

        await DevMcpTestUtils.EnableTwoFactorAndSignInWithIt(server, scope, email, userId, TestContext.CancellationToken);
        var firstTrustedSession = await ReadCurrentSession(server, scope);

        await SetReachability(server, firstTrustedSession.Id, notificationsAllowed: true, connected: true);

        await SignInWithTwoFactorAgain(server, scope, email, userId);
        var secondTrustedSession = await ReadCurrentSession(server, scope);

        Assert.AreNotEqual(firstTrustedSession.Id, secondTrustedSession.Id,
            "The second sign-in has to be its own session, otherwise there is no other device in play.");
        Assert.IsTrue(secondTrustedSession.Trusted);

        var sentTo = await scope.ServiceProvider.GetRequiredService<IUserController>()
                                                .SendElevatedAccessToken(TestContext.CancellationToken);

        Assert.IsTrue(sentTo.SentToOtherDevices,
            "The account holds another session that passed the second factor, which is exactly what may be pushed to.");
    }

    /// <summary>
    /// Permission is not delivery: a trusted session that opted in but holds no hub connection and no push
    /// subscription receives nothing, so the answer must not name it.
    /// </summary>
    [TestMethod]
    public async Task SendElevatedAccessToken_Should_NotClaimOtherDevices_WhenTheTrustedOneIsOffline()
    {
        await using var server = await StartServer();
        await using var scope = server.WebApp.Services.CreateAsyncScope();
        var (email, userId) = await TestAccountUtils.CreateAndSignIn(server, scope, TestContext.CancellationToken);

        await DevMcpTestUtils.EnableTwoFactorAndSignInWithIt(server, scope, email, userId, TestContext.CancellationToken);
        var offlineSession = await ReadCurrentSession(server, scope);

        Assert.IsTrue(offlineSession.Trusted, "The setup depends on this session being trusted; only its reachability is in question.");

        await SetReachability(server, offlineSession.Id, notificationsAllowed: true, connected: false);

        await SignInWithTwoFactorAgain(server, scope, email, userId);

        var sentTo = await scope.ServiceProvider.GetRequiredService<IUserController>()
                                                .SendElevatedAccessToken(TestContext.CancellationToken);

        Assert.IsFalse(sentTo.SentToOtherDevices,
            "The device allowed notifications and then went away - no hub connection, no subscription. Reading the " +
            "opt-in as a delivery is what sends the user to watch a phone that never rings.");
    }

    /// <summary>
    /// Trusted is necessary but not sufficient: both delivery paths also require notifications to be allowed.
    /// </summary>
    [TestMethod]
    public async Task SendElevatedAccessToken_Should_NotClaimOtherDevices_WhenTheTrustedOneIsMuted()
    {
        await using var server = await StartServer();
        await using var scope = server.WebApp.Services.CreateAsyncScope();
        var (email, userId) = await TestAccountUtils.CreateAndSignIn(server, scope, TestContext.CancellationToken);

        await DevMcpTestUtils.EnableTwoFactorAndSignInWithIt(server, scope, email, userId, TestContext.CancellationToken);
        var mutedSession = await ReadCurrentSession(server, scope);

        Assert.IsTrue(mutedSession.Trusted, "The setup depends on this session being trusted; only its notifications are off.");
        Assert.AreNotEqual(UserSessionNotificationStatus.Allowed, mutedSession.NotificationStatus);

        // Connected, so the mute is the only thing left that can stop the message.
        await SetReachability(server, mutedSession.Id, notificationsAllowed: false, connected: true);

        await SignInWithTwoFactorAgain(server, scope, email, userId);

        var sentTo = await scope.ServiceProvider.GetRequiredService<IUserController>()
                                                .SendElevatedAccessToken(TestContext.CancellationToken);

        Assert.IsFalse(sentTo.SentToOtherDevices,
            "Nothing can arrive on a muted device, so claiming it leaves the user watching a phone that will never ring.");
    }

    /// <summary>
    /// Sets the two things a device needs to actually receive the message - the notification opt-in it grants when
    /// asked, and a live hub connection. The answer names the sessions the code really went to, so both count.
    /// </summary>
    private async Task SetReachability(AppTestServer server, Guid sessionId, bool notificationsAllowed, bool connected)
    {
        await using var dbScope = server.WebApp.Services.CreateAsyncScope();
        var dbContext = dbScope.ServiceProvider.GetRequiredService<AppDbContext>();

        var session = await dbContext.UserSessions.SingleAsync(us => us.Id == sessionId, TestContext.CancellationToken);

        session.NotificationStatus = notificationsAllowed ? UserSessionNotificationStatus.Allowed : UserSessionNotificationStatus.Muted;
        session.SignalRConnectionId = connected ? Guid.NewGuid().ToString() : null;

        await dbContext.SaveChangesAsync(TestContext.CancellationToken);
    }

    /// <summary>Signs the same account in a second time through the authenticator, producing another trusted session.</summary>
    private async Task SignInWithTwoFactorAgain(AppTestServer server, AsyncServiceScope scope, string email, Guid userId)
    {
        string sharedKey;

        await using (var dbScope = server.WebApp.Services.CreateAsyncScope())
        {
            var userManager = dbScope.ServiceProvider.GetRequiredService<UserManager<User>>();
            var user = await userManager.FindByIdAsync(userId.ToString());
            sharedKey = (await userManager.GetAuthenticatorKeyAsync(user!))!;
        }

        var authManager = scope.ServiceProvider.GetRequiredService<AuthManager>();

        await authManager.SignIn(new()
        {
            Email = email,
            Password = "P@ssw0rdP@ssw0rd", // The password DevMcpTestUtils set on this account.
            TwoFactorCode = DevMcpTestUtils.ComputeTotp(sharedKey)
        }, TestContext.CancellationToken);
    }

    /// <summary>Reads the row behind the access token this scope currently holds.</summary>
    private async Task<UserSession> ReadCurrentSession(AppTestServer server, AsyncServiceScope scope)
    {
        var accessToken = await DevMcpTestUtils.AccessToken(scope);
        var sessionId = IAuthTokenProvider.ParseAccessToken(accessToken, validateExpiry: false).GetSessionId();

        await using var dbScope = server.WebApp.Services.CreateAsyncScope();

        return await dbScope.ServiceProvider.GetRequiredService<AppDbContext>()
            .UserSessions.AsNoTracking()
            .SingleAsync(us => us.Id == sessionId, TestContext.CancellationToken);
    }

    private async Task<AppTestServer> StartServer()
    {
        var server = new AppTestServer();
        await server.Build(services => services.AddIntegrationApiOnlyTestsServices()).Start(TestContext.CancellationToken);
        return server;
    }

    public TestContext TestContext { get; set; } = default!;
}
