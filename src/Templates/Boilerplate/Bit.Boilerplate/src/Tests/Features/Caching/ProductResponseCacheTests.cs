//+:cnd:noEmit
using Microsoft.AspNetCore.Localization;
using Boilerplate.Shared.Features.Products;
using Boilerplate.Server.Api.Features.Tenants;
using Boilerplate.Server.Api.Features.Products;
using Boilerplate.Server.Api.Infrastructure.Services;
using Boilerplate.Server.Shared.Infrastructure.Services;

namespace Boilerplate.Tests.Features.Caching;

[TestClass, TestCategory("IntegrationTest"), TestCategory("Caching"), TestCategory("PreRendering")]
public partial class ProductResponseCacheTests
{
    // Seeded members of the default (fallback) store tenant. See UserConfiguration and TenantUserConfiguration.
    private const string TenantAdminEmail = "store-admin@bitplatform.dev";
    private const string TenantUserEmail = "store-user@bitplatform.dev";
    private const string Password = "123456";

    public TestContext TestContext { get; set; } = default!;

    /// <summary>
    /// Proves that with <c>ResponseCaching:EnableOutputCaching</c> and <c>WebAppRender:PrerenderEnabled</c> both on, the
    /// product's <c>UserAgnostic</c> API response and its pre-rendered public page really are stored in ASP.NET Core's
    /// output cache, that a write through the app reaches them, and that a change made behind the app's back does not.
    /// <list type="number">
    /// <item>A product is inserted straight into the database for the default fallback tenant, then read as the
    /// signed-in tenant-user through <c>ProductViewController.Get</c> (which is <c>UserAgnostic</c>, so it is cached
    /// even for an authenticated caller, keyed by her tenant - See <c>AppResponseCachePolicy</c>), and anonymously as
    /// the pre-rendered <c>/product/{shortId}</c> page - the latter under both culture prefixes and with a query string,
    /// each of which lands in the output cache as an entry of its own. All of them are now in the output cache.</item>
    /// <item>The tenant-admin - the member holding <c>ProductCatalog_Manage</c> for that tenant - edits the description
    /// through the real <c>ProductController.Update</c> endpoint, which purges the product right after saving. Every
    /// reader immediately sees the new description, which proves the tags <c>AppResponseCachePolicy</c> writes match the
    /// paths <c>ResponseCacheService.PurgeProductCache</c> evicts - and, since that purge only ever names the bare path,
    /// that the culture and query string an entry was cached under are no part of its tag.</item>
    /// <item>The product is then deleted straight from the database, bypassing the app and therefore the purge. Both
    /// readers keep seeing the deleted product, which is the definitive proof that their responses are being served from
    /// the output cache rather than re-read from the database.</item>
    /// <item>Finally the cache is purged again and the very same reads now report the product as gone, which rules out
    /// any explanation for step 3 other than the cache.</item>
    /// </list>
    /// Note: this test needs both <c>ProductController</c> (Admin module) and <c>ProductViewController</c> / the product
    /// page (Sales module), and <c>module</c> is a single-choice template parameter, so no generated project can host it.
    /// It is excluded unconditionally in template.json and only ever runs against the template's own source tree.
    /// </summary>
    [TestMethod]
    public async Task OutputCache_Should_ServeProduct_UntilItsCacheIsPurged()
    {
        await using var server = new AppTestServer();

        await server.Build(
            configureTestServices: services => services.AddIntegrationApiOnlyTestsServices().FakeExternalStatistics(),
            configureTestConfigurations: configuration =>
            {
                // Pre-rendering makes the server produce the product page's HTML itself, so the page is a cacheable
                // response rather than an empty shell filled in later by the client.
                configuration["WebAppRender:PrerenderEnabled"] = "true";
                configuration["ResponseCaching:EnableOutputCaching"] = "true";
            }).Start(TestContext.CancellationToken);

        // A marker keeps the assertions immune to anything else the page happens to render, and keeps this test's rows
        // isolated from the other tests sharing the same database.
        var marker = Guid.NewGuid().ToString("N");
        var productName = $"cached-product-{marker}";
        var originalDescription = $"original-description-{marker}";
        var updatedDescription = $"updated-description-{marker}";

        var (productId, productShortId) = await CreateProduct(server, productName, originalDescription);

        try
        {

            // ---- Step 1: both readers fetch the product, filling the output cache ----

            // The signed-in tenant-user reads it through the public (UserAgnostic) product view API.
            await using var tenantUserScope = server.WebApp.Services.CreateAsyncScope();
            await SignIn(tenantUserScope, TenantUserEmail);
            var tenantUserProductView = tenantUserScope.ServiceProvider.GetRequiredService<IProductViewController>();

            var seenByTenantUser = await tenantUserProductView.Get(productShortId, TestContext.CancellationToken);
            Assert.AreEqual(productName, seenByTenantUser.Name);
            Assert.AreEqual(originalDescription, seenByTenantUser.DescriptionText);

            // ...and an anonymous visitor reads the pre-rendered public product page. A bare HttpClient - rather than the
            // app's own one from DI - keeps this reader free of any access token and of the client-side message handlers
            // (retry, client caching, exception translation) that would sit between the assertions and what the server
            // actually returned.
            using var visitorHttpClient = new HttpClient { BaseAddress = server.WebAppServerAddress };

            // The same page, requested four ways. Each of them gets its own output cache entry - the path is part of the
            // key, the entry varies by culture, and QueryKeys = "*" gives every query string its own entry - yet they all
            // carry the one tag AppResponseCachePolicy derives from the bare, culture-less path. So the single purge that
            // the admin's edit triggers in step 2 has to clear all four at once.
            string[] pageUrls =
            [
                $"{PageUrls.Product}/{productShortId}",
                $"/en-US{PageUrls.Product}/{productShortId}",
                $"/fa-IR{PageUrls.Product}/{productShortId}",
                $"{PageUrls.Product}/{productShortId}?utm_source={marker}"
            ];

            foreach (var pageUrl in pageUrls)
            {
                var html = await GetProductPage(visitorHttpClient, pageUrl, assertOutputCachingIsActive: true);
                Assert.Contains(productName, html, $"'{pageUrl}' should render the product.");
                Assert.Contains(originalDescription, html, $"'{pageUrl}' should render the product.");
            }

            // ---- Step 2: the tenant-admin edits the description through the real write endpoint ----

            await using (var tenantAdminScope = server.WebApp.Services.CreateAsyncScope())
            {
                var tenantAdminUser = await SignIn(tenantAdminScope, TenantAdminEmail);

                // ProductController demands a privileged session, a selected tenant and ProductCatalog_Manage. Her fresh
                // password sign-in covers the first, and being a t-admin of the tenant that owns the product covers the rest.
                Assert.AreEqual(TenantConfiguration.FallbackTenantId, tenantAdminUser.GetTenantId());
                Assert.IsTrue(tenantAdminUser.HasFeature(AppFeatures.AdminPanel.ProductCatalog_Manage));

                // A real authenticated PUT, so the purge under test is the one the endpoint itself performs after saving
                // (See ProductController.Update) - including running it under a genuine HttpContext.
                var products = tenantAdminScope.ServiceProvider.GetRequiredService<IProductController>();

                var toUpdate = await products.Get(productId, TestContext.CancellationToken);
                toUpdate.DescriptionText = updatedDescription;
                toUpdate.DescriptionHTML = $"<p>{updatedDescription}</p>";

                var updated = await products.Update(toUpdate, TestContext.CancellationToken);
                Assert.AreEqual(updatedDescription, updated.DescriptionText);
            }

            // Both readers see the new description straight away, so the tags AppResponseCachePolicy writes for these
            // requests really are the ones ResponseCacheService.PurgeProductCache evicts. For the page that also means the
            // one purge reached every culture and every query string variant of it, none of which the purging code knows
            // anything about - it only ever names the bare path "/product/{shortId}".
            var seenAfterPurge = await tenantUserProductView.Get(productShortId, TestContext.CancellationToken);
            Assert.AreEqual(updatedDescription, seenAfterPurge.DescriptionText);

            foreach (var pageUrl in pageUrls)
            {
                var html = await GetProductPage(visitorHttpClient, pageUrl);
                Assert.Contains(updatedDescription, html, $"'{pageUrl}' should have been purged by the product's update.");
                Assert.DoesNotContain(originalDescription, html, $"'{pageUrl}' should have been purged by the product's update.");
            }

            // ---- Step 3: the product is deleted behind the app's back, so nothing purges its cache ----

            await DeleteProduct(server, productId);

            await using (var scope = server.WebApp.Services.CreateAsyncScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                Assert.IsFalse(await dbContext.Products.IgnoreQueryFilters().AnyAsync(p => p.Id == productId, TestContext.CancellationToken),
                    "The product should be gone from the database at this point.");
            }

            // Nothing evicted the cached responses, so both readers keep being served the deleted product. Reaching the
            // database would have produced a ResourceNotFoundException (API) and a not-found page (pre-rendered page).
            var seenAfterDelete = await tenantUserProductView.Get(productShortId, TestContext.CancellationToken);
            Assert.AreEqual(productName, seenAfterDelete.Name);
            Assert.AreEqual(updatedDescription, seenAfterDelete.DescriptionText);

            foreach (var pageUrl in pageUrls)
            {
                var html = await GetProductPage(visitorHttpClient, pageUrl);
                Assert.Contains(productName, html, $"'{pageUrl}' should still be served from the cache.");
                Assert.Contains(updatedDescription, html, $"'{pageUrl}' should still be served from the cache.");
            }

            // ---- Step 4: purging makes the deletion visible, which rules out anything but the cache in step 3 ----

            // The row is gone, so ProductController.Delete would 404 before reaching its purge; this control step calls the
            // shared purge service directly instead.
            await using (var scope = server.WebApp.Services.CreateAsyncScope())
            {
                await PurgeProductCache(scope, productShortId);
            }

            await Assert.ThrowsExactlyAsync<ResourceNotFoundException>(
                () => tenantUserProductView.Get(productShortId, TestContext.CancellationToken));

            foreach (var pageUrl in pageUrls)
            {
                var html = await GetProductPage(visitorHttpClient, pageUrl);
                Assert.DoesNotContain(productName, html, $"'{pageUrl}' should have been purged.");
            }
        }
        finally
        {
            // Step 3 already removed it on the happy path; this is what keeps a run that fails anywhere above from
            // leaving a product behind in the database this suite shares with the developer's own app. An
            // ExecuteDeleteAsync of an already deleted row affects zero rows, and the server is disposed after this.
            await DeleteProduct(server, productId);
        }
    }

