using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using Boilerplate.Shared.Features.Identity;
using Boilerplate.Server.Api.Infrastructure.Data;
using Boilerplate.Client.Core.Infrastructure.Services;
using Boilerplate.Client.Core.Infrastructure.Services.Contracts;

namespace Boilerplate.Tests.Features.Identity;

/// <summary>
/// Covers refresh-token rotation in <c>IdentityController.Refresh</c>: every successful refresh issues a new token
/// pair and stamps <see cref="UserSession.RenewedOn"/>, and presenting a token that was NOT the one minted at that
/// last rotation is treated as theft - the session row is deleted, which signs that device out for good.
/// <para>
/// Every test here drives a <see cref="FakeTimeProvider"/>, because the check compares the presented token's <c>iat</c>
/// against <c>RenewedOn</c> with a 30-second tolerance. Left on the wall clock, a whole test runs inside that
/// tolerance, so reuse would never be detected and the tests would pass for the wrong reason.
/// </para>
/// </summary>
[TestClass, TestCategory("IntegrationTest")]
public partial class RefreshTokenRotationTests
{
    /// <summary>
    /// The interval a real client refreshes at - the access token's lifetime (<c>Identity:BearerTokenExpiration</c>,
    /// 5 minutes by default). Comfortably above the 30-second rotation tolerance.
    /// </summary>
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromMinutes(5);

    /// <summary>
    /// The happy path: after a full access-token lifetime the client presents the refresh token it holds, and gets a
    /// brand-new pair back while the session row records the rotation.
    /// </summary>
    [TestMethod]
    public async Task CurrentRefreshToken_Should_RotateAndStampTheSession()
    {
        var (server, scope, timeProvider) = await StartServerAndSignIn();
        await using var _ = server;
        await using var __ = scope;

        var identityController = scope.ServiceProvider.GetRequiredService<IIdentityController>();
        var (sessionId, firstRefreshToken) = await ReadSession(scope);

        var (startedOn, renewedOnBefore) = await ReadSessionTimestamps(server, sessionId);
        Assert.IsNull(renewedOnBefore, "A session that has never been refreshed carries no RenewedOn.");

        timeProvider.Advance(RefreshInterval);

        var response = await identityController.Refresh(new() { RefreshToken = firstRefreshToken }, TestContext.CancellationToken);

        Assert.IsNotNull(response.AccessToken, "A successful refresh must return a new access token.");
        Assert.IsNotNull(response.RefreshToken, "Rotation means a successful refresh also returns a NEW refresh token.");
        Assert.AreNotEqual(firstRefreshToken, response.RefreshToken, "The refresh token must rotate, not be handed back unchanged.");

        var (_, renewedOnAfter) = await ReadSessionTimestamps(server, sessionId);
        Assert.AreEqual(startedOn + (long)RefreshInterval.TotalSeconds, renewedOnAfter,
            "UserSession.RenewedOn must record the instant of this rotation, which is exactly one refresh interval after the session started.");
    }

    /// <summary>
    /// Reuse detection. The client rotates once and then a second party replays the SUPERSEDED token a while later -
    /// the signature is still valid and the token is nowhere near its 14-day expiry, so <c>iat</c> vs <c>RenewedOn</c>
    /// is the only thing standing between a stolen token and a fresh session. The request must be rejected AND the
    /// session row destroyed, so the token cannot be retried against a session that still exists.
    /// </summary>
    [TestMethod]
    public async Task SupersededRefreshToken_Should_BeRejectedAndDestroyTheSession()
    {
        var (server, scope, timeProvider) = await StartServerAndSignIn();
        await using var _ = server;
        await using var __ = scope;

        var identityController = scope.ServiceProvider.GetRequiredService<IIdentityController>();
        var (sessionId, supersededToken) = await ReadSession(scope);

        // A legitimate rotation: `supersededToken` is spent here and replaced by the one in the response.
        timeProvider.Advance(RefreshInterval);
        await identityController.Refresh(new() { RefreshToken = supersededToken }, TestContext.CancellationToken);

        // Some time later the superseded token is presented again.
        timeProvider.Advance(RefreshInterval);

        await Assert.ThrowsExactlyAsync<UnauthorizedException>(
            () => identityController.Refresh(new() { RefreshToken = supersededToken }, TestContext.CancellationToken),
            "Replaying a superseded refresh token must be rejected as rotation/theft.");

        Assert.IsFalse(await SessionExists(server, sessionId),
            "Detecting reuse must delete the UserSession row, otherwise the device stays refreshable after a suspected theft.");
    }

