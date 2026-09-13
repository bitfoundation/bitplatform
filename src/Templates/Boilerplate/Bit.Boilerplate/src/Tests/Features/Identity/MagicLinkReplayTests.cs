namespace Boilerplate.Tests.Features.Identity;

/// <summary>
/// A magic link is a bearer credential for the whole account, so the sign-in that uses it must spend it.
/// <c>OtpRequestedOn</c> is the gate: every purpose string is built from it (<c>Otp_{method},{OtpRequestedOn}</c>)
/// and <c>OtpSignIn</c> refuses the code while it is null, so clearing it on success burns the code.
/// </summary>
[TestClass, TestCategory("IntegrationTest")]
public partial class MagicLinkReplayTests
{
    public TestContext TestContext { get; set; } = default!;

    /// <summary>
    /// The replay: the same code presented twice must be refused as expired, without spending a lockout strike.
    /// </summary>
    [TestMethod]
    public async Task ReplayingAConsumedMagicLinkCode_Should_BeRejectedAsExpired()
    {
        await using var server = new AppTestServer();
        await server.Build(services => services.AddIntegrationApiOnlyTestsServices()).Start(TestContext.CancellationToken);

        await using var scope = server.WebApp.Services.CreateAsyncScope();
        var identityController = scope.ServiceProvider.GetRequiredService<IIdentityController>();

        // A confirmed, per-run account. Confirming already signs it in once, which leaves no outstanding code behind.
        var (email, _) = await TestAccountUtils.CreateAndSignIn(server, scope, TestContext.CancellationToken);

        // The magic link e-mail of a repeat sign-in: the account is confirmed, so the code arrives as a plain OTP.
        await identityController.SendOtp(new() { Email = email }, null, TestContext.CancellationToken);

        var captured = await server.WaitForCapturedEmail(email,
            capturedEmail => capturedEmail.Kind is CapturedEmailKind.Otp, TestContext.CancellationToken);

        var tokens = await identityController.SignIn(new() { Email = email, Otp = captured.Token }, TestContext.CancellationToken);

        Assert.IsFalse(string.IsNullOrWhiteSpace(tokens.AccessToken),
            "The first use of the emailed code is the legitimate sign-in and has to succeed.");

        var failedCountBeforeReplay = await ReadAccessFailedCount(server, email);
        var sessionCountBeforeReplay = await ReadUserSessionCount(server, email);

        // Whoever reads the mailbox next - a second device, a forwarded copy, a shared inbox - opens the same link.
        var replay = await Assert.ThrowsExactlyAsync<BadRequestException>(
            () => identityController.SignIn(new() { Email = email, Otp = captured.Token }, TestContext.CancellationToken),
            "A magic link code is single-use; the sign-in it performed must have spent it.");

        Assert.AreEqual(nameof(AppStrings.ExpiredToken), replay.Key,
            "The completed sign-in leaves no outstanding code, so the expiry gate must reject the replay before the " +
            "token check - reporting anything else means OtpRequestedOn survived the sign-in.");

        Assert.AreEqual(failedCountBeforeReplay, await ReadAccessFailedCount(server, email),
            "Rejecting a provably unverifiable replay must not spend the account's lockout budget.");

        Assert.AreEqual(sessionCountBeforeReplay, await ReadUserSessionCount(server, email),
            "A refused replay must not leave a second UserSession - i.e. a second live device - behind.");
    }

    /// <summary>
    /// Asking for a new code right after signing in must still work: clearing <c>OtpRequestedOn</c> also releases
    /// the resend throttle, which measures from that timestamp. Deliberately does not assert the two codes differ -
    /// the purpose string has second precision, so two requests in one second mint the same digits.
    /// </summary>
    [TestMethod]
    public async Task RequestingANewCode_Should_BeAllowedRightAfterAConsumedOne()
    {
        await using var server = new AppTestServer();
        await server.Build(services => services.AddIntegrationApiOnlyTestsServices()).Start(TestContext.CancellationToken);

        await using var scope = server.WebApp.Services.CreateAsyncScope();
        var identityController = scope.ServiceProvider.GetRequiredService<IIdentityController>();

        var (email, _) = await TestAccountUtils.CreateAndSignIn(server, scope, TestContext.CancellationToken);

        await identityController.SendOtp(new() { Email = email }, null, TestContext.CancellationToken);

        var firstCode = await server.WaitForCapturedEmail(email,
            capturedEmail => capturedEmail.Kind is CapturedEmailKind.Otp, TestContext.CancellationToken);

        await identityController.SignIn(new() { Email = email, Otp = firstCode.Token }, TestContext.CancellationToken);

        // A second request straight after the sign-in: the throttle is measured from OtpRequestedOn, which the
        // sign-in cleared, so this must not be answered with TooManyRequests.
        await identityController.SendOtp(new() { Email = email }, null, TestContext.CancellationToken);

        // Newest-first: the mail this SendOtp just captured. The digits may equal firstCode.Token
        // (purpose string is second-precision) and still be the live code, because OtpRequestedOn was restamped.
        var secondCode = await server.WaitForCapturedEmail(email,
            capturedEmail => capturedEmail.Kind is CapturedEmailKind.Otp,
            TestContext.CancellationToken);

        var tokens = await identityController.SignIn(new() { Email = email, Otp = secondCode.Token }, TestContext.CancellationToken);

        Assert.IsFalse(string.IsNullOrWhiteSpace(tokens.AccessToken),
            "The freshly issued code has to sign the account in; burning the previous one must not burn the flow.");
    }

    private static async Task<int> ReadAccessFailedCount(AppTestServer server, string email)
    {
        await using var scope = server.WebApp.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var normalizedEmail = email.ToUpperInvariant();

        return await dbContext.Set<User>()
            .Where(u => u.NormalizedEmail == normalizedEmail)
            .Select(u => u.AccessFailedCount)
            .SingleAsync();
    }

    private static async Task<int> ReadUserSessionCount(AppTestServer server, string email)
    {
        await using var scope = server.WebApp.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var normalizedEmail = email.ToUpperInvariant();

        return await dbContext.UserSessions.CountAsync(us => us.User!.NormalizedEmail == normalizedEmail);
    }
}
