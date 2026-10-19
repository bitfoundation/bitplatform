namespace Bit.Bonboarding;

/// <summary>
/// Everything a template needs to render a step, plus the commands to drive the tour. This is the
/// single object passed to every <c>RenderFragment</c> the library exposes, including the fully
/// headless <c>ChildContent</c> of <c>BonboardingRoot</c>.
/// </summary>
public sealed class BonboardingStepRenderContext
{
    /// <summary>The running session.</summary>
    public required IBonboardingTourSession Session { get; init; }

    /// <summary>The step being rendered.</summary>
    public required BonboardingStepDefinition Step { get; init; }

    /// <summary>Options resolved for this step.</summary>
    public required BonboardingEffectiveStepOptions Options { get; init; }

    /// <summary>Localized title, or <see langword="null"/>.</summary>
    public string? Title { get; init; }

    /// <summary>Localized description, or <see langword="null"/>.</summary>
    public string? Description { get; init; }

    /// <summary>Labels in force, after tour overrides and localization.</summary>
    public BonboardingLabels Labels => Options.Labels;

    /// <summary>Index of the step in <see cref="BonboardingTourDefinition.Steps"/>.</summary>
    public int Index => Session.CurrentIndex;

    /// <summary>One-based position for progress display.</summary>
    public int Position => Session.DisplayPosition;

    /// <summary>Number of steps expected to be shown.</summary>
    public int Count => Session.DisplayCount;

    /// <summary>Latest measured target rectangle, before spotlight padding.</summary>
    public BonboardingRect? TargetRect => Session.TargetRect;

    /// <summary>Where the popover was placed.</summary>
    public BonboardingPlacementResult? Placement => Session.Placement;

    /// <summary>True when there is no earlier step to return to.</summary>
    public bool IsFirst => Session.IsFirstStep;

    /// <summary>True when this is the final step, so the forward button reads "Done".</summary>
    public bool IsLast => Session.IsLastStep;

    /// <summary>True while the engine is waiting for a target or a route.</summary>
    public bool IsWaiting => Session.IsWaiting;

    /// <summary>Text for the forward button, already switched to the done label on the last step.</summary>
    public string NextLabel => IsLast
        ? Step.DoneLabel ?? Labels.Done
        : Step.NextLabel ?? Labels.Next;

    /// <summary>Text for the back button.</summary>
    public string PreviousLabel => Step.PreviousLabel ?? Labels.Previous;

    /// <summary>Text for the skip button.</summary>
    public string SkipLabel => Step.SkipLabel ?? Labels.Skip;

    /// <summary>Progress text such as "2 of 5".</summary>
    public string ProgressText => BonboardingLabels.Format(Labels.ProgressFormat, BonboardingLabels.DefaultProgressFormat, Position, Count);

    /// <summary>Advances, or completes the tour on the last step.</summary>
    public Task NextAsync() => Session.NextAsync();

    /// <summary>Goes back one step.</summary>
    public Task PreviousAsync() => Session.PreviousAsync();

    /// <summary>Ends the tour as skipped.</summary>
    public Task SkipAsync() => Session.SkipAsync();

    /// <summary>Ends the tour as closed.</summary>
    public Task CloseAsync() => Session.DismissAsync();

    /// <summary>Ends the tour as completed.</summary>
    public Task CompleteAsync() => Session.CompleteAsync();

    /// <summary>Jumps to a step by id.</summary>
    public Task GoToAsync(string stepId) => Session.GoToAsync(stepId);

    /// <summary>Runs a custom footer action.</summary>
    public Task InvokeAsync(BonboardingStepAction action) => Session.InvokeActionAsync(action);
}
