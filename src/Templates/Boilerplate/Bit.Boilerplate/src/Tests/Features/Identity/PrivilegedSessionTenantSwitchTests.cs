//+:cnd:noEmit
using Boilerplate.Shared.Features.Tenants;
using Boilerplate.Server.Api.Features.Tenants;

namespace Boilerplate.Tests.Features.Identity;

/// <summary>
/// A privileged session must not carry its privilege from one tenant into another.
/// <para>
/// <c>UpdateUserSessionPrivilegeStatus</c> weighs a <b>tenant-scoped limit</b> - the <c>MAX_PRIVILEGED_SESSIONS</c>
/// claim lives on a role, and roles resolve only inside their own tenant (See <c>UserClaimsService.GetClaims</c>,
/// which filters <c>role.TenantId == null || role.TenantId == tenantId</c>) - and the flag it produces is sticky, so
/// that once-privileged sessions do not lose their slot on every refresh. Those two together are why
/// <c>IdentityController.Refresh</c> clears <c>userSession.Privileged</c> when the session changes tenant: without
/// that, a privilege earned under one tenant's claims would be kept under every other tenant's, and since creating a
/// tenant is self-service and makes the creator its t-admin with an unlimited claim, any account could lift its own
/// session cap once per device.
/// </para>
/// </summary>
[TestClass, TestCategory("IntegrationTest")]
public partial class PrivilegedSessionTenantSwitchTests
{
    public TestContext TestContext { get; set; } = default!;

    /// <summary>
    /// The regression guard for that clearing: fill the cap, get refused, mint your own tenant, and come back.
    /// <para>
    /// <c>IdentityController.Refresh</c> sets <c>userSession.Privileged = false</c> whenever the session actually
    /// changes tenant, so the flag is re-earned below against the claims of the tenant being entered. This test drives
    /// the round trip that the clearing exists for - into a self-created tenant carrying an unlimited claim and back
    /// into one that caps her at three - and requires the privileged-session claim to come back <c>false</c>. Remove
    /// that one line and the final assertion fails; verified.
    /// </para>
    /// </summary>
    [TestMethod]
    public async Task CreatingAThrowawayTenant_Should_NotCarryPrivilegeBackIntoACappedTenant()
    {
        await using var server = new AppTestServer();

        await server.Build(services => services.AddIntegrationApiOnlyTestsServices()).Start(TestContext.CancellationToken);

        // The shipped default. The fourth sign-in is the first one that cannot be privileged.
        const int maxPrivilegedSessions = 3;

        await using var firstScope = server.WebApp.Services.CreateAsyncScope();

        var (email, userId) = await TestAccountUtils.CreateAndSignIn(server, firstScope, TestContext.CancellationToken);

        // A freshly auto-provisioned account belongs to no tenant, and the bypass is only meaningful against a tenant
        // that actually caps her - so give her an accepted membership of the seeded one, the way
        // UserGroupFeatureManagementUITests arranges its member.
        await JoinSeededTenant(server, userId, TestContext.CancellationToken);

        // ...and move THIS session into it explicitly. The membership was written after this session was created, and
        // Refresh never re-derives a tenant for a session that has none (it only follows an explicit RequestedTenantId
        // - See IdentityController.Refresh). Without this the session sits outside the tenant and the fill below is one
        // short, which is a fault in the arrangement, not in the code under test.
        await SwitchTenant(firstScope, TenantConfiguration.FallbackTenantId, TestContext.CancellationToken);

        // Fill the remaining privileged slots from their own scopes: IStorageService is per scope, so each is a
        // separate device with its own session.
        List<AsyncServiceScope> deviceScopes = [];

        try
        {
            for (var device = 1; device < maxPrivilegedSessions; device++)
            {
                var scope = server.WebApp.Services.CreateAsyncScope();
                deviceScopes.Add(scope);
                await SignInAgain(server, scope, email, TestContext.CancellationToken);
            }

            // The fourth device. Every privileged slot is taken, so this one must not be privileged.
            await using var cappedScope = server.WebApp.Services.CreateAsyncScope();
            var cappedToken = await SignInAgain(server, cappedScope, email, TestContext.CancellationToken);

            Assert.AreEqual("false", ReadPrivilegedClaim(cappedToken),
                $"With {maxPrivilegedSessions} privileged sessions already in place, the next sign-in must not be privileged - otherwise the rest of this test proves nothing.");

            // She mints a tenant of her own. Create is gated on elevated access only, which is a code to her own
            // address, and it makes her its t-admin with MAX_PRIVILEGED_SESSIONS = UNLIMITED.
            await TestAccountUtils.Elevate(server, cappedScope, email, TestContext.CancellationToken);

            var ownTenant = await cappedScope.ServiceProvider.GetRequiredService<ITenantController>()
                .Create(new() { Name = $"t{Guid.NewGuid():N}" }, TestContext.CancellationToken);

            // Inside her own tenant she is legitimately unlimited, so this one IS expected to be privileged.
            var insideOwnTenant = await SwitchTenant(cappedScope, ownTenant.Id, TestContext.CancellationToken);

            Assert.AreEqual("true", ReadPrivilegedClaim(insideOwnTenant),
                "A t-admin of her own tenant carries MAX_PRIVILEGED_SESSIONS = UNLIMITED there, so this step is supposed to succeed. It is the round trip that follows which must not stick.");

            // ... and back into the tenant that caps her at three, where three privileged sessions already exist.
            var backInCappedTenant = await SwitchTenant(cappedScope, TenantConfiguration.FallbackTenantId, TestContext.CancellationToken);


            Assert.AreEqual("false", ReadPrivilegedClaim(backInCappedTenant),
                "Privilege earned under her own tenant's unlimited claim followed her back into a tenant that grants her nothing. The limit is tenant-scoped (the claim only resolves inside the role's tenant) but the count and the Privileged flag are global, so any registered account can lift its own privileged-session cap with two API calls, repeatable per device.");
        }
        finally
        {
            foreach (var scope in deviceScopes)
            {
                await scope.DisposeAsync();
            }
        }
    }

