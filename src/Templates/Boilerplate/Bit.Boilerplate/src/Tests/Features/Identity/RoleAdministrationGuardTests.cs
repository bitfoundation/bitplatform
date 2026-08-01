using Microsoft.EntityFrameworkCore;
using Boilerplate.Shared.Features.Identity;
using Boilerplate.Server.Api.Infrastructure.Data;

namespace Boilerplate.Tests.Features.Identity;

/// <summary>
/// Two ways the role-management surface can quietly hand out - or lock away - more than the caller asked for. Both are
/// invisible from the endpoint's own code: one is a guard that is simply absent next to five siblings that have it, the
/// other is an <c>IdentityResult.Success</c> returned for a delete that matched nothing.
/// </summary>
[TestClass, TestCategory("IntegrationTest")]
public class RoleAdministrationGuardTests
{
    /// <summary>
    /// <c>RemoveAllUsersFromRole</c> deletes every <c>UserRole</c> row for a role in one statement, which bypasses the
    /// last-global-admin guard <c>ToggleUserRole</c> applies one row at a time. Pointed at g-admin - whose id is a
    /// literal in <c>RoleConfiguration</c> and therefore needs no enumeration - it removes EVERY global administrator at
    /// once, and <c>ToggleUserRole</c> then refuses to grant g-admin back to anyone because doing so itself requires
    /// being a global admin. There is no way back from inside the app, which is what makes this worth a test.
    /// <para>
    /// Only g-admin is protected: emptying any other role, <c>demo</c> and <c>t-admin</c> included, is an ordinary bulk
    /// operation and a global admin can always undo it. The second half of this test pins that, so the guard cannot be
    /// widened back into "no built-in role may be emptied".
    /// </para>
    /// </summary>
    [TestMethod]
    public async Task RemoveAllUsersFromRole_Should_RefuseOnlyTheGlobalAdminRole()
    {
        await using var server = await StartServer();
        await using var scope = server.WebApp.Services.CreateAsyncScope();
        var (roleManagementController, signedInUserId, grant) = await SignInAsGlobalAdmin(server, scope);
        await using var globalAdminGrant = grant;

        var globalAdminRoleId = await ReadRoleId(server, AppRoles.GlobalAdmin);

        var assignmentsBefore = await CountRoleAssignments(server, globalAdminRoleId);
        Assert.IsGreaterThan(0, assignmentsBefore, "This test just granted itself the role, so there is at least one assignment.");

        await Assert.ThrowsExactlyAsync<BadRequestException>(
            () => roleManagementController.RemoveAllUsersFromRole(globalAdminRoleId, TestContext.CancellationToken),
            "Emptying g-admin removes the last global admin, and no endpoint can appoint another one.");

        Assert.AreEqual(assignmentsBefore, await CountRoleAssignments(server, globalAdminRoleId),
            "The rejected call must not have deleted anything.");

        // Every other role can be emptied. Asserted against a role this test creates and assigns itself to, NOT against
        // the seeded demo role: the database outlives the run, so emptying a seeded role would strip a seeded account's
        // membership permanently and every later run - and the running app - would inherit the damage.
        var ownRoleId = await CreateRole(roleManagementController);
        await roleManagementController.ToggleUserRole(new() { RoleId = ownRoleId, UserId = signedInUserId }, TestContext.CancellationToken);

        Assert.AreEqual(1, await CountRoleAssignments(server, ownRoleId), "The role should now have exactly this test's account in it.");

        await roleManagementController.RemoveAllUsersFromRole(ownRoleId, TestContext.CancellationToken);

        Assert.AreEqual(0, await CountRoleAssignments(server, ownRoleId),
            "Only g-admin is protected; emptying any other role must be allowed.");
    }

    /// <summary>
    /// <c>UpdateClaims</c> has to REPLACE a single-valued claim. It used to remove by <c>(ClaimType, ClaimValue)</c>
    /// using the value being written, so when the value had changed the delete matched nothing, returned
    /// <c>IdentityResult.Success</c>, and the add left a second row behind. Nothing fails at that point - the damage
    /// shows up in the readers: the token builder takes <c>Max()</c> over every <c>mx-p-s</c> row, so an administrator
    /// LOWERING the privileged-session cap silently left the old, higher value in force, and a <c>-1</c> (unlimited)
    /// set once could never be taken back.
    /// </summary>
    [TestMethod]
    public async Task UpdateClaims_Should_ReplaceTheClaimValue_RatherThanAccumulateRows()
    {
        await using var server = await StartServer();
        await using var scope = server.WebApp.Services.CreateAsyncScope();
        var (roleManagementController, _, grant) = await SignInAsGlobalAdmin(server, scope);
        await using var globalAdminGrant = grant;

        var roleId = await CreateRole(roleManagementController);

        await roleManagementController.AddClaims(roleId,
            [new() { ClaimType = AppClaimTypes.MAX_PRIVILEGED_SESSIONS, ClaimValue = "5" }], TestContext.CancellationToken);

        // The administrator tightens the limit. This is the direction that used to be a no-op.
        await roleManagementController.UpdateClaims(roleId,
            [new() { ClaimType = AppClaimTypes.MAX_PRIVILEGED_SESSIONS, ClaimValue = "2" }], TestContext.CancellationToken);

        var values = await ReadClaimValues(server, roleId, AppClaimTypes.MAX_PRIVILEGED_SESSIONS);

        Assert.HasCount(1, values,
            $"A single-valued claim must end up as exactly one row; found [{string.Join(", ", values)}]. Two rows mean the " +
            "reader's Max() keeps the old value, so the cap can only ever be raised.");
        Assert.AreEqual("2", values[0], "The stored value must be the one the administrator just saved.");
    }

