using Boilerplate.Client.Core.Infrastructure.Services.Contracts;

namespace Boilerplate.Tests.Features.Identity;

[TestClass, TestCategory("UITest")]
public partial class PrivilegedSessionTests : AppPageTest
{
    /// <summary>
    /// A single account signs in four times from four separate browsers (devices). Only a limited number of a user's
    /// sessions may be <c>Privileged</c> (See <see cref="AuthPolicies.PRIVILEGED_ACCESS"/> and
    /// IdentityController.UpdateUserSessionPrivilegeStatus); once that many privileged sessions already exist, every
    /// further sign-in produces a non-privileged session. With the template's default limit of three, the fourth
    /// sign-in is the first one that can't be privileged, and this test verifies from that newest session that:
    /// <list type="number">
    /// <item>The Settings &gt; Sessions page reports the privileged-devices limit as fully used - three of the three
    /// allowed - even though four sessions now exist, i.e. this newest session is <b>not</b> privileged. That page is the
    /// app's own surface for privileged-session status and, unlike the Dashboard, isn't gated behind a selected tenant or
    /// the Admin module, so it is reachable in every configuration.</item>
    /// <item>This session's access token - read straight from the browser's localStorage and parsed exactly the way the
    /// client does (See <see cref="IAuthTokenProvider.ParseAccessToken"/>) - carries the privileged-session claim
    /// (<see cref="AppClaimTypes.PRIVILEGED_SESSION"/>) as <c>false</c>. This is the definitive, configuration-independent
    /// proof that the session lost its privilege.</item>
    /// </list>
    /// </summary>
    [TestMethod]
    public async Task ExceedingPrivilegedSessionsLimit_Should_LeaveNewestSessionUnprivileged()
    {
        // The template's default privileged-sessions limit (AppSettings.Identity.MaxPrivilegedSessionsCount) is three, so
        // the fourth sign-in is the first one that falls outside the limit and is therefore non-privileged.
        const int maxPrivilegedSessions = 3;

        await using var server = new AppTestServer(Context);
        await server.Build().Start(TestContext.CancellationToken);

        var serverAddress = server.WebAppServerAddress;

        // The same brand-new account is used for every sign-in; a fresh e-mail keeps its privileged-session count
        // isolated from other (parallel) tests that share the same database.
        var email = MagicLinkSignInUtils.NewTestEmail();

        // 1st sign-in (in the default browser): the brand-new account is registered and this session becomes privileged.
        await MagicLinkSignInUtils.SignInViaMagicLinkOtp(Page, server, email, TestContext.CancellationToken);

        // Fill the remaining privileged slots with more sign-ins, each from its own isolated browser (a different device,
        // so a separate session). These stay within the limit, so they become privileged too. Each browser can close
        // right afterwards - the server-side UserSession persists regardless, keeping its privileged slot taken.
        for (var device = 0; device < maxPrivilegedSessions - 1; device++)
        {
            await using var deviceContext = await Browser.NewContextAsync(ContextOptions());
            await SetBlazorWebAssemblyServerAddress(serverAddress, deviceContext);
            var devicePage = await deviceContext.NewPageAsync();
            devicePage.SetDefaultTimeout((float)TimeSpan.FromSeconds(30).TotalMilliseconds);
            await MagicLinkSignInUtils.SignInAgainViaMagicLinkOtp(devicePage, server, email, TestContext.CancellationToken);
        }

        // The final sign-in (a fourth isolated browser): all the allowed privileged sessions already exist, so this
        // newest session is NOT privileged. Keep its browser open to inspect the session's own state below.
        await using var newestContext = await Browser.NewContextAsync(ContextOptions());
        await SetBlazorWebAssemblyServerAddress(serverAddress, newestContext);
        var newestPage = await newestContext.NewPageAsync();
        newestPage.SetDefaultTimeout((float)TimeSpan.FromSeconds(30).TotalMilliseconds);
        await MagicLinkSignInUtils.SignInAgainViaMagicLinkOtp(newestPage, server, email, TestContext.CancellationToken);

        // From the newest session, open the Settings > Sessions page.
        await newestPage.GotoAsync(new Uri(serverAddress, $"{PageUrls.Settings}/{PageUrls.SettingsSections.Sessions}").ToString(),
            new() { WaitUntil = WaitUntilState.NetworkIdle });

        // The three earlier sign-ins are listed as "other sessions" besides this current (newest) one, so four exist total.
        await Expect(newestPage.Locator(".other-session-persona")).ToHaveCountAsync(maxPrivilegedSessions);

        // The privileged-devices message reports the limit as fully used - the used count equals the allowed count (three
        // of three) - even though a fourth session exists, proving this newest sign-in did not add a privileged session.
        // The message renders the allowed count (arg 0) and the used count (arg 1) with the "N0" format (See SessionsSection.razor).
        var limit = maxPrivilegedSessions.ToString("N0", CultureInfo.CurrentCulture);
        await Expect(newestPage.GetByText(string.Format(AppStrings.PrivilegedDeviceLimitMessage, limit, limit))).ToBeVisibleAsync();

        // Definitive proof: read this session's access token straight from the browser's localStorage (where AuthManager
        // persists it, See WebStorageService) and parse it the same way the client does. Its privileged-session claim
        // must be present and false - a check that holds in every module / multi-tenant configuration.
        var accessToken = await newestPage.EvaluateAsync<string?>("() => localStorage.getItem('access_token')");
        Assert.IsNotNull(accessToken, "The newly signed-in session should have persisted its access token to localStorage.");

        var sessionClaims = IAuthTokenProvider.ParseAccessToken(accessToken, validateExpiry: false);
        Assert.AreEqual("false", sessionClaims.FindFirst(AppClaimTypes.PRIVILEGED_SESSION)?.Value,
            "The session that exceeded the privileged-sessions limit must carry the privileged-session claim as false.");
    }
}
