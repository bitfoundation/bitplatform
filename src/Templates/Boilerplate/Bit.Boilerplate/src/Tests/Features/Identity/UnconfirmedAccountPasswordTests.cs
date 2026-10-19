//+:cnd:noEmit
using Boilerplate.Shared.Features.Identity.Dtos;
//#if (captcha == "reCaptcha")
using System.Text;
using Boilerplate.Server.Api.Features.Identity.Services;
//#endif

namespace Boilerplate.Tests.Features.Identity;

/// <summary>
/// Someone who signs up with another person's e-mail never proves it, so their password must not survive that person's own sign up, code request or confirmation.
/// </summary>
[TestClass, TestCategory("IntegrationTest")]
public class UnconfirmedAccountPasswordTests
{
    private const string AttackerPassword = "hack123";
    private const string OwnerPassword = "ali456";

    public TestContext TestContext { get; set; } = default!;

    [TestMethod]
    public async Task SigningUpAndConfirming_Should_KeepThePassword()
    {
        await using var server = await StartServer();
        await using var client = server.CreateAppClient();
        var identityController = client.GetController<IIdentityController>();
        var email = MagicLinkSignInUtils.NewTestEmail();

        await identityController.SignUp(NewSignUpRequest(email, OwnerPassword), TestContext.CancellationToken);
        await ConfirmEmail(server, identityController, email);

        Assert.IsFalse(await client.AuthManager.SignIn(new() { Email = email, Password = OwnerPassword }, TestContext.CancellationToken));
    }

    [TestMethod]
    public async Task TheOwnersSignUp_Should_ReplaceThePasswordSetBeforeIt()
    {
        await using var server = await StartServer();
        await using var client = server.CreateAppClient();
        var identityController = client.GetController<IIdentityController>();
        var email = MagicLinkSignInUtils.NewTestEmail();

        await identityController.SignUp(NewSignUpRequest(email, AttackerPassword), TestContext.CancellationToken);
        await Assert.ThrowsAsync<KnownException>(() => identityController.SignUp(NewSignUpRequest(email, OwnerPassword), TestContext.CancellationToken));
        await ConfirmEmail(server, identityController, email);

        await Assert.ThrowsExactlyAsync<UnauthorizedException>(
            () => client.AuthManager.SignIn(new() { Email = email, Password = AttackerPassword }, TestContext.CancellationToken));
        Assert.IsFalse(await client.AuthManager.SignIn(new() { Email = email, Password = OwnerPassword }, TestContext.CancellationToken));
    }

    [TestMethod]
    public async Task TheOwnersCodeRequest_Should_RemoveThePasswordSetBeforeIt()
    {
        await using var server = await StartServer();
        await using var client = server.CreateAppClient();
        var identityController = client.GetController<IIdentityController>();
        var email = MagicLinkSignInUtils.NewTestEmail();

        await identityController.SignUp(NewSignUpRequest(email, AttackerPassword), TestContext.CancellationToken);
        await Assert.ThrowsAsync<KnownException>(() => identityController.SendOtp(new() { Email = email }, null, TestContext.CancellationToken));
        await ConfirmEmail(server, identityController, email);

        await Assert.ThrowsExactlyAsync<UnauthorizedException>(
            () => client.AuthManager.SignIn(new() { Email = email, Password = AttackerPassword }, TestContext.CancellationToken));
    }

    [TestMethod]
    public async Task SignUp_Should_RefuseAnEmailTogetherWithAPhoneNumber()
    {
        await using var server = await StartServer();
        await using var client = server.CreateAppClient();
        var identityController = client.GetController<IIdentityController>();

        var request = NewSignUpRequest(MagicLinkSignInUtils.NewTestEmail(), OwnerPassword);
        request.PhoneNumber = "+31684207362";

        await Assert.ThrowsExactlyAsync<ResourceValidationException>(() => identityController.SignUp(request, TestContext.CancellationToken));
    }

    private async Task ConfirmEmail(AppTestServer server, IIdentityController identityController, string email)
    {
        var captured = await server.WaitForCapturedEmail(email, e => e.Kind is CapturedEmailKind.EmailToken, TestContext.CancellationToken);

        await identityController.ConfirmEmail(new() { Email = email, Token = captured.Token }, TestContext.CancellationToken);
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

    private static SignUpRequestDto NewSignUpRequest(string email, string password) => new()
    {
        UserName = Guid.NewGuid().ToString(),
        Email = email,
        Password = password,
        //#if (captcha == "reCaptcha")
        GoogleRecaptchaResponse = "test"
        //#endif
    };
}
