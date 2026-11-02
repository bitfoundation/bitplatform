using Boilerplate.Shared.Features.Identity.Dtos;
//#if (multitenant == true)
using Boilerplate.Server.Api.Features.Tenants;
//#endif

namespace Boilerplate.Tests.Features.Identity;

/// <summary>
/// Role claims are copied verbatim into the access token (<c>AppUserClaimsPrincipalFactory.GenerateClaims</c>), and
/// <c>AppJwtSecureDataFormat.Unprotect</c> then reads that token's <b>role</b> claims and hands out every global-admin
/// feature to anyone in <c>g-admin</c>. So whatever claim type the role-management endpoints accept is, transitively,
/// something a caller can put in their own token.
/// <para>
/// That is why <c>EnsureCallerCanGrantClaims</c> is an allow-list of the two claim types the roles page actually sets,
/// not a block-list of the dangerous ones. A block-list of "features" alone left <c>ClaimTypes.Role = "g-admin"</c>
/// wide open, which turned the ordinary <c>Roles_Manage</c> permission into silent global administrator: grant the
/// role claim, refresh, and the token reader promotes you.
/// </para>
/// <para>
/// Nothing about this is visible from the endpoint's own code - the request succeeds, the row is written, and the
/// escalation only materialises two files away at token-read time. It is also exactly the shape a well-meaning
/// "let administrators set arbitrary claims" change would reintroduce.
/// </para>
/// </summary>
[TestClass, TestCategory("IntegrationTest")]
public class RoleClaimGrantAllowListTests
{
    /// <summary>
    /// The escalation itself, plus the three other claim types the app computes server-side per session. None of them
    /// may be settable on a role: <c>s-id</c> / <c>p-s</c> / <c>e-s</c> are session facts, and a role-supplied copy is
    /// merged into the principal alongside the real one.
    /// </summary>
    [TestMethod]
    [DataRow(ClaimTypes.Role, AppRoles.GlobalAdmin, DisplayName = "role -> g-admin (full privilege escalation)")]
    [DataRow(ClaimTypes.Role, "t-admin", DisplayName = "role -> t-admin")]
    [DataRow(AppClaimTypes.PRIVILEGED_SESSION, "true", DisplayName = "p-s -> true")]
    [DataRow(AppClaimTypes.ELEVATED_SESSION, "99999999999", DisplayName = "e-s -> far future")]
    [DataRow(AppClaimTypes.SESSION_ID, "8ff71671-a1d6-4f97-abb9-d87d7b47d6e7", DisplayName = "s-id -> another session")]
    [DataRow("anything-else", "whatever", DisplayName = "an unknown claim type")]
    public async Task AddClaims_Should_RefuseEveryClaimTypeOutsideTheAllowList(string claimType, string claimValue)
    {
        await using var server = await StartServer();
        await using var client = server.CreateAppClient();
        var (roleManagementController, grant) = await SignInAsGlobalAdmin(client);
        await using var globalAdminGrant = grant;

        var roleId = await CreateRole(roleManagementController);

        await Assert.ThrowsExactlyAsync<UnauthorizedException>(
            () => roleManagementController.AddClaims(roleId, [new() { ClaimType = claimType, ClaimValue = claimValue }], TestContext.CancellationToken),
            $"'{claimType}' is not one of the two claim types the roles page sets, so it must be refused even for a global admin.");

        Assert.AreEqual(0, await CountClaims(server, roleId),
            "A refused grant must write nothing - the check has to run before the rows are added.");
    }

