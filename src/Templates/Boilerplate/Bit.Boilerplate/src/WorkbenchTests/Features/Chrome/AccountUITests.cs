//+:cnd:noEmit
namespace Boilerplate.WorkbenchTests.Features.Chrome;

/// <summary>The app's account menu in a view: the accounts, signing in and out, the tenant, time zone, notifications and accent, which change the view instead of calling a server.</summary>
[TestClass, TestCategory("UITest")]
public class AccountUITests : AppViewPageTest
{
    private static string DefaultLanguage => CultureInfoManager.SupportedCultures.First(sc => sc.Culture.Name == CultureInfoManager.DefaultCulture.Name).DisplayName;

    private ILocator TimeZoneField => AccountMenu.GetByRole(AriaRole.Combobox, new() { Name = AppStrings.TimeZone, Exact = true });

    private ILocator TimeZoneSearch => View().GetByRole(AriaRole.Combobox, new() { Name = AppStrings.FindTimeZone, Exact = true });

    /// <summary>Opens the Settings page with its Privacy section open instead of the Profile one.</summary>
    private async Task OpenSection()
    {
        await OpenWorkbench("settings");
        await Press(View().GetByRole(AriaRole.Button, new() { Name = AppStrings.PrivacyTitle, Exact = true }));
        await Expect(View().GetByRole(AriaRole.Region, new() { Name = AppStrings.ProfileTitle, Exact = true })).Not.ToBeVisibleAsync();
    }

    private Task<string> PrimaryColor() => View().Locator("body").EvaluateAsync<string>("e => getComputedStyle(e).getPropertyValue('--bit-clr-pri').trim()");

    [TestMethod, EverySurface]
    public async Task AnotherAccount_Should_ShowThatAccountsProfileOnTheSettingsPage(WorkbenchSurface surface)
    {
        await OpenWorkbench("settings");

        var fullName = View().GetByRole(AriaRole.Textbox, new() { Name = AppStrings.FullName, Exact = true });

        await Expect(fullName).ToHaveValueAsync(WorkbenchStrings.DemoUserYas);

        await OpenAccountMenu();
        await Press(MenuItem(WorkbenchStrings.DemoUserZahra));

        await Expect(fullName).ToHaveValueAsync(WorkbenchStrings.DemoUserZahra);
    }

    [TestMethod, EverySurface]
    public async Task TheProfileButton_Should_OpenTheProfileSectionOfTheSettingsPage(WorkbenchSurface surface)
    {
        await OpenWorkbench("theme");

        await OpenAccountMenu();
        await Press(MenuItem(AppStrings.ProfileTitle));

        await ExpectTheAccountMenuClosed();
        await Expect(PageTitle).ToHaveTextAsync(WorkbenchStrings.SettingsTitle);
        await Expect(View().GetByRole(AriaRole.Textbox, new() { Name = AppStrings.FullName, Exact = true })).ToHaveValueAsync(WorkbenchStrings.DemoUserYas);
        await Expect(Page).ToHaveURLAsync(new Regex("/workbench/settings"));
    }

    [TestMethod, EverySurface]
    public async Task TheUsersPicture_Should_OpenTheProfileSectionToo(WorkbenchSurface surface)
    {
        await OpenSection();

        await OpenAccountMenu();
        await Press(AccountMenu.Locator(".persona").GetByRole(AriaRole.Button));

        await ExpectTheAccountMenuClosed();
        await Expect(View().GetByRole(AriaRole.Region, new() { Name = AppStrings.ProfileTitle, Exact = true })).ToBeVisibleAsync();
    }


    private async Task OpenTheTimeZoneList()
    {
        await Press(TimeZoneField);
        await Expect(View().GetByRole(AriaRole.Option).First).ToBeVisibleAsync();
    }
}
