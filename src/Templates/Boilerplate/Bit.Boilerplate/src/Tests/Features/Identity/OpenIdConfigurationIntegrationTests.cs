using System.Net.Sockets;
using System.Security.Cryptography;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography.X509Certificates;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Boilerplate.Server.Api.Infrastructure.Services;

namespace Boilerplate.Tests.Features.Identity;

/// <summary>
/// <c>AppCertificate.md</c> makes a promise that nothing else in the suite checks: another backend service can
/// validate tokens this API issues, knowing nothing but its address - no private key, no copied public key, no shared
/// secret. Everything it needs comes from <c>/.well-known/openid-configuration</c> and the JWKS behind it.
/// <para>
/// These tests stand up a second, genuinely separate ASP.NET Core app inside the test - the "other backend service" -
/// wired exactly as the doc's snippet says, and send it a token minted by <see cref="AppTestServer"/>. That exercises
/// the parts inference cannot reach: whether a two-field discovery document is enough for
/// <c>OpenIdConnectConfigurationRetriever</c>, whether the published JWKS really carries usable keys, and whether the
/// issuer/audience/algorithm the API stamps are the ones a stock <c>JwtBearer</c> expects.
/// </para>
/// </summary>
[TestClass, TestCategory("IntegrationTest")]
public class OpenIdConfigurationIntegrationTests
{
    public TestContext TestContext { get; set; } = default!;

    /// <summary>
    /// The headline: sign in against the API, hand the access token to the other service, and get through its
    /// <c>RequireAuthorization</c> gate.
    /// </summary>
    [TestMethod]
    public async Task AnotherBackendService_Should_AcceptOurAccessToken_UsingOnlyTheDiscoveryDocument()
    {
        await using var server = new AppTestServer();
        await server.Build(s => s.AddIntegrationApiOnlyTestsServices()).Start(TestContext.CancellationToken);

        await using var scope = server.WebApp.Services.CreateAsyncScope();
        await SignIn(scope);

        var accessToken = await scope.ServiceProvider.GetRequiredService<IStorageService>().GetItem("access_token");
        Assert.IsNotNull(accessToken, "Signing in should have stored an access token.");

        await using var resourceServer = await StartResourceServer(server);

        var (status, body) = await CallProtectedEndpoint(resourceServer, accessToken);

        Assert.AreEqual(HttpStatusCode.OK, status,
            "A service that only knows this API's address must be able to validate its tokens. A failure here means " +
            "the discovery document, the JWKS, or the issuer/audience/algorithm the API stamps does not match what a " +
            "stock JwtBearer expects - which is exactly what AppCertificate.md tells operators to rely on.");

        Assert.AreEqual(UserIdOf(accessToken).ToString(), body,
            "Validating the signature is only half of it: the resource server has to end up with the caller's identity, " +
            "or it can authorize the request but not act on it.");
    }

    /// <summary>
    /// The control. Without it the test above would pass against a service whose endpoint was never protected at all.
    /// </summary>
    [TestMethod]
    public async Task AnotherBackendService_Should_RejectAnAnonymousCallAndAForgedToken()
    {
        await using var server = new AppTestServer();
        await server.Build(s => s.AddIntegrationApiOnlyTestsServices()).Start(TestContext.CancellationToken);

        await using var scope = server.WebApp.Services.CreateAsyncScope();
        await SignIn(scope);

        var accessToken = await scope.ServiceProvider.GetRequiredService<IStorageService>().GetItem("access_token");

        await using var resourceServer = await StartResourceServer(server);

        Assert.AreEqual(HttpStatusCode.Unauthorized, (await CallProtectedEndpoint(resourceServer, accessToken: null)).Status,
            "The endpoint has to be protected, or the acceptance test above proves nothing.");

        using var rogueRsa = RSA.Create(3072);
        var forged = ReSign(new JwtSecurityToken(accessToken!), new RsaSecurityKey(rogueRsa) { KeyId = "rogue" });

        Assert.AreEqual(HttpStatusCode.Unauthorized, (await CallProtectedEndpoint(resourceServer, forged)).Status,
            "A token signed by a key that is not in the published JWKS must be refused.");
    }

    /// <summary>
    /// The rotation story, seen from the outside: a token signed by a retired certificate is accepted by an
    /// independent validator too, because that certificate's public key is still in the JWKS. This is the assertion
    /// that makes "rotate without signing anybody out" true for sibling services and not just for this API.
    /// The retired pair is staged by <see cref="SigningKeyRotationTests"/>'s module initializer.
    /// </summary>
    [TestMethod]
    public async Task AnotherBackendService_Should_AcceptATokenSignedByARetiredCertificate()
    {
        await using var server = new AppTestServer();
        await server.Build(s => s.AddIntegrationApiOnlyTestsServices()).Start(TestContext.CancellationToken);

        var activeThumbprint = AppCertificateService.GetActiveAppCertificate(server.WebApp.Configuration).Thumbprint;
        var retired = AppCertificateService.GetAllAppCertificates(server.WebApp.Configuration)
            .FirstOrDefault(cert => cert.Thumbprint != activeThumbprint);

        Assert.IsNotNull(retired, "No retired certificate was staged, so this test would prove nothing.");

        await using var scope = server.WebApp.Services.CreateAsyncScope();
        await SignIn(scope);

        var accessToken = await scope.ServiceProvider.GetRequiredService<IStorageService>().GetItem("access_token");

        var signedByRetired = ReSign(new JwtSecurityToken(accessToken!),
            new RsaSecurityKey(retired.GetRSAPrivateKey()!) { KeyId = retired.Thumbprint });

        await using var resourceServer = await StartResourceServer(server);

        var (status, body) = await CallProtectedEndpoint(resourceServer, signedByRetired);

        Assert.AreEqual(HttpStatusCode.OK, status,
            "The retired certificate's public key is published in the JWKS, so a token it signed before the rotation " +
            "has to keep working for a sibling service as well.");

        Assert.AreEqual(UserIdOf(accessToken).ToString(), body,
            "And it has to arrive as the same user - a rotation must not change who the caller is.");
    }