    /// <summary>
    /// A session that has already been revoked (its row deleted, e.g. by the previous test's path or from the Sessions
    /// page) cannot be refreshed back into existence, even with a structurally valid, unexpired refresh token.
    /// </summary>
    [TestMethod]
    public async Task RefreshToken_Of_ARevokedSession_Should_BeRejected()
    {
        var (server, scope, timeProvider) = await StartServerAndSignIn();
        await using var _ = server;
        await using var __ = scope;

        var identityController = scope.ServiceProvider.GetRequiredService<IIdentityController>();
        var (sessionId, refreshToken) = await ReadSession(scope);

        await using (var dbScope = server.WebApp.Services.CreateAsyncScope())
        {
            var dbContext = dbScope.ServiceProvider.GetRequiredService<AppDbContext>();
            await dbContext.UserSessions.Where(us => us.Id == sessionId).ExecuteDeleteAsync(TestContext.CancellationToken);
        }

        timeProvider.Advance(RefreshInterval);

        await Assert.ThrowsExactlyAsync<UnauthorizedException>(
            () => identityController.Refresh(new() { RefreshToken = refreshToken }, TestContext.CancellationToken),
            "A refresh token whose UserSession row is gone must not be honoured.");
    }

    /// <summary>
    /// <b>Currently failing by design - requires a schema change.</b>
    /// <para>
    /// The lost-rotation-response case. The server rotates successfully and mints a replacement token, but the response
    /// never reaches the client (dropped connection, HTTP timeout). <c>AuthManager.StoreTokens</c> only runs on success,
    /// so the client still holds the SUPERSEDED token, and <c>RetryDelegatingHandler</c> re-sends the identical request
    /// moments later. That retry must succeed - the client did nothing wrong and there is no theft here.
    /// </para>
    /// <para>
    /// It cannot pass today, and not because the tolerance is too small: the 30 seconds is compared against
    /// <c>|iat - RenewedOn|</c>, which for a superseded token is a whole refresh interval (5 minutes), never the retry
    /// delay. No value of that constant separates "the client retried 3 seconds later" from "someone replayed an old
    /// token", because the server keeps only ONE rotation timestamp. Fixing it needs the previous one as well - e.g.
    /// <c>UserSession.PreviousRenewedOn</c> - so the check can accept the immediately-preceding token briefly after a
    /// rotation while still rejecting anything older. Widening it with an elapsed-time window instead would accept ANY
    /// previously-valid token for that session during the 30 seconds after every rotation.
    /// </para>
    /// </summary>
    [TestMethod, Ignore("Needs UserSession.PreviousRenewedOn (and a migration); see the remarks above.")]
    public async Task LostRotationResponse_Should_LetTheClientRetryWithTheSupersededToken()
    {
        var (server, scope, timeProvider) = await StartServerAndSignIn();
        await using var _ = server;
        await using var __ = scope;

        var identityController = scope.ServiceProvider.GetRequiredService<IIdentityController>();
        var (_, refreshToken) = await ReadSession(scope);

        // The rotation the client never sees the response of: the reply is simply discarded here.
        timeProvider.Advance(RefreshInterval);
        await identityController.Refresh(new() { RefreshToken = refreshToken }, TestContext.CancellationToken);

        // RetryDelegatingHandler's first retry - ~3 seconds later, with the token the client still has.
        timeProvider.Advance(TimeSpan.FromSeconds(3));

        var retried = await identityController.Refresh(new() { RefreshToken = refreshToken }, TestContext.CancellationToken);

        Assert.IsNotNull(retried.AccessToken,
            "A retry moments after a lost rotation response must succeed instead of signing the device out.");
    }

