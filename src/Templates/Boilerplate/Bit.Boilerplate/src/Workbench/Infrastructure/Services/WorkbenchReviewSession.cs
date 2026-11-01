using Bit.Bcrdt;

namespace Boilerplate.Workbench.Infrastructure.Services;

/// <summary>
/// One circuit's copy of the review document, synced through the relay: the threads, page statuses, flows and the agent.
/// It writes only for requests made on this machine; opened from anywhere else, the review is read-only.
/// </summary>
public sealed partial class WorkbenchReviewSession : IAsyncDisposable
{
    [AutoInject] private IHttpContextAccessor httpContextAccessor = default!;
    [AutoInject] private IBcrdtCollaborationService collaboration = default!;
    [AutoInject] private WorkbenchReviewAttachments attachments = default!;

    private bool? canWrite;
    private Task? opening;
    private BcrdtCollaborativeDocument? document;

    /// <summary>Raised after the document changes, whoever changed it.</summary>
    public event Action? Changed;

    /// <summary>Whether this circuit's request came from this machine, decided once per circuit.</summary>
    public bool CanWrite => canWrite ??= httpContextAccessor.HttpContext is { } context && WorkbenchLocalRequests.IsLocal(context);

    public bool IsOpen => document is not null;

    public IReadOnlyList<WorkbenchReviewThread> Threads { get; private set; } = [];

    public IReadOnlyDictionary<string, WorkbenchPageReview> Pages { get; private set; } = new Dictionary<string, WorkbenchPageReview>();

    public WorkbenchReviewAgentInfo? Agent { get; private set; }

    public IReadOnlyList<WorkbenchFlow> Flows { get; private set; } = [];

    /// <summary>Creates an empty flow; returns its id, or null when this circuit can't write.</summary>
    public string? CreateFlow(string name)
    {
        if (document is null || CanWrite is false || string.IsNullOrWhiteSpace(name))
            return null;

        var id = Guid.NewGuid().ToString("N")[..16];

        document.Transact(_ => WorkbenchReviewDocument.SaveFlow(document.GetMap("flows"), id, name, [], []), null);

        return id;
    }

    /// <summary>Adds a view to a flow, right of its last screen and linked from it, with the view's page state.</summary>
    public async Task AddScreen(string flowId, string pageKey, string query, string? note, string? state, string? linkLabel)
    {
        if (document is null || CanWrite is false || WorkbenchReviewDocument.ReadFlows(document.GetMap("flows")).FirstOrDefault(f => f.Id == flowId) is not { } flow)
            return;

        var screenId = Guid.NewGuid().ToString("N")[..16];
        var hasState = await attachments.SaveState($"{flowId}{screenId}", state);
        var (x, y) = WorkbenchFlowLayout.Next(flow);
        var link = flow.Screens.LastOrDefault() is { } last ? new WorkbenchFlowLink(Guid.NewGuid().ToString("N")[..16], last.Id, screenId, linkLabel, null) : null;

        document.Transact(_ => WorkbenchReviewDocument.AddScreen(document.GetMap("flows"), flowId, new(screenId, pageKey, query, note, hasState, x, y), link), null);
    }

    public void MoveScreen(string flowId, string screenId, double x, double y)
    {
        EditFlows(flows => WorkbenchReviewDocument.MoveScreen(flows, flowId, screenId, x, y));
    }

    public void RemoveScreen(string flowId, string screenId)
    {
        if (CanWrite is false)
            return;

        EditFlows(flows => WorkbenchReviewDocument.RemoveScreen(flows, flowId, screenId));
        attachments.DeleteState($"{flowId}{screenId}");
    }

    public string AddLink(string flowId, string from, string to)
    {
        var linkId = Guid.NewGuid().ToString("N")[..16];

        EditFlows(flows => WorkbenchReviewDocument.AddLink(flows, flowId, new(linkId, from, to, null, null)));

        return linkId;
    }

    public void SetLinkLabel(string flowId, string linkId, string? label)
    {
        EditFlows(flows => WorkbenchReviewDocument.SetLinkLabel(flows, flowId, linkId, label));
    }

    public void SetLinkSpot(string flowId, string linkId, WorkbenchFlowSpot? spot)
    {
        EditFlows(flows => WorkbenchReviewDocument.SetLinkSpot(flows, flowId, linkId, spot));
    }

    public void RemoveLink(string flowId, string linkId)
    {
        EditFlows(flows => WorkbenchReviewDocument.RemoveLink(flows, flowId, linkId));
    }

