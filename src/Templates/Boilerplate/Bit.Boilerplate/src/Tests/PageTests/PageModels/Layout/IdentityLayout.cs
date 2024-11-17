using Boilerplate.Tests.PageTests.PageModels.Identity;

namespace Boilerplate.Tests.PageTests.PageModels.Layout;

public abstract partial class IdentityLayout(IPage page, Uri serverAddress)
    : RootLayout(page, serverAddress)
{
    public async Task AssertSignInSuccess(string userEmail = TestData.DefaultTestEmail, string? userFullName = TestData.DefaultTestFullName)
    {
        var displayName = string.IsNullOrWhiteSpace(userFullName) ? userEmail : userFullName;

        await Assertions.Expect(Page.GetByRole(AriaRole.Button, new() { Name = displayName })).ToBeVisibleAsync();
        await Assertions.Expect(Page.Locator(".bit-prs.persona").First).ToContainTextAsync(displayName);
        await Assertions.Expect(Page.Locator(".bit-prs.persona").Last).ToContainTextAsync(displayName);
        await Assertions.Expect(Page.Locator(".bit-prs.persona").Last).ToContainTextAsync(userEmail);
        await Assertions.Expect(Page.GetByRole(AriaRole.Button, new() { Name = AppStrings.SignOut })).ToBeVisibleAsync();
        await Assertions.Expect(Page.GetByRole(AriaRole.Button, new() { Name = AppStrings.SignIn })).ToBeHiddenAsync();
    }

    public async Task<SignInPage> SignOut()
    {
        await Page.Locator(".bit-crd.panel").GetByRole(AriaRole.Button, new() { Name = AppStrings.SignOut }).ClickAsync();
        await Page.GetByRole(AriaRole.Dialog).GetByRole(AriaRole.Button, new() { Name = AppStrings.SignOut, Exact = true }).ClickAsync();

        return new SignInPage(Page, WebAppServerAddress) { ReturnUrl = new Uri(Page.Url).PathAndQuery.TrimStart('/') };
    }
}
