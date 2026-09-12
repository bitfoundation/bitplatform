using Boilerplate.Tests.E2E.Infrastructure.Services;

namespace Boilerplate.Tests.E2E.Features.PushNotification;

/// <summary>
/// The delivery half of push, which exists nowhere but on a real device: the app asks Firebase for a registration
/// token, hands it to the deployment, the deployment asks FCM to deliver, Google delivers, and
/// <c>PushNotificationFirebaseMessagingService.OnMessageReceived</c> posts the notification Android then holds.
/// <para>
/// Driven through the app's own notifications switch, signed out - <c>AppMenu.ConfirmNotifications</c> then asks for
/// the welcome push through <c>TestPushNotificationSetup</c>, which is addressed to this device and no other. What is
/// asserted is what Android says it is showing, since the notification is not in the WebView at all.
/// </para>
/// <para>
/// Not parallelized: both apps run on the single connected device/emulator (See AndroidSmokeTests).
/// </para>
/// </summary>
[TestClass, TestCategory(TestCategories.Android), DoNotParallelize, Retry(2)]
public class AndroidPushNotificationTests : AppTestBase
{
    /// <summary>FCM's own round trip, plus the Hangfire job in front of it.</summary>
    private static readonly TimeSpan deliveryDeadline = TimeSpan.FromMinutes(2);

    /// <summary>The token is issued by Google Play services, which an emulator image may not have at all.</summary>
    private static readonly TimeSpan registrationDeadline = TimeSpan.FromSeconds(45);

    protected override IAppOpener AppOpener => new AndroidAppOpener();

    [TestMethod]
    [DataRow(App.Todo, DisplayName = "Todo")]
    [DataRow(App.AdminPanel, DisplayName = "AdminPanel")]
    public async Task TurningNotificationsOn_Should_PutTheWelcomePushOnTheDevice(App app)
    {
        DeployedApiClientProvider.SkipWithoutGlobalAdminCredentials();

        var applicationId = DeployedApps.AndroidAppIdOf(app)!;
        var registeredAfter = DateTimeOffset.UtcNow.AddMinutes(-1).ToUnixTimeSeconds();

        // Opened first: the launch clears the app's data, which also revokes the runtime permission granted before it.
        var page = await OpenApp(app);
        await WaitUntilInteractive(page);

        // Android 13+ asks for this in a dialog of its own. Granted from the outside so the test is about whether the
        // push arrives, not about a system dialog no WebView can answer.
        await Playwright.RunAndroidShell($"pm grant {applicationId} android.permission.POST_NOTIFICATIONS");

        // Signed out on purpose: with no session to store the preference on, the switch asks the deployment for the
        // welcome push directly, and nothing else about the account or its data is touched by the run.
        await ClickAppMenuItem(page, AppStrings.Notifications);

        var deviceId = await WaitForRegistration(registeredAfter);

        // The device's own row, and the app recreates it on the next launch - but a test's leftovers are not the
        // deployment's to keep.
        RegisterForCleanup(() => DeleteSubscription(deviceId));

        var notifications = await WaitForNotification(applicationId, AppStrings.TestPushNotificationTitle);

        Assert.Contains(AppStrings.TestPushNotificationTitle, notifications, StringComparison.Ordinal,
            $"Android is holding no notification titled '{AppStrings.TestPushNotificationTitle}' for {applicationId} within {deliveryDeadline}. Everything up to the push service is covered by PushNotificationSubscriptionTests, so what did not happen here is the delivery itself.");
    }

    /// <summary>
    /// The registration the app made with the deployment, which is also the gate: without Google Play services there is
    /// no Firebase token, the app has nothing to subscribe with, and there is no delivery to wait for.
    /// </summary>
    private async Task<string> WaitForRegistration(long registeredAfter)
    {
        var globalApiClient = await DeployedApiClientProvider.GetGlobalApiClient(TestContext.CancellationToken);

        var deadline = DateTimeOffset.UtcNow + registrationDeadline;

        while (DateTimeOffset.UtcNow < deadline)
        {
            await using var dbContext = await globalApiClient.DbContextFactory!.CreateDbContextAsync(TestContext.CancellationToken);

            var deviceId = await dbContext.PushNotificationSubscriptions.IgnoreQueryFilters()
                .Where(sub => sub.Platform == "fcmV1" && sub.RenewedOn >= registeredAfter)
                .OrderByDescending(sub => sub.RenewedOn)
                .Select(sub => sub.DeviceId)
                .FirstOrDefaultAsync(TestContext.CancellationToken);

            if (deviceId is not null)
                return deviceId;

            await Task.Delay(TimeSpan.FromSeconds(2), TestContext.CancellationToken);
        }

        Assert.Inconclusive($"The app registered no Firebase push subscription with the deployment within {registrationDeadline}. An emulator image without Google Play services issues no token, and then there is nothing for a push to be delivered to.");
        return default!;
    }

    /// <summary>
    /// Android's own record of what it is showing. <c>--noredact</c> keeps the title readable; without it the dump says
    /// REDACTED and the wait below simply runs out, which the failure message accounts for.
    /// </summary>
    private async Task<string> WaitForNotification(string applicationId, string title)
    {
        var deadline = DateTimeOffset.UtcNow + deliveryDeadline;
        var last = string.Empty;

        while (DateTimeOffset.UtcNow < deadline)
        {
            var dump = await Playwright.RunAndroidShell("dumpsys notification --noredact");

            // Only the records of the app under test: another app's notification must not pass this.
            last = string.Join(Environment.NewLine, dump.Split("NotificationRecord(", StringSplitOptions.RemoveEmptyEntries)
                                                        .Where(record => record.Contains($"pkg={applicationId}", StringComparison.Ordinal)));

            if (last.Contains(title, StringComparison.Ordinal))
                return last;

            await Task.Delay(TimeSpan.FromSeconds(3), TestContext.CancellationToken);
        }

        return last;
    }

    /// <summary>Not on the test's token: a canceled or timed out run still owes the deployment its cleanup.</summary>
    private static async Task DeleteSubscription(string deviceId)
    {
        var globalApiClient = await DeployedApiClientProvider.GetGlobalApiClient(CancellationToken.None);
        await using var dbContext = await globalApiClient.DbContextFactory!.CreateDbContextAsync(CancellationToken.None);

        await dbContext.PushNotificationSubscriptions.IgnoreQueryFilters()
            .Where(sub => sub.DeviceId == deviceId)
            .ExecuteDeleteAsync(CancellationToken.None);
    }
}
