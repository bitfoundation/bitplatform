using OtpNet;

namespace Boilerplate.Tests.E2E.Features.Identity;

/// <summary>
/// Prints the global admin's current authenticator code so it can be pasted into a deployment's two factor prompt by hand.
/// </summary>
[TestClass, TestCategory(TestCategories.Api)]
public partial class GlobalAdminTwoFactorCodeTests
{
    public TestContext TestContext { get; set; } = default!;

    /// <summary>The next code comes along too, so one handed over with a second left on it is still usable.</summary>
    [TestMethod]
    public void GlobalAdminAuthenticatorKey_Should_ProduceATwoFactorCode()
    {
        var configuration = DeployedApiClientProvider.Services.GetRequiredService<IConfiguration>();

        var email = configuration["GlobalAdminEmail"];
        var authenticatorKey = configuration["GlobalAdminAuthenticatorKey"];

        Assert.IsFalse(string.IsNullOrWhiteSpace(authenticatorKey),
            "'GlobalAdminAuthenticatorKey' was found in neither this project's user secrets nor the environment variables.");

        byte[] sharedKey;
        try
        {
            sharedKey = Base32Encoding.ToBytes(authenticatorKey);
        }
        catch (ArgumentException exp)
        {
            throw new AssertFailedException($"'GlobalAdminAuthenticatorKey' is not the Base32 shared key an authenticator app enrolls with: {exp.Message}", exp);
        }

        var totp = new Totp(sharedKey);
        var code = totp.ComputeTotp();
        var remainingSeconds = totp.RemainingSeconds();
        var nextCode = totp.ComputeTotp(DateTimeOffset.UtcNow.AddSeconds(remainingSeconds + 1).UtcDateTime);

        TestContext.WriteLine($"Two factor code for '{email}': {code} (valid for {remainingSeconds}s, then {nextCode})");

        Assert.IsTrue(Regex.IsMatch(code, "^[0-9]{6}$"), $"A TOTP code is six digits, and this one is '{code}'.");
    }
}
