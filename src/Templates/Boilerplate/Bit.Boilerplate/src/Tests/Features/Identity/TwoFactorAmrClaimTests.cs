using OtpNet;
using Microsoft.AspNetCore.Identity;

namespace Boilerplate.Tests.Features.Identity;

/// <summary>
/// <c>amr=mfa</c> is what ASP.NET Identity's SignInManager stamps on a completed two-factor sign-in (RFC 8176), and it
/// is the natural claim for a policy that means "this session really passed a second factor". The catch is that this
/// app mints every access token from <c>AppUserClaimsPrincipalFactory</c>, so a claim only outlives the sign-in if it
/// is carried as a session claim - and access tokens here live five minutes.
/// </summary>
[TestClass, TestCategory("IntegrationTest")]
public class TwoFactorAmrClaimTests
{
    public TestContext TestContext { get; set; } = default!;

    [TestMethod]
    public async Task TheAmrClaim_Should_SurviveARefresh_SoAPolicyCanRelyOnIt()
    {
        await using var server = new AppTestServer();
        await server.Build(services => services.AddIntegrationApiOnlyTestsServices()).Start(TestContext.CancellationToken);
        await using var scope = server.WebApp.Services.CreateAsyncScope();

        var (email, userId) = await TestAccountUtils.CreateAndSignIn(server, scope, TestContext.CancellationToken);

        // The per-run account is created through the magic link, so it has no password to sign in with a second time.
        const string password = "P@ssw0rdP@ssw0rd";
        await using (var dbScope = server.WebApp.Services.CreateAsyncScope())
        {
            var dbContext = dbScope.ServiceProvider.GetRequiredService<AppDbContext>();
            var account = await dbContext.Users.SingleAsync(item => item.Id == userId, TestContext.CancellationToken);
            account.PasswordHash = new PasswordHasher<User>().HashPassword(account, password);
            await dbContext.SaveChangesAsync(TestContext.CancellationToken);
        }

        var userController = scope.ServiceProvider.GetRequiredService<IUserController>();
        var enrolment = await userController.TwoFactorAuth(new(), TestContext.CancellationToken);
        var sharedKey = enrolment.SharedKey!.Replace(" ", "");

        var enabled = await userController.TwoFactorAuth(
            new() { Enable = true, TwoFactorCode = ComputeTotp(sharedKey) }, TestContext.CancellationToken);
        Assert.IsTrue(enabled.IsTwoFactorEnabled, "Sanity: two factor has to be on before signing in with it.");

        var authManager = scope.ServiceProvider.GetRequiredService<AuthManager>();

        var requiresTwoFactor = await authManager.SignIn(
            new() { Email = email, Password = password }, TestContext.CancellationToken);
        Assert.IsTrue(requiresTwoFactor, "An account with two factor on must be challenged for the second factor.");

        await authManager.SignIn(new()
        {
            Email = email,
            Password = password,
            TwoFactorCode = ComputeTotp(sharedKey)
        }, TestContext.CancellationToken);

        var afterSignIn = await ReadAccessToken(scope);
        Assert.IsTrue(afterSignIn.HasClaim("amr", "mfa"),
            $"SignInManager stamps amr=mfa on a completed two-factor sign-in. Claims: [{Describe(afterSignIn)}]");

        var refreshToken = await scope.ServiceProvider.GetRequiredService<IStorageService>().GetItem("refresh_token");
        var rawRefreshClaims = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().ReadJwtToken(refreshToken).Claims.ToArray();
        Assert.Contains(claim => claim.Type == "amr" && claim.Value == "mfa", rawRefreshClaims,
            "The refresh token has to carry amr, since that is what the refresh reads it back from.");

        await scope.ServiceProvider.GetRequiredService<AuthManager>().RefreshToken(requestedBy: nameof(TwoFactorAmrClaimTests));

        var afterRefresh = await ReadAccessToken(scope);
        Assert.IsTrue(afterRefresh.HasClaim("amr", "mfa"),
            "A five minute access token is refreshed constantly, so a policy built on amr=mfa would start refusing a " +
            $"legitimately two-factor session on its first refresh. Claims: [{Describe(afterRefresh)}]");
    }

    private static async Task<ClaimsPrincipal> ReadAccessToken(AsyncServiceScope scope)
    {
        var accessToken = await scope.ServiceProvider.GetRequiredService<IAuthTokenProvider>().GetAccessToken();
        Assert.IsFalse(string.IsNullOrWhiteSpace(accessToken), "There should be an access token to inspect.");
        return IAuthTokenProvider.ParseAccessToken(accessToken!, validateExpiry: false);
    }

    private static string Describe(ClaimsPrincipal principal)
        => string.Join(", ", principal.Claims.Select(claim => $"{claim.Type}={claim.Value}"));

    private static string ComputeTotp(string base32Secret) => new Totp(Base32Encoding.ToBytes(base32Secret)).ComputeTotp();
}
