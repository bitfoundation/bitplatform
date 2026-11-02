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
    public async Task ANewView_Should_ListTheOtherAccountsWithTheFirstUserActive(WorkbenchSurface surface)
    {
        await OpenWorkbench("theme");

        await Expect(AccountButton(WorkbenchStrings.DemoUserYas)).ToBeVisibleAsync();

        await OpenAccountMenu();

        await Expect(AccountMenu.GetByText(WorkbenchDemoUsers.All[0].Email)).ToBeVisibleAsync();
        await Expect(MenuItem(WorkbenchStrings.DemoUserSaleh)).ToBeVisibleAsync();
        await Expect(MenuItem(WorkbenchStrings.DemoUserZahra)).ToBeVisibleAsync();
        await Expect(MenuItem(WorkbenchStrings.DemoUserYas)).ToHaveCountAsync(0);
    }

    [TestMethod, EverySurface]
    public async Task AnotherAccount_Should_SignTheViewInAsThemAndReachTheTools(WorkbenchSurface surface)
    {
        await OpenWorkbench("theme");

        await OpenAccountMenu();
        await Press(MenuItem(WorkbenchStrings.DemoUserSaleh));

        await WaitForTheViewToReloadWith("user=1");
        await Expect(AccountButton(WorkbenchStrings.DemoUserSaleh)).ToBeVisibleAsync();
        await Expect(Page).ToHaveURLAsync(new Regex(@"\?user=1$"));

        await OpenAccountMenu();

        await Expect(AccountMenu.GetByText(WorkbenchDemoUsers.All[1].Email)).ToBeVisibleAsync();
        await Expect(MenuItem(WorkbenchStrings.DemoUserYas)).ToBeVisibleAsync();
        await Expect(MenuItem(WorkbenchStrings.DemoUserZahra)).ToBeVisibleAsync();
        await Expect(MenuItem(WorkbenchStrings.DemoUserSaleh)).ToHaveCountAsync(0);

        await ReloadTheView();

        await Expect(AccountButton(WorkbenchStrings.DemoUserSaleh)).ToBeVisibleAsync();

        await OpenTools();

        await Expect(UserPicker).ToContainTextAsync(WorkbenchStrings.DemoUserSaleh);
    }

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

    [TestMethod, EverySurface]
    public async Task ASingleAccount_Should_ListNoOthersButOfferToAddOne(WorkbenchSurface surface)
    {
        await OpenWorkbench("theme", "?auth=signedin");

        await OpenAccountMenu();

        await Expect(AccountMenu.GetByText(WorkbenchDemoUsers.All[0].Email)).ToBeVisibleAsync();
        await Expect(MenuItem(WorkbenchStrings.DemoUserSaleh)).ToHaveCountAsync(0);
        await Expect(MenuItem(WorkbenchStrings.DemoUserZahra)).ToHaveCountAsync(0);
        await Expect(MenuItem(AppStrings.AddAnotherAccount)).ToBeVisibleAsync();
    }

    [TestMethod, EverySurface]
    public async Task AddAnotherAccount_Should_PointAtTheUserPicker(WorkbenchSurface surface)
    {
        await OpenWorkbench("theme");

        await OpenAccountMenu();
        await Press(MenuItem(AppStrings.AddAnotherAccount));

        await ExpectTheAccountMenuClosed();
        await Expect(SignInGuide).ToBeVisibleAsync();
        await Expect(SignInTools).ToBeInViewportAsync(new() { Ratio = 1 });

        await Press(Button(WorkbenchStrings.GotIt));

        await Expect(SignInGuide).ToHaveCountAsync(0);
    }

    [TestMethod, EverySurface]
    public async Task SignOut_Should_AskAndThenSignTheViewOut(WorkbenchSurface surface)
    {
        await OpenWorkbench("theme");

        await OpenAccountMenu();
        await Press(MenuItem(AppStrings.SignOut));

        var dialog = View().GetByRole(AriaRole.Alertdialog);

        await ExpectTheAccountMenuClosed();
        await Expect(dialog.GetByText(AppStrings.SignOutPrompt)).ToBeVisibleAsync();

        await Press(dialog.GetByRole(AriaRole.Button, new() { Name = AppStrings.Cancel, Exact = true }));

        await Expect(dialog).ToHaveCountAsync(0);
        await Expect(AccountButton(WorkbenchStrings.DemoUserYas)).ToBeVisibleAsync();
        await Expect(Page).Not.ToHaveURLAsync(new Regex("auth="));

        await OpenAccountMenu();
        await Press(MenuItem(AppStrings.SignOut));
        await Press(dialog.GetByRole(AriaRole.Button, new() { Name = AppStrings.SignOut, Exact = true }));

        await Expect(AccountButton(AppStrings.AccountMenuTitle)).ToBeVisibleAsync();
        await Expect(Page).ToHaveURLAsync(new Regex(@"\?auth=signedout$"));

        await OpenTools();

        await Expect(SignInTools.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.SignedOut, Exact = true })).ToHaveAttributeAsync("aria-pressed", "true");
    }

    [TestMethod, EverySurface]
    public async Task ASignedOutMenu_Should_OfferSigningInAndNoAccount(WorkbenchSurface surface)
    {
        await OpenWorkbench("theme", "?auth=signedout");

        await OpenAccountMenu();

        foreach (var name in new[] { AppStrings.SignIn, AppStrings.SignInByModal, AppStrings.SignUp, AppStrings.Language })
        {
            await Expect(MenuItem(name)).ToBeVisibleAsync();
        }

        foreach (var name in new[] { AppStrings.SignOut, AppStrings.AddAnotherAccount, AppStrings.ProfileTitle })
        {
            await Expect(MenuItem(name)).ToHaveCountAsync(0);
        }

        //#if (multitenant == true)
        await Expect(MenuItem(AppStrings.Tenant)).ToHaveCountAsync(0);
        //#endif
    }

    [TestMethod, EverySurface]
    public async Task EverySignInOfASignedOutMenu_Should_PointAtTheUserPicker(WorkbenchSurface surface)
    {
        await OpenWorkbench("theme", "?auth=signedout");

        foreach (var name in new[] { AppStrings.SignIn, AppStrings.SignInByModal, AppStrings.SignUp })
        {
            await OpenAccountMenu();
            await Press(MenuItem(name));

            await ExpectTheAccountMenuClosed();
            await Expect(SignInGuide).ToBeVisibleAsync();

            await Press(Button(WorkbenchStrings.GotIt));

            await Expect(SignInGuide).ToHaveCountAsync(0);

            await CloseTools();
        }
    }

    [TestMethod, EverySurface]
    public async Task SignInInAView_Should_BringTheUserPickerIntoSightAndPointAtIt(WorkbenchSurface surface)
    {
        await OpenWorkbench("theme", "?auth=signedout");

        if (IsPhone is false)
        {
            await Page.Locator("#wb-tab-review").ClickAsync();
            await Button(WorkbenchStrings.Tools).ClickAsync();
            await Expect(Page.Locator("#wb-tab-review")).Not.ToBeInViewportAsync();
        }

        await OpenAccountMenu();
        await Press(MenuItem(AppStrings.SignIn));

        await Expect(SignInGuide).ToBeVisibleAsync();
        await Expect(SignInTools).ToBeInViewportAsync(new() { Ratio = 1 });

        if (IsPhone is false)
        {
            await Expect(Page.Locator("#wb-tab-view")).ToHaveAttributeAsync("aria-selected", "true");
        }

        var signedIn = SignInTools.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.SignedIn, Exact = true });

        await Press(signedIn);
        await Expect(signedIn).ToHaveAttributeAsync("aria-pressed", "true");

        await PickUser(WorkbenchStrings.DemoUserZahra);

        await Press(Button(WorkbenchStrings.GotIt));

        await Expect(SignInGuide).ToHaveCountAsync(0);

        await CloseTools();

        await Expect(AccountButton(WorkbenchStrings.DemoUserZahra)).ToBeVisibleAsync();
    }

    [TestMethod, EverySurface]
    public async Task SignInInAPreviewAlone_Should_ExplainInAMessageBox(WorkbenchSurface surface)
    {
        await OpenPreview("theme", "?auth=signedout");

        var note = Page.GetByText(WorkbenchStrings.SignInNote);

        foreach (var name in new[] { AppStrings.SignIn, AppStrings.SignInByModal, AppStrings.SignUp })
        {
            await Press(Page.Locator("header .persona").First);
            await Press(Page.Locator(".app-menu-card").GetByRole(AriaRole.Button, new() { Name = name, Exact = true }));

            await Expect(note).ToBeVisibleAsync();

            await Press(Page.GetByRole(AriaRole.Dialog).Filter(new() { HasText = WorkbenchStrings.SignInNote }).GetByRole(AriaRole.Button).Last);

            await Expect(note).ToHaveCountAsync(0);
        }

        await Expect(SignInGuide).ToHaveCountAsync(0);
    }

    [TestMethod, EverySurface]
    public async Task TheLanguageList_Should_MarkTheCurrentLanguageAndGoBack(WorkbenchSurface surface)
    {
        await OpenWorkbench("theme");

        await OpenAccountMenu();
        await Press(MenuItem(AppStrings.Language));

        var languages = AccountMenu.GetByRole(AriaRole.Radiogroup, new() { Name = AppStrings.SelectLanguage, Exact = true });

        await Expect(languages.GetByRole(AriaRole.Radio)).ToHaveCountAsync(CultureInfoManager.SupportedCultures.Length);
        await Expect(languages.GetByRole(AriaRole.Radio, new() { Name = DefaultLanguage })).ToBeCheckedAsync();
        await Expect(MenuItem(AppStrings.SignOut)).ToHaveCountAsync(0);

        await Press(MenuItem(AppStrings.SelectLanguage));

        await Expect(languages).ToHaveCountAsync(0);
        await Expect(MenuItem(AppStrings.SignOut)).ToBeVisibleAsync();
    }

    [TestMethod, EverySurface]
    public async Task ClosingTheMenu_Should_BringItBackToItsMainList(WorkbenchSurface surface)
    {
        await OpenWorkbench("theme");

        await OpenAccountMenu();
        await Press(MenuItem(AppStrings.Language));
        await Expect(MenuItem(AppStrings.SelectLanguage)).ToBeVisibleAsync();

        await CloseAccountMenu();

        await OpenAccountMenu();

        await Expect(MenuItem(AppStrings.Language)).ToBeVisibleAsync();
        await Expect(MenuItem(AppStrings.SelectLanguage)).ToHaveCountAsync(0);
    }

    [TestMethod, EverySurface]
    [TestCategory("Localization")]
    public async Task TheDefaultLanguage_Should_ComeOutOfTheAddress(WorkbenchSurface surface)
    {
        await OpenWorkbench("theme", "?culture=fa-IR");

        await Expect(View().Locator("html")).ToHaveAttributeAsync("dir", "rtl");

        await OpenAccountMenu();
        await Press(MenuItem(AppInCulture("fa-IR", nameof(AppStrings.Language))));
        await Press(AccountMenu.GetByText(DefaultLanguage, new() { Exact = true }));

        await Expect(Page).Not.ToHaveURLAsync(new Regex("culture="));
        await WaitForViews();
        await Expect(View().Locator("html")).ToHaveAttributeAsync("lang", CultureInfoManager.DefaultCulture.Name);
        await Expect(View().Locator("html")).Not.ToHaveAttributeAsync("dir", "rtl");
    }

    //#if (multitenant == true)
    [TestMethod, EverySurface]
    public async Task ATenant_Should_BePickedFromItsListAndStayAfterAReload(WorkbenchSurface surface)
    {
        await OpenWorkbench("theme");

        await OpenAccountMenu();
        await Press(MenuItem(AppStrings.Tenant));

        var tenants = AccountMenu.GetByRole(AriaRole.Radiogroup, new() { Name = AppStrings.SwitchTenant, Exact = true });
        var headOffice = tenants.GetByRole(AriaRole.Radio, new() { Name = WorkbenchStrings.DemoTenantHeadOffice, Exact = true });

        await Expect(tenants.GetByRole(AriaRole.Radio)).ToHaveCountAsync(WorkbenchDemoUsers.TenantKeys.Count);
        await Expect(tenants.GetByRole(AriaRole.Radio, new() { Name = WorkbenchStrings.DemoTenantStore, Exact = true })).ToBeCheckedAsync();

        await Press(tenants.GetByText(WorkbenchStrings.DemoTenantHeadOffice, new() { Exact = true }));

        await Expect(tenants).ToHaveCountAsync(0);

        await Press(MenuItem(AppStrings.Tenant));

        await Expect(headOffice).ToBeCheckedAsync();

        await ReloadTheViewOnceSaved($"\"TenantKey\":\"{nameof(WorkbenchStrings.DemoTenantHeadOffice)}\"");
        await OpenAccountMenu();
        await Press(MenuItem(AppStrings.Tenant));

        await Expect(headOffice).ToBeCheckedAsync();
    }

    [TestMethod, EverySurface]
    public async Task TheTenantList_Should_OfferManagingAndGoBack(WorkbenchSurface surface)
    {
        await OpenWorkbench("theme");

        await OpenAccountMenu();
        await Press(MenuItem(AppStrings.Tenant));

        await Expect(MenuItem(AppStrings.ManageMyTenants)).ToBeVisibleAsync();
        await Expect(MenuItem(AppStrings.SignOut)).ToHaveCountAsync(0);

        await Press(MenuItem(AppStrings.SwitchTenant));

        await Expect(MenuItem(AppStrings.ManageMyTenants)).ToHaveCountAsync(0);
        await Expect(MenuItem(AppStrings.SignOut)).ToBeVisibleAsync();
    }
    //#endif

    private async Task OpenTheTimeZoneList()
    {
        await Press(TimeZoneField);
        await Expect(View().GetByRole(AriaRole.Option).First).ToBeVisibleAsync();
    }

    [TestMethod, EverySurface]
    public async Task ATimeZone_Should_BeFoundByNameAndStayAfterAReload(WorkbenchSurface surface)
    {
        await OpenWorkbench("theme");

        await OpenAccountMenu();

        await OpenTheTimeZoneList();
        await TimeZoneSearch.FillAsync("Tokyo");
        await Press(View().GetByRole(AriaRole.Option, new() { NameRegex = new Regex("Tokyo") }));

        await Expect(TimeZoneField).ToContainTextAsync("Tokyo");

        await ReloadTheViewOnceSaved("Tokyo");
        await OpenAccountMenu();

        await Expect(TimeZoneField).ToContainTextAsync("Tokyo");
    }

    [TestMethod, EverySurface]
    public async Task ATimeZoneSearchWithNoMatch_Should_SaySoAndTheListShouldClose(WorkbenchSurface surface)
    {
        await OpenWorkbench("theme");

        await OpenAccountMenu();

        await OpenTheTimeZoneList();
        await TimeZoneSearch.FillAsync("Atlantis");

        await Expect(View().GetByRole(AriaRole.Listbox, new() { Name = AppStrings.TimeZone, Exact = true }).GetByText(AppStrings.NoTimeZoneMatchesSearchMessage)).ToBeVisibleAsync();

        // A desktop's phone frame draws its status bar over the list's Close button, so there the keyboard closes it.
        if (IsPhone)
        {
            await Press(View().Locator(".bit-tzp-cal").GetByRole(AriaRole.Button, new() { Name = AppStrings.Close, Exact = true }));
        }
        else
        {
            await TimeZoneSearch.PressAsync("Escape");
        }

        await Expect(TimeZoneField).ToHaveAttributeAsync("aria-expanded", "false");
    }

    //#if (signalR == true || notification == true)
    [TestMethod, EverySurface]
    public async Task TheNotificationsSwitch_Should_TurnThemOffAndStayOffAfterAReload(WorkbenchSurface surface)
    {
        await OpenWorkbench("theme");

        await OpenAccountMenu();

        var turnOff = AccountMenu.GetByRole(AriaRole.Switch, new() { Name = AppStrings.TurnNotificationsOff, Exact = true });
        var turnOn = AccountMenu.GetByRole(AriaRole.Switch, new() { Name = AppStrings.TurnNotificationsOn, Exact = true });

        await Expect(turnOff).ToBeCheckedAsync();

        await Press(turnOff);

        await Expect(turnOn).Not.ToBeCheckedAsync();

        await Press(MenuItem(AppStrings.Notifications));

        await Expect(turnOff).ToBeCheckedAsync();

        await Press(MenuItem(AppStrings.Notifications));

        await Expect(turnOn).Not.ToBeCheckedAsync();

        await ReloadTheViewOnceSaved("\"AreNotificationsOn\":false");
        await OpenAccountMenu();

        await Expect(turnOn).Not.ToBeCheckedAsync();
    }
    //#endif

    [TestMethod, EverySurface]
    public async Task TheThemeSwitch_Should_GoBothWays(WorkbenchSurface surface)
    {
        await OpenWorkbench("theme");

        await OpenAccountMenu();

        await Press(AccountMenu.GetByRole(AriaRole.Switch, new() { Name = AppStrings.SwitchToDarkThemeTitle, Exact = true }));

        await Expect(View().Locator("html")).ToHaveAttributeAsync("bit-theme", new Regex("dark"));
        await Expect(MenuItem(AppStrings.Dark)).ToBeVisibleAsync();

        await Press(AccountMenu.GetByRole(AriaRole.Switch, new() { Name = AppStrings.SwitchToLightThemeTitle, Exact = true }));

        await Expect(View().Locator("html")).Not.ToHaveAttributeAsync("bit-theme", new Regex("dark"));
        await Expect(MenuItem(AppStrings.Light)).ToBeVisibleAsync();
        await Expect(Page).Not.ToHaveURLAsync(new Regex("theme=dark"));
    }

    [TestMethod, EverySurface]
    public async Task TheToolsAccent_Should_ComeBeforeThePickOnThePage(WorkbenchSurface surface)
    {
        await OpenWorkbench("theme");

        var original = await PrimaryColor();

        await OpenAccountMenu();
        await Press(AccountMenu.Locator(".accent-color-switcher").GetByRole(AriaRole.Button).Nth(1));

        var picked = await WorkbenchTestReviewer.WaitFor(PrimaryColor, primary => primary != original);

        await WaitUntilSaved("\"Accent\":\"#");
        await CloseAccountMenu();

        var accents = Tools.Locator("div.accents button[role='radio']");

        await OpenTools();
        await Press(accents.Nth(2));
        await CloseTools();

        await Expect(Page).ToHaveURLAsync(new Regex(@"\?accent=[0-9a-f]{6}$"));
        await WaitForViews();
        await WorkbenchTestReviewer.WaitFor(PrimaryColor, primary => primary != original && primary != picked);

        await OpenTools();
        await Press(accents.First);
        await CloseTools();

        await Expect(Page).Not.ToHaveURLAsync(new Regex("accent="));
        await WaitForViews();
        await WorkbenchTestReviewer.WaitFor(PrimaryColor, primary => primary == picked);
    }

    [TestMethod, EverySurface]
    public async Task AnAccent_Should_ChangeTheViewsColorStayAfterAReloadAndGoBackToTheDefault(WorkbenchSurface surface)
    {
        await OpenWorkbench("theme");

        var original = await PrimaryColor();

        await OpenAccountMenu();

        var swatches = AccountMenu.Locator(".accent-color-switcher").GetByRole(AriaRole.Button);

        await Expect(swatches.First).ToHaveAttributeAsync("aria-pressed", "true");

        await Press(swatches.Nth(1));

        await Expect(swatches.Nth(1)).ToHaveAttributeAsync("aria-pressed", "true");
        await Expect(swatches.First).ToHaveAttributeAsync("aria-pressed", "false");
        await WorkbenchTestReviewer.WaitFor(PrimaryColor, primary => primary != original);

        await ReloadTheViewOnceSaved("\"Accent\":\"#");

        await WorkbenchTestReviewer.WaitFor(PrimaryColor, primary => primary != original);

        await OpenAccountMenu();

        await Expect(swatches.Nth(1)).ToHaveAttributeAsync("aria-pressed", "true");

        await Press(swatches.First);

        await Expect(swatches.First).ToHaveAttributeAsync("aria-pressed", "true");
        await WorkbenchTestReviewer.WaitFor(PrimaryColor, primary => primary == original);
    }
}
