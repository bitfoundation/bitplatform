namespace Boilerplate.Tests.Features.PushNotification;

/// <summary>
/// The app menu's notifications switch - the device's opt-in for push and SignalR's in-app messages alike (See
/// <c>NotificationPreferenceService</c>). It starts off, and turning it on must RECORD the choice and show it even
/// when the platform refuses the push: in-app messages need no permission, so only the push is called out as blocked.
/// The permission has to be denied for that to be observable, which is what chromium reports on its own and what
/// firefox is launched to report (See AppPageTest.LaunchOptionsAsync).
/// </summary>
[TestClass, TestCategory("UITest"), Retry(2)]
public partial class PushNotificationsToggleUITests : AppPageTest
{
    private const string NotificationsEnabledStoreKey = "NotificationsEnabled"; // NotificationPreferenceService.NotificationsEnabledStoreKey

    [TestMethod]
    public async Task NotificationsSwitch_Should_RecordTheChoice_AndCallOutThePushThePlatformRefuses()
    {
        await using var server = new AppTestServer(Context);
        await server.Build().Start(TestContext.CancellationToken);

        await Page.GotoAsync(server.WebAppServerAddress.ToString(), new() { WaitUntil = WaitUntilState.NetworkIdle });

        var permission = await Page.EvaluateAsync<string>("() => Notification.permission");
        if (permission is not "denied")
        {
            // Nothing below is meaningful without a refusal to observe. "default" is no better than "granted" here:
            // the request it leads to is answered by nobody in an automated browser, so it never settles at all.
            Assert.Inconclusive($"This test needs a browser whose notification permission is denied, but it is '{permission}'.");
            return;
        }

        var callout = Page.Locator(".app-menu-callout");

        await OpenAppMenu(callout);

        var notificationsSwitch = NotificationsSwitch(callout);

        // Off until the user turns it on.
        await Expect(notificationsSwitch).ToHaveAttributeAsync("aria-checked", "false");

        await notificationsSwitch.ClickAsync();

        // The choice is stored and shown even though the browser refuses the push...
        await Expect(notificationsSwitch).ToHaveAttributeAsync("aria-checked", "true");
        await Page.WaitForFunctionAsync($"() => localStorage.getItem('{NotificationsEnabledStoreKey}') === 'true'");

        // ...which is called out under the switch instead.
        await Expect(callout).ToContainTextAsync(AppStrings.PushNotificationsBlockedMessage);
    }

    private static ILocator NotificationsSwitch(ILocator callout) => callout.GetByRole(AriaRole.Switch,
        new() { NameRegex = new("notifications", System.Text.RegularExpressions.RegexOptions.IgnoreCase) });

    /// <summary>
    /// Opens the header app menu by its chevron opener and waits for the notifications row, whose state is read
    /// asynchronously as the menu opens (See <c>AppMenu.OnDropMenuOpen</c>).
    /// </summary>
    private async Task OpenAppMenu(ILocator callout)
    {
        await Page.Locator(".menu-chevron").ClickAsync();
        await Expect(callout).ToBeVisibleAsync();
        await Expect(NotificationsSwitch(callout)).ToBeVisibleAsync();
    }
}
