namespace Bit.Brouter.Demos.Core;

/// <summary>
/// Scoped state shared between demo pages and the routes declared in AppRouter - the realistic
/// pattern for a LeaveGuard that needs to know whether a page is dirty, and for showing the last
/// awaited navigation outcome after the page that triggered it has unmounted.
/// </summary>
public sealed class DemoState
{
    /// <summary>Set by EditorPage; read by the /editor route's LeaveGuard.</summary>
    public bool IsEditorDirty { get; set; }

    /// <summary>The most recent NavigateAsync outcome, recorded by OutcomesPage.</summary>
    public string? LastOutcome { get; set; }

    /// <summary>Whether the /unstable route's loader should throw (toggled from UnstablePage's error UI).</summary>
    public bool UnstableShouldFail { get; set; } = true;

    /// <summary>
    /// Route-lifecycle events recorded by LifecyclePage (newest first). Owned by this scoped state
    /// rather than the page so the Disposing deactivation - which fires while the page tears
    /// down - is still readable after the instance is gone.
    /// </summary>
    public List<string> LifecycleLog { get; } = [];
}

/// <summary>Payload produced by the /data route's loader (see AppRouter.LoadData).</summary>
public sealed record LoadedInfo(DateTime LoadedAt, bool WasRevalidation, string Page);

/// <summary>
/// Payload produced by the /deferred route's loader: the critical part resolves before render,
/// the slow part is an unawaited task streamed in via &lt;BrouterAwait&gt;.
/// </summary>
public sealed record DeferredReport(string Summary, Task<string[]> SlowDetails);
