using Boilerplate.Shared.Features.Identity;
using Boilerplate.Shared.Features.MinimalApiSample;

namespace Boilerplate.Tests.Features.RateLimiting;

/// <summary>
/// <b>Regression test for BP-160.</b>
/// <para>
/// <c>services.AddRateLimiter()</c> used to be called with the parameterless overload, which leaves
/// <c>RateLimiterOptions.GlobalLimiter</c> null and registers no named policy - and no endpoint anywhere in the
/// template carried <c>[EnableRateLimiting]</c>. Both <c>app.UseRateLimiter()</c> calls were therefore inert
/// middleware: the anonymous credential endpoints had no throttling at all, in every configuration, while the
/// pipeline read as though they did.
/// </para>
/// <para>
/// Both halves of the fix are pinned, because each can regress on its own and neither is visible from a build:
/// the policy exists AND an endpoint opts into it (first method), and it is <b>named</b> rather than global, so
/// it does not throttle the rest of the app - the Blazor circuit negotiate, the health probes and the Hangfire
/// dashboard all sit behind the same <c>UseRateLimiter</c> call (second method).
/// </para>
/// </summary>
[TestClass, TestCategory("IntegrationTest")]
public class IdentityRateLimitTests
{
    private const int BurstSize = 40; // AppRateLimitPolicies.IDENTITY permits 30 per minute.

    /// <summary>
    /// Driven through the app's own typed controller proxy, so the rejection arrives as the
    /// <see cref="TooManyRequestsException"/> a real client would handle rather than as a bare status code.
    /// The e-mail is a fresh Guid that no account owns: the endpoint answers <c>UserNotFound</c> for it and
    /// writes nothing, so this test cannot leave state behind in the shared development database.
    /// </summary>
    [TestMethod]
    public async Task AnAnonymousIdentityEndpoint_Should_StopServingAfterTheBurstLimit()
    {
        await using var server = new AppTestServer();

        await server.Build(services => services.AddIntegrationApiOnlyTestsServices()).Start(TestContext.CancellationToken);

        await using var scope = server.WebApp.Services.CreateAsyncScope();

        var identityController = scope.ServiceProvider.GetRequiredService<IIdentityController>();

        var served = 0;

        for (var i = 0; i < BurstSize; i++)
        {
            try
            {
                await identityController.SendConfirmEmailToken(new() { Email = $"{Guid.NewGuid()}@example.com" }, TestContext.CancellationToken);
            }
            catch (TooManyRequestsException)
            {
                // Control: the endpoint really answered before it started refusing, so this is throttling
                // rather than the request never getting there.
                Assert.IsGreaterThan(0, served, "The endpoint must serve some requests before throttling starts.");
                return;
            }
            catch (BadRequestException)
            {
                served++; // UserNotFound - the endpoint's own answer for an unknown e-mail.
            }
        }

        Assert.Fail($"A burst of {BurstSize} requests against a rate-limited anonymous endpoint was never throttled ({served} served).");
    }

    /// <summary>
    /// The same burst against an endpoint that does NOT opt in must never be throttled. This is what
    /// distinguishes the named policy that shipped from the global limiter the finding argued against - a global
    /// limiter would make this method fail. Completing the loop IS the assertion: a 429 would surface as
    /// <see cref="TooManyRequestsException"/> and fail the test on its own.
    /// </summary>
    [TestMethod]
    public async Task AnEndpointThatDoesNotOptIn_Should_NotBeRateLimited()
    {
        await using var server = new AppTestServer();

        await server.Build(services => services.AddIntegrationApiOnlyTestsServices()).Start(TestContext.CancellationToken);

        await using var scope = server.WebApp.Services.CreateAsyncScope();

        var minimalApiSampleController = scope.ServiceProvider.GetRequiredService<IMinimalApiSampleController>();

        for (var i = 0; i < BurstSize; i++)
        {
            var result = await minimalApiSampleController.MinimalApiSample($"burst-{i}", queryStringParameter: null, TestContext.CancellationToken);

            Assert.AreEqual($"burst-{i}", result.GetProperty("routeParameter").GetString(),
                $"Control: the un-opted-in endpoint must keep answering. Failed on request {i + 1}.");
        }
    }

    public TestContext TestContext { get; set; } = default!;
}
