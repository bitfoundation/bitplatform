using Boilerplate.Tests.Infrastructure.Components;
using Boilerplate.Tests.Infrastructure.Services;

namespace Boilerplate.Tests.Features.Identity;

[TestClass, TestCategory("UITest")]
public partial class UserGroupFeatureManagementUITests : AppPageTest
{
    // The seeded store tenant admin (a t-admin, not a global admin) and a regular member of the same tenant's
    // demo user-group. See UserConfiguration, UserRoleConfiguration and TenantUserConfiguration.
    private const string StoreAdminEmail = "store-admin@bitplatform.dev";
    private const string StoreUserEmail = "store-user@bitplatform.dev";
    private const string Password = "123456";

    // The "Manage roles" (a.k.a. "User groups") page is gated behind the Roles_Manage feature. In the role editor's
    // Features tab, each feature's leaf is labelled with its raw field name.
    private const string RolesManageFeatureName = nameof(AppFeatures.Management.Roles_Manage);

    // bit renders an icon as <i class="bit-icon bit-icon--{IconName}">, so the feature toggle's current state can be read
    // from its glyph: "AddTo" while the feature is unassigned, "RemoveFrom" once it is assigned.
    private const string AddFeatureIconSelector = ".bit-icon--AddTo";
    private const string RemoveFeatureIconSelector = ".bit-icon--RemoveFrom";

    /// <summary>
    /// End-to-end feature-set journey proving a user-group's features flow into its members' access tokens at sign-in:
    /// <list type="number">
    /// <item>The tenant admin grants the "Manage roles" (Roles_Manage) feature to the seeded demo user-group (needs elevated access).</item>
    /// <item>A demo member (store-user) signs in and, because her fresh token now carries the feature, can reach the Manage roles page.</item>
    /// <item>The tenant admin removes that feature from the demo user-group again (still elevated, so no new prompt).</item>
    /// <item>The demo member signs out and back in; her new token no longer carries the feature, so the Manage roles page is off-limits.</item>
    /// </list>
    /// </summary>
    [TestMethod, Ignore]
    public async Task TenantAdmin_GrantAndRevokeDemoGroupFeature_Should_ControlMemberAccess()
    {
        await using var server = new AppTestServer(Context);
        await server.Build().Start(TestContext.CancellationToken);
        var serverAddress = server.WebAppServerAddress;

        // ---- Browser 1: the tenant admin (the default Page / Context) ----
        await SignInWithPassword(Page, serverAddress, StoreAdminEmail, Password);

        // She adds the Manage roles page to the demo user-group's feature set.
        await SetDemoGroupRolesManageFeature(Page, server, granted: true);

        // ---- Browser 2: the demo member, in her own isolated browser context ----
        await using var memberContext = await Browser.NewContextAsync(ContextOptions());
        await SetBlazorWebAssemblyServerAddress(serverAddress, memberContext);
        var memberPage = await memberContext.NewPageAsync();
        memberPage.SetDefaultTimeout((float)TimeSpan.FromSeconds(30).TotalMilliseconds);

        // Her first sign-in happens after the grant, so her token carries the demo group's freshly added feature and she
        // can reach the Manage roles page.
        await SignInWithPassword(memberPage, serverAddress, StoreUserEmail, Password);
        await AssertManageRolesPageAccessible(memberPage, serverAddress, accessible: true);

        // ---- Browser 1: the admin removes the feature from the demo user-group again ----
        await SetDemoGroupRolesManageFeature(Page, server, granted: false);

        // ---- Browser 2: features are captured at sign-in, so only after re-authenticating does she lose access ----
        await SignOut(memberPage, serverAddress);
        await SignInWithPassword(memberPage, serverAddress, StoreUserEmail, Password);
        await AssertManageRolesPageAccessible(memberPage, serverAddress, accessible: false);
    }

    private async Task SignInWithPassword(IPage page, Uri serverAddress, string email, string password)
    {
        await page.GotoAsync(new Uri(serverAddress, PageUrls.SignIn).ToString(),
            new() { WaitUntil = WaitUntilState.NetworkIdle });

        await page.GetByPlaceholder(AppStrings.EmailPlaceholder).FillAsync(email);
        await page.GetByPlaceholder(AppStrings.PasswordPlaceholder).FillAsync(password);
        await page.GetByRole(AriaRole.Button, new() { Name = AppStrings.Continue, Exact = true }).ClickAsync();

        await Expect(page).ToHaveURLAsync(serverAddress.ToString());
    }

