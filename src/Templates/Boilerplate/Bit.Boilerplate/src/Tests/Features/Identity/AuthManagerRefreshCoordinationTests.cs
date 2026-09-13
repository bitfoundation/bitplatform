//+:cnd:noEmit
using Boilerplate.Shared.Features.Identity.Dtos;

namespace Boilerplate.Tests.Features.Identity;

/// <summary>
/// <c>AuthManager.RefreshToken</c> de-duplicates concurrent refreshes through a single shared
/// <c>TaskCompletionSource</c>. That is desirable while one request is genuinely in flight and both callers want the
/// same thing - but a caller asking for something specific (a tenant to switch into, an elevated access token) must
/// never be answered by somebody else's plain refresh, because its arguments would be silently discarded and it would
/// still be told it succeeded.
/// <para>
/// The original bug was subtler than "two callers race": the shared source was created without
/// <c>RunContinuationsAsynchronously</c>, so <c>SetResult</c> resumed the awaiting caller <b>synchronously, inside the
/// refresh</b> - before the <c>finally</c> that clears the field had run. A caller that awaited one refresh and
/// immediately started another therefore found a non-null, already-completed field, and got that task back.
/// </para>
/// </summary>
[TestClass, TestCategory("IntegrationTest"), TestCategory("Identity")]
public partial class AuthManagerRefreshCoordinationTests
{
    public TestContext TestContext { get; set; } = default!;

    //#if (multitenant == true)
    /// <summary>
    /// The regression guard, driving the real sequence: a plain refresh is <b>in flight</b> - which is the normal
    /// state, <c>AppClientCoordinator</c> refreshes on every auth-state propagation - and the user switches tenant
    /// while it is. The switch must reach the wire carrying its tenant id rather than being handed the plain
    /// refresh's answer.
    /// <para>
    /// ⚠ An earlier version of this test awaited the first refresh to completion and only then switched tenant. It
    /// passed against the unfixed code, because by then the shared source had already been cleared - i.e. it would
    /// have shipped as a guard that guards nothing. Holding the first request open is what makes it discriminating,
    /// and is also the shape the defect actually takes in the app.
    /// </para>
    /// </summary>
    [TestMethod]
    public async Task ATenantSwitch_DuringAnInFlightRefresh_Should_StillReachTheServer()
    {
        var requestedTenantId = Guid.NewGuid();
        var refreshRequests = new List<RefreshTokenRequestDto>();
        var releaseTheFirstRefresh = new TaskCompletionSource();

        var identityController = A.Fake<IIdentityController>();
        A.CallTo(() => identityController.Refresh(A<RefreshTokenRequestDto>._, A<CancellationToken>._))
            .ReturnsLazily(async (RefreshTokenRequestDto request, CancellationToken _) =>
            {
                refreshRequests.Add(request);
                if (refreshRequests.Count is 1)
                {
                    await releaseTheFirstRefresh.Task; // Hold the plain refresh open so the switch genuinely overlaps it.
                }
                // No access token: StoreTokens returns early, so this test needs no parseable jwt. What is under test
                // is which requests reach the controller, not what comes back.
                return new TokenResponseDto();
            });

        await using var server = new AppTestServer();
        await server.Build(configureTestServices: services =>
        {
            services.AddIntegrationApiOnlyTestsServices();
            services.RemoveAll<IIdentityController>();
            services.AddScoped(_ => identityController);
        }).Start(TestContext.CancellationToken);

        await using var scope = server.WebApp.Services.CreateAsyncScope();

        // RefreshToken bails out before calling the controller when there is no stored refresh token.
        await scope.ServiceProvider.GetRequiredService<IStorageService>().SetItem("refresh_token", "a-refresh-token");

        var authManager = scope.ServiceProvider.GetRequiredService<AuthManager>();

        var plainRefresh = authManager.RefreshToken(requestedBy: nameof(ATenantSwitch_DuringAnInFlightRefresh_Should_StillReachTheServer));
        var tenantSwitch = authManager.SwitchTenant(requestedTenantId, TestContext.CancellationToken);

        releaseTheFirstRefresh.SetResult();
        await Task.WhenAll(plainRefresh, tenantSwitch);

        Assert.HasCount(2, refreshRequests,
            "The tenant switch was answered by the in-flight plain refresh, so it never issued a request of its own - " +
            "and still reported success to the caller.");
        Assert.AreEqual(requestedTenantId, refreshRequests[1].RequestedTenantId);
    }
    //#endif

