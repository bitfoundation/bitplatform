using Boilerplate.Tests.PageTests.PageModels.Layout;
using Microsoft.AspNetCore.WebUtilities;

namespace Boilerplate.Tests.PageTests.PageModels.Identity;

public partial class SignInPage(IPage page, Uri serverAddress)
    : MainLayout(page, serverAddress)
{
    public override string PagePath => Urls.SignInPage;
    public override string PageTitle => AppStrings.SignInPageTitle;
    public string? ReturnUrl { private get; init; }

    public override async Task AssertOpen()
    {
        await base.AssertOpen();
        await AssertTab(Tab.Email);
    }

    public async Task ClickOnEmailTab()
    {
        await Page.GetByRole(AriaRole.Tab, new() { Name = AppStrings.Email }).ClickAsync();
    }

    public async Task ClickOnPhoneTab()
    {
        await Page.GetByRole(AriaRole.Tab, new() { Name = AppStrings.PhoneNumber }).ClickAsync();
    }

    public async Task AssertEmailTab()
    {
        await AssertTab(Tab.Email);
    }

    public async Task AssertPhoneTab()
    {
        await AssertTab(Tab.Phone);
    }

    private async Task AssertTab(Tab tab)
    {
        AssertReturnUrl();
        await Assertions.Expect(Page.GetByRole(AriaRole.Main)).ToContainTextAsync(AppStrings.SignInPanelSubtitle);
        switch (tab)
        {
            case Tab.Email:
                await Assertions.Expect(Page.GetByPlaceholder(AppStrings.EmailPlaceholder)).ToBeVisibleAsync();
                break;
            case Tab.Phone:
                await Assertions.Expect(Page.GetByPlaceholder(AppStrings.PhoneNumberPlaceholder)).ToBeVisibleAsync();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(tab), tab, null);
        }
        await Assertions.Expect(Page.GetByPlaceholder(AppStrings.PasswordPlaceholder)).ToBeVisibleAsync();
        await Assertions.Expect(Page.GetByRole(AriaRole.Button, new() { Name = AppStrings.SignIn })).ToBeVisibleAsync();
        var forgotPassswordLink = Page.GetByRole(AriaRole.Link, new() { Name = AppStrings.ForgotPasswordLink });
        await Assertions.Expect(forgotPassswordLink).ToBeVisibleAsync();
        await Assertions.Expect(forgotPassswordLink).ToHaveAttributeAsync("href", Urls.ForgotPasswordPage);
        await Assertions.Expect(Page.GetByLabel(AppStrings.RememberMe)).ToBeCheckedAsync();
    }

    public async Task<IdentityHomePage> SignInWithEmail(string email = TestData.DefaultTestEmail, string password = TestData.DefaultTestPassword)
    {
        return await SignInWithEmail<IdentityHomePage>(email, password);
    }

    public async Task<TPage> SignInWithEmail<TPage>(string email = TestData.DefaultTestEmail, string password = TestData.DefaultTestPassword)
        where TPage : IdentityLayout
    {
        return await SignInCore<TPage>(Tab.Email, email, password);
    }

    public async Task<IdentityHomePage> SignInWithPhone(string phone, string password = TestData.DefaultTestPassword)
    {
        return await SignInWithPhone<IdentityHomePage>(phone, password);
    }

    public async Task<TPage> SignInWithPhone<TPage>(string phone, string password = TestData.DefaultTestPassword)
        where TPage : IdentityLayout
    {
        return await SignInCore<TPage>(Tab.Phone, phone, password);
    }

    private async Task<TPage> SignInCore<TPage>(Tab tab, string emailOrPhone, string password)
        where TPage : IdentityLayout
    {
        switch (tab)
        {
            case Tab.Email:
                await Page.GetByPlaceholder(AppStrings.EmailPlaceholder).FillAsync(emailOrPhone);
                break;
            case Tab.Phone:
                await Page.GetByPlaceholder(AppStrings.PhoneNumberPlaceholder).FillAsync(emailOrPhone);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(tab), tab, null);
        }
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

    private enum Tab
    {
        Email,
        Phone
    }
}
