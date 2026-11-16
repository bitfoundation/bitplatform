namespace Bit.Bmotion;
/// <summary>
/// Schedules the work that follows an animation's completion on the thread the animation was
/// started from.
/// <para>
/// On Blazor Server that is the circuit's renderer dispatcher, which the engine relies on for
/// thread safety (it is lock-free, see <see cref="BmotionAnimationEngine"/>) and which a consumer's
/// <c>OnAnimationComplete</c> handler needs: a component re-rendering from a thread-pool thread
/// throws, and the exception vanishes with the fire-and-forget task. On WebAssembly there is no
/// synchronization context and a single thread, so this is the default scheduler as before.
/// </para>
/// </summary>
internal static class BmotionContinuations
{
    /// <summary>Runs <paramref name="continuation"/> once <paramref name="task"/> settles, on the caller's context.</summary>
    internal static Task OnCallerContext(this Task task, Action<Task> continuation)
        => task.ContinueWith(continuation, CancellationToken.None, TaskContinuationOptions.None, CallerScheduler());

    /// <summary>Runs <paramref name="continuation"/> once <paramref name="task"/> settles, on the caller's context, and awaits what it returns.</summary>
    internal static Task OnCallerContext<T>(this Task<T> task, Func<Task<T>, Task> continuation)
        => task.ContinueWith(continuation, CancellationToken.None, TaskContinuationOptions.None, CallerScheduler()).Unwrap();

    /// <summary>
    /// Runs <paramref name="work"/> on the caller's context once <paramref name="delayMs"/> have passed. False, with
    /// nothing scheduled, on a thread there is no coming back to: one with no synchronization context, off WebAssembly
    /// (whose one thread is the caller's) - a timer's thread there would race whatever the caller does meanwhile.
    /// </summary>
    internal static bool AfterDelayOnCallerContext(long delayMs, Action work)
    {
        if (SynchronizationContext.Current is null && !OperatingSystem.IsBrowser()) return false;
        _ = Task.Delay(TimeSpan.FromMilliseconds(Math.Max(delayMs, 1))).OnCallerContext(_ => work());
        return true;
    }

    /// <summary>
    /// Runs <paramref name="work"/> when <paramref name="token"/> is cancelled, posted to the caller's context - so a
    /// token cancelled from another thread (<c>CancelAfter</c>'s timer) still stops the engine from the thread that
    /// started the animation. Run at once when cancelled on that context, or when there is none.
    /// <para>
    /// Disposing the registration (on the caller's context) also drops a callback already posted and not yet run:
    /// the animation it would stop has settled, and a later one on the same properties must not be stopped by it.
    /// </para>
    /// </summary>
    internal static IDisposable RegisterOnCallerContext(this CancellationToken token, Action work)
        => new CallerContextRegistration(token, work);

    private sealed class CallerContextRegistration : IDisposable
    {
        private readonly CancellationTokenRegistration _registration;
        // Read and written on the caller's context only: by Dispose, and by the posted callback.
        private bool _disposed;

        public CallerContextRegistration(CancellationToken token, Action work)
        {
            var context = SynchronizationContext.Current;
            _registration = token.Register(() =>
            {
                if (context is null || context == SynchronizationContext.Current) Run();
                else context.Post(_ => Run(), null);
            });

            void Run()
            {
                if (!_disposed) work();
            }
        }

        public void Dispose()
        {
            _disposed = true;
            _registration.Dispose();
        }
    }

    private static TaskScheduler CallerScheduler()
        => SynchronizationContext.Current is null
            ? TaskScheduler.Default
            : TaskScheduler.FromCurrentSynchronizationContext();
}
