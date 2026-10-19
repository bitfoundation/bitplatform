using Microsoft.AspNetCore.Components;

namespace Bit.Bcrdt.Demo.Client.Collaboration;

/// <summary>
/// The shared plumbing of every side-by-side demo page.
/// </summary>
/// <remarks>
/// <para>
/// Deriving from <see cref="BcrdtComponentBase"/> rather than <see cref="ComponentBase"/>
/// is what keeps these pages correct under load. Change events arrive from the transport's receive
/// path, and the base class marshals every callback onto the renderer's dispatcher and coalesces
/// bursts of them into a single render - so three replicas being typed into at once cannot produce
/// a render per keystroke per panel, and nothing ever calls <c>StateHasChanged</c> off-thread.
/// </para>
/// <para>
/// It also owns the teardown. Every replica is registered as a tracked resource, so leaving the
/// page closes its documents, drops its connections and unsubscribes its observers.
/// </para>
/// </remarks>
public abstract class ReplicaPageBase : BcrdtComponentBase
{
    /// <summary>The session-wide workspace that creates replicas and keeps their stores.</summary>
    [Inject]
    protected DemoWorkspace Workspace { get; set; } = default!;

    /// <summary>The replicas this page shows, in panel order.</summary>
    protected IReadOnlyList<DemoReplica> Replicas { get; private set; } = [];

    /// <summary>Whether the replicas have finished opening.</summary>
    protected bool Ready => Replicas.Count > 0;

    /// <summary>The document every replica on this page is a copy of.</summary>
    protected abstract string DocumentId { get; }

    /// <summary>One label per replica panel.</summary>
    protected abstract string[] ReplicaLabels { get; }

    /// <summary>Runs once the replicas are open, for per-page setup such as undo stacks.</summary>
    protected virtual void OnReplicasOpened() { }

    /// <inheritdoc/>
    protected override async Task OnInitializedAsync()
    {
        // The page is prerendered on the server before it runs in the browser. The replicas belong to the
        // browser - the in-memory network they talk over lives in its process - so the prerendered page only
        // shows the "opening" placeholder, and the interactive render opens them.
        if (!RendererInfo.IsInteractive) return;

        Replicas = await Workspace.OpenReplicasAsync(DocumentId, ReplicaLabels);

        foreach (DemoReplica replica in Replicas)
        {
            TrackDisposable(replica);

            // Three separate subscriptions rather than one, because they describe different things:
            // the document changing, who is present changing, and the connection changing. All three
            // funnel into the same coalesced render.
            Track(replica.Document);
            Track(replica.Document.Presence);
            if (replica.Document.Sync is not null) Track(replica.Document.Sync);
        }

        OnReplicasOpened();
    }
}
