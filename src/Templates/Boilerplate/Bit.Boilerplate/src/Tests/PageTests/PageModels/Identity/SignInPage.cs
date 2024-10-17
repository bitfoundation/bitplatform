using Boilerplate.Tests.PageTests.PageModels.Layout;
using Microsoft.AspNetCore.WebUtilities;

namespace Boilerplate.Tests.PageTests.PageModels.Identity;

public partial class SignInPage(IPage page, Uri serverAddress)
    : MainLayout(page, serverAddress)
{
    public override string PagePath => Urls.SignInPage;
    public override string PageTitle => AppStrings.SignInTitle;
    public string? ReturnUrl { private get; init; }

    public override async Task AssertOpen()
    {
        await base.AssertOpen();

        AssertReturnUrl();
        await Assertions.Expect(Page.GetByRole(AriaRole.Main)).ToContainTextAsync(AppStrings.SignInPanelSubtitle);
        await Assertions.Expect(Page.GetByPlaceholder(AppStrings.EmailPlaceholder)).ToBeVisibleAsync();
        await Assertions.Expect(Page.GetByPlaceholder(AppStrings.PasswordPlaceholder)).ToBeVisibleAsync();
        await Assertions.Expect(Page.GetByRole(AriaRole.Button, new() { Name = AppStrings.SignIn })).ToBeVisibleAsync();
        var forgotPassswordLink = Page.GetByRole(AriaRole.Link, new() { Name = AppStrings.ForgotPasswordLink });
        await Assertions.Expect(forgotPassswordLink).ToBeVisibleAsync();
        await Assertions.Expect(forgotPassswordLink).ToHaveAttributeAsync("href", Urls.ForgotPasswordPage);
        await Assertions.Expect(Page.GetByLabel(AppStrings.RememberMe)).ToBeCheckedAsync();
    }

    public async Task<IdentityHomePage> SignIn(string email = "test@bitplatform.dev", string password = "123456")
    {
        return await SignIn<IdentityHomePage>(email, password);
    }

    public async Task<TPage> SignIn<TPage>(string email = "test@bitplatform.dev", string password = "123456")
        where TPage : IdentityLayout
    {
        await Page.GetByPlaceholder(AppStrings.EmailPlaceholder).FillAsync(email);
        await Page.GetByPlaceholder(AppStrings.PasswordPlaceholder).FillAsync(password);
        await Page.GetByRole(AriaRole.Button, new() { Name = AppStrings.SignIn }).ClickAsync();

        AssertReturnUrl();
        if (ReturnUrl is null)
            Assert.AreEqual(typeof(TPage), typeof(IdentityHomePage));

        return (TPage)Activator.CreateInstance(typeof(TPage), Page, WebAppServerAddress)!;
    }

    public async Task AssertSignInFailed()
    {
        await Assertions.Expect(Page.GetByText(AppStrings.InvalidUserCredentials)).ToBeVisibleAsync();
        await Assertions.Expect(Page.Locator(".bit-prs.persona")).ToBeHiddenAsync();
        await Assertions.Expect(Page.GetByRole(AriaRole.Button, new() { Name = AppStrings.SignIn })).ToBeVisibleAsync();
        await Assertions.Expect(Page.GetByRole(AriaRole.Button, new() { Name = AppStrings.SignOut })).ToBeHiddenAsync();
    }

    public override async Task AssertSignOut()
    {
        await Assertions.Expect(Page.Locator(".bit-prs.persona")).ToBeHiddenAsync();
        await Assertions.Expect(Page.GetByRole(AriaRole.Button, new() { Name = AppStrings.SignOut })).ToBeHiddenAsync();

        if (ReturnUrl is null)
            await Assertions.Expect(Page).ToHaveURLAsync(new Uri(WebAppServerAddress, Urls.SignInPage).ToString());
        else
            await Assertions.Expect(Page).ToHaveURLAsync(new Uri(WebAppServerAddress, $"{Urls.SignInPage}?return-url={ReturnUrl}").ToString());
    }

    private void AssertReturnUrl()
    {
        var returnUrl = QueryHelpers.ParseQuery(new Uri(Page.Url).Query).GetValueOrDefault("return-url").ToString();
        if (ReturnUrl is null)
            Assert.AreEqual(string.Empty, returnUrl);
        else
            Assert.AreEqual(ReturnUrl, returnUrl);
    }
}
