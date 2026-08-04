namespace Boilerplate.Tests.Features.Identity;

/// <summary>
/// The self-service endpoints that decide who can still get back into an account: the ones that re-point its e-mail /
/// phone identifier, and the one that turns the second factor off. All of them are reachable with nothing but a valid
/// access token, so what gates them is the only thing standing between a token stolen for five minutes and permanent
/// ownership of the account - <see cref="AuthPolicies.ELEVATED_ACCESS"/>, which additionally requires a code delivered
/// to the identifier the account ALREADY has.
/// <para>
/// These run through the generated HTTP proxies against a real server rather than by resolving the controller, because
/// the authorization attributes are exactly what is under test and they only run in the request pipeline.
/// </para>
/// </summary>
[TestClass, TestCategory("IntegrationTest")]
public class AccountSelfServiceSecurityTests
{
    /// <summary>
    /// A signed-in-but-not-elevated caller must not be able to start an e-mail change. This is the first step of the
    /// takeover chain: the six-digit confirmation is mailed to the address the CALLER just supplied, so without a gate
    /// the attacker's own inbox is the only proof required - and afterwards every recovery path (password reset,
    /// magic-link sign-in, OTP) points at them.
    /// </summary>
    [TestMethod]
    public async Task SendChangeEmailToken_WithoutElevatedAccess_Should_BeRejected()
    {
        await using var server = await StartServer();
        await using var scope = server.WebApp.Services.CreateAsyncScope();
        await TestAccountUtils.CreateAndSignIn(server, scope, TestContext.CancellationToken);

        var userController = scope.ServiceProvider.GetRequiredService<IUserController>();

        await Assert.ThrowsExactlyAsync<ForbiddenException>(
            () => userController.SendChangeEmailToken(new() { Email = MagicLinkSignInUtils.NewTestEmail() }, TestContext.CancellationToken),
            "Re-pointing the account's e-mail hands over every recovery path it controls, so it must require elevated access.");
    }

    /// <summary>
    /// Same question for the second factor. Note the asymmetry being asserted: <c>Enable = true</c> is deliberately NOT
    /// gated, because it already requires a valid code from the authenticator, and the empty read request is not gated
    /// either - the settings page issues one on every visit to fetch the shared key and the recovery-code count, so
    /// gating the endpoint wholesale would prompt for a code just to LOOK at the tab.
    /// </summary>
    [TestMethod]
    public async Task DisablingTwoFactor_WithoutElevatedAccess_Should_BeRejected_WhileReadingSettingsStaysOpen()
    {
        await using var server = await StartServer();
        await using var scope = server.WebApp.Services.CreateAsyncScope();
        await TestAccountUtils.CreateAndSignIn(server, scope, TestContext.CancellationToken);

        var userController = scope.ServiceProvider.GetRequiredService<IUserController>();

        var settings = await userController.TwoFactorAuth(new(), TestContext.CancellationToken);
        Assert.IsFalse(string.IsNullOrEmpty(settings.SharedKey),
            "Reading the 2fa settings must stay ungated, otherwise opening the Settings tab prompts for an elevated-access code.");

        await Assert.ThrowsExactlyAsync<ForbiddenException>(
            () => userController.TwoFactorAuth(new() { Enable = false }, TestContext.CancellationToken),
            "Turning 2fa off needs no code at all, so without elevated access a stolen access token removes the second factor.");

        await Assert.ThrowsExactlyAsync<ForbiddenException>(
            () => userController.TwoFactorAuth(new() { ResetSharedKey = true }, TestContext.CancellationToken),
            "Resetting the shared key also disables 2fa, so it is gated the same way.");
    }

