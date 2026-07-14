using Microsoft.EntityFrameworkCore;
using Boilerplate.Tests.Features.Identity;
using Boilerplate.Server.Api.Infrastructure.Data;

namespace Boilerplate.Tests.Features.SignalR;

[TestClass, TestCategory("UITest")]
public partial class ProfileRealtimeUpdateUITests : AppPageTest
{
    /// <summary>
    /// Proves that editing a profile pushes to the user's other signed-in sessions in real time over SignalR, with no
    /// manual refresh (See UserController.Update publishing <c>PROFILE_UPDATED</c>, AppClientCoordinator relaying the
    /// SignalR <c>PUBLISH_MESSAGE</c> into the PubSub bus, and MainLayout's <c>PROFILE_UPDATED</c> subscription that
    /// swaps the cascaded current user so the header persona re-renders):
    /// <list type="number">
    /// <item>A brand-new account signs in on browser A (the default Page) with a magic link OTP, then signs in again on a second, isolated browser B - two sessions of the very same user.</item>
    /// <item>Both browsers' SignalR connections get registered on the server (their session rows carry a connection id), which is the precondition for the realtime push to actually reach browser B.</item>
    /// <item>On browser A, the user opens the Profile settings, changes the full name to a new, unique value and saves it.</item>
    /// <item>Browser B, still sitting on the home page and never reloaded, sees its header account persona update to the new full name - delivered to it over SignalR.</item>
    /// </list>
    /// A per-test unique (magic link) account is used rather than a shared seeded one on purpose: the tests run in parallel
    /// (See MSTestSettings, Workers = 2) against one shared database, so mutating a seeded account's display name would race
    /// other tests that sign into and assert on that same account. Editing only the full name also keeps the session valid -
    /// UserController.Update patches FullName/Gender/BirthDate through userManager.UpdateAsync, which does not roll the
    /// security stamp, so browser A is not signed out.
    /// </summary>
    [TestMethod]
    public async Task ProfileEdit_Should_UpdateOtherSessionHeaderPersonaInRealtime()
    {
        await using var server = new AppTestServer(Context);
        await server.Build().Start(TestContext.CancellationToken);

        var serverAddress = server.WebAppServerAddress;

        // The very same account signs into two independent browsers, so it owns two distinct sessions. A fresh e-mail
        // keeps this account (and its sessions) isolated from other tests that share the same database.
        var email = MagicLinkSignInUtils.NewTestEmail();

        // ---- Browser A: the editor (the default Page / Context). First sign-in registers and confirms the account. ----
        await MagicLinkSignInUtils.SignInViaMagicLinkOtp(Page, server, email, TestContext.CancellationToken);

        // ---- Browser B: a second session of the same user, in its own isolated browser context. ----
        await using var otherContext = await Browser.NewContextAsync(ContextOptions());
        await SetBlazorWebAssemblyServerAddress(serverAddress, otherContext);
        var otherPage = await otherContext.NewPageAsync();
        otherPage.SetDefaultTimeout((float)TimeSpan.FromSeconds(30).TotalMilliseconds);

        // The account is already confirmed now, so this repeat sign-in receives a plain OTP and opens a brand-new session.
        await MagicLinkSignInUtils.SignInAgainViaMagicLinkOtp(otherPage, server, email, TestContext.CancellationToken);

        // Browser B lands on the home page with the header account persona rendered.
        await Expect(otherPage.Locator(".bit-prs.persona").First).ToBeVisibleAsync();

        // The realtime push (UserController.Update) only targets sessions whose SignalR connection id is stored on the
        // server, so wait until both browsers' hubs are registered before editing. SignalR traffic is invisible to
        // Playwright's network waits, hence this explicit, deterministic gate on the server-side precondition.
        await WaitForConnectedSignalRSessions(server, email, expectedConnectedSessions: 2, TestContext.CancellationToken);

        // ---- Browser A: change the full name on the Profile settings section and save. ----
        // Navigating straight to /settings/profile expands the Profile accordion (See SettingsPage: DefaultExpandedKey),
        // revealing the full name field and the Save button.
        await Page.GotoAsync(new Uri(serverAddress, $"{PageUrls.Settings}/{PageUrls.SettingsSections.Profile}").ToString(),
            new() { WaitUntil = WaitUntilState.NetworkIdle });

        var newFullName = $"Realtime Update {Guid.NewGuid():N}";

        await Page.GetByPlaceholder(AppStrings.FullName).FillAsync(newFullName);
        await Page.GetByRole(AriaRole.Button, new() { Name = AppStrings.Save, Exact = true }).ClickAsync();

        // Browser A confirms the save round-tripped (See ProfileSection.SaveProfile -> SnackBarService.Success).
        await Expect(Page.GetByText(AppStrings.ProfileUpdatedSuccessfullyMessage)).ToBeVisibleAsync();

        // ---- Browser B: without any reload, its header persona reflects the new full name, pushed to it over SignalR. ----
        // The persona shows the user's DisplayName, which is FullName once it is set (See UserDto.DisplayName).
        await Expect(otherPage.Locator(".bit-prs.persona").First)
            .ToContainTextAsync(newFullName, new() { Timeout = (float)TimeSpan.FromSeconds(30).TotalMilliseconds });
    }

    /// <summary>
    /// Polls the (shared) test database until at least <paramref name="expectedConnectedSessions"/> of the account's user
    /// sessions have a SignalR connection id assigned (See AppHub.OnConnectedAsync / ChangeAuthenticationStateImplementation),
    /// i.e. both browsers' hubs are connected. Scoping by the per-test unique e-mail keeps this immune to sessions created
    /// by other parallel tests. UserSession is not tenant-aware, so it is safely readable from this bare (HttpContext-less)
    /// scope; IgnoreQueryFilters is applied only as belt-and-suspenders.
    /// </summary>
    private static async Task WaitForConnectedSignalRSessions(AppTestServer server, string email, int expectedConnectedSessions, CancellationToken cancellationToken)
    {
        var deadline = DateTimeOffset.UtcNow + TimeSpan.FromSeconds(30);

        while (true)
        {
            await using var scope = server.WebApp.Services.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var connectedSessions = await dbContext.UserSessions
                .IgnoreQueryFilters()
                .CountAsync(us => us.User!.Email == email && us.SignalRConnectionId != null, cancellationToken);

            if (connectedSessions >= expectedConnectedSessions)
                return;

            if (DateTimeOffset.UtcNow >= deadline)
                throw new TimeoutException($"Only {connectedSessions} of {expectedConnectedSessions} SignalR sessions connected for '{email}' within the timeout.");

            await Task.Delay(TimeSpan.FromMilliseconds(250), cancellationToken);
        }
    }
}