    /// <summary>
    /// <c>UpdateClaims</c> is a separate endpoint with its own body, and it writes through <c>DbContext</c> directly
    /// rather than through <c>RoleManager</c>. It needs its own copy of the guard, and has to be asserted separately
    /// or a refactor that keeps only <c>AddClaims</c> guarded reads as "still covered".
    /// </summary>
    [TestMethod]
    public async Task UpdateClaims_Should_RefuseEveryClaimTypeOutsideTheAllowList()
    {
        await using var server = await StartServer();
        await using var client = server.CreateAppClient();
        var (roleManagementController, grant) = await SignInAsGlobalAdmin(client);
        await using var globalAdminGrant = grant;

        var roleId = await CreateRole(roleManagementController);

        await Assert.ThrowsExactlyAsync<UnauthorizedException>(
            () => roleManagementController.UpdateClaims(roleId,
                [new() { ClaimType = ClaimTypes.Role, ClaimValue = AppRoles.GlobalAdmin }], TestContext.CancellationToken),
            "UpdateClaims must apply the same allow-list as AddClaims; it is a second way into the same table.");

        Assert.AreEqual(0, await CountClaims(server, roleId), "A refused update must write nothing.");
    }

    /// <summary>
    /// One request, one legitimate claim and one forbidden claim. The whole request has to be refused - a guard that
    /// filters instead of rejecting, or that only inspects the first entry, would let the escalation ride along with a
    /// harmless feature grant.
    /// </summary>
    [TestMethod]
    public async Task AddClaims_Should_RefuseTheWholeRequest_WhenOneClaimIsForbidden()
    {
        await using var server = await StartServer();
        await using var client = server.CreateAppClient();
        var (roleManagementController, grant) = await SignInAsGlobalAdmin(client);
        await using var globalAdminGrant = grant;

        var roleId = await CreateRole(roleManagementController);

        await Assert.ThrowsExactlyAsync<UnauthorizedException>(
            () => roleManagementController.AddClaims(roleId,
            [
                new() { ClaimType = AppClaimTypes.FEATURES, ClaimValue = AppFeatures.AdminPanel.Dashboard_View },
                new() { ClaimType = ClaimTypes.Role, ClaimValue = AppRoles.GlobalAdmin }
            ], TestContext.CancellationToken));

        Assert.AreEqual(0, await CountClaims(server, roleId),
            "Neither claim may be written: the legitimate one must not act as a carrier for the forbidden one.");
    }

    /// <summary>
    /// Joining a role hands over everything the role holds, so <c>ToggleUserRole</c> asks what <c>AddClaims</c> asks.
    /// Otherwise a delegated role manager (a custom role with <c>Roles_Manage</c>) joins a richer role and gets the
    /// features they could never grant one by one.
    /// </summary>
    [TestMethod]
    public async Task ToggleUserRole_Should_RefuseARole_WhoseFeaturesTheCallerLacks()
    {
        await using var server = await StartServer();
        await using var adminClient = server.CreateAppClient();
        await using var managerClient = server.CreateAppClient();
        var (roleManagementController, grant) = await SignInAsGlobalAdmin(adminClient);
        await using var globalAdminGrant = grant;

        var managersRoleId = await CreateRole(roleManagementController, AppFeatures.Management.Roles_Manage);
        var leanerRoleId = await CreateRole(roleManagementController, AppFeatures.Management.Roles_Manage);
        var richerRoleId = await CreateRole(roleManagementController, AppFeatures.Management.Users_Manage);

        var (managerEmail, managerId) = await TestAccountUtils.CreateAndSignIn(managerClient, TestContext.CancellationToken);

        //#if (multitenant == true)
        await AcceptIntoFallbackTenant(server, managerId);
        //#endif

        await roleManagementController.ToggleUserRole(new() { UserId = managerId, RoleId = managersRoleId }, TestContext.CancellationToken);

        //#if (multitenant == true)
        Assert.IsTrue(await managerClient.AuthManager.SwitchTenant(TenantConfiguration.FallbackTenantId, TestContext.CancellationToken),
            "The manager is an accepted member of the seeded tenant, so switching into it must work.");
        //#endif

        await TestAccountUtils.Elevate(managerClient, managerEmail, TestContext.CancellationToken);

        var managerRoleManagementController = managerClient.GetController<IRoleManagementController>();

        await managerRoleManagementController.ToggleUserRole(new() { UserId = managerId, RoleId = leanerRoleId }, TestContext.CancellationToken);

        await Assert.ThrowsExactlyAsync<UnauthorizedException>(
            () => managerRoleManagementController.ToggleUserRole(new() { UserId = managerId, RoleId = richerRoleId }, TestContext.CancellationToken),
            "The manager lacks Users_Manage, so joining a role that holds it must be refused.");

        //#if (multitenant == true)
        var tenantAdminRoleId = await ReadFallbackTenantAdminRoleId(server);

        await Assert.ThrowsExactlyAsync<UnauthorizedException>(
            () => managerRoleManagementController.ToggleUserRole(new() { UserId = managerId, RoleId = tenantAdminRoleId }, TestContext.CancellationToken),
            "t-admin holds no feature rows; the features its name implies must count too.");

        Assert.IsFalse(await IsInRole(server, managerId, tenantAdminRoleId), "A refused join must write nothing.");
        //#endif

        Assert.IsTrue(await IsInRole(server, managerId, leanerRoleId),
            "The control: a role that holds only what the manager holds can be joined, so the refusals above are the feature check.");
        Assert.IsFalse(await IsInRole(server, managerId, richerRoleId), "A refused join must write nothing.");
    }


