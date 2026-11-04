//+:cnd:noEmit
namespace Boilerplate.WorkbenchTests.Features.AppPages;

/// <summary>The Settings page's Sessions section: the current session, the others with the application one among them, and revoking.</summary>
[TestClass, TestCategory("UITest")]
public class SessionsSectionUITests : SettingsPageTest
{
    private ILocator Sessions => Section(AppStrings.SessionsTitle);

    private ILocator OtherSessions => Sessions.GetByRole(AriaRole.Listitem);

    private ILocator Revoke => Sessions.GetByRole(AriaRole.Button, new() { Name = AppStrings.RevokeSessionTitle, Exact = true });

    private static string LimitMessage(int used) => Message(AppStrings.PrivilegedDeviceLimitMessage, "3", used.ToString(CultureInfo.InvariantCulture));

    [TestMethod, EverySurface]
    public async Task TheSection_Should_ShowTheCurrentSessionAndTheOthersWithTheirLimit(WorkbenchSurface surface)
    {
        await OpenSection(AppStrings.SessionsTitle);

        await Expect(Sessions.GetByText(LimitMessage(2))).ToBeVisibleAsync();
        await Expect(Sessions.GetByText(AppStrings.CurrentSession, new() { Exact = true })).ToBeVisibleAsync();
        await Expect(Sessions.GetByText(AppStrings.OtherSessions, new() { Exact = true })).ToBeVisibleAsync();
        await Expect(Sessions.GetByText(AppStrings.Online, new() { Exact = true })).ToBeVisibleAsync();
        await Expect(Sessions.GetByText(AppStrings.Recently, new() { Exact = true })).ToBeVisibleAsync();
        await Expect(OtherSessions).ToHaveCountAsync(4);
        await Expect(Revoke).ToHaveCountAsync(4);
    }

    [TestMethod, EverySurface]
    public async Task AnApplicationsSession_Should_SayWhichApplicationItIs(WorkbenchSurface surface)
    {
        await OpenSection(AppStrings.SessionsTitle);

        var application = OtherSessions.Filter(new() { HasText = AppStrings.OAuthSessionAuthorizedApplication });

        await Expect(application).ToHaveCountAsync(1);
        await Expect(application).ToContainTextAsync(AppStrings.OAuthScopeHealthzShortDescription);
    }

    //#if (signalR == true || notification == true)
    [TestMethod, EverySurface]
    public async Task EverySession_Should_SayWhetherItGetsNotifications(WorkbenchSurface surface)
    {
        await OpenSection(AppStrings.SessionsTitle);

        await Expect(Sessions.GetByRole(AriaRole.Img, new() { Name = AppStrings.SessionNotificationsAllowedTitle, Exact = true })).ToHaveCountAsync(2);
        await Expect(Sessions.GetByRole(AriaRole.Img, new() { Name = AppStrings.SessionNotificationsMutedTitle, Exact = true })).ToHaveCountAsync(3);
    }
    //#endif

    [TestMethod, EverySurface]
    public async Task Revoking_Should_RemoveTheSessionUpdateTheCountAndStayAfterAReload(WorkbenchSurface surface)
    {
        await OpenSection(AppStrings.SessionsTitle);

        var privileged = OtherSessions.Filter(new() { HasText = "Rotterdam" });

        await Press(privileged.GetByRole(AriaRole.Button, new() { Name = AppStrings.RevokeSessionTitle, Exact = true }));

        await Expect(Snack(AppStrings.RemoveSessionSuccessMessage)).ToBeVisibleAsync();
        await Expect(privileged).ToHaveCountAsync(0);
        await Expect(OtherSessions).ToHaveCountAsync(3);
        await Expect(Sessions.GetByText(LimitMessage(1))).ToBeVisibleAsync();

        await ReloadTheViewOnceSaved("\"RevokedSessionIds\":[\"");

        await Expect(OtherSessions).ToHaveCountAsync(3);
        await Expect(privileged).ToHaveCountAsync(0);
    }

    [TestMethod, EverySurface]
    public async Task RevokingEveryOtherSession_Should_LeaveOnlyTheCurrentOne(WorkbenchSurface surface)
    {
        await OpenSection(AppStrings.SessionsTitle);

        for (var left = 3; left >= 0; left--)
        {
            await Press(Revoke.First);
            await Expect(OtherSessions).ToHaveCountAsync(left);
        }

        await Expect(Sessions.GetByText(AppStrings.OtherSessions, new() { Exact = true })).ToHaveCountAsync(0);
        await Expect(Sessions.GetByText(AppStrings.CurrentSession, new() { Exact = true })).ToBeVisibleAsync();
    }
}
