namespace Bit.Blex;

/// <summary>
/// The execution context a manager's state belongs to -- in Blazor, the renderer's
/// <c>Dispatcher</c>, which serializes everything a circuit or a BlazorWebView does. Stores are not
/// synchronized: every change to them has to happen there, exactly like every render does.
/// </summary>
/// <remarks>
/// <c>&lt;BlexProvider&gt;</c> binds the renderer's dispatcher to <see cref="BlexManager.Dispatcher"/>
/// automatically. Implement this to bind another one (a desktop UI thread, a test's single-threaded
/// context); a host that dispatches from one thread anyway needs none.
/// </remarks>
public interface IBlexDispatcher
{
    /// <summary>Whether the caller is already running in this dispatcher's context.</summary>
    bool CheckAccess();

    /// <summary>
    /// Runs <paramref name="workItem"/> in this dispatcher's context and returns a task that completes
    /// with it -- faulted with its exception when it throws.
    /// </summary>
    Task InvokeAsync(Func<Task> workItem);
}