    /// <summary>
    /// Proves that an edge cacheable response carries the <c>Cache-Tag</c> header the CDN edge entry is later purged by,
    /// and that the tag is exactly the one <c>ResponseCacheService.PurgeProductCache</c> sends to Cloudflare.
    /// The requests deliberately differ in query string, casing and culture: the edge keeps a separate entry per full
    /// URL, so a tag that mirrored any of those would leave the other variants stranded on the edge until they expired
    /// on their own - which is the whole reason the purge is by tag rather than by URL.
    /// </summary>
    [TestMethod]
    public async Task EdgeCaching_Should_TagResponses_WithThePathTheyArePurgedBy()
    {
        await using var server = new AppTestServer();

        await server.Build(
            configureTestServices: services => services.AddIntegrationApiOnlyTestsServices().FakeExternalStatistics(),
            configureTestConfigurations: configuration =>
            {
                configuration["ResponseCaching:EnableCdnEdgeCaching"] = "true";
            }).Start(TestContext.CancellationToken);

        var marker = Guid.NewGuid().ToString("N");
        var (productId, productShortId) = await CreateProduct(server, $"tagged-product-{marker}", $"description-{marker}");

        try
        {

            // A bare HttpClient keeps the client-side message handlers out of the way, so the headers asserted below are
            // the ones the server actually wrote.
            using var visitorHttpClient = new HttpClient { BaseAddress = server.WebAppServerAddress };

            var requestPath = $"/api/v1/ProductView/Get/{productShortId}";
            using var response = await visitorHttpClient.GetAsync($"{requestPath}?utm_source=test", TestContext.CancellationToken);
            response.EnsureSuccessStatusCode();

            // Edge:-1 would mean the response was never meant for the edge, making the rest of this test meaningless.
            Assert.IsTrue(response.Headers.TryGetValues("App-Cache-Response", out var appCacheResponse));
            Assert.DoesNotContain("Edge:-1", string.Concat(appCacheResponse!),
                "Edge caching should be active for this anonymous, UserAgnostic API response.");

            Assert.IsTrue(response.Headers.TryGetValues(AppResponseCachePolicy.CacheTagHeaderName, out var cacheTag),
                "An edge cacheable response must tell the CDN which tag to store it under, otherwise it could never be purged.");

            var tag = string.Concat(cacheTag!);
            Assert.AreEqual($"/api/v1/productview/get/{productShortId}", tag);
            Assert.AreEqual(AppResponseCachePolicy.CreateCacheTag(requestPath), tag,
                "The tag on the response must be the one ResponseCacheService purges the path by.");

            // The very same read in Persian. An API takes its culture from Accept-Language rather than from a path segment,
            // so this is where the culture could leak into the tag on this endpoint; the page equivalent - /fa-IR/product/5
            // and /en-US/product/5 collapsing onto one tag - is covered by the output cache test above.
            using var faRequest = new HttpRequestMessage(HttpMethod.Get, requestPath);
            faRequest.Headers.AcceptLanguage.Add(new("fa-IR"));
            using var faResponse = await visitorHttpClient.SendAsync(faRequest, TestContext.CancellationToken);
            faResponse.EnsureSuccessStatusCode();

            Assert.IsTrue(faResponse.Headers.TryGetValues(AppResponseCachePolicy.CacheTagHeaderName, out var faCacheTag));
            Assert.AreEqual(tag, string.Concat(faCacheTag!),
                "The culture a response was produced in must not be part of the tag it is purged by.");

            // A non-ASCII route reaches CreateCacheTag escaped when the policy reads it out of Uri.AbsolutePath, but raw
            // when a caller of PurgeCache types it out. Tags are matched literally, so the two forms have to converge -
            // otherwise such a page would be tagged with bytes Cloudflare rejects and could never be purged.
            Assert.AreEqual(AppResponseCachePolicy.CreateCacheTag("/%D9%85%D8%AD%D8%B5%D9%88%D9%84/5"),
                            AppResponseCachePolicy.CreateCacheTag("/محصول/5"));
        }
        finally
        {
            // This test used to insert a product and never remove it, so every run left one behind in the SQLite
            // file the whole suite and the developer's own app share - visible in /products, in /products.xml and
            // in the dashboard's product counts.
            await DeleteProduct(server, productId);
        }
    }

