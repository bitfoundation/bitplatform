using OtpNet;
using Microsoft.AspNetCore.Identity;
using Boilerplate.Shared.Features.Identity.Dtos;

namespace Boilerplate.Tests.Features.Identity;

[TestClass, TestCategory("IntegrationTest")]
public class IdentityCodeHardeningTests
{
    private const string Password = "P@ssw0rdP@ssw0rd";

    public TestContext TestContext { get; set; } = default!;

    [TestMethod]
    public async Task ACodeComputedFromTheSecurityStamp_Should_NotSignIn()
    {
        await using var server = new AppTestServer();
        await server.Build().Start(TestContext.CancellationToken);
        await using var client = server.CreateAppClient();

        var (email, userId) = await TestAccountUtils.CreateAndSignIn(client, TestContext.CancellationToken);

        var identityController = client.GetController<IIdentityController>();

        await identityController.SendOtp(new() { Email = email }, null, TestContext.CancellationToken);

        var otpEmail = await server.WaitForCapturedEmail(email, e => e.Kind is CapturedEmailKind.Otp, TestContext.CancellationToken);

        string codeComputedFromTheSecurityStamp;

        await using (var scope = server.ApiApp.Services.CreateAsyncScope())
        {
            var user = await scope.ServiceProvider.GetRequiredService<AppDbContext>().Users.SingleAsync(u => u.Id == userId, TestContext.CancellationToken);

            var stockUserManager = ActivatorUtilities.CreateInstance<UserManager<User>>(scope.ServiceProvider);

            codeComputedFromTheSecurityStamp = await stockUserManager.GenerateUserTokenAsync(user, TokenOptions.DefaultPhoneProvider,
                FormattableString.Invariant($"Otp_Email,{user.OtpRequestedOn?.ToUniversalTime()}"));
        }

        Assert.AreNotEqual(otpEmail.Token, codeComputedFromTheSecurityStamp,
            "The security stamp is inside every access and refresh token, so a code that can be computed from it can be computed by anyone who reads one.");

        await Assert.ThrowsExactlyAsync<UnauthorizedException>(
            () => identityController.SignIn(new() { Email = email, Otp = codeComputedFromTheSecurityStamp }, TestContext.CancellationToken));

        await identityController.SignIn(new() { Email = email, Otp = otpEmail.Token }, TestContext.CancellationToken);
    }

    [TestMethod]
    public async Task ATwoFactorCodeSentByEmail_Should_SignInOnlyOnce()
    {
        await using var server = new AppTestServer();
        await server.Build().Start(TestContext.CancellationToken);
        await using var client = server.CreateAppClient();

        var (email, userId) = await TestAccountUtils.CreateAndSignIn(client, TestContext.CancellationToken);

        await SetPassword(server, userId);
        await EnableTwoFactor(client);

        var authManager = client.AuthManager;
        var identityController = client.GetController<IIdentityController>();

        Assert.IsTrue(await authManager.SignIn(new() { Email = email, Password = Password }, TestContext.CancellationToken),
            "An account with two factor on must be challenged for the second factor.");

        await identityController.SendTwoFactorToken(new() { Email = email, Password = Password }, TestContext.CancellationToken);

        var twoFactorEmail = await server.WaitForCapturedEmail(email, e => e.Kind is CapturedEmailKind.TwoFactor, TestContext.CancellationToken);

        Assert.IsFalse(await authManager.SignIn(new() { Email = email, Password = Password, TwoFactorCode = twoFactorEmail.Token }, TestContext.CancellationToken),
            "The code from the e-mail must complete the sign in.");

        await Assert.ThrowsExactlyAsync<UnauthorizedException>(
            () => authManager.SignIn(new() { Email = email, Password = Password, TwoFactorCode = twoFactorEmail.Token }, TestContext.CancellationToken),
            "A two factor code that has already signed in once must not sign in again.");
    }

    [TestMethod]
    public async Task ResetPassword_Should_AskForTheSecondFactor_WhenTwoFactorIsOn()
    {
        await using var server = new AppTestServer();
        await server.Build().Start(TestContext.CancellationToken);
        await using var client = server.CreateAppClient();

        var (email, userId) = await TestAccountUtils.CreateAndSignIn(client, TestContext.CancellationToken);

        var recoveryCodes = await EnableTwoFactor(client);

        var identityController = client.GetController<IIdentityController>();

        await identityController.SendResetPasswordToken(new() { Email = email }, TestContext.CancellationToken);

        var resetPasswordEmail = await server.WaitForCapturedEmail(email, e => e.Kind is CapturedEmailKind.ResetPassword, TestContext.CancellationToken);

        ResetPasswordRequestDto NewRequest(string? twoFactorCode) => new()
        {
            Email = email,
            Token = resetPasswordEmail.Token,
            Password = Password,
            ConfirmPassword = Password,
            TwoFactorCode = twoFactorCode
        };

        var withoutTheSecondFactor = await Assert.ThrowsExactlyAsync<BadRequestException>(
            () => identityController.ResetPassword(NewRequest(null), TestContext.CancellationToken),
            "Whoever reads the account's inbox or SMS must not be able to replace the password of an account with two factor on.");

        Assert.AreEqual(nameof(AppStrings.ResetPasswordTwoFactorCodeRequired), withoutTheSecondFactor.Key);

        await Assert.ThrowsExactlyAsync<BadRequestException>(
            () => identityController.ResetPassword(NewRequest("not-a-code"), TestContext.CancellationToken));

        await identityController.ResetPassword(NewRequest(recoveryCodes[0]), TestContext.CancellationToken);

        Assert.IsTrue(await client.AuthManager.SignIn(new() { Email = email, Password = Password }, TestContext.CancellationToken),
            "The new password must be accepted, and the account must still ask for its second factor.");

        await using var scope = server.ApiApp.Services.CreateAsyncScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var user = await userManager.FindByIdAsync(userId.ToString());

        Assert.AreEqual(recoveryCodes.Length - 1, await userManager.CountRecoveryCodesAsync(user!), "The recovery code that reset the password must be spent.");
    }

    private async Task SetPassword(AppTestServer server, Guid userId)
    {
        await using var scope = server.ApiApp.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = await dbContext.Users.SingleAsync(u => u.Id == userId, TestContext.CancellationToken);
        user.PasswordHash = new PasswordHasher<User>().HashPassword(user, Password);
        await dbContext.SaveChangesAsync(TestContext.CancellationToken);
    }

    private async Task<string[]> EnableTwoFactor(AppClient client)
    {
        var userController = client.GetController<IUserController>();

        var enrolment = await userController.TwoFactorAuth(new(), TestContext.CancellationToken);
        var sharedKey = enrolment.SharedKey!.Replace(" ", "");

        var enabled = await userController.TwoFactorAuth(new()
        {
            Enable = true,
            TwoFactorCode = new Totp(Base32Encoding.ToBytes(sharedKey)).ComputeTotp()
        }, TestContext.CancellationToken);

        Assert.IsTrue(enabled.IsTwoFactorEnabled, "Two factor has to be on before the test can rely on it.");

        return enabled.RecoveryCodes!;
    }
}
