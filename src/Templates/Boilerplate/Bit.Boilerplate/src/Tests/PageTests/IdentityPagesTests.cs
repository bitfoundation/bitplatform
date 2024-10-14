//+:cnd:noEmit
using Boilerplate.Tests.PageTests.PageModels.Identity;
using Boilerplate.Server.Api.Models.Identity;
using Boilerplate.Server.Api.Data;

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
    public async Task SignIn_Should_Work_With_ValidCredentials()
    {
        var signInPage = new SignInPage(Page, WebAppServerAddress);

        await signInPage.Open();
        await signInPage.AssertOpen();

        var signedInPage = await signInPage.SignIn();
        await signedInPage.AssertSignInSuccess();
    }

    [TestMethod]
    public async Task SignIn_Should_Fail_With_InvalidCredentials()
    {
        var signInPage = new SignInPage(Page, WebAppServerAddress);

        await signInPage.Open();
        await signInPage.AssertOpen();

        await signInPage.SignIn(email: "invalid@bitplatform.dev", password: "invalid");
        await signInPage.AssetNotSignedIn();
    }

    [TestMethod]
    public async Task SignOut_Should_WorkAsExpected()
    {
        await using var scope = TestServer.WebApp.Services.CreateAsyncScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var email = $"{Guid.NewGuid()}@gmail.com";
        var user = new User
        {
            EmailConfirmed = true,
            UserName = email,
            NormalizedUserName = email.ToUpperInvariant(),
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            SecurityStamp = "959ff4a9-4b07-4cc1-8141-c5fc033daf83",
            ConcurrencyStamp = "315e1a26-5b3a-4544-8e91-2760cd28e231",
            PasswordHash = "AQAAAAIAAYagAAAAEP0v3wxkdWtMkHA3Pp5/JfS+42/Qto9G05p2mta6dncSK37hPxEHa3PGE4aqN30Aag==", // 123456
        };

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var signInPage = new SignInPage(Page, WebAppServerAddress);

        await signInPage.Open();
        await signInPage.AssertOpen();

        var signedInPage = await signInPage.SignIn(email);
        await signedInPage.AssertSignInSuccess(email);

        await dbContext.Entry(user).ReloadAsync();
        Assert.AreEqual(1, user.Sessions.Count);

        await signedInPage.SignOut();
        await signedInPage.AssertSignOut();

        await dbContext.Entry(user).ReloadAsync();
        Assert.AreEqual(0, user.Sessions.Count);
    }

    [TestMethod]
    public async Task SignUp_Should_Work_With_MagicLink()
    {
        var signupPage = new SignUpPage(Page, WebAppServerAddress);

        await signupPage.Open();
        await signupPage.AssertOpen();

        await signupPage.SignUp();
        await signupPage.AssertSignUp();

        await signupPage.OpenEmail();
        await signupPage.AssertConfirmationEmailContent();

        await signupPage.ConfirmByMagicLink();
        await signupPage.AssertConfirm();
    }

    [TestMethod]
    public async Task SignUp_Should_Work_With_OtpCode()
    {
        var signupPage = new SignUpPage(Page, WebAppServerAddress);

        await signupPage.Open();
        await signupPage.AssertOpen();

        await signupPage.SignUp();
        await signupPage.AssertSignUp();

        await signupPage.OpenEmail();
        await signupPage.AssertConfirmationEmailContent();

        await signupPage.ConfirmByOtp();
        await signupPage.AssertConfirm();
    }
}
