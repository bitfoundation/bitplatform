//+:cnd:noEmit
using Microsoft.EntityFrameworkCore;
using Boilerplate.Shared.Features.Identity;
using Boilerplate.Server.Api.Infrastructure.Data;
using Boilerplate.Tests.Infrastructure.Services;
using Boilerplate.Client.Core.Infrastructure.Services;
using Boilerplate.Client.Core.Infrastructure.Services.Contracts;
using Boilerplate.Server.Api.Features.Identity.Models;

namespace Boilerplate.Tests.Features.Identity;

/// <summary>
/// Creates the account a test needs, per run, through the shipped endpoints only - no direct seeding of users and no
/// reuse of the seeded <c>test@bitplatform.dev</c> account.
/// <para>
/// This matters for more than isolation. Several identity flows are throttled or stamped <b>per user</b>
/// (<c>ElevatedAccessTokenRequestedOn</c>, <c>EmailTokenRequestedOn</c>, the lockout counter), and the test database
/// outlives a single run - so a test that drives those flows on the shared seeded account leaves the throttle armed for
/// every later test and for the next run. A per-run account with a Guid e-mail turns any leftover into an inert orphan.
/// </para>
/// </summary>
public static class TestAccountUtils
{
    /// <summary>
    /// Registers a brand-new account, confirms it with the code from the captured e-mail, and signs it in - the same
    /// three steps the magic-link UI performs, driven through the API so no browser is needed. Returns the account's
    /// e-mail and id.
    /// </summary>
    public static async Task<(string email, Guid userId)> CreateAndSignIn(
        AppTestServer server, AsyncServiceScope scope, CancellationToken cancellationToken)
    {
        var identityController = scope.ServiceProvider.GetRequiredService<IIdentityController>();

        var email = MagicLinkSignInUtils.NewTestEmail();

        // An unknown identifier auto-provisions the account and mails its confirmation code; the endpoint still reports
        // the account is not confirmed yet, which is why this throws.
        await Assert.ThrowsExactlyAsync<BadRequestException>(
            () => identityController.SendOtp(new() { Email = email }, null, cancellationToken));

        var captured = await server.WaitForCapturedEmail(email,
            e => e.Kind is CapturedEmailKind.EmailToken, cancellationToken);

        // Confirming returns the token pair, exactly as ConfirmPage receives it; AuthManager is what persists it.
        var tokens = await identityController.ConfirmEmail(new() { Email = email, Token = captured.Token }, cancellationToken);

        await scope.ServiceProvider.GetRequiredService<AuthManager>().StoreTokens(tokens);

        var userId = await ReadUserId(server, email, cancellationToken);

        return (email, userId);
    }

    /// <summary>
    /// Grants the seeded <c>g-admin</c> role to <paramref name="userId"/> and refreshes the token pair so the new role is
    /// actually in the access token. The role row is written directly because the only endpoint that could grant it
    /// (<c>ToggleUserRole</c>) refuses unless the caller is already a global admin - which is the thing being set up.
    /// <para>
    /// <b>Dispose the returned handle</b> (<c>await using</c>) so the grant is revoked afterwards. The database outlives
    /// the run, so a leftover grant is not an inert orphan - it is a real global administrator sitting in the developer's
    /// database, and they pile up one per test run.
    /// </para>
    /// </summary>
    public static async Task<GlobalAdminGrant> MakeGlobalAdmin(AppTestServer server, AsyncServiceScope scope, Guid userId, CancellationToken cancellationToken)
    {
        await using (var dbScope = server.WebApp.Services.CreateAsyncScope())
        {
            var dbContext = dbScope.ServiceProvider.GetRequiredService<AppDbContext>();

            var globalAdminRoleId = await dbContext.Roles
                .Where(r => r.Name == AppRoles.GlobalAdmin)
                .Select(r => r.Id)
                .SingleAsync(cancellationToken);

            await dbContext.UserRoles.AddAsync(new UserRole { UserId = userId, RoleId = globalAdminRoleId }, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var accessToken = await scope.ServiceProvider.GetRequiredService<AuthManager>()
            .RefreshToken(requestedBy: nameof(MakeGlobalAdmin));

        Assert.IsFalse(string.IsNullOrEmpty(accessToken), "Refreshing after the role grant should have produced a new access token.");

        var principal = IAuthTokenProvider.ParseAccessToken(accessToken, validateExpiry: false);

        Assert.IsTrue(principal.IsInRole(AppRoles.GlobalAdmin),
            $"The refreshed access token must carry the g-admin role. Claims: [{string.Join(", ", principal.Claims.Select(c => $"{c.Type}={c.Value}"))}]");

        //#if (multitenant == true)
        // The management controllers additionally require AuthPolicies.TENANT_SELECTED, and a freshly auto-provisioned
        // account has no tenant selected. A global admin may switch into any active tenant (See UserController.GetTenants).
        var userController = scope.ServiceProvider.GetRequiredService<IUserController>();

        var tenants = await userController.GetTenants(cancellationToken);
        Assert.IsTrue(tenants.Count > 0, "A global admin should see every active tenant, and the template seeds one.");

        Assert.IsTrue(await scope.ServiceProvider.GetRequiredService<AuthManager>().SwitchTenant(tenants[0].Id, cancellationToken),
            "Switching into the seeded tenant should succeed for a global admin.");
        //#endif

        return new GlobalAdminGrant(server, userId);
    }

    /// <summary>Revokes the g-admin grant <see cref="MakeGlobalAdmin"/> made, when the test is done with it.</summary>
    public sealed class GlobalAdminGrant(AppTestServer server, Guid userId) : IAsyncDisposable
    {
        public async ValueTask DisposeAsync()
        {
            await using var scope = server.WebApp.Services.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            await dbContext.UserRoles
                .Where(ur => ur.UserId == userId && ur.Role!.Name == AppRoles.GlobalAdmin)
                .ExecuteDeleteAsync();
        }
    }

    /// <summary>
    /// Drives the real elevation flow - request the code, read it from the captured e-mail, refresh the token pair with
    /// it - so a test that needs <see cref="AuthPolicies.ELEVATED_ACCESS"/> obtains it the way a user would.
    /// </summary>
    public static async Task Elevate(AppTestServer server, AsyncServiceScope scope, string email, CancellationToken cancellationToken)
    {
        var userController = scope.ServiceProvider.GetRequiredService<IUserController>();

        await userController.SendElevatedAccessToken(cancellationToken);

        var captured = await server.WaitForCapturedEmail(email,
            e => e.Kind is CapturedEmailKind.ElevatedAccess, cancellationToken);

        var accessToken = await scope.ServiceProvider.GetRequiredService<AuthManager>()
            .RefreshToken(requestedBy: nameof(Elevate), elevatedAccessToken: captured.Token);

        Assert.IsFalse(string.IsNullOrEmpty(accessToken), "The elevation refresh should have produced a new access token.");
    }

    public static async Task<Guid> ReadUserId(AppTestServer server, string email, CancellationToken cancellationToken)
    {
        await using var scope = server.WebApp.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var normalizedEmail = email.ToUpperInvariant();

        return await dbContext.Set<User>()
            .Where(u => u.NormalizedEmail == normalizedEmail)
            .Select(u => u.Id)
            .SingleAsync(cancellationToken);
    }
}

