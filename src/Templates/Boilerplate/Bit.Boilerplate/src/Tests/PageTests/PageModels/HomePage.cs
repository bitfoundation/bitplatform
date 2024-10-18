using Boilerplate.Tests.PageTests.PageModels.Layout;

namespace Boilerplate.Tests.PageTests.PageModels;

public partial class MainHomePage(IPage page, Uri serverAddress)
    : MainLayout(page, serverAddress)
{
    public override string PagePath => Urls.HomePage;
    public override string PageTitle => AppStrings.HomeTitle;
}

public partial class IdentityHomePage(IPage page, Uri serverAddress)
    : IdentityLayout(page, serverAddress)
{
    public override string PagePath => Urls.HomePage;
    public override string PageTitle => AppStrings.HomeTitle;

    public async Task<MainHomePage> SignOut()
    {
        await Page.GetByRole(AriaRole.Button, new() { Name = AppStrings.SignOut }).ClickAsync();
        await Page.GetByRole(AriaRole.Dialog).GetByRole(AriaRole.Button, new() { Name = AppStrings.SignOut }).ClickAsync();

        return new MainHomePage(Page, WebAppServerAddress);
    }
}
