namespace Boilerplate.WorkbenchTests.Features.Showcases;

/// <summary>The buildings page in a view: the grid that loads more rows as it scrolls, its search, sort and filter row, the map that agrees with it, list or map on a narrow screen, the add, edit and delete flows, the users who may use them, the design options, the switches and the data states.</summary>
[TestClass, TestCategory("UITest")]
public class BuildingsUITests : ShowcaseUITest
{
    private const string PageKey = "buildings";

    private const string Wide = "?device=desktop-1536";

    private static readonly int seedCount = BuildingSeed.Create().Count;

    /// <summary>The red a selected marker's picture is drawn in.</summary>
    private static readonly Regex selectedMarkerColor = new("rgb%28229%2C%2072%2C%2077%29");

    private ILocator Grid => View().GetByRole(AriaRole.Grid, new() { Name = WorkbenchStrings.BuildingsHeading });

    private ILocator Names => Grid.Locator(".name-cell .name");

    private ILocator Row(string name) => Grid.GetByRole(AriaRole.Row).Filter(new() { Has = View().GetByText(name, new() { Exact = true }) });

    private ILocator MapMarkers => View().Locator(".bit-map-marker-table tbody tr");

    private ILocator MapPane => View().Locator(".map-pane");

    private ILocator ListPane => View().Locator(".list-pane");

    private ILocator MapPins => MapPane.Locator(".leaflet-marker-icon");

    private ILocator Search => View().GetByRole(AriaRole.Searchbox, new() { Name = WorkbenchStrings.BuildingsSearch });

    private ILocator AddBuilding => ViewButton(WorkbenchStrings.BuildingsAdd);

    private ILocator ViewSwitch => View().GetByRole(AriaRole.Group, new() { Name = WorkbenchStrings.BuildingsViewSwitch });