    /// <summary>
    /// Adds or removes the Roles_Manage feature on the seeded demo user-group from the Manage roles page. The first
    /// (grant) call needs elevated access - an elevated token is e-mailed (and dev-logged) and an OTP prompt appears;
    /// the later (revoke) call reuses that still-valid elevation, so no new prompt shows.
    /// </summary>
    private async Task SetDemoGroupRolesManageFeature(IPage page, AppTestServer server, bool granted)
    {
        await page.GotoAsync(new Uri(server.WebAppServerAddress, PageUrls.Roles).ToString(),
            new() { WaitUntil = WaitUntilState.NetworkIdle });

        // Select the seeded demo user-group, then open the Features tab where each feature has an add/remove toggle.
        await page.GetByText(AppRoles.Demo, new() { Exact = true }).ClickAsync();
        await page.GetByText(AppStrings.Features, new() { Exact = true }).ClickAsync();

        // The toggle button sits right after the feature's label in the same row. Its glyph reflects the current state:
        // "AddTo" while unassigned, "RemoveFrom" once assigned.
        var toggle = page.GetByText(RolesManageFeatureName, new() { Exact = true }).Locator("xpath=following::button[1]");
        var targetStateIcon = granted ? RemoveFeatureIconSelector : AddFeatureIconSelector;

        // The toggle stays disabled (and its glyph reads as unassigned) until the selected role's claims finish loading, so
        // wait for it to become enabled before trusting its glyph.
        await Expect(toggle).ToBeEnabledAsync();

        // Be tolerant of the demo user-group already being in the wanted state (e.g. a feature the seed already granted):
        // if the toggle is already showing the target glyph there is nothing to do and no elevation is needed.
        if (await toggle.Locator(targetStateIcon).IsVisibleAsync())
            return;

        await toggle.ClickAsync();

        // Mutating a role's features needs elevated access. The first such mutation in the session pops the OTP prompt (and
        // an elevated token is e-mailed / dev-logged); later mutations reuse that still-valid elevation, so no prompt shows.
        // Wait briefly for the prompt and only fill it when it actually appears, rather than assuming which call elevates.
        try
        {
            await page.Locator(".bit-otp-inp").First.WaitForAsync(new()
            {
                Timeout = (float)TimeSpan.FromSeconds(10).TotalMilliseconds
            });

            var captured = await server.WaitForCapturedEmail(StoreAdminEmail,
                capturedEmail => capturedEmail.Kind is CapturedEmailKind.ElevatedAccess, TestContext.CancellationToken);

            // Filling the OTP elevates her session (a token refresh) and then persists the role-claim change.
            await BitOtpInputUtils.FillOtpInputs(page, captured.Token!);
        }
        catch (TimeoutException)
        {
            // No prompt appeared: the session is still elevated from an earlier mutation, so the change was persisted directly.
        }

        // The role-claim change runs over the Blazor Server circuit (invisible to Playwright's network events), so wait on
        // the toggle's own icon flipping to its new state to know the change has been persisted before moving on.
        await Expect(toggle.Locator(targetStateIcon)).ToBeVisibleAsync();
    }

    private async Task AssertManageRolesPageAccessible(IPage page, Uri serverAddress, bool accessible)
    {
        await page.GotoAsync(new Uri(serverAddress, PageUrls.Roles).ToString(),
            new() { WaitUntil = WaitUntilState.NetworkIdle });

        await Expect(page).ToHaveTitleAsync(accessible ? AppStrings.RolesPageTitle : AppStrings.NotAuthorizedPageTitle);
    }

    private async Task SignOut(IPage page, Uri serverAddress)
    {
        await page.GotoAsync(serverAddress.ToString(), new() { WaitUntil = WaitUntilState.NetworkIdle });

        // Open the account drop-menu in the header, then start signing out (which opens a confirmation dialog).
        await page.Locator(".menu-chevron").ClickAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = AppStrings.SignOut }).ClickAsync();

        // Confirm in the dialog. Its OK button carries the same "Sign out" label as the menu button, so target the last one.
        await Expect(page.GetByText(AppStrings.SignOutPrompt)).ToBeVisibleAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = AppStrings.SignOut }).Last.ClickAsync();

        // Signing out clears the tokens and closes the dialog; wait until the confirmation prompt is gone.
        await Expect(page.GetByText(AppStrings.SignOutPrompt)).ToBeHiddenAsync();
    }
}