    private async Task<AppTestServer> StartServer()
    {
        var server = new AppTestServer();
        await server.Build().Start(TestContext.CancellationToken);
        return server;
    }

    /// <summary>
    /// A per-run account promoted to global admin and elevated - the most privileged caller the app has, so a refusal
    /// here is a refusal for everyone. The returned grant must be disposed, or the developer's database keeps the
    /// extra administrator.
    /// </summary>
    private async Task<(IRoleManagementController Controller, TestAccountUtils.GlobalAdminGrant Grant)> SignInAsGlobalAdmin(
        AppClient client)
    {
        var (email, userId) = await TestAccountUtils.CreateAndSignIn(client, TestContext.CancellationToken);
        var grant = await TestAccountUtils.MakeGlobalAdmin(client, userId, TestContext.CancellationToken);
        await TestAccountUtils.Elevate(client, email, TestContext.CancellationToken);

        return (client.GetController<IRoleManagementController>(), grant);
    }

    private async Task<Guid> CreateRole(IRoleManagementController roleManagementController, params string[] features)
    {
        var role = await roleManagementController.Create(
            new() { Id = Guid.CreateVersion7(), Name = $"ops-{Guid.NewGuid():N}" }, TestContext.CancellationToken);

        if (features.Length > 0)
        {
            await roleManagementController.AddClaims(role.Id,
                [.. features.Select(feature => new ClaimDto { ClaimType = AppClaimTypes.FEATURES, ClaimValue = feature })], TestContext.CancellationToken);
        }

        return role.Id;
    }

    private async Task<bool> IsInRole(AppTestServer server, Guid userId, Guid roleId)
    {
        await using var scope = server.ApiApp.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        return await dbContext.UserRoles.AnyAsync(ur => ur.UserId == userId && ur.RoleId == roleId, TestContext.CancellationToken);
    }

    //#if (multitenant == true)
    private async Task AcceptIntoFallbackTenant(AppTestServer server, Guid userId)
    {
        await using var scope = server.ApiApp.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await dbContext.TenantUsers.AddAsync(new() { TenantId = TenantConfiguration.FallbackTenantId, UserId = userId, AcceptedOn = DateTimeOffset.UtcNow }, TestContext.CancellationToken);

        await dbContext.SaveChangesAsync(TestContext.CancellationToken);
    }

    private async Task<Guid> ReadFallbackTenantAdminRoleId(AppTestServer server)
    {
        await using var scope = server.ApiApp.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        return await dbContext.Roles
            .Where(r => r.Name == AppRoles.TenantAdmin && r.TenantId == TenantConfiguration.FallbackTenantId)
            .Select(r => r.Id)
            .SingleAsync(TestContext.CancellationToken);
    }
    //#endif

    private async Task<int> CountClaims(AppTestServer server, Guid roleId)
    {
        await using var scope = server.ApiApp.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        return await dbContext.RoleClaims.CountAsync(rc => rc.RoleId == roleId, TestContext.CancellationToken);
    }

    public TestContext TestContext { get; set; } = default!;
}
