using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.Brouter;

/// <summary>
/// Renders deferred (streamed) data: give it a <see cref="Task"/> your route <see cref="Broute.Loader"/>
/// returned <em>unawaited</em> inside its result object, and the route reveals immediately while this
/// component shows <see cref="Pending"/>, then <see cref="Resolved"/> (or <see cref="Error"/>) when the
/// task settles. The router-level equivalent of React Router's <c>&lt;Await&gt;</c> / TanStack Router's
/// deferred data: critical data blocks navigation via the loader, slow below-the-fold data streams in.
/// </summary>
/// <remarks>
/// Pattern: the loader returns quickly with the slow parts as unawaited tasks -
/// <c>return new PostData(post: await FetchPost(...), comments: FetchComments(...))</c> - and the page
/// renders <c>&lt;BrouterAwait Task="@Data.Get&lt;PostData&gt;().Comments"&gt;...&lt;/BrouterAwait&gt;</c>.
/// Note that loader results containing live tasks are skipped by <see cref="BrouterOptions.PersistLoaderState"/>
/// (tasks aren't serializable), so such loaders re-run on the interactive pass - by design.
/// </remarks>
public sealed class BrouterAwait<TValue> : ComponentBase, IDisposable
{
    /// <summary>The deferred task to await. A new task reference restarts the pending/resolved cycle.</summary>
    [Parameter, EditorRequired] public Task<TValue>? Task { get; set; }

    /// <summary>Shown while the task is running (and when <see cref="Task"/> is null).</summary>
    [Parameter] public RenderFragment? Pending { get; set; }

    /// <summary>Shown when the task completes successfully; receives the result.</summary>
    [Parameter] public RenderFragment<TValue>? Resolved { get; set; }

    /// <summary>
    /// Shown when the task faults (or is cancelled - it receives the <see cref="TaskCanceledException"/>).
    /// When omitted, a faulted task renders nothing; the failure stays observable on the task itself.
    /// </summary>
    [Parameter] public RenderFragment<Exception>? Error { get; set; }

    // The task instance this component is currently observing. Guards against a superseded task
    // (route data refreshed mid-await) applying its completion render over the newer task's state.
    private Task<TValue>? _observed;

    // Set on disposal so an in-flight ObserveAsync resuming afterwards never schedules a render.
    private bool _disposed;

    protected override void OnParametersSet()
    {
        if (ReferenceEquals(_observed, Task)) return;

        _observed = Task;
        if (Task is { IsCompleted: false })
        {
            _ = ObserveAsync(Task);
        }
    }

    private async Task ObserveAsync(Task<TValue> task)
    {
        try
        {
            await task;
        }
        catch
        {
            // The failure renders via the task's own status below; awaiting here only observes the
            // exception so it never surfaces as an unobserved-task crash.
        }

        if (_disposed is false && ReferenceEquals(_observed, task))
        {
            await InvokeAsync(StateHasChanged);
        }
    }

    public void Dispose()
    {
        _disposed = true;
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        base.BuildRenderTree(builder);

        var task = Task;
        if (task is null || task.IsCompleted is false)
        {
            if (Pending is not null) builder.AddContent(0, Pending);
            return;
        }

        if (task.IsCompletedSuccessfully)
        {
            if (Resolved is not null) builder.AddContent(1, Resolved(task.Result));
            return;
        }

        // Faulted or cancelled.
        if (Error is not null)
        {
            Exception exception = task.IsCanceled
                ? new TaskCanceledException(task)
                : task.Exception!.InnerExceptions.Count == 1
                    ? task.Exception.InnerExceptions[0]
                    : task.Exception;
            builder.AddContent(2, Error(exception));
        }
    }
}