    /// <summary>
    /// <b>Currently failing by design - requires the same schema change.</b>
    /// <para>
    /// The other side of keeping a single rotation timestamp. When two rotations happen less than 30 seconds apart -
    /// which the app does to itself, since <c>AuthManager.SwitchTenant</c> and <c>TryEnterElevatedAccessMode</c> each
    /// force an extra refresh right after a routine one - the oldest token's <c>iat</c> is still within the tolerance of
    /// the newest <c>RenewedOn</c>. So rotation detection silently skips a generation and a token that was superseded
    /// twice is still accepted.
    /// </para>
    /// <para>
    /// Comparing <c>iat</c> against the current and previous rotation stamps closes this at the same time as the
    /// lost-response case above.
    /// </para>
    /// </summary>
    [TestMethod, Ignore("Needs UserSession.PreviousRenewedOn (and a migration); see the remarks above.")]
    public async Task TwoRotationsWithinTheTolerance_Should_StillRejectTheOldestToken()
    {
        var (server, scope, timeProvider) = await StartServerAndSignIn();
        await using var _ = server;
        await using var __ = scope;

        var identityController = scope.ServiceProvider.GetRequiredService<IIdentityController>();
        var (_, oldestToken) = await ReadSession(scope);

        // Both rotations happen inside the 30-second tolerance of when `oldestToken` was minted - the shape produced by
        // a routine refresh immediately followed by SwitchTenant / TryEnterElevatedAccessMode.
        timeProvider.Advance(TimeSpan.FromSeconds(5));
        var firstRotation = await identityController.Refresh(new() { RefreshToken = oldestToken }, TestContext.CancellationToken);

        timeProvider.Advance(TimeSpan.FromSeconds(5));
        // Calling the controller directly does not write the new tokens back to storage (only AuthManager does), so the
        // replacement has to be taken from the response - re-reading storage would just replay `oldestToken` again.
        await identityController.Refresh(new() { RefreshToken = firstRotation.RefreshToken }, TestContext.CancellationToken);

        await Assert.ThrowsExactlyAsync<UnauthorizedException>(
            () => identityController.Refresh(new() { RefreshToken = oldestToken }, TestContext.CancellationToken),
            "A token superseded two rotations ago must be rejected regardless of how close together those rotations were.");
    }

    /// <summary>
    /// Boots a test server whose clock this test drives, and signs the seeded test account in so a real
    /// <see cref="UserSession"/> and a real token pair exist. The fake clock is seeded from the wall clock so nothing
    /// else (certificate validity, token signature validation) sees a skewed time.
    /// </summary>
    private async Task<(AppTestServer server, AsyncServiceScope scope, FakeTimeProvider timeProvider)> StartServerAndSignIn()
    {
        var timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);

        var server = new AppTestServer();

        await server.Build(services =>
        {
            services.AddIntegrationApiOnlyTestsServices();
            services.Replace(ServiceDescriptor.Singleton<TimeProvider>(timeProvider));
        }).Start(TestContext.CancellationToken);

        var scope = server.WebApp.Services.CreateAsyncScope();

        await scope.ServiceProvider.GetRequiredService<AuthManager>().SignIn(new()
        {
            Email = TestData.DefaultTestEmail,
            Password = TestData.DefaultTestPassword
        }, TestContext.CancellationToken);

        return (server, scope, timeProvider);
    }

    /// <summary>
    /// Reads the refresh token the client is currently holding, plus the id of the session it belongs to (the
    /// <see cref="AppClaimTypes.SESSION_ID"/> claim of the matching access token).
    /// </summary>
    private static async Task<(Guid sessionId, string refreshToken)> ReadSession(AsyncServiceScope scope)
    {
        var storageService = scope.ServiceProvider.GetRequiredService<IStorageService>();

        var accessToken = await storageService.GetItem("access_token");
        var refreshToken = await storageService.GetItem("refresh_token");

        Assert.IsNotNull(accessToken, "Signing in should have stored an access token.");
        Assert.IsNotNull(refreshToken, "Signing in should have stored a refresh token.");

        var sessionId = IAuthTokenProvider.ParseAccessToken(accessToken, validateExpiry: false).GetSessionId();

        return (sessionId, refreshToken);
    }

    private static async Task<(long startedOn, long? renewedOn)> ReadSessionTimestamps(AppTestServer server, Guid sessionId)
    {
        await using var scope = server.WebApp.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        return await dbContext.UserSessions
            .Where(us => us.Id == sessionId)
            .Select(us => ValueTuple.Create(us.StartedOn, us.RenewedOn))
            .SingleAsync();
    }

    private static async Task<bool> SessionExists(AppTestServer server, Guid sessionId)
    {
        await using var scope = server.WebApp.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        return await dbContext.UserSessions.AnyAsync(us => us.Id == sessionId);
    }

    public TestContext TestContext { get; set; } = default!;
}
