using Boilerplate.Tests.E2E.Infrastructure.Services;

namespace Boilerplate.Tests.E2E.Features.RateLimiting;

/// <summary>
/// The IDENTITY policy is a fixed window in the API process' own memory (See <c>RateLimitOptionsExtensions</c>).
/// <para>
/// Not parallelized, and it restarts the API afterwards: the burst exhausts a partition every anonymous caller of that
/// deployment shares.
/// </para>
/// </summary>
[TestClass, TestCategory(TestCategories.Api), Retry(2), DoNotParallelize]
public class IdentityRateLimitTests
{
    private const int burstSize = 45; // The policy permits 30 a minute; the rest are what proves it stops serving.

    /// <summary>The policy's own window, and so the longest a partition can stay exhausted without a restart.</summary>
    private static readonly TimeSpan window = TimeSpan.FromMinutes(1);

    public TestContext TestContext { get; set; } = default!;

    [TestMethod]
    public async Task AnAnonymousIdentityEndpoint_Should_StopServingAfterTheBurstLimit()
    {
        var api = DeployedApps.TodoApi;

        await using var apiClient = DeployedApiClientProvider.CreateApiClientFor(api);

        var identityController = apiClient.Services.GetRequiredService<IIdentityController>();

        var served = 0;

        try
        {
            for (var i = 0; i < burstSize; i++)
            {
                try
                {
                    // An address no account owns: the endpoint answers UserNotFound, writes nothing and mails nobody.
                    await identityController.SendConfirmEmailToken(new() { Email = $"{Guid.NewGuid()}@e2e.invalid" }, TestContext.CancellationToken);
                    served++;
                }
                catch (BadRequestException)
                {
                    served++;
                }
                catch (TooManyRequestsException)
                {
                    // Only the middleware can answer 429 here - the endpoint's own resend delay needs an existing user.
                    Assert.IsGreaterThan(0, served,
                        $"{api} refused the very first request, so this run proved no throttling of its own: it inherited a window someone else had already exhausted.");

                    await AssertHealthIsNotThrottled(api, apiClient);

                    return;
                }
            }

            Assert.Fail($"A burst of {burstSize} requests against {api}'s rate limited identity endpoint was never throttled ({served} served).");
        }
        finally
        {
            await ResetTheWindow(api);
        }
    }

    /// <summary>
    /// The policy is named rather than global, so /health - what the deployment is judged by - keeps answering while
    /// the identity endpoints are shut.
    /// </summary>
    private async Task AssertHealthIsNotThrottled(string api, DeployedApiClient apiClient)
    {
        try
        {
            using var response = await apiClient.HttpClient.GetAsync("health", TestContext.CancellationToken);
        }
        catch (TooManyRequestsException exception)
        {
            Assert.Fail($"{api}health was throttled alongside the identity endpoints, so the limiter is not scoped to its policy: {exception.Message}");
        }
    }

    /// <summary>
    /// Not on the test's own token: a cancelled or timed out test still owes the next one an unthrottled deployment.
    /// </summary>
    private async Task ResetTheWindow(string api)
    {
        var (sitePath, _) = DeployedApps.DeploymentOfApi(api);

        var refusal = await DeployedSite.TryRestart(sitePath, new Uri(new Uri(api), "health"), CancellationToken.None);

        if (refusal is null)
        {
            TestContext.WriteLine($"Restarted {api}, so it starts counting again from zero.");
            return;
        }

        // Fixed windows also replenish with time, which is the only way back when the process cannot be stopped.
        TestContext.WriteLine($"Could not restart {api} ({refusal}), so waiting {window} out for the window to expire on its own.");

        await Task.Delay(window, CancellationToken.None);
    }
}
