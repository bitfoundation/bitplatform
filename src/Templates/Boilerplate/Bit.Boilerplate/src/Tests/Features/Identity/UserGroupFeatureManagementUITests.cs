using Microsoft.AspNetCore.Identity;
using Boilerplate.Server.Api.Features.Tenants;

namespace Boilerplate.Tests.Features.Identity;

[TestClass, TestCategory("UITest"), Retry(2)]
public partial class UserGroupFeatureManagementUITests : AppPageTest
{
    // The seeded store tenant admin (a t-admin, not a global admin). See UserConfiguration and UserRoleConfiguration.
    // She is only ever the ACTOR here: the user-group she edits and the member who signs in are both created per run,
    // so this test never mutates anything another test - or its own next run - depends on.
    private const string StoreAdminEmail = "store-admin@bitplatform.dev";
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
    /// <item>The tenant admin grants the "Manage roles" (Roles_Manage) feature to a user-group. Her session was just
    /// created by a password sign-in, so it is provably not elevated and the OTP step-up MUST be demanded - the test
    /// fails if it is not.</item>
    /// <item>A member of that group signs in and, because her fresh token now carries the feature, can reach the Manage roles page.</item>
    /// <item>The tenant admin removes that feature from the user-group again (still elevated, so no new prompt).</item>
    /// <item>The member signs out and back in; her new token no longer carries the feature, so the Manage roles page is off-limits.</item>
    /// </list>
    /// </summary>
    [TestMethod]
    public async Task TenantAdmin_GrantAndRevokeUserGroupFeature_Should_ControlMemberAccess()
    {
        await using var server = new AppTestServer(Context);
        await server.Build().Start(TestContext.CancellationToken);
        var serverAddress = server.WebAppServerAddress;

        // The database outlives the test run, so the group and the member are created here rather than taken from the
        // seed. Editing a seeded user-group would mean a run that dies between the grant and the revoke leaves an admin
        // feature on a group every other test's users belong to - poisoning every later run until someone wipes the
        // database by hand. Owning the arrangement outright removes that entire class of failure.
        var (userGroupName, memberEmail) = await CreateUserGroupWithMember(server);

        // ---- Browser 1: the tenant admin (the default Page / Context) ----
        await SignInWithPassword(Page, serverAddress, StoreAdminEmail, Password);

        // She adds the Manage roles page to the user-group's feature set. She has just signed in with a password and the
        // seeded store admin has no two-factor, so her session cannot already be elevated: this mutation MUST be
        // challenged, and the test says so rather than tolerating a missing prompt.
        await SetUserGroupRolesManageFeature(Page, server, userGroupName, granted: true, elevationIsRequired: true);

        // ---- Browser 2: the group's member, in her own isolated browser context ----
        await using var memberContext = await NewBrowserContext(serverAddress);
        var memberPage = await memberContext.NewPageAsync();

        // Her first sign-in happens after the grant, so her token carries the group's freshly added feature and she
        // can reach the Manage roles page.
        await SignInWithPassword(memberPage, serverAddress, memberEmail, Password);
        await AssertManageRolesPageAccessible(memberPage, serverAddress, accessible: true);

        // ---- Browser 1: the admin removes the feature from the user-group again ----
        // Her elevated window from the grant is still open, so this one legitimately may or may not prompt.
        await SetUserGroupRolesManageFeature(Page, server, userGroupName, granted: false, elevationIsRequired: false);

        // ---- Browser 2: features are captured at sign-in, so only after re-authenticating does she lose access ----
        await SignOut(memberPage, serverAddress);
        await SignInWithPassword(memberPage, serverAddress, memberEmail, Password);
        await AssertManageRolesPageAccessible(memberPage, serverAddress, accessible: false);
    }

