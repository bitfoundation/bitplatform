//+:cnd:noEmit
using Boilerplate.Shared.Features.Tenants;
using Boilerplate.Server.Api.Features.Tenants;

namespace Boilerplate.Tests.Features.Tenants;

/// <summary>
/// Two things an invitation must NOT be, both of which it was.
/// <list type="number">
/// <item><b>A read of the invitee's profile.</b> <c>InviteUser</c> grants the tenant's demo role immediately, before
/// the invitation is accepted, so every tenant-scoped read has to gate on the accepted membership rather than on the
/// role. <c>RoleManagementController.GetUsers</c> was the one that gated on the role alone, which handed a tenant
/// admin the whole <c>UserDto</c> - e-mail, phone number, full name, gender, birth date - of anyone they merely typed
/// into the invite box.</item>
/// <item><b>Permanent.</b> A pending membership could not be removed by anybody: the invitee's <c>LeaveTenant</c> set
/// <c>AcceptedOn</c> to null on a row where it was already null (reporting success and changing nothing), and the
/// tenant admin's <c>Delete</c> refuses rows that are not accepted. The tenant title and the inviter's display name
/// are both attacker-chosen free text, so that is a permanent card in a stranger's tenant list.</item>
/// </list>
/// <para>
/// Driven through the shipped endpoints with per-run accounts and a per-run tenant, because <c>IStorageService</c> is
/// registered per scope - so each scope holds its own signed-in identity and the typed proxies carry the right bearer
/// token without any faking. The per-run tenant is left behind, exactly as <c>TenantInvitationUITests</c> leaves the
/// one it creates: its name is a Guid, so it matches no host and no later run.
/// </para>
/// </summary>
[TestClass, TestCategory("IntegrationTest")]
public partial class TenantInvitationIsolationTests
{
    public TestContext TestContext { get; set; } = default!;

