//+:cnd:noEmit
using Boilerplate.Tests.Services;
using Boilerplate.Server.Api.Data;
using Boilerplate.Tests.PageTests.PageModels;
using Boilerplate.Tests.PageTests.PageModels.Identity;
using Boilerplate.Tests.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Boilerplate.Tests.PageTests.BlazorServer;

[TestClass]
public partial class IdentityPagesTests : PageTestBase
{
    [TestMethod]
    public async Task UnauthorizedUser_Should_RedirectToSignInPage()
    {
        var response = await Page.GotoAsync(new Uri(WebAppServerAddress, Urls.SettingsPage).ToString());
        await Page.WaitForHydrationToComplete();

        Assert.IsNotNull(response);
        //NOTE: Status code differs between pre-render Disabled (200) and Enabled(401)
        //Once it resolved we can uncomment this line
        //Assert.AreEqual(StatusCodes.Status200OK, response.Status);

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
                var identityHomePage = await signInPage.SignInWithEmail();
                await identityHomePage.AssertSignInSuccess();
                break;
            case "InvalidCredentials":
                await signInPage.SignInWithEmail(email: "invalid@bitplatform.dev", password: "invalid");
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

        var identityHomePage = await signInPage.SignInWithEmail(email);
        await identityHomePage.AssertSignInSuccess(email, userFullName: null);

        await dbContext.Entry(user).Reference(u => u.Sessions).LoadAsync();
        Assert.AreEqual(1, user.Sessions.Count);

        var mainHomePage = await identityHomePage.SignOut();
        await mainHomePage.AssertSignOut();

        await dbContext.Entry(user).Reference(u => u.Sessions).LoadAsync();
        Assert.AreEqual(0, user.Sessions.Count);
    }

    [TestMethod]
    [DataRow("Token")]
    [DataRow("InvalidToken")]
    [DataRow("MagicLink")]
    public async Task SignUp(string mode)
    {
        var signupPage = new SignUpPage(Page, WebAppServerAddress);

        await signupPage.Open();
        await signupPage.AssertOpen();

        var email = $"{Guid.NewGuid()}@gmail.com";
        var confirmPage = await signupPage.SignUp(email);
        await confirmPage.AssertOpen();

        IdentityHomePage identityHomePage;
        switch (mode)
        {
            case "Token":
                var confirmationEmail = await signupPage.OpenConfirmationEmail();
                await confirmationEmail.AssertContent();
                var token = await confirmationEmail.GetToken();
                identityHomePage = await confirmPage.ConfirmByToken(token);
                break;
            case "MagicLink":
                confirmationEmail = await signupPage.OpenConfirmationEmail();
                await confirmationEmail.AssertContent();
                identityHomePage = await confirmationEmail.OpenMagicLink<IdentityHomePage>();
                break;
            case "InvalidToken":
                await confirmPage.ConfirmByToken("111111");
                await confirmPage.AssertInvalidToken();
                return;
            default:
                throw new NotSupportedException();
        }

        await identityHomePage.AssertOpen();
        await identityHomePage.AssertSignInSuccess(email, userFullName: null);
    }

    [TestMethod]
    [DataRow("Token")]
    [DataRow("InvalidToken")]
    [DataRow("MagicLink")]
    [DataRow("TooManyRequests")]
    [DataRow("NotExisted")]
    public async Task ForgotPassword(string mode)
    {
        var email = await CreateNewUser();

        var forgotPasswordPage = new ForgotPasswordPage(Page, WebAppServerAddress);

        await forgotPasswordPage.Open();
        await forgotPasswordPage.AssertOpen();

        if (mode is "NotExisted")
        {
            await forgotPasswordPage.ForgotPassword("not-existed@bitplatform.dev");
            await forgotPasswordPage.AssertUserNotFound();
            return;
        }

        var resetPasswordPage = await forgotPasswordPage.ForgotPassword(email);
        await resetPasswordPage.AssertOpen();

        const string newPassword = "new_password";
        switch (mode)
        {
            case "Token":
                var resetPasswordEmail = await forgotPasswordPage.OpenResetPasswordEmail();
                await resetPasswordEmail.AssertContent();
                var token = await resetPasswordEmail.GetToken();
                await resetPasswordPage.ContinueByToken(token);
                break;
            case "MagicLink":
                resetPasswordEmail = await forgotPasswordPage.OpenResetPasswordEmail();
                await resetPasswordEmail.AssertContent();
                resetPasswordPage = await resetPasswordEmail.OpenMagicLink();
                break;
            case "InvalidToken":
                await resetPasswordPage.ContinueByToken("111111");
                await resetPasswordPage.SetPassword(newPassword);
                await resetPasswordPage.AssertInvalidToken();
                return;
            case "TooManyRequests":
                await Page.GoBackAsync();
                await forgotPasswordPage.ForgotPassword(email);
                await forgotPasswordPage.AssertTooManyRequests();
                return;
            default:
                throw new NotSupportedException();
        }
        await resetPasswordPage.AssertValidToken();

        await resetPasswordPage.SetPassword(newPassword);
        await resetPasswordPage.AssertSetPassword();

        var signInPage = new SignInPage(Page, WebAppServerAddress);

        await signInPage.Open();
        await signInPage.AssertOpen();

        var identityHomePage = await signInPage.SignInWithEmail(email, newPassword);
        await identityHomePage.AssertSignInSuccess(email, userFullName: null);
    }

