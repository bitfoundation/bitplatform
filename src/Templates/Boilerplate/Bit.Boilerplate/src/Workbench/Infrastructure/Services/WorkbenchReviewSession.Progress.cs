using Bit.Bcrdt;

namespace Boilerplate.Workbench.Infrastructure.Services;

/// <summary>
/// The agents' tasks of the Progress page in this circuit. On this machine, or for a reviewer an invite link was opened
/// for, the page shows the real tasks and what is done on them reaches the agent. While there are none, and for any other
/// visitor, it shows sample tasks kept in the circuit, which can be tried but reach nobody.
/// </summary>
public sealed partial class WorkbenchReviewSession
{
    [AutoInject] private WorkbenchSecrets secrets = default!;

    private BcrdtDocument? samples;
    private IReadOnlyList<WorkbenchProgressTask>? sampleTasks;

    /// <summary>The agents' real tasks, newest change first.</summary>
    public IReadOnlyList<WorkbenchProgressTask> Tasks { get; private set; } = [];

    /// <summary>Whether this circuit sees the real tasks and can act on them.</summary>
    public bool CanSeeTasks => SavesComments;

    /// <summary>Whether the page shows the samples: while no task has started, or to a visitor who may not see the real ones.</summary>
    public bool ShowsSampleTasks => CanSeeTasks is false || Tasks.Count == 0;

    public IReadOnlyList<WorkbenchProgressTask> ShownTasks => ShowsSampleTasks ? SampleTasks : Tasks;

    /// <summary>How many questions and steps of the real tasks wait for the user.</summary>
    public int NeedsYouCount => CanSeeTasks ? Tasks.Sum(t => t.NeedsYouCount) : 0;

    private IReadOnlyList<WorkbenchProgressTask> SampleTasks => sampleTasks ??= [.. WorkbenchProgressDocument.ReadTasks(Samples.GetMap(WorkbenchProgressDocument.MapName)).Select(t => t with { IsSample = true })];

    private BcrdtDocument Samples
    {
        get
        {
            if (samples is not null)
                return samples;

            samples = new();
            samples.Transact(_ =>
            {
                foreach (var task in WorkbenchSampleTasks.Create(localizer))
                {
                    WorkbenchProgressDocument.Write(samples.GetMap(WorkbenchProgressDocument.MapName), task);
                }
            });

            return samples;
        }
    }

    public bool CanAnswer(WorkbenchProgressTask task) => task.IsFinished is false && (task.IsSample || CanSeeTasks);

    /// <summary>Only this machine removes a task, and never a sample.</summary>
    public bool CanDelete(WorkbenchProgressTask task) => task.IsSample is false && Access.IsLocal;

    /// <summary>Answers a question of the task; returns false when the answer doesn't fit it or it was answered meanwhile.</summary>
    public bool Answer(WorkbenchProgressTask task, string askId, IReadOnlyList<string> choices, string? text, string by)
    {
        return EditProgress(task, progress => WorkbenchProgressDocument.Answer(progress, task.Id, askId, choices, text, by));
    }

    public bool CompleteStep(WorkbenchProgressTask task, string stepId, string by)
    {
        return EditProgress(task, progress => WorkbenchProgressDocument.CompleteStep(progress, task.Id, stepId, by));
    }

    public bool SendMessage(WorkbenchProgressTask task, string text, string by)
    {
        return EditProgress(task, progress => WorkbenchProgressDocument.AddMessage(progress, task.Id, text, by));
    }

    /// <summary>
    /// Whether this circuit may put a secret where a question says: in dev mode on this machine only, as gh runs in the user's
    /// name there, so never an invited reviewer elsewhere. A sample takes one anywhere and keeps nothing of it.
    /// </summary>
    public bool CanStoreSecrets(WorkbenchProgressTask task) => task.IsSample || CanWrite;

    /// <summary>
    /// Puts what the user pasted into a secret question where the question says, then marks it answered; the secret itself
    /// never enters the document. A sample's is only marked. Returns what went wrong, or null once it is stored.
    /// </summary>
    public async Task<string?> StoreSecret(WorkbenchProgressTask task, WorkbenchProgressAsk ask, string secret, string by, CancellationToken cancellationToken)
    {
        var failed = localizer[nameof(WorkbenchStrings.ProgressAnswerFailed)].Value;

        if (CanAnswer(task) is false || CanStoreSecrets(task) is false || ask.Kind is not WorkbenchProgressAskKind.Secret || string.IsNullOrWhiteSpace(secret))
            return failed;

        if (task.IsSample is false)
        {
            // Read again, so a question taken back or answered meanwhile stores nothing.
            if (Tasks.FirstOrDefault(t => t.Id == task.Id)?.Asks.FirstOrDefault(a => a.Id == ask.Id) is not { IsOpen: true } current)
                return failed;

            if (await secrets.Store(task.Id, current, secret.Trim(), cancellationToken) is { } problem)
                return localizer[nameof(WorkbenchStrings.ProgressSecretStoreFailed), problem].Value;
        }

        return EditProgress(task, progress => WorkbenchProgressDocument.MarkStored(progress, task.Id, ask.Id, by)) ? null : failed;
    }

    public void DeleteTask(WorkbenchProgressTask task)
    {
        if (CanDelete(task))
        {
            document?.Transact(_ => document.GetMap(WorkbenchProgressDocument.MapName).Delete(task.Id), null);
            WorkbenchSecrets.DeleteFiles(task.Id);
        }
    }

    /// <summary>Edits a sample in the circuit, or a real task in the document when this circuit may.</summary>
    private bool EditProgress(WorkbenchProgressTask task, Func<BcrdtMap, bool> edit)
    {
        var done = false;

        if (task.IsSample)
        {
            Samples.Transact(_ => done = edit(Samples.GetMap(WorkbenchProgressDocument.MapName)));
            sampleTasks = null;
            Changed?.Invoke();
        }
        else if (CanSeeTasks && document is not null)
        {
            document.Transact(_ => done = edit(document.GetMap(WorkbenchProgressDocument.MapName)), null);
        }

        return done;
    }

    private void RefreshTasks()
    {
        Tasks = document is null ? [] : WorkbenchProgressDocument.ReadTasks(document.GetMap(WorkbenchProgressDocument.MapName));
    }
}