    /// <summary>
    /// A response body can depend on <b>both</b> cultures - text comes from <c>CurrentUICulture</c> while prices, dates
    /// and numbers come from <c>CurrentCulture</c> (See <c>ProductDto.FormatPrice</c>) - and the
    /// <c>.AspNetCore.Culture</c> cookie can set the two apart as <c>c=&lt;culture&gt;|uic=&lt;uiCulture&gt;</c>. So a
    /// cache key that recorded only the UI culture would let this request's German price formatting be stored under the
    /// en-US entry and replayed to every en-US visitor, including one that sends no culture cookie at all.
    /// </summary>
    [TestMethod]
    public async Task OutputCache_Should_NotServe_ABodyFormattedInAnotherCulture()
    {
        if (CultureInfoManager.InvariantGlobalization)
        {
            Assert.Inconclusive("There is no culture dimension in the cache key when globalization is invariant.");
        }

        await using var server = new AppTestServer();

        await server.Build(
            configureTestServices: services => services.AddIntegrationApiOnlyTestsServices().FakeExternalStatistics(),
            configureTestConfigurations: configuration => configuration["ResponseCaching:EnableOutputCaching"] = "true")
            .Start(TestContext.CancellationToken);

        var marker = Guid.NewGuid().ToString("N");
        var (productId, productShortId) = await CreateProduct(server, $"culture-product-{marker}", $"description-{marker}");

        try
        {
            using var visitorHttpClient = new HttpClient { BaseAddress = server.WebAppServerAddress };

            var requestPath = $"/api/v1/ProductView/Get/{productShortId}";

            // The two halves deliberately disagree: format like Germany, translate like the default culture.
            using var poisoningRequest = new HttpRequestMessage(HttpMethod.Get, requestPath);
            poisoningRequest.Headers.Add("Cookie", $"{CookieRequestCultureProvider.DefaultCookieName}=c%3Dde-DE%7Cuic%3Den-US");
            using var poisoningResponse = await visitorHttpClient.SendAsync(poisoningRequest, TestContext.CancellationToken);
            poisoningResponse.EnsureSuccessStatusCode();

            var poisoningBody = await poisoningResponse.Content.ReadAsStringAsync(TestContext.CancellationToken);

            // Non-vacuity: the requested formatting culture has to actually reach the body, or the assertion below cannot fail.
            Assert.Contains("€", poisoningBody, "The formatting culture the cookie asked for never reached the response body.");

            using var victimResponse = await visitorHttpClient.GetAsync(requestPath, TestContext.CancellationToken);
            victimResponse.EnsureSuccessStatusCode();

            var victimBody = await victimResponse.Content.ReadAsStringAsync(TestContext.CancellationToken);

            Assert.DoesNotContain("€", victimBody,
                "A visitor sending no culture cookie was served the cache entry of a request that asked for a different formatting culture.");
        }
        finally
        {
            await DeleteProduct(server, productId);
        }
    }