    /// <summary>
    /// The user id the API put in the token, read the way the client heads read it. The resource server is asserted
    /// against this rather than against a hard-coded value, so the test says "the identity survived the trip" rather
    /// than "some string came back".
    /// </summary>
    private static Guid UserIdOf(string? accessToken)
    {
        return IAuthTokenProvider.ParseAccessToken(accessToken!, validateExpiry: false).GetUserId();
    }

    /// <summary>
    /// A second ASP.NET Core app, configured exactly as <c>AppCertificate.md</c>'s "Integrating Other Backend
    /// Services" snippet says: an <c>Authority</c> and nothing else. It shares no code, no key and no configuration
    /// with the API beyond the two literals every consumer is told to set.
    /// </summary>
    private async Task<WebApplication> StartResourceServer(AppTestServer server)
    {
        var configuration = server.WebApp.Configuration;

        var builder = WebApplication.CreateSlimBuilder();

        builder.WebHost.UseUrls(GenerateServerUrl());

        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
        {
            options.Authority = server.WebAppServerAddress.ToString();
            options.RequireHttpsMetadata = false;
            options.TokenValidationParameters = new()
            {
                ClockSkew = TimeSpan.Zero,
                RequireSignedTokens = true,

                ValidateIssuerSigningKey = true,
                ValidAlgorithms = [SecurityAlgorithms.RsaSha256],

                RequireExpirationTime = true,

                ValidateAudience = true,
                ValidAudience = configuration["Identity:Audience"],

                ValidateIssuer = true,
                ValidIssuer = configuration["Identity:Issuer"]
            };
        });
        builder.Services.AddAuthorization();

        var resourceServer = builder.Build();

        resourceServer.UseAuthentication();
        resourceServer.UseAuthorization();

        // Echoes the caller's identity rather than a constant, so the assertions can tell "the request got past the
        // authorization gate" from "the claims actually survived the trip". A resource server that authorizes but
        // cannot see who it is talking to is useless, and a 200 alone would not catch that.
        resourceServer.MapGet("/protected", (ClaimsPrincipal user) =>
            user.FindFirstValue(ClaimTypes.NameIdentifier) ?? "<no user id claim reached the resource server>")
            .RequireAuthorization();

        await resourceServer.StartAsync(TestContext.CancellationToken);

        return resourceServer;
    }

    /// <summary>
    /// Returns the status code and, on success, whatever identity the resource server saw for the caller.
    /// </summary>
    private async Task<(HttpStatusCode Status, string Body)> CallProtectedEndpoint(WebApplication resourceServer, string? accessToken)
    {
        using var httpClient = new HttpClient { BaseAddress = new Uri(resourceServer.Urls.Single()) };

        using var request = new HttpRequestMessage(HttpMethod.Get, "protected");
        if (accessToken is not null)
        {
            request.Headers.Authorization = new("Bearer", accessToken);
        }

        using var response = await httpClient.SendAsync(request, TestContext.CancellationToken);

        return (response.StatusCode, await response.Content.ReadAsStringAsync(TestContext.CancellationToken));
    }

    /// <summary>
    /// Re-issues the token with a different key and nothing else changed. The registered claims are dropped because
    /// the descriptor writes its own; leaving them in would duplicate <c>aud</c>.
    /// </summary>
    private static string ReSign(JwtSecurityToken source, RsaSecurityKey key)
    {
        string[] registeredClaims = [JwtRegisteredClaimNames.Exp, JwtRegisteredClaimNames.Iat, JwtRegisteredClaimNames.Nbf,
            JwtRegisteredClaimNames.Iss, JwtRegisteredClaimNames.Aud];

        return new JwtSecurityTokenHandler().CreateEncodedJwt(new SecurityTokenDescriptor
        {
            Issuer = source.Issuer,
            Audience = source.Audiences.Single(),
            Expires = DateTime.UtcNow.AddMinutes(5),
            Subject = new ClaimsIdentity(source.Claims.Where(claim => registeredClaims.Contains(claim.Type) is false)),
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.RsaSha256Signature)
        });
    }

    private static string GenerateServerUrl()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return $"http://127.0.0.1:{port}/";
    }

    private Task SignIn(AsyncServiceScope scope)
    {
        return scope.ServiceProvider.GetRequiredService<AuthManager>().SignIn(new()
        {
            Email = TestData.DefaultTestEmail,
            Password = TestData.DefaultTestPassword
        }, TestContext.CancellationToken);
    }
}