    /// <summary>
    /// Switches the scope''s session into a tenant by calling <c>Refresh</c> directly, rather than through
    /// <c>AuthManager.SwitchTenant</c>.
    /// <para>
    /// Deliberate: <c>AuthManager.RefreshToken</c> returns the in-flight <c>accessTokenTsc.Task</c> when one exists, and
    /// its <c>TaskCompletionSource</c> is built without <c>RunContinuationsAsynchronously</c> - so <c>SetResult</c>
    /// resumes the awaiting caller BEFORE the <c>finally</c> that nulls the field. Two back-to-back refreshes therefore
    /// hand the second caller the first call''s result and drop its <c>requestedTenantId</c> silently. That cost this
    /// test an hour: the session never left the tenant, and the assertion "it came back privileged" was reading a token
    /// from a switch that never happened. Going straight to the endpoint keeps the client''s refresh coordinator out of
    /// a server-side assertion, and the tenant claim is asserted below so a no-op can never be mistaken for a result.
    /// </para>
    /// </summary>
    private static async Task<string> SwitchTenant(AsyncServiceScope scope, Guid tenantId, CancellationToken cancellationToken)
    {
        var storageService = scope.ServiceProvider.GetRequiredService<IStorageService>();

        var refreshToken = await storageService.GetItem("refresh_token");
        Assert.IsFalse(string.IsNullOrEmpty(refreshToken), "The scope has to be signed in before it can switch tenant.");

        var tokens = await scope.ServiceProvider.GetRequiredService<IIdentityController>()
            .Refresh(new() { RefreshToken = refreshToken, RequestedTenantId = tenantId }, cancellationToken);

        await scope.ServiceProvider.GetRequiredService<AuthManager>().StoreTokens(tokens);

        var principal = IAuthTokenProvider.ParseAccessToken(tokens.AccessToken!, validateExpiry: false);

        Assert.AreEqual(tenantId.ToString(), principal.FindFirst(AppClaimTypes.TENANT_ID)?.Value,
            "The switch has to actually land in the requested tenant, otherwise every assertion after it is reading the wrong session state.");

        return tokens.AccessToken!;
    }
    /// <summary>Signs an already confirmed account in again through the OTP flow, creating a brand-new UserSession.</summary>
    private async Task<string> SignInAgain(AppTestServer server, AsyncServiceScope scope, string email, CancellationToken cancellationToken)
    {
        var identityController = scope.ServiceProvider.GetRequiredService<IIdentityController>();

        await identityController.SendOtp(new() { Email = email }, null, cancellationToken);

        var captured = await server.WaitForCapturedEmail(email,
            capturedEmail => capturedEmail.Kind is CapturedEmailKind.Otp, cancellationToken);

        var tokens = await identityController.SignIn(new() { Email = email, Otp = captured.Token }, cancellationToken);

        await scope.ServiceProvider.GetRequiredService<AuthManager>().StoreTokens(tokens);

        return tokens.AccessToken!;
    }

    private static async Task JoinSeededTenant(AppTestServer server, Guid userId, CancellationToken cancellationToken)
    {
        await using var scope = server.WebApp.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await dbContext.TenantUsers.AddAsync(new()
        {
            TenantId = TenantConfiguration.FallbackTenantId,
            UserId = userId,
            AcceptedOn = DateTimeOffset.UtcNow
        }, cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static string? ReadPrivilegedClaim(string? accessToken)
    {
        Assert.IsFalse(string.IsNullOrEmpty(accessToken), "Every step here has to produce an access token to read the claim from.");

        return IAuthTokenProvider.ParseAccessToken(accessToken!, validateExpiry: false)
                                .FindFirst(AppClaimTypes.PRIVILEGED_SESSION)?.Value;
    }
}
