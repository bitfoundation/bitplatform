using Microsoft.EntityFrameworkCore;
using Boilerplate.Shared.Features.Identity;
using Boilerplate.Server.Api.Infrastructure.Data;

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
        await using var scope = server.WebApp.Services.CreateAsyncScope();
        var (roleManagementController, grant) = await SignInAsGlobalAdmin(server, scope);
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
        await using var scope = server.WebApp.Services.CreateAsyncScope();
        var (roleManagementController, grant) = await SignInAsGlobalAdmin(server, scope);
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
        await using var scope = server.WebApp.Services.CreateAsyncScope();
        var (roleManagementController, grant) = await SignInAsGlobalAdmin(server, scope);
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


    private async Task<AppTestServer> StartServer()
    {
        var server = new AppTestServer();
        await server.Build(services => services.AddIntegrationApiOnlyTestsServices()).Start(TestContext.CancellationToken);
        return server;
    }

    /// <summary>
    /// A per-run account promoted to global admin and elevated - the most privileged caller the app has, so a refusal
    /// here is a refusal for everyone. The returned grant must be disposed, or the developer's database keeps the
    /// extra administrator.
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

    private async Task<int> CountClaims(AppTestServer server, Guid roleId)
    {
        await using var scope = server.WebApp.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        return await dbContext.RoleClaims.CountAsync(rc => rc.RoleId == roleId, TestContext.CancellationToken);
    }

    public TestContext TestContext { get; set; } = default!;
}
