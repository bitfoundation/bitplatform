using Boilerplate.Shared.Features.Identity.Dtos;

namespace Boilerplate.Tests.Features.Identity;

[TestClass, TestCategory("IntegrationTest"), TestCategory("Identity")]
public partial class DeviceAccountsTests
{
    public TestContext TestContext { get; set; } = default!;

    [TestMethod]
    public async Task SigningInAnotherAccount_Should_KeepTheFirstOne_AndSwitchingBack_Should_UseItsOwnSession()
    {
        await using var server = new AppTestServer();
        await server.Build().Start(TestContext.CancellationToken);

        await using var client = server.CreateAppClient();

        var (_, firstUserId) = await TestAccountUtils.CreateAndSignIn(client, TestContext.CancellationToken);
        var firstSessionId = (await CurrentUser(client)).GetSessionId();

        var (_, secondUserId) = await TestAccountUtils.CreateAndAddAccount(client, TestContext.CancellationToken);

        Assert.AreEqual(secondUserId, (await CurrentUser(client)).GetUserId());
        Assert.AreSequenceEqual(new[] { firstUserId }, await OtherAccountIds(client));
        Assert.AreEqual(1, await CountUserSessions(server, firstUserId), "Adding an account must not end the session of the one that was active.");

        Assert.IsTrue(await client.AuthManager.SwitchAccount(firstUserId));

        var user = await CurrentUser(client);

        Assert.AreEqual(firstUserId, user.GetUserId());
        Assert.AreEqual(firstSessionId, user.GetSessionId(), "Switching back must use the account's own session, not a new sign-in.");
        Assert.AreEqual(firstUserId, (await client.GetController<IUserController>().GetCurrentUser(TestContext.CancellationToken)).Id,
            "Requests made after a switch must be made as the account switched to.");
        Assert.AreSequenceEqual(new[] { secondUserId }, await OtherAccountIds(client));
    }

    [TestMethod]
    public async Task SigningOut_Should_EndOnlyTheActiveAccount_AndActivateTheNextOne()
    {
        await using var server = new AppTestServer();
        await server.Build().Start(TestContext.CancellationToken);

        await using var client = server.CreateAppClient();

        var (_, firstUserId) = await TestAccountUtils.CreateAndSignIn(client, TestContext.CancellationToken);
        var (_, secondUserId) = await TestAccountUtils.CreateAndAddAccount(client, TestContext.CancellationToken);

        await client.AuthManager.SignOut(TestContext.CancellationToken);

        Assert.AreEqual(0, await CountUserSessions(server, secondUserId));
        Assert.AreEqual(1, await CountUserSessions(server, firstUserId), "Signing out must end only the active account's session.");
        Assert.AreEqual(firstUserId, (await CurrentUser(client)).GetUserId(), "The other account on the device must take the signed out one's place.");
        Assert.IsEmpty(await OtherAccountIds(client));
    }

    [TestMethod]
    public async Task SigningOutOfAllAccounts_Should_EndEverySessionOnTheDevice()
    {
        await using var server = new AppTestServer();
        await server.Build().Start(TestContext.CancellationToken);

        await using var client = server.CreateAppClient();

        var (_, firstUserId) = await TestAccountUtils.CreateAndSignIn(client, TestContext.CancellationToken);
        var (_, secondUserId) = await TestAccountUtils.CreateAndAddAccount(client, TestContext.CancellationToken);

        await client.AuthManager.SignOutAll(TestContext.CancellationToken);

        Assert.IsFalse((await CurrentUser(client)).IsAuthenticated());
        Assert.IsEmpty(await OtherAccountIds(client));
        Assert.AreEqual(0, await CountUserSessions(server, firstUserId), "An account that was not active has a session of its own, and it must end too.");
        Assert.AreEqual(0, await CountUserSessions(server, secondUserId));
    }

    [TestMethod]
    public async Task SigningInAgainToAnAccountOnTheDevice_Should_EndItsEarlierSession()
    {
        await using var server = new AppTestServer();
        await server.Build().Start(TestContext.CancellationToken);

        await using var client = server.CreateAppClient();

        var (firstEmail, firstUserId) = await TestAccountUtils.CreateAndSignIn(client, TestContext.CancellationToken);
        var firstSessionId = (await CurrentUser(client)).GetSessionId();

        var (_, secondUserId) = await TestAccountUtils.CreateAndAddAccount(client, TestContext.CancellationToken);

        var identityController = client.GetController<IIdentityController>();

        await identityController.SendOtp(new() { Email = firstEmail }, null, TestContext.CancellationToken);

        var otp = await server.WaitForCapturedEmail(firstEmail, e => e.Kind is CapturedEmailKind.Otp, TestContext.CancellationToken);

        Assert.IsFalse(await client.AuthManager.SignIn(new() { Email = firstEmail, Otp = otp.Token }, TestContext.CancellationToken));

        var user = await CurrentUser(client);

        Assert.AreEqual(firstUserId, user.GetUserId());
        Assert.AreNotEqual(firstSessionId, user.GetSessionId());
        Assert.AreEqual(1, await CountUserSessions(server, firstUserId), "The session the account had on this device before must end rather than stay behind in the sessions list.");
        Assert.AreSequenceEqual(new[] { secondUserId }, await OtherAccountIds(client));
    }

