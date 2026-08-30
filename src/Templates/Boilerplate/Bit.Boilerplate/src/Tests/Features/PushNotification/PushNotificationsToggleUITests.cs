namespace Boilerplate.Tests.Features.PushNotification;

/// <summary>
/// The app menu's push notifications switch - the device-level opt-in every other push path respects (See
/// <c>PushNotificationServiceBase.IsEnabled</c>). Two things about it have to hold, and neither did:
/// <list type="number">
/// <item>It must report what is true of this device, not just the stored preference, which defaults to enabled for
/// a device that was never asked - so it read ON in a browser whose notification permission was denied.</item>
/// <item>Turning it on must RECORD that intent even when the platform then refuses. The enable path used to bail
/// out before <c>SetEnabled</c>, leaving anyone who had opted out stuck that way and the automatic re-subscribe
/// short circuited - while the same thing from the settings page, which has no such pre-flight check, worked.</item>
/// </list>
/// Playwright's chromium reports the permission as denied, which is the case both points are about.
/// </summary>
[TestClass, TestCategory("UITest"), Retry(2)]
public partial class PushNotificationsToggleUITests : AppPageTest
{
    private const string OptOutStoreKey = "PushNotificationsDisabled"; // PushNotificationServiceBase.PushNotificationsDisabledStoreKey

    [TestMethod]
    public async Task PushNotificationsSwitch_Should_FollowThePlatform_AndStillRecordTheIntentWhenItRefuses()
    {
        await using var server = new AppTestServer(Context);
        await server.Build().Start(TestContext.CancellationToken);

        await Page.GotoAsync(server.WebAppServerAddress.ToString(), new() { WaitUntil = WaitUntilState.NetworkIdle });

        var permission = await Page.EvaluateAsync<string>("() => Notification.permission");
        if (permission is "granted")
        {
            // Nothing below is meaningful where notifications are granted: no refusal can be observed.
            Assert.Inconclusive("This test needs a browser that does not grant notification permission.");
            return;
        }

        // The state the old enable path could never get out of: opted out on a device that also cannot be asked.
        await Page.EvaluateAsync($"() => localStorage.setItem('{OptOutStoreKey}', 'true')");
        await Page.ReloadAsync(new() { WaitUntil = WaitUntilState.NetworkIdle });

        var callout = Page.Locator(".app-menu-callout");

        await OpenAppMenu(callout);

        var pushSwitch = callout.GetByRole(AriaRole.Switch,
            new() { NameRegex = new("push notifications", System.Text.RegularExpressions.RegexOptions.IgnoreCase) });

        await Expect(pushSwitch).ToHaveAttributeAsync("aria-checked", "false");

        await pushSwitch.ClickAsync();

        // The platform still refuses, so the switch stays off and says so...
        await Expect(Page.Locator(".snackbar")).ToContainTextAsync(AppStrings.PushNotificationsBlockedMessage);
        await Expect(pushSwitch).ToHaveAttributeAsync("aria-checked", "false");

        // ...but the opt-out is gone, which is the whole difference: a later subscribe, once the permission has
        // been granted, now goes through.
        await Page.WaitForFunctionAsync($"() => localStorage.getItem('{OptOutStoreKey}') === null");
    }

    /// <summary>
    /// Opens the header app menu by its chevron opener and waits for the push notifications row, whose state is read
    /// asynchronously as the menu opens (See <c>AppMenu.OnDropMenuOpen</c>).
    /// </summary>
    private async Task OpenAppMenu(ILocator callout)
    {
        await Page.Locator(".menu-chevron").ClickAsync();
        await Expect(callout).ToBeVisibleAsync();
        await Expect(callout.GetByRole(AriaRole.Switch,
            new() { NameRegex = new("push notifications", System.Text.RegularExpressions.RegexOptions.IgnoreCase) })).ToBeVisibleAsync();
    }
}
