using Bit.Bonboarding;
using Microsoft.AspNetCore.Components.Web;

namespace Boilerplate.Workbench.Components.Pages;

/// <summary>
/// /progress shows the long tasks agents run from their chats, each chat's own: what each one did, what it does now, what
/// is left, and what it needs from the user, who answers here with a click, a few words or their voice. With no task yet,
/// and for a visitor who may not see the real ones, it shows three samples that can be tried. A short guide walks a first
/// visit through it.
/// </summary>
public partial class ProgressPage
{
    public const string GuideId = "workbench-progress-guide";

    private bool isOpenFailed;
    private bool isSendingMessage;
    // The task whose Delete was pressed once; the next press deletes only that one.
    private string? confirmingDeleteId;
    private Timer? clock;
    // What the user is writing to each task's agent, kept with its task as the page moves between them.
    private readonly Dictionary<string, string?> drafts = [];

    [AutoInject] private WorkbenchReviewSession reviewSession = default!;
    [AutoInject] private WorkbenchStore store = default!;
    [AutoInject] private BitThemeManager bitThemeManager = default!;
    [AutoInject] private IBonboardingService bonboardingService = default!;
    [AutoInject] private Bit.Butil.WebAudio webAudio = default!;
    [AutoInject] private ILogger<ProgressPage> logger = default!;

    [Parameter] public string? TaskId { get; set; }

    /// <summary>The tasks under their headings: what needs the user first, then what runs, then what is over.</summary>
    private IEnumerable<(string TitleKey, List<WorkbenchProgressTask> Tasks)> Groups
    {
        get
        {
            var tasks = reviewSession.ShownTasks;

            (string, List<WorkbenchProgressTask>)[] groups =
            [
                (nameof(WorkbenchStrings.ProgressGroupNeedsYou), [.. tasks.Where(t => t.NeedsYouCount > 0).OrderByDescending(t => t.UpdatedAt)]),
                (nameof(WorkbenchStrings.ProgressGroupWorking), [.. tasks.Where(t => t.IsFinished is false && t.NeedsYouCount == 0).OrderByDescending(t => t.UpdatedAt)]),
                (nameof(WorkbenchStrings.ProgressGroupFinished), [.. tasks.Where(t => t.IsFinished).OrderByDescending(t => t.UpdatedAt)])
            ];

            return groups.Where(group => group.Item2.Count > 0);
        }
    }

    /// <summary>The task the address names, or on a wide screen without one, the first of the list.</summary>
    private WorkbenchProgressTask? SelectedTask => TaskId is null
        ? Groups.SelectMany(group => group.Tasks).FirstOrDefault()
        : reviewSession.ShownTasks.FirstOrDefault(t => t.Id == TaskId);

    /// <summary>The folder of the worktree this Workbench runs from, with its branch.</summary>
    private static string? RunningFrom => WorkbenchFolders.WorkingTree is { } tree
        ? WorkbenchFolders.Branch is { } branch ? $"{Path.GetFileName(tree)} ({branch})" : Path.GetFileName(tree)
        : null;

    private string BackIcon => CurrentDir is BitDir.Rtl ? BitIconName.Forward : BitIconName.Back;

    /// <summary>The name the user's answers and messages carry: the one they gave on a comment, or "You".</summary>
    private string Author => store.ReviewerName ?? Localizer[nameof(WorkbenchStrings.ProgressYou)];

    protected override void OnInitialized()
    {
        reviewSession.Changed += OnReviewChanged;
    }

    protected override void OnParametersSet()
    {
        confirmingDeleteId = null;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender is false)
            return;

        // The tasks live in the review document, which opens only once the page is interactive.
        try
        {
            await reviewSession.Open(Localizer[nameof(WorkbenchStrings.Reviewer)]);
        }
        catch (Exception exception)
        {
            isOpenFailed = true;
            logger.LogError(exception, "The review document could not be opened for the progress page.");
        }

        if (CurrentCancellationToken.IsCancellationRequested)
            return;

        StateHasChanged();

