namespace Boilerplate.Workbench.Components.Pages.Showcases.Buildings.Data;

public partial class BuildingsService
{
    [AutoInject] private BuildingsStore store = default!;
    [AutoInject] private WorkbenchScenarioState scenarioState = default!;

    public Task<List<Building>> GetAll(CancellationToken cancellationToken) =>
        scenarioState.Load("buildings.list", () => Current().ToList(), () => [], cancellationToken);

    public async Task<Building> Save(Building building, CancellationToken cancellationToken)
    {
        if (NetherlandsBoundary.Contains(building.Latitude, building.Longitude) is false)
            throw new InvalidOperationException("Buildings can only be placed inside the Netherlands.");

        await scenarioState.Save(cancellationToken);

        var buildings = Current().ToList();
        var index = buildings.FindIndex(b => b.Id == building.Id);

        if (index >= 0)
        {
            buildings[index] = building.Clone();
        }
        else
        {
            buildings.Add(building.Clone());
        }

        store.StoreItems([.. buildings]);

        return building.Clone();
    }

    public async Task Delete(Guid id, CancellationToken cancellationToken)
    {
        await scenarioState.Save(cancellationToken);

        store.StoreItems([.. Current().Where(b => b.Id != id)]);
    }

    private IEnumerable<Building> Current() => (store.Items ?? [.. BuildingSeed.Create()]).Select(b => b.Clone());
}
