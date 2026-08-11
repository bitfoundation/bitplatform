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
}
