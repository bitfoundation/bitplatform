namespace Bit.Blow;

/// <summary>
/// Pauses a run between nodes: while paused, no node starts and the ones working finish. Hand one to
/// <see cref="BlowRunRequest.Control"/>; one control may serve several runs.
/// </summary>
public sealed class BlowRunControl
{
    private readonly object _gate = new();
    private TaskCompletionSource? _resumed;

    /// <summary>True while paused.</summary>
    public bool IsPaused
    {
        get { lock (_gate) return _resumed is not null; }
    }

    /// <summary>Stops nodes from starting until <see cref="Resume"/>.</summary>
    public void Pause()
    {
        lock (_gate) _resumed ??= new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    /// <summary>Lets nodes start again.</summary>
    public void Resume()
    {
        TaskCompletionSource? resumed;
        lock (_gate)
        {
            resumed = _resumed;
            _resumed = null;
        }
        resumed?.TrySetResult();
    }

    internal Task WhenResumedAsync(CancellationToken cancellationToken)
    {
        Task? wait;
        lock (_gate) wait = _resumed?.Task;
        return wait is null ? Task.CompletedTask : wait.WaitAsync(cancellationToken);
    }
}
