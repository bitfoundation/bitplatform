//+:cnd:noEmit
using Microsoft.EntityFrameworkCore;
using Boilerplate.Shared.Features.Products;
using Boilerplate.Server.Api.Features.Tenants;
using Boilerplate.Server.Api.Features.Products;
using Boilerplate.Server.Api.Infrastructure.Data;
using Boilerplate.Server.Api.Infrastructure.Services;
using Boilerplate.Client.Core.Infrastructure.Services;

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
    /// <item>A product is inserted straight into the database for the default fallback tenant, then read twice: once as
    /// the signed-in tenant-user through <c>ProductViewController.Get</c> (which is <c>UserAgnostic</c>, so it is cached
    /// even for an authenticated caller, keyed by her tenant - See <c>AppResponseCachePolicy</c>), and once anonymously
    /// as the pre-rendered <c>/product/{shortId}</c> page. Both responses are now in the output cache.</item>
    /// <item>The tenant-admin - the member holding <c>ProductCatalog_Manage</c> for that tenant - edits the description
    /// through the real <c>ProductController.Update</c> endpoint, which purges the product right after saving. Both
    /// readers immediately see the new description, which proves the tags <c>AppResponseCachePolicy</c> writes match the
    /// paths <c>ResponseCacheService.PurgeProductCache</c> evicts.</item>
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

        var pageHtml = await GetProductPage(visitorHttpClient, productShortId, assertOutputCachingIsActive: true);
        Assert.Contains(productName, pageHtml);
        Assert.Contains(originalDescription, pageHtml);

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

        // Both readers see the new description straight away, so the tags AppResponseCachePolicy writes for these two
        // requests really are the ones ResponseCacheService.PurgeProductCache evicts.
        var seenAfterPurge = await tenantUserProductView.Get(productShortId, TestContext.CancellationToken);
        Assert.AreEqual(updatedDescription, seenAfterPurge.DescriptionText);

        pageHtml = await GetProductPage(visitorHttpClient, productShortId);
        Assert.Contains(updatedDescription, pageHtml);
        Assert.DoesNotContain(originalDescription, pageHtml);

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

        pageHtml = await GetProductPage(visitorHttpClient, productShortId);
        Assert.Contains(productName, pageHtml);
        Assert.Contains(updatedDescription, pageHtml);

        // ---- Step 4: purging makes the deletion visible, which rules out anything but the cache in step 3 ----

        // The row is gone, so ProductController.Delete would 404 before reaching its purge; this control step calls the
        // shared purge service directly instead.
        await using (var scope = server.WebApp.Services.CreateAsyncScope())
        {
            await PurgeProductCache(scope, productShortId);
        }

        await Assert.ThrowsExactlyAsync<ResourceNotFoundException>(
            () => tenantUserProductView.Get(productShortId, TestContext.CancellationToken));

        pageHtml = await GetProductPage(visitorHttpClient, productShortId);
        Assert.DoesNotContain(productName, pageHtml);
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
    private async Task<string> GetProductPage(HttpClient httpClient, int productShortId, bool assertOutputCachingIsActive = false)
    {
        using var response = await httpClient.GetAsync($"{PageUrls.Product}/{productShortId}", TestContext.CancellationToken);

        if (assertOutputCachingIsActive)
        {
            // AppResponseCachePolicy reports what it decided for this request; Output:-1 would mean the page never
            // reached the output cache, making the rest of this test meaningless.
            Assert.IsTrue(response.Headers.TryGetValues("App-Cache-Response", out var appCacheResponse));
            Assert.DoesNotContain("Output:-1", string.Concat(appCacheResponse!),
                "Output caching should be active for the pre-rendered product page.");
        }

        return await response.Content.ReadAsStringAsync(TestContext.CancellationToken);
    }
}
