namespace Boilerplate.Tests.Features.TimeZone;

[TestClass, TestCategory("UITest"), Retry(2)]
public partial class TimeZoneSelectionUITests : AppPageTest
{
    /// <summary>
    /// An anonymous visitor picks a time zone from the header app-menu and the choice sticks across a refresh:
    /// <list type="number">
    /// <item>Open the public home page - no sign-in needed - and open the app-menu's "Time zone" panel. Like the
    /// culture and tenant panels it is a sub-panel of the same drop-menu, but this list holds hundreds of entries,
    /// so it is virtualized and carries a search box (See <c>AppMenu.ShowTimeZones</c>).</item>
    /// <item>Search for a zone other than the device's current one and pick the first hit. Selecting stores the zone
    /// id on the device under the raw <c>time-zone</c> key (See <c>TimeZoneService.ChangeTimeZone</c> and
    /// <c>WebStorageService</c>), which is what makes it work without any signed-in user.</item>
    /// <item>Picking a zone soft restarts the app, so every date/time is re-rendered in the new zone and the menu
    /// comes back closed rather than on its main panel (See <c>ClientAppMessages.SOFT_RESTART</c>).</item>
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

        // The panel opens on the device's own zone, and clicking the already selected row changes nothing - so the
        // test must pick a zone that is NOT the current one. Both terms exist in both spellings the list can carry
        // (the Windows display names "(UTC+03:30) Tehran" / "(UTC+09:00) Osaka, Sapporo, Tokyo" and the IANA ids,
        // shown without their area: "Tehran" / "Tokyo"), so the pick is host-OS agnostic.
        var currentZone = await Page.EvaluateAsync<string?>(
            "() => document.querySelector('.app-menu-callout .time-zone-item[aria-checked=\"true\"]')?.innerText");
        var searchTerm = currentZone?.Contains("Tokyo", StringComparison.OrdinalIgnoreCase) is true ? "Tehran" : "Tokyo";

        // Narrow the hundreds of zones down through the panel's search box. The full list also contains the match,
        // so waiting for the label alone can hand the click a node that the debounced filter re-render is about to
        // replace, losing the click - waiting for the list to shrink is what proves the filtered render is on screen.
        await callout.GetByPlaceholder(AppStrings.FindTimeZone).FillAsync(searchTerm);
        await Page.WaitForFunctionAsync(
            "() => { const n = document.querySelectorAll('.app-menu-callout .time-zone-item').length; return n >= 1 && n <= 5; }");

        var pickedZoneText = await Page.EvaluateAsync<string?>(
            "() => document.querySelector('.app-menu-callout .time-zone-item')?.innerText.trim()");

        await callout.Locator(".time-zone-item").First.ClickAsync();

        // The selection is persisted on the device under the raw `time-zone` key - canonicalized to the IANA id, which
        // can differ from the id of the clicked (e.g. Windows) list entry (See TimeZoneService.ChangeTimeZone).
        // Picking also soft restarts the app, so let that settle before reloading.
        var persistedTimeZoneId = (await Page.WaitForFunctionAsync("() => localStorage.getItem('time-zone')")).ToString();
        Assert.IsFalse(string.IsNullOrEmpty(persistedTimeZoneId), "Picking a time zone should persist its id in localStorage.");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // A full page refresh must keep the choice, and the reopened panel must show it as the selected entry.
        await Page.ReloadAsync(new() { WaitUntil = WaitUntilState.NetworkIdle });

        await OpenTimeZonePanel(callout);

        var selectedTextAfterReload = await Page.EvaluateAsync<string?>(
            "() => document.querySelector('.app-menu-callout .time-zone-item[aria-checked=\"true\"]')?.innerText.trim()");

        Assert.AreEqual(pickedZoneText, selectedTextAfterReload,
            $"Reopening the time zone panel after a refresh must show the previously picked zone as selected (persisted as '{persistedTimeZoneId}').");

        // The current zone leads the list, so it never has to be searched for. Virtualization renders the rows
        // around the top of the scroll region, so the leading one is always among them.
        var firstListedText = await Page.EvaluateAsync<string?>(
            "() => document.querySelector('.app-menu-callout .time-zone-item')?.innerText.trim()");

        Assert.AreEqual(selectedTextAfterReload, firstListedText,
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
        await Expect(callout.Locator(".time-zone-item").First).ToBeVisibleAsync();
    }
}
