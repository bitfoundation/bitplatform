namespace Boilerplate.Workbench.Infrastructure.Services;

public partial class WorkbenchScenarioState
{
    private int pendingLoads;
    private long lastActivityTicks = DateTimeOffset.UtcNow.UtcTicks;
    private readonly ConcurrentDictionary<string, int> attempts = [];

    public WorkbenchScenario Scenario { get; private set; } = new();

    public event Action? Changed;

    public bool IsSettled => Volatile.Read(ref pendingLoads) == 0;

    public DateTimeOffset LastActivity => new(Interlocked.Read(ref lastActivityTicks), TimeSpan.Zero);

    public void Set(WorkbenchScenario scenario)
    {
        Scenario = scenario;
        attempts.Clear();
        Touch();
        Changed?.Invoke();
    }

    public void Touch() => Interlocked.Exchange(ref lastActivityTicks, DateTimeOffset.UtcNow.UtcTicks);

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

    public Task Save(CancellationToken cancellationToken = default) => SimulateLatency(cancellationToken);

    private static Task SimulateLatency(CancellationToken cancellationToken) => Task.Delay(Random.Shared.Next(50, 151), cancellationToken);
}
