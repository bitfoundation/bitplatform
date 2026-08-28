namespace Boilerplate.Tests.Features.TimeZone;

[TestClass, TestCategory("UITest"), Retry(2)]
public partial class TimeZoneSelectionUITests : AppPageTest
{
    /// <summary>
    /// An anonymous visitor picks a time zone from the header app-menu and the choice sticks across a refresh:
    /// <list type="number">
    /// <item>Open the public home page - no sign-in needed - and open the app-menu's "Time zone" panel. Like the
    /// culture and tenant panels it is a sub-panel of the same drop-menu, but this list holds hundreds of entries,
    /// so it carries a search box (See <c>AppMenu.ShowTimeZones</c>).</item>
    /// <item>Search for a zone other than the device's current one and pick the first hit. Selecting stores the zone
    /// id on the device under the raw <c>time-zone</c> key (See <c>TimeZoneService.ChangeTimeZone</c> and
    /// <c>WebStorageService</c>), which is what makes it work without any signed-in user.</item>
    /// <item>Picking a zone hands control back to the main menu panel and refreshes the current page, the way the
    /// tenant panel does (See <c>AppMenu.OnTimeZoneChanged</c>).</item>
    /// <item>Reload the page, reopen the panel, and assert the stored zone is the one the list shows as selected -
    /// and that it leads the list, which is how the panel presents the current zone.</item>
    /// </list>
    /// </summary>
    [TestMethod]
    public async Task AnonymousUser_Should_PickATimeZone_AndPersistAcrossRefresh()
    {
        await using var server = new AppTestServer(Context);
        await server.Build().Start(TestContext.CancellationToken);

        // The home page is public, so opening it needs no sign-in.
        await Page.GotoAsync(server.WebAppServerAddress.ToString(), new() { WaitUntil = WaitUntilState.NetworkIdle });

        var callout = Page.Locator(".app-menu-callout");

        await OpenTimeZonePanel(callout);

        // The panel opens on the device's own zone, and clicking an already-checked radio fires no change - so the
        // test must pick a zone that is NOT the current one. Both terms exist in both spellings the list can carry
        // (the Windows display names "(UTC+03:30) Tehran" / "(UTC+09:00) Osaka, Sapporo, Tokyo" and the IANA ids
        // "Asia/Tehran" / "Asia/Tokyo"), so the pick is host-OS agnostic.
        var currentZone = await Page.EvaluateAsync<string?>(
            "() => document.querySelector('.app-menu-callout input.bit-chg-inp:checked')?.value");
        var searchTerm = currentZone?.Contains("Tokyo", StringComparison.OrdinalIgnoreCase) is true ? "Tehran" : "Tokyo";

        // Narrow the hundreds of zones down through the panel's search box. The full list also contains the match,
        // so waiting for the label alone can hand the click a node that the debounced filter re-render is about to
        // replace, losing the click - waiting for the list to shrink is what proves the filtered render is on screen.
        await callout.GetByPlaceholder(AppStrings.FindTimeZone).FillAsync(searchTerm);
        await Page.WaitForFunctionAsync(
            "() => { const n = document.querySelectorAll('.app-menu-callout input.bit-chg-inp').length; return n >= 1 && n <= 5; }");

        // The first filtered label pairs with the first filtered input, whose value is the id the click selects.
        var pickedZoneId = await Page.EvaluateAsync<string?>(
            "() => document.querySelector('.app-menu-callout input.bit-chg-inp')?.value");

        await callout.Locator("label.bit-chg-itl", new() { HasText = searchTerm }).First.ClickAsync();

        // The selection is persisted on the device under the raw `time-zone` key - canonicalized to the IANA id, which
        // can differ from the id of the clicked (e.g. Windows) list entry (See TimeZoneService.ChangeTimeZone).
        // Picking also refreshes the current page (See AppMenu.OnTimeZoneChanged), so let that settle before reloading.
        var persistedTimeZoneId = (await Page.WaitForFunctionAsync("() => localStorage.getItem('time-zone')")).ToString();
        Assert.IsFalse(string.IsNullOrEmpty(persistedTimeZoneId), "Picking a time zone should persist its id in localStorage.");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // A full page refresh must keep the choice, and the reopened panel must show it as the selected entry.
        await Page.ReloadAsync(new() { WaitUntil = WaitUntilState.NetworkIdle });

        await OpenTimeZonePanel(callout);

        var selectedValueAfterReload = await Page.EvaluateAsync<string?>(
            "() => document.querySelector('.app-menu-callout input.bit-chg-inp:checked')?.value");

        Assert.AreEqual(pickedZoneId, selectedValueAfterReload,
            $"Reopening the time zone panel after a refresh must show the previously picked zone as selected (persisted as '{persistedTimeZoneId}').");

        // The current zone leads the list, so it never has to be searched for to be seen.
        var firstListedValue = await Page.EvaluateAsync<string?>(
            "() => document.querySelector('.app-menu-callout input.bit-chg-inp')?.value");

        Assert.AreEqual(selectedValueAfterReload, firstListedValue,
            "The current time zone must be the first entry of the list.");
    }

    /// <summary>
    /// Opens the header app-menu - available to anonymous users - by clicking its chevron opener, enters the
    /// "Time zone" sub-panel, and waits for its zone list, which is built asynchronously after the panel opens
    /// (the stored preference is read from device storage first), to actually be on screen.
    /// </summary>
    private async Task OpenTimeZonePanel(ILocator callout)
    {
        await Page.Locator(".menu-chevron").ClickAsync();
        await Expect(callout).ToBeVisibleAsync();

        await callout.GetByRole(AriaRole.Button, new() { Name = AppStrings.TimeZone }).ClickAsync();
        await Expect(callout.GetByPlaceholder(AppStrings.FindTimeZone)).ToBeVisibleAsync();
        await Expect(callout.Locator("input.bit-chg-inp").First).ToBeAttachedAsync();
    }
}
