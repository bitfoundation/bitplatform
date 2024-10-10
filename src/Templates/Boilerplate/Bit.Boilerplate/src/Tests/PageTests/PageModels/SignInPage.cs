namespace Boilerplate.Tests.PageTests.PageModels;

public partial class SignInPage(IPage page, Uri serverAddress)
    : MainLayout(page, serverAddress, Urls.SignInPage, AppStrings.SignInTitle)
{
    public async Task<IdentityLayout> SignIn(string email = "test@bitplatform.dev", string password = "123456")
    {
        await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
        await page.GetByPlaceholder(AppStrings.EmailPlaceholder).FillAsync(email);
        await page.GetByPlaceholder(AppStrings.PasswordPlaceholder).FillAsync(password);
        await page.GetByRole(AriaRole.Button, new() { Name = AppStrings.SignIn }).ClickAsync();

        return new(page, serverAddress, Urls.HomePage, AppStrings.HomeTitle);
    }

    public async Task AssertSignInFailed()
    {
        await Assertions.Expect(page.GetByText(AppStrings.InvalidUserCredentials)).ToBeVisibleAsync();
        await Assertions.Expect(page.Locator(".persona")).ToBeHiddenAsync();
        await Assertions.Expect(page.GetByRole(AriaRole.Button, new() { Name = AppStrings.SignIn })).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByRole(AriaRole.Button, new() { Name = AppStrings.SignOut })).ToBeHiddenAsync();
    }
}
