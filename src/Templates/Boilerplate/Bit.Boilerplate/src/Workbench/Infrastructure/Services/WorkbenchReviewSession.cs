using Bit.Bcrdt;

namespace Boilerplate.Workbench.Infrastructure.Services;

public sealed partial class WorkbenchReviewSession : IAsyncDisposable
{
    [AutoInject] private IConfiguration configuration = default!;
    [AutoInject] private IBcrdtCollaborationService collaboration = default!;
    [AutoInject] private WorkbenchReviewAttachments attachments = default!;

    private BcrdtCollaborativeDocument? document;

    public event Action? Changed;

    public bool IsEnabled => configuration.GetValue("Reviews:Enabled", true);

    public bool IsOpen => document is not null;

    public IReadOnlyList<WorkbenchReviewThread> Threads { get; private set; } = [];

    public IReadOnlyDictionary<string, WorkbenchPageReview> Pages { get; private set; } = new Dictionary<string, WorkbenchPageReview>();

    public WorkbenchReviewAgentInfo? Agent { get; private set; }

    public async Task Open(string reviewerName)
    {
        if (document is not null || IsEnabled is false)
            return;

        var id = Guid.NewGuid().ToString("N");

        document = await collaboration.OpenAsync(WorkbenchReviewDocument.Id, new() { User = new(id, reviewerName, BcrdtPresenceService.SuggestColor(id), null) }, CancellationToken.None);
        document.Changed += OnDocumentChanged;

        Refresh();
    }

    public IEnumerable<WorkbenchReviewThread> ThreadsOf(string pageKey) => Threads.Where(t => t.PageKey == pageKey);

    public WorkbenchPageReview? PageOf(string pageKey) => Pages.GetValueOrDefault(pageKey);

    public async Task<string?> AddThread(string pageKey, string query, WorkbenchReviewAnchor anchor, string author, string text, string? state)
    {
        if (document is null || string.IsNullOrWhiteSpace(text))
            return null;

        var id = Guid.NewGuid().ToString("N")[..16];
        var hasState = await attachments.SaveState(id, state);

        document.Transact(_ => WorkbenchReviewDocument.AddThread(document.GetMap("threads"), id, pageKey, query, anchor, author, text, hasState), null);

        return id;
    }

    public void Reply(string threadId, string author, string text)
    {
        if (document is null || string.IsNullOrWhiteSpace(text))
            return;

        document.Transact(_ => WorkbenchReviewDocument.AddMessage(document.GetMap("threads"), threadId, fromAgent: false, author, text), null);
    }

    public void SetStatus(string threadId, WorkbenchReviewStatus status)
    {
        document?.Transact(_ => WorkbenchReviewDocument.SetStatus(document.GetMap("threads"), threadId, status, fromAgent: false), null);
    }

    public void Delete(string threadId)
    {
        document?.Transact(_ => document.GetMap("threads").Delete(threadId), null);
    }

    public void SetPageStatus(string pageKey, WorkbenchPageStatus status, string by)
    {
        document?.Transact(_ => WorkbenchReviewDocument.SetPageStatus(document.GetMap("pages"), pageKey, status, by), null);
    }

    private void OnDocumentChanged(BcrdtUpdateEventArgs args)
    {
        Refresh();
        Changed?.Invoke();
    }

    private void Refresh()
    {
        if (document is null)
            return;

        Threads = WorkbenchReviewDocument.ReadThreads(document.GetMap("threads"));
        Pages = WorkbenchReviewDocument.ReadPages(document.GetMap("pages"));
        Agent = WorkbenchReviewDocument.ReadAgent(document.GetMap("agent"));
    }

    public async ValueTask DisposeAsync()
    {
        if (document is null)
            return;

        document.Changed -= OnDocumentChanged;
        await collaboration.CloseAsync(WorkbenchReviewDocument.Id);
    }
}
