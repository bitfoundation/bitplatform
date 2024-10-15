//+:cnd:noEmit
using Boilerplate.Tests.PageTests.PageModels.Identity;
using Boilerplate.Server.Api.Data;
using Boilerplate.Tests.PageTests.PageModels.Layout;
using Boilerplate.Tests.Services;

namespace Boilerplate.Tests.PageTests;

[TestClass]
public partial class IdentityPagesTests : PageTestBase
{
    [TestMethod]
    public async Task UnauthorizedUser_Should_RedirectToSignInPage()
    {
        var response = await Page.GotoAsync(new Uri(WebAppServerAddress, Urls.SettingsPage).ToString());

        Assert.IsNotNull(response);
        Assert.AreEqual(StatusCodes.Status200OK, response.Status);

        await Expect(Page).ToHaveURLAsync(new Uri(WebAppServerAddress, "/sign-in?return-url=settings").ToString());
    }

    [TestMethod]
    [DataRow("ValidCredentials")]
    [DataRow("InvalidCredentials")]
    public async Task SignIn(string mode)
    {
        var signInPage = new SignInPage(Page, WebAppServerAddress);

        await signInPage.Open();
        await signInPage.AssertOpen();

        switch (mode)
        {
            case "ValidCredentials":
                var signedInPage = await signInPage.SignIn();
                await signedInPage.AssertSignInSuccess();
                break;
            case "InvalidCredentials":
                await signInPage.SignIn(email: "invalid@bitplatform.dev", password: "invalid");
                await signInPage.AssertSignInFailed();
                break;
            default:
                throw new NotSupportedException();
        }
    }

    [TestMethod]
    public async Task SignOut()
    {
        await using var scope = TestServer.WebApp.Services.CreateAsyncScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userService = new UserService(dbContext);

        var email = $"{Guid.NewGuid()}@gmail.com";
        var user = await userService.AddUser(email);

        var signInPage = new SignInPage(Page, WebAppServerAddress);

        await signInPage.Open();
        await signInPage.AssertOpen();

        var signedInPage = await signInPage.SignIn(email);
        await signedInPage.AssertSignInSuccess(email, null);

        await dbContext.Entry(user).ReloadAsync();
        Assert.AreEqual(1, user.Sessions.Count);

        await signedInPage.SignOut();
        await signedInPage.AssertSignOut();

        await dbContext.Entry(user).ReloadAsync();
        Assert.AreEqual(0, user.Sessions.Count);
    }

    [TestMethod]
    [DataRow("Token")]
    [DataRow("MagicLink")]
    public async Task SignUp(string mode)
    {
        var signupPage = new SignUpPage(Page, WebAppServerAddress);

        await signupPage.Open();
        await signupPage.AssertOpen();

        var email = $"{Guid.NewGuid()}@gmail.com";
        var confirmPage = await signupPage.SignUp(email);
        await confirmPage.AssertOpen();

        var confirmationEmail = await signupPage.OpenConfirmationEmail();
        await confirmationEmail.AssertContent();

        IdentityLayout signedInPage;
        switch (mode)
        {
            case "Token":
                var token = await confirmationEmail.GetToken();
                signedInPage = await confirmPage.ConfirmByToken(email: null, token);
                break;
            case "MagicLink":
                signedInPage = await confirmationEmail.OpenMagicLink();
                break;
            default:
                throw new NotSupportedException();
        }

        await signedInPage.AssertOpen();
        await signedInPage.AssertSignInSuccess(email, userFullName: null);
    }

    [TestMethod]
    [DataRow("Token")]
    [DataRow("InvalidToken")]
    [DataRow("MagicLink")]
    public async Task ForgotPassword(string mode)
    {
        await using var scope = TestServer.WebApp.Services.CreateAsyncScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userService = new UserService(dbContext);
        var email = $"{Guid.NewGuid()}@gmail.com";
        await userService.AddUser(email);

        var forgotPasswordPage = new ForgotPasswordPage(Page, WebAppServerAddress);

        await forgotPasswordPage.Open();
        await forgotPasswordPage.AssertOpen();

        var resetPasswordPage = await forgotPasswordPage.ForgotPassword(email);
        await resetPasswordPage.AssertOpen();

        var resetPasswordEmail = await forgotPasswordPage.OpenResetPasswordEmail();
        await resetPasswordEmail.AssertContent();

        const string newPassword = "new_password";
        switch (mode)
        {
            case "Token":
                var token = await resetPasswordEmail.GetToken();
                await resetPasswordPage.ContinueByToken(email: null, token);
                break;
            case "InvalidToken":
                await resetPasswordPage.ContinueByToken(email: null, "111111");
                await resetPasswordPage.SetPassword(newPassword);
                await resetPasswordPage.AssertInvalidToken();
                return;
            case "MagicLink":
                resetPasswordPage = await resetPasswordEmail.OpenMagicLink();
                await resetPasswordPage.Continue();
                break;
            default:
                throw new NotSupportedException();
        }
        await resetPasswordPage.AssertValidToken();

        await resetPasswordPage.SetPassword(newPassword);
        await resetPasswordPage.AssertSetPassword();

        var signInPage = new SignInPage(Page, WebAppServerAddress);

        await signInPage.Open();
        await signInPage.AssertOpen();

        var signedInPage = await signInPage.SignIn(email, newPassword);
        await signedInPage.AssertSignInSuccess(email, userFullName: null);
    }
}
