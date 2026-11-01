namespace Boilerplate.Workbench.Infrastructure.Services;

/// <summary>
/// The scenario of the view this circuit renders, and how its pages load data: as the data state says, a load waits, fails or
/// comes back empty. It counts the loads in flight, so a screenshot waits until the page has settled.
/// </summary>
public partial class WorkbenchScenarioState
{
    private int pendingLoads;
    private long lastActivityTicks = DateTimeOffset.UtcNow.UtcTicks;
    private readonly ConcurrentDictionary<string, int> attempts = [];

    public WorkbenchScenario Scenario { get; private set; } = new();

    public event Action? Changed;

    public bool IsSettled => Volatile.Read(ref pendingLoads) == 0;

    /// <summary>When a load last started or ended, or the scenario last changed.</summary>
    public DateTimeOffset LastActivity => new(Interlocked.Read(ref lastActivityTicks), TimeSpan.Zero);

    /// <summary>Switches to another scenario; a source in the Error state fails again on its next load.</summary>
    public void Set(WorkbenchScenario scenario)
    {
        Scenario = scenario;
        attempts.Clear();
        Touch();
        Changed?.Invoke();
    }

    public void Touch() => Interlocked.Exchange(ref lastActivityTicks, DateTimeOffset.UtcNow.UtcTicks);

    /// <summary>Counts a load the page makes on its own, so a screenshot waits for it too.</summary>
    public async Task<T> Track<T>(Task<T> task)
    {
        Interlocked.Increment(ref pendingLoads);
        Touch();

        try
        {
            return await task;
        }
        finally
        {
            Interlocked.Decrement(ref pendingLoads);
            Touch();
        }
    }

    /// <summary>A data source's state; in Mixed, a hash of the source's name picks error, empty or live, the same on every run.</summary>
    public WorkbenchDataState ResolveState(string source)
    {
        if (Scenario.Data is not WorkbenchDataState.Mixed)
            return Scenario.Data;

        var hash = 0;
        foreach (var c in source)
        {
            hash = unchecked(hash * 31 + c);
        }

        return Math.Abs(hash % 4) switch
        {
            0 => WorkbenchDataState.Error,
            1 => WorkbenchDataState.Empty,
            _ => WorkbenchDataState.Live
        };
    }

    /// <summary>
    /// Loads sample data as the data state says: never finishing, slowly, empty, or failing on the first attempt so a retry works.
    /// </summary>
    public async Task<T> Load<T>(string source, Func<T> live, Func<T> empty, CancellationToken cancellationToken = default)
    {
        Interlocked.Increment(ref pendingLoads);
        Touch();

        try
        {
            var attempt = attempts.AddOrUpdate(source, 1, (_, value) => value + 1);

            switch (ResolveState(source))
            {
                case WorkbenchDataState.Loading:
                    await Task.Delay(Timeout.Infinite, cancellationToken);
                    break;
                case WorkbenchDataState.Slow:
                    await Task.Delay(Random.Shared.Next(1200, 2400), cancellationToken);
                    return live();
                case WorkbenchDataState.Error when attempt == 1:
                    await SimulateLatency(cancellationToken);
                    throw new WorkbenchSimulatedException(source);
                case WorkbenchDataState.Empty:
                    await SimulateLatency(cancellationToken);
                    return empty();
            }

            await SimulateLatency(cancellationToken);
            return live();
        }
        finally
        {
            Interlocked.Decrement(ref pendingLoads);
            Touch();
        }
    }

    /// <summary>Waits as a save to a server would.</summary>
    public Task Save(CancellationToken cancellationToken = default) => SimulateLatency(cancellationToken);

    private static Task SimulateLatency(CancellationToken cancellationToken) => Task.Delay(Random.Shared.Next(50, 151), cancellationToken);
}
