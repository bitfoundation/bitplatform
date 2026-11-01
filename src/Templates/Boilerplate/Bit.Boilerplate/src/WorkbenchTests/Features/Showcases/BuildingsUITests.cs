namespace Boilerplate.WorkbenchTests.Features.Showcases;

/// <summary>The Buildings showcase in a browser: the grid that loads more rows as it scrolls, its search, sort and filter, the map and the editor.</summary>
[TestClass, TestCategory("UITest")]
public class BuildingsUITests : WorkbenchPageTest
{
    private static readonly int seedCount = BuildingSeed.Create().Count;

    private ILocator Rows => Page.Locator(".buildings-grid .bit-dtg-body .name-cell");

    private ILocator MapMarkers => Page.Locator(".bit-map-marker-table tbody tr");

    private ILocator Subtitle(int buildings, int provinces) => Page.GetByText(string.Format(CultureInfo.InvariantCulture, WorkbenchStrings.BuildingsSubtitle, buildings, provinces));

    [TestMethod]
    public async Task TheGrid_Should_LoadMoreRowsAsItScrolls()
    {
        await OpenPreview("buildings", "?device=desktop-1536&sw-amount=many");

        await Expect(Subtitle(seedCount * 12, 12)).ToBeVisibleAsync();
        await Expect(Rows).ToHaveCountAsync(20);

        var loaded = 20;

        for (var i = 0; i < 3; i++)
        {
            await Rows.Last.ScrollIntoViewIfNeededAsync();
            loaded = await WorkbenchTestReviewer.WaitFor(() => Rows.CountAsync(), count => count > loaded);
        }

        Assert.IsGreaterThanOrEqualTo(80, loaded);
        Assert.IsLessThan(seedCount * 12, loaded, "Rows load in batches, not all at once.");
    }

    [TestMethod]
    public async Task Search_Should_FilterTheGridAndTheMapTogether()
    {
        await OpenPreview("buildings", "?device=desktop-1536");

        await Expect(MapMarkers).ToHaveCountAsync(seedCount);

        await Page.GetByRole(AriaRole.Searchbox, new() { Name = WorkbenchStrings.BuildingsSearch }).FillAsync("Rotterdam");

        await Expect(Subtitle(4, 1)).ToBeVisibleAsync();
        await Expect(Rows).ToHaveCountAsync(4);
        await Expect(MapMarkers).ToHaveCountAsync(4);
        await Expect(Rows).ToContainTextAsync(["Euromast", "De Rotterdam", "Maastoren", "Markthal"]);
    }

    [TestMethod]
    public async Task TheSearch_Should_StayAfterAReload()
    {
        await OpenPreview("buildings", "?device=desktop-1536");

        await Page.GetByRole(AriaRole.Searchbox, new() { Name = WorkbenchStrings.BuildingsSearch }).FillAsync("Rotterdam");
        await Expect(Rows).ToHaveCountAsync(4);

        await Page.ReloadAsync();
        await Page.WaitForFunctionAsync("() => window.__workbenchReady === true");

        await Expect(Page.GetByRole(AriaRole.Searchbox, new() { Name = WorkbenchStrings.BuildingsSearch })).ToHaveValueAsync("Rotterdam");
        await Expect(Rows).ToHaveCountAsync(4);
    }

    [TestMethod]
    public async Task SortingByYear_Should_PutTheOldestFirstThenTheNewest()
    {
        await OpenPreview("buildings", "?device=desktop-1536");

        var year = Page.Locator(".buildings-grid button.bit-dtg-htext").Filter(new() { HasText = WorkbenchStrings.BuildingYear });

        await year.ClickAsync();

        await Expect(Rows.First).ToContainTextAsync("Basilica of Saint Servatius");
        await Expect(Page.Locator(".buildings-grid [role='columnheader'][aria-sort='ascending']")).ToHaveCountAsync(1);

        await year.ClickAsync();

        await Expect(Rows.First).ToContainTextAsync("Atlas Theater");
        await Expect(Page.Locator(".buildings-grid [role='columnheader'][aria-sort='descending']")).ToHaveCountAsync(1);
    }

    [TestMethod]
    public async Task TheFilterRow_Should_FilterByName()
    {
        await OpenPreview("buildings", "?device=desktop-1536");

        await Page.Locator(".buildings-grid .bit-dtg-filter-row input").First.FillAsync("toren");

        await Expect(Rows).Not.ToHaveCountAsync(20);

        var names = await Rows.Locator(".name").AllTextContentsAsync();

        Assert.IsNotEmpty(names);
        Assert.IsTrue(names.All(name => name.Contains("toren", StringComparison.OrdinalIgnoreCase)), string.Join(", ", names));
    }