    /// <summary>
    /// The other half of the contract, and the reason the shared source exists at all: two plain refreshes that
    /// overlap must still collapse into one request. Without this the fix above could be "give everyone their own
    /// request", which would restore the thundering herd the coordinator was written to prevent.
    /// </summary>
    [TestMethod]
    public async Task TwoOverlappingPlainRefreshes_Should_StillShareOneRequest()
    {
        var refreshRequests = new List<RefreshTokenRequestDto>();
        var releaseTheRefresh = new TaskCompletionSource();

        var identityController = A.Fake<IIdentityController>();
        A.CallTo(() => identityController.Refresh(A<RefreshTokenRequestDto>._, A<CancellationToken>._))
            .ReturnsLazily(async (RefreshTokenRequestDto request, CancellationToken _) =>
            {
                refreshRequests.Add(request);
                await releaseTheRefresh.Task; // Hold it in flight so the second caller genuinely overlaps the first.
                return new TokenResponseDto();
            });

        await using var server = new AppTestServer();
        await server.Build(configureTestServices: services =>
        {
            services.AddIntegrationApiOnlyTestsServices();
            services.RemoveAll<IIdentityController>();
            services.AddScoped(_ => identityController);
        }).Start(TestContext.CancellationToken);

        await using var scope = server.WebApp.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<IStorageService>().SetItem("refresh_token", "a-refresh-token");

        var authManager = scope.ServiceProvider.GetRequiredService<AuthManager>();

        var first = authManager.RefreshToken(requestedBy: "first");
        var second = authManager.RefreshToken(requestedBy: "second");

        releaseTheRefresh.SetResult();
        await Task.WhenAll(first, second);

        Assert.HasCount(1, refreshRequests, "Two overlapping plain refreshes must share a single request.");
    }

    /// <summary>
    /// The third part of the contract, and the one that has no visible failure until it happens: the Task
    /// <c>RefreshToken</c> hands back must <b>always</b> complete.
    /// <para>
    /// It is produced by a <c>TaskCompletionSource</c> that the fire-and-forget implementation completes, and that used
    /// to happen on two statements deep inside an inner try - while three awaits sat outside it, the first of them a
    /// storage read that is JS interop on the web client. Anything thrown there left the source uncompleted on a task
    /// nobody observes, and since <c>RefreshToken</c> takes no <c>CancellationToken</c>, every awaiter then waited
    /// forever: <c>AuthDelegatingHandler</c> in the middle of an HTTP request, the SignalR access token provider, a
    /// tenant switch. The user's only way out was reloading the app.
    /// </para>
    /// </summary>
    [TestMethod]
    public async Task AFailureInsideTheRefresh_Should_StillCompleteTheAwaitingCallers()
    {
        await using var server = new AppTestServer();
        await server.Build(configureTestServices: services =>
        {
            services.AddIntegrationApiOnlyTestsServices();
            services.RemoveAll<IStorageService>();
            services.AddScoped<IStorageService, ThrowsWhenTheRefreshTokenIsRead>();
        }).Start(TestContext.CancellationToken);

        await using var scope = server.WebApp.Services.CreateAsyncScope();

        var authManager = scope.ServiceProvider.GetRequiredService<AuthManager>();

        // Two callers, because the second joins the first's shared source rather than starting its own - so a source
        // that is never completed strands both, which is the shape the defect takes in the app.
        var first = authManager.RefreshToken(requestedBy: "first");
        var second = authManager.RefreshToken(requestedBy: "second");

        // Any timeout at all is the assertion: unfixed, this waits forever rather than a few seconds.
        var results = await Task.WhenAll(first, second).WaitAsync(TimeSpan.FromSeconds(30), TestContext.CancellationToken);

        Assert.IsNull(results[0], "A refresh that could not run reports failure by returning null.");
        Assert.IsNull(results[1]);

        // The manager has to be usable afterwards: the finally still clears the shared field and releases the
        // semaphore, so a later refresh is not blocked by the failed one.
        Assert.IsNull(await authManager.RefreshToken(requestedBy: "after").WaitAsync(TimeSpan.FromSeconds(30), TestContext.CancellationToken));
    }

    /// <summary>
    /// Reproduces the one real thrower on that path: reading the refresh token is JS interop on the web client, which
    /// throws when the circuit is being torn down or the call times out. Everything else behaves normally, so the test
    /// fails for this reason and no other.
    /// </summary>
    private sealed class ThrowsWhenTheRefreshTokenIsRead : IStorageService
    {
        private readonly TestStorageService inner = new();

        public ValueTask<string?> GetItem(string key)
        {
            if (key is "refresh_token")
                throw new InvalidOperationException("JavaScript interop calls cannot be issued at this time.");

            return inner.GetItem(key);
        }

        public ValueTask<bool> IsPersistent(string key) => inner.IsPersistent(key);
        public ValueTask RemoveItem(string key) => inner.RemoveItem(key);
        public ValueTask SetItem(string key, string? value, bool persistent = true) => inner.SetItem(key, value, persistent);
        public ValueTask Clear() => inner.Clear();
    }
}
