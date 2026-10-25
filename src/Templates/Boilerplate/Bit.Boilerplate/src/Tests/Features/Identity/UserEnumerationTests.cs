//+:cnd:noEmit
using Boilerplate.Shared.Features.Identity.Dtos;
//#if (captcha == "reCaptcha")
using System.Text;
using Boilerplate.Server.Api.Features.Identity.Services;
//#endif

namespace Boilerplate.Tests.Features.Identity;

[TestClass, TestCategory("IntegrationTest")]
public class UserEnumerationTests
{
    public TestContext TestContext { get; set; } = default!;

    [TestMethod]
    public async Task TheSendTokenEndpoints_Should_AnswerAnUnknownEmailLikeAnExistingOne()
    {
        await using var server = await StartServer();
        await using var client = server.CreateAppClient();

        var (email, _) = await TestAccountUtils.CreateAndSignIn(client, TestContext.CancellationToken);
        var unknownEmail = MagicLinkSignInUtils.NewTestEmail();

        var identityController = client.GetController<IIdentityController>();

        await identityController.SendResetPasswordToken(new() { Email = unknownEmail }, TestContext.CancellationToken);
        await identityController.SendResetPasswordToken(new() { Email = email }, TestContext.CancellationToken);

        await identityController.SendConfirmEmailToken(new() { Email = unknownEmail }, TestContext.CancellationToken);
        await identityController.SendConfirmEmailToken(new() { Email = email }, TestContext.CancellationToken);

        var forTheUnknownEmail = await Assert.ThrowsExactlyAsync<BadRequestException>(
            () => identityController.SendTwoFactorToken(new() { Email = unknownEmail, Password = "WrongP@ssw0rd" }, TestContext.CancellationToken));
        var forTheExistingEmail = await Assert.ThrowsExactlyAsync<BadRequestException>(
            () => identityController.SendTwoFactorToken(new() { Email = email, Password = "WrongP@ssw0rd" }, TestContext.CancellationToken));

        Assert.AreEqual(forTheExistingEmail.Key, forTheUnknownEmail.Key);
        Assert.AreEqual(forTheExistingEmail.Message, forTheUnknownEmail.Message);
    }

    [TestMethod]
    public async Task APasswordReset_Should_AnswerAnUnknownEmailLikeAnExistingOne_OnTheFollowingRequestsToo()
    {
        await using var server = await StartServer();
        await using var client = server.CreateAppClient();

        var (email, _) = await TestAccountUtils.CreateAndSignIn(client, TestContext.CancellationToken);
        var unknownEmail = MagicLinkSignInUtils.NewTestEmail();

        var identityController = client.GetController<IIdentityController>();

        await identityController.SendResetPasswordToken(new() { Email = unknownEmail }, TestContext.CancellationToken);
        await identityController.SendResetPasswordToken(new() { Email = email }, TestContext.CancellationToken);

        await Assert.ThrowsExactlyAsync<TooManyRequestsException>(
            () => identityController.SendResetPasswordToken(new() { Email = unknownEmail }, TestContext.CancellationToken),
            "Asking again right away is refused for a real account, so it has to be refused for an unknown one as well.");
        await Assert.ThrowsExactlyAsync<TooManyRequestsException>(
            () => identityController.SendResetPasswordToken(new() { Email = email }, TestContext.CancellationToken));

        ResetPasswordRequestDto WithAWrongCode(string address) => new()
        {
            Email = address,
            Token = "000000",
            Password = "P@ssw0rdP@ssw0rd",
            ConfirmPassword = "P@ssw0rdP@ssw0rd"
        };

        var forTheUnknownEmail = await Assert.ThrowsExactlyAsync<BadRequestException>(
            () => identityController.ResetPassword(WithAWrongCode(unknownEmail), TestContext.CancellationToken));
        var forTheExistingEmail = await Assert.ThrowsExactlyAsync<BadRequestException>(
            () => identityController.ResetPassword(WithAWrongCode(email), TestContext.CancellationToken));

        Assert.AreEqual(forTheExistingEmail.Key, forTheUnknownEmail.Key);
    }

    [TestMethod]
    public async Task SignUp_Should_NotRevealAnExistingAccount_AndTellItsOwnerInstead()
    {
        await using var server = await StartServer();
        await using var client = server.CreateAppClient();

        var (email, _) = await TestAccountUtils.CreateAndSignIn(client, TestContext.CancellationToken);

        var identityController = client.GetController<IIdentityController>();

        await identityController.SignUp(NewSignUpRequest(email), TestContext.CancellationToken);

        var notice = await server.WaitForCapturedEmail(email, e => e.Kind is CapturedEmailKind.AccountAlreadyExists, TestContext.CancellationToken);

        Assert.IsNotNull(notice.Link);
        Assert.Contains(PageUrls.SignIn, notice.Link.ToString());

        await identityController.SignUp(NewSignUpRequest(email), TestContext.CancellationToken);

        var notices = server.ApiApp.Services.GetRequiredService<EmailCaptureStore>().Captured
            .Count(e => e.IsTo(email) && e.Kind is CapturedEmailKind.AccountAlreadyExists);

        Assert.AreEqual(1, notices, "Signing up again right away must not mail the owner again, or sign up becomes a way to flood an inbox.");
    }

    private async Task<AppTestServer> StartServer()
    {
        var server = new AppTestServer();

        await server.Build(configureTestServices: services =>
        {
            //#if (captcha == "reCaptcha")
            services.AddHttpClient<GoogleRecaptchaService>().ConfigurePrimaryHttpMessageHandler(() => new ExternalSignInHealthChecksTests.RoutingHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"success":true}""", Encoding.UTF8, "application/json")
            })));
            //#endif
        }).Start(TestContext.CancellationToken);

        return server;
    }

    private static SignUpRequestDto NewSignUpRequest(string email) => new()
    {
        UserName = Guid.NewGuid().ToString(),
        Email = email,
        Password = "P@ssw0rdP@ssw0rd",
        //#if (captcha == "reCaptcha")
        GoogleRecaptchaResponse = "test"
        //#endif
    };
}