    [TestMethod]
    public async Task PendingInvitation_Should_BeInvisibleToTheTenantAndRemovableByTheInvitee()
    {
        await using var server = new AppTestServer();

        await server.Build(services => services.AddIntegrationApiOnlyTestsServices()).Start(TestContext.CancellationToken);

        await using var adminScope = server.WebApp.Services.CreateAsyncScope();
        await using var inviteeScope = server.WebApp.Services.CreateAsyncScope();

        // ---- The tenant admin: her own account, her own tenant, so nothing here touches the seeded store tenant ----
        var (adminEmail, _) = await TestAccountUtils.CreateAndSignIn(server, adminScope, TestContext.CancellationToken);

        await TestAccountUtils.Elevate(server, adminScope, adminEmail, TestContext.CancellationToken);

        // Creating a tenant makes the creator its t-admin, with an already-accepted membership (See TenantController.Create).
        var tenant = await adminScope.ServiceProvider.GetRequiredService<ITenantController>()
            .Create(new() { Name = $"t{Guid.NewGuid():N}" }, TestContext.CancellationToken);

        Assert.IsTrue(await adminScope.ServiceProvider.GetRequiredService<AuthManager>().SwitchTenant(tenant.Id, TestContext.CancellationToken),
            "The creator must be able to switch into the tenant she just created, otherwise her token carries no tenant and the rest of this test cannot run.");

        // ---- The invitee: an ordinary account that already exists, so the invitation only adds a membership ----
        var (inviteeEmail, inviteeUserId) = await TestAccountUtils.CreateAndSignIn(server, inviteeScope, TestContext.CancellationToken);

        await adminScope.ServiceProvider.GetRequiredService<ITenantController>()
            .InviteUser(new() { Email = inviteeEmail }, TestContext.CancellationToken);

        var demoRoleId = await ReadTenantDemoRoleId(server, tenant.Id, TestContext.CancellationToken);

        var roleManagementController = adminScope.ServiceProvider.GetRequiredService<IRoleManagementController>();

        // The invitation itself already gave her the tenant's demo role (See UserManagerExtensions.AssignDemoRole), so
        // this is the exact state in which gating on the role alone leaks her profile.
        await AssertUserHoldsTenantRole(server, inviteeUserId, demoRoleId, TestContext.CancellationToken);

        var membersWhilePending = await roleManagementController.GetUsers(demoRoleId, TestContext.CancellationToken);

        Assert.IsFalse(membersWhilePending.Any(u => u.Id == inviteeUserId),
            $"A pending invitee was returned by RoleManagement/GetUsers, which hands the tenant admin her e-mail, phone number, full name, gender and birth date for an invitation she never accepted. Returned: [{string.Join(", ", membersWhilePending.Select(u => u.Email))}].");

        // Non-vacuity: the very same call must find her the moment the membership is accepted. Without this the
        // assertion above would also pass against an endpoint that returns nothing at all.
        await SetInvitationAcceptedOn(server, tenant.Id, inviteeUserId, DateTimeOffset.UtcNow, TestContext.CancellationToken);

        var membersOnceAccepted = await roleManagementController.GetUsers(demoRoleId, TestContext.CancellationToken);

        Assert.IsTrue(membersOnceAccepted.Any(u => u.Id == inviteeUserId),
            "Once the invitation is accepted the member must show up - otherwise the filter is simply hiding everyone and proves nothing.");

        // ---- Back to pending, and this time she declines it ----
        await SetInvitationAcceptedOn(server, tenant.Id, inviteeUserId, null, TestContext.CancellationToken);

        await TestAccountUtils.Elevate(server, inviteeScope, inviteeEmail, TestContext.CancellationToken);

        await inviteeScope.ServiceProvider.GetRequiredService<IUserController>()
            .LeaveTenant(tenant.Id, TestContext.CancellationToken);

        await using var assertScope = server.WebApp.Services.CreateAsyncScope();
        var dbContext = assertScope.ServiceProvider.GetRequiredService<AppDbContext>();

        Assert.IsFalse(await dbContext.TenantUsers.AnyAsync(tu => tu.TenantId == tenant.Id && tu.UserId == inviteeUserId, TestContext.CancellationToken),
            "Leaving a membership that was still pending has to delete it. Setting AcceptedOn to null on a row where it is already null changes nothing while still reporting success, and the tenant admin cannot remove it either (UserManagementController.EnsureUserIsInCurrentTenant only reaches accepted memberships) - so the invitation would be permanent.");
    }

    private static async Task<Guid> ReadTenantDemoRoleId(AppTestServer server, Guid tenantId, CancellationToken cancellationToken)
    {
        await using var scope = server.WebApp.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        return await dbContext.Roles
            .Where(r => r.TenantId == tenantId && r.Name == AppRoles.Demo)
            .Select(r => r.Id)
            .SingleAsync(cancellationToken);
    }

    private static async Task AssertUserHoldsTenantRole(AppTestServer server, Guid userId, Guid roleId, CancellationToken cancellationToken)
    {
        await using var scope = server.WebApp.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        Assert.IsTrue(await dbContext.UserRoles.AnyAsync(ur => ur.UserId == userId && ur.RoleId == roleId, cancellationToken),
            "The invitation is supposed to grant the tenant's demo role up front; if it stopped doing that, this test is no longer covering the case it was written for.");
    }

    private static async Task SetInvitationAcceptedOn(AppTestServer server, Guid tenantId, Guid userId, DateTimeOffset? acceptedOn, CancellationToken cancellationToken)
    {
        await using var scope = server.WebApp.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Written directly rather than through Refresh's tenant switch: acceptance itself is already covered end to end
        // by TenantInvitationUITests, and what is under test here is what each endpoint does with the resulting state.
        var affectedRows = await dbContext.TenantUsers
            .Where(tu => tu.TenantId == tenantId && tu.UserId == userId)
            .ExecuteUpdateAsync(tu => tu.SetProperty(t => t.AcceptedOn, acceptedOn), cancellationToken);

        Assert.AreEqual(1, affectedRows, "The invitation row this test arranges should exist.");
    }
}
