using Boilerplate.Shared.Features.Identity.Dtos;

namespace Boilerplate.Tests.Features.Identity;

/// <summary>
/// The max-privileged-sessions quota is the app's only single-valued role claim, and the roles page treats it as a
/// scalar it can set, change and clear. Two things have to hold for that to be true, and neither is visible from the
/// endpoint's own signature:
/// <list type="number">
/// <item><b>Update replaces.</b> <c>UpdateClaims</c> deletes the existing rows of the type and inserts the new one
/// inside a transaction, so a role must end up with exactly ONE <c>mx-p-s</c> row carrying the new value. Two rows, or
/// zero, both read as a plausible success: the endpoint returns 200 either way and the page optimistically writes the
/// value it sent into its own cache, so the screen keeps showing a number the database does not hold.</item>
/// <item><b>The quota can be taken back off.</b> Removing the claim is how a role falls back to
/// <c>AppSettings.Identity.MaxPrivilegedSessionsCount</c>. The page could set the quota but never clear it - an empty
/// field returned before sending anything - so this capability was reachable only from the database.</item>
/// </list>
/// <para>
/// Note what this file deliberately does NOT cover. The insert in <c>UpdateClaims</c> is built inside its retrying
/// execution strategy's delegate so that a retry re-creates it; constructing it outside means the entities from the
/// failed attempt are already <c>Unchanged</c> after <c>SaveChangesAsync</c> accepted them, the retry's
/// <c>ExecuteDeleteAsync</c> removes the old row again, and the role is left with no quota at all - still 200. That
/// path needs <c>EnableRetryOnFailure</c>, which only SqlServer / PostgreSQL / MySql configure; the test database is
/// Sqlite, whose execution strategy never re-runs the delegate. So the retry itself cannot be exercised here. What the
/// single-row assertion below does catch is the likelier regression: a "cleanup" that hoists the entity construction
/// back out of the delegate while leaving the <c>Add</c> inside.
/// </para>
/// </summary>
[TestClass, TestCategory("IntegrationTest")]
public class RoleQuotaClaimLifecycleTests
{
    public TestContext TestContext { get; set; } = default!;

    [TestMethod]
    public async Task UpdateClaims_Should_ReplaceTheQuota_LeavingExactlyOneRow()
    {
        await using var server = await StartServer();
        await using var scope = server.WebApp.Services.CreateAsyncScope();
        var (roleManagementController, grant) = await SignInAsGlobalAdmin(server, scope);
        await using var globalAdminGrant = grant;

        var roleId = await CreateRole(roleManagementController);

        await roleManagementController.AddClaims(roleId, [Quota("3")], TestContext.CancellationToken);

        await roleManagementController.UpdateClaims(roleId, [Quota("5")], TestContext.CancellationToken);

        var values = await ReadQuotaValues(server, roleId);

        Assert.AreEqual(1, values.Count,
            $"A single-valued claim must not accumulate rows - every reader (and the roles page) treats mx-p-s as one number, so a second row makes the effective quota depend on row order. Found: [{string.Join(", ", values)}].");

        Assert.AreEqual("5", values[0],
            "The surviving row must carry the NEW value. Zero rows would mean the insert was lost while the delete landed, which the endpoint still reports as 200.");
    }

    /// <summary>
    /// Clearing the number field on the roles page now means "this role has no quota of its own". That translates to
    /// deleting the claim, so the endpoint has to accept this claim type for deletion - <c>DeleteClaims</c> keeps its
    /// own allow-list, and mx-p-s being absent from it would make the page's clear path a silent 401.
    /// </summary>
    [TestMethod]
    public async Task DeleteClaims_Should_RemoveTheQuota_SoARoleCanFallBackToTheAppDefault()
    {
        await using var server = await StartServer();
        await using var scope = server.WebApp.Services.CreateAsyncScope();
        var (roleManagementController, grant) = await SignInAsGlobalAdmin(server, scope);
        await using var globalAdminGrant = grant;

        var roleId = await CreateRole(roleManagementController);

        await roleManagementController.AddClaims(roleId, [Quota("3")], TestContext.CancellationToken);

        Assert.AreEqual(1, (await ReadQuotaValues(server, roleId)).Count,
            "Arrange check: the quota has to be there before removing it proves anything.");

        await roleManagementController.DeleteClaims(roleId, [Quota("3")], TestContext.CancellationToken);

        Assert.AreEqual(0, (await ReadQuotaValues(server, roleId)).Count,
            "With the claim gone the role falls back to AppSettings.Identity.MaxPrivilegedSessionsCount. Without this path a quota can be set on a role and never taken off it again from the UI.");
    }


    private static ClaimDto Quota(string value) => new() { ClaimType = AppClaimTypes.MAX_PRIVILEGED_SESSIONS, ClaimValue = value };

    private async Task<AppTestServer> StartServer()
    {
        var server = new AppTestServer();
        await server.Build(services => services.AddIntegrationApiOnlyTestsServices()).Start(TestContext.CancellationToken);
        return server;
    }

    /// <summary>
    /// A global admin, elevated: every endpoint touched here carries <c>ELEVATED_ACCESS</c>, and granting a quota is
    /// additionally capped by the caller's own (<c>EnsureCallerCanGrantClaims</c>), so a lesser caller would fail for
    /// reasons that have nothing to do with what is under test.
    /// </summary>
    private async Task<(IRoleManagementController Controller, TestAccountUtils.GlobalAdminGrant Grant)> SignInAsGlobalAdmin(
        AppTestServer server, AsyncServiceScope scope)
    {
        var (email, userId) = await TestAccountUtils.CreateAndSignIn(server, scope, TestContext.CancellationToken);
        var grant = await TestAccountUtils.MakeGlobalAdmin(server, scope, userId, TestContext.CancellationToken);
        await TestAccountUtils.Elevate(server, scope, email, TestContext.CancellationToken);

        return (scope.ServiceProvider.GetRequiredService<IRoleManagementController>(), grant);
    }

    private async Task<Guid> CreateRole(IRoleManagementController roleManagementController)
    {
        var role = await roleManagementController.Create(
            new() { Id = Guid.CreateVersion7(), Name = $"ops-{Guid.NewGuid():N}" }, TestContext.CancellationToken);

        return role.Id;
    }

    private async Task<List<string>> ReadQuotaValues(AppTestServer server, Guid roleId)
    {
        await using var scope = server.WebApp.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        return await dbContext.RoleClaims
            .Where(rc => rc.RoleId == roleId && rc.ClaimType == AppClaimTypes.MAX_PRIVILEGED_SESSIONS)
            .Select(rc => rc.ClaimValue!)
            .ToListAsync(TestContext.CancellationToken);
    }
}
