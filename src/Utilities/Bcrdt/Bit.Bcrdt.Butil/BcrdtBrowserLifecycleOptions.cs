namespace Bit.Bcrdt;

/// <summary>What <see cref="BcrdtBrowserLifecycle"/> does when the browser reports a change.</summary>
public sealed class BcrdtBrowserLifecycleOptions
{
    /// <summary>
    /// Whether documents waiting out a reconnection backoff reconnect at once when the browser comes back online,
    /// the page is shown again, or a frozen page resumes.
    /// </summary>
    /// <remarks>
    /// The sync provider backs off exponentially, up to 30 seconds by default, because it cannot tell an outage
    /// that is over from one that is not. The browser can: a laptop waking up or a phone leaving a tunnel reports
    /// <c>online</c>, and a tab brought back to the front reports itself visible. Only a document in
    /// <see cref="BcrdtConnectionStatus.Reconnecting"/> is touched - never one the application disconnected on
    /// purpose, nor one that was refused, which stays <see cref="BcrdtConnectionStatus.Faulted"/>.
    /// </remarks>
    public bool ReconnectWhenOnline { get; set; } = true;

    /// <summary>
    /// Whether every open document's pending changes are flushed to its store when the page is hidden, unloaded or
    /// frozen - the last moments a browser reliably gives a page to save its state.
    /// </summary>
    /// <remarks>
    /// The persistence layer writes in the background as changes happen, so this does not start writes that would
    /// otherwise wait; it waits for them, then runs <see cref="AfterFlush"/>, and reports a change that could not be
    /// stored through <see cref="BcrdtBrowserLifecycle.Error"/>.
    /// </remarks>
    public bool FlushWhenHidden { get; set; } = true;

    /// <summary>
    /// Runs once every open document's changes have reached its store, when the page is hidden - to make the
    /// store itself durable, when its writes are buffered.
    /// </summary>
    /// <remarks>
    /// For example a <c>BcrdtBlazeDbDocumentStore</c> with <c>WaitForDurability</c> off, whose writes reach the
    /// database's memory at once and its storage on the next background flush:
    /// <c>options.AfterFlush = cancellationToken => database.FlushAsync(cancellationToken);</c>
    /// </remarks>
    public Func<CancellationToken, ValueTask>? AfterFlush { get; set; }

    /// <summary>
    /// Whether the local participant is withdrawn from every open document's presence when the page is unloaded or
    /// put in the back/forward cache, and restored when it is shown again.
    /// </summary>
    /// <remarks>
    /// Without it a closed tab stays listed until the presence timeout - 30 seconds by default - because nothing
    /// runs to say goodbye. The withdrawal is sent while the page is being torn down, so it reaches the others when
    /// the browser still lets the connection send - a navigation or a closed tab, as a rule - and the timeout
    /// covers the rest.
    /// </remarks>
    public bool SuspendPresenceOnPageHide { get; set; } = true;

    /// <summary>
    /// Whether to ask the browser, when the lifecycle starts, to make this site's storage persistent - so documents
    /// kept for offline use are not evicted when the device runs short of space.
    /// </summary>
    /// <remarks>
    /// Off by default, because some browsers ask the user rather than deciding by themselves, and a prompt at
    /// startup that the application did not ask for is a surprise. Chromium decides from how the site is used,
    /// and grants it at once to an installed app. <see cref="BcrdtBrowserLifecycle.IsStoragePersistent"/> says
    /// what the browser answered.
    /// </remarks>
    public bool RequestPersistentStorage { get; set; }
}