    [TestMethod]
    public async Task SigningInAgainToTheActiveAccount_Should_KeepTheOtherAccounts()
    {
        await using var server = new AppTestServer();
        await server.Build().Start(TestContext.CancellationToken);

        await using var client = server.CreateAppClient();

        var (firstEmail, firstUserId) = await TestAccountUtils.CreateAndSignIn(client, TestContext.CancellationToken);
        var (_, secondUserId) = await TestAccountUtils.CreateAndAddAccount(client, TestContext.CancellationToken);

        Assert.IsTrue(await client.AuthManager.SwitchAccount(firstUserId));

        var firstSessionId = (await CurrentUser(client)).GetSessionId();

        await client.GetController<IIdentityController>().SendOtp(new() { Email = firstEmail }, null, TestContext.CancellationToken);

        var otp = await server.WaitForCapturedEmail(firstEmail, e => e.Kind is CapturedEmailKind.Otp, TestContext.CancellationToken);

        Assert.IsFalse(await client.AuthManager.SignIn(new() { Email = firstEmail, Otp = otp.Token }, TestContext.CancellationToken));

        var user = await CurrentUser(client);

        Assert.AreEqual(firstUserId, user.GetUserId());
        Assert.AreNotEqual(firstSessionId, user.GetSessionId());
        Assert.AreEqual(1, await CountUserSessions(server, firstUserId));
        Assert.AreSequenceEqual(new[] { secondUserId }, await OtherAccountIds(client), "Signing in again to the active account must not touch the other accounts on the device.");
        Assert.AreEqual(1, await CountUserSessions(server, secondUserId));
    }

    [TestMethod]
    public async Task ARevokedRefreshToken_Should_EndItsSession()
    {
        await using var server = new AppTestServer();
        await server.Build().Start(TestContext.CancellationToken);

        await using var client = server.CreateAppClient();

        var (_, userId) = await TestAccountUtils.CreateAndSignIn(client, TestContext.CancellationToken);

        var refreshToken = await client.Services.GetRequiredService<IStorageService>().GetItem("refresh_token");
        var identityController = client.GetController<IIdentityController>();

        await identityController.RevokeRefreshToken(new() { RefreshToken = "not-a-refresh-token" }, TestContext.CancellationToken);
        Assert.AreEqual(1, await CountUserSessions(server, userId));

        await identityController.RevokeRefreshToken(new() { RefreshToken = refreshToken }, TestContext.CancellationToken);

        Assert.AreEqual(0, await CountUserSessions(server, userId));
        await Assert.ThrowsAsync<UnauthorizedException>(() => identityController.Refresh(new() { RefreshToken = refreshToken }, TestContext.CancellationToken));
    }

    [TestMethod]
    public async Task ARefreshThatCompletesAfterASwitch_Should_KeepItsAnswerForItsAccount()
    {
        var holdRefresh = false;
        var refreshStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseTheRefresh = new TaskCompletionSource();
        TokenResponseDto? heldAnswer = null;

        await using var server = new AppTestServer();
        await server.Build(configureTestWebAppServices: services =>
        {
            var original = services.Single(service => service.ServiceType == typeof(IIdentityController));
            services.Remove(original);
            services.AddTransient(sp =>
            {
                var real = (IIdentityController)ActivatorUtilities.CreateInstance(sp, original.ImplementationType!);
                var identityController = A.Fake<IIdentityController>(options => options.Wrapping(real));

                A.CallTo(() => identityController.Refresh(A<RefreshTokenRequestDto>._, A<CancellationToken>._))
                    .ReturnsLazily(async (RefreshTokenRequestDto request, CancellationToken cancellationToken) =>
                    {
                        if (holdRefresh is false)
                            return await real.Refresh(request, cancellationToken);

                        holdRefresh = false;
                        refreshStarted.SetResult();
                        await releaseTheRefresh.Task;
                        return heldAnswer = await real.Refresh(request, cancellationToken);
                    });

                return identityController;
            });
        }).Start(TestContext.CancellationToken);

        await using var client = server.CreateAppClient();

        var (_, firstUserId) = await TestAccountUtils.CreateAndSignIn(client, TestContext.CancellationToken);
        var (_, secondUserId) = await TestAccountUtils.CreateAndAddAccount(client, TestContext.CancellationToken);

        holdRefresh = true;
        var refresh = client.AuthManager.RefreshToken(requestedBy: nameof(ARefreshThatCompletesAfterASwitch_Should_KeepItsAnswerForItsAccount));

        await refreshStarted.Task.WaitAsync(TimeSpan.FromSeconds(30), TestContext.CancellationToken);

        Assert.IsTrue(await client.AuthManager.SwitchAccount(firstUserId));

        releaseTheRefresh.SetResult();

        Assert.IsNull(await refresh.WaitAsync(TimeSpan.FromSeconds(30), TestContext.CancellationToken),
            "The account the refresh was made for is no longer the active one, so its token must not be handed to requests made as the active one.");
        Assert.AreEqual(firstUserId, (await CurrentUser(client)).GetUserId());

        var secondAccount = (await client.AuthManager.GetOtherAccounts()).Single(account => account.User.Id == secondUserId);

        Assert.AreEqual(heldAnswer!.RefreshToken, secondAccount.RefreshToken,
            "The server has rotated the account's refresh token, so dropping the answer would leave the account holding one the server no longer accepts.");
    }

    private static async Task<ClaimsPrincipal> CurrentUser(AppClient client)
    {
        return (await client.AuthManager.GetAuthenticationStateAsync()).User;
    }

    private static async Task<Guid[]> OtherAccountIds(AppClient client)
    {
        return [.. (await client.AuthManager.GetOtherAccounts()).Select(account => account.User.Id)];
    }

    private async Task<int> CountUserSessions(AppTestServer server, Guid userId)
    {
        await using var scope = server.ApiApp.Services.CreateAsyncScope();

        return await scope.ServiceProvider.GetRequiredService<AppDbContext>().UserSessions
            .CountAsync(us => us.UserId == userId, TestContext.CancellationToken);
    }
}
