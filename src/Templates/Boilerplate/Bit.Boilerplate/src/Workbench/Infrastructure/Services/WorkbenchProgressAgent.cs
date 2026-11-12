using Bit.Bcrdt.Server;

namespace Boilerplate.Workbench.Infrastructure.Services;

/// <summary>
/// What a coding agent does on the Progress page, over HTTP: start a task, keep it current, ask the user and wait for the
/// answers. It edits the server's copy of the review document, which syncs to every open browser at once.
/// </summary>
public sealed partial class WorkbenchProgressAgent
{
    /// <summary>The longest a wait for the user lasts before it returns empty.</summary>
    public static readonly TimeSpan MaxWait = TimeSpan.FromSeconds(55);

    [AutoInject] private IServiceProvider serviceProvider = default!;

    // Completed and replaced on every update of the document, which wakes the waiters.
    private TaskCompletionSource changed = new(TaskCreationOptions.RunContinuationsAsynchronously);

    private BcrdtDocumentHost Host => serviceProvider.GetRequiredService<BcrdtDocumentHost>();

    /// <summary>Wakes every waiter; the document host calls it for each update a browser sends.</summary>
    public void Pulse() => Interlocked.Exchange(ref changed, new(TaskCreationOptions.RunContinuationsAsynchronously)).TrySetResult();

    public async Task<List<WorkbenchProgressTask>> GetTasks(CancellationToken cancellationToken)
    {
        return await Host.ReadAsync(WorkbenchReviewDocument.Id, document => WorkbenchProgressDocument.ReadTasks(document.GetMap(WorkbenchProgressDocument.MapName)), cancellationToken);
    }

    public async Task<WorkbenchProgressTask?> GetTask(string id, CancellationToken cancellationToken)
    {
        return await Host.ReadAsync(WorkbenchReviewDocument.Id, document => WorkbenchProgressDocument.ReadTask(document.GetMap(WorkbenchProgressDocument.MapName), id), cancellationToken);
    }

    /// <summary>Starts a task; returns its id, or what is wrong with the update.</summary>
    public async Task<(string? Id, string? Problem)> Create(string agentName, WorkbenchProgressUpdate update, CancellationToken cancellationToken)
    {
        if (WorkbenchProgressDocument.Check(update, null) is { } problem)
            return (null, problem);

        var id = Guid.NewGuid().ToString("N")[..12];

        await Host.EditAsync(WorkbenchReviewDocument.Id, document => WorkbenchProgressDocument.Create(document.GetMap(WorkbenchProgressDocument.MapName), id, agentName, update), cancellationToken);

        return (id, null);
    }

    /// <summary>Changes a task; returns whether it was found and what is wrong with the update.</summary>
    public async Task<(bool Found, string? Problem)> Update(string agentName, string id, WorkbenchProgressUpdate update, CancellationToken cancellationToken)
    {
        if (await GetTask(id, cancellationToken) is not { } task)
            return (false, null);

        if (WorkbenchProgressDocument.Check(update, task) is { } problem)
            return (true, problem);

        await Host.EditAsync(WorkbenchReviewDocument.Id, document => WorkbenchProgressDocument.Apply(document.GetMap(WorkbenchProgressDocument.MapName), id, agentName, update), cancellationToken);

        return (true, null);
    }

    public async Task<bool> Delete(string id, CancellationToken cancellationToken)
    {
        var found = false;

        await Host.EditAsync(WorkbenchReviewDocument.Id, document =>
        {
            var progress = document.GetMap(WorkbenchProgressDocument.MapName);

            found = progress.ContainsKey(id);
            progress.Delete(id);
        }, cancellationToken);

        return found;
    }

    /// <summary>
    /// Returns what the user did on the task after the cursor (answers, finished steps and messages) as soon as there is
    /// any, or nothing once the timeout passes; null when the task is gone. Waiting shows the agent as there.
    /// </summary>
    public async Task<(long Cursor, WorkbenchProgressTask Task, List<WorkbenchProgressEntry> Events)?> Wait(string agentName, string id, long since, TimeSpan timeout, CancellationToken cancellationToken)
    {
        var touched = false;

        await Host.EditAsync(WorkbenchReviewDocument.Id, document => touched = WorkbenchProgressDocument.Touch(document.GetMap(WorkbenchProgressDocument.MapName), id, agentName), cancellationToken);

        if (touched is false)
            return null;

        var deadline = DateTimeOffset.UtcNow + (timeout > MaxWait ? MaxWait : timeout);

        while (true)
        {
            var signal = Volatile.Read(ref changed).Task;

            if (await GetTask(id, cancellationToken) is not { } task)
                return null;

            var fresh = task.Feed.Where(e => e.FromYou && e.Sequence > since).ToList();

            if (fresh.Count > 0)
                return (fresh.Max(e => e.Sequence), task, fresh);

            var remaining = deadline - DateTimeOffset.UtcNow;

            if (remaining <= TimeSpan.Zero)
                return (since, task, []);

            // The delay's timer is cancelled as soon as the signal wins, so a busy page doesn't pile timers up.
            using (var delay = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            {
                await Task.WhenAny(signal, Task.Delay(remaining, delay.Token));
                delay.Cancel();
            }

            cancellationToken.ThrowIfCancellationRequested();
        }
    }
}
