namespace Boilerplate.Tests.Features.Identity;

/// <summary>
/// Enrolling a passkey ADDS a way to sign in, which makes it an account-factor change and puts it in the same class as
/// changing the e-mail, changing the user name, revoking a session and deleting the account - all of which carry
/// <see cref="AuthPolicies.ELEVATED_ACCESS"/>.
/// <para>
/// It is worth its own file because the consequence is unusually durable. A WebAuthn credential is a row in its own
/// table; nothing on the password-change path, the security-stamp rotation or <c>RevokeSession</c> touches it. So an
/// access token stolen for five minutes buys a credential that survives every remedy the product offers the victim -
/// and <c>PasswordlessTab</c> shows a single button driven by a LOCAL storage flag, so the account owner cannot even
/// see that it exists.
/// </para>
/// <para>
/// These run through the generated HTTP proxies against a real server rather than by resolving the controller, because
/// the authorization attribute is exactly what is under test and attributes only run in the request pipeline.
/// </para>
/// </summary>
[TestClass, TestCategory("IntegrationTest")]
public class WebAuthnEnrolmentElevationTests
{
    /// <summary>
    /// The finding itself: a signed-in-but-not-elevated caller must not be able to enrol a passkey.
    /// <para>
    /// The assertion deliberately stops at the authorization boundary rather than completing a ceremony. A real
    /// attestation cannot be produced without an authenticator, but it is not needed: if the gate is in place the
    /// request never reaches the body, so a <see cref="ForbiddenException"/> here is exactly the signal, and it is the
    /// same shape <see cref="AccountSelfServiceSecurityTests"/> asserts for the sibling endpoints.
    /// </para>
    /// </summary>
    [TestMethod]
    public async Task CreateWebAuthnCredential_WithoutElevatedAccess_Should_BeRejected()
    {
        await using var server = await StartServer();
        await using var scope = server.WebApp.Services.CreateAsyncScope();
        await TestAccountUtils.CreateAndSignIn(server, scope, TestContext.CancellationToken);

        var userController = scope.ServiceProvider.GetRequiredService<IUserController>();

        await Assert.ThrowsExactlyAsync<ForbiddenException>(
            () => userController.CreateWebAuthnCredential(EmptyAttestation, TestContext.CancellationToken),
            "Enrolling a passkey adds a permanent sign-in method that survives a password change AND revoking every session, " +
            "so it must require elevated access exactly like Delete, ChangeUserName and RevokeSession do.");
    }

    /// <summary>
    /// The other half, and the test that stops the first one from passing for the wrong reason: with elevation the call
    /// must get PAST authorization and fail on its own terms instead.
    /// <para>
    /// Without this, the gate could be wrong in the opposite direction - locking out the legitimate path - and
    /// <see cref="CreateWebAuthnCredential_WithoutElevatedAccess_Should_BeRejected"/> would still be green, because it
    /// cannot tell "refused by policy" from "refused for any other reason". Asserting NOT-Forbidden is the whole point;
    /// the empty attestation is expected to fail validation, and that failure is the proof the request got through.
    /// </para>
    /// </summary>
    [TestMethod]
    public async Task CreateWebAuthnCredential_WithElevatedAccess_Should_PassAuthorization()
    {
        await using var server = await StartServer();
        await using var scope = server.WebApp.Services.CreateAsyncScope();
        var (email, _) = await TestAccountUtils.CreateAndSignIn(server, scope, TestContext.CancellationToken);

        await TestAccountUtils.Elevate(server, scope, email, TestContext.CancellationToken);

        var userController = scope.ServiceProvider.GetRequiredService<IUserController>();

        var exception = await Assert.ThrowsAsync<Exception>(
            () => userController.CreateWebAuthnCredential(EmptyAttestation, TestContext.CancellationToken));

        Assert.IsNotInstanceOfType<ForbiddenException>(exception,
            "With an elevated session the request must reach the endpoint body. A ForbiddenException here means the gate " +
            $"locks out the legitimate enrolment path too. Actual: {exception.GetType().Name}: {exception.Message}");
    }


    /// <summary>
    /// The shared contract takes the attestation as a raw <see cref="JsonElement"/>, so an empty object is all that is
    /// needed to reach - or be refused before - the endpoint body.
    /// </summary>
    private static JsonElement EmptyAttestation => JsonDocument.Parse("{}").RootElement;

    private async Task<AppTestServer> StartServer()
    {
        var server = new AppTestServer();
        await server.Build(services => services.AddIntegrationApiOnlyTestsServices()).Start(TestContext.CancellationToken);
        return server;
    }

    public TestContext TestContext { get; set; } = default!;
}

