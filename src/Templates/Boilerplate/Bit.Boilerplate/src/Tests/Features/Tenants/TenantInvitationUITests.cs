//+:cnd:noEmit
using Boilerplate.Tests.Features.Identity;
using Boilerplate.Tests.Infrastructure.Components;
using Boilerplate.Tests.Infrastructure.Services;

namespace Boilerplate.Tests.Features.Tenants;

[TestClass, TestCategory("UITest")]
public partial class TenantInvitationUITests : AppPageTest
{
    // The seeded store tenant admin. She's a t-admin (not a global admin), See UserConfiguration.
    private const string StoreAdminEmail = "store-admin@bitplatform.dev";
    private const string StoreAdminPassword = "123456";

    /// <summary>
    /// End-to-end multi-tenant invitation journey across two isolated browsers:
    /// <list type="number">
    /// <item>The tenant admin signs in and creates a brand-new tenant (which needs an elevated access token).</item>
    /// <item>The admin invites a brand-new e-mail address; nobody owns it yet, so the invitation itself registers the account as a pending member (See TenantController.InviteUser), and while the invitation is pending she does NOT appear in the tenant's users list.</item>
    /// <item>That freshly-created account signs in for the first time with a magic link (OTP), the same way as the identity test - proving a user created by an invitation is a fully functional account - yet with the invitation still pending she can't reach the Dashboard.</item>
    /// <item>She accepts the invitation from the "manage my tenants" page, which lets her reach the Dashboard and makes her show up in the admin's users list.</item>
    /// <item>She leaves the tenant (which also needs an elevated access token); the Dashboard becomes off-limits again and she disappears from the users list.</item>
    /// </list>
    /// </summary>
    [TestMethod]
    public async Task TenantAdmin_InviteAcceptAndLeave_Flow_Should_WorkAsExpected()
    {
        await using var server = new AppTestServer(Context);
        await server.Build().Start(TestContext.CancellationToken);
        var serverAddress = server.WebAppServerAddress;

        // ---- Browser 1: the tenant admin (the default Page / Context) ----
        await SignInWithPassword(Page, serverAddress, StoreAdminEmail, StoreAdminPassword);

        // She creates a brand-new tenant. Its name must satisfy sub-domain rules; a lowercase Guid qualifies.
        var tenantName = Guid.NewGuid().ToString();
        await CreateTenant(Page, server, tenantName);

        // ---- Browser 1: the admin invites a brand-new e-mail into the new tenant ----
        // Nobody owns this address yet, so the invitation itself registers the account: a pending member with a random
        // password and an unconfirmed e-mail (See TenantController.InviteUser -> CreateUserWithDemoRole).
        var invitedEmail = MagicLinkSignInUtils.NewTestEmail();
        await InviteUserToCurrentTenant(Page, server, invitedEmail);

        // ---- Browser 2: the invited user, in her own isolated browser context ----
        await using var invitedContext = await Browser.NewContextAsync(ContextOptions());
        await SetBlazorWebAssemblyServerAddress(serverAddress, invitedContext);
        var invitedPage = await invitedContext.NewPageAsync();
        invitedPage.SetDefaultTimeout((float)TimeSpan.FromSeconds(30).TotalMilliseconds);

        // The account the invitation just created signs in for the very first time with the magic link OTP, exactly the
        // way a self-registered user would (the same flow as MagicLinkSignInTests). Getting signed in proves that a user
        // created by an invitation is a fully functional account, not merely a placeholder membership row.
        await MagicLinkSignInUtils.SignInViaMagicLinkOtp(invitedPage, server, invitedEmail, TestContext.CancellationToken);

        //#if (module == "Admin")
        // Her invitation is still pending (she hasn't switched into the tenant), so no tenant is selected for her (See
        // IdentityController.GetTenantId, which only returns accepted memberships) and she can't reach the Dashboard yet.
        await AssertDashboardAccessible(invitedPage, serverAddress, accessible: false);
        //#endif

        // ---- Browser 1: signing in accepted nothing, so she still must not appear in the tenant's users list ----
        await AssertUserInTenantUsersList(Page, serverAddress, invitedEmail, shouldExist: false);

        // ---- Browser 2: she accepts the invitation from the "manage my tenants" page ----
        await AcceptTenantInvitation(invitedPage, serverAddress, tenantName);

        //#if (module == "Admin")
        // Now she can reach the Dashboard (Demo role + a selected tenant).
        await AssertDashboardAccessible(invitedPage, serverAddress, accessible: true);
        //#endif

        // ---- Browser 1: she now shows up in the tenant's users list ----
        await AssertUserInTenantUsersList(Page, serverAddress, invitedEmail, shouldExist: true);

        // ---- Browser 2: she leaves the tenant ----
        await LeaveTenant(invitedPage, server, tenantName, invitedEmail);

        //#if (module == "Admin")
        // The Dashboard is off-limits for her again (hidden from the menu / not authorized).
        await AssertDashboardAccessible(invitedPage, serverAddress, accessible: false);
        //#endif

        // ---- Browser 1: she disappears from the tenant's users list again ----
        await AssertUserInTenantUsersList(Page, serverAddress, invitedEmail, shouldExist: false);
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

    private async Task CreateTenant(IPage page, AppTestServer server, string tenantName)
    {
        await page.GotoAsync(new Uri(server.WebAppServerAddress, PageUrls.ManageMyTenants).ToString(),
            new() { WaitUntil = WaitUntilState.NetworkIdle });

        // She already belongs to the seeded store tenant, so the "create" section starts collapsed; expand it.
        await page.GetByText(AppStrings.CreateNewTenant).First.ClickAsync();

        // The create and the rename forms share the same "tenant name" placeholder; the create one comes first in the DOM.
        // Expanding the create section resets its form (See ManageMyTenantsPage.OnSectionExpand); that async re-render can
        // wipe a value filled too eagerly, so fill and confirm it stuck before submitting.
        await FillEnsuringStable(page.GetByPlaceholder(AppStrings.EnterTenantName).First, tenantName);
        await page.GetByRole(AriaRole.Button, new() { Name = AppStrings.Save, Exact = true }).First.ClickAsync();

        // Creating a tenant needs elevated access: an elevated token is e-mailed (and dev-logged) and the OTP prompt appears.
        await page.Locator(".bit-otp-inp").First.WaitForAsync();

        var captured = await server.WaitForCapturedEmail(StoreAdminEmail, capturedEmail => capturedEmail.Kind is CapturedEmailKind.ElevatedAccess, TestContext.CancellationToken);
        var elevatedToken = captured.Token!;
        await BitOtpInputUtils.FillOtpInputs(page, elevatedToken);

        // After elevation the tenant gets created and she is switched into it, so it shows up as her current tenant.
        await Expect(page.GetByText(tenantName).First).ToBeVisibleAsync();
    }

    private async Task InviteUserToCurrentTenant(IPage page, AppTestServer server, string email)
    {
        await page.GotoAsync(new Uri(server.WebAppServerAddress, PageUrls.ManageMyTenants).ToString(),
            new() { WaitUntil = WaitUntilState.NetworkIdle });

        // Expand the "Invite user to <tenant>" section (only the current tenant's admin sees it). Its title carries the
        // tenant name, so match it by its stable prefix.
        var inviteHeaderPrefix = AppStrings.InviteUserToTenant.Replace("{0}", "").Trim();
        await page.GetByText(inviteHeaderPrefix).First.ClickAsync();

        await page.GetByPlaceholder(AppStrings.EmailPlaceholder).FillAsync(email);
        await page.GetByRole(AriaRole.Button, new() { Name = AppStrings.Invite, Exact = true }).ClickAsync();

        // Inviting also needs elevated access, but she is still elevated from creating the tenant, so no new prompt shows.
        // The success snackbar confirms the server processed the invitation; then let its e-mail background job drain.
        await Expect(page.GetByText(AppStrings.UserInvitedSuccessfullyMessage)).ToBeVisibleAsync();
        await server.WaitForBackgroundJobsToComplete(TestContext.CancellationToken);
    }

    private async Task AssertUserInTenantUsersList(IPage page, Uri serverAddress, string email, bool shouldExist)
    {
        // Reload the Users page so it re-fetches the current tenant's users afresh.
        await page.GotoAsync(new Uri(serverAddress, PageUrls.Users).ToString(),
            new() { WaitUntil = WaitUntilState.NetworkIdle });

        // The list only holds users of the current tenant that have accepted their invitation (See UserManagementController.GetAllUsers).
        await page.GetByPlaceholder(AppStrings.SearchUsersPlaceholder).FillAsync(email);

        var userItem = page.GetByText(email);

        if (shouldExist)
        {
            await Expect(userItem.First).ToBeVisibleAsync();
        }
        else
        {
            // With the pending/absent user filtered out, the list is empty and shows the "no users" message.
            await Expect(page.GetByText(AppStrings.NoUserMessage)).ToBeVisibleAsync();
            await Expect(userItem).ToHaveCountAsync(0);
        }
    }

    private async Task AcceptTenantInvitation(IPage page, Uri serverAddress, string tenantName)
    {
        await page.GotoAsync(new Uri(serverAddress, PageUrls.ManageMyTenants).ToString(),
            new() { WaitUntil = WaitUntilState.NetworkIdle });

        // Her pending invitation shows up as a tenant card with an "Accept" action. The button carries an icon whose
        // glyph leaks into its accessible name, so match the name by substring (the default) rather than exactly.
        await Expect(page.GetByText(tenantName).First).ToBeVisibleAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = AppStrings.AcceptInvitation }).ClickAsync();

