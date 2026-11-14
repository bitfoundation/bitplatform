namespace Bit.BlazorUI.Tests.E2E.Extras;

/// <summary>
/// Drives BitNotificationCenter at /e2e/notification-center in a real browser: the bell counting the notifications of
/// the page's snack bar, the panel opened from the keyboard with the focus moved into it and checked by axe, Escape
/// handing the focus back to the bell, and Clear all.
/// </summary>
[TestClass]
public class BitNotificationCenterE2ETests : E2ETestBase
{
    private const string Route = "/e2e/notification-center";

    private ILocator Bell => Page.Locator("#ntc .bit-drm-btn");

    private ILocator Panel => Page.Locator(".bit-ntc-cal");

    [TestMethod]
    public async Task Bell_CountsWhatIsUnread_AndOpeningItFromTheKeyboard_ReadsItWithTheFocusInThePanel()
    {
        await OpenAsync(Route);
        await Expect(Bell).ToHaveAttributeAsync("aria-label", "Notifications");

        await Page.Locator("#btn-info").ClickAsync();
        await Page.Locator("#btn-error").ClickAsync();

        await Expect(Bell.Locator(".bit-ntc-bdg")).ToHaveTextAsync("2");
        await Expect(Bell).ToHaveAttributeAsync("aria-label", "Notifications, 2 unread");

        await Bell.FocusAsync();
        await Page.Keyboard.PressAsync("Enter");

        await Expect(Panel).ToBeVisibleAsync();
        await Expect(Panel).ToHaveAttributeAsync("role", "dialog");
        await Expect(Bell).ToHaveAttributeAsync("aria-expanded", "true");
        // The first control of the panel takes the focus as it opens.
        await Expect(Panel.Locator(".bit-ntc-clr")).ToBeFocusedAsync();
        await Expect(Panel.Locator(".bit-ntc-itt")).ToHaveTextAsync(["Unread: Upload failed", "Unread: New comment"]);
        await Expect(Panel.Locator(".bit-ntc-itm.bit-ntc-new")).ToHaveCountAsync(2);
        await Expect(Panel.Locator("time.bit-ntc-tim")).ToHaveCountAsync(2);
        await Expect(Bell.Locator(".bit-ntc-bdg")).ToHaveCountAsync(0);
        await Expect(Bell).ToHaveAttributeAsync("aria-label", "Notifications");
        await AssertNoAxeViolationsAsync(".bit-ntc-cal");

        await Page.Keyboard.PressAsync("Escape");

        await Expect(Panel).Not.ToBeVisibleAsync();
        await Expect(Bell).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task ClearAll_EmptiesTheList()
    {
        await OpenAsync(Route);
        await Page.Locator("#btn-info").ClickAsync();
        await Expect(Bell.Locator(".bit-ntc-bdg")).ToHaveTextAsync("1");

        await Bell.ClickAsync();
        await Panel.Locator(".bit-ntc-clr").ClickAsync();

        await Expect(Panel.Locator(".bit-ntc-emp")).ToHaveTextAsync("No items found");
        await Expect(Panel.Locator(".bit-ntc-itm")).ToHaveCountAsync(0);
        await Expect(Panel.Locator(".bit-ntc-clr")).ToHaveCountAsync(0);
    }
}
