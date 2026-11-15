using System.Text.Json;

namespace Boilerplate.Workbench.Infrastructure.Services.StandIns;

/// <summary>
/// What every stand-in of one of the app's controllers shares, so the app's own pages run in the Workbench as they do on its
/// server: every read goes through the view's scenario, so the Loading, Empty, Error, Mixed and Slow switches reach the
/// page, every write waits as a save to a server would, and what goes back and forth is a copy, as it is over HTTP.
/// </summary>
/// <param name="scenarioState">The view's scenario, whose data state every read follows.</param>
/// <param name="sources">What the reads of this stand-in are named under in the scenario, such as <c>settings</c>.</param>
public abstract class WorkbenchStandIn(WorkbenchScenarioState scenarioState, string sources)
{
    private static readonly JsonSerializerOptions jsonOptions = new(JsonSerializerDefaults.Web);

    protected WorkbenchScenarioState ScenarioState => scenarioState;

    /// <summary>
    /// A read, as the view's data state says, of a copy of what <paramref name="live"/> gives. A failure is the one the app's
    /// client raises when its server doesn't answer, so the app's page handles it as it would.
    /// </summary>
    protected async Task<T> Load<T>(string source, Func<T> live, Func<T> empty, CancellationToken cancellationToken)
    {
        try
        {
            return await scenarioState.Load($"{sources}.{source}", () => Copy(live()), () => Copy(empty()), cancellationToken);
        }
        catch (WorkbenchSimulatedException exception)
        {
            throw new Boilerplate.Shared.Infrastructure.Exceptions.TransientException(AppStrings.TransientException, exception);
        }
    }

    /// <summary>A write: waits as a save would, changes the sample data, and hands back a copy of the result.</summary>
    protected async Task<T> Save<T>(Func<T> change, CancellationToken cancellationToken)
    {
        await scenarioState.Save(cancellationToken);
        return Copy(change());
    }

    protected async Task Save(Action change, CancellationToken cancellationToken)
    {
        await scenarioState.Save(cancellationToken);
        change();
    }

    protected static T Copy<T>(T value) => value is null ? value : JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(value, jsonOptions), jsonOptions)!;

    protected static NotSupportedException NotUsedByTheWorkbench([System.Runtime.CompilerServices.CallerMemberName] string member = "")
        => new($"The Workbench's sample data doesn't stand in for {member}: no page the Workbench shows calls it.");
}
