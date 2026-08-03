using System.Text.Json;
using System.Security.Cryptography;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using Boilerplate.Server.Api.Infrastructure.Services;

namespace Boilerplate.Tests.Features.Identity;

/// <summary>
/// A certificate rotation with no sign-out rests entirely on the server trusting more than one key at a time and on
/// each key being distinguishable. Both halves are invisible on every happy path - tokens keep working perfectly with
/// a single certificate and a hard-coded <c>kid</c> - so nothing but these tests fails if either is undone.
/// <para>
/// The mechanism: <c>AppCertificateService</c> loads <c>AppCertificate.crt</c> plus every
/// <c>AppCertificate.{name}.crt</c> pair beside it, gives each key the certificate's own thumbprint as its
/// <c>KeyId</c>, signs with the active one and hands all of them to <c>IssuerSigningKeys</c> and to
/// <c>/.well-known/jwks</c>.
/// Replace those thumbprints with a shared constant - which is what the code used to do - and a validator can no
/// longer tell two certificates apart, which is exactly what makes an overlap window impossible.
/// </para>
/// </summary>
[TestClass, TestCategory("IntegrationTest")]
public class SigningKeyRotationTests
{
    public TestContext TestContext { get; set; } = default!;

    /// <summary>
    /// The <c>kid</c> in the JWT header has to name the certificate that signed it. A constant would still produce a
    /// perfectly valid token, so this asserts against the actual thumbprint rather than merely against "not empty".
    /// </summary>
    [TestMethod]
    public async Task IssuedTokens_Should_CarryTheSigningCertificatesThumbprintAsKid()
    {
        await using var server = new AppTestServer();
        await server.Build(s => s.AddIntegrationApiOnlyTestsServices()).Start(TestContext.CancellationToken);

        await using var scope = server.WebApp.Services.CreateAsyncScope();
        await SignIn(scope);

        var accessToken = await scope.ServiceProvider.GetRequiredService<IStorageService>().GetItem("access_token");
        Assert.IsNotNull(accessToken, "Signing in should have stored an access token.");

        var activeThumbprint = AppCertificateService.GetActiveAppCertificate(server.WebApp.Configuration).Thumbprint;

        Assert.AreEqual(activeThumbprint, new JwtSecurityToken(accessToken).Header.Kid,
            "The token must be stamped with the thumbprint of the certificate that signed it. A shared constant makes " +
            "every certificate look alike, and a validator holding two keys then cannot pick the right one.");
    }

    /// <summary>
    /// Whatever the server trusts, it must publish - otherwise a sibling service reading the discovery document
    /// rejects tokens this server considers perfectly valid the moment a second certificate is introduced.
    /// </summary>
    [TestMethod]
    public async Task Jwks_Should_PublishOneKeyPerTrustedCertificate()
    {
        await using var server = new AppTestServer();
        await server.Build(s => s.AddIntegrationApiOnlyTestsServices()).Start(TestContext.CancellationToken);

        var expectedKids = AppCertificateService.GetAllAppCertificates(server.WebApp.Configuration)
            .Select(cert => cert.Thumbprint)
            .ToArray();

        using var anonymousHttpClient = new HttpClient { BaseAddress = server.WebAppServerAddress };
        using var response = await anonymousHttpClient.GetAsync(".well-known/jwks", TestContext.CancellationToken);
        response.EnsureSuccessStatusCode();

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.CancellationToken));

        var publishedKids = document.RootElement.GetProperty("keys")
            .EnumerateArray()
            .Select(key => key.GetProperty("kid").GetString())
            .ToArray();

        Assert.AreSequenceEqual(expectedKids, publishedKids, SequenceOrder.InAnyOrder, "Every certificate the server accepts a token from has to appear in the JWKS, each under its own kid.");
    }

    /// <summary>
    /// The control for both tests above: proves the trusted key set is actually consulted. Without it they would
    /// still pass against a server that validated no signature at all.
    /// </summary>
    [TestMethod]
    public async Task ATokenSignedByAnUntrustedKey_Should_BeRejected()
    {
        await using var server = new AppTestServer();
        await server.Build(s => s.AddIntegrationApiOnlyTestsServices()).Start(TestContext.CancellationToken);

        await using var scope = server.WebApp.Services.CreateAsyncScope();
        await SignIn(scope);

        var storageService = scope.ServiceProvider.GetRequiredService<IStorageService>();
        var userController = scope.ServiceProvider.GetRequiredService<IUserController>();

        // Sanity: the real token works, so the failure below is about the signing key and nothing else.
        Assert.AreEqual(TestData.DefaultTestEmail, (await userController.GetCurrentUser(TestContext.CancellationToken)).Email);

        var realAccessToken = new JwtSecurityToken(await storageService.GetItem("access_token") ?? throw new InvalidOperationException("No access token."));

        // Same issuer, same audience, same claims, same algorithm - only the key differs.
        var rogueKey = new RsaSecurityKey(RSA.Create(3072)) { KeyId = "rogue" };
        var forged = new JwtSecurityTokenHandler().CreateEncodedJwt(new SecurityTokenDescriptor
        {
            Issuer = realAccessToken.Issuer,
            Audience = realAccessToken.Audiences.Single(),
            Expires = DateTime.UtcNow.AddMinutes(5),
            Subject = new ClaimsIdentity(realAccessToken.Claims),
            SigningCredentials = new SigningCredentials(rogueKey, SecurityAlgorithms.RsaSha256Signature)
        });

        await storageService.SetItem("access_token", forged);

        await Assert.ThrowsExactlyAsync<UnauthorizedException>(
            () => userController.GetCurrentUser(TestContext.CancellationToken),
            "A token signed by a key the server does not trust must be rejected, however well-formed it is.");
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
