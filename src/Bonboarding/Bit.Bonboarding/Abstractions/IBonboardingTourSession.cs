namespace Bit.Bonboarding;

/// <summary>
/// One running (or finished) instance of a tour. Sessions are independent, so several may exist at
/// once, though only one is rendered by a given host at a time.
/// </summary>
public interface IBonboardingTourSession
{
    /// <summary>Unique id of this session instance.</summary>
    string Id { get; }

    /// <summary>The tour being run.</summary>
    BonboardingTourDefinition Tour { get; }

    /// <summary>Current lifecycle state.</summary>
    BonboardingTourStatus Status { get; }

    /// <summary>The step on screen, or <see langword="null"/> before start and after the end.</summary>
    BonboardingStepDefinition? CurrentStep { get; }

    /// <summary>Index of <see cref="CurrentStep"/> in <see cref="BonboardingTourDefinition.Steps"/>, or <c>-1</c>.</summary>
    int CurrentIndex { get; }

    /// <summary>
    /// One-based position among the steps this session will actually show, for progress display.
    /// Conditional steps that were filtered out are not counted.
    /// </summary>
    int DisplayPosition { get; }

    /// <summary>Number of steps this session expects to show, for progress display.</summary>
    int DisplayCount { get; }

    /// <summary>Resolved options for <see cref="CurrentStep"/>.</summary>
    BonboardingEffectiveStepOptions? CurrentOptions { get; }

    /// <summary>Last measured target rectangle in viewport coordinates, spotlight padding excluded.</summary>
    BonboardingRect? TargetRect { get; }

    /// <summary>Where the popover was placed, once it has been measured.</summary>
    BonboardingPlacementResult? Placement { get; }

    /// <summary>True when the current step has a resolved, on-screen target.</summary>
    bool HasTarget { get; }

    /// <summary>True when the engine is waiting for a target element or a route.</summary>
    bool IsWaiting { get; }

    /// <summary>True when no earlier step is reachable.</summary>
    bool IsFirstStep { get; }

    /// <summary>True when no later step is reachable.</summary>
    bool IsLastStep { get; }

    /// <summary>Ids of the steps visited so far, oldest first.</summary>
    IReadOnlyList<string> History { get; }

    /// <summary>Scratch state shared by every hook and template in this session.</summary>
    IDictionary<string, object?> State { get; }

    /// <summary>Why the tour stopped, once it has.</summary>
    BonboardingTourEndReason? EndReason { get; }

    /// <summary>The failure that stopped the tour, if any.</summary>
    Exception? Error { get; }

    /// <summary>True while the session is on screen and interactive.</summary>
    bool IsActive { get; }

    /// <summary>
    /// Raised whenever anything a UI would render has changed. The host component subscribes to
    /// this instead of polling.
    /// </summary>
    event EventHandler<BonboardingTourSessionChangedEventArgs>? Changed;

    /// <summary>Shows the first eligible step.</summary>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>Moves forward, honouring branching hooks, conditions and validation.</summary>
    Task NextAsync(CancellationToken cancellationToken = default);

    /// <summary>Moves back through the visit history.</summary>
    Task PreviousAsync(CancellationToken cancellationToken = default);

    /// <summary>Jumps to a step by id.</summary>
    /// <exception cref="BonboardingException">The tour has no step with that id.</exception>
    Task GoToAsync(string stepId, CancellationToken cancellationToken = default);

    /// <summary>Jumps to a step by index.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The index is outside the tour's steps.</exception>
    Task GoToAsync(int index, CancellationToken cancellationToken = default);

    /// <summary>Ends the tour as skipped by the user.</summary>
    Task SkipAsync(CancellationToken cancellationToken = default);

    /// <summary>Ends the tour as successfully completed.</summary>
    Task CompleteAsync(CancellationToken cancellationToken = default);

    /// <summary>Ends the tour as closed by the user.</summary>
    Task DismissAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops the tour without recording a decision by the user, keeping saved progress so it can
    /// resume later. This is what happens when another tour takes over.
    /// </summary>
    Task CancelAsync(CancellationToken cancellationToken = default);

    /// <summary>Hides the UI and stops observing, keeping the current position.</summary>
    Task PauseAsync(CancellationToken cancellationToken = default);

    /// <summary>Shows the current step again after <see cref="PauseAsync"/>.</summary>
    Task ResumeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Re-resolves the target and repositions. Call after your own code changes the layout in a way
    /// the built-in observers cannot see.
    /// </summary>
    Task RefreshAsync(CancellationToken cancellationToken = default);

    /// <summary>Runs a custom footer action and applies its <see cref="BonboardingStepAction.Effect"/>.</summary>
    Task InvokeActionAsync(BonboardingStepAction action, CancellationToken cancellationToken = default);
}
