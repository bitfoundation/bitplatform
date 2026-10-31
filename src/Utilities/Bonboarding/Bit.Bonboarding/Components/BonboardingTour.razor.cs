using Microsoft.AspNetCore.Components;

namespace Bit.Bonboarding;

/// <summary>
/// Declares a tour in markup. Child <see cref="BonboardingStep"/> components become its steps, in
/// the order they are rendered.
/// </summary>
public sealed partial class BonboardingTour : ComponentBase, IAsyncDisposable
{
    private readonly List<BonboardingStep> _steps = [];
    private readonly Dictionary<string, int> _ordinals = new(StringComparer.Ordinal);

    private int _ordinalSource;

    private BonboardingTourDefinition? _definition;
    private bool _autoStarted;
    private bool _disposed;

    [Inject] private IServiceProvider Services { get; set; } = default!;

    // Resolved rather than injected, so the tour also registers when the app never called AddBitBonboardingServices.
    // Null until first used: DisposeAsync leaves alone what was never resolved.
    private BonboardingRuntime? _runtime;
    private BonboardingRuntime Runtime => _runtime ??= BonboardingRuntime.Of(Services);
    private IBonboardingService BonboardingService => Runtime.Service;
    private TimeProvider Time => Runtime.Time;

    /// <summary>Stable identifier, also used as the persistence key.</summary>
    [Parameter, EditorRequired] public required string Id { get; set; }

    /// <summary>Human-readable name, used as the dialog label when a step has no title.</summary>
    [Parameter] public string? Title { get; set; }

    /// <summary>What the tour shows, for a list of tours a user can pick from.</summary>
    [Parameter] public string? Description { get; set; }

    /// <summary>Gate evaluated before the tour starts; returning false keeps it from starting.</summary>
    [Parameter] public Func<BonboardingTourContext, ValueTask<bool>>? CanStart { get; set; }

    /// <summary>Arbitrary payload carried through to callbacks and analytics.</summary>
    [Parameter] public object? Data { get; set; }

    /// <summary>Bump this after editing the steps to discard stale saved progress.</summary>
    [Parameter] public int Version { get; set; } = 1;

    /// <summary>
    /// Whether the tour starts itself once its markup is on screen. An automatic start never interrupts a tour the
    /// user is taking: it queues behind it (see <see cref="BonboardingBusyBehavior.Queue"/>), and it passes over a
    /// tour the user snoozed.
    /// </summary>
    [Parameter] public BonboardingAutoStart AutoStart { get; set; } = BonboardingAutoStart.None;

    /// <summary>Order among queued automatic starts; see <see cref="BonboardingTourDefinition.Priority"/>.</summary>
    [Parameter] public int Priority { get; set; }

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
    [Parameter] public bool? ShowNext { get; set; }
    [Parameter] public double? Offset { get; set; }
    [Parameter] public double? OverlayOpacity { get; set; }
    [Parameter] public double? SpotlightPadding { get; set; }
    [Parameter] public double? SpotlightRadius { get; set; }
    [Parameter] public BonboardingSpotlightShape? SpotlightShape { get; set; }
    [Parameter] public BonboardingScrollMode? Scroll { get; set; }
    [Parameter] public BonboardingMissingTargetBehavior? MissingTarget { get; set; }
    [Parameter] public TimeSpan? WaitTimeout { get; set; }
    [Parameter] public BonboardingMissingTargetBehavior? WaitTimeoutBehavior { get; set; }
    [Parameter] public TimeSpan? AnimationDuration { get; set; }
    [Parameter] public int? ZIndex { get; set; }
    [Parameter] public string? PopoverClass { get; set; }
    [Parameter] public bool? KeyboardNavigation { get; set; }
    [Parameter] public bool? TrapFocus { get; set; }
    [Parameter] public bool? RestoreFocus { get; set; }
    [Parameter] public bool? AutoFocus { get; set; }

    /// <summary>Replaces the popover contents for every step that has no template of its own.</summary>
    [Parameter] public RenderFragment<BonboardingStepRenderContext>? Template { get; set; }

    /// <summary>
    /// Escape hatch for anything not exposed as a parameter. Runs after the parameters are applied
    /// and before the tour is registered.
    /// </summary>
    /// <remarks>
    /// The definition is rebuilt whenever the tour or one of its steps receives parameters, and
    /// this runs on every rebuild, so it must be idempotent: assign values rather than appending to
    /// collections or subscribing to events.
    /// </remarks>
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

    /// <summary>True while the tour has started and not ended, including while it is paused.</summary>
    private bool IsLive => Session is { IsActive: true } or { Status: BonboardingTourStatus.Paused };

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

