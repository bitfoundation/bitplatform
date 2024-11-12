using Boilerplate.Tests.PageTests.PageModels.Layout;

namespace Boilerplate.Tests.PageTests.PageModels;

public partial class MainHomePage(IPage page, Uri serverAddress)
    : MainLayout(page, serverAddress)
{
    public override string PagePath => Urls.HomePage;
    public override string PageTitle => AppStrings.HomePageTitle;

    public async Task ChangeCulture(string cultureDisplayName)
    {
        await Page.GetByRole(AriaRole.Combobox).Locator($"//img[contains(@src, 'flags')]").ClickAsync();
        await Page.Locator($"button[role='option']:has-text('{cultureDisplayName}')").ClickAsync();
    }

    public async Task AssertLocalized(IStringLocalizer localizer, string cultureName, string cultureDisplayName)
    {
        await Assertions.Expect(Page).ToHaveTitleAsync(localizer[nameof(AppStrings.HomePageTitle)]);

        await Assertions.Expect(Page.GetByText(localizer[nameof(AppStrings.HomePanelTitle)] + " " + localizer[nameof(AppStrings.HomePanelSubtitle)])).ToBeVisibleAsync();

        await Assertions.Expect(Page.GetByText(localizer[nameof(AppStrings.HomeMessage)])).ToBeVisibleAsync();

        await Assertions.Expect(Page.GetByRole(AriaRole.Link, new() { Name = localizer[nameof(AppStrings.SignIn)] })).ToBeVisibleAsync();

        await Assertions.Expect(Page.GetByRole(AriaRole.Link, new() { Name = localizer[nameof(AppStrings.SignUp)] })).ToBeVisibleAsync();

        var cultureDropdown = Page.GetByRole(AriaRole.Combobox).Locator($"//img[contains(@src, 'flags/{cultureName}')]");

        await Assertions.Expect(cultureDropdown).ToBeVisibleAsync();

        await cultureDropdown.ClickAsync();

        await Assertions.Expect(Page.Locator($"button[role='option']:has-text('{cultureDisplayName}')")).ToHaveAttributeAsync("aria-selected", "true");
    }
}

public partial class IdentityHomePage(IPage page, Uri serverAddress)
    : IdentityLayout(page, serverAddress)
{
    public override string PagePath => Urls.HomePage;
    public override string PageTitle => AppStrings.HomePageTitle;

    public async Task<MainHomePage> SignOut()
    {
        await Page.GetByRole(AriaRole.Button, new() { Name = AppStrings.SignOut }).ClickAsync();
        await Page.GetByRole(AriaRole.Dialog).GetByRole(AriaRole.Button, new() { Name = AppStrings.SignOut }).ClickAsync();

        return new MainHomePage(Page, WebAppServerAddress);
    }
}
