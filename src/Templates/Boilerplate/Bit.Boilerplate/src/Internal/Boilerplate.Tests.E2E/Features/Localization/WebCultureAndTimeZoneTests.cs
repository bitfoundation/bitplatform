using Boilerplate.Tests.E2E.Infrastructure.Services;

namespace Boilerplate.Tests.E2E.Features.Localization;

/// <summary>
/// The browser's language on a first visit, the language and time zone a user picks in the app menu, and both of those
/// outliving a reload. The browser is Dutch and in Amsterdam: neither is the default, so a page in either can only have
/// come from the browser, and nothing a test leaves behind is the default either.
/// </summary>
[TestClass, TestCategory(TestCategories.Web), Retry(2)]
public partial class WebCultureAndTimeZoneTests : AppTestBase
{
    private const string browserCulture = "nl-NL";

    /// <summary>Right to left, so the switch shows in the document's direction too.</summary>
    private const string pickedCulture = "fa-IR";

    private const string browserTimeZone = "Europe/Amsterdam";
    private const string pickedTimeZone = "Asia/Tokyo";

    protected override IAppOpener AppOpener => new WebAppOpener();

    /// <summary>Accept-Language and navigator.language, for the server's culture redirect and the client alike.</summary>
    public override BrowserNewContextOptions ContextOptions()
    {
        var options = base.ContextOptions();

        options.Locale = browserCulture;
        options.TimezoneId = browserTimeZone;

        return options;
    }

    [TestMethod]
    [DataRow(App.Sales, DisplayName = "Sales (Blazor Router)")]
    [DataRow(App.Todo, DisplayName = "Todo (Blazor Router)")]
    [DataRow(App.AdminPanel, DisplayName = "AdminPanel (bit Brouter)")]
    public async Task Culture_Should_FollowTheBrowser_ThenStayWhatTheUserPicked(App app)
    {
        var page = await OpenApp(app);
        await WaitUntilInteractive(page);

        await ExpectCulture(page, browserCulture);

        await ChangeCulture(page, pickedCulture, currentCultureName: browserCulture);
        await ExpectCulture(page, pickedCulture);

        await page.ReloadAsync();
        await WaitUntilInteractive(page);
        await ExpectCulture(page, pickedCulture);

        // The bare root names no culture, so the remembered one has to outrank the browser's Accept-Language. Not the
        // url: once bswup's worker is installed it answers "/" with the app shell itself (Todo, AdminPanel), so only a
        // server that renders every document (Sales) gets to redirect it onto /fa-IR/.
        await page.GotoAsync(DeployedApps.AddressOf(app));
        await WaitUntilInteractive(page);
        await ExpectCulture(page, pickedCulture, inUrl: false);
    }

    [TestMethod]
    [DataRow(App.Sales, DisplayName = "Sales (Blazor Router)")]
    [DataRow(App.Todo, DisplayName = "Todo (Blazor Router)")]
    [DataRow(App.AdminPanel, DisplayName = "AdminPanel (bit Brouter)")]
    public async Task TimeZone_Should_StayWhatTheUserPicked_AfterAReload(App app)
    {
        var page = await OpenApp(app);
        await WaitUntilInteractive(page);

        var timeZones = await OpenTimeZones(page);
        await page.GetByPlaceholder(Localized(nameof(AppStrings.FindTimeZone))).FillEnsuringStable("Tokyo");
        await timeZones.Locator(".time-zone-item", new() { HasText = "Tokyo" }).First.ClickAsync();

        // TimeZoneService stores the IANA id on the device, whatever runtime rendered the menu.
        await page.WaitForFunctionAsync("zone => localStorage.getItem('time-zone') === zone", pickedTimeZone);

        await page.ReloadAsync();
        await WaitUntilInteractive(page);

        Assert.AreEqual(pickedTimeZone, await page.EvaluateAsync<string?>("() => localStorage.getItem('time-zone')"));

        timeZones = await OpenTimeZones(page);
        await Expect(timeZones.Locator(".time-zone-item[aria-checked='true']")).ToContainTextAsync("Tokyo");
    }

    /// <summary>The document's language and direction, and - unless told otherwise - the url's culture segment.</summary>
    private async Task ExpectCulture(IPage page, string cultureName, bool inUrl = true)
    {
        if (inUrl)
            await Expect(page).ToHaveURLAsync(new Regex($"/{Regex.Escape(cultureName)}(/|$)"));

        var html = page.Locator("html");
        await Expect(html).ToHaveAttributeAsync("lang", cultureName);

        // Case-insensitive, as HTML reads it: the server renders "rtl", Butil's Document.SetDir wrote "Rtl".
        var rtl = new Regex("^rtl$", RegexOptions.IgnoreCase);

        if (CultureInfoManager.GetCultureInfo(cultureName)!.TextInfo.IsRightToLeft)
            await Expect(html).ToHaveAttributeAsync("dir", rtl);
        else
            await Expect(html).Not.ToHaveAttributeAsync("dir", rtl);
    }

    private async Task<ILocator> OpenTimeZones(IPage page)
    {
        await Expect(page.Locator("main.non-identity")).ToBeVisibleAsync();

        await ClickAppMenuItem(page, Localized(nameof(AppStrings.TimeZone)));

        var timeZones = page.Locator(".time-zone-list");
        await Expect(timeZones).ToBeVisibleAsync();

        return timeZones;
    }

    private static string Localized(string key) => AppStrings.ResourceManager.GetString(key, CultureInfoManager.GetCultureInfo(browserCulture))!;
}
