namespace Bit.BevTools;

/// <summary>
/// Faulted tasks nobody awaited: a fire-and-forget call (<c>_ = SaveAsync()</c>) whose exception would otherwise vanish.
/// The runtime reports them when such a task is collected (<see cref="TaskScheduler.UnobservedTaskException"/>), on the
/// finalizer thread, where nothing says which user's work it was: they reach the only session of a single-user host
/// (WebAssembly, Hybrid) and nobody's in a server process. BevTools never marks them observed, so the application's
/// own handling - or the runtime's - is unchanged.
/// </summary>
internal static class BevUnobservedTasks
{
    private static int _subscribed;

    public static void EnsureSubscribed()
    {
        if (Interlocked.Exchange(ref _subscribed, 1) == 0)
        {
            TaskScheduler.UnobservedTaskException += OnUnobserved;
        }
    }

    private static void OnUnobserved(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        try
        {
            Record(SessionResolver.Resolve(), e.Exception);
        }
        catch
        {
            // Never let the diagnostics tool fail the finalizer thread.
        }
    }

    internal static void Record(BevToolsSession? session, AggregateException? exception)
    {
        if (session is null || !session.IsEnabled || exception is null)
        {
            return;
        }

        foreach (var inner in exception.InnerExceptions.Take(5))
        {
            if (IsBevToolsOwn(inner))
            {
                continue;
            }

            session.Errors.Record(inner, "task", "A task's exception was never observed (fire-and-forget): " + inner.Message);
        }
    }

    // A task BevTools started (a preference save, a bridge call) that failed is not the application's failure. The
    // outermost frame below the task machinery is who started the chain - the async method whose task it is, the lambda
    // Task.Run ran: an application's call that merely passed through a BevTools wrapper (a tracked IJSRuntime) has the
    // wrapper inside it, not outermost.
    internal static bool IsBevToolsOwn(Exception exception)
    {
        try
        {
            var frames = new System.Diagnostics.StackTrace(exception, fNeedFileInfo: false).GetFrames();
            var outermost = frames.Select(f => f.GetMethod()).LastOrDefault(m => m is not null && !IsTaskMachinery(m.DeclaringType?.Namespace));
            return outermost?.Module.Assembly == typeof(BevUnobservedTasks).Assembly;
        }
        catch
        {
            return false;
        }
    }

    private static bool IsTaskMachinery(string? ns) =>
        ns is not null && (ns.StartsWith("System.Threading", StringComparison.Ordinal) || ns.StartsWith("System.Runtime.CompilerServices", StringComparison.Ordinal) || ns.StartsWith("System.Runtime.ExceptionServices", StringComparison.Ordinal));
}
