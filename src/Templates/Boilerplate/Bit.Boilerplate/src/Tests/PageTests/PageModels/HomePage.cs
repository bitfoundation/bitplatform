using Boilerplate.Tests.PageTests.PageModels.Layout;
using Boilerplate.Tests.Services;

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

    public override async Task AssertOpen()
    {
        var (displayName, culture) = CultureInfoManager.SupportedCultures.Single(x => x.Culture.Name == CultureInfoManager.DefaultCulture.Name);
        var localizer = StringLocalizerFactory.Create<AppStrings>(culture.Name);
        await AssertLocalized(localizer, culture.Name, displayName);
    }

    public async Task AssertLocalized(IStringLocalizer localizer, string cultureName, string cultureDisplayName)
    {
        await Assertions.Expect(Page).ToHaveTitleAsync(localizer[nameof(AppStrings.HomePageTitle)]);

        await Assertions.Expect(Page.GetByRole(AriaRole.Heading, new() { Level = 4, Name = localizer[nameof(AppStrings.HomePanelTitle)] + " " + localizer[nameof(AppStrings.HomePanelSubtitle)] })).ToBeVisibleAsync();
        await Assertions.Expect(Page.GetByText(localizer[nameof(AppStrings.HomeMessage)])).ToBeVisibleAsync();

        await Assertions.Expect(Page.GetByRole(AriaRole.Link, new() { Name = localizer[nameof(AppStrings.SignIn)] })).ToBeVisibleAsync();
        await Assertions.Expect(Page.GetByRole(AriaRole.Link, new() { Name = localizer[nameof(AppStrings.SignUp)] })).ToBeVisibleAsync();
    }

    public async Task AssertCultureCombobox(string cultureName, string cultureDisplayName)
    {
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

    public new async Task<MainHomePage> SignOut()
    {
        await Page.Locator(".bit-crd.panel").GetByRole(AriaRole.Button, new() { Name = AppStrings.SignOut }).ClickAsync();
        await Page.GetByRole(AriaRole.Dialog).GetByRole(AriaRole.Button, new() { Name = AppStrings.SignOut, Exact = true }).ClickAsync();

        return new MainHomePage(Page, WebAppServerAddress);
    }
}
