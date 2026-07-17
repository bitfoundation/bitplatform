namespace Boilerplate.Tests.Features.Culture;

[TestClass, TestCategory("UITest")]
public partial class CultureSelectionUITests : AppPageTest
{
    /// <summary>
    /// An anonymous visitor changes the app language from the header app-menu and the UI re-renders in the chosen language:
    /// <list type="number">
    /// <item>Open the home page (no sign-in required) and confirm the default English home message is shown.</item>
    /// <item>Open the header app-menu (the persona drop-menu), tap Language, and pick Persian (fa-IR) from the culture list. Selecting a culture writes the .AspNetCore.Culture cookie and force-reloads the culture-less URL (See <c>CultureService.ChangeCulture</c>).</item>
    /// <item>After the reload the home message now shows its Persian (fa-IR) translation, read from the resx for the fa-IR culture (never hard-coded), and the English message is gone.</item>
    /// <item>Switch back to English from the same menu and confirm the English home message returns, proving the switch is reversible.</item>
    /// </list>
    /// The culture selector only exists when invariant globalization is disabled (See <c>AppMenu.razor</c> / <c>CultureInfoManager.InvariantGlobalization</c>), so the test is inconclusive on an invariant build.
    /// </summary>
    [TestMethod, TestCategory("Localization")]
    public async Task ChangingCulture_Should_SwitchUiLanguage()
    {
        if (CultureInfoManager.InvariantGlobalization)
        {
            Assert.Inconclusive("The culture selector is only available when invariant globalization is disabled.");
            return;
        }

        await using var server = new AppTestServer(Context);
        await server.Build().Start(TestContext.CancellationToken);

        var serverAddress = server.WebAppServerAddress;

        // Read the expected home messages straight from the resx for each culture instead of hard-coding them.
        var faCulture = CultureInfoManager.GetCultureInfo("fa-IR")!;
        var defaultHomeMessage = AppStrings.ResourceManager.GetString(nameof(AppStrings.HomeMessage), CultureInfo.InvariantCulture)!;
        var faHomeMessage = AppStrings.ResourceManager.GetString(nameof(AppStrings.HomeMessage), faCulture)!;

        // The culture list labels are the hard-coded DisplayName values from CultureInfoManager.SupportedCultures
        // (e.g. "فارسی" for fa-IR, "English US" for en-US); they are not resx strings, so read them from there.
        var faDisplayName = CultureInfoManager.SupportedCultures.First(sc => sc.Culture.Name == "fa-IR").DisplayName;
        var enDisplayName = CultureInfoManager.SupportedCultures.First(sc => sc.Culture.Name == "en-US").DisplayName;
        var faLanguageLabel = AppStrings.ResourceManager.GetString(nameof(AppStrings.Language), faCulture)!;

        // The home page is public, so opening it needs no sign-in. It renders in the default (English) culture.
        await Page.GotoAsync(serverAddress.ToString(), new() { WaitUntil = WaitUntilState.NetworkIdle });
        await Expect(Page.GetByText(defaultHomeMessage)).ToBeVisibleAsync();

        // ---- Switch to Persian (fa-IR) ----

        // Open the header app-menu (the persona drop-menu) via its chevron icon.
        await Page.Locator(".menu-chevron").ClickAsync();

        // Open the culture sub-menu, then confirm the "Select language" list is showing.
        await Page.GetByRole(AriaRole.Button, new() { Name = AppStrings.Language }).ClickAsync();
        await Expect(Page.GetByText(AppStrings.SelectLanguage)).ToBeVisibleAsync();

        // Pick Persian. This writes the culture cookie and force-reloads the page (See CultureService.ChangeCulture).
        await Page.GetByText(faDisplayName, new() { Exact = true }).ClickAsync();

        // After the reload the home message is now in Persian and the English one is gone.
        await Expect(Page.GetByText(faHomeMessage)).ToBeVisibleAsync();
        await Expect(Page.GetByText(defaultHomeMessage)).ToBeHiddenAsync();

        // ---- Switch back to English ----

        // The menu chrome class stays the same across cultures, but the Language button now carries its Persian label.
        await Page.Locator(".menu-chevron").ClickAsync();
        await Page.GetByRole(AriaRole.Button, new() { Name = faLanguageLabel }).ClickAsync();
        await Page.GetByText(enDisplayName, new() { Exact = true }).ClickAsync();

        // The English home message returns, proving the language switch is reversible.
        await Expect(Page.GetByText(defaultHomeMessage)).ToBeVisibleAsync();
        await Expect(Page.GetByText(faHomeMessage)).ToBeHiddenAsync();
    }
}