        // "2 minutes ago" moves on while the page stays open.
        clock = new(_ => InvokeAsync(StateHasChanged), null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));

        await WhenStateRestored();

        if (CurrentCancellationToken.IsCancellationRequested)
            return;

        // The browser's Back on the Workbench tour's Flows step lands here: back a step, as the tour would otherwise wait out of
        // sight for that page. Otherwise the first visit gets this page's guide, unless the tour brought it here.
        if (bonboardingService.FindSession(WorkbenchViewPage.TourId) is { WaitReason: BonboardingWaitReason.Route } tour)
        {
            await tour.PreviousAsync();
        }
        else if (bonboardingService.Active is null)
        {
            await bonboardingService.StartOnceAsync(CreateGuide());
        }
    }

    private Task StartGuide() => bonboardingService.StartAsync(CreateGuide());

    /// <summary>
    /// The guide of this page: the list of every chat's tasks, what waits for the user, the steps, the activity where the
    /// user writes to the agent, and the sound that calls them. It shows them on the task the page shows, which a phone opens
    /// for the steps after the list, and the end of the guide brings back the address it started from.
    /// </summary>
    private BonboardingTourDefinition CreateGuide()
    {
        var task = SelectedTask;
        var taskUrl = task is null ? WorkbenchUrls.Progress : WorkbenchUrls.ProgressTask(task.Id);
        var startedFrom = NavigationManager.ToBaseRelativePath(NavigationManager.Uri);

        return new()
        {
            Id = GuideId,
            MissingTarget = BonboardingMissingTargetBehavior.Center,
            // Only from the task the guide opened itself, so a page the user went to on their own stays.
            OnEnded = _ =>
            {
                var address = NavigationManager.ToBaseRelativePath(NavigationManager.Uri);

                if (address != startedFrom && $"/{address}" == taskUrl)
                {
                    NavigationManager.NavigateTo(startedFrom, replace: true);
                }

                return ValueTask.CompletedTask;
            },
            Steps =
            {
                new BonboardingStepDefinition
                {
                    Id = "list",
                    Target = BonboardingStepTarget.Anchor("progress-list"),
                    Title = nameof(WorkbenchStrings.ProgressGuideListTitle),
                    Description = nameof(WorkbenchStrings.ProgressGuideListText),
                    Placement = BonboardingPlacement.InlineEnd
                },
                new BonboardingStepDefinition
                {
                    Id = "your-turn",
                    Route = taskUrl,
                    When = _ => ValueTask.FromResult(task?.NeedsYouCount > 0),
                    Target = BonboardingStepTarget.Anchor("progress-your-turn"),
                    Title = nameof(WorkbenchStrings.ProgressGuideTurnTitle),
                    Description = nameof(WorkbenchStrings.ProgressGuideTurnText),
                    Placement = BonboardingPlacement.InlineStart
                },
                new BonboardingStepDefinition
                {
                    Id = "steps",
                    Route = taskUrl,
                    Target = BonboardingStepTarget.Anchor("progress-steps"),
                    Title = nameof(WorkbenchStrings.ProgressGuideStepsTitle),
                    Description = nameof(WorkbenchStrings.ProgressGuideStepsText),
                    Placement = BonboardingPlacement.InlineStart
                },
                new BonboardingStepDefinition
                {
                    Id = "activity",
                    Route = taskUrl,
                    Target = BonboardingStepTarget.Anchor("progress-activity"),
                    Title = nameof(WorkbenchStrings.ProgressGuideActivityTitle),
                    Description = nameof(WorkbenchStrings.ProgressGuideActivityText),
                    Placement = BonboardingPlacement.InlineStart
                },
                new BonboardingStepDefinition
                {
                    Id = "sound",
                    Title = nameof(WorkbenchStrings.ProgressGuideSoundTitle),
                    Description = nameof(WorkbenchStrings.ProgressGuideSoundText)
                }
            }
        };
    }

    private async Task<bool> Answer(WorkbenchProgressTask task, WorkbenchProgressAsk ask, IReadOnlyList<string> choices, string? text)
    {
        await UnlockAudio();

        if (reviewSession.Answer(task, ask.Id, choices, text, Author) is false)
        {
            SnackBarService.Error(Localizer[nameof(WorkbenchStrings.ProgressAnswerFailed)]);
            return false;
        }

        Sent(task);

        return true;
    }

    private async Task StoreSecret(WorkbenchProgressTask task, WorkbenchProgressAsk ask, string secret)
    {
        await UnlockAudio();

        if (await reviewSession.StoreSecret(task, ask, secret, Author, CurrentCancellationToken) is { } problem)
        {
            SnackBarService.Error(problem);
        }
        else if (task.IsSample)
        {
            SnackBarService.Show(Localizer[nameof(WorkbenchStrings.ProgressSampleSentTitle)], Localizer[nameof(WorkbenchStrings.ProgressSampleSecret), task.Agent]);
        }
        else
        {
            SnackBarService.Success(Localizer[nameof(WorkbenchStrings.ProgressSecretStored), task.Agent]);
        }
    }

    private async Task CompleteStep(WorkbenchProgressTask task, WorkbenchProgressStep step)
    {
        await UnlockAudio();

        if (reviewSession.CompleteStep(task, step.Id, Author))
        {
            Sent(task);
        }
    }

    private string? Draft(WorkbenchProgressTask task) => drafts.GetValueOrDefault(task.Id);

    private void SetDraft(WorkbenchProgressTask task, string? text) => drafts[task.Id] = text;

    /// <summary>
    /// Sends the task's draft. It is read before the await, so a second press meanwhile sends nothing more, and what the user
    /// went on typing then stays in the box.
    /// </summary>
    private async Task SendMessage(WorkbenchProgressTask task)
    {
        if (isSendingMessage || Draft(task)?.Trim() is not { Length: > 0 } text)
            return;

        isSendingMessage = true;

        try
        {
            await UnlockAudio();

            if (reviewSession.SendMessage(task, text, Author))
            {
                if (Draft(task)?.Trim() == text)
                {
                    drafts.Remove(task.Id);
                }

                Sent(task);
            }
        }
        finally
        {
            isSendingMessage = false;
        }
    }

    private Task SendOnCtrlEnter(KeyboardEventArgs e, WorkbenchProgressTask task)
    {
        return e.Key is "Enter" && (e.CtrlKey || e.MetaKey) ? SendMessage(task) : Task.CompletedTask;
    }

    /// <summary>Tells the user where what they sent went: to the agent, or nowhere for a sample.</summary>
    private void Sent(WorkbenchProgressTask task)
    {
        if (task.IsSample)
        {
            SnackBarService.Show(Localizer[nameof(WorkbenchStrings.ProgressSampleSentTitle)], Localizer[nameof(WorkbenchStrings.ProgressSampleSent), task.Agent]);
        }
        else
        {
            SnackBarService.Success(Localizer[nameof(WorkbenchStrings.ProgressSent), task.Agent]);
        }
    }

    /// <summary>Deletes the task on the second press for that same task; the first turns its button into a question.</summary>
    private void DeleteTask(WorkbenchProgressTask task)
    {
        if (confirmingDeleteId != task.Id)
        {
            confirmingDeleteId = task.Id;
            return;
        }

        confirmingDeleteId = null;
        reviewSession.DeleteTask(task);
        NavigationManager.NavigateTo(WorkbenchUrls.Progress);
    }

    private void Reload() => NavigationManager.Refresh(forceReload: true);

    /// <summary>Unlocks audio for the sound that calls the user back, which browsers allow only after a click.</summary>
    private async Task UnlockAudio()
    {
        try
        {
            await webAudio.Resume();
        }
        catch (JSException)
        {
        }
    }

    private string EntryText(WorkbenchProgressEntry entry)
    {
        return entry.Kind is WorkbenchProgressEntryKind.Step ? Localizer[nameof(WorkbenchStrings.ProgressEntryStepDone), entry.Text] : entry.Text;
    }

    /// <summary>
    /// An answer's line as its question and what was said: the options picked, yes or no, and the words added. Two lines
    /// rather than an arrow between them, which would point the wrong way once the two read in different directions.
    /// </summary>
    private (string Question, string Said)? AnswerOf(WorkbenchProgressTask task, WorkbenchProgressEntry entry)
    {
        if (entry.Kind is not WorkbenchProgressEntryKind.Answer || task.Asks.FirstOrDefault(a => a.Id == entry.RefId) is not { Answer: { } answer } ask)
            return null;

        if (ask.Kind is WorkbenchProgressAskKind.Secret)
            return (ask.Question, Localizer[nameof(WorkbenchStrings.ProgressSecretAnswered)]);

        var picked = ask.Kind is WorkbenchProgressAskKind.Confirm
            ? answer.Choices.Select(choice => Localizer[choice == WorkbenchProgressDocument.Yes ? nameof(WorkbenchStrings.ProgressYes) : nameof(WorkbenchStrings.ProgressNo)].Value)
            : answer.Choices;

        return (ask.Question, string.Join(" · ", new[] { string.Join(", ", picked), answer.Text }.Where(part => string.IsNullOrWhiteSpace(part) is false)));
    }

    private static string StepIcon(WorkbenchProgressStep step) => step.Status switch
    {
        WorkbenchProgressStepStatus.Done => BitIconName.CompletedSolid,
        WorkbenchProgressStepStatus.Doing when step.Owner is WorkbenchProgressOwner.You => BitIconName.Ringer,
        WorkbenchProgressStepStatus.Blocked => BitIconName.StatusCircleBlock,
        WorkbenchProgressStepStatus.Skipped => BitIconName.SkypeCircleMinus,
        _ => BitIconName.CircleRing
    };

    private static BitColor NoteColor(WorkbenchProgressNoteKind kind) => kind switch
    {
        WorkbenchProgressNoteKind.Warning => BitColor.Warning,
        WorkbenchProgressNoteKind.Decision => BitColor.Primary,
        WorkbenchProgressNoteKind.Success => BitColor.Success,
        WorkbenchProgressNoteKind.Error => BitColor.Error,
        _ => BitColor.Info
    };

    private static string NoteIcon(WorkbenchProgressNoteKind kind) => kind switch
    {
        WorkbenchProgressNoteKind.Warning => BitIconName.Warning,
        WorkbenchProgressNoteKind.Decision => BitIconName.Pinned,
        WorkbenchProgressNoteKind.Success => BitIconName.CompletedSolid,
        WorkbenchProgressNoteKind.Error => BitIconName.ErrorBadge,
        _ => BitIconName.Info
    };

    /// <summary>
    /// The direction the agent writes the task in, from the letters of its title and summary, when it isn't the page's: its
    /// texts then all read that way, rather than each by its first word, which is often a name in Latin letters.
    /// </summary>
    private static string? TaskDirection(WorkbenchProgressTask task)
    {
        var letters = $"{task.Title} {task.Summary}".Where(char.IsLetter).ToArray();
        var rightToLeft = letters.Count(IsRightToLeft) * 2 > letters.Length;

        return letters.Length == 0 || rightToLeft == CultureInfo.CurrentUICulture.TextInfo.IsRightToLeft ? null : rightToLeft ? "rtl" : "ltr";
    }

    private static bool IsRightToLeft(char letter) => letter is >= '\u0590' and <= '\u08FF' or >= '\uFB1D' and <= '\uFDFF' or >= '\uFE70' and <= '\uFEFF';

    /// <summary>A Workbench page opens in place; anything else in a new tab.</summary>
    private static string? LinkTarget(string url) => url.StartsWith('/') ? null : "_blank";

    /// <summary>A link named by its host, or by its path inside the Workbench.</summary>
    private static string LinkText(string url) => Uri.TryCreate(url, UriKind.Absolute, out var uri) ? uri.Host : url;

    private async Task ToggleShellTheme()
    {
        await bitThemeManager.ToggleDarkLightAsync();
    }

    private void OnReviewChanged() => InvokeAsync(StateHasChanged);

    public override async ValueTask DisposeAsync()
    {
        reviewSession.Changed -= OnReviewChanged;

        if (clock is not null)
        {
            await clock.DisposeAsync();
        }

        await base.DisposeAsync();
    }
}
