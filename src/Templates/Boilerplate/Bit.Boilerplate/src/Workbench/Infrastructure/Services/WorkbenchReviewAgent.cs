using Bit.Bcrdt.Server;
using Microsoft.AspNetCore.WebUtilities;

namespace Boilerplate.Workbench.Infrastructure.Services;

public sealed partial class WorkbenchReviewAgent
{
    public static readonly TimeSpan MaxWait = TimeSpan.FromSeconds(55);

    [AutoInject] private IServiceProvider serviceProvider = default!;
    [AutoInject] private WorkbenchSnapshots snapshots = default!;
    [AutoInject] private WorkbenchReviewAttachments attachments = default!;
    [AutoInject] private WorkbenchScreenshotService screenshotService = default!;

    private TaskCompletionSource changed = new(TaskCreationOptions.RunContinuationsAsynchronously);

    private BcrdtDocumentHost Host => serviceProvider.GetRequiredService<BcrdtDocumentHost>();

    public void Pulse() => Interlocked.Exchange(ref changed, new(TaskCreationOptions.RunContinuationsAsynchronously)).TrySetResult();

    public async Task<List<WorkbenchReviewThread>> GetThreads(CancellationToken cancellationToken)
    {
        return await Host.ReadAsync(WorkbenchReviewDocument.Id, document => WorkbenchReviewDocument.ReadThreads(document.GetMap("threads")), cancellationToken);
    }

    public async Task<Dictionary<string, WorkbenchPageReview>> GetPages(CancellationToken cancellationToken)
    {
        return await Host.ReadAsync(WorkbenchReviewDocument.Id, document => WorkbenchReviewDocument.ReadPages(document.GetMap("pages")), cancellationToken);
    }

    public async Task<(long Cursor, List<WorkbenchReviewThread> Threads)> Wait(string agentName, long since, TimeSpan timeout, CancellationToken cancellationToken)
    {
        await Touch(agentName, null, cancellationToken);

        var deadline = DateTimeOffset.UtcNow + (timeout > MaxWait ? MaxWait : timeout);

        while (true)
        {
            var signal = Volatile.Read(ref changed).Task;
            var fresh = (await GetThreads(cancellationToken)).Where(t => t.Sequence > since && t.Status is not WorkbenchReviewStatus.Done).ToList();

            if (fresh.Count > 0)
                return (fresh.Max(t => t.Sequence), fresh);

            var remaining = deadline - DateTimeOffset.UtcNow;

            if (remaining <= TimeSpan.Zero)
                return (since, []);

            await Task.WhenAny(signal, Task.Delay(remaining, cancellationToken));
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Delay(150, cancellationToken);
        }
    }

    public Task<bool> Reply(string agentName, string threadId, string text, CancellationToken cancellationToken)
    {
        return Edit(agentName, threadId, threads => WorkbenchReviewDocument.AddMessage(threads, threadId, fromAgent: true, agentName, text), cancellationToken);
    }

    public Task<bool> SetStatus(string agentName, string threadId, WorkbenchReviewStatus status, CancellationToken cancellationToken)
    {
        return Edit(agentName, status is WorkbenchReviewStatus.Working ? threadId : null, threads => WorkbenchReviewDocument.SetStatus(threads, threadId, status, fromAgent: true), cancellationToken);
    }

    public Task<bool> SetTasks(string agentName, string threadId, IEnumerable<WorkbenchReviewTask> tasks, CancellationToken cancellationToken)
    {
        return Edit(agentName, threadId, threads => WorkbenchReviewDocument.SetTasks(threads, threadId, tasks), cancellationToken);
    }

    public async Task<bool> SetPageStatus(string agentName, string pageKey, WorkbenchPageStatus status, CancellationToken cancellationToken)
    {
        if (WorkbenchPageCatalog.Find(pageKey) is not { } page)
            return false;

        await Host.EditAsync(WorkbenchReviewDocument.Id, document =>
        {
            WorkbenchReviewDocument.SetPageStatus(document.GetMap("pages"), page.Key, status, agentName);
            WorkbenchReviewDocument.SetAgent(document.GetMap("agent"), agentName, null);
        }, cancellationToken);

        return true;
    }

    public async Task<List<WorkbenchFlow>> GetFlows(CancellationToken cancellationToken)
    {
        return await Host.ReadAsync(WorkbenchReviewDocument.Id, document => WorkbenchReviewDocument.ReadFlows(document.GetMap("flows")), cancellationToken);
    }

    public async Task<string?> SaveFlow(string agentName, string name, IEnumerable<(string PageKey, string? Query, string? Note)> steps, CancellationToken cancellationToken)
    {
        var list = steps.Where(step => WorkbenchPageCatalog.Find(step.PageKey) is not null)
                        .Select(step => new WorkbenchFlowStep(Guid.NewGuid().ToString("N")[..16], WorkbenchPageCatalog.Find(step.PageKey)!.Key, WorkbenchScenario.Parse(step.Query).ToQueryString(), step.Note, false))
                        .ToArray();

        if (string.IsNullOrWhiteSpace(name) || list.Length == 0)
            return null;

        var id = Guid.NewGuid().ToString("N")[..16];

        await Host.EditAsync(WorkbenchReviewDocument.Id, document =>
        {
            WorkbenchReviewDocument.AddFlow(document.GetMap("flows"), id, name, list);
            WorkbenchReviewDocument.SetAgent(document.GetMap("agent"), agentName, null);
        }, cancellationToken);

        return id;
    }

    public async Task<(byte[] Png, string FileName)?> Screenshot(string threadId, CancellationToken cancellationToken)
    {
        var thread = (await GetThreads(cancellationToken)).FirstOrDefault(t => t.Id == threadId);

        if (thread is null || WorkbenchPageCatalog.Find(thread.PageKey) is not { } page)
            return null;

        var snapshotId = thread.HasState ? snapshots.Add(await attachments.ReadState(thread.Id)) : null;
        var query = thread.Scenario.ToQueryString();

        return await screenshotService.CapturePage(page, snapshotId is null ? query : QueryHelpers.AddQueryString(query, WorkbenchUrls.SnapshotQueryKey, snapshotId), cancellationToken);
    }

    private async Task Touch(string agentName, string? threadId, CancellationToken cancellationToken)
    {
        await Host.EditAsync(WorkbenchReviewDocument.Id, document => WorkbenchReviewDocument.SetAgent(document.GetMap("agent"), agentName, threadId), cancellationToken);
    }

    private async Task<bool> Edit(string agentName, string? activeThreadId, Func<Bit.Bcrdt.BcrdtMap, bool> edit, CancellationToken cancellationToken)
    {
        var done = false;

        await Host.EditAsync(WorkbenchReviewDocument.Id, document =>
        {
            done = edit(document.GetMap("threads"));
            WorkbenchReviewDocument.SetAgent(document.GetMap("agent"), agentName, activeThreadId);
        }, cancellationToken);

        return done;
    }
}
