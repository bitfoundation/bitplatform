//+:cnd:noEmit
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.FileProviders;
using Boilerplate.Server.Shared;
using Microsoft.AspNetCore.OutputCaching;
using Boilerplate.Server.Shared.Infrastructure.Services;

namespace Boilerplate.Tests.Features.Caching;

/// <summary>
/// <c>AppResponseCachePolicy</c> gives the output cache a tenant dimension through <c>VaryByValues["Tenant"]</c>. That
/// covers the shared caches and nothing else: <c>VaryByValues</c> never becomes a response header, so the two
/// <b>private</b> caches - the browser's own HTTP cache and the client's <c>CacheDelegatingHandler</c> - cannot see
/// it. They key on the URL, which for an authenticated caller is identical whatever tenant they are in. So a
/// <c>max-age</c> on a tenant filtered body lets a browser replay one tenant's rows to whoever signs in next on that
/// profile, and the browser's cache outlives the process - which is why this has to be fixed on the server.
/// <para>
/// This is a plain unit test rather than an integration one because <c>AppTestServer</c> hard-codes
/// <c>EnvironmentName = Development</c>, and the policy zeroes every client ttl in Development. No test that boots the
/// real server can observe a client <c>max-age</c> at all, so the rule under test would be invisible to it.
/// </para>
/// </summary>
[TestClass, TestCategory("UnitTest"), TestCategory("Caching")]
public partial class ResponseCachePolicyClientCacheTests
{
    private const int MaxAgeSeconds = 300;

    /// <summary>
    /// The control. An anonymous caller resolves its tenant from the host, so the tenant is already part of the URL
    /// every private cache keys on, and the carve-out must not touch it - that traffic is what the client cache
    /// exists for. If this fails, the rule stopped being a carve-out and became a blanket pessimisation.
    /// </summary>
    [TestMethod]
    public async Task AnAnonymousCaller_Should_KeepTheClientMaxAge()
    {
        var httpContext = await RunPolicy(tenantId: null);

        var cacheControl = httpContext.Response.GetTypedHeaders().CacheControl;

        Assert.IsNotNull(cacheControl);
        Assert.AreEqual(TimeSpan.FromSeconds(MaxAgeSeconds), cacheControl.MaxAge);
    }

    [TestMethod]
    public async Task ACallerWhoseTenantCameFromAClaim_Should_NotGetAClientMaxAge()
    {
        var httpContext = await RunPolicy(tenantId: Guid.NewGuid());

        Assert.IsNull(httpContext.Response.GetTypedHeaders().CacheControl?.MaxAge,
            "A response the server filtered by the caller's tenant claim must not be storable in a URL keyed private " +
            "cache, or the next person to use this browser profile is served this tenant's rows.");

        Assert.IsNull(httpContext.Response.GetTypedHeaders().CacheControl?.SharedMaxAge,
            "Nor in the CDN edge: the Tenant dimension is a VaryByValues entry, which never becomes a response " +
            "header, so an edge keyed on the URL would hand this tenant's rows to the next one.");

        var decision = httpContext.Response.Headers["App-Cache-Response"].ToString();
        Assert.Contains("Client:-1", decision);
        Assert.Contains("Edge:-1", decision);
        // The output cache is the one cache that CAN see the Tenant dimension, so it stays enabled - otherwise this
        // rule would be a blanket pessimisation rather than a carve-out.
        Assert.DoesNotContain("Output:-1", decision);
    }

    /// <summary>
    /// The same reasoning one step out: <c>UserAgnostic = false</c> declares that the body depends on the caller, and
    /// the two private caches are no more per-user than the shared ones - one browser profile and one running app each
    /// span every user who signs in on that device. No shipped endpoint sets that combination today; this pins the
    /// decision surface the attribute's own documentation points template consumers at.
    /// </summary>
    [TestMethod]
    public async Task AnAuthenticatedCallerOfANonUserAgnosticEndpoint_Should_NotGetAClientMaxAge()
    {
        var httpContext = await RunPolicy(tenantId: null, authenticated: true, userAgnostic: false);

        Assert.IsNull(httpContext.Response.GetTypedHeaders().CacheControl?.MaxAge);

        var decision = httpContext.Response.Headers["App-Cache-Response"].ToString();
        Assert.Contains("Client:-1", decision);
        Assert.Contains("Edge:-1", decision);
        Assert.Contains("Output:-1", decision);
    }

    /// <summary>
    /// Drives the real policy against a request shaped like <c>ProductViewController.Get</c> - the endpoint the defect
    /// was found on: <c>UserAgnostic</c> (so the authenticated downgrade for the shared caches does not fire) with a
    /// five minute <c>MaxAge</c>, outside Development (which would zero every client ttl on its own).
    /// </summary>
    private static async Task<HttpContext> RunPolicy(Guid? tenantId, bool authenticated = false, bool userAgnostic = true)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Scheme = "https";
        httpContext.Request.Host = new("localhost");
        httpContext.Request.Path = "/api/v1/ProductView/Get/1";

        httpContext.SetEndpoint(new Endpoint(
            requestDelegate: null,
            new EndpointMetadataCollection(new AppResponseCacheAttribute { MaxAge = MaxAgeSeconds, UserAgnostic = userAgnostic }),
            displayName: nameof(ResponseCachePolicyClientCacheTests)));

        if (tenantId is not null || authenticated)
        {
            Claim[] claims = tenantId is null ? [] : [new Claim(AppClaimTypes.TENANT_ID, tenantId.Value.ToString())];
            httpContext.User = new(new ClaimsIdentity(claims, authenticationType: "Bearer"));
        }

        var policy = new AppResponseCachePolicy(new ProductionEnvironment(), new ServerSharedSettings());

        await policy.CacheRequestAsync(new OutputCacheContext { HttpContext = httpContext }, CancellationToken.None);

        return httpContext;
    }

    private sealed class ProductionEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Production;
        public string ApplicationName { get; set; } = nameof(ResponseCachePolicyClientCacheTests);
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
