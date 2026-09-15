namespace Boilerplate.Tests.E2E.Features.Categories;

/// <summary>
/// An edited but unsaved category holds the user on the categories page: AddOrEditCategoryModal's NavigationLock
/// refuses every navigation while its form is modified, and its BitModal blocks the page around it. Only the admin
/// panel has categories, and <see cref="StoreAdmin"/>'s tenant owns its catalogue. The Web, Windows and Android classes
/// deriving from this decide what "leaving" is.
/// </summary>
public abstract class UnsavedCategoryTestsBase : AppTestBase
{
    /// <summary>Marks the edit, so a run that saved it after all can take it off again.</summary>
    private const string unsavedSuffix = " (e2e unsaved)";

    /// <summary>The platform's own way back.</summary>
    protected abstract Task GoBack(IPage page);

    [TestMethod]
    [DataRow(App.AdminPanel, DisplayName = nameof(App.AdminPanel))]
    public virtual async Task LeavingWithAnUnsavedCategoryName_Should_BeRefused(App app)
    {
        await SkipWithoutGlobalAdminCredentials();

        RegisterForCleanup(RestoreCategoryNames);

        var page = await OpenApp(app);
        await WaitUntilInteractive(page);
        await SignInStoreAdminInOpenApp(page);

        await GoToWhenInteractive(page, PageUrls.Categories);

        // By title, like the other icon-only buttons the tests click (See AiChatPanelTestBase).
        await page.GetByTitle(AppStrings.Edit, new() { Exact = true }).First.ClickAsync();

        var nameBox = page.GetByPlaceholder(AppStrings.EnterCategoryName);
        await Expect(nameBox).Not.ToHaveValueAsync("");

        // Typed and left at once, as a user would: nothing moves the focus out of the field first.
        var editedName = $"{await nameBox.InputValueAsync()}{unsavedSuffix}";
        await nameBox.FillEnsuringStable(editedName);

        await GoBack(page);

        // Refused is nothing happening, which the assertions below would confirm at once; the app gets time to act first.
        await Task.Delay(TimeSpan.FromSeconds(3));

        await Expect(page).ToHaveURLAsync(new Regex($"{Regex.Escape(PageUrls.Categories)}/?$", RegexOptions.IgnoreCase));
        await Expect(page.GetByText(AppStrings.EditCategory, new() { Exact = true })).ToBeVisibleAsync();
        await Expect(nameBox).ToHaveValueAsync(editedName);
    }

    /// <summary>Not on the test's token: a canceled or timed out test still owes the deployment its cleanup.</summary>
    private static async Task RestoreCategoryNames()
    {
        var globalApiClient = await DeployedApiClientProvider.GetGlobalApiClient(CancellationToken.None);
        await using var dbContext = await globalApiClient.DbContextFactory!.CreateDbContextAsync(CancellationToken.None);

        await dbContext.Categories.IgnoreQueryFilters()
            .Where(c => c.Name!.EndsWith(unsavedSuffix))
            .ExecuteUpdateAsync(setters => setters.SetProperty(c => c.Name, c => c.Name!.Substring(0, c.Name.Length - unsavedSuffix.Length)), CancellationToken.None);
    }
}