    /// <summary>
    /// Creates this run's own user-group in the seeded store tenant plus one member of it, both named after a fresh Guid
    /// so concurrent or repeated runs can never collide. The member starts with no features at all, which is what makes
    /// the "off-limits" half of the journey meaningful.
    /// </summary>
    private async Task<(string UserGroupName, string MemberEmail)> CreateUserGroupWithMember(AppTestServer server)
    {
        await using var scope = server.WebApp.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();

        // Short enough to stay readable in the user-groups list, which is where the test clicks it.
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var userGroupName = $"test-{suffix}";
        var memberEmail = $"{userGroupName}@bitplatform.dev";

        var userGroup = new Role
        {
            Name = userGroupName,
            NormalizedName = userGroupName.ToUpperInvariant(),
            TenantId = TenantConfiguration.FallbackTenantId
        };
        await dbContext.Roles.AddAsync(userGroup, TestContext.CancellationToken);
        await dbContext.SaveChangesAsync(TestContext.CancellationToken);

        // Through the UserManager so the password is hashed and the user is normalized exactly as a real sign-up would be.
        var member = new User { Email = memberEmail, UserName = memberEmail, EmailConfirmed = true, LockoutEnabled = true };
        var result = await userManager.CreateAsync(member, Password);
        Assert.IsTrue(result.Succeeded,
            $"Creating the test member failed: {string.Join(", ", result.Errors.Select(error => error.Description))}");

        await dbContext.UserRoles.AddAsync(new()
        {
            UserId = member.Id,
            RoleId = userGroup.Id,
            TenantId = TenantConfiguration.FallbackTenantId
        }, TestContext.CancellationToken);

        // An ACCEPTED tenant membership is what makes a tenant get selected at her sign-in; without it her token carries
        // no tenant, so the group's tenant-scoped feature claims would never reach it (See IdentityController.GetTenantId).
        await dbContext.TenantUsers.AddAsync(new()
        {
            TenantId = TenantConfiguration.FallbackTenantId,
            UserId = member.Id,
            AcceptedOn = DateTimeOffset.UtcNow
        }, TestContext.CancellationToken);

        await dbContext.SaveChangesAsync(TestContext.CancellationToken);

        return (userGroupName, memberEmail);
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
    /// Adds or removes the Roles_Manage feature on the given user-group from the Manage roles page.
    /// <para>
    /// <paramref name="elevationIsRequired"/> is what makes this test a test of the step-up gate rather than of the
    /// toggle. When true the OTP prompt MUST appear, and its absence fails the run: that is the call made right after a
    /// password sign-in, where the session provably carries no elevation. When false the caller's earlier elevation may
    /// still be valid, so both outcomes are legitimate and a missing prompt is tolerated. Tolerating it on BOTH calls -
    /// which is what a bare catch(TimeoutException) does - leaves the whole file green after the ELEVATED_ACCESS policy
    /// is removed from RoleManagementController.AddClaims/DeleteClaims, since the only remaining assertion is that the
    /// mutation happened at all.
    /// </para>
    /// </summary>
    private async Task SetUserGroupRolesManageFeature(IPage page, AppTestServer server, string userGroupName, bool granted, bool elevationIsRequired)
    {
        await page.GotoAsync(new Uri(server.WebAppServerAddress, PageUrls.Roles).ToString(),
            new() { WaitUntil = WaitUntilState.NetworkIdle });

        // Everything is scoped to the user-groups card. The page's own side menu is a nav too, and a long group list can
        // end up underneath it - which shows up as Playwright reporting that the menu "intercepts pointer events".
        var userGroupsCard = page.Locator(".roles-card");

        // Searching narrows the list to this run's group, so the click target is unambiguous and sits at the top of the card.
        await userGroupsCard.GetByPlaceholder(AppStrings.SearchRolesPlaceholder).FillAsync(userGroupName);

        // The search box is debounced, so the list is briefly still the unfiltered one. Wait for a group that is always
        // there (demo) to drop out before clicking, rather than racing the re-render.
        await Expect(userGroupsCard.GetByText(AppRoles.Demo, new() { Exact = true })).ToBeHiddenAsync();

        // Select this run's user-group, then open the Features tab where each feature has an add/remove toggle.
        await userGroupsCard.GetByText(userGroupName, new() { Exact = true }).ClickAsync();
        await page.GetByText(AppStrings.Features, new() { Exact = true }).ClickAsync();

        // The toggle button sits right after the feature's label in the same row. Its glyph reflects the current state:
        // "AddTo" while unassigned, "RemoveFrom" once assigned.
        var toggle = page.GetByText(RolesManageFeatureName, new() { Exact = true }).Locator("xpath=following::button[1]");
        var targetStateIcon = granted ? RemoveFeatureIconSelector : AddFeatureIconSelector;

        // The toggle stays disabled (and its glyph reads as unassigned) until the selected group's claims finish loading, so
        // wait for it to become enabled before trusting its glyph.
        await Expect(toggle).ToBeEnabledAsync();

        // The group is this run's own, so its starting state is known: unassigned before the grant, assigned before the
        // revoke. Say so out loud - finding it already in the target state means the toggle resolved to the wrong row (or
        // the claims never loaded), and failing here beats silently skipping the mutation and blaming the assertion later.
        await Expect(toggle.Locator(granted ? AddFeatureIconSelector : RemoveFeatureIconSelector)).ToBeVisibleAsync();

        await toggle.ClickAsync();

        // Mutating a group's features needs elevated access: the OTP prompt appears and an elevated token is e-mailed
        // (and dev-logged). Filling it elevates her session (a token refresh) and then persists the role-claim change.
        var otpPrompt = page.Locator(".bit-otp-inp").First;

        var elevationWasChallenged = true;

        if (elevationIsRequired)
        {
            // No try/catch: an unelevated session that is NOT challenged is the regression this test exists to catch.
            await otpPrompt.WaitForAsync();
        }
        else
        {
            try
            {
                await otpPrompt.WaitForAsync(new() { Timeout = (float)TimeSpan.FromSeconds(10).TotalMilliseconds });
            }
            catch (TimeoutException)
            {
                // No prompt appeared: the session is still elevated from an earlier mutation, so the change was persisted directly.
                elevationWasChallenged = false;
            }
        }

        if (elevationWasChallenged)
        {
            var captured = await server.WaitForCapturedEmail(StoreAdminEmail,
                capturedEmail => capturedEmail.Kind is CapturedEmailKind.ElevatedAccess, TestContext.CancellationToken);

            await BitOtpInputUtils.FillOtpInputs(page, captured.Token!);
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