    private ILocator ListButton => ViewSwitch.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.BuildingsListView, Exact = true });

    private ILocator MapButton => ViewSwitch.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.BuildingsMapView, Exact = true });

    private ILocator Editor => View().Locator(".bit-mdl-ctn.editor-modal");

    private ILocator Field(string label) => Editor.GetByLabel(label, new() { Exact = true });

    private ILocator Save => Editor.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.Save, Exact = true });

    private ILocator Cancel => Editor.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.Cancel, Exact = true });

    private ILocator DeleteDialog => View().GetByRole(AriaRole.Alertdialog, new() { Name = WorkbenchStrings.BuildingsDeleteTitle });

    private ILocator Cards => View().GetByRole(AriaRole.List, new() { Name = WorkbenchStrings.BuildingsHeading }).GetByRole(AriaRole.Listitem);

    private ILocator Card(string name) => Cards.Filter(new() { Has = View().GetByText(name, new() { Exact = true }) });

    private ILocator ColumnHeader(string title) => Grid.GetByRole(AriaRole.Button, new() { Name = title, Exact = true });

    private ILocator FilterOf(string column, AriaRole role) => Grid.GetByRole(role, new() { Name = string.Format(CultureInfo.InvariantCulture, BitBlazorUIStrings.DataGridFilterByFormat, column), Exact = true });

    private ILocator Subtitle(int buildings, int provinces) => View().GetByText(string.Format(CultureInfo.InvariantCulture, WorkbenchStrings.BuildingsSubtitle, buildings, provinces));

    private static string Saved(string name) => string.Format(CultureInfo.InvariantCulture, WorkbenchStrings.BuildingsSaved, name);

    private static string Deleted(string name) => string.Format(CultureInfo.InvariantCulture, WorkbenchStrings.BuildingsDeleted, name);

    private async Task AddABuilding(string name, string city)
    {
        await Press(AddBuilding);
        await Expect(Editor.GetByText(WorkbenchStrings.BuildingsAddTitle)).ToBeVisibleAsync();
        await Field(WorkbenchStrings.BuildingName).FillAsync(name);
        await Field(WorkbenchStrings.BuildingCity).FillAsync(city);
        await Press(Save);
        await Expect(Notice(Saved(name))).ToBeVisibleAsync();
    }

    private async Task OpenEditorOf(ILocator row)
    {
        await Press(row.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.BuildingsEdit, Exact = true }));
        await Expect(Editor.GetByText(WorkbenchStrings.BuildingsEditTitle)).ToBeVisibleAsync();
    }

    private async Task AskToDelete(ILocator row)
    {
        await Press(row.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.BuildingsDelete, Exact = true }));
        await Expect(DeleteDialog).ToBeVisibleAsync();
    }

    private ILocator DialogButton(string name) => DeleteDialog.GetByRole(AriaRole.Button, new() { Name = name, Exact = true });

    /// <summary>Where the only marker on the map sits in the map's pane, which only the map's own pan and zoom change.</summary>
    private async Task<(double X, double Y)> PinPlace()
    {
        var place = await MapPins.EvaluateAsync<double[]>("""
            pin => {
                const box = pin.getBoundingClientRect();
                const map = pin.closest('.map-pane').getBoundingClientRect();
                return [box.left - map.left, box.top - map.top];
            }
            """);

        return (place[0], place[1]);
    }

    /// <summary>Waits until the map has stopped moving, as it glides on for a moment after a drag or a zoom.</summary>
    private async Task<(double X, double Y)> SettledPin()
    {
        var (last, reads) = ((double.NaN, double.NaN), 0);

        await WorkbenchTestReviewer.WaitFor(async () =>
        {
            var place = await PinPlace();

            reads = place == last ? reads + 1 : 0;
            last = place;

            return reads;
        }, count => count >= 6);

        return last;
    }

    /// <summary>Drags the map from a spot beside its marker: with the mouse on a desktop and a finger on a phone.</summary>
    private async Task PanTheMap(int byX, int byY)
    {
        var box = (await MapPane.BoundingBoxAsync())!;
        var (x, y) = (box.X + box.Width * 0.7, box.Y + box.Height * 0.3);

        if (IsPhone)
        {
            var cdp = await Page.Context.NewCDPSessionAsync(Page);

            await Touch(cdp, "touchStart", x, y);

            for (var step = 1; step <= 8; step++)
            {
                await Touch(cdp, "touchMove", x + byX * step / 8d, y + byY * step / 8d);
            }

            await Touch(cdp, "touchEnd", x + byX, y + byY);
        }
        else
        {
            await Page.Mouse.MoveAsync((float)x, (float)y);
            await Page.Mouse.DownAsync();
            await Page.Mouse.MoveAsync((float)x + byX, (float)y + byY, new() { Steps = 8 });
            await Page.Mouse.UpAsync();
        }
    }

    [TestMethod, EverySurface]
    public async Task TheGrid_Should_ShowTheBuildingsWithTheirCityAndProvince(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey);

        await Expect(View().GetByRole(AriaRole.Heading, new() { Name = WorkbenchStrings.BuildingsHeading, Level = 2 })).ToBeVisibleAsync();
        await Expect(Subtitle(seedCount, 12)).ToBeVisibleAsync();
        await Expect(Names).ToHaveCountAsync(20);
        await Expect(Names.First).ToHaveTextAsync("Rijksmuseum");
        await Expect(Row("Rijksmuseum")).ToContainTextAsync($"Amsterdam · {WorkbenchStrings.ProvinceNorthHolland}");

        await ExpectNoSidewaysScroll();
    }

    [TestMethod, EverySurface]
    public async Task TheGrid_Should_LoadMoreRowsAsItScrolls(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey, "?sw-amount=many");

        await Expect(Subtitle(seedCount * 12, 12)).ToBeVisibleAsync();
        await Expect(Names).ToHaveCountAsync(20);

        var loaded = 20;

        for (var i = 0; i < 3; i++)
        {
            await Names.Last.ScrollIntoViewIfNeededAsync();
            loaded = await WorkbenchTestReviewer.WaitFor(() => Names.CountAsync(), count => count > loaded);
        }

        Assert.IsGreaterThanOrEqualTo(80, loaded);
        Assert.IsLessThan(seedCount * 12, loaded, "Rows load in batches, not all at once.");
    }

    [TestMethod, OnDesktops]
    public async Task AWideView_Should_ShowTheGridAndTheMapSideBySide(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey, Wide);

        await Expect(ViewSwitch).ToHaveCountAsync(0);
        await Expect(ListPane).ToBeVisibleAsync();
        await Expect(MapPane).ToBeVisibleAsync();
        await Expect(ColumnHeader(WorkbenchStrings.BuildingType)).ToBeVisibleAsync();
        await Expect(ColumnHeader(WorkbenchStrings.BuildingYear)).ToBeVisibleAsync();

        var list = (await ListPane.BoundingBoxAsync())!;
        var map = (await MapPane.BoundingBoxAsync())!;

        Assert.AreEqual(list.Y, map.Y, 2);
        Assert.IsGreaterThan(list.X + list.Width - 1, map.X);

        await ExpectNoSidewaysScroll();
    }

    [TestMethod, EverySurface]
    public async Task ANarrowView_Should_ShowTheListOrTheMapAndKeepTheChoiceAfterAReload(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey);

        await Expect(ListButton).ToHaveAttributeAsync("aria-pressed", "true");
        await Expect(ListPane).ToBeVisibleAsync();
        await Expect(MapPane).Not.ToBeVisibleAsync();
        await Expect(ColumnHeader(WorkbenchStrings.BuildingType)).ToHaveCountAsync(0);
        await Expect(ColumnHeader(WorkbenchStrings.BuildingYear)).ToHaveCountAsync(0);

        await Press(MapButton);

        await Expect(MapButton).ToHaveAttributeAsync("aria-pressed", "true");
        await Expect(MapPane).ToBeVisibleAsync();
        await Expect(ListPane).Not.ToBeVisibleAsync();
        await Expect(MapPins.First).ToBeVisibleAsync();

        await ReloadTheView();

        await Expect(MapButton).ToHaveAttributeAsync("aria-pressed", "true");
        await Expect(MapPane).ToBeVisibleAsync();
        await Expect(MapPins.First).ToBeVisibleAsync();

        await Press(ListButton);

        await Expect(ListPane).ToBeVisibleAsync();
        await Expect(MapPane).Not.ToBeVisibleAsync();
        await Expect(Names.First).ToBeVisibleAsync();
    }

    [TestMethod, EverySurface]
    public async Task Search_Should_FilterTheGridAndTheMapTogether(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey);

        await Expect(MapMarkers).ToHaveCountAsync(seedCount);

        await Search.FillAsync("Rotterdam");

        await Expect(Subtitle(4, 1)).ToBeVisibleAsync();
        await Expect(Names).ToHaveTextAsync(["Euromast", "De Rotterdam", "Maastoren", "Markthal"]);
        await Expect(MapMarkers).ToHaveCountAsync(4);

        await Press(MapButton);

        await Expect(MapPins.First).ToBeVisibleAsync();
    }

    [TestMethod, EverySurface]
    public async Task Search_Should_AlsoFindAProvinceOrAType(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey);

        await Search.FillAsync(WorkbenchStrings.ProvinceZeeland);

        await Expect(Subtitle(BuildingSeed.Create().Count(building => building.Province is DutchProvince.Zeeland), 1)).ToBeVisibleAsync();

        await Search.FillAsync(WorkbenchStrings.BuildingTypeStadium);

        await Expect(Names).ToHaveCountAsync(BuildingSeed.Create().Count(building => building.Type is BuildingType.Stadium));
    }

    [TestMethod, EverySurface]
    public async Task ASearchWithoutResults_Should_LeaveTheGridEmptyUntilItIsCleared(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey);

        await Search.FillAsync("Nowhere at all");

        await Expect(Subtitle(0, 0)).ToBeVisibleAsync();
        await Expect(Names).ToHaveCountAsync(0);
        await Expect(MapMarkers).ToHaveCountAsync(0);

        await Search.FillAsync(string.Empty);

        await Expect(Subtitle(seedCount, 12)).ToBeVisibleAsync();
        await Expect(Names).ToHaveCountAsync(20);
        await Expect(MapMarkers).ToHaveCountAsync(seedCount);
    }

    [TestMethod, EverySurface]
    public async Task TheSearch_Should_StayAfterAReload(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey);

        await Search.FillAsync("Rotterdam");
        await Expect(Names).ToHaveCountAsync(4);

        await ReloadTheView();

        await Expect(Search).ToHaveValueAsync("Rotterdam");
        await Expect(Names).ToHaveCountAsync(4);
        await Expect(MapMarkers).ToHaveCountAsync(4);
    }

    [TestMethod, EverySurface]
    public async Task APressRightAfterASearch_Should_NotGetLost(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey);

        await Search.FillAsync("Rotterdam");
        await Expect(Names).ToHaveCountAsync(4);

        await Press(Row("Euromast").GetByRole(AriaRole.Rowheader));

        await Expect(Row("Euromast")).ToHaveAttributeAsync("aria-selected", "true");

        await Search.FillAsync("Euromast");
        await Expect(Names).ToHaveCountAsync(1);

        await OpenEditorOf(Row("Euromast"));

        await Expect(Field(WorkbenchStrings.BuildingName)).ToHaveValueAsync("Euromast");
    }

    [TestMethod, EverySurface]
    public async Task SortingByName_Should_PutTheFirstNameFirstThenTheLast(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey);

        var sorted = BuildingSeed.Create().Select(building => building.Name).Order(StringComparer.Create(CultureInfo.CurrentCulture, ignoreCase: false)).ToArray();

        await Press(ColumnHeader(WorkbenchStrings.BuildingName));

        await Expect(Names.First).ToHaveTextAsync(sorted[0]);
        await Expect(Grid.Locator("[role='columnheader'][aria-sort='ascending']")).ToHaveCountAsync(1);

        await Press(ColumnHeader(WorkbenchStrings.BuildingName));

        await Expect(Names.First).ToHaveTextAsync(sorted[^1]);
        await Expect(Grid.Locator("[role='columnheader'][aria-sort='descending']")).ToHaveCountAsync(1);
    }

    [TestMethod, OnDesktops]
    public async Task SortingByYear_Should_PutTheOldestFirstThenTheNewest(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey, Wide);

        await Press(ColumnHeader(WorkbenchStrings.BuildingYear));

        await Expect(Names.First).ToHaveTextAsync("Basilica of Saint Servatius");
        await Expect(Grid.Locator("[role='columnheader'][aria-sort='ascending']")).ToHaveCountAsync(1);

        await Press(ColumnHeader(WorkbenchStrings.BuildingYear));

        await Expect(Names.First).ToHaveTextAsync("Atlas Theater");
        await Expect(Grid.Locator("[role='columnheader'][aria-sort='descending']")).ToHaveCountAsync(1);
    }

    [TestMethod, OnDesktops]
    public async Task SortingByType_Should_FollowTheTypeNamesAsTheyAreShown(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey, Wide);

        await Press(ColumnHeader(WorkbenchStrings.BuildingType));

        await Expect(Grid.Locator("[role='columnheader'][aria-sort='ascending']")).ToHaveCountAsync(1);
        await Expect(Row(await Names.First.InnerTextAsync())).ToContainTextAsync(WorkbenchStrings.BuildingTypeChurch);
    }

    [TestMethod, EverySurface]
    public async Task TheFilterRow_Should_FilterByName(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey);

        await FilterOf(WorkbenchStrings.BuildingName, AriaRole.Textbox).FillAsync("toren");

        await Expect(Names).Not.ToHaveCountAsync(20);

        var names = await Names.AllTextContentsAsync();

        Assert.IsNotEmpty(names);
        Assert.IsTrue(names.All(name => name.Contains("toren", StringComparison.OrdinalIgnoreCase)), string.Join(", ", names));
    }

    [TestMethod, OnDesktops]
    public async Task TheFilterRow_Should_FilterByType(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey, Wide);

        await FilterOf(WorkbenchStrings.BuildingType, AriaRole.Combobox).SelectOptionAsync(BuildingType.Stadium.ToString());

        await Expect(Names).ToHaveCountAsync(BuildingSeed.Create().Count(building => building.Type is BuildingType.Stadium));
    }

    [TestMethod, OnDesktops]
    public async Task TheFilterRow_Should_FilterByYear(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey, Wide);

        await FilterOf(WorkbenchStrings.BuildingYear, AriaRole.Textbox).FillAsync("1885");

        await Expect(Names).ToHaveTextAsync(BuildingSeed.Create().Where(building => building.Year is 1885).Select(building => building.Name).ToArray());
    }

    [TestMethod, EverySurface]
    public async Task TheColumnsMenu_Should_HideAColumn(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey);

        await Expect(Grid.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.BuildingsDelete }).First).ToBeVisibleAsync();

        await Press(ListPane.GetByRole(AriaRole.Button, new() { Name = BitBlazorUIStrings.DataGridColumnsText, Exact = true }));
        await Press(ListPane.GetByRole(AriaRole.Checkbox, new() { Name = AppStrings.Action, Exact = true }));

        await Expect(Grid.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.BuildingsDelete })).ToHaveCountAsync(0);
    }

    [TestMethod, EverySurface]
    public async Task ExportCsv_Should_DownloadTheFilteredBuildings(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey);

        await Search.FillAsync("Rotterdam");
        await Expect(Names).ToHaveCountAsync(4);

        var download = await Page.RunAndWaitForDownloadAsync(() => Press(ListPane.GetByRole(AriaRole.Button, new() { Name = BitBlazorUIStrings.DataGridExportCsvText, Exact = true })));

        var path = await download.PathAsync();
        var text = await File.ReadAllTextAsync(path);

        Assert.Contains("Euromast", text);
        Assert.DoesNotContain("Rijksmuseum", text);
    }

    [TestMethod, EverySurface]
    public async Task ExportExcel_Should_DownloadASpreadsheet(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey);

        var download = await Page.RunAndWaitForDownloadAsync(() => Press(ListPane.GetByRole(AriaRole.Button, new() { Name = BitBlazorUIStrings.DataGridExportExcelText, Exact = true })));

        Assert.EndsWith(".xlsx", download.SuggestedFilename);
    }

    [TestMethod, EverySurface]
    public async Task SelectingARow_Should_MarkItAndKeepItAfterAReload(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey);

        await Press(Row("Westerkerk").GetByRole(AriaRole.Rowheader));

        await Expect(Row("Westerkerk")).ToHaveAttributeAsync("aria-selected", "true");

        await ReloadTheView();

        await Expect(Row("Westerkerk")).ToHaveAttributeAsync("aria-selected", "true");
    }

    [TestMethod, OnDesktops]
    public async Task SelectingARow_Should_FlyTheMapToItsMarkerInRed(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey, Wide);

        await Press(Row("Rijksmuseum").GetByRole(AriaRole.Rowheader));

        var marker = MapPane.Locator(".leaflet-marker-icon[alt='Rijksmuseum']");

        await Expect(marker).ToBeVisibleAsync();
        await Expect(marker).ToHaveAttributeAsync("src", selectedMarkerColor);
    }

    [TestMethod, EverySurface]
    public async Task PressingAMarker_Should_SelectItsRow(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey);

        await Search.FillAsync("Euromast");
        await Expect(Names).ToHaveCountAsync(1);

        await Press(MapButton);
        await Expect(MapPins).ToHaveCountAsync(1);
        await Press(MapPins);

        await Expect(MapPins).ToHaveAttributeAsync("src", selectedMarkerColor);

        await Press(ListButton);

        await Expect(Row("Euromast")).ToHaveAttributeAsync("aria-selected", "true");
    }

    [TestMethod, OnDesktops]
    public async Task DoubleClickingAMarker_Should_OpenItsEditor(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey, Wide);

        await Search.FillAsync("Euromast");
        await Expect(MapPins).ToHaveCountAsync(1);

        await MapPins.DblClickAsync();

        await Expect(Editor.GetByText(WorkbenchStrings.BuildingsEditTitle)).ToBeVisibleAsync();
        await Expect(Field(WorkbenchStrings.BuildingName)).ToHaveValueAsync("Euromast");
    }

    [TestMethod, OnDesktops]
    public async Task DoubleClickingInsideTheCountry_Should_AddABuildingThere(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey, $"{Wide}&sw-amount=few");

        await MapPane.DblClickAsync();

        await Expect(Editor.GetByText(WorkbenchStrings.BuildingsAddTitle)).ToBeVisibleAsync();
        await Expect(Field(WorkbenchStrings.BuildingLatitude)).ToHaveValueAsync(new Regex(@"^5[0-3]\.\d+$"));
        await Expect(Field(WorkbenchStrings.BuildingLongitude)).ToHaveValueAsync(new Regex(@"^[3-7]\.\d+$"));
    }

    [TestMethod, OnPhones]
    public async Task DoubleTappingInsideTheCountry_Should_AddABuildingThere(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey, "?sw-amount=few");

        await Press(MapButton);
        await Expect(MapPins.First).ToBeVisibleAsync();

        await MapPane.ScrollIntoViewIfNeededAsync();

        var box = (await MapPane.BoundingBoxAsync())!;

        await Page.Touchscreen.TapAsync(box.X + box.Width * 0.6f, box.Y + box.Height * 0.4f);
        await Page.Touchscreen.TapAsync(box.X + box.Width * 0.6f, box.Y + box.Height * 0.4f);

        await Expect(Editor.GetByText(WorkbenchStrings.BuildingsAddTitle)).ToBeVisibleAsync();
    }

    [TestMethod, OnDesktops]
    public async Task DoubleClickingOutsideTheCountry_Should_WarnAndOpenNothing(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey, $"{Wide}&sw-amount=few");

        var box = (await MapPane.BoundingBoxAsync())!;

        await MapPane.DblClickAsync(new() { Position = new() { X = box.Width * 0.95f, Y = box.Height * 0.08f } });

        await Expect(Notice(WorkbenchStrings.BuildingsOutsideTitle)).ToBeVisibleAsync();
        await Expect(Editor).ToHaveCountAsync(0);
    }

    [TestMethod, EverySurface]
    public async Task TheMapsPanAndZoom_Should_StayAfterAReload(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey, IsPhone ? string.Empty : Wide);

        await Search.FillAsync("Euromast");
        await Expect(Names).ToHaveCountAsync(1);

        if (IsPhone)
        {
            await Press(MapButton);
        }

        await Expect(MapPins).ToHaveCountAsync(1);

        var fitted = await SettledPin();

        await Press(MapPane.Locator(".leaflet-control-zoom-in"));
        await WorkbenchTestReviewer.WaitFor(PinPlace, place => place != fitted);

        var zoomed = await SettledPin();

        await PanTheMap(byX: -40, byY: -30);
        await WorkbenchTestReviewer.WaitFor(PinPlace, place => place != zoomed);

        var left = await SettledPin();

        await ReloadTheView();

        await Expect(MapPins).ToHaveCountAsync(1);

        await WorkbenchTestReviewer.WaitFor(PinPlace, place => Math.Abs(place.X - left.X) <= 2 && Math.Abs(place.Y - left.Y) <= 2);
    }

    [TestMethod, EverySurface]
    [TestCategory("Localization")]
    public async Task TheDeleteQuestion_Should_SpeakTheViewsLanguage(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey, "?culture=fa-IR");

        var dialog = View().GetByRole(AriaRole.Alertdialog, new() { Name = InCulture("fa-IR", nameof(WorkbenchStrings.BuildingsDeleteTitle)) });

        await Press(View().GetByRole(AriaRole.Grid).GetByRole(AriaRole.Button, new() { Name = InCulture("fa-IR", nameof(WorkbenchStrings.BuildingsDelete)), Exact = true }).First);

        await Expect(dialog).ToBeVisibleAsync();
        await Expect(dialog.GetByRole(AriaRole.Button, new() { Name = InCulture("fa-IR", nameof(WorkbenchStrings.Cancel)), Exact = true })).ToBeVisibleAsync();
        await Expect(dialog.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("^(Ok|Cancel)$", RegexOptions.IgnoreCase) })).ToHaveCountAsync(0);
    }

    [TestMethod, EverySurface]
    public async Task SwitchingToTheMap_Should_ShowTheBuildingSelectedInTheList(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey);

        await Press(Row("Rijksmuseum").GetByRole(AriaRole.Rowheader));
        await Expect(Row("Rijksmuseum")).ToHaveAttributeAsync("aria-selected", "true");

        await Press(MapButton);

        var marker = MapPane.Locator(".leaflet-marker-icon[alt='Rijksmuseum']");

        await Expect(marker).ToBeVisibleAsync();
        await Expect(marker).ToHaveAttributeAsync("src", selectedMarkerColor);
    }

    [TestMethod, EverySurface]
    public async Task AnAdmin_Should_AddABuildingThatShowsInTheGridAndOnTheMapAndStays(WorkbenchSurface surface)
    {
        var name = $"Museum {Guid.NewGuid():N}";

        await OpenWorkbench(PageKey);

        await Expect(View().GetByText(WorkbenchStrings.BuildingsMapHint)).ToHaveCountAsync(1);

        await AddABuilding(name, "Otterlo");

        await Expect(Subtitle(seedCount + 1, 12)).ToBeVisibleAsync();
        await Expect(MapMarkers).ToHaveCountAsync(seedCount + 1);

        await Search.FillAsync(name);

        await Expect(Names).ToHaveTextAsync([name]);
        await Expect(Row(name)).ToHaveAttributeAsync("aria-selected", "true");

        await ReloadTheView();

        await Expect(Names).ToHaveTextAsync([name]);
    }

    [TestMethod, EverySurface]
    public async Task TheEditor_Should_KeepEveryFieldOfANewBuilding(WorkbenchSurface surface)
    {
        var name = $"Tower {Guid.NewGuid():N}";

        await OpenWorkbench(PageKey);

        await Press(AddBuilding);

        await Field(WorkbenchStrings.BuildingName).FillAsync(name);
        await Field(WorkbenchStrings.BuildingCity).FillAsync("Maastricht");
        await Pick(Editor.GetByRole(AriaRole.Combobox, new() { Name = WorkbenchStrings.BuildingProvince }), WorkbenchStrings.ProvinceLimburg);
        await Pick(Editor.GetByRole(AriaRole.Combobox, new() { Name = WorkbenchStrings.BuildingType }), WorkbenchStrings.BuildingTypeTower);

        await Field(WorkbenchStrings.BuildingYear).FillAsync("1999");
        await Field(WorkbenchStrings.BuildingYear).BlurAsync();
        await Field(WorkbenchStrings.BuildingHeight).FillAsync("42.5");
        await Field(WorkbenchStrings.BuildingHeight).BlurAsync();
        await Field(WorkbenchStrings.BuildingFloors).FillAsync("12");
        await Field(WorkbenchStrings.BuildingFloors).BlurAsync();
        await Field(WorkbenchStrings.BuildingLatitude).FillAsync("50.85");
        await Field(WorkbenchStrings.BuildingLatitude).BlurAsync();
        await Field(WorkbenchStrings.BuildingLongitude).FillAsync("5.69");
        await Field(WorkbenchStrings.BuildingLongitude).BlurAsync();

        await Press(Save);

        await Expect(Notice(Saved(name))).ToBeVisibleAsync();
        await Search.FillAsync(name);
        await Expect(Row(name)).ToContainTextAsync($"Maastricht · {WorkbenchStrings.ProvinceLimburg}");

        await OpenEditorOf(Row(name));

        await Expect(Field(WorkbenchStrings.BuildingYear)).ToHaveValueAsync("1999");
        await Expect(Field(WorkbenchStrings.BuildingHeight)).ToHaveValueAsync("42.5");
        await Expect(Field(WorkbenchStrings.BuildingFloors)).ToHaveValueAsync("12");
        await Expect(Field(WorkbenchStrings.BuildingLatitude)).ToHaveValueAsync("50.85");
        await Expect(Field(WorkbenchStrings.BuildingLongitude)).ToHaveValueAsync("5.69");
        await Expect(Editor.GetByRole(AriaRole.Combobox, new() { Name = WorkbenchStrings.BuildingType })).ToContainTextAsync(WorkbenchStrings.BuildingTypeTower);
        await Expect(Editor.GetByRole(AriaRole.Combobox, new() { Name = WorkbenchStrings.BuildingProvince })).ToContainTextAsync(WorkbenchStrings.ProvinceLimburg);
    }

    [TestMethod, EverySurface]
    public async Task TheEditor_Should_RefuseABuildingWithoutANameOrACity(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey);

        await Press(AddBuilding);
        await Field(WorkbenchStrings.BuildingName).FillAsync("   ");
        await Press(Save);

        await Expect(Editor.GetByText(WorkbenchStrings.BuildingsNameRequired)).ToBeVisibleAsync();
        await Expect(Editor.GetByText(WorkbenchStrings.BuildingsCityRequired)).ToBeVisibleAsync();
        await Expect(Editor.GetByText(WorkbenchStrings.BuildingsAddTitle)).ToBeVisibleAsync();
        await Expect(Subtitle(seedCount, 12)).ToBeVisibleAsync();

        await Field(WorkbenchStrings.BuildingName).FillAsync("Named at last");
        await Field(WorkbenchStrings.BuildingName).BlurAsync();

        await Expect(Editor.GetByText(WorkbenchStrings.BuildingsNameRequired)).ToHaveCountAsync(0);
        await Expect(Editor.GetByText(WorkbenchStrings.BuildingsCityRequired)).ToBeVisibleAsync();
    }

    [TestMethod, EverySurface]
    public async Task TheEditor_Should_RefuseAWrongYearOrAPlaceOutsideTheCountry(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey);

        await Press(AddBuilding);
        await Field(WorkbenchStrings.BuildingName).FillAsync("Cologne Cathedral");
        await Field(WorkbenchStrings.BuildingCity).FillAsync("Cologne");
        await Field(WorkbenchStrings.BuildingYear).FillAsync("500");
        await Field(WorkbenchStrings.BuildingYear).BlurAsync();
        await Field(WorkbenchStrings.BuildingLatitude).FillAsync("50.9413");
        await Field(WorkbenchStrings.BuildingLatitude).BlurAsync();
        await Field(WorkbenchStrings.BuildingLongitude).FillAsync("6.9583");
        await Field(WorkbenchStrings.BuildingLongitude).BlurAsync();
        await Press(Save);

        await Expect(Editor.GetByText(WorkbenchStrings.BuildingsYearInvalid)).ToBeVisibleAsync();
        await Expect(Editor.GetByText(WorkbenchStrings.BuildingsOutsideBody)).ToBeVisibleAsync();
        await Expect(Subtitle(seedCount, 12)).ToBeVisibleAsync();

        await Field(WorkbenchStrings.BuildingYear).FillAsync((DateTime.Now.Year + 1).ToString(CultureInfo.InvariantCulture));
        await Field(WorkbenchStrings.BuildingYear).BlurAsync();
        await Press(Save);

        await Expect(Editor.GetByText(WorkbenchStrings.BuildingsYearInvalid)).ToBeVisibleAsync();
    }

    [TestMethod, EverySurface]
    public async Task CancelAndClose_Should_DropTheDraft(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey);

        await Press(AddBuilding);
        await Field(WorkbenchStrings.BuildingName).FillAsync("Never saved");
        await Press(Cancel);

        await Expect(Editor.GetByText(WorkbenchStrings.BuildingsAddTitle)).ToHaveCountAsync(0);
        await Expect(Subtitle(seedCount, 12)).ToBeVisibleAsync();

        await Press(AddBuilding);

        await Expect(Field(WorkbenchStrings.BuildingName)).ToHaveValueAsync(string.Empty);

        await Field(WorkbenchStrings.BuildingName).FillAsync("Never saved either");
        await Press(Editor.Locator(".editor-header").GetByRole(AriaRole.Button));

        await Expect(Editor.GetByText(WorkbenchStrings.BuildingsAddTitle)).ToHaveCountAsync(0);
        await Expect(Subtitle(seedCount, 12)).ToBeVisibleAsync();

        await ReloadTheView();

        await Expect(Editor.GetByText(WorkbenchStrings.BuildingsAddTitle)).ToHaveCountAsync(0);
    }

    [TestMethod, EverySurface]
    public async Task TheDraft_Should_ReopenAfterAReload(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey);

        await Press(AddBuilding);
        await Field(WorkbenchStrings.BuildingName).FillAsync("Half typed");
        await Field(WorkbenchStrings.BuildingName).BlurAsync();

        await ReloadTheView();

        await Expect(Editor.GetByText(WorkbenchStrings.BuildingsAddTitle)).ToBeVisibleAsync();
        await Expect(Field(WorkbenchStrings.BuildingName)).ToHaveValueAsync("Half typed");
    }

    [TestMethod, EverySurface]
    public async Task Editing_Should_ChangeTheRowAndStayAfterAReload(WorkbenchSurface surface)
    {
        var name = $"Rijksmuseum {Guid.NewGuid():N}";

        await OpenWorkbench(PageKey);

        await OpenEditorOf(Row("Rijksmuseum"));

        await Expect(Field(WorkbenchStrings.BuildingName)).ToHaveValueAsync("Rijksmuseum");
        await Expect(Field(WorkbenchStrings.BuildingCity)).ToHaveValueAsync("Amsterdam");
        await Expect(Field(WorkbenchStrings.BuildingYear)).ToHaveValueAsync("1885");

        await Field(WorkbenchStrings.BuildingName).FillAsync(name);
        await Press(Save);

        await Expect(Notice(Saved(name))).ToBeVisibleAsync();
        await Expect(Names.First).ToHaveTextAsync(name);
        await Expect(Subtitle(seedCount, 12)).ToBeVisibleAsync();

        await ReloadTheView();

        await Expect(Names.First).ToHaveTextAsync(name);
    }

    [TestMethod, EverySurface]
    public async Task CancellingAnEdit_Should_LeaveTheRowAlone(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey);

        await OpenEditorOf(Row("Rijksmuseum"));

        await Field(WorkbenchStrings.BuildingName).FillAsync("Changed my mind");
        await Press(Cancel);

        await Expect(Names.First).ToHaveTextAsync("Rijksmuseum");

        await ReloadTheView();

        await Expect(Names.First).ToHaveTextAsync("Rijksmuseum");
    }

    [TestMethod, OnDesktops]
    public async Task DoubleClickingARow_Should_OpenItsEditor(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey);

        await Row("Westerkerk").GetByRole(AriaRole.Rowheader).DblClickAsync();

        await Expect(Field(WorkbenchStrings.BuildingName)).ToHaveValueAsync("Westerkerk");
    }

    [TestMethod, EverySurface]
    public async Task DeletingABuilding_Should_AskFirstAndStayDeletedAfterAReload(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey);

        await Expect(Names.First).ToHaveTextAsync("Rijksmuseum");

        await AskToDelete(Row("Rijksmuseum"));

        await Expect(DeleteDialog).ToContainTextAsync(string.Format(CultureInfo.InvariantCulture, WorkbenchStrings.BuildingsDeleteBody, "Rijksmuseum"));

        await Press(DialogButton(AppStrings.Delete));

        await Expect(Notice(Deleted("Rijksmuseum"))).ToBeVisibleAsync();
        await Expect(Names.First).ToHaveTextAsync("Amsterdam Centraal");
        await Expect(Subtitle(seedCount - 1, 12)).ToBeVisibleAsync();
        await Expect(MapMarkers).ToHaveCountAsync(seedCount - 1);

        await ReloadTheView();

        await Expect(Names.First).ToHaveTextAsync("Amsterdam Centraal");
        await Expect(Subtitle(seedCount - 1, 12)).ToBeVisibleAsync();
    }

    [TestMethod, EverySurface]
    public async Task RefusingTheDelete_Should_KeepTheBuilding(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey);

        await AskToDelete(Row("Rijksmuseum"));

        await Press(DialogButton(WorkbenchStrings.Cancel));

        await Expect(DeleteDialog).ToHaveCountAsync(0);
        await Expect(Names.First).ToHaveTextAsync("Rijksmuseum");
        await Expect(Subtitle(seedCount, 12)).ToBeVisibleAsync();
    }

    [TestMethod, OnDesktops]
    public async Task TheDeleteKeyOnTheMap_Should_AskToDeleteTheSelectedBuilding(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey, Wide);

        await Press(Row("Rijksmuseum").GetByRole(AriaRole.Rowheader));
        await Expect(Row("Rijksmuseum")).ToHaveAttributeAsync("aria-selected", "true");

        await MapPane.Locator(".leaflet-container").FocusAsync();
        await Page.Keyboard.PressAsync("Delete");

        await Expect(DeleteDialog).ToContainTextAsync("Rijksmuseum");

        await Press(DialogButton(AppStrings.Delete));

        await Expect(Notice(Deleted("Rijksmuseum"))).ToBeVisibleAsync();
        await Expect(Row("Rijksmuseum")).ToHaveCountAsync(0);
    }

    [TestMethod, EverySurface]
    public async Task ADemoUser_Should_NotBeOfferedToChangeTheBuildings(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey, "?user=2");

        await Expect(Names).ToHaveCountAsync(20);
        await Expect(AddBuilding).ToHaveCountAsync(0);
        await Expect(View().GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.BuildingsEdit })).ToHaveCountAsync(0);
        await Expect(View().GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.BuildingsDelete })).ToHaveCountAsync(0);
        await Expect(View().GetByText(WorkbenchStrings.BuildingsMapHint)).ToHaveCountAsync(0);
    }

    [TestMethod, EverySurface]
    public async Task ADemoUser_Should_StillSearchAndSelect(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey, "?user=2");

        await Search.FillAsync("Rotterdam");
        await Expect(Names).ToHaveCountAsync(4);

        await Press(Row("Euromast").GetByRole(AriaRole.Rowheader));
        await Expect(Row("Euromast")).ToHaveAttributeAsync("aria-selected", "true");
    }

    [TestMethod, OnDesktops]
    public async Task ADemoUsersDoubleClick_Should_OpenNoEditor(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey, $"{Wide}&user=2&sw-amount=few");

        await Row("Rijksmuseum").GetByRole(AriaRole.Rowheader).DblClickAsync();
        await MapPane.DblClickAsync();

        await Expect(Row("Rijksmuseum")).ToHaveAttributeAsync("aria-selected", "true");
        await Expect(Editor.GetByText(WorkbenchStrings.BuildingsEditTitle)).ToHaveCountAsync(0);
        await Expect(Editor.GetByText(WorkbenchStrings.BuildingsAddTitle)).ToHaveCountAsync(0);
    }

    [TestMethod, EverySurface]
    public async Task TheStoreManager_Should_BeOfferedToChangeTheBuildings(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey, "?user=1");

        await Expect(Names).ToHaveCountAsync(20);
        await Expect(AddBuilding).ToBeVisibleAsync();
    }

    [TestMethod, EverySurface]
    public async Task TheCardsOption_Should_ListTheBuildingsAsCardsAndTableShouldComeBack(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey);

        await UseTheTools(WorkbenchStrings.BuildingsVariantCards);

        await Expect(Page).ToHaveURLAsync(new Regex("variant=cards"));
        await Expect(Cards).ToHaveCountAsync(seedCount);
        await Expect(Grid).ToHaveCountAsync(0);
        await Expect(Card("Rijksmuseum")).ToContainTextAsync(WorkbenchStrings.BuildingTypeMuseum);
        await Expect(Card("Rijksmuseum")).ToContainTextAsync("1885");

        await UseTheTools(WorkbenchStrings.BuildingsVariantTable);

        await Expect(Page).Not.ToHaveURLAsync(new Regex("variant=cards"));
        await Expect(Names).ToHaveCountAsync(20);
    }

    [TestMethod, EverySurface]
    public async Task TheCards_Should_SelectSearchEditAndDelete(WorkbenchSurface surface)
    {
        var name = $"Rijksmuseum {Guid.NewGuid():N}";

        await OpenWorkbench(PageKey, "?variant=cards");

        await Press(Card("Westerkerk").Locator(".card-main"));

        await Expect(Card("Westerkerk")).ToHaveClassAsync(new Regex("selected"));

        await Search.FillAsync("Rotterdam");

        await Expect(Cards).ToHaveCountAsync(4);

        await Search.FillAsync(string.Empty);

        await Expect(Cards).ToHaveCountAsync(seedCount);

        await OpenEditorOf(Card("Rijksmuseum"));

        await Field(WorkbenchStrings.BuildingName).FillAsync(name);
        await Press(Save);

        await Expect(Card(name)).ToBeVisibleAsync();
        await Expect(Card("Rijksmuseum")).ToHaveCountAsync(0);

        await AskToDelete(Card(name));
        await Press(DialogButton(AppStrings.Delete));

        await Expect(Card(name)).ToHaveCountAsync(0);
        await Expect(Cards).ToHaveCountAsync(seedCount - 1);
    }

    [TestMethod, OnDesktops]
    public async Task DoubleClickingACard_Should_OpenItsEditor(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey, "?variant=cards");

        await Card("Westerkerk").Locator(".card-main").DblClickAsync();

        await Expect(Field(WorkbenchStrings.BuildingName)).ToHaveValueAsync("Westerkerk");
    }

    [TestMethod, EverySurface]
    public async Task TheAmountSwitch_Should_ChangeHowManyBuildingsShow(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey);

        await UseTheTools(WorkbenchStrings.BuildingsAmountFew);

        await Expect(Page).ToHaveURLAsync(new Regex("sw-amount=few"));
        await Expect(Names).ToHaveCountAsync(3);
        await Expect(MapMarkers).ToHaveCountAsync(3);

        await UseTheTools(WorkbenchStrings.BuildingsAmountMany);

        await Expect(Subtitle(seedCount * 12, 12)).ToBeVisibleAsync();
        await Expect(MapMarkers).ToHaveCountAsync(seedCount * 12);

        await UseTheTools(WorkbenchStrings.BuildingsAmountNormal);

        await Expect(Page).Not.ToHaveURLAsync(new Regex("sw-amount"));
        await Expect(Subtitle(seedCount, 12)).ToBeVisibleAsync();
    }

    [TestMethod, EverySurface]
    public async Task TheNamesSwitch_Should_ShowLongNamesThatStillFit(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey);

        await UseTheTools(WorkbenchStrings.BuildingsNamesLong);

        await Expect(Page).ToHaveURLAsync(new Regex("sw-names=long"));
        await Expect(Names.First).ToContainTextAsync("Visitor Centre, Archive and Research Library of the Historic Building");

        await ExpectNoSidewaysScroll();

        await UseTheTools(WorkbenchStrings.BuildingsNamesAsTheyAre);

        await Expect(Names.First).ToHaveTextAsync("Rijksmuseum");
    }

    [TestMethod, EverySurface]
    public async Task LongNamesOnCards_Should_StillFit(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey, "?variant=cards&sw-names=long");

        await Expect(Cards.First).ToContainTextAsync("Visitor Centre, Archive and Research Library of the Historic Building");

        await ExpectNoSidewaysScroll();
    }

    [TestMethod, EverySurface]
    public async Task AnEditWhileASwitchReshapesTheList_Should_StayOnThePageAndNotBeSaved(WorkbenchSurface surface)
    {
        var name = $"Renamed {Guid.NewGuid():N}";

        await OpenWorkbench(PageKey, "?sw-names=long");

        await OpenEditorOf(Grid.GetByRole(AriaRole.Row).Filter(new() { HasText = "Rijksmuseum, Visitor Centre" }));

        await Field(WorkbenchStrings.BuildingName).FillAsync(name);
        await Press(Save);

        await Expect(Names.First).ToHaveTextAsync(name);

        await ReloadTheView();

        await Expect(Names.First).ToContainTextAsync("Rijksmuseum, Visitor Centre");
    }

    [TestMethod, EverySurface]
    public async Task Loading_Should_ShowPlaceholdersAndHoldBackAdding(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey, "?data=loading");

        await Expect(ListPane.Locator(".state")).ToBeVisibleAsync();
        await Expect(Grid).ToHaveCountAsync(0);
        await Expect(AddBuilding).ToBeDisabledAsync();
    }

    [TestMethod, EverySurface]
    public async Task Slow_Should_ShowPlaceholdersFirstAndTheBuildingsAfterAWhile(WorkbenchSurface surface)
    {
        await OpenWithoutWaiting(PageKey, "?data=slow");

        await Expect(ListPane.Locator(".state")).ToBeVisibleAsync();
        await Expect(Names).ToHaveCountAsync(20);
        await Expect(ListPane.Locator(".state")).ToHaveCountAsync(0);
        await Expect(AddBuilding).ToBeEnabledAsync();
    }

    [TestMethod, EverySurface]
    public async Task Empty_Should_SayThereIsNothingAndStillOfferAdding(WorkbenchSurface surface)
    {
        var name = $"First {Guid.NewGuid():N}";

        await OpenWorkbench(PageKey, "?data=empty");

        await Expect(ListPane).ToContainTextAsync(WorkbenchStrings.BuildingsEmptyTitle);
        await Expect(ListPane).ToContainTextAsync(WorkbenchStrings.BuildingsEmptyDescription);
        await Expect(Subtitle(0, 0)).ToBeVisibleAsync();
        await Expect(MapMarkers).ToHaveCountAsync(0);
        await Expect(AddBuilding).ToBeEnabledAsync();

        await AddABuilding(name, "Delft");

        await Expect(Names).ToHaveTextAsync([name]);
        await Expect(ListPane).Not.ToContainTextAsync(WorkbenchStrings.BuildingsEmptyTitle);
    }

    [TestMethod, EverySurface]
    public async Task Error_Should_SaySoAndARetryShouldLoadTheBuildings(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey, "?data=error");

        await Expect(ListPane.GetByRole(AriaRole.Alert)).ToContainTextAsync(WorkbenchStrings.BuildingsFailedTitle);
        await Expect(AddBuilding).ToBeDisabledAsync();

        await Press(ListPane.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.PosRetry, Exact = true }));

        await Expect(Names).ToHaveCountAsync(20);
        await Expect(ListPane.GetByRole(AriaRole.Alert)).ToHaveCountAsync(0);
        await Expect(AddBuilding).ToBeEnabledAsync();
        await Expect(MapMarkers).ToHaveCountAsync(seedCount);
    }

    [TestMethod, EverySurface]
    public async Task Mixed_Should_FailItsOnlySourceAndARetryShouldLoadIt(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey, "?data=mixed");

        await Expect(ListPane.GetByRole(AriaRole.Alert)).ToContainTextAsync(WorkbenchStrings.BuildingsFailedTitle);

        await Press(ListPane.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.PosRetry, Exact = true }));

        await Expect(ListPane.GetByRole(AriaRole.Alert)).ToHaveCountAsync(0);
    }
}
