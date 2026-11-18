namespace Bit.Blex;

/// <summary>Callbacks, optimistic updates and cache invalidation around a <see cref="BlexMutation{TArg, TResult}"/>.</summary>
/// <typeparam name="TArg">What the mutation is called with.</typeparam>
/// <typeparam name="TResult">What the server answers.</typeparam>
public sealed record BlexMutationOptions<TArg, TResult>
{
    /// <summary>
    /// Runs before the mutation is sent -- the place for an optimistic update. Return the
    /// <see cref="IBlexRollback"/> that undoes it (<see cref="BlexQueryClient.Optimistic{T}"/> returns one;
    /// <see cref="BlexRollback.Combine"/> joins several) and a failure rolls it back automatically.
    /// </summary>
    public Func<TArg, IBlexRollback?>? OnMutate { get; init; }

    /// <summary>Runs after the server accepted the change, with its answer -- the place to <see cref="BlexQueryClient.SetQueryData{T}(BlexQueryKey, T)">write it into the cache</see>.</summary>
    public Action<TResult, TArg>? OnSuccess { get; init; }

    /// <summary>Runs after the change failed (and its optimistic update was rolled back).</summary>
    public Action<Exception, TArg>? OnError { get; init; }

    /// <summary>Runs last, whichever way the change went.</summary>
    public Action<TArg>? OnSettled { get; init; }

    /// <summary>
    /// Keys to invalidate once the change settles, success or failure -- the server's view has moved either
    /// way. The mutation stays pending until the observed ones have refetched, so the UI never shows
    /// "saved" next to the list from before the save -- unless the app is offline, where those refetches wait for
    /// the network and the mutation settles without them.
    /// </summary>
    public IReadOnlyList<BlexQueryKey>? Invalidates { get; init; }

    /// <summary>How many times a failed call is retried. Defaults to <c>0</c>: a change is not repeated unless asked to.</summary>
    public int RetryCount { get; init; }

    /// <summary>The wait before the first retry; it doubles before each further one, up to 30 seconds.</summary>
    public TimeSpan RetryDelay { get; init; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Whether a call waits for the network while the app is offline -- pending and <see cref="BlexMutation{TArg, TResult}.IsPaused"/>,
    /// its optimistic update already shown -- and is sent once it returns. <c>null</c> uses the client's.
    /// </summary>
    public BlexNetworkMode? NetworkMode { get; init; }
}
