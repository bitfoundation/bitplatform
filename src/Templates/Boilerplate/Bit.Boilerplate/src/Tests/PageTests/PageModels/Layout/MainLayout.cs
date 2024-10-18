namespace Boilerplate.Tests.PageTests.PageModels.Layout;

public abstract partial class MainLayout(IPage page, Uri serverAddress)
    : RootLayout(page, serverAddress)
{
    public virtual async Task AssertSignOut()
    {
        await Assertions.Expect(Page).ToHaveURLAsync(new Uri(WebAppServerAddress, PagePath).ToString());
        await Assertions.Expect(Page.GetByRole(AriaRole.Button, new() { Name = AppStrings.SignOut })).ToBeHiddenAsync();
        await Assertions.Expect(Page.Locator(".bit-prs.persona")).ToBeHiddenAsync();
    }
}
