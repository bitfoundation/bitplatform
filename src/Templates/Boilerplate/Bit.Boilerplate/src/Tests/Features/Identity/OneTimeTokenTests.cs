namespace Boilerplate.Tests.Features.Identity;

/// <summary>
/// The one-time token flows (e-mail / phone confirmation and password reset) share one shape: a nullable
/// <c>*TokenRequestedOn</c> column, an expiry gate that subtracts it from now, and a token whose <b>purpose string</b>
/// embeds that same timestamp. Two things about that shape fail quietly, and both are pinned here.
/// <para>
/// 1. <b>The expiry gate is a lifted comparison.</b> <c>DateTimeOffset - DateTimeOffset?</c> is <c>TimeSpan?</c>, and a
/// lifted <c>&gt;</c> is <c>false</c> when the column is null - which is exactly the state a <i>consumed</i> token
/// leaves behind. Without an explicit null check the gate is skipped and the request runs on into a verification that
/// can never succeed, charging the user an <c>AccessFailedAsync</c> strike on the way.
/// </para>
/// <para>
/// 2. <b>The purpose string must be built from the same value on both sides.</b> The account lookup is
/// case-insensitive (it matches on <c>NormalizedEmail</c>) but the purpose is an HMAC input, so verifying against the
/// caller's casing while minting from the stored casing rejects the user's own valid code.
/// </para>
/// Neither failure is visible from the code alone - both look like an ordinary "Invalid token." - which is why they
/// are worth the cost of a test.
/// </summary>
[TestClass, TestCategory("IntegrationTest")]
public class OneTimeTokenTests
{
    /// <summary>
    /// Re-opening a confirmation link that was already used. <c>ConfirmEmail</c> nulls <c>EmailTokenRequestedOn</c> on
    /// success, and <c>ConfirmPage</c> auto-submits whenever the link carries an <c>emailToken</c>, so a second device,
    /// a second tab or a reload replays it. The replay must be rejected as expired <b>without</b> touching the
    /// account's failed-access count: that count does not decay, so five such replays over an account's life would
    /// lock the user out of password sign-in for something they did not do.
    /// </summary>
    [TestMethod]
    public async Task ReplayingAConsumedConfirmationToken_Should_NotChargeAFailedAccessAttempt()
    {
        await using var server = new AppTestServer();
        await server.Build(services => services.AddIntegrationApiOnlyTestsServices()).Start(TestContext.CancellationToken);

        await using var scope = server.WebApp.Services.CreateAsyncScope();
        var identityController = scope.ServiceProvider.GetRequiredService<IIdentityController>();

        var email = MagicLinkSignInUtils.NewTestEmail();

        await Assert.ThrowsExactlyAsync<BadRequestException>(
            () => identityController.SendOtp(new() { Email = email }, null, TestContext.CancellationToken));

        var captured = await server.WaitForCapturedEmail(email, e => e.Kind is CapturedEmailKind.EmailToken, TestContext.CancellationToken);

        // First use succeeds and clears EmailTokenRequestedOn.
        await identityController.ConfirmEmail(new() { Email = email, Token = captured.Token }, TestContext.CancellationToken);

        var failedCountBeforeReplay = await ReadAccessFailedCount(server, email);

        var replay = await Assert.ThrowsExactlyAsync<BadRequestException>(
            () => identityController.ConfirmEmail(new() { Email = email, Token = captured.Token }, TestContext.CancellationToken),
            "Replaying a consumed confirmation token must be rejected.");

        Assert.AreEqual(nameof(AppStrings.ExpiredToken), replay.Key,
            "A consumed token has no outstanding request, so the expiry gate must reject it before the token check - " +
            "reporting InvalidToken instead means the lifted null comparison let it through.");

        Assert.AreEqual(failedCountBeforeReplay, await ReadAccessFailedCount(server, email),
            "Rejecting a provably unverifiable replay must not spend the user's lockout budget.");
    }

