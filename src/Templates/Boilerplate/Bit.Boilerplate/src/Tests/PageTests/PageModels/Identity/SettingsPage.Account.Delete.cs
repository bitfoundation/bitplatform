namespace Boilerplate.Tests.PageTests.PageModels.Identity;

public partial class SettingsPage
{
    public async Task ClickOnDeleteTab()
    {
        await Page.GetByRole(AriaRole.Tab, new() { Name = AppStrings.Delete }).ClickAsync();
    }

    public async Task AssertDeleteTab()
    {
        await Assertions.Expect(Page.GetByLabel(AppStrings.Delete).GetByRole(AriaRole.Heading)).ToContainTextAsync(AppStrings.DeleteAccount);
        await Assertions.Expect(Page.GetByLabel(AppStrings.Delete).GetByRole(AriaRole.Paragraph)).ToContainTextAsync(AppStrings.DeleteAccountPrompt);
        await Assertions.Expect(Page.GetByLabel(AppStrings.Delete).GetByRole(AriaRole.Button, new() { Name = AppStrings.DeleteAccount })).ToBeVisibleAsync();
    }

    public async Task<SignInPage> DeleteUser()
    {
        await Page.GetByLabel(AppStrings.Delete).GetByRole(AriaRole.Button, new() { Name = AppStrings.DeleteAccount }).ClickAsync();
        var currentUrl = Page.Url;
        await Page.GetByRole(AriaRole.Dialog).GetByRole(AriaRole.Button, new() { Name = AppStrings.Yes }).ClickAsync();
        await Page.WaitForURLAsync(url => url != currentUrl);
        return new(Page, WebAppServerAddress) { ReturnUrl = new Uri(currentUrl).PathAndQuery.TrimStart('/') };
    }
}
