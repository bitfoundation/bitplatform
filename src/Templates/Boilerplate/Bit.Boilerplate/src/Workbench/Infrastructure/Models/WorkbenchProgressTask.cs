namespace Boilerplate.Workbench.Infrastructure.Models;

/// <summary>
/// A long task an agent shows on the Progress page: where it stands, its steps, what it needs from the user and what
/// happened so far. Each agent chat keeps one of its own, so several run side by side.
/// </summary>
public sealed record WorkbenchProgressTask(string Id,
                                           string Title,
                                           string? Summary,
                                           string Agent,
                                           WorkbenchProgressStatus Status,
                                           DateTimeOffset CreatedAt,
                                           DateTimeOffset UpdatedAt,
                                           DateTimeOffset? SeenAt,
                                           WorkbenchProgressStep[] Steps,
                                           WorkbenchProgressNote[] Notes,
                                           WorkbenchProgressLink[] Links,
                                           WorkbenchProgressAsk[] Asks,
                                           WorkbenchProgressEntry[] Feed)
{
    /// <summary>One of the samples the page shows while no agent has started a task.</summary>
    public bool IsSample { get; init; }

    /// <summary>The agent's write-up of the task in Markdown, like the report a long run ends with.</summary>
    public string? Report { get; init; }

    public bool IsFinished => Status is not WorkbenchProgressStatus.Working;

    /// <summary>The questions that wait for an answer; none once the task is over.</summary>
    public IEnumerable<WorkbenchProgressAsk> OpenAsks => IsFinished ? [] : Asks.Where(a => a.IsOpen);

    /// <summary>The steps only the user can take that are due now; none once the task is over.</summary>
    public IEnumerable<WorkbenchProgressStep> StepsForYou => IsFinished ? [] : Steps.Where(s => s.Owner is WorkbenchProgressOwner.You && s.Status is WorkbenchProgressStepStatus.Doing);

    /// <summary>How many things wait for the user: open questions and their steps that are due.</summary>
    public int NeedsYouCount => OpenAsks.Count() + StepsForYou.Count();

    public int CompletedCount => Steps.Count(s => s.Status is WorkbenchProgressStepStatus.Done or WorkbenchProgressStepStatus.Skipped);

    /// <summary>How far the steps are, from 0 to 100; a finished task without steps counts as all the way.</summary>
    public double Percent => Steps.Length == 0 ? (Status is WorkbenchProgressStatus.Done ? 100 : 0) : Math.Round(CompletedCount * 100.0 / Steps.Length);

    /// <summary>Whether the agent called within the time an agent counts as online for.</summary>
    public bool IsAgentActive => SeenAt is { } seen && DateTimeOffset.UtcNow - seen < WorkbenchReviewAgentInfo.OnlineFor;

    /// <summary>The newest of the user's answers, messages and finished steps, which the agent waits from.</summary>
    public long Cursor => Feed.Where(e => e.FromYou).Select(e => e.Sequence).DefaultIfEmpty(0).Max();
}

/// <summary>One step of a task, taken by the agent or by the user.</summary>
public sealed record WorkbenchProgressStep(string Id, string Text, WorkbenchProgressOwner Owner, WorkbenchProgressStepStatus Status, string? Detail, string? Link);

/// <summary>Something the user must not miss: a warning, a decision, a result.</summary>
public sealed record WorkbenchProgressNote(string Id, WorkbenchProgressNoteKind Kind, string Text);

public sealed record WorkbenchProgressLink(string Label, string Url);

/// <summary>A question the agent asks the user, answered on the page with text, a choice, a yes or no, or a secret.</summary>
public sealed record WorkbenchProgressAsk(string Id,
                                          string Question,
                                          string? Detail,
                                          WorkbenchProgressAskKind Kind,
                                          string[] Options,
                                          string? Placeholder,
                                          DateTimeOffset At,
                                          WorkbenchProgressAnswer? Answer,
                                          bool IsWithdrawn)
{
    public bool IsOpen => Answer is null && IsWithdrawn is false;

    /// <summary>Where a secret question's answer goes; the answer itself never comes back.</summary>
    public WorkbenchProgressSecret? Secret { get; init; }
}

/// <summary>
/// Where the Workbench puts a secret the user pastes: a GitHub secret, of an environment or of the repository, or a file
/// outside the repository that the agent's commands read.
/// </summary>
public sealed record WorkbenchProgressSecret(WorkbenchProgressSecretTo To, string? Name, string? Environment);

/// <summary>The user's answer: the options picked, yes or no for a confirmation, and what they wrote.</summary>
public sealed record WorkbenchProgressAnswer(string[] Choices, string? Text, string By, DateTimeOffset At);

/// <summary>A line of a task's activity: an update from the agent, or a message, an answer or a finished step from the user.</summary>
public sealed record WorkbenchProgressEntry(WorkbenchProgressEntryKind Kind, string Author, string Text, DateTimeOffset At, long Sequence, string? RefId)
{
    public bool FromYou => Kind is not WorkbenchProgressEntryKind.Update;

    /// <summary>What kind of news an agent's update is, like a success or a problem found; null for a plain one.</summary>
    public WorkbenchProgressNoteKind? Tone { get; init; }
}

public enum WorkbenchProgressStatus
{
    Working,
    Done,
    Failed,
    Stopped
}

public enum WorkbenchProgressStepStatus
{
    Todo,
    Doing,
    Done,
    Blocked,
    Skipped
}

public enum WorkbenchProgressOwner
{
    Agent,
    You
}

public enum WorkbenchProgressNoteKind
{
    Info,
    Warning,
    Decision,
    Success,
    Error
}

public enum WorkbenchProgressAskKind
{
    Text,
    Choice,
    Choices,
    Confirm,
    Secret
}

public enum WorkbenchProgressSecretTo
{
    GitHub,
    File
}

public enum WorkbenchProgressEntryKind
{
    Update,
    Message,
    Answer,
    Step
}