    /// <summary>
    /// The state a completed e-mail change has to leave behind, and the reason it is worth a test: neither half is
    /// visible from the endpoint's code, because it writes through <c>IUserEmailStore.SetEmailAsync</c> rather than
    /// <c>UserManager.ChangeEmailAsync</c>, and that raw store call updates <c>Email</c> and <c>NormalizedEmail</c> only.
    /// <list type="number">
    /// <item><c>EmailConfirmed</c> must be true - the caller proved control of the address by quoting a code sent to it.
    /// Left false, every "mail me a code" path silently skips the address while the UI shows it as the account's e-mail.</item>
    /// <item>The security stamp must rotate - changing the recovery address is precisely when every other session should
    /// stop being able to refresh. Left alone, the account's other sessions survive the change unnoticed.</item>
    /// </list>
    /// </summary>
    [TestMethod]
    public async Task ChangeEmail_Should_ConfirmTheNewAddress_And_RotateTheSecurityStamp()
    {
        await using var server = await StartServer();
        await using var scope = server.WebApp.Services.CreateAsyncScope();
        var (email, _) = await TestAccountUtils.CreateAndSignIn(server, scope, TestContext.CancellationToken);

        var userController = scope.ServiceProvider.GetRequiredService<IUserController>();

        await TestAccountUtils.Elevate(server, scope, email, TestContext.CancellationToken);

        var (confirmedBefore, stampBefore) = await ReadEmailState(server, email);
        Assert.IsTrue(confirmedBefore, "The account confirmed its e-mail while being created.");

        var newEmail = MagicLinkSignInUtils.NewTestEmail();

        await userController.SendChangeEmailToken(new() { Email = newEmail }, TestContext.CancellationToken);

        var captured = await server.WaitForCapturedEmail(newEmail, e => e.Kind is CapturedEmailKind.EmailToken, TestContext.CancellationToken);

        await userController.ChangeEmail(new() { Email = newEmail, Token = captured.Token }, TestContext.CancellationToken);

        var (confirmedAfter, stampAfter) = await ReadEmailState(server, newEmail);

        Assert.IsTrue(confirmedAfter,
            "The token was delivered to the new address and quoted back, so that address is proven - it must end up confirmed.");
        Assert.AreNotEqual(stampBefore, stampAfter,
            "Changing the account's recovery address must rotate the security stamp, otherwise the other sessions keep refreshing.");
    }

    /// <summary>
    /// Deleting the account is the most destructive self-service operation there is - it wipes every
    /// <c>UserSession</c> and the user row inside one transaction, and it cannot be undone. Its only protection is the
    /// single <see cref="AuthPolicies.ELEVATED_ACCESS"/> attribute on the endpoint.
    /// <para>
    /// The one test that used to touch it drove the already-elevated happy path and asserted the ABSENCE of an
    /// elevated-access e-mail, which holds just as well with the attribute deleted - so removing the gate would not
    /// have turned anything red. This asserts the gate itself, the same way the two lesser self-service endpoints
    /// above already do.
    /// </para>
    /// </summary>
    [TestMethod]
    public async Task DeleteAccount_WithoutElevatedAccess_Should_BeRejected()
    {
        await using var server = await StartServer();
        await using var scope = server.WebApp.Services.CreateAsyncScope();
        var (email, userId) = await TestAccountUtils.CreateAndSignIn(server, scope, TestContext.CancellationToken);

        var userController = scope.ServiceProvider.GetRequiredService<IUserController>();

        await Assert.ThrowsExactlyAsync<ForbiddenException>(
            () => userController.Delete(TestContext.CancellationToken),
            "Without elevated access, an access token stolen for five minutes is enough to destroy the account.");

        // Deliberately not asserted by re-running the delete: the row still being there is the whole property, and
        // reading it costs nothing.
        await using var dbScope = server.WebApp.Services.CreateAsyncScope();
        var dbContext = dbScope.ServiceProvider.GetRequiredService<AppDbContext>();

        Assert.IsTrue(await dbContext.Set<User>().AnyAsync(u => u.Id == userId, TestContext.CancellationToken),
            "The refused request must not have deleted anything.");
    }

