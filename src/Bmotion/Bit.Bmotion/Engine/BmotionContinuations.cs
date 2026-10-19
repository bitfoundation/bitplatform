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

    private static TaskScheduler CallerScheduler()
        => SynchronizationContext.Current is null
            ? TaskScheduler.Default
            : TaskScheduler.FromCurrentSynchronizationContext();
}
