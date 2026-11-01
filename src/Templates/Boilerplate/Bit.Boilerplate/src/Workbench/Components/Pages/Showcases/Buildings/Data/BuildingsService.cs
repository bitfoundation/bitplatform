namespace Boilerplate.Workbench.Components.Pages.Showcases.Buildings.Data;

/// <summary>The buildings API's stand-in: reads go through the scenario state, and writes land in <c>BuildingsStore</c>.</summary>
public partial class BuildingsService
{
    [AutoInject] private BuildingsStore store = default!;
    [AutoInject] private WorkbenchScenarioState scenarioState = default!;

    /// <summary>The buildings, shaped by the page's amount and names switches.</summary>
    public Task<List<Building>> GetAll(CancellationToken cancellationToken) =>
        scenarioState.Load("buildings.list", () => Shape(Current()).ToList(), () => [], cancellationToken);

    /// <summary>Saves a copy of the building, adding it when its id is new.</summary>
    public async Task<Building> Save(Building building, CancellationToken cancellationToken)
    {
        if (NetherlandsBoundary.Contains(building.Latitude, building.Longitude) is false)
            throw new InvalidOperationException("Buildings can only be placed inside the Netherlands.");

        await scenarioState.Save(cancellationToken);

        if (IsReshaped)
            return building.Clone();

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

        if (IsReshaped)
            return;

        store.StoreItems([.. Current().Where(b => b.Id != id)]);
    }

    /// <summary>
    /// Whether a switch shows numbered copies or lengthened names. Those aren't the user's buildings, so edits made
    /// meanwhile stay on the page and are not saved.
    /// </summary>
    private bool IsReshaped => scenarioState.Scenario.GetSwitch("amount") is "many" || scenarioState.Scenario.GetSwitch("names") is "long";

    /// <summary>Copies of the user's buildings, or of the samples before their first change.</summary>
    private IEnumerable<Building> Current() => (store.Items ?? [.. BuildingSeed.Create()]).Select(b => b.Clone());

    /// <summary>Applies the page's switches: fewer or many more buildings, and very long names.</summary>
    private IEnumerable<Building> Shape(IEnumerable<Building> buildings)
    {
        var scenario = scenarioState.Scenario;

        buildings = scenario.GetSwitch("amount") switch
        {
            "few" => buildings.Take(3),
            "many" => BuildingSeed.Multiply([.. buildings], 12),
            _ => buildings
        };

        return scenario.GetSwitch("names") is "long" ? buildings.Select(BuildingSeed.Lengthen) : buildings;
    }
}
