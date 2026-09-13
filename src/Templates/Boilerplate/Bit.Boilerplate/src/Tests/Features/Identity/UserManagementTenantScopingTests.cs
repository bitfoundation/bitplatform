//+:cnd:noEmit
using Boilerplate.Shared.Features.Tenants;

namespace Boilerplate.Tests.Features.Identity;

/// <summary>
/// <c>UserManagementController</c>'s two guards refuse with different exceptions, and therefore with different HTTP
/// status codes: <c>EnsureCallerCanRevokeSessionsOf</c> answers <c>BadRequestException</c> (400) when the target holds
/// the g-admin role, <c>EnsureUserIsInCurrentTenant</c> answers <c>ResourceNotFoundException</c> (404) when the target
/// is not an accepted member of the caller's tenant.
/// <para>
/// Asking the first question before the second turns that difference into an oracle. A tenant admin who owns nothing
/// but a user id - she cannot see the account, its tenant, or anything else about it - learns from the status code
/// alone whether it is a global administrator. So the tenant guard runs first, and every management call against a
/// user outside the caller's tenant has to be indistinguishable from every other.
/// </para>
/// </summary>
[TestClass, TestCategory("IntegrationTest")]
public partial class UserManagementTenantScopingTests
{
    public TestContext TestContext { get; set; } = default!;

    [TestMethod]
    public async Task ManagingAUserOutsideTheCurrentTenant_Should_AnswerTheSameWhetherOrNotHeIsAGlobalAdmin()
    {
        await using var server = new AppTestServer();

        await server.Build(services => services.AddIntegrationApiOnlyTestsServices()).Start(TestContext.CancellationToken);

        await using var adminScope = server.WebApp.Services.CreateAsyncScope();
        await using var outsiderScope = server.WebApp.Services.CreateAsyncScope();
        await using var globalAdminScope = server.WebApp.Services.CreateAsyncScope();

        // The caller: a tenant admin of her own, per-run tenant. A t-admin holds every g-admin feature except
        // Tenants_Manage_Global (See AppFeatures.GetTenantAdminFeatures), which is exactly the shape that makes both
        // guards live for her - Users_Manage lets her reach the endpoints, and the missing global feature means
        // neither guard short-circuits.
        var (adminEmail, _) = await TestAccountUtils.CreateAndSignIn(server, adminScope, TestContext.CancellationToken);

        await TestAccountUtils.Elevate(server, adminScope, adminEmail, TestContext.CancellationToken);

        var tenant = await adminScope.ServiceProvider.GetRequiredService<ITenantController>()
            .Create(new() { Name = $"t{Guid.NewGuid():N}" }, TestContext.CancellationToken);

        // The elevated window is stored as a claim expiry, so it survives this refresh (See AuthPolicies.ELEVATED_ACCESS)
        // and the ELEVATED_ACCESS endpoints below stay reachable.
        Assert.IsTrue(await adminScope.ServiceProvider.GetRequiredService<AuthManager>().SwitchTenant(tenant.Id, TestContext.CancellationToken),
            "The creator must be able to switch into the tenant she just created, otherwise her token carries no tenant and the rest of this test cannot run.");

        // The two targets. Both are freshly auto-provisioned, so neither has a membership in her tenant; the only
        // difference between them is the g-admin role, which is the fact that must not leak.
        var (_, outsiderUserId) = await TestAccountUtils.CreateAndSignIn(server, outsiderScope, TestContext.CancellationToken);

        var (_, globalAdminUserId) = await TestAccountUtils.CreateAndSignIn(server, globalAdminScope, TestContext.CancellationToken);

        await using var _ = await TestAccountUtils.MakeGlobalAdmin(server, globalAdminScope, globalAdminUserId, TestContext.CancellationToken);

        var userManagement = adminScope.ServiceProvider.GetRequiredService<IUserManagementController>();

        // Delete: a BadRequestException here is EnsureCallerCanRevokeSessionsOf answering ahead of the tenant guard.
        // It is a harmless-looking "you cannot remove a super admin" - and it is only ever said about a global admin.
        await Assert.ThrowsExactlyAsync<ResourceNotFoundException>(
            () => userManagement.Delete(globalAdminUserId, TestContext.CancellationToken),
            "Deleting a global admin who is outside the current tenant must be refused as not-found, exactly like any other out-of-tenant user.");

        await Assert.ThrowsExactlyAsync<ResourceNotFoundException>(
            () => userManagement.Delete(outsiderUserId, TestContext.CancellationToken),
            "The control: an ordinary out-of-tenant user answers not-found, so the assertion above is comparing against something.");

        // RevokeAllUserSessions: same pair of guards, same order, same leak.
        await Assert.ThrowsExactlyAsync<ResourceNotFoundException>(
            () => userManagement.RevokeAllUserSessions(globalAdminUserId, TestContext.CancellationToken),
            "Revoking the sessions of a global admin outside the current tenant must be refused as not-found, exactly like any other out-of-tenant user.");

        await Assert.ThrowsExactlyAsync<ResourceNotFoundException>(
            () => userManagement.RevokeAllUserSessions(outsiderUserId, TestContext.CancellationToken),
            "The control for RevokeAllUserSessions.");

        // Non-vacuity from the other end: the global-admin guard itself is still armed. Accept the outsider into her
        // tenant, hand him the g-admin role, and the 400 she must not have seen above is the correct answer here.
        await AcceptIntoTenant(server, tenant.Id, outsiderUserId, TestContext.CancellationToken);

        await using var __ = await TestAccountUtils.MakeGlobalAdmin(server, outsiderScope, outsiderUserId, TestContext.CancellationToken);

        await Assert.ThrowsExactlyAsync<BadRequestException>(
            () => userManagement.RevokeAllUserSessions(outsiderUserId, TestContext.CancellationToken),
            "Inside her own tenant the global-admin guard has to fire - otherwise the reordering has simply disabled it and these assertions pass for the wrong reason.");
    }

    /// <summary>
    /// Writes the accepted membership directly. Acceptance itself is covered end to end by
    /// <c>TenantInvitationUITests</c>; what is under test here is what the management endpoints do with the state.
    /// </summary>
    private static async Task AcceptIntoTenant(AppTestServer server, Guid tenantId, Guid userId, CancellationToken cancellationToken)
    {
        await using var scope = server.WebApp.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await dbContext.TenantUsers.AddAsync(new() { TenantId = tenantId, UserId = userId, AcceptedOn = DateTimeOffset.UtcNow }, cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
