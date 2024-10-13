using Boilerplate.Tests.PageTests.PageModels.Layout;

namespace Boilerplate.Tests.PageTests.PageModels.Identity;

public partial class SignInPage(IPage page, Uri serverAddress)
    : MainLayout(page, serverAddress, Urls.SignInPage, AppStrings.SignInTitle)
{
    public async Task<IdentityLayout> SignIn(string email = "test@bitplatform.dev", string password = "123456")
    {
        //Ensure the page is completely loaded
        await Assertions.Expect(page.GetByPlaceholder(AppStrings.EmailPlaceholder)).ToBeVisibleAsync();

        await page.GetByPlaceholder(AppStrings.EmailPlaceholder).FillAsync(email);
        await page.GetByPlaceholder(AppStrings.PasswordPlaceholder).FillAsync(password);
        await page.GetByRole(AriaRole.Button, new() { Name = AppStrings.SignIn }).ClickAsync();

        return new(page, serverAddress, Urls.HomePage, AppStrings.HomeTitle);
    }

    public async Task AssetNotSignedIn()
    {
        await Assertions.Expect(page.GetByText(AppStrings.InvalidUserCredentials)).ToBeVisibleAsync();
        await Assertions.Expect(page.Locator(".bit-prs")).ToBeHiddenAsync();
        await Assertions.Expect(page.GetByRole(AriaRole.Button, new() { Name = AppStrings.SignIn })).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByRole(AriaRole.Button, new() { Name = AppStrings.SignOut })).ToBeHiddenAsync();
    }
}
