using Boilerplate.Server.Api.Features.Tenants;
using Boilerplate.Tests.E2E.Infrastructure.Services;

namespace Boilerplate.Tests.E2E.Features.Concurrency;

/// <summary>
/// Optimistic concurrency end to end: every edit carries the <c>Version</c> it was loaded with, and
/// <c>AppDbContext.OnSavingChanges</c> matches the row on that value rather than on the one it just read, so on
/// PostgreSQL (<c>xmin</c>) a save based on a superseded version is refused instead of silently overwriting.
/// <para>
/// Not parallelized: it edits a real product of the store tenant, and leaves it as it found it.
/// </para>
/// </summary>
[TestClass, TestCategory(TestCategories.Web), Retry(2), DoNotParallelize]
public partial class WebProductConcurrencyTests : AppTestBase
{
    protected override IAppOpener AppOpener => new WebAppOpener();

    /// <summary>
    /// Two browsers of the store's admin open the same product. The first adds a dot to the alt text and saves; the
    /// second, still holding the version both loaded, does the same and is refused. Reloaded, it shows the first
    /// admin's dot - the other person's edit - and removing it saves fine, which also puts the product back.
    /// </summary>
    [TestMethod]
    public async Task StaleEdit_Should_BeRefusedWithAConcurrencyError_AndSaveOnceReloaded()
    {
        await SkipWithoutGlobalAdminCredentials();

        var globalApiClient = await DeployedApiClientProvider.GetGlobalApiClient(TestContext.CancellationToken);
        await using var dbContext = await globalApiClient.DbContextFactory!.CreateDbContextAsync(TestContext.CancellationToken);

        // The alt text field only exists for a product that has a picture. The last such one of the store, as the
        // attachment test takes whichever the database lists first.
        var product = await dbContext.Products.IgnoreQueryFilters()
            .Where(p => p.TenantId == TenantConfiguration.FallbackTenantId && p.HasPrimaryImage)
            .OrderByDescending(p => p.ShortId)
            .Select(p => new { p.Id, p.PrimaryImageAltText })
            .FirstAsync(TestContext.CancellationToken);

        var originalAltText = product.PrimaryImageAltText ?? string.Empty;

        // Only for a run that fails half way: the journey below ends where it started, through the app.
        RegisterForCleanup(() => RestoreAltText(product.Id, product.PrimaryImageAltText));

        var firstAdminPage = Page;
        await SignInStoreAdmin(firstAdminPage);

        await using var secondAdminContext = await NewBrowserContext(Browser);
        var secondAdminPage = await secondAdminContext.NewPageAsync();
        await SignInStoreAdmin(secondAdminPage);

        // Both load the product before either saves, so both hold the same Version.
        var firstAltText = await OpenEditor(firstAdminPage, product.Id);
        var secondAltText = await OpenEditor(secondAdminPage, product.Id);

        await Expect(firstAltText).ToHaveValueAsync(originalAltText);
        await Expect(secondAltText).ToHaveValueAsync(originalAltText);

        await firstAltText.FillEnsuringStable($"{originalAltText}.");
        await SaveAndExpectSuccess(firstAdminPage);

        // Not the same dot: a stale save whose values equal the row's changes no column, so EF sends no UPDATE - and with
        // it no WHERE on the version - and it "succeeds" as the no-op it is.
        await secondAltText.FillEnsuringStable($"{originalAltText},");
        await secondAdminPage.GetByRole(AriaRole.Button, new() { Name = AppStrings.Save, Exact = true }).ClickAsync();

        // ConflictException is an interrupting one: a message box, and the editor stays where it was.
        await Expect(secondAdminPage.GetByText(AppStrings.UpdateConcurrencyException)).ToBeVisibleAsync();
        await Expect(secondAdminPage).ToHaveURLAsync(new Regex(Regex.Escape($"{PageUrls.AddOrEditProduct}/{product.Id}"), RegexOptions.IgnoreCase));

        // Reloaded, the editor carries the first admin's save - and its version.
        await secondAdminPage.ReloadAsync();
        await WaitUntilInteractive(secondAdminPage);
        secondAltText = AltTextField(secondAdminPage);
        await Expect(secondAltText).ToHaveValueAsync($"{originalAltText}.");

        await secondAltText.FillEnsuringStable(originalAltText);
        await SaveAndExpectSuccess(secondAdminPage);

        var savedAltText = await dbContext.Products.IgnoreQueryFilters()
            .Where(p => p.Id == product.Id)
            .Select(p => p.PrimaryImageAltText)
            .SingleAsync(TestContext.CancellationToken);

        Assert.AreEqual(originalAltText, savedAltText ?? string.Empty, "The reloaded edit is the one that has to stick.");
    }

    private static ILocator AltTextField(IPage page) => page.GetByLabel(AppStrings.AltText, new() { Exact = true });

    private async Task<ILocator> OpenEditor(IPage page, Guid productId)
    {
        await page.GoToInApp($"{PageUrls.AddOrEditProduct}/{productId}");

        var altText = AltTextField(page);
        await Expect(altText).ToBeVisibleAsync();

        return altText;
    }

    /// <summary>Save goes back to the list only once ProductController.Update has returned.</summary>
    private async Task SaveAndExpectSuccess(IPage page)
    {
        await page.GetByRole(AriaRole.Button, new() { Name = AppStrings.Save, Exact = true }).ClickAsync();

        await Expect(page).ToHaveURLAsync(new Regex($"{Regex.Escape(PageUrls.Products)}/?$"));
    }

    private static async Task RestoreAltText(Guid productId, string? altText)
    {
        var globalApiClient = await DeployedApiClientProvider.GetGlobalApiClient(CancellationToken.None);
        await using var dbContext = await globalApiClient.DbContextFactory!.CreateDbContextAsync(CancellationToken.None);

        await dbContext.Products.IgnoreQueryFilters()
            .Where(p => p.Id == productId && p.PrimaryImageAltText != altText)
            .ExecuteUpdateAsync(setters => setters.SetProperty(p => p.PrimaryImageAltText, altText), CancellationToken.None);
    }
}