    public void RenameFlow(string flowId, string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return;

        EditFlows(flows => WorkbenchReviewDocument.RenameFlow(flows, flowId, name));
    }

    /// <summary>Deletes a flow and the page states saved for its screens.</summary>
    public void DeleteFlow(string flowId)
    {
        if (CanWrite is false)
            return;

        var screens = Flows.FirstOrDefault(f => f.Id == flowId)?.Screens ?? [];

        EditFlows(flows => flows.Delete(flowId));

        foreach (var screen in screens)
        {
            attachments.DeleteState($"{flowId}{screen.Id}");
        }
    }

    /// <summary>Edits the flows in one change, when this circuit may write.</summary>
    private void EditFlows(Action<BcrdtMap> edit)
    {
        if (CanWrite is false)
            return;

        document?.Transact(_ => edit(document.GetMap("flows")), null);
    }

    /// <summary>Opens the document once per circuit, under the reviewer's name for presence; calls made meanwhile wait for the same open.</summary>
    public Task Open(string reviewerName) => opening ??= OpenDocument(reviewerName);

    private async Task OpenDocument(string reviewerName)
    {
        var id = Guid.NewGuid().ToString("N");

        document = await collaboration.OpenAsync(WorkbenchReviewDocument.Id, new() { User = new(id, reviewerName, BcrdtPresenceService.SuggestColor(id), null) }, CancellationToken.None);
        document.Changed += OnDocumentChanged;

        Refresh();
    }

    public IEnumerable<WorkbenchReviewThread> ThreadsOf(string pageKey) => Threads.Where(t => t.PageKey == pageKey);

    public WorkbenchPageReview? PageOf(string pageKey) => Pages.GetValueOrDefault(pageKey);

    /// <summary>Adds a comment with the view's page state; returns its id, or null when this circuit can't write.</summary>
    public async Task<string?> AddThread(string pageKey, string query, WorkbenchReviewAnchor anchor, string author, string text, string? state)
    {
        if (document is null || CanWrite is false || string.IsNullOrWhiteSpace(text))
            return null;

        var id = Guid.NewGuid().ToString("N")[..16];
        var hasState = await attachments.SaveState(id, state);

        document.Transact(_ => WorkbenchReviewDocument.AddThread(document.GetMap("threads"), id, pageKey, query, anchor, author, text, hasState), null);

        return id;
    }

    public void Reply(string threadId, string author, string text)
    {
        if (document is null || CanWrite is false || string.IsNullOrWhiteSpace(text))
            return;

        document.Transact(_ => WorkbenchReviewDocument.AddMessage(document.GetMap("threads"), threadId, fromAgent: false, author, text), null);
    }

    public void SetStatus(string threadId, WorkbenchReviewStatus status)
    {
        if (CanWrite is false)
            return;

        document?.Transact(_ => WorkbenchReviewDocument.SetStatus(document.GetMap("threads"), threadId, status, fromAgent: false), null);
    }

    /// <summary>Deletes a comment and the page state saved with it.</summary>
    public void Delete(string threadId)
    {
        if (document is null || CanWrite is false)
            return;

        document.Transact(_ => document.GetMap("threads").Delete(threadId), null);
        attachments.DeleteState(threadId);
    }

    public void SetPageStatus(string pageKey, WorkbenchPageStatus status, string by)
    {
        if (CanWrite is false)
            return;

        document?.Transact(_ => WorkbenchReviewDocument.SetPageStatus(document.GetMap("pages"), pageKey, status, by), null);
    }

    private void OnDocumentChanged(BcrdtUpdateEventArgs args)
    {
        Refresh();
        Changed?.Invoke();
    }

    /// <summary>Reads the document into the properties above.</summary>
    private void Refresh()
    {
        if (document is null)
            return;

        Threads = WorkbenchReviewDocument.ReadThreads(document.GetMap("threads"));
        Pages = WorkbenchReviewDocument.ReadPages(document.GetMap("pages"));
        Agent = WorkbenchReviewDocument.ReadAgent(document.GetMap("agent"));
        Flows = WorkbenchReviewDocument.ReadFlows(document.GetMap("flows"));
    }

    public async ValueTask DisposeAsync()
    {
        if (document is null)
            return;

        document.Changed -= OnDocumentChanged;
        await collaboration.CloseAsync(WorkbenchReviewDocument.Id);
    }
}