    [TestMethod]
    public async Task AnAdmin_Should_AddABuildingThatShowsInTheGridAndOnTheMap()
    {
        await OpenPreview("buildings", "?device=desktop-1536");

        await Page.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.BuildingsAdd }).ClickAsync();

        await Expect(Page.GetByText(WorkbenchStrings.BuildingsAddTitle)).ToBeVisibleAsync();

        await Page.GetByLabel(WorkbenchStrings.BuildingName, new() { Exact = true }).FillAsync("Kröller-Müller Museum");
        await Page.GetByLabel(WorkbenchStrings.BuildingCity, new() { Exact = true }).FillAsync("Otterlo");
        await Page.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.Save, Exact = true }).ClickAsync();

        await Expect(Page.GetByText(string.Format(CultureInfo.InvariantCulture, WorkbenchStrings.BuildingsSaved, "Kröller-Müller Museum")).First).ToBeVisibleAsync();
        await Expect(Subtitle(seedCount + 1, 12)).ToBeVisibleAsync();
        await Expect(MapMarkers).ToHaveCountAsync(seedCount + 1);
    }

    [TestMethod]
    public async Task TheEditor_Should_RefuseABuildingWithoutANameWithAWrongYearOrOutsideTheCountry()
    {
        await OpenPreview("buildings", "?device=desktop-1536");

        await Page.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.BuildingsAdd }).ClickAsync();
        await Page.GetByLabel(WorkbenchStrings.BuildingName, new() { Exact = true }).FillAsync("   ");
        await Page.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.Save, Exact = true }).ClickAsync();

        await Expect(Page.GetByText(WorkbenchStrings.BuildingsNameRequired)).ToBeVisibleAsync();
        await Expect(Page.GetByText(WorkbenchStrings.BuildingsCityRequired)).ToBeVisibleAsync();
        await Expect(Page.GetByText(WorkbenchStrings.BuildingsAddTitle)).ToBeVisibleAsync();

        await Page.GetByLabel(WorkbenchStrings.BuildingName, new() { Exact = true }).FillAsync("Cologne Cathedral");
        await Page.GetByLabel(WorkbenchStrings.BuildingCity, new() { Exact = true }).FillAsync("Cologne");
        await Page.GetByLabel(WorkbenchStrings.BuildingYear, new() { Exact = true }).FillAsync("500");
        await Page.GetByLabel(WorkbenchStrings.BuildingLatitude, new() { Exact = true }).FillAsync("50.9413");
        await Page.GetByLabel(WorkbenchStrings.BuildingLongitude, new() { Exact = true }).FillAsync("6.9583");
        await Page.GetByLabel(WorkbenchStrings.BuildingLongitude, new() { Exact = true }).PressAsync("Tab");
        await Page.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.Save, Exact = true }).ClickAsync();

        await Expect(Page.GetByText(WorkbenchStrings.BuildingsYearInvalid)).ToBeVisibleAsync();
        await Expect(Page.Locator(".editor-modal").GetByText(WorkbenchStrings.BuildingsOutsideBody)).ToBeVisibleAsync();
        await Expect(Subtitle(seedCount, 12)).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task DeletingABuilding_Should_AskFirst()
    {
        await OpenPreview("buildings", "?device=desktop-1536&sw-amount=few");

        await Expect(Rows).ToHaveCountAsync(3);

        var first = (await Rows.First.Locator(".name").TextContentAsync())!;

        await Page.Locator(".buildings-grid .bit-dtg-body").GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.BuildingsDelete }).First.ClickAsync();

        var dialog = Page.Locator("[role='dialog'], [role='alertdialog']").Filter(new() { HasText = WorkbenchStrings.BuildingsDeleteTitle });

        await Expect(dialog).ToContainTextAsync(string.Format(CultureInfo.InvariantCulture, WorkbenchStrings.BuildingsDeleteBody, first));

        await dialog.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("^ok$", RegexOptions.IgnoreCase) }).ClickAsync();

        await Expect(Page.GetByText(string.Format(CultureInfo.InvariantCulture, WorkbenchStrings.BuildingsDeleted, first)).First).ToBeVisibleAsync();
        await Expect(Rows).ToHaveCountAsync(2);
    }

    [TestMethod]
    public async Task ADemoUser_Should_NotBeOfferedToChangeTheBuildings()
    {
        await OpenPreview("buildings", "?device=desktop-1536&user=2");

        await Expect(Rows).ToHaveCountAsync(20);
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.BuildingsAdd })).ToHaveCountAsync(0);
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.BuildingsDelete })).ToHaveCountAsync(0);
        await Expect(Page.GetByText(WorkbenchStrings.BuildingsMapHint)).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task OnAPhone_Should_ShowTheListOrTheMap()
    {
        await Page.SetViewportSizeAsync(402, 874);
        await OpenPreview("buildings");

        var group = Page.GetByRole(AriaRole.Group, new() { Name = WorkbenchStrings.BuildingsViewSwitch });

        await Expect(group.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.BuildingsListView })).ToHaveAttributeAsync("aria-pressed", "true");
        await Expect(Page.Locator(".split.show-list")).ToBeVisibleAsync();

        await group.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.BuildingsMapView }).ClickAsync();

        await Expect(Page.Locator(".split.show-map")).ToBeVisibleAsync();
        await Expect(group.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.BuildingsMapView })).ToHaveAttributeAsync("aria-pressed", "true");
    }

    [TestMethod]
    public async Task LongNames_Should_ShowInTheGrid()
    {
        await OpenPreview("buildings", "?device=desktop-1536&sw-names=long");

        await Expect(Rows.First).ToContainTextAsync("Visitor Centre, Archive and Research Library of the Historic Building");
    }
}