        // Accepting switches her into the tenant (no elevated access needed for switching), so it becomes her current
        // tenant and the "Accept" action disappears from its card.
        await Expect(page.GetByRole(AriaRole.Button, new() { Name = AppStrings.AcceptInvitation }))
            .ToHaveCountAsync(0);
    }

    private async Task LeaveTenant(IPage page, AppTestServer server, string tenantName, string email)
    {
        await page.GotoAsync(new Uri(server.WebAppServerAddress, PageUrls.ManageMyTenants).ToString(),
            new() { WaitUntil = WaitUntilState.NetworkIdle });

        // The Leave/Accept buttons carry icons whose glyphs leak into their accessible names, so match by substring.
        await page.GetByRole(AriaRole.Button, new() { Name = AppStrings.LeaveTenant }).ClickAsync();

        // Leaving needs elevated access; she has none, so an elevated token is e-mailed (and dev-logged) and the OTP prompt appears.
        await page.Locator(".bit-otp-inp").First.WaitForAsync();

        var captured = await server.WaitForCapturedEmail(email, capturedEmail => capturedEmail.Kind is CapturedEmailKind.ElevatedAccess, TestContext.CancellationToken);
        var elevatedToken = captured.Token!;
        await BitOtpInputUtils.FillOtpInputs(page, elevatedToken);

        // Leaving resets her membership to "not accepted", so the tenant reverts to a pending invitation (its "Accept"
        // action reappears) and she is no longer signed into it.
        await Expect(page.GetByRole(AriaRole.Button, new() { Name = AppStrings.AcceptInvitation }))
            .ToBeVisibleAsync();
    }

    //#if (module == "Admin")
    private async Task AssertDashboardAccessible(IPage page, Uri serverAddress, bool accessible)
    {
        await page.GotoAsync(new Uri(serverAddress, PageUrls.Dashboard).ToString(),
            new() { WaitUntil = WaitUntilState.NetworkIdle });

        await Expect(page).ToHaveTitleAsync(accessible ? AppStrings.Dashboard : AppStrings.NotAuthorizedPageTitle);
    }
    //#endif

    /// <summary>
    /// Fills a text field and makes sure the value survives, retrying if an async re-render (e.g. a form that resets
    /// itself when its accordion section finishes expanding) wipes a value that was filled a moment too early.
    /// </summary>
    private static async Task FillEnsuringStable(ILocator field, string value)
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            await field.FillAsync(value);

            // Give any pending reset a moment to land, then confirm our value is still there.
            await field.Page.WaitForTimeoutAsync((float)TimeSpan.FromMilliseconds(500).TotalMilliseconds);

            if (await field.InputValueAsync() == value)
                return;
        }

        throw new InvalidOperationException($"Could not keep the field filled with '{value}'.");
    }
}
