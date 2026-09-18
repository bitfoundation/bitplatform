using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.Http.Connections;
using Boilerplate.Shared.Features.Tenants;
using Boilerplate.Shared.Features.Categories;
using Boilerplate.Shared.Infrastructure.Services;

namespace Boilerplate.Tests.Features.SignalR;

/// <summary>
/// <c>DASHBOARD_DATA_CHANGED</c> makes every receiving client re-query the dashboard endpoints, so publishing it
/// to "AuthenticatedClients" woke every tenant. Both directions are asserted: the untouched tenant receives nothing,
/// and the tenant that changed still receives it - otherwise a group nobody joins would also pass.
/// </summary>
/// <remarks>
/// Real <see cref="HubConnection"/>s rather than a recording double: the fix has two halves - joining the tenant
/// group in <c>AppHub</c> and publishing to it - and a double only proves the second.
/// </remarks>
[TestClass, TestCategory("IntegrationTest")]
public partial class DashboardBroadcastTenantScopeTests
{
    // Seeded tenant-admin of the default (fallback) "store" tenant; holds ProductCatalog_Manage. See UserConfiguration.
    private const string StoreAdminEmail = "store-admin@bitplatform.dev";
    private const string Password = "123456";

    public TestContext TestContext { get; set; } = default!;

    [TestMethod]
    public async Task ACatalogChange_Should_OnlyWakeTheChangedTenantsClients()
    {
        await using var server = new AppTestServer();

        await server.Build(services => services.AddIntegrationApiOnlyTestsServices()).Start(TestContext.CancellationToken);

        // IStorageService is registered per scope, so each scope holds its own signed-in identity and every typed API
        // client resolved from it calls the server as that user (See TenantInvitationIsolationTests).
        await using var storeScope = server.WebApp.Services.CreateAsyncScope();
        await using var otherScope = server.WebApp.Services.CreateAsyncScope();

        // ---- Tenant A: the seeded "store" tenant and its seeded t-admin ----
        var requiresTwoFactor = await storeScope.ServiceProvider.GetRequiredService<AuthManager>()
            .SignIn(new() { Email = StoreAdminEmail, Password = Password }, TestContext.CancellationToken);

        Assert.IsFalse(requiresTwoFactor, $"'{StoreAdminEmail}' is not expected to have two factor authentication enabled.");

        // ---- Tenant B: a per-run tenant, whose creator becomes its t-admin with an accepted membership ----
        var (otherEmail, _) = await TestAccountUtils.CreateAndSignIn(server, otherScope, TestContext.CancellationToken);

        await TestAccountUtils.Elevate(server, otherScope, otherEmail, TestContext.CancellationToken);

        var otherTenant = await otherScope.ServiceProvider.GetRequiredService<ITenantController>()
            .Create(new() { Name = $"t{Guid.NewGuid():N}" }, TestContext.CancellationToken);

        Assert.IsTrue(await otherScope.ServiceProvider.GetRequiredService<AuthManager>().SwitchTenant(otherTenant.Id, TestContext.CancellationToken),
            "The creator must be able to switch into the tenant she just created, otherwise her token carries no tenant and nothing below is tenant scoped.");

        // Connected only now, after both identities are final: the group a connection joins comes from the tenant claim
        // of the token it handshakes with.
        await using var storeClient = await ConnectAsSignedInClient(server, storeScope);
        await using var otherClient = await ConnectAsSignedInClient(server, otherScope);

        var storeCategories = storeScope.ServiceProvider.GetRequiredService<ICategoryController>();
        var otherCategories = otherScope.ServiceProvider.GetRequiredService<ICategoryController>();

        CategoryDto? createdInOther = null, createdInStore = null;

        try
        {
            // ---- Tenant B changes its catalog ----
            createdInOther = await otherCategories.Create(NewCategory(), TestContext.CancellationToken);

            await WaitUntil(() => otherClient.DashboardMessages >= 1,
                "The tenant whose own catalog changed never received DASHBOARD_DATA_CHANGED, so its open dashboards keep showing stale numbers until they are reloaded by hand.");

            // The two clients are served by one Publish call on one server, so a message for the store client is not
            // still in flight once the other client has its own; this grace period only covers a slower socket.
            await Task.Delay(TimeSpan.FromSeconds(1), TestContext.CancellationToken);

            Assert.AreEqual(0, storeClient.DashboardMessages,
                "A category created inside another tenant reached the store tenant's signed-in client. DASHBOARD_DATA_CHANGED makes every receiver re-query the three dashboard endpoints, so this is a cross-tenant wake-up for data the receiver cannot even see.");

            Assert.AreEqual(1, otherClient.DashboardMessages, "One create must publish exactly one message.");

            // ---- And the reverse direction, which is also the non-vacuity check ----
            createdInStore = await storeCategories.Create(NewCategory(), TestContext.CancellationToken);

            await WaitUntil(() => storeClient.DashboardMessages >= 1,
                "The store tenant's client received nothing for a change made inside its own tenant - the realtime dashboard is simply broken, which would make the assertion above pass for the wrong reason.");

            await Task.Delay(TimeSpan.FromSeconds(1), TestContext.CancellationToken);

            Assert.AreEqual(1, otherClient.DashboardMessages,
                "The other tenant's client must be left alone by the store tenant's change as well; a group that only happens to be right in one direction is not scoping anything.");
        }
        finally
        {
            // CancellationToken.None: the cleanup has to run even when the test itself was cancelled or timed out.
            if (createdInOther is not null)
                await otherCategories.Delete(createdInOther.Id, createdInOther.Version, CancellationToken.None);

            if (createdInStore is not null)
                await storeCategories.Delete(createdInStore.Id, createdInStore.Version, CancellationToken.None);
        }
    }

