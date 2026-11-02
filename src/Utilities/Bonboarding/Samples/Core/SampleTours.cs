namespace Bit.Bonboarding.Samples.Core;

/// <summary>
/// The ids every page refers to, and the tour that is defined in C# rather than in markup. The welcome tour is
/// declared in <see cref="Shared.MainLayout"/>; the workspace tour crosses two routes, so it is built here once and
/// registered from the layout, where it stays registered for as long as the app runs.
/// </summary>
public static class SampleTours
{
    public const string WelcomeId = "welcome";
    public const string WorkspaceId = "workspace";

    /// <summary>The checklist of the guidance page; the progress page ticks one of its items off.</summary>
    public const string ChecklistId = "getting-started";

    /// <summary>The checklist item the progress page ticks off by being opened.</summary>
    public const string ProgressItemId = "progress";

    /// <summary>Raised by the guidance page when a draft is saved; one of the checklist's items waits for it.</summary>
    public const string DraftSavedSignal = "draft-saved";

    /// <summary>The hint behind the beacon on the guidance page, stored like a one-step tour once it is read.</summary>
    public const string ExportHintId = "export-hint";

    /// <summary>Raised by the workspace page when a project is created; the tour's second step waits for it.</summary>
    public const string ProjectCreatedSignal = "project-created";

    /// <summary>
    /// A tour across /workspace and /workspace/settings. Every step has an explicit id: ids are what is stored, so
    /// they are what lets the tour resume at the right step after a reload.
    /// </summary>
    public static BonboardingTourDefinition CreateWorkspace() => new()
    {
        Id = WorkspaceId,
        Title = "Workspace tour",
        Description = "Two pages, with a step that waits for you to create a project.",
        Version = 1,
        Progress = BonboardingProgressStyle.Bar,
        Steps =
        {
            new BonboardingStepDefinition
            {
                Id = "projects",
                Route = "workspace",
                Target = BonboardingStepTarget.Anchor("projects"),
                Title = "Your projects",
                Description = "Everything in this workspace, newest first.",
                Placement = BonboardingPlacement.Bottom,
            },
            new BonboardingStepDefinition
            {
                Id = "create",
                Route = "workspace",
                Target = BonboardingStepTarget.Anchor("new-project"),
                Title = "Create one",
                Description = "Go ahead and press it: this step moves on when the page says a project was created.",
                AdvanceOnSignal = ProjectCreatedSignal,
                Interaction = BonboardingInteractionMode.TargetOnly,
                ShowNext = false,
            },
            new BonboardingStepDefinition
            {
                Id = "members",
                // The engine navigates here first, then looks for the target, so the element does not have to exist
                // on the page the tour started from.
                Route = "workspace/settings",
                Target = BonboardingStepTarget.Anchor("members"),
                Title = "Now on the settings page",
                Description = "The tour navigated here by itself and waited for the page to render.",
            },
            new BonboardingStepDefinition
            {
                Id = "api-keys",
                Route = "workspace/settings",
                Target = BonboardingStepTarget.Anchor("api-keys"),
                Title = "API keys",
                Description = "Keys are shown once and then hashed.",
                Placement = BonboardingPlacement.Top,
            },
            new BonboardingStepDefinition
            {
                Id = "back",
                Route = "workspace",
                Target = BonboardingStepTarget.Anchor("projects"),
                Title = "Back where you started",
                Description = "A tour can cross routes as often as it needs to, in either direction.",
                DoneLabel = "Finish",
            },
        },
    };
}
