using Bit.Bcrdt;

namespace Boilerplate.Workbench.Infrastructure.Services;

/// <summary>
/// One circuit's copy of the review document, synced through the relay: the threads, page statuses, flows and the agent.
/// Comments can always be written: on this machine they go into the document for the agent that connects, and from elsewhere
/// they stay in the circuit. Everything else writes only in dev mode, for requests made on this machine.
/// </summary>
public sealed partial class WorkbenchReviewSession : IAsyncDisposable
{
    [AutoInject] private IHttpContextAccessor httpContextAccessor = default!;
    [AutoInject] private IConfiguration configuration = default!;
    [AutoInject] private IBcrdtCollaborationService collaboration = default!;
    [AutoInject] private WorkbenchReviewAttachments attachments = default!;
    [AutoInject] private IStringLocalizer<WorkbenchStrings> localizer = default!;

    private WorkbenchFlow[]? sampleFlows;
    private (bool CanWrite, bool SavesComments)? access;
    private Task? opening;
    private BcrdtCollaborativeDocument? document;
    private Timer? agentGoneTimer;

    // A visitor's comments, which live and die with the circuit and never reach the document.
    private readonly BcrdtDocument visitorComments = new();
    private readonly Dictionary<string, string> visitorStates = [];

    /// <summary>Raised after the document changes, whoever changed it.</summary>
    public event Action? Changed;

    /// <summary>Whether this circuit may write: in dev mode, with its request from this machine, decided once per circuit.</summary>
    public bool CanWrite => Access.CanWrite;

    /// <summary>Whether this circuit's comments go into the document, where an agent finds them: on this machine, with or without dev mode.</summary>
    public bool SavesComments => Access.SavesComments;

    // Read together, as the request is only there when the circuit starts.
    private (bool CanWrite, bool SavesComments) Access => access ??= httpContextAccessor.HttpContext is { } context
        ? (WorkbenchLocalRequests.CanChange(context, configuration), WorkbenchLocalRequests.IsLocal(context))
        : (false, false);

    public bool IsOpen => document is not null;

    public IReadOnlyList<WorkbenchReviewThread> Threads { get; private set; } = [];

    public IReadOnlyDictionary<string, WorkbenchPageReview> Pages { get; private set; } = new Dictionary<string, WorkbenchPageReview>();

    public WorkbenchReviewAgentInfo? Agent { get; private set; }

    public IReadOnlyList<WorkbenchFlow> Flows { get; private set; } = [];

    /// <summary>The flows to show: the review's own, then the samples.</summary>
    public IReadOnlyList<WorkbenchFlow> AllFlows => [.. Flows, .. sampleFlows ??= WorkbenchSampleFlows.Create(localizer)];

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

    /// <summary>Whether this circuit can answer, finish or delete a comment: on this machine any of them, and from elsewhere only its own.</summary>
    public bool CanChange(string threadId) => SavesComments || visitorComments.GetMap("threads").GetMap(threadId) is not null;

    /// <summary>Adds a comment, with the view's page state when it goes into the document; returns its id, or null when it has no text or the document isn't open.</summary>
    public async Task<string?> AddThread(string pageKey, string query, WorkbenchReviewAnchor anchor, string author, string text, string? state)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        var id = Guid.NewGuid().ToString("N")[..16];

        if (SavesComments is false)
        {
            if (state is not null)
            {
                visitorStates[id] = state;
            }

            visitorComments.Transact(_ => WorkbenchReviewDocument.AddThread(visitorComments.GetMap("threads"), id, pageKey, query, anchor, author, text, state is not null));
            OnCommentsChanged();

            return id;
        }

        if (document is null)
            return null;

        var hasState = await attachments.SaveState(id, state);

        document.Transact(_ => WorkbenchReviewDocument.AddThread(document.GetMap("threads"), id, pageKey, query, anchor, author, text, hasState), null);

        return id;
    }

    public void Reply(string threadId, string author, string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;

        EditComments(threadId, threads => WorkbenchReviewDocument.AddMessage(threads, threadId, fromAgent: false, author, text));
    }

    public void SetStatus(string threadId, WorkbenchReviewStatus status)
    {
        EditComments(threadId, threads => WorkbenchReviewDocument.SetStatus(threads, threadId, status, fromAgent: false));
    }

    /// <summary>The page state kept with a comment.</summary>
    public async Task<string?> ReadState(string threadId) => visitorStates.TryGetValue(threadId, out var state) ? state : await attachments.ReadState(threadId);

    /// <summary>Deletes a comment and the page state saved with it.</summary>
    public void Delete(string threadId)
    {
        EditComments(threadId, threads => threads.Delete(threadId));
        visitorStates.Remove(threadId);

        if (SavesComments)
        {
            attachments.DeleteState(threadId);
        }
    }

    /// <summary>Edits a comment where it lives, when this circuit may change it.</summary>
    private void EditComments(string threadId, Action<BcrdtMap> edit)
    {
        if (SavesComments)
        {
            document?.Transact(_ => edit(document.GetMap("threads")), null);
        }
        else if (CanChange(threadId))
        {
            visitorComments.Transact(_ => edit(visitorComments.GetMap("threads")));
            OnCommentsChanged();
        }
    }

    public void SetPageStatus(string pageKey, WorkbenchPageStatus status, string by)
    {
        if (CanWrite is false)
            return;

        document?.Transact(_ => WorkbenchReviewDocument.SetPageStatus(document.GetMap("pages"), pageKey, status, by), null);
    }

    private void OnDocumentChanged(BcrdtUpdateEventArgs args) => OnCommentsChanged();

    // Also when the document never opened, as a visitor's comments don't need it.
    private void OnCommentsChanged()
    {
        Refresh();
        Changed?.Invoke();
    }

    /// <summary>Reads the document into the properties above.</summary>
    private void Refresh()
    {
        List<WorkbenchReviewThread> saved = document is null ? [] : WorkbenchReviewDocument.ReadThreads(document.GetMap("threads"));

        // A visitor's comments are numbered on from the page's saved ones.
        Threads = [.. saved, .. WorkbenchReviewDocument.ReadThreads(visitorComments.GetMap("threads")).Select(t => t with { Number = t.Number + saved.Count(s => s.PageKey == t.PageKey) })];

        if (document is null)
            return;

        Pages = WorkbenchReviewDocument.ReadPages(document.GetMap("pages"));
        Agent = WorkbenchReviewDocument.ReadAgent(document.GetMap("agent"));
        Flows = WorkbenchReviewDocument.ReadFlows(document.GetMap("flows"));

        WatchAgent();
    }

    /// <summary>An agent that stops calling changes nothing in the document, so the review is told once it counts as gone.</summary>
    private void WatchAgent()
    {
        if (Agent is not { IsActive: true } agent)
            return;

        agentGoneTimer ??= new(_ => Changed?.Invoke());
        agentGoneTimer.Change(agent.At + WorkbenchReviewAgentInfo.OnlineFor + TimeSpan.FromSeconds(1) - DateTimeOffset.UtcNow, Timeout.InfiniteTimeSpan);
    }

    public async ValueTask DisposeAsync()
    {
        visitorComments.Dispose();

        if (document is null)
            return;

        document.Changed -= OnDocumentChanged;
        agentGoneTimer?.Dispose();
        await collaboration.CloseAsync(WorkbenchReviewDocument.Id);
    }
}