    /// <summary>
    /// The multi-valued case, asserted so the fix for the single-valued one cannot be generalised by accident.
    /// A role holds one <c>FEATURES</c> row per feature, so replacing every row of that type would silently revoke the
    /// features that were not in the request.
    /// </summary>
    [TestMethod]
    public async Task UpdateClaims_Should_NotRevokeTheOtherFeatureClaimsOfTheRole()
    {
        await using var server = await StartServer();
        await using var scope = server.WebApp.Services.CreateAsyncScope();
        var (roleManagementController, _, grant) = await SignInAsGlobalAdmin(server, scope);
        await using var globalAdminGrant = grant;

        var roleId = await CreateRole(roleManagementController);

        await roleManagementController.AddClaims(roleId,
        [
            new() { ClaimType = AppClaimTypes.FEATURES, ClaimValue = AppFeatures.AdminPanel.Dashboard_View },
            new() { ClaimType = AppClaimTypes.FEATURES, ClaimValue = AppFeatures.Todo.Todo_Manage_Self }
        ], TestContext.CancellationToken);

        await roleManagementController.UpdateClaims(roleId,
            [new() { ClaimType = AppClaimTypes.FEATURES, ClaimValue = AppFeatures.AdminPanel.Dashboard_View }], TestContext.CancellationToken);

        var values = await ReadClaimValues(server, roleId, AppClaimTypes.FEATURES);

        CollectionAssert.AreEquivalent(
            new[] { AppFeatures.AdminPanel.Dashboard_View, AppFeatures.Todo.Todo_Manage_Self },
            values,
            "FEATURES is multi-valued, so updating one of a role's features must leave the others alone.");
    }


    private async Task<AppTestServer> StartServer()
    {
        var server = new AppTestServer();
        await server.Build(services => services.AddIntegrationApiOnlyTestsServices()).Start(TestContext.CancellationToken);
        return server;
    }

    /// <summary>
    /// A per-run account, made a global admin and elevated. The returned <see cref="TestAccountUtils.GlobalAdminGrant"/>
    /// must be disposed by the caller so the developer's database is not left with an extra global administrator.
    /// </summary>
    private async Task<(IRoleManagementController Controller, Guid UserId, TestAccountUtils.GlobalAdminGrant Grant)> SignInAsGlobalAdmin(
        AppTestServer server, AsyncServiceScope scope)
    {
        var (email, userId) = await TestAccountUtils.CreateAndSignIn(server, scope, TestContext.CancellationToken);
        var grant = await TestAccountUtils.MakeGlobalAdmin(server, scope, userId, TestContext.CancellationToken);
        await TestAccountUtils.Elevate(server, scope, email, TestContext.CancellationToken);

        return (scope.ServiceProvider.GetRequiredService<IRoleManagementController>(), userId, grant);
    }

    private async Task<Guid> CreateRole(IRoleManagementController roleManagementController)
    {
        var role = await roleManagementController.Create(
            new() { Id = Guid.CreateVersion7(), Name = $"ops-{Guid.NewGuid():N}" }, TestContext.CancellationToken);

        return role.Id;
    }

    private async Task<Guid> ReadRoleId(AppTestServer server, string roleName)
    {
        await using var scope = server.WebApp.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        return await dbContext.Roles.Where(r => r.Name == roleName).Select(r => r.Id).FirstAsync(TestContext.CancellationToken);
    }

    private async Task<int> CountRoleAssignments(AppTestServer server, Guid roleId)
    {
        await using var scope = server.WebApp.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        return await dbContext.UserRoles.CountAsync(ur => ur.RoleId == roleId, TestContext.CancellationToken);
    }

    private async Task<string[]> ReadClaimValues(AppTestServer server, Guid roleId, string claimType)
    {
        await using var scope = server.WebApp.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        return await dbContext.RoleClaims
            .Where(rc => rc.RoleId == roleId && rc.ClaimType == claimType)
            .Select(rc => rc.ClaimValue!)
            .ToArrayAsync(TestContext.CancellationToken);
    }

    public TestContext TestContext { get; set; } = default!;
}