    private CategoryDto NewCategory() => new() { Id = Guid.CreateSequentialGuid(), Name = $"dash-cat-{Guid.NewGuid():N}", Color = "#336699" };

    /// <summary>
    /// A real hub connection for <paramref name="scope"/>, counting the <c>DASHBOARD_DATA_CHANGED</c> it receives.
    /// </summary>
    private async Task<SignedInClient> ConnectAsSignedInClient(AppTestServer server, AsyncServiceScope scope)
    {
        var accessToken = await scope.ServiceProvider.GetRequiredService<IAuthTokenProvider>().GetAccessToken();

        Assert.IsFalse(string.IsNullOrWhiteSpace(accessToken), "An anonymous connection joins no group at all, which would make this test assert nothing.");

        var hubConnection = new HubConnectionBuilder()
            .WithUrl(new Uri(server.WebAppServerAddress, "app-hub"), options =>
            {
                options.Transports = HttpTransportType.WebSockets;
                options.AccessTokenProvider = () => Task.FromResult<string?>(accessToken);
            })
            .Build();

        var client = new SignedInClient(hubConnection);

        await hubConnection.StartAsync(TestContext.CancellationToken);

        // StartAsync returns once the handshake is answered, which happens before OnConnectedAsync has finished joining
        // the groups. A hub method is dispatched only after OnConnectedAsync completed, and this one re-runs that very
        // join, so awaiting it is a deterministic gate on group membership rather than a sleep.
        await hubConnection.InvokeAsync(SharedAppMessages.ChangeAuthenticationState, accessToken, TestContext.CancellationToken);

        return client;
    }

    private async Task WaitUntil(Func<bool> condition, string message)
    {
        var deadline = DateTimeOffset.UtcNow + TimeSpan.FromSeconds(15);

        while (condition() is false)
        {
            if (DateTimeOffset.UtcNow >= deadline)
                Assert.Fail(message);

            await Task.Delay(TimeSpan.FromMilliseconds(50), TestContext.CancellationToken);
        }
    }

    /// <summary>A signed-in client's hub connection, with the <c>DASHBOARD_DATA_CHANGED</c> messages it received.</summary>
    private sealed class SignedInClient : IAsyncDisposable
    {
        private readonly HubConnection hubConnection;
        private int dashboardMessages;

        public SignedInClient(HubConnection hubConnection)
        {
            this.hubConnection = hubConnection;

            // Exactly what AppClientCoordinator subscribes to; the payload of this particular message is always null.
            hubConnection.On<string, object?>(SharedAppMessages.PUBLISH_MESSAGE, (message, _) =>
            {
                if (message is SharedAppMessages.DASHBOARD_DATA_CHANGED)
                {
                    Interlocked.Increment(ref dashboardMessages);
                }
            });
        }

        public int DashboardMessages => Volatile.Read(ref dashboardMessages);

        public ValueTask DisposeAsync() => hubConnection.DisposeAsync();
    }
}
