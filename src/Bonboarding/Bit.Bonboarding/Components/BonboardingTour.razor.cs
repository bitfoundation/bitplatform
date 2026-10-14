using Microsoft.AspNetCore.Components;

namespace Bit.Bonboarding;

/// <summary>
/// Declares a tour in markup. Child <see cref="BonboardingStep"/> components become its steps, in
/// the order they are rendered.
/// </summary>
public sealed partial class BonboardingTour : ComponentBase, IAsyncDisposable
{
    private readonly List<BonboardingStep> _steps = [];

    private BonboardingTourDefinition? _definition;
    private bool _autoStarted;
    private bool _disposed;

    [Inject] private IBonboardingService BonboardingService { get; set; } = default!;

    /// <summary>Stable identifier, also used as the persistence key.</summary>
    [Parameter, EditorRequired] public required string Id { get; set; }

    /// <summary>Human-readable name, used as the dialog label when a step has no title.</summary>
    [Parameter] public string? Title { get; set; }

    /// <summary>Bump this after editing the steps to discard stale saved progress.</summary>
    [Parameter] public int Version { get; set; } = 1;

    /// <summary>Whether the tour starts itself once its markup is on screen.</summary>
    [Parameter] public BonboardingAutoStart AutoStart { get; set; } = BonboardingAutoStart.None;

    /// <summary>The steps.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    // ---- Common option overrides, applied to every step ---------------------

    [Parameter] public BonboardingPlacement? Placement { get; set; }
    [Parameter] public BonboardingTheme? Theme { get; set; }
    [Parameter] public BonboardingDirection? Direction { get; set; }
    [Parameter] public BonboardingProgressStyle? Progress { get; set; }
    [Parameter] public BonboardingInteractionMode? Interaction { get; set; }
    [Parameter] public bool? ShowOverlay { get; set; }
    [Parameter] public bool? ShowSpotlight { get; set; }
    [Parameter] public bool? ShowSkip { get; set; }
    [Parameter] public bool? ShowClose { get; set; }
    [Parameter] public bool? ShowPrevious { get; set; }
    [Parameter] public bool? CloseOnEscape { get; set; }
    [Parameter] public bool? CloseOnOverlayClick { get; set; }
    [Parameter] public bool? Persist { get; set; }
    [Parameter] public BonboardingLabels? Labels { get; set; }

    /// <summary>Replaces the popover contents for every step that has no template of its own.</summary>
    [Parameter] public RenderFragment<BonboardingStepRenderContext>? Template { get; set; }

    /// <summary>
    /// Escape hatch for anything not exposed as a parameter. Runs after the parameters are applied
    /// and before the tour is registered.
    /// </summary>
    [Parameter] public Action<BonboardingTourDefinition>? Configure { get; set; }

    // ---- Lifecycle callbacks ------------------------------------------------

    [Parameter] public EventCallback OnStarted { get; set; }
    [Parameter] public EventCallback<BonboardingStepChangedEventArgs> OnStepChanged { get; set; }
    [Parameter] public EventCallback<BonboardingTourEndedEventArgs> OnEnded { get; set; }

    /// <summary>The definition assembled from the markup. Safe to inspect and to pass around.</summary>
    public BonboardingTourDefinition Definition => _definition ??= new BonboardingTourDefinition { Id = Id };

    /// <summary>The running session for this tour, if there is one.</summary>
    public IBonboardingTourSession? Session => BonboardingService.FindSession(Id);

    /// <summary>True while this tour is on screen.</summary>
    public bool IsRunning => Session is { IsActive: true };

    protected override void OnParametersSet()
    {
        Rebuild();
        BonboardingService.Register(Definition);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        // Children have registered by the time the first render completes, and OnAfterRender never
        // runs while prerendering, so this is the earliest safe point to start.
        if (!firstRender || _disposed || AutoStart == BonboardingAutoStart.None || _autoStarted) return;

        _autoStarted = true;

        if (Definition.Steps.Count == 0) return;

        if (AutoStart == BonboardingAutoStart.Once)
        {
            await BonboardingService.StartOnceAsync(Definition);
        }
        else
        {
            await BonboardingService.StartAsync(Definition);
        }
    }

