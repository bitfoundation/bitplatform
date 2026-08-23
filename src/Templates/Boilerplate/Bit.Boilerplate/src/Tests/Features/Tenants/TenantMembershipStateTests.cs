//+:cnd:noEmit
using Boilerplate.Shared.Features.Tenants;

namespace Boilerplate.Tests.Features.Tenants;

/// <summary>
/// <c>UserController.GetTenants</c> answers three different questions with one nullable bool, and the client draws a
/// destructive affordance from it.
/// <list type="bullet">
/// <item><c>null</c> - the caller has no membership row at all. Only a global admin ever sees this, because she is
/// listed every <b>active</b> tenant in the deployment whether or not she belongs to it.</item>
/// <item><c>false</c> - invited, not accepted yet. Leaving such a membership <b>deletes</b> it (that is how an
/// invitation is declined) and only a fresh invite brings it back.</item>
/// <item><c>true</c> - accepted. Leaving resets <c>AcceptedOn</c> and the user can re-accept herself.</item>
/// </list>
/// <para>
/// The endpoint used to collapse the first two into <c>false</c> (<c>AcceptedOn is not null</c>), which is why
/// ManageMyTenantsPage put a "Leave tenant" button on tenants the global admin had never joined - an action whose only
/// possible outcome is a 404, after it has already spent an elevated-access one-time code to get there. The client gate
/// and the decline-vs-leave wording are both derived from this one value, so the tri-state is the load-bearing part.
/// </para>
/// <para>
/// Asserted through the shipped endpoint rather than the projection, because the collapse happened in the mapping step
/// AFTER the query - a test that read the query would have passed against the broken build.
/// </para>
/// </summary>
[TestClass, TestCategory("IntegrationTest")]
public class TenantMembershipStateTests
{
    public TestContext TestContext { get; set; } = default!;

    /// <summary>
    /// All three states in one run, against one caller. Separate tests would each need their own global admin and their
    /// own tenant, and the distinction under test is precisely that the three values differ from each other - which is
    /// only observable when they are read side by side.
    /// </summary>
    [TestMethod]
    public async Task GetTenants_Should_TellNoMembershipApartFromAnUnacceptedInvitation()
    {
        await using var server = new AppTestServer();

        await server.Build(services => services.AddIntegrationApiOnlyTestsServices()).Start(TestContext.CancellationToken);

        await using var strangerScope = server.WebApp.Services.CreateAsyncScope();
        await using var adminScope = server.WebApp.Services.CreateAsyncScope();

        // A tenant owned by somebody else entirely. Its creator gets an accepted membership; the global admin below
        // gets none, which is the state that used to be indistinguishable from a pending invitation.
        var (strangerEmail, _) = await TestAccountUtils.CreateAndSignIn(server, strangerScope, TestContext.CancellationToken);
        await TestAccountUtils.Elevate(server, strangerScope, strangerEmail, TestContext.CancellationToken);

        var foreignTenant = await strangerScope.ServiceProvider.GetRequiredService<ITenantController>()
            .Create(new() { Name = $"t{Guid.NewGuid():N}" }, TestContext.CancellationToken);

        // The caller: a global admin, so GetTenants returns every active tenant to her, including foreignTenant.
        var (adminEmail, adminUserId) = await TestAccountUtils.CreateAndSignIn(server, adminScope, TestContext.CancellationToken);
        await using var globalAdminGrant = await TestAccountUtils.MakeGlobalAdmin(server, adminScope, adminUserId, TestContext.CancellationToken);
        await TestAccountUtils.Elevate(server, adminScope, adminEmail, TestContext.CancellationToken);

        // Her own tenant, accepted, because creating one makes the creator its t-admin.
        var ownTenant = await adminScope.ServiceProvider.GetRequiredService<ITenantController>()
            .Create(new() { Name = $"t{Guid.NewGuid():N}" }, TestContext.CancellationToken);

        // And a genuine pending invitation from a third tenant, which is the value the null must not look like.
        var invitedTenant = await CreateTenantAndInvite(server, adminEmail, TestContext.CancellationToken);

        var tenants = await adminScope.ServiceProvider.GetRequiredService<IUserController>()
            .GetTenants(TestContext.CancellationToken);

        var foreign = tenants.SingleOrDefault(t => t.Id == foreignTenant.Id);
        var own = tenants.SingleOrDefault(t => t.Id == ownTenant.Id);
        var invited = tenants.SingleOrDefault(t => t.Id == invitedTenant);

        // Non-vacuity first: if a global admin stopped being listed every active tenant, the null assertion below would
        // pass by absence rather than by state and this test would silently stop covering anything.
        Assert.IsNotNull(foreign,
            "A global admin must be listed every active tenant, including ones she never joined - that listing is exactly what makes the no-membership state reachable.");
        Assert.IsNotNull(own, "Her own tenant must be in her list.");
        Assert.IsNotNull(invited, "The tenant she was invited to must be in her list.");

        Assert.IsNull(foreign.CurrentUserHasAcceptedThisTenantInvitation,
            "A tenant the caller has no TenantUser row for must come back as null, not false. Reported as false, ManageMyTenantsPage renders a 'Leave tenant' button on it, and clicking that spends an elevated-access one-time code only to receive ResourceNotFoundException from UserController.LeaveTenant.");

        Assert.IsTrue(own.CurrentUserHasAcceptedThisTenantInvitation,
            "Creating a tenant accepts the creator's membership immediately (See TenantController.Create).");

        Assert.IsFalse(invited.CurrentUserHasAcceptedThisTenantInvitation,
            "An invitation that has not been accepted is false - distinct from the null above. This is the state in which leaving DELETES the membership rather than resetting it, which is why the client has to be able to tell the two apart before it words the confirmation.");
    }

    /// <summary>
    /// A second tenant whose admin invites <paramref name="inviteeEmail"/> and leaves the invitation pending.
    /// </summary>
    private async Task<Guid> CreateTenantAndInvite(AppTestServer server, string inviteeEmail, CancellationToken cancellationToken)
    {
        await using var scope = server.WebApp.Services.CreateAsyncScope();

        var (email, _) = await TestAccountUtils.CreateAndSignIn(server, scope, cancellationToken);
        await TestAccountUtils.Elevate(server, scope, email, cancellationToken);

        var tenantController = scope.ServiceProvider.GetRequiredService<ITenantController>();
        var tenant = await tenantController.Create(new() { Name = $"t{Guid.NewGuid():N}" }, cancellationToken);

        // InviteUser targets the caller's CURRENT tenant, so she has to be signed into the one she just made.
        Assert.IsTrue(await scope.ServiceProvider.GetRequiredService<AuthManager>().SwitchTenant(tenant.Id, cancellationToken),
            "The creator must be able to switch into her own tenant, otherwise the invitation below lands on the wrong one.");

        await tenantController.InviteUser(new() { Email = inviteeEmail }, cancellationToken);

        return tenant.Id;
    }
}