    /// <summary>
    /// <c>ProductViewController</c> is <c>UserAgnostic</c>, so its responses are shared-cached even for a signed-in
    /// caller - and its rows are tenant filtered, so the tenant <b>must</b> be part of the cache key or one tenant's
    /// catalogue is served to another. The only thing standing between those two facts is the
    /// <c>VaryByValues["Tenant"]</c> rule in <c>AppResponseCachePolicy</c>, and nothing used to notice if it went away:
    /// every other test here reads as one tenant, so deleting that rule left the whole suite green.
    /// <para>
    /// This proves the key really splits on it, without needing a second tenant: an anonymous read (whose principal
    /// carries no tenant claim, so no Tenant value is added) fills the cache, the row is then deleted behind the app's
    /// back, and a read carrying the tenant member's token must MISS - it can only be served from the anonymous entry if
    /// the two keys are identical.
    /// </para>
    /// </summary>
    [TestMethod]
    public async Task OutputCache_Should_KeyProductResponses_ByTenant()
    {
        await using var server = new AppTestServer();

        await server.Build(
            configureTestServices: services => services.AddIntegrationApiOnlyTestsServices().FakeExternalStatistics(),
            configureTestConfigurations: configuration => configuration["ResponseCaching:EnableOutputCaching"] = "true")
            .Start(TestContext.CancellationToken);

        var marker = Guid.NewGuid().ToString("N");
        var productName = $"tenant-keyed-product-{marker}";
        var (productId, productShortId) = await CreateProduct(server, productName, $"description-{marker}");

        try
        {
            // One bare HttpClient for both reads, so the ONLY difference between the two requests is the token: any other
            // difference (a header the DI client adds, for instance) would split the cache key on its own and the test
            // would pass for the wrong reason.
            using var visitorHttpClient = new HttpClient { BaseAddress = server.WebAppServerAddress };

            var requestPath = $"/api/v1/ProductView/Get/{productShortId}";

            using var anonymousResponse = await visitorHttpClient.GetAsync(requestPath, TestContext.CancellationToken);
            anonymousResponse.EnsureSuccessStatusCode();
            Assert.Contains(productName, await anonymousResponse.Content.ReadAsStringAsync(TestContext.CancellationToken));

            Assert.IsTrue(anonymousResponse.Headers.TryGetValues("App-Cache-Response", out var cacheDecision));
            Assert.DoesNotContain("Output:-1", string.Concat(cacheDecision!), "Output caching has to be active, or there is no entry to share.");

            await DeleteProduct(server, productId);

            // The control: the anonymous caller still sees the deleted product, which is what proves there IS a cache
            // entry for the assertion below to be able to hit.
            using var cachedAnonymousResponse = await visitorHttpClient.GetAsync(requestPath, TestContext.CancellationToken);
            cachedAnonymousResponse.EnsureSuccessStatusCode();
            Assert.Contains(productName, await cachedAnonymousResponse.Content.ReadAsStringAsync(TestContext.CancellationToken),
                "The anonymous read was not served from the output cache, so this test cannot detect a shared entry.");

            await using var tenantUserScope = server.WebApp.Services.CreateAsyncScope();
            await SignIn(tenantUserScope, TenantUserEmail);
            var accessToken = await tenantUserScope.ServiceProvider.GetRequiredService<IStorageService>().GetItem("access_token");
            Assert.IsFalse(string.IsNullOrEmpty(accessToken), "Signing in did not store an access token, so the read below would not be authenticated.");

            using var tenantMemberRequest = new HttpRequestMessage(HttpMethod.Get, requestPath);
            tenantMemberRequest.Headers.Authorization = new("Bearer", accessToken);
            using var tenantMemberResponse = await visitorHttpClient.SendAsync(tenantMemberRequest, TestContext.CancellationToken);

            Assert.AreEqual(HttpStatusCode.NotFound, tenantMemberResponse.StatusCode,
                "A caller whose tenant comes from a claim was served the cache entry of a caller whose tenant came from the host, " +
                "so the tenant is not part of the output cache key.");
        }
        finally
        {
            await DeleteProduct(server, productId);
        }
    }

