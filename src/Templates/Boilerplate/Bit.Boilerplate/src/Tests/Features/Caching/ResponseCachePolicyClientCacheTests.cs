//+:cnd:noEmit
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.FileProviders;
using Boilerplate.Server.Shared;
using Microsoft.AspNetCore.OutputCaching;
using Boilerplate.Server.Shared.Infrastructure.Services;
using Boilerplate.Client.Core.Infrastructure.Services.HttpMessageHandlers;

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
    /// The control. An anonymous caller resolves its tenant from the host, and a signed-in member never asks for its URL
    /// (the client adds the member's tenant to it, See AuthDelegatingHandler), so the carve-out must not touch it - that
    /// traffic is what the client cache exists for. If this fails, the rule stopped being a carve-out and became a
    /// blanket pessimisation.
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
    /// The url a member asks for carries its tenant (See AuthDelegatingHandler), but the server does not resolve the
    /// tenant from it, so an anonymous caller sending the same parameter is answered with the host's rows. Kept at the
    /// edge or in the browser, that answer would be handed to the members of that tenant.
    /// </summary>
    [TestMethod]
    public async Task AnAnonymousCallerNamingATenantInTheUrl_Should_NotGetAClientOrEdgeMaxAge()
    {
        var httpContext = await RunPolicy(tenantId: null, queryString: $"?{AppResponseCacheAttribute.TenantQueryParameterName}={Guid.NewGuid()}");

        Assert.IsNull(httpContext.Response.GetTypedHeaders().CacheControl?.MaxAge);
        Assert.IsNull(httpContext.Response.GetTypedHeaders().CacheControl?.SharedMaxAge);

        var decision = httpContext.Response.Headers["App-Cache-Response"].ToString();
        Assert.Contains("Client:-1", decision);
        Assert.Contains("Edge:-1", decision);
        // The output cache varies by the resolved tenant (See the Tenant rule), so a member's request never meets this entry.
        Assert.DoesNotContain("Output:-1", decision);
    }

    /// <summary>
    /// The client's half of the rule above: a member's GET names its token's tenant, so it never asks for the url an
    /// anonymous caller's answer is kept under.
    /// </summary>
    [TestMethod]
    public async Task AMembersGetRequest_Should_CarryItsTenantInTheUrl()
    {
        var tenantId = Guid.NewGuid();

        var requestUri = await SendThroughAuthDelegatingHandler(HttpMethod.Get, BuildAccessToken(tenantId));

        Assert.AreEqual($"{ProductViewUrl}&{AppResponseCacheAttribute.TenantQueryParameterName}={tenantId}", requestUri.AbsoluteUri);
    }

    [TestMethod]
    public async Task AnAnonymousGetOrAMembersPost_Should_KeepTheUrl()
    {
        Assert.AreEqual(ProductViewUrl, (await SendThroughAuthDelegatingHandler(HttpMethod.Get, accessToken: null)).AbsoluteUri);
        Assert.AreEqual(ProductViewUrl, (await SendThroughAuthDelegatingHandler(HttpMethod.Post, BuildAccessToken(Guid.NewGuid()))).AbsoluteUri);
    }

    private const string ProductViewUrl = "https://localhost/api/v1/ProductView/Get?%24top=6&%24orderby=Name";

    private static async Task<Uri> SendThroughAuthDelegatingHandler(HttpMethod method, string? accessToken)
    {
        var tokenProvider = A.Fake<IAuthTokenProvider>();
        A.CallTo(() => tokenProvider.GetAccessToken()).Returns(accessToken);

        var transport = new RequestCapturingHandler();

        using var invoker = new HttpMessageInvoker(new AuthDelegatingHandler(jsRuntime: null!, storageService: null!, serviceProvider: null!,
            tokenProvider, localizer: null!, transport));

        using var response = await invoker.SendAsync(new HttpRequestMessage(method, ProductViewUrl), CancellationToken.None);

        return transport.RequestUri!;
    }

    private static string BuildAccessToken(Guid tenantId)
    {
        var claims = $$"""{"nameid":"{{Guid.NewGuid()}}","{{AppClaimTypes.TENANT_ID}}":"{{tenantId}}","exp":{{DateTimeOffset.UtcNow.AddMinutes(5).ToUnixTimeSeconds()}}}""";

        return $"header.{Convert.ToBase64String(Encoding.UTF8.GetBytes(claims)).TrimEnd('=').Replace('+', '-').Replace('/', '_')}.signature";
    }

    private sealed class RequestCapturingHandler : HttpMessageHandler
    {
        public Uri? RequestUri { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestUri = request.RequestUri;

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }

    /// <summary>
    /// Drives the real policy against a request shaped like <c>ProductViewController.Get</c> - the endpoint the defect
    /// was found on: <c>UserAgnostic</c> (so the authenticated downgrade for the shared caches does not fire) with a
    /// five minute <c>MaxAge</c>, outside Development (which would zero every client ttl on its own).
    /// </summary>
    private static async Task<HttpContext> RunPolicy(Guid? tenantId, bool authenticated = false, bool userAgnostic = true, string? queryString = null)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Scheme = "https";
        httpContext.Request.Host = new("localhost");
        httpContext.Request.Path = "/api/v1/ProductView/Get/1";
        httpContext.Request.QueryString = queryString is null ? QueryString.Empty : new(queryString);

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
