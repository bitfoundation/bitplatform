namespace Boilerplate.Tests.Features.Identity;

/// <summary>
/// What the ungated 2fa read is allowed to return, and when.
/// <para>
/// The read itself is deliberately not gated - the settings page issues one on every visit, so
/// <see cref="AuthPolicies.ELEVATED_ACCESS"/> on the endpoint would prompt for a code just to LOOK at the tab, and
/// <see cref="AccountSelfServiceSecurityTests"/> pins that openness on purpose. That makes the RESPONSE the only place
/// the shared key can be protected, and this file is where that is asserted.
/// </para>
/// <para>
/// The shared key and the <c>otpauth://</c> uri ARE the second factor: anyone holding either can generate valid codes
/// indefinitely, and neither a password change, a security-stamp rotation nor revoking every session invalidates them.
/// Handing them to a caller who holds nothing but an access token defeats the factor that exists precisely to survive a
/// first-factor compromise. Suppressing only the QR image - which is what the code used to do - is no protection while
/// the secret sits next to it in the same json.
/// </para>
/// </summary>
[TestClass, TestCategory("IntegrationTest")]
public class TwoFactorSharedKeyExposureTests
{
    /// <summary>
    /// The finding: once two factor is ON, the enrolment material must not come back from an ungated read.
    /// </summary>
    [TestMethod]
    public async Task TwoFactorAuth_Should_NotReturnTheSharedKey_WhenTwoFactorIsAlreadyEnabled()
    {
        await using var server = await StartServer();
        await using var scope = server.WebApp.Services.CreateAsyncScope();
        await TestAccountUtils.CreateAndSignIn(server, scope, TestContext.CancellationToken);

        var userController = scope.ServiceProvider.GetRequiredService<IUserController>();

        // Enrol for real: read the key while 2fa is off, compute a live code from it, and turn 2fa on with that code.
        // Enable is deliberately not gated by elevated access - it already requires a valid code, which is stronger proof.
        var enrolment = await userController.TwoFactorAuth(new(), TestContext.CancellationToken);
        Assert.IsFalse(string.IsNullOrEmpty(enrolment.SharedKey), "Enrolment must be possible while 2fa is off.");

        var enabled = await userController.TwoFactorAuth(
            new() { Enable = true, TwoFactorCode = ComputeTotp(enrolment.SharedKey) }, TestContext.CancellationToken);

        Assert.IsTrue(enabled.IsTwoFactorEnabled, "Two factor should be on after enabling it with a valid code.");

        // The read an attacker holding a stolen access token would make.
        var read = await userController.TwoFactorAuth(new(), TestContext.CancellationToken);

        Assert.IsTrue(read.IsTwoFactorEnabled, "Sanity: the read must reflect that 2fa is on.");
        Assert.IsEmpty(read.SharedKey,
            "The shared key IS the second factor. Returned to a caller holding only an access token, it lets them generate " +
            "valid codes forever - and nothing the victim can do (password change, revoking sessions) invalidates it.");
        Assert.IsEmpty(read.AuthenticatorUri,
            "The otpauth uri embeds the raw secret, so withholding SharedKey while returning this changes nothing.");
        Assert.IsEmpty(read.QrCode,
            "The QR encodes the same uri; this was already suppressed and must stay suppressed.");
    }

    /// <summary>
    /// The capability the fix must NOT break, and the reason this test exists: enrolment reads the key from exactly this
    /// endpoint while 2fa is off. A fix that suppressed the key unconditionally would make two-factor impossible to set
    /// up, and the test above would still be green.
    /// </summary>
    [TestMethod]
    public async Task TwoFactorAuth_Should_StillReturnTheSharedKey_WhenTwoFactorIsOff()
    {
        await using var server = await StartServer();
        await using var scope = server.WebApp.Services.CreateAsyncScope();
        await TestAccountUtils.CreateAndSignIn(server, scope, TestContext.CancellationToken);

        var userController = scope.ServiceProvider.GetRequiredService<IUserController>();

        var read = await userController.TwoFactorAuth(new(), TestContext.CancellationToken);

        Assert.IsFalse(read.IsTwoFactorEnabled, "A fresh account has 2fa off.");
        Assert.IsFalse(string.IsNullOrEmpty(read.SharedKey), "The enrolling user needs the key - this is how they get it.");
        Assert.IsFalse(string.IsNullOrEmpty(read.AuthenticatorUri), "And the uri their authenticator app scans.");
        Assert.IsFalse(string.IsNullOrEmpty(read.QrCode), "And the QR image the settings page renders.");
    }

    /// <summary>
    /// The second path the fix could plausibly have broken: resetting the shared key also turns 2fa off, so the response
    /// must carry the NEW key for re-enrolment. This depends on an ordering that is invisible unless you look for it -
    /// the reset clears TwoFactorEnabled before the key is read - so it is worth pinning rather than re-deriving.
    /// </summary>
    [TestMethod]
    public async Task TwoFactorAuth_Should_ReturnANewSharedKey_AfterResettingIt()
    {
        await using var server = await StartServer();
        await using var scope = server.WebApp.Services.CreateAsyncScope();
        var (email, _) = await TestAccountUtils.CreateAndSignIn(server, scope, TestContext.CancellationToken);

        var userController = scope.ServiceProvider.GetRequiredService<IUserController>();

        var before = await userController.TwoFactorAuth(new(), TestContext.CancellationToken);

        // Resetting weakens the second factor, so unlike Enable it does require elevated access.
        await TestAccountUtils.Elevate(server, scope, email, TestContext.CancellationToken);

        var after = await userController.TwoFactorAuth(new() { ResetSharedKey = true }, TestContext.CancellationToken);

        Assert.IsFalse(after.IsTwoFactorEnabled, "Resetting the shared key also turns 2fa off.");
        Assert.IsFalse(string.IsNullOrEmpty(after.SharedKey),
            "Re-enrolment needs the new key in this very response, so the suppression must key off the POST-reset state.");
        Assert.AreNotEqual(before.SharedKey, after.SharedKey, "Resetting must actually produce a different key.");
    }


    /// <summary>
    /// Computes the current TOTP code for <paramref name="formattedSharedKey"/>, which the endpoint returns in the
    /// space-separated form the settings page displays.
    /// </summary>
    private static string ComputeTotp(string formattedSharedKey)
    {
        var unformatted = formattedSharedKey.Replace(" ", string.Empty).ToUpperInvariant();
        return new OtpNet.Totp(OtpNet.Base32Encoding.ToBytes(unformatted)).ComputeTotp();
    }

    private async Task<AppTestServer> StartServer()
    {
        var server = new AppTestServer();
        await server.Build(services => services.AddIntegrationApiOnlyTestsServices()).Start(TestContext.CancellationToken);
        return server;
    }

    public TestContext TestContext { get; set; } = default!;
}
