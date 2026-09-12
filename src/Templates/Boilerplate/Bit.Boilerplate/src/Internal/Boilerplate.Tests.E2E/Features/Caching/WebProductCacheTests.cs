using Boilerplate.Server.Api.Features.Tenants;
using Boilerplate.Tests.E2E.Infrastructure.Services;

namespace Boilerplate.Tests.E2E.Features.Caching;

/// <summary>
/// The Sales product page through the caches between its visitors and the database. The deployment has output caching
/// off, so Cloudflare's edge (s-maxage) is the shared one; behind it are the browser's http cache (max-age) and, in a
/// running app, CacheDelegatingHandler's memory cache.
/// <para>
/// Every assertion is made by a new visitor - a browser context of its own, with no http cache and no service worker.
/// A returning visitor proves nothing about the edge: bswup's worker answers her navigations itself, with the app shell,
/// and the booted app fetches the product on its own.
/// </para>
/// <para>
/// Not parallelized: it edits a real product of the store tenant, and puts it back.
/// </para>
/// </summary>
[TestClass, TestCategory(TestCategories.Web), Retry(2), DoNotParallelize]
public partial class WebProductCacheTests : AppTestBase
{
    protected override IAppOpener AppOpener => new WebAppOpener();

    /// <summary>
    /// A change behind the app's back stays invisible, since nothing purged the edge; the same kind of change made
    /// through the app reaches the next visitor, since its save purges the product.
    /// </summary>
    [TestMethod]
    public async Task ProductPage_Should_ComeFromTheEdge_UntilAnEditThroughTheAppPurgesIt()
    {
        await SkipWithoutGlobalAdminCredentials();

        var globalApiClient = await DeployedApiClientProvider.GetGlobalApiClient(TestContext.CancellationToken);
        await using var dbContext = await globalApiClient.DbContextFactory!.CreateDbContextAsync(TestContext.CancellationToken);

        // The last one of the store's catalogue: the attachment test takes the first product that has a picture.
        var product = await dbContext.Products.IgnoreQueryFilters()
            .Where(p => p.TenantId == TenantConfiguration.FallbackTenantId)
            .OrderByDescending(p => p.ShortId)
            .Select(p => new { p.Id, p.ShortId, Name = p.Name!, p.DescriptionHTML, p.DescriptionText })
            .FirstAsync(TestContext.CancellationToken);

        var marker = $"e2e{Guid.NewGuid():N}"[..16];
        var pageUrl = new Uri(new Uri(DeployedApps.Sales), $"/en-US{PageUrls.Product}/{product.ShortId}").ToString();

        // Registered before the write, so a failure anywhere below still puts the description back.
        RegisterForCleanup(() => RestoreDescription(product.Id, product.DescriptionHTML, product.DescriptionText));

        // ---- 1. A visitor opens the page, so the edge holds the page and the product the app fetches ----
        var returningVisitor = Page;
        await OpenProductPage(returningVisitor, pageUrl);
        await ExpectProduct(returningVisitor, product.Name);

        // ---- 2. The description changes in the database, bypassing the app and so its purge ----
        await dbContext.Products.IgnoreQueryFilters()
            .Where(p => p.Id == product.Id)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(p => p.DescriptionHTML, p => p.DescriptionHTML + "<p>!" + marker + "</p>")
                .SetProperty(p => p.DescriptionText, p => p.DescriptionText + " !" + marker), TestContext.CancellationToken);

        await AsANewVisitor(pageUrl, page => ExpectProduct(page, product.Name, notShowing: marker));

        // Put back before the edit below: the edit form loads the product from the database and would save the marker too.
        await RestoreDescription(product.Id, product.DescriptionHTML, product.DescriptionText);

        // ---- 3. The store's admin renames the product in AdminPanel, whose save purges it ----
        var renamed = $"{product.Name} {marker}";
        var isRenamed = false;

        await using var adminContext = await NewBrowserContext(Browser);
        var adminPage = await adminContext.NewPageAsync();

