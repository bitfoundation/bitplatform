//+:cnd:noEmit
namespace Boilerplate.WorkbenchTests.Features.AppPages;

/// <summary>The Settings page's Sessions section: the current session, the other devices, the applications apart from them, and revoking.</summary>
[TestClass, TestCategory("UITest")]
public class SessionsSectionUITests : SettingsPageTest
{
    private ILocator Sessions => Section(AppStrings.SessionsTitle);

    private ILocator OtherSessions => Sessions.GetByRole(AriaRole.List, new() { Name = AppStrings.OtherSessions, Exact = true }).GetByRole(AriaRole.Listitem);

    private ILocator Applications => Sessions.GetByRole(AriaRole.List, new() { Name = AppStrings.OAuthSessionsTitle, Exact = true }).GetByRole(AriaRole.Listitem);

    private ILocator Revoke => Sessions.GetByRole(AriaRole.Button, new() { Name = AppStrings.RevokeSessionTitle, Exact = true });

    private ILocator RevokeAccess => Sessions.GetByRole(AriaRole.Button, new() { Name = AppStrings.OAuthClientRevokeAccess, Exact = true });

    private ILocator RevokeAccessDialog => View().GetByRole(AriaRole.Dialog, new() { Name = AppStrings.OAuthClientRevokeAccess, Exact = true });

    private static string LimitMessage(int used) => Message(AppStrings.PrivilegedDeviceLimitMessage, "3", used.ToString(CultureInfo.InvariantCulture));

    /// <summary>The demo user, who has the server's limit of privileged sessions; the admins have none.</summary>
    private const string LimitedUser = "?user=2";

    private const string UptimeMonitor = "Uptime monitor";

    [TestMethod, EverySurface]
    public async Task TheSection_Should_ShowTheCurrentSessionAndTheOthersWithTheirLimit(WorkbenchSurface surface)
    {
        await OpenSection(AppStrings.SessionsTitle, LimitedUser);

        await Expect(Sessions.GetByText(LimitMessage(2))).ToBeVisibleAsync();
        await Expect(Sessions.GetByText(AppStrings.CurrentSession, new() { Exact = true })).ToBeVisibleAsync();
        await Expect(Sessions.GetByText(AppStrings.OtherSessions, new() { Exact = true })).ToBeVisibleAsync();
        await Expect(Sessions.GetByText(AppStrings.Online, new() { Exact = true })).ToBeVisibleAsync();
        await Expect(Sessions.GetByText(AppStrings.Recently, new() { Exact = true })).ToBeVisibleAsync();
        await Expect(OtherSessions).ToHaveCountAsync(3);
        await Expect(Revoke).ToHaveCountAsync(3);
    }

    [TestMethod, EverySurface]
    public async Task AnAdmin_Should_HaveNoLimitOfPrivilegedSessions(WorkbenchSurface surface)
    {
        await OpenSection(AppStrings.SessionsTitle);

        await Expect(Sessions.GetByText(AppStrings.CurrentSession, new() { Exact = true })).ToBeVisibleAsync();
        await Expect(Sessions.GetByText(LimitMessage(2))).ToHaveCountAsync(0);
    }

    [TestMethod, EverySurface]
    public async Task AnApplication_Should_BeListedApartFromTheDevices(WorkbenchSurface surface)
    {
        await OpenSection(AppStrings.SessionsTitle, LimitedUser);

        await Expect(Applications).ToHaveCountAsync(1);
        await Expect(Applications).ToContainTextAsync(UptimeMonitor);
        await Expect(Applications).ToContainTextAsync(AppStrings.OAuthScopeHealthzShortDescription);
        await Expect(Sessions.GetByText(AppStrings.OAuthSessionsHint, new() { Exact = true })).ToBeVisibleAsync();

        await Expect(OtherSessions.Filter(new() { HasText = UptimeMonitor })).ToHaveCountAsync(0);
        await Expect(Applications.GetByRole(AriaRole.Button, new() { Name = AppStrings.RevokeSessionTitle, Exact = true })).ToHaveCountAsync(0);
        await Expect(Sessions.GetByText(LimitMessage(2))).ToBeVisibleAsync();
    }

