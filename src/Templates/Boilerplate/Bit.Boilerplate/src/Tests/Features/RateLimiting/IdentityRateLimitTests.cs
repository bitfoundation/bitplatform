using System.Net;

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
/// This test pins both halves of the fix, because each can regress on its own and neither is visible from a
/// build: the policy exists AND at least one endpoint opts into it (first method), and the policy is <b>named</b>
/// rather than global, so it does not throttle the rest of the app - the Blazor circuit negotiate, the health
/// probes and the Hangfire dashboard all sit behind the same <c>UseRateLimiter</c> call (second method).
/// </para>
/// </summary>
[TestClass, TestCategory("IntegrationTest")]
public class IdentityRateLimitTests
{
    /// <summary>
    /// <c>AppRateLimitPolicies.IDENTITY</c> permits 30 requests per minute per partition, and every request here
    /// shares the loopback partition, so the burst must be cut off before it completes. The e-mail address is a
    /// fresh Guid that no account owns: <c>SendConfirmEmailToken</c> answers 400 <c>UserNotFound</c> for it and
    /// writes nothing, so this test cannot leave state behind in the shared development database.
    /// </summary>
    [TestMethod]
    public async Task AnAnonymousIdentityEndpoint_Should_StopServingAfterTheBurstLimit()
    {
        await using var server = new AppTestServer();

        await server.Build(services => services.AddIntegrationApiOnlyTestsServices()).Start(TestContext.CancellationToken);

        using var httpClient = new HttpClient { BaseAddress = server.WebAppServerAddress };

        var statusCodes = await SendBurst(httpClient, "api/v1/Identity/SendConfirmEmailToken",
            $$"""{"email":"{{Guid.NewGuid()}}@example.com"}""", count: 40);

        // Control: the endpoint really is reachable and really is answering, so a missing 429 below would mean
        // "not throttled", not "never got there".
        Assert.IsTrue(statusCodes.Contains(HttpStatusCode.BadRequest),
            $"Expected the endpoint's own 400 UserNotFound among the responses. Got: {Describe(statusCodes)}");

        Assert.IsTrue(statusCodes.Contains(HttpStatusCode.TooManyRequests),
            $"A 40-request burst against a rate-limited anonymous endpoint must be throttled. Got: {Describe(statusCodes)}");
    }

    /// <summary>
    /// The same burst against an endpoint that does NOT opt in must never be throttled. This is what distinguishes
    /// the named policy that shipped from the global limiter the finding explicitly argued against - a global
    /// limiter would make this method fail.
    /// </summary>
    [TestMethod]
    public async Task AnEndpointThatDoesNotOptIn_Should_NotBeRateLimited()
    {
        await using var server = new AppTestServer();

        await server.Build(services => services.AddIntegrationApiOnlyTestsServices()).Start(TestContext.CancellationToken);

        using var httpClient = new HttpClient { BaseAddress = server.WebAppServerAddress };

        var statusCodes = await SendBurst(httpClient, ".well-known/jwks", content: null, count: 40);

        Assert.IsTrue(statusCodes.Contains(HttpStatusCode.OK),
            $"Control: the un-opted-in endpoint must answer at all. Got: {Describe(statusCodes)}");

        Assert.IsFalse(statusCodes.Contains(HttpStatusCode.TooManyRequests),
            $"The rate limit must stay scoped to the endpoints that opt in, otherwise health probes and the Blazor "
            + $"circuit share a bucket with it. Got: {Describe(statusCodes)}");
    }

    private async Task<HttpStatusCode[]> SendBurst(HttpClient httpClient, string route, string? content, int count)
    {
        List<HttpStatusCode> statusCodes = [];

        for (var i = 0; i < count; i++)
        {
            using var body = content is null ? null : new StringContent(content, System.Text.Encoding.UTF8, "application/json");

            using var response = content is null
                ? await httpClient.GetAsync(route, TestContext.CancellationToken)
                : await httpClient.PostAsync(route, body, TestContext.CancellationToken);

            statusCodes.Add(response.StatusCode);
        }

        return [.. statusCodes];
    }

    private static string Describe(HttpStatusCode[] statusCodes)
        => string.Join(", ", statusCodes.GroupBy(statusCode => statusCode).Select(group => $"{(int)group.Key} x{group.Count()}"));

    public TestContext TestContext { get; set; } = default!;
}
