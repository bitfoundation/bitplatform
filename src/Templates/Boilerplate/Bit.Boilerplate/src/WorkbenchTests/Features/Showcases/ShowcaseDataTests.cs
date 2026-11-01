namespace Boilerplate.WorkbenchTests.Features.Showcases;

/// <summary>
/// The showcases' sample data has to look the same on every run, so screenshots and comments stay comparable, and has
/// to follow the page switches, so Compare can spread them over the views.
/// </summary>
[TestClass, TestCategory("UnitTest")]
public class ShowcaseDataTests
{
    private static readonly ServiceProvider services = WorkbenchTestServer.CreateBuilder().Services.BuildServiceProvider();

    public TestContext TestContext { get; set; } = default!;

    private static (AsyncServiceScope Scope, T Service) Resolve<T>(WorkbenchScenario scenario) where T : notnull
    {
        var scope = services.CreateAsyncScope();

        scope.ServiceProvider.GetRequiredService<WorkbenchScenarioState>().Set(scenario);

        return (scope, scope.ServiceProvider.GetRequiredService<T>());
    }

    [TestMethod]
    public async Task TheBuildings_Should_FollowTheAmountAndNamesSwitches()
    {
        var seedCount = BuildingSeed.Create().Count;

        Assert.HasCount(seedCount, await GetBuildings(new()));
        Assert.HasCount(3, await GetBuildings(new WorkbenchScenario().WithSwitch("amount", "few")));

        var many = await GetBuildings(new WorkbenchScenario().WithSwitch("amount", "many"));

        Assert.HasCount(seedCount * 12, many);
        Assert.AreAllDistinct(many.Select(b => b.Id).ToArray());
        Assert.IsTrue(many.All(b => NetherlandsBoundary.Contains(b.Latitude, b.Longitude)), "Every copy still sits inside the country.");

        var longNames = await GetBuildings(new WorkbenchScenario().WithSwitch("names", "long"));

        Assert.IsTrue(longNames.All(b => b.Name.Length > 60));
    }

    [TestMethod]
    public async Task TheBuildings_Should_FollowTheDataState()
    {
        Assert.IsEmpty(await GetBuildings(new() { Data = WorkbenchDataState.Empty }));

        var (scope, buildings) = Resolve<BuildingsService>(new() { Data = WorkbenchDataState.Error });

        await using (scope)
        {
            await Assert.ThrowsExactlyAsync<WorkbenchSimulatedException>(() => buildings.GetAll(TestContext.CancellationToken));
            Assert.IsNotEmpty(await buildings.GetAll(TestContext.CancellationToken), "Try again works.");
        }
    }

    [TestMethod]
    public async Task ASavedBuilding_Should_StayInTheUsersCopy()
    {
        var (scope, buildings) = Resolve<BuildingsService>(new());

        await using (scope)
        {
            var store = scope.ServiceProvider.GetRequiredService<BuildingsStore>();
            var rijksmuseum = (await buildings.GetAll(TestContext.CancellationToken)).Single(b => b.Name == "Rijksmuseum");

            Assert.IsNull(store.Items, "Nothing is copied before the first change.");

            await buildings.Save(new Building { Name = "Kröller-Müller Museum", City = "Otterlo", Latitude = 52.0951, Longitude = 5.8165 }, TestContext.CancellationToken);
            await buildings.Delete(rijksmuseum.Id, TestContext.CancellationToken);

            var names = (await buildings.GetAll(TestContext.CancellationToken)).Select(b => b.Name).ToArray();

            Assert.Contains("Kröller-Müller Museum", names);
            Assert.DoesNotContain("Rijksmuseum", names);
            Assert.IsNotNull(store.Items);
        }
    }

    [TestMethod]
    public async Task ABuildingOutsideTheCountry_Should_BeRefused()
    {
        var (scope, buildings) = Resolve<BuildingsService>(new());

        await using (scope)
        {
            await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => buildings.Save(new Building { Name = "Cologne Cathedral", City = "Cologne", Latitude = 50.9413, Longitude = 6.9583 }, TestContext.CancellationToken));
        }
    }

    [TestMethod]
    public async Task AnEditWhileASwitchReshapesTheList_Should_NotBeSaved()
    {
        var (scope, buildings) = Resolve<BuildingsService>(new WorkbenchScenario().WithSwitch("amount", "many"));

        await using (scope)
        {
            var store = scope.ServiceProvider.GetRequiredService<BuildingsStore>();
            var first = (await buildings.GetAll(TestContext.CancellationToken))[^1];

            await buildings.Save(first, TestContext.CancellationToken);
            await buildings.Delete(first.Id, TestContext.CancellationToken);

            Assert.IsNull(store.Items, "Numbered copies aren't the user's buildings.");
        }
    }

    [TestMethod]
    public async Task TheDashboardsNumbers_Should_BeTheSameOnEveryRunAndFollowTheFilter()
    {
        var today = PosDashboardFilter.Default;
        var month = today with { Days = 30 };
        var utrecht = today with { StoreKey = nameof(WorkbenchStrings.PosStoreUtrecht) };

        var first = await GetNetSales(today);
        var second = await GetNetSales(today);

        Assert.AreEqual(first.Value, second.Value);
        Assert.AreSequenceEqual(first.Trend, second.Trend);
        Assert.HasCount(12, first.Trend);
        Assert.IsGreaterThan(first.Value, (await GetNetSales(month)).Value);
        Assert.IsLessThan(first.Value, (await GetNetSales(utrecht)).Value, "One store sells less than all of them.");
        Assert.IsGreaterThan(0m, first.Change);
    }

    [TestMethod]
    public async Task TheDashboard_Should_HaveNothingToShowWhenEmpty()
    {
        var (scope, data) = Resolve<PosDashboardDataService>(new() { Data = WorkbenchDataState.Empty });

        await using (scope)
        {
            var filter = PosDashboardFilter.Default;

            Assert.AreEqual(0m, (await data.GetNetSales(filter, TestContext.CancellationToken)).Value);
            Assert.IsEmpty(await data.GetSalesByHour(filter, TestContext.CancellationToken));
            Assert.IsEmpty(await data.GetTopProducts(filter, TestContext.CancellationToken));
            Assert.IsEmpty(await data.GetRecentOrders(filter, TestContext.CancellationToken));
        }
    }

    private async Task<List<Building>> GetBuildings(WorkbenchScenario scenario)
    {
        var (scope, buildings) = Resolve<BuildingsService>(scenario);

        await using (scope)
        {
            return await buildings.GetAll(TestContext.CancellationToken);
        }
    }

    private async Task<PosKpi> GetNetSales(PosDashboardFilter filter)
    {
        var (scope, data) = Resolve<PosDashboardDataService>(new());

        await using (scope)
        {
            return await data.GetNetSales(filter, TestContext.CancellationToken);
        }
    }
}