    //#if (signalR == true || notification == true)
    [TestMethod, EverySurface]
    public async Task EveryDevice_Should_SayWhetherItGetsNotifications(WorkbenchSurface surface)
    {
        await OpenSection(AppStrings.SessionsTitle);

        await Expect(Sessions.GetByRole(AriaRole.Img, new() { Name = AppStrings.SessionNotificationsAllowedTitle, Exact = true })).ToHaveCountAsync(2);
        await Expect(Sessions.GetByRole(AriaRole.Img, new() { Name = AppStrings.SessionNotificationsMutedTitle, Exact = true })).ToHaveCountAsync(2);
        await Expect(Applications.GetByRole(AriaRole.Img, new() { Name = AppStrings.SessionNotificationsMutedTitle, Exact = true })).ToHaveCountAsync(0);
    }
    //#endif

    [TestMethod, EverySurface]
    public async Task Revoking_Should_RemoveTheSessionUpdateTheCountAndStayAfterAReload(WorkbenchSurface surface)
    {
        await OpenSection(AppStrings.SessionsTitle, LimitedUser);

        var privileged = OtherSessions.Filter(new() { HasText = "Rotterdam" });

        await Press(privileged.GetByRole(AriaRole.Button, new() { Name = AppStrings.RevokeSessionTitle, Exact = true }));

        await Expect(Snack(AppStrings.RemoveSessionSuccessMessage)).ToBeVisibleAsync();
        await Expect(privileged).ToHaveCountAsync(0);
        await Expect(OtherSessions).ToHaveCountAsync(2);
        await Expect(Sessions.GetByText(LimitMessage(1))).ToBeVisibleAsync();

        await WaitUntilSaved(state => state.Contains("Utrecht", StringComparison.Ordinal) && state.Contains("Rotterdam", StringComparison.Ordinal) is false);
        await ReloadTheView();

        await Expect(OtherSessions).ToHaveCountAsync(2);
        await Expect(privileged).ToHaveCountAsync(0);
    }

    [TestMethod, EverySurface]
    public async Task RevokingEveryOtherDevice_Should_LeaveTheCurrentOneAndTheApplications(WorkbenchSurface surface)
    {
        await OpenSection(AppStrings.SessionsTitle);

        for (var left = 2; left >= 0; left--)
        {
            await Press(Revoke.First);
            await Expect(OtherSessions).ToHaveCountAsync(left);
        }

        await Expect(Sessions.GetByText(AppStrings.OtherSessions, new() { Exact = true })).ToHaveCountAsync(0);
        await Expect(Sessions.GetByText(AppStrings.CurrentSession, new() { Exact = true })).ToBeVisibleAsync();
        await Expect(Applications).ToHaveCountAsync(1);
    }

    [TestMethod, EverySurface]
    public async Task RevokingAnApplication_Should_AskFirst(WorkbenchSurface surface)
    {
        await OpenSection(AppStrings.SessionsTitle);

        await Press(RevokeAccess);

        await Expect(RevokeAccessDialog.GetByText(Message(AppStrings.OAuthSessionRevokeConfirmMessage, UptimeMonitor))).ToBeVisibleAsync();

        await Press(RevokeAccessDialog.GetByRole(AriaRole.Button, new() { Name = AppStrings.Cancel, Exact = true }));

        await Expect(RevokeAccessDialog).ToHaveCountAsync(0);
        await Expect(Applications).ToHaveCountAsync(1);

        await Press(RevokeAccess);
        await Press(RevokeAccessDialog.GetByRole(AriaRole.Button, new() { Name = AppStrings.OAuthClientRevokeAccess, Exact = true }));

        await Expect(Snack(AppStrings.RemoveSessionSuccessMessage)).ToBeVisibleAsync();
        await Expect(Applications).ToHaveCountAsync(0);
        await Expect(Sessions.GetByText(AppStrings.OAuthSessionsTitle, new() { Exact = true })).ToHaveCountAsync(0);
        await Expect(OtherSessions).ToHaveCountAsync(3);
    }
}
