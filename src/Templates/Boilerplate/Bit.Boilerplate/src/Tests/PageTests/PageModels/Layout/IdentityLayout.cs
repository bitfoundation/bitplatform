namespace Boilerplate.Tests.PageTests.PageModels.Layout;

public partial class IdentityLayout(IPage page, Uri serverAddress, string pagePath, string pageTitle)
    : MainLayout(page, serverAddress, pagePath, pageTitle)
{
    public async Task AssertSignInSuccess(string userFullName = "Boilerplate test account")
    {
        await Assertions.Expect(page).ToHaveURLAsync(serverAddress.ToString());
        await Assertions.Expect(page.GetByRole(AriaRole.Button, new() { Name = userFullName })).ToBeVisibleAsync();
        await Assertions.Expect(page.Locator(".bit-prs").First).ToContainTextAsync(userFullName);
        await Assertions.Expect(page.Locator(".bit-prs").Last).ToContainTextAsync(userFullName);
        await Assertions.Expect(page.GetByRole(AriaRole.Button, new() { Name = AppStrings.SignOut })).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByRole(AriaRole.Button, new() { Name = AppStrings.SignIn })).ToBeHiddenAsync();
    }

    public async Task SignOut()
    {
        await page.GetByRole(AriaRole.Button, new() { Name = AppStrings.SignOut }).ClickAsync();
        await page.GetByRole(AriaRole.Dialog).GetByRole(AriaRole.Button, new() { Name = AppStrings.SignOut }).ClickAsync();
    }

    public async Task AssertSignOut()
    {
        await Assertions.Expect(page).ToHaveURLAsync(serverAddress.ToString());
        await Assertions.Expect(page.GetByRole(AriaRole.Link, new() { Name = AppStrings.SignIn })).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByRole(AriaRole.Button, new() { Name = AppStrings.SignOut })).ToBeHiddenAsync();
        await Assertions.Expect(page.Locator(".bit-prs")).ToBeHiddenAsync();
    }
}
