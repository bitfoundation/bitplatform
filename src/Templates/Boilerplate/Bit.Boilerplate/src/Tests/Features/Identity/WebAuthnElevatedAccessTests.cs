using Fido2NetLib;

namespace Boilerplate.Tests.Features.Identity;

/// <summary>
/// Covers the passkey half of <see cref="AuthPolicies.ELEVATED_ACCESS"/>: <c>IdentityController.ElevateByWebAuthn</c>
/// turns the raw <see cref="JsonElement"/> on <c>RefreshTokenRequestDto</c> into an
/// <see cref="AuthenticatorAssertionRawResponse"/> through the app's own serializer. Every other WebAuthn endpoint
/// takes the Fido2 type as an action parameter and is bound by MVC's reflection resolver; this one goes through the
/// source-generated contexts, where an unregistered type or a converter that did not run fails at runtime only.
/// </summary>
[TestClass, TestCategory("IntegrationTest")]
public class WebAuthnElevatedAccessTests
{
    /// <summary>
    /// A browser's PublicKeyCredential JSON has base64url byte members and spells the client data
    /// <c>clientDataJSON</c>, not <c>clientDataJson</c>. All of it has to survive the server's own serializer.
    /// </summary>
    [TestMethod]
    public async Task AWebAuthnAssertionOnTheRefreshRequest_Should_DeserializeThroughTheAppSerializer()
    {
        await using var server = await StartServer();
        await using var scope = server.WebApp.Services.CreateAsyncScope();

        var jsonSerializerOptions = scope.ServiceProvider.GetRequiredService<JsonSerializerOptions>();

        var assertion = BrowserAssertion.Deserialize(jsonSerializerOptions.GetTypeInfo<AuthenticatorAssertionRawResponse>());

        Assert.IsNotNull(assertion, "The assertion JSON a browser produces must deserialize into the Fido2 type.");
        Assert.AreSequenceEqual(new byte[] { 1, 2, 3, 4 }, assertion.RawId,
            "rawId is base64url text on the wire; Fido2's converter has to decode it back to the credential id the lookup uses.");
        Assert.AreSequenceEqual("{\"hi\":1}"u8.ToArray(), assertion.Response.ClientDataJson,
            "clientDataJSON carries the challenge the cached assertion options are found by, so it must arrive byte for byte.");
    }

    /// <summary>
    /// The negative half: an unsigned assertion must fail the refresh, not quietly return an ordinary token as if
    /// the WebAuthn part had never been asked for - a caller that got one would believe it had elevated.
    /// </summary>
    [TestMethod]
    public async Task AnUnverifiableAssertion_Should_FailTheRefreshRatherThanElevateIt()
    {
        await using var server = await StartServer();
        await using var scope = server.WebApp.Services.CreateAsyncScope();

        await TestAccountUtils.CreateAndSignIn(server, scope, TestContext.CancellationToken);

        var storageService = scope.ServiceProvider.GetRequiredService<IStorageService>();
        var refreshToken = await storageService.GetItem("refresh_token");
        var identityController = scope.ServiceProvider.GetRequiredService<IIdentityController>();

        await Assert.ThrowsAsync<Exception>(
            () => identityController.Refresh(new()
            {
                RefreshToken = refreshToken,
                WebAuthnClientResponse = BrowserAssertion
            }, TestContext.CancellationToken),
            "An assertion that matches no stored credential must fail the whole refresh.");

        var accessToken = await storageService.GetItem("access_token");
        var user = IAuthTokenProvider.ParseAccessToken(accessToken, validateExpiry: false);

        Assert.IsNull(user.GetElevatedSessionExpiresOn(),
            "A rejected assertion must leave the session exactly as unelevated as it was.");
    }


    /// <summary>The shape of a PublicKeyCredential as a browser serializes it, with base64url byte members.</summary>
    private static JsonElement BrowserAssertion => JsonDocument.Parse(
        """
        {
            "id": "AQIDBA",
            "rawId": "AQIDBA",
            "type": "public-key",
            "response": {
                "authenticatorData": "BQYHCA",
                "clientDataJSON": "eyJoaSI6MX0",
                "signature": "CQoLDA",
                "userHandle": "DQ4PEA"
            }
        }
        """).RootElement;

    private async Task<AppTestServer> StartServer()
    {
        var server = new AppTestServer();
        await server.Build(services => services.AddIntegrationApiOnlyTestsServices()).Start(TestContext.CancellationToken);
        return server;
    }

    public TestContext TestContext { get; set; } = default!;
}
