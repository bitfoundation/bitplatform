using Boilerplate.Tests.PageTests.PageModels.Layout;

namespace Boilerplate.Tests.PageTests.PageModels.Identity;

public partial class SignInPage(IPage page, Uri serverAddress)
    : MainLayout(page, serverAddress, Urls.SignInPage, AppStrings.SignInTitle)
{
    public override async Task AssertOpen()
    {
        await base.AssertOpen();

        await Assertions.Expect(page.GetByRole(AriaRole.Main)).ToContainTextAsync(AppStrings.SignInPanelSubtitle);
        await Assertions.Expect(page.GetByPlaceholder(AppStrings.EmailPlaceholder)).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByPlaceholder(AppStrings.PasswordPlaceholder)).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByRole(AriaRole.Button, new() { Name = AppStrings.SignIn })).ToBeVisibleAsync();
        var forgotPassswordLink = page.GetByRole(AriaRole.Link, new() { Name = AppStrings.ForgotPasswordLink });
        await Assertions.Expect(forgotPassswordLink).ToBeVisibleAsync();
        await Assertions.Expect(forgotPassswordLink).ToHaveAttributeAsync("href", Urls.ForgotPasswordPage);
        await Assertions.Expect(page.GetByLabel(AppStrings.RememberMe)).ToBeCheckedAsync();
    }

    public async Task<IdentityLayout> SignIn(string email = "test@bitplatform.dev", string password = "123456")
    {
        await page.GetByPlaceholder(AppStrings.EmailPlaceholder).FillAsync(email);
        await page.GetByPlaceholder(AppStrings.PasswordPlaceholder).FillAsync(password);
        await page.GetByRole(AriaRole.Button, new() { Name = AppStrings.SignIn }).ClickAsync();

        return new(page, serverAddress, Urls.HomePage, AppStrings.HomeTitle);
    }

    public async Task AssertSignInFailed()
    {
        await Assertions.Expect(page.GetByText(AppStrings.InvalidUserCredentials)).ToBeVisibleAsync();
        await Assertions.Expect(page.Locator(".bit-prs")).ToBeHiddenAsync();
        await Assertions.Expect(page.GetByRole(AriaRole.Button, new() { Name = AppStrings.SignIn })).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByRole(AriaRole.Button, new() { Name = AppStrings.SignOut })).ToBeHiddenAsync();
    }
}