    /// <summary>
    /// The same gate on the phone flow, which the shipped seed data reaches directly: <c>UserConfiguration</c> seeds
    /// <c>PhoneNumberConfirmed = true</c> while leaving <c>PhoneNumberTokenRequestedOn</c> null.
    /// </summary>
    [TestMethod]
    public async Task ConfirmingAPhoneWithNoOutstandingToken_Should_ReportExpiredRatherThanInvalid()
    {
        await using var server = new AppTestServer();
        await server.Build(services => services.AddIntegrationApiOnlyTestsServices()).Start(TestContext.CancellationToken);

        await using var scope = server.WebApp.Services.CreateAsyncScope();
        var identityController = scope.ServiceProvider.GetRequiredService<IIdentityController>();

        var exception = await Assert.ThrowsExactlyAsync<BadRequestException>(
            () => identityController.ConfirmPhone(new() { PhoneNumber = SeededTestPhoneNumber, Token = "123456" }, TestContext.CancellationToken),
            "There is no outstanding phone token for the seeded account, so this must not be accepted.");

        Assert.AreEqual(nameof(AppStrings.ExpiredToken), exception.Key,
            "PhoneNumberTokenRequestedOn is null for the seeded account, so the expiry gate must fire first.");
    }

    /// <summary>
    /// A user who registered as <c>John.Doe@Example.com</c> and later types <c>john.doe@example.com</c> resolves to the
    /// same account, because Identity looks accounts up by <c>NormalizedEmail</c>. The 6-digit code they were sent must
    /// therefore still work - it is minted from the stored address, so verifying against the typed one would reject a
    /// correctly delivered code and charge a strike for it.
    /// </summary>
    [TestMethod]
    public async Task ConfirmEmail_Should_AcceptACaseVariantOfTheRegisteredAddress()
    {
        await using var server = new AppTestServer();
        await server.Build(services => services.AddIntegrationApiOnlyTestsServices()).Start(TestContext.CancellationToken);

        await using var scope = server.WebApp.Services.CreateAsyncScope();
        var identityController = scope.ServiceProvider.GetRequiredService<IIdentityController>();

        var email = $"Mixed.Case.{Guid.NewGuid()}@BitPlatform.dev";

        await Assert.ThrowsExactlyAsync<BadRequestException>(
            () => identityController.SendOtp(new() { Email = email }, null, TestContext.CancellationToken));

        var captured = await server.WaitForCapturedEmail(email, e => e.Kind is CapturedEmailKind.EmailToken, TestContext.CancellationToken);

        // The user types their address in a different case than they registered with.
        await identityController.ConfirmEmail(
            new() { Email = email.ToLowerInvariant(), Token = captured.Token }, TestContext.CancellationToken);

        await using var dbScope = server.WebApp.Services.CreateAsyncScope();
        var dbContext = dbScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var normalizedEmail = email.ToUpperInvariant();

        Assert.IsTrue(await dbContext.Set<User>().AnyAsync(u => u.NormalizedEmail == normalizedEmail && u.EmailConfirmed, TestContext.CancellationToken),
            "The code was correct and was delivered to that address, so a difference in casing must not reject it.");
    }

    /// <summary>
    /// <c>[Phone]</c> (the only validation the DTOs carry) is far more permissive than libphonenumber, which is what
    /// actually parses the value: <c>"+1"</c> passes the attribute and throws inside <c>PhoneService</c>. On an
    /// anonymous endpoint that difference is the difference between a 400 and a 500 logged at Critical.
    /// </summary>
    [TestMethod]
    [DataRow("+1")]
    [DataRow("+999999999999999999")]
    public async Task AnUnparsablePhoneNumber_Should_BeABadRequestRatherThanAServerFault(string phoneNumber)
    {
        await using var server = new AppTestServer();
        await server.Build(services => services.AddIntegrationApiOnlyTestsServices()).Start(TestContext.CancellationToken);

        await using var scope = server.WebApp.Services.CreateAsyncScope();
        var identityController = scope.ServiceProvider.GetRequiredService<IIdentityController>();

        await Assert.ThrowsExactlyAsync<BadRequestException>(
            () => identityController.SendConfirmPhoneToken(new() { PhoneNumber = phoneNumber }, TestContext.CancellationToken),
            $"'{phoneNumber}' passes the [Phone] attribute but libphonenumber cannot parse it; that is a bad request, not a server fault.");
    }

    /// <summary>The phone number <c>UserConfiguration</c> seeds for the default test account.</summary>
    private const string SeededTestPhoneNumber = "+31684207362";

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

    public TestContext TestContext { get; set; } = default!;
}