    /// <summary>
    /// Inserts a product for the default fallback tenant. <c>IgnoreQueryFilters</c> is required for the reads because
    /// this bare DB scope has no HttpContext, so the tenant-aware row level security query filter has no current tenant
    /// to resolve and would otherwise throw (See <c>TenantProvider.GetCurrentTenantId</c>). The insert assigns the
    /// tenant explicitly for the same reason (See <c>AppDbContext.OnSavingChanges</c>).
    /// </summary>
    private async Task<(Guid Id, int ShortId)> CreateProduct(AppTestServer server, string name, string description)
    {
        await using var scope = server.WebApp.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var categoryId = await dbContext.Products
            .IgnoreQueryFilters()
            .Where(p => p.TenantId == TenantConfiguration.FallbackTenantId)
            .Select(p => p.CategoryId)
            .FirstAsync(TestContext.CancellationToken);

        var product = new Product
        {
            Id = Guid.NewGuid(),
            // ShortId is unique across the whole table and the seed already occupies a low range, so stay well above it
            // to survive other tests inserting their own products in parallel.
            ShortId = Random.Shared.Next(1_000_000, int.MaxValue),
            Name = name,
            Price = 12_345M,
            CategoryId = categoryId,
            CreatedOn = DateTimeOffset.UtcNow,
            DescriptionText = description,
            DescriptionHTML = $"<p>{description}</p>",
            TenantId = TenantConfiguration.FallbackTenantId
        };

        await dbContext.Products.AddAsync(product, TestContext.CancellationToken);
        await dbContext.SaveChangesAsync(TestContext.CancellationToken);

        return (product.Id, product.ShortId);
    }

