using System.Text.Json;
using System.Security.Cryptography;
using System.Runtime.CompilerServices;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography.X509Certificates;
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
/// </para>
/// <para>
/// To exercise that for real these tests stage a genuine retired pair. It has to exist before the first
/// <c>AppTestServer</c> is built - <c>AppCertificateService</c> caches its certificates the first time it is asked -
/// so it is written from a <see cref="ModuleInitializerAttribute"/>, which runs before <c>[AssemblyInitialize]</c>.
/// The pair lands in the test assembly's own output directory and is rewritten on every run.
/// </para>
/// </summary>
[TestClass, TestCategory("IntegrationTest")]
public class SigningKeyRotationTests
{
    private const string RetiredCertificateName = "AppCertificate.rotation-overlap";

    public TestContext TestContext { get; set; } = default!;

    /// <summary>
    /// Writes a retired certificate pair next to the active one, so the whole test assembly runs against a server
    /// that is mid-rotation. An extra trusted public key changes nothing for any other test - it can validate tokens
    /// nobody ever signed with it.
    /// </summary>
    [ModuleInitializer]
    internal static void StageRetiredCertificate()
    {
        var path = Path.Combine(AppContext.BaseDirectory, RetiredCertificateName);

        using var rsa = RSA.Create(3072);
        var request = new CertificateRequest($"CN={RetiredCertificateName}", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        using var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddYears(1));

        File.WriteAllText($"{path}.crt", certificate.ExportCertificatePem());
        File.WriteAllText($"{path}.key", rsa.ExportPkcs8PrivateKeyPem());
    }

    /// <summary>
    /// The <c>kid</c> in the JWT header has to name the certificate that signed it. A constant would still produce a
    /// perfectly valid token, so this asserts against the actual thumbprint rather than merely against "not empty",
    /// and against the <b>active</b> one specifically - a retired certificate must never sign anything.
    /// </summary>
    [TestMethod]
    public async Task IssuedTokens_Should_CarryTheActiveCertificatesThumbprintAsKid()
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

        var trustedCertificates = AppCertificateService.GetAllAppCertificates(server.WebApp.Configuration);

        // Without this the test would pass vacuously if the staged pair were never picked up: one expected key, one
        // published key, equal sets, nothing about rotation proven.
        Assert.IsGreaterThan(1, trustedCertificates.Length,
            $"The staged retired pair ({RetiredCertificateName}.*) should have been loaded alongside the active certificate.");

        using var anonymousHttpClient = new HttpClient { BaseAddress = server.WebAppServerAddress };
        using var response = await anonymousHttpClient.GetAsync(".well-known/jwks", TestContext.CancellationToken);
        response.EnsureSuccessStatusCode();

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.CancellationToken));

        var publishedKids = document.RootElement.GetProperty("keys")
            .EnumerateArray()
            .Select(key => key.GetProperty("kid").GetString())
            .ToArray();

        Assert.AreSequenceEqual(trustedCertificates.Select(cert => cert.Thumbprint), publishedKids, SequenceOrder.InAnyOrder,
            "Every certificate the server accepts a token from has to appear in the JWKS, each under its own kid.");
    }

    /// <summary>
    /// The acceptance test for the whole feature: a token signed by a <b>retired</b> certificate is still honoured.
    /// This is what "rotate without signing anybody out" means in practice - the tokens minted before the rotation
    /// were signed by what is now the retired key, and they have to keep working until they expire.
    /// </summary>
    [TestMethod]
    public async Task ATokenSignedByARetiredCertificate_Should_StillBeAccepted()
    {
        await using var server = new AppTestServer();
        await server.Build(s => s.AddIntegrationApiOnlyTestsServices()).Start(TestContext.CancellationToken);

        await using var scope = server.WebApp.Services.CreateAsyncScope();
        await SignIn(scope);

        var storageService = scope.ServiceProvider.GetRequiredService<IStorageService>();
        var userController = scope.ServiceProvider.GetRequiredService<IUserController>();

        var retired = AppCertificateService.GetAllAppCertificates(server.WebApp.Configuration)
            .Single(cert => cert.Subject.Contains(RetiredCertificateName, StringComparison.Ordinal));

        var reSigned = ReSign(await CurrentAccessToken(storageService), retired);

        await storageService.SetItem("access_token", reSigned);

        Assert.AreEqual(TestData.DefaultTestEmail, (await userController.GetCurrentUser(TestContext.CancellationToken)).Email,
            "A token signed by a retired certificate must keep working for its remaining lifetime - that is the whole " +
            "point of keeping the retired pair around after a rotation.");
    }

    /// <summary>
    /// The control for everything above: an unrelated key must still be refused. Without this the acceptance test
    /// would pass just as happily against a server that validated no signature at all.
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

        using var rogueRsa = RSA.Create(3072);
        var forged = ReSign(await CurrentAccessToken(storageService), new RsaSecurityKey(rogueRsa) { KeyId = "rogue" });

        await storageService.SetItem("access_token", forged);

        await Assert.ThrowsExactlyAsync<UnauthorizedException>(
            () => userController.GetCurrentUser(TestContext.CancellationToken),
            "A token signed by a key the server does not trust must be rejected, however well-formed it is.");
    }

    private async Task<JwtSecurityToken> CurrentAccessToken(IStorageService storageService)
    {
        var accessToken = await storageService.GetItem("access_token");
        Assert.IsNotNull(accessToken, "Signing in should have stored an access token.");
        return new JwtSecurityToken(accessToken);
    }

    private static string ReSign(JwtSecurityToken source, X509Certificate2 certificate)
    {
        return ReSign(source, new RsaSecurityKey(certificate.GetRSAPrivateKey()!) { KeyId = certificate.Thumbprint });
    }

    /// <summary>
    /// Re-issues the given token with a different key and nothing else changed. The registered claims are dropped
    /// because the descriptor writes its own; leaving them in would duplicate <c>aud</c> and make the test about
    /// claim shape rather than about which key signed it.
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

    private Task SignIn(AsyncServiceScope scope)
    {
        return scope.ServiceProvider.GetRequiredService<AuthManager>().SignIn(new()
        {
            Email = TestData.DefaultTestEmail,
            Password = TestData.DefaultTestPassword
        }, TestContext.CancellationToken);
    }
}