        // Nobody asked for this start, so it waits for a tour already running rather than taking its place.
        var options = new BonboardingStartOptions { WhenBusy = BonboardingBusyBehavior.Queue };

        try
        {
            if (AutoStart == BonboardingAutoStart.Once)
            {
                await BonboardingService.StartOnceAsync(Definition, options);
            }
            else if (await BonboardingService.GetRecordAsync(Id) is not { } record || !record.IsSnoozedAt(Time.GetUtcNow()))
            {
                await BonboardingService.StartAsync(Definition, options);
            }
        }
        catch (BonboardingException)
        {
            // CanStart declined, or the definition is invalid (the service has logged why). An automatic
            // start has no caller to report that to, and an exception escaping a lifecycle method would
            // take the whole circuit down.
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

        // Blazor disposes a parent before its children, so once the tour itself is gone the steps
        // are going with the page rather than being removed from the tour. A session that outlives
        // the page (a multi-page tour) still needs the full step list on the shared definition.
        if (_disposed) return;

        Rebuild();
    }

    /// <summary>
    /// Hands out the fallback sort key for a step without an explicit <see cref="BonboardingStep.Order"/>.
    /// A step that is removed and rendered again (inside an <c>@if</c>, say) gets its original key
    /// back by id, so it returns to its declared position instead of moving to the end.
    /// </summary>
    internal int AssignOrdinal(string? stepId)
    {
        if (stepId is null) return ++_ordinalSource;

        if (!_ordinals.TryGetValue(stepId, out var ordinal))
        {
            ordinal = ++_ordinalSource;
            _ordinals[stepId] = ordinal;
        }

        return ordinal;
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
        definition.Description = Description;
        definition.CanStart = CanStart;
        definition.Data = Data;
        definition.Priority = Priority;
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
        definition.ShowNext = ShowNext;
        definition.Offset = Offset;
        definition.OverlayOpacity = OverlayOpacity;
        definition.SpotlightPadding = SpotlightPadding;
        definition.SpotlightRadius = SpotlightRadius;
        definition.SpotlightShape = SpotlightShape;
        definition.Scroll = Scroll;
        definition.MissingTarget = MissingTarget;
        definition.WaitTimeout = WaitTimeout;
        definition.WaitTimeoutBehavior = WaitTimeoutBehavior;
        definition.AnimationDuration = AnimationDuration;
        definition.ZIndex = ZIndex;
        definition.PopoverClass = PopoverClass;
        definition.KeyboardNavigation = KeyboardNavigation;
        definition.TrapFocus = TrapFocus;
        definition.RestoreFocus = RestoreFocus;
        definition.AutoFocus = AutoFocus;

        // Marshalled onto the renderer: the session can reach these from a browser callback or from
        // code running off the renderer, and an EventCallback re-renders the component that owns
        // it, which Blazor Server only allows on that component's dispatcher.
        definition.OnStarted = OnStarted.HasDelegate
            ? _ => new ValueTask(InvokeAsync(() => OnStarted.InvokeAsync()))
            : null;

        definition.OnStepChanged = OnStepChanged.HasDelegate
            ? args => new ValueTask(InvokeAsync(() => OnStepChanged.InvokeAsync(args)))
            : null;

        definition.OnEnded = OnEnded.HasDelegate
            ? args => new ValueTask(InvokeAsync(() => OnEnded.InvokeAsync(args)))
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
        if (!IsLive || Session is not { } session) return Task.CompletedTask;

        return reason switch
        {
            BonboardingTourEndReason.Completed => session.CompleteAsync(cancellationToken),
            BonboardingTourEndReason.Skipped => session.SkipAsync(cancellationToken),
            BonboardingTourEndReason.Snoozed => session.SnoozeAsync(BonboardingDefaults.SnoozeDuration, cancellationToken),
            BonboardingTourEndReason.Cancelled => session.CancelAsync(cancellationToken),
            _ => session.DismissAsync(cancellationToken),
        };
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        // Nothing was registered when the services were never reached - resolving them failed, say.
        if (_runtime is null) return;

        // Leave a running session alone: the tour may legitimately outlive the markup that
        // declared it, for instance while navigating between the pages of a multi-page tour.
        if (IsLive) return;

        BonboardingService.Unregister(Id);

        // An automatic start still waiting for its turn was for this page; the page is gone.
        if (Session is { Status: BonboardingTourStatus.Idle } waiting)
        {
            await waiting.CancelAsync();
        }
    }
}