    // ---- Step registration --------------------------------------------------

    internal void AttachStep(BonboardingStep step)
    {
        if (_steps.Contains(step)) return;

        _steps.Add(step);
        Rebuild();
    }

    internal void DetachStep(BonboardingStep step)
    {
        if (!_steps.Remove(step)) return;
        Rebuild();
    }

    internal void NotifyStepChanged() => Rebuild();

    /// <summary>
    /// Re-projects the child components into the definition. The definition object itself is kept,
    /// so a tour that is already running picks up edits without being restarted.
    /// </summary>
    private void Rebuild()
    {
        var definition = Definition;

        definition.Id = Id;
        definition.Title = Title;
        definition.Version = Version;
        definition.Persist = Persist;
        definition.Labels = Labels;
        definition.Template = Template;

        definition.Placement = Placement;
        definition.Theme = Theme;
        definition.Direction = Direction;
        definition.Progress = Progress;
        definition.Interaction = Interaction;
        definition.ShowOverlay = ShowOverlay;
        definition.ShowSpotlight = ShowSpotlight;
        definition.ShowSkip = ShowSkip;
        definition.ShowClose = ShowClose;
        definition.ShowPrevious = ShowPrevious;
        definition.CloseOnEscape = CloseOnEscape;
        definition.CloseOnOverlayClick = CloseOnOverlayClick;

        definition.OnStarted = OnStarted.HasDelegate
            ? _ => new ValueTask(OnStarted.InvokeAsync())
            : null;

        definition.OnStepChanged = OnStepChanged.HasDelegate
            ? args => new ValueTask(OnStepChanged.InvokeAsync(args))
            : null;

        definition.OnEnded = OnEnded.HasDelegate
            ? args => new ValueTask(OnEnded.InvokeAsync(args))
            : null;

        definition.Steps.Clear();
        foreach (var step in _steps.OrderBy(step => step.SortKey))
        {
            definition.Steps.Add(step.Definition);
        }

        Configure?.Invoke(definition);
    }

    // ---- Imperative API -----------------------------------------------------

    /// <summary>Starts the tour.</summary>
    public Task<IBonboardingTourSession> StartAsync(BonboardingStartOptions? options = null, CancellationToken cancellationToken = default)
        => BonboardingService.StartAsync(Definition, options, cancellationToken);

    /// <summary>Starts the tour unless the user has already completed or dismissed it.</summary>
    public Task<IBonboardingTourSession?> StartOnceAsync(CancellationToken cancellationToken = default)
        => BonboardingService.StartOnceAsync(Definition, cancellationToken: cancellationToken);

    /// <summary>Clears saved progress and starts again from the first step.</summary>
    public Task<IBonboardingTourSession> RestartAsync(CancellationToken cancellationToken = default)
        => BonboardingService.RestartAsync(Definition, cancellationToken);

    /// <summary>Ends the tour if it is running.</summary>
    public Task StopAsync(BonboardingTourEndReason reason = BonboardingTourEndReason.Dismissed, CancellationToken cancellationToken = default)
    {
        if (Session is not { IsActive: true } session) return Task.CompletedTask;

        return reason switch
        {
            BonboardingTourEndReason.Completed => session.CompleteAsync(cancellationToken),
            BonboardingTourEndReason.Skipped => session.SkipAsync(cancellationToken),
            _ => session.DismissAsync(cancellationToken),
        };
    }

    public ValueTask DisposeAsync()
    {
        if (_disposed) return ValueTask.CompletedTask;
        _disposed = true;

        // Leave a running session alone: the tour may legitimately outlive the markup that
        // declared it, for instance while navigating between the pages of a multi-page tour.
        if (Session is not { IsActive: true })
        {
            BonboardingService.Unregister(Id);
        }

        return ValueTask.CompletedTask;
    }
}
