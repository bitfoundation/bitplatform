using Microsoft.EntityFrameworkCore;
using Boilerplate.Shared.Features.Identity;
using Boilerplate.Server.Api.Infrastructure.Data;
using Boilerplate.Client.Core.Infrastructure.Services;
using Boilerplate.Client.Core.Infrastructure.Services.Contracts;

namespace Boilerplate.Tests.Features.Identity;

/// <summary>
/// Access tokens and refresh tokens are minted by the same <c>AppJwtSecureDataFormat</c>, from the same RSA key, with
/// the same issuer and the same claim set - <c>AppBearerTokenOptionsConfigurator</c> builds two instances of one type
/// and the only difference between the two JWTs is <c>exp</c>. What keeps them apart is a single line: each protector
/// gets its own <c>aud</c> (<c>{Identity:Audience}</c> vs <c>{Identity:Audience}:RefreshToken</c>) and validates only
/// its own.
/// <para>
/// That is a lot of security resting on one string concatenation that nothing else in the system references, and it
/// is invisible on every happy path - both token classes keep working perfectly whether or not the separation exists.
/// These two tests are the only thing that fails if it is removed, simplified, or if a future change reintroduces a
/// shared audience.
/// </para>
/// </summary>
[TestClass, TestCategory("IntegrationTest")]
public class TokenClassSeparationTests
{
    /// <summary>
    /// A 5-minute access token must not buy a 14-day session. Without the audience split, <c>Refresh</c> unprotects
    /// it happily (the refresh protector sets <c>ValidateLifetime = false</c>), it carries <c>s-id</c>, the security
    /// stamp and an <c>iat</c> inside the rotation tolerance, so every check passes and the caller walks away with a
    /// fresh token pair. Anything that leaks an access token - a proxy log, a HAR file, an error report - would
    /// therefore leak a two-week credential.
    /// <para>
    /// The second assertion covers the other half of the same defect: the rejection must happen <b>before</b> the
    /// <c>UserSession</c> is loaded, or the <c>catch (UnauthorizedException) when (userSession is not null)</c>
    /// handler deletes the session and a stale access token becomes a way to sign the victim out.
    /// </para>
    /// </summary>
    [TestMethod]
    public async Task AnAccessToken_Should_NotBeAcceptedAtTheRefreshEndpoint()
    {
        await using var server = await StartServerAndSignIn();
        await using var scope = server.WebApp.Services.CreateAsyncScope();
        await SignIn(scope);

        var storageService = scope.ServiceProvider.GetRequiredService<IStorageService>();
        var accessToken = await storageService.GetItem("access_token");
        Assert.IsNotNull(accessToken, "Signing in should have stored an access token.");

        var sessionId = IAuthTokenProvider.ParseAccessToken(accessToken, validateExpiry: false).GetSessionId();

        await Assert.ThrowsExactlyAsync<UnauthorizedException>(
            () => scope.ServiceProvider.GetRequiredService<IIdentityController>()
                       .Refresh(new() { RefreshToken = accessToken }, TestContext.CancellationToken),
            "An access token presented as a refresh token must be rejected. The two JWTs differ only in `aud`.");

        Assert.IsTrue(await SessionExists(server, sessionId),
            "The rejection must come from the token protector, before Refresh loads the UserSession - otherwise the " +
            "rotation-theft handler deletes the row and replaying a stale access token signs the real user out.");
    }

    /// <summary>
    /// The mirror image: a 14-day refresh token must not authenticate ordinary API calls. If it does, a stolen
    /// refresh token is a bearer credential for its full lifetime <b>and</b> the rotation tripwire never fires,
    /// because a token that is never presented at <c>/Refresh</c> never advances <c>UserSession.RenewedOn</c> - so
    /// the one control designed to detect exactly this theft stays silent for two weeks.
    /// </summary>
    [TestMethod]
    public async Task ARefreshToken_Should_NotAuthenticateAnApiCall()
    {
        await using var server = await StartServerAndSignIn();
        await using var scope = server.WebApp.Services.CreateAsyncScope();
        await SignIn(scope);

        var storageService = scope.ServiceProvider.GetRequiredService<IStorageService>();
        var userController = scope.ServiceProvider.GetRequiredService<IUserController>();

        // Sanity: with the real access token this call succeeds, so a failure below is about the token class and not
        // about the request being malformed or the account being unusable.
        Assert.AreEqual(TestData.DefaultTestEmail, (await userController.GetCurrentUser(TestContext.CancellationToken)).Email);

        var refreshToken = await storageService.GetItem("refresh_token");
        Assert.IsNotNull(refreshToken, "Signing in should have stored a refresh token.");

        // TestAuthTokenProvider reads "access_token" from storage and AuthDelegatingHandler puts it in the
        // Authorization header, so this is exactly what an attacker holding only the refresh token would send.
        await storageService.SetItem("access_token", refreshToken);

        await Assert.ThrowsExactlyAsync<UnauthorizedException>(
            () => userController.GetCurrentUser(TestContext.CancellationToken),
            "A refresh token sent as `Authorization: Bearer` must not authenticate an API call.");
    }

    private async Task<AppTestServer> StartServerAndSignIn()
    {
        var server = new AppTestServer();
        await server.Build(services => services.AddIntegrationApiOnlyTestsServices()).Start(TestContext.CancellationToken);
        return server;
    }

    private Task SignIn(AsyncServiceScope scope)
    {
        return scope.ServiceProvider.GetRequiredService<AuthManager>().SignIn(new()
        {
            Email = TestData.DefaultTestEmail,
            Password = TestData.DefaultTestPassword
        }, TestContext.CancellationToken);
    }

    private async Task<bool> SessionExists(AppTestServer server, Guid sessionId)
    {
        await using var scope = server.WebApp.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        return await dbContext.UserSessions.AnyAsync(us => us.Id == sessionId, TestContext.CancellationToken);
    }

    public TestContext TestContext { get; set; } = default!;
}
