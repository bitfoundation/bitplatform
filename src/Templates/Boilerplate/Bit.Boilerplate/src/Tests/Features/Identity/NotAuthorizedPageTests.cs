using Bunit;
using Microsoft.AspNetCore.Components.Authorization;
using Boilerplate.Shared.Features.Tenants;
using Boilerplate.Shared.Features.Tenants.Dtos;
using Boilerplate.Client.Core.Components.Pages;
using Boilerplate.Client.Core.Components.Pages.Tenants;
using Boilerplate.Client.Core.Components.Pages.Management;

namespace Boilerplate.Tests.Features.Identity;

[TestClass, TestCategory("UITest"), Retry(2)]
public class NotAuthorizedPageTests
{
    public Microsoft.VisualStudio.TestTools.UnitTesting.TestContext TestContext { get; set; } = default!;

    [TestMethod]
    public async Task NotAuthorizedPage_Should_PointAUserWithoutATenant_ToTheirTenants()
    {
        await using var server = new AppTestServer();
        await server.Build().Start(TestContext.CancellationToken);

        await using var ctx = server.CreateBunitContext();

        await SignInABrandNewAccount(server, ctx);

        var cut = ctx.Render<CascadingAuthenticationState>(parameters => parameters
            .AddChildContent<NotAuthorizedPage>(page => page.Add(p => p.PageType, typeof(UsersPage))));

        cut.WaitForAssertion(() => Assert.Contains(AppStrings.SelectTenantMessage, cut.Find("section").TextContent),
            timeout: TimeSpan.FromSeconds(30));

        Assert.Contains(AppStrings.ManageMyTenants, cut.Find("section").TextContent);
        Assert.DoesNotContain(AppStrings.TryRemovingOtherSessions, cut.Find("section").TextContent,
            "A brand-new account's first session is privileged, so that is not why the page was refused.");

        // Not a race: the tenants load before the message shows.
        Assert.IsEmpty(cut.FindComponents<TenantSwitchList>(),
            "A brand-new account has neither a tenant nor an invitation, so the message stands alone.");
    }

    /// <summary>A pending invitation selects no tenant at sign-in (See IdentityController.GetTenantId), so this page is where she lands.</summary>
    [TestMethod]
    public async Task NotAuthorizedPage_Should_AcceptAPendingInvitation_RightWhereTheUserIsRefused()
    {
        await using var server = new AppTestServer();
        await server.Build().Start(TestContext.CancellationToken);

        await using var ctx = server.CreateBunitContext();

        var email = await SignInABrandNewAccount(server, ctx);

        var (tenantId, tenantName) = await CreateTenantAndInvite(server, email, TestContext.CancellationToken);

        var cut = ctx.Render<CascadingAuthenticationState>(parameters => parameters
            .AddChildContent<NotAuthorizedPage>(page => page.Add(p => p.PageType, typeof(UsersPage))));

        cut.WaitForState(() => cut.FindAll("button").Any(button => button.TextContent.Contains(AppStrings.AcceptInvitation)),
            timeout: TimeSpan.FromSeconds(30));

        var accept = cut.FindAll("button").Single(button => button.TextContent.Contains(AppStrings.AcceptInvitation));

        Assert.Contains(tenantName, accept.ParentElement!.TextContent, "The invitation's Accept has to be on that tenant's own row.");

        await accept.ClickAsync(new());

        var tenant = (await ctx.Services.GetRequiredService<IUserController>().GetTenants(TestContext.CancellationToken))
            .Single(t => t.Id == tenantId);

        Assert.AreEqual(TenantMembershipStatus.Member, tenant.CurrentUserMembershipStatus, "Accept has to accept the invitation.");

        var user = (await ctx.Services.GetRequiredService<AuthenticationStateProvider>().GetAuthenticationStateAsync()).User;

        Assert.AreEqual(tenantId, user.GetTenantId(), "Accept has to sign the user into the tenant she was invited to.");
    }

    private async Task<string> SignInABrandNewAccount(AppTestServer server, BunitContext ctx)
    {
        await using var client = server.CreateAppClient();

        var identityController = client.GetController<IIdentityController>();
        var email = MagicLinkSignInUtils.NewTestEmail();

        await Assert.ThrowsExactlyAsync<BadRequestException>(
            () => identityController.SendOtp(new() { Email = email }, null, TestContext.CancellationToken));

        var captured = await server.WaitForCapturedEmail(email,
            capturedEmail => capturedEmail.Kind is CapturedEmailKind.EmailToken, TestContext.CancellationToken);

        var tokens = await identityController.ConfirmEmail(new() { Email = email, Token = captured.Token }, TestContext.CancellationToken);

        await ctx.Services.GetRequiredService<AuthManager>().StoreTokens(tokens);

        return email;
    }

    private static async Task<(Guid TenantId, string TenantName)> CreateTenantAndInvite(AppTestServer server, string inviteeEmail, CancellationToken cancellationToken)
    {
        await using var client = server.CreateAppClient();

        var (email, _) = await TestAccountUtils.CreateAndSignIn(client, cancellationToken);
        await TestAccountUtils.Elevate(client, email, cancellationToken);

        var tenantController = client.GetController<ITenantController>();
        var tenant = await tenantController.Create(new() { Name = $"t{Guid.NewGuid():N}" }, cancellationToken);

        Assert.IsTrue(await client.AuthManager.SwitchTenant(tenant.Id, cancellationToken),
            "The creator must be able to switch into her own tenant, otherwise the invitation below lands on the wrong one.");

        await tenantController.InviteUser(new() { Email = inviteeEmail }, cancellationToken);

        return (tenant.Id, tenant.Name!);
    }
}