    /// <summary>
    /// Moving onto an address another account already holds. <c>IdentityOptions.User.RequireUniqueEmail</c> is left at
    /// its framework default of <c>false</c> and <c>UserValidator</c> never checks a phone number under any setting, so
    /// nothing above the database rejects this - it used to reach the filtered unique index in <c>UserConfiguration</c>
    /// and come back as a raw <c>DbUpdateException</c>, i.e. a 500 logged at Critical for something the user can fix.
    /// </summary>
    [TestMethod]
    public async Task SendChangeEmailToken_ToAnAddressAnotherAccountOwns_Should_BeARequestError()
    {
        await using var server = await StartServer();
        await using var scope = server.WebApp.Services.CreateAsyncScope();
        var (email, _) = await TestAccountUtils.CreateAndSignIn(server, scope, TestContext.CancellationToken);

        var userController = scope.ServiceProvider.GetRequiredService<IUserController>();
        var identityController = scope.ServiceProvider.GetRequiredService<IIdentityController>();

        await TestAccountUtils.Elevate(server, scope, email, TestContext.CancellationToken);

        // A second, real account, created the way the app itself creates one.
        var otherEmail = MagicLinkSignInUtils.NewTestEmail();
        await Assert.ThrowsExactlyAsync<BadRequestException>(
            () => identityController.SendOtp(new() { Email = otherEmail }, null, TestContext.CancellationToken));

        var exception = await Assert.ThrowsExactlyAsync<BadRequestException>(
            () => userController.SendChangeEmailToken(new() { Email = otherEmail }, TestContext.CancellationToken),
            "An address another account already owns is a request the user can correct, not a server fault.");

        Assert.AreEqual(AppStrings.DuplicateEmailOrPhoneNumber, exception.Message,
            "The user has to be told which part of their request was wrong.");
    }

    /// <summary>
    /// <b>Currently failing by design - BP-133 is open.</b>
    /// <para>
    /// <c>SendElevatedAccessToken</c> delivers by e-mail only when the address is confirmed, by SMS only when the phone
    /// is confirmed, and its fallback branch reaches only the user's OTHER sessions. An account with neither identifier
    /// confirmed and a single session - which is exactly what an external sign-in produces when the provider asserts no
    /// e-mail and no phone claim - therefore gets HTTP 200 with the code delivered nowhere, and the resend throttle is
    /// stamped anyway, so retrying is refused for the next five minutes. That account can never satisfy
    /// <see cref="AuthPolicies.ELEVATED_ACCESS"/>, so it can never delete itself, revoke a session or leave a tenant.
    /// </para>
    /// <para>
    /// Unblocking it needs a product decision this review will not make: either give that population a real channel
    /// (permit the requesting session's own push subscription, or return the code in the authenticated response), or
    /// fail loudly. Either way the endpoint must stop reporting success when it delivered nothing.
    /// </para>
    /// </summary>
    [TestMethod, Ignore("BP-133 is open: needs a decision on which channel serves users with no confirmed identifier. " +
        "Without the attribute it fails with 'Expected exception of exact type BadRequestException but no exception was thrown' - i.e. HTTP 200 having delivered nothing.")]
    public async Task SendElevatedAccessToken_WithNoDeliveryChannel_Should_NotReportSuccess()
    {
        await using var server = await StartServer();
        await using var scope = server.WebApp.Services.CreateAsyncScope();
        var (email, userId) = await TestAccountUtils.CreateAndSignIn(server, scope, TestContext.CancellationToken);

        // Put the account into the shape an external sign-in leaves behind: neither identifier confirmed.
        await using (var dbScope = server.WebApp.Services.CreateAsyncScope())
        {
            var dbContext = dbScope.ServiceProvider.GetRequiredService<AppDbContext>();
            await dbContext.Set<User>()
                .Where(u => u.Id == userId)
                .ExecuteUpdateAsync(u => u.SetProperty(x => x.EmailConfirmed, false)
                                          .SetProperty(x => x.PhoneNumberConfirmed, false), TestContext.CancellationToken);
        }

        var userController = scope.ServiceProvider.GetRequiredService<IUserController>();

        await Assert.ThrowsExactlyAsync<BadRequestException>(
            () => userController.SendElevatedAccessToken(TestContext.CancellationToken),
            "Reporting success while delivering the code nowhere leaves the account permanently unable to elevate.");
    }


    private async Task<AppTestServer> StartServer()
    {
        var server = new AppTestServer();
        await server.Build(services => services.AddIntegrationApiOnlyTestsServices()).Start(TestContext.CancellationToken);
        return server;
    }

    private async Task<(bool emailConfirmed, string? securityStamp)> ReadEmailState(AppTestServer server, string email)
    {
        await using var scope = server.WebApp.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var normalizedEmail = email.ToUpperInvariant();

        return await dbContext.Set<User>()
            .Where(u => u.NormalizedEmail == normalizedEmail)
            .Select(u => ValueTuple.Create(u.EmailConfirmed, u.SecurityStamp))
            .SingleAsync(TestContext.CancellationToken);
    }

    public TestContext TestContext { get; set; } = default!;
}
