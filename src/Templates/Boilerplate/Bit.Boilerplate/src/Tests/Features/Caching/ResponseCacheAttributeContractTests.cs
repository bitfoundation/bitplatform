using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.OutputCaching;
using Boilerplate.Shared.Infrastructure.Attributes;

namespace Boilerplate.Tests.Features.Caching;

/// <summary>
/// Every rule an <see cref="AppResponseCacheAttribute"/> has to satisfy, checked against the endpoints the app maps
/// rather than against source text - so a Blazor page's <c>@attribute</c>, a controller action and a minimal api
/// handler are all covered through the endpoint metadata <c>AppResponseCachePolicy</c> itself reads at runtime.
/// <para>
/// Unlike most of this suite it ships to generated projects, since that is where new pages and endpoints get the
/// attribute - so it names no module or feature specific endpoint and validates whatever the project mapped.
/// </para>
/// </summary>
[TestClass, TestCategory("IntegrationTest"), TestCategory("Caching")]
public partial class ResponseCacheAttributeContractTests
{
    public TestContext TestContext { get; set; } = default!;

    /// <summary>The policy every cacheable endpoint is wired to, by <c>.CacheOutput(...)</c>.</summary>
    private const string PolicyName = "AppResponseCachePolicy";

    [TestMethod]
    public async Task EveryAppResponseCacheAttribute_Should_BeValid()
    {
        await using var server = new AppTestServer();

        await server.Build(services => services.AddIntegrationApiOnlyTestsServices().FakeExternalStatistics())
                    .Start(TestContext.CancellationToken);

        var endpoints = ((IEndpointRouteBuilder)server.WebApp).DataSources
            .SelectMany(dataSource => dataSource.Endpoints)
            .ToArray();

        Assert.IsGreaterThan(0, endpoints.Length, "No endpoints were discovered, so this test would pass without checking anything.");

        List<string> problems = [];
        var annotatedEndpoints = 0;

        foreach (var endpoint in endpoints)
        {
            var attributes = endpoint.Metadata.OfType<AppResponseCacheAttribute>().ToArray();

            if (attributes.Length is 0)
                continue;

            annotatedEndpoints++;

            var endpointName = endpoint.DisplayName ?? endpoint.ToString() ?? "<unnamed endpoint>";

            // Without a .CacheOutput(...) the attribute does nothing at all - no headers, no entry, no complaint.
            // MapControllers and MapRazorComponents apply the policy to every action and page at once, so it is a new
            // MINIMAL API that has to remember the call itself (see the sitemap endpoints, which each repeat it).
            // Only presence is asserted: .CacheOutput("name") records an internal NamedPolicy no public api exposes
            // the name of. Enough here, since the only other policy ("HealthChecks") is on endpoints without this attribute.
            if (endpoint.Metadata.OfType<IOutputCachePolicy>().Any() is false)
            {
                problems.Add($"'{endpointName}' carries [AppResponseCache] but no output cache policy. " +
                             $"Add .CacheOutput(\"{PolicyName}\") to the endpoint, or the attribute silently does nothing.");
            }

            foreach (var attribute in attributes)
            {
                // What GetResponseCacheAttribute throws on at request time - which only happens once it is hit.
                if (attribute.MaxAge is -1 && attribute.SharedMaxAge is -1)
                {
                    problems.Add($"'{endpointName}' sets neither MaxAge nor SharedMaxAge. At least one of them must be specified, " +
                                 "otherwise the endpoint throws on its first request.");
                    continue;
                }

                foreach (var (name, value) in new[] { (nameof(attribute.MaxAge), attribute.MaxAge), (nameof(attribute.SharedMaxAge), attribute.SharedMaxAge) })
                {
                    // -1 is "layer off", anything else is seconds. Zero is neither: the policy tests both `!= -1`
                    // and `> 0`, so it takes one branch and not the other - announcing a layer that stores nothing.
                    if (value is not -1 && value <= 0)
                    {
                        problems.Add($"'{endpointName}' sets {name} to {value}. It has to be -1 (that layer is off) or a positive number of seconds.");
                    }
                }

                // SharedMaxAge falls back to MaxAge when unset, which is what an endpoint that names only MaxAge gets.
                var effectiveSharedMaxAge = attribute.SharedMaxAge is -1 ? attribute.MaxAge : attribute.SharedMaxAge;

                // ResponseCacheService reaches the output cache and the CDN edge only, so a client copy that outlives
                // the shared one survives every purge and keeps being served after the purge worked everywhere else.
                if (attribute.MaxAge > 0 && effectiveSharedMaxAge > 0 && attribute.MaxAge > effectiveSharedMaxAge)
                {
                    problems.Add($"'{endpointName}' caches in the client for {attribute.MaxAge}s but in the shared caches for only {effectiveSharedMaxAge}s. " +
                                 "The client layers cannot be purged, so a purge would leave the longest lived copies of this response stale. " +
                                 "Keep MaxAge at or below SharedMaxAge.");
                }
            }
        }

        Assert.IsGreaterThan(0, annotatedEndpoints,
            "No endpoint carried [AppResponseCache], so this test asserted nothing. The sitemap endpoints alone should have matched.");

        Assert.IsEmpty(problems, $"Invalid [AppResponseCache] usage:{Environment.NewLine}- {string.Join($"{Environment.NewLine}- ", problems)}");
    }
}