        try
        {
            await SignInStoreAdmin(adminPage);

            await RenameProduct(adminPage, product.Id, renamed);
            isRenamed = true;

            // ---- 4. The next visitor gets the edit: the edge entry is gone ----
            await ExpectANewVisitorToSee(pageUrl, renamed);

            // Logged, not asserted: see the class summary. What the returning visitor is shown is bswup's and the
            // browser's call, not the edge's.
            await OpenProductPage(returningVisitor, pageUrl);
            await returningVisitor.WaitForLoadStateAsync(LoadState.NetworkIdle);
            TestContext.WriteLine($"After the edit, the returning visitor sees the {(await returningVisitor.GetByText(marker).CountAsync() > 0 ? "edited" : "old")} name.");
        }
        finally
        {
            // Through the app, so the purge clears the renamed page from the edge too.
            if (isRenamed)
                await RenameProduct(adminPage, product.Id, product.Name);
        }

        await ExpectANewVisitorToSee(pageUrl, product.Name);
    }

    private async Task AsANewVisitor(string pageUrl, Func<IPage, Task> assert)
    {
        await using var context = await NewBrowserContext(Browser);
        var page = await context.NewPageAsync();

        await OpenProductPage(page, pageUrl);
        await assert(page);
    }

    /// <summary>
    /// The edge purge is a background job: it lands in seconds, or after its retries when Cloudflare rate limits it. So
    /// new visitors are tried until the deadline - each in a context of its own, so no attempt is answered by the cache
    /// of the one before.
    /// </summary>
    private async Task ExpectANewVisitorToSee(string pageUrl, string productName)
    {
        // Past the job's first two retries (15 s, then 60 s).
        var deadline = DateTimeOffset.UtcNow + TimeSpan.FromMinutes(2);

        while (true)
        {
            try
            {
                await AsANewVisitor(pageUrl, page => Expect(page.GetByText(productName, new() { Exact = true }).First).ToBeVisibleAsync(new() { Timeout = 5_000 }));
                return;
            }
            catch (PlaywrightException) when (DateTimeOffset.UtcNow < deadline)
            {
            }
        }
    }

    /// <summary>A real document load, with what the edge said about it on the log.</summary>
    private async Task OpenProductPage(IPage page, string pageUrl)
    {
        var response = await page.GotoAsync(pageUrl);

        var headers = response is null ? null : await response.AllHeadersAsync();
        TestContext.WriteLine($"{pageUrl}: {response?.Status} cf-cache-status={headers?.GetValueOrDefault("cf-cache-status")} age={headers?.GetValueOrDefault("age")} cache-control={headers?.GetValueOrDefault("cache-control")}");

        await WaitUntilInteractive(page);
    }

    /// <summary>
    /// Waits for the booted app to have loaded the product itself - the prerendered html is replaced by a shimmer and
    /// then by what the api answered - before deciding what the page shows.
    /// </summary>
    private async Task ExpectProduct(IPage page, string productName, string? notShowing = null)
    {
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Expect(page.GetByRole(AriaRole.Button, new() { Name = AppStrings.Buy, Exact = true })).ToBeVisibleAsync();
        await Expect(page.GetByText(productName, new() { Exact = true }).First).ToBeVisibleAsync();

        if (notShowing is not null)
            await Expect(page.GetByText(notShowing)).ToHaveCountAsync(0);
    }

    private async Task RenameProduct(IPage page, Guid productId, string name)
    {
        await page.GoToInApp($"{PageUrls.AddOrEditProduct}/{productId}");

        var nameField = page.GetByPlaceholder(AppStrings.EnterProductName);
        await Expect(nameField).Not.ToHaveValueAsync(string.Empty);
        await nameField.FillEnsuringStable(name);

        await page.GetByRole(AriaRole.Button, new() { Name = AppStrings.Save, Exact = true }).ClickAsync();

        // Save goes back to the list once ProductController.Update has saved; the edge purge it queued follows.
        await Expect(page).ToHaveURLAsync(new Regex($"{Regex.Escape(PageUrls.Products)}/?$"));
    }

    private static async Task RestoreDescription(Guid productId, string? descriptionHtml, string? descriptionText)
    {
        var globalApiClient = await DeployedApiClientProvider.GetGlobalApiClient(CancellationToken.None);
        await using var dbContext = await globalApiClient.DbContextFactory!.CreateDbContextAsync(CancellationToken.None);

        await dbContext.Products.IgnoreQueryFilters()
            .Where(p => p.Id == productId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(p => p.DescriptionHTML, descriptionHtml)
                .SetProperty(p => p.DescriptionText, descriptionText), CancellationToken.None);
    }
}
