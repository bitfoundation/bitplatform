namespace Boilerplate.Tests.PageTests.PageModels;

public partial class AdminLayout(IPage page, Uri serverAddress, string pagePath, string pageTitle)
    : MainLayout(page, serverAddress, pagePath, pageTitle)
{
    public async Task AssertSignInSuccess(string userFullName = "Boilerplate test account")
    {
        await Assertions.Expect(page).ToHaveURLAsync(serverAddress.ToString());
        await Assertions.Expect(page.GetByRole(AriaRole.Button, new() { Name = userFullName })).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByText(userFullName).First).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByText(userFullName).Nth(1)).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByRole(AriaRole.Button, new() { Name = AppStrings.SignOut })).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByRole(AriaRole.Button, new() { Name = AppStrings.SignIn })).ToBeVisibleAsync(new() { Visible = false });
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
        await Assertions.Expect(page.GetByRole(AriaRole.Button, new() { Name = AppStrings.SignOut })).ToBeVisibleAsync(new() { Visible = false });
        await Assertions.Expect(page.Locator(".persona")).ToBeVisibleAsync(new() { Visible = false });
    }
}