    [TestMethod]
    [DataRow("Token")]
    [DataRow("InvalidToken")]
    [DataRow("MagicLink")]
    [DataRow("TooManyRequests")]
    public async Task ChangeEmail(string mode)
    {
        var email = await CreateNewUser();

        var signInPage = new SignInPage(Page, WebAppServerAddress);

        await signInPage.Open();
        await signInPage.AssertOpen();

        var identityHomePage = await signInPage.SignInWithEmail(email);
        await identityHomePage.AssertSignInSuccess(email, userFullName: null);

        var settingsPage = new SettingsPage(Page, WebAppServerAddress);

        await settingsPage.Open();
        await settingsPage.AssertOpen();

        await settingsPage.ExpandAccount();
        await settingsPage.AssertExpandAccount(email);

        var newEmail = $"{Guid.NewGuid()}@gmail.com";
        await settingsPage.ChangeEmail(newEmail);
        await settingsPage.AssertChangeEmail();

        switch (mode)
        {
            case "Token":
                var confirmationEmail = await settingsPage.OpenConfirmationEmail();
                await confirmationEmail.AssertContent();
                var token = await confirmationEmail.GetToken();
                await settingsPage.ConfirmEmailByToken(token);
                break;
            case "MagicLink":
                confirmationEmail = await settingsPage.OpenConfirmationEmail();
                await confirmationEmail.AssertContent();
                settingsPage = await confirmationEmail.OpenMagicLink();
                break;
            case "InvalidToken":
                await settingsPage.ConfirmEmailByToken("111111");
                await settingsPage.AssertEmailInvalidToken();
                return;
            case "TooManyRequests":
                await settingsPage.ClickOnPhoneTab();
                await settingsPage.ClickOnEmailTab();
                await settingsPage.ChangeEmail(newEmail);
                await settingsPage.AssertTooManyRequestsForChangeEmail();
                return;
            default:
                throw new NotSupportedException();
        }
        await settingsPage.AssertConfirmEmailSuccess();

        signInPage = await settingsPage.SignOut();
        await signInPage.AssertOpen();
        await signInPage.AssertSignOut();

        await signInPage.SignInWithEmail(email);
        await signInPage.AssertSignInFailed();

        settingsPage = await signInPage.SignInWithEmail<SettingsPage>(newEmail);
        await settingsPage.AssertSignInSuccess(newEmail, userFullName: null);
    }

    [TestMethod]
    [DataRow("Token")]
    [DataRow("InvalidToken")]
    [DataRow("TooManyRequests")]
    public async Task ChangePhone(string mode)
    {
        var email = await CreateNewUser();

        var signInPage = new SignInPage(Page, WebAppServerAddress);

        await signInPage.Open();
        await signInPage.AssertOpen();

        var identityHomePage = await signInPage.SignInWithEmail(email);
        await identityHomePage.AssertSignInSuccess(email, userFullName: null);

        var settingsPage = new SettingsPage(Page, WebAppServerAddress);

        await settingsPage.Open();
        await settingsPage.AssertOpen();

        await settingsPage.ExpandAccount();
        await settingsPage.ClickOnPhoneTab();
        await settingsPage.AssertPhoneTab(null);

        var phone = $"+1{Random.Shared.Next(1111111111, int.MaxValue)}";
        await settingsPage.ChangePhone(phone);
        await settingsPage.AssertChangePhone();

        switch (mode)
        {
            case "Token":
                var token = settingsPage.GetPhoneToken();
                await settingsPage.ConfirmPhoneByToken(token);
                await settingsPage.AssertConfirmPhoneSuccess();

                signInPage = await settingsPage.SignOut();
                await signInPage.AssertOpen();
                await signInPage.AssertSignOut();

                await signInPage.ClickOnPhoneTab();
                await signInPage.AssertPhoneTab();

                settingsPage = await signInPage.SignInWithPhone<SettingsPage>(phone);
                await settingsPage.AssertSignInSuccess(email, userFullName: null);
                return;
            case "InvalidToken":
                await settingsPage.ConfirmPhoneByToken("111111");
                await settingsPage.AssertPhoneInvalidToken();
                return;
            case "TooManyRequests":
                await settingsPage.ClickOnEmailTab();
                await settingsPage.ClickOnPhoneTab();
                await settingsPage.ChangePhone(phone);
                await settingsPage.AssertTooManyRequestsForChangePhone();
                return;
            default:
                throw new NotSupportedException();
        }
    }

    [TestMethod]
    public async Task DeleteUser()
    {
        await using var scope = TestServer.WebApp.Services.CreateAsyncScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userService = new UserService(dbContext);
        var email = $"{Guid.NewGuid()}@gmail.com";
        await userService.AddUser(email);

        var signInPage = new SignInPage(Page, WebAppServerAddress);

        await signInPage.Open();
        await signInPage.AssertOpen();

        var identityHomePage = await signInPage.SignInWithEmail(email);
        await identityHomePage.AssertSignInSuccess(email, userFullName: null);

        var settingsPage = new SettingsPage(Page, WebAppServerAddress);

        await settingsPage.Open();
        await settingsPage.AssertOpen();

        await settingsPage.ExpandAccount();
        await settingsPage.ClickOnDeleteTab();

        signInPage = await settingsPage.DeleteUser();
        await signInPage.AssertSignOut();

        var exists = await dbContext.Users.AnyAsync(u => u.Email == email);
        Assert.IsFalse(exists, "User must be deleted.");
    }

    private async Task<string> CreateNewUser()
    {
        await using var scope = TestServer.WebApp.Services.CreateAsyncScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userService = new UserService(dbContext);
        var email = $"{Guid.NewGuid()}@gmail.com";
        await userService.AddUser(email);

        return email;
    }
}