    private async Task DeleteProduct(AppTestServer server, Guid productId)
    {
        await using var scope = server.WebApp.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await dbContext.Products
            .IgnoreQueryFilters()
            .Where(p => p.Id == productId)
            .ExecuteDeleteAsync(TestContext.CancellationToken);
    }

    /// <summary>
    /// Signs the given e-mail in within <paramref name="scope"/> and returns her resulting claims. The access token
    /// lands in that scope's (in-memory) TestStorageService, so every typed API client resolved from the same scope
    /// calls the server as her.
    /// </summary>
    private async Task<ClaimsPrincipal> SignIn(AsyncServiceScope scope, string email)
    {
        var authManager = scope.ServiceProvider.GetRequiredService<AuthManager>();

        var requiresTwoFactor = await authManager.SignIn(new()
        {
            Email = email,
            Password = Password
        }, TestContext.CancellationToken);

        Assert.IsFalse(requiresTwoFactor, $"'{email}' is not expected to have two factor authentication enabled.");

        return (await authManager.GetAuthenticationStateAsync()).User;
    }

    /// <summary>
    /// Runs <c>ResponseCacheService.PurgeProductCache</c>, which needs an HttpContext of its own to decide whether the
    /// request came through a CDN, and there is none in a bare DI scope (See <c>ResponseCacheService.PurgeCache</c>).
    /// </summary>
    private async Task PurgeProductCache(AsyncServiceScope scope, int productShortId)
    {
        var httpContextAccessor = scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>();
        httpContextAccessor.HttpContext ??= new DefaultHttpContext { RequestServices = scope.ServiceProvider };

        var responseCacheService = scope.ServiceProvider.GetRequiredService<ResponseCacheService>();

        await responseCacheService.PurgeProductCache(productShortId);
    }

    /// <summary>
    /// Fetches the pre-rendered public product page. A raw HttpClient is used rather than Playwright so that what gets
    /// asserted is the exact HTML the server produced (or replayed from the output cache), with no client-side
    /// re-rendering on top of it.
    /// </summary>
    private async Task<string> GetProductPage(HttpClient httpClient, string pageUrl, bool assertOutputCachingIsActive = false)
    {
        using var response = await httpClient.GetAsync(pageUrl, TestContext.CancellationToken);

        if (assertOutputCachingIsActive)
        {
            // AppResponseCachePolicy reports what it decided for this request; Output:-1 would mean the page never
            // reached the output cache, making the rest of this test meaningless.
            Assert.IsTrue(response.Headers.TryGetValues("App-Cache-Response", out var appCacheResponse));
            Assert.DoesNotContain("Output:-1", string.Concat(appCacheResponse!),
                $"Output caching should be active for the pre-rendered product page at '{pageUrl}'.");
        }

        return await response.Content.ReadAsStringAsync(TestContext.CancellationToken);
    }
}
