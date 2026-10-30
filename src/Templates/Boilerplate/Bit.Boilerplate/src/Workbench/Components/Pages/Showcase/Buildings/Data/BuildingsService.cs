namespace Boilerplate.Workbench.Components.Pages.Showcase.Buildings.Data;

public partial class BuildingsService
{
    private readonly List<Building> buildings = BuildingSeed.Create();

    [AutoInject] private WorkbenchScenarioState scenarioState = default!;

    public Task<List<Building>> GetAll(CancellationToken cancellationToken) =>
        scenarioState.Load("buildings.list", () => buildings.Select(b => b.Clone()).ToList(), () => [], cancellationToken);

    public async Task<Building> Save(Building building, CancellationToken cancellationToken)
    {
        if (NetherlandsBoundary.Contains(building.Latitude, building.Longitude) is false)
            throw new InvalidOperationException("Buildings can only be placed inside the Netherlands.");

        await scenarioState.Save(cancellationToken);

        var index = buildings.FindIndex(b => b.Id == building.Id);

        if (index >= 0)
        {
            buildings[index] = building.Clone();
        }
        else
        {
            buildings.Add(building.Clone());
        }

        return building.Clone();
    }

    public async Task Delete(Guid id, CancellationToken cancellationToken)
    {
        await scenarioState.Save(cancellationToken);

        buildings.RemoveAll(b => b.Id == id);
    }
}
