using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.Extensions.Logging;

namespace Bit.Bonboarding;

/// <summary>
/// The tour state machine. Owns step selection, branching, waiting, persistence and the
/// conversation with the browser layer. It renders nothing: the UI observes
/// <see cref="Changed"/> and reads the published state.
/// </summary>
/// <remarks>
/// Every public command funnels through <see cref="RunGuardedAsync"/>, which holds a single
/// transition lock. That is what makes rapid clicking, a resize storm and a route change arriving
/// at the same moment safe: they queue instead of interleaving. Long-running work inside a
/// transition also bumps <c>_generation</c>, so an await that resumes after the user has already
/// moved on can detect it lost the race and abandon its results.
/// <para>
/// Awaits here deliberately do not use <c>ConfigureAwait(false)</c>. Application hooks run after
/// them, and on Blazor Server a hook that touches a component (an <c>EventCallback</c>, a
/// <c>StateHasChanged</c>) has to run on the renderer's dispatcher, which is the context commands
/// arrive on.
/// </para>
/// </remarks>
internal sealed class BonboardingTourSession : IBonboardingTourSession, IBonboardingInteropCallbacks, IDisposable, IAsyncDisposable
{
    private readonly BonboardingOptions _globalOptions;
    private readonly IBonboardingInterop _interop;
    private readonly IBonboardingStore _store;
    private readonly IBonboardingLocalizer _localizer;
    private readonly NavigationManager? _navigation;
    private readonly IServiceProvider _services;
    private readonly ILogger _logger;
    private readonly Func<BonboardingEvent, ValueTask> _publish;

    /// <summary>Serialises transitions so two commands can never interleave.</summary>
    private readonly SemaphoreSlim _gate = new(1, 1);

    /// <summary>
    /// The application hook the current async flow is running inside, if any. Hooks are awaited
    /// while <see cref="_gate"/> is held and are entirely within their rights to call back into the
    /// session (a validation handler that dismisses the tour, say), so those nested calls run inline
    /// instead of waiting for a lock their own caller is holding. See <see cref="HookScope"/> for how
    /// work a hook starts in the background is kept from running alongside the transition.
    /// </summary>
    private readonly AsyncLocal<HookScope?> _hookScope = new();

    /// <summary>Where the current step was entered from, for the hooks that run once its target appears.</summary>
    private BonboardingStepDefinition? _enteredFrom;
    private BonboardingStepChangeReason _enteredBy;

    /// <summary>
    /// True once the current step's show hooks have run, so a target that is lost and found again
    /// does not report the step as shown a second time.
    /// </summary>
    private bool _stepShown;

    /// <summary>
    /// True once the current step got as far as <see cref="BonboardingStepDefinition.OnBeforeShow"/>,
    /// so <see cref="BonboardingStepDefinition.OnBeforeLeave"/> runs exactly for the steps whose show
    /// hook ran, including one that was then skipped because its target was missing.
    /// </summary>
    private bool _stepPrepared;

    private readonly List<string> _history = [];
    private readonly Dictionary<string, object?> _state = [];

    /// <summary>Indices of the steps whose conditions currently pass, in order.</summary>
    private List<int> _plan = [];

    private CancellationTokenSource? _stepCts;
    private BonboardingBrowserEnvironment _environment = BonboardingBrowserEnvironment.Unknown;

    private BonboardingStepGeometry? _geometry;
    private long _lastSequence = -1;
    private bool _attached;
    private bool _navigationHooked;
    private bool _disposed;
    private bool _focusCaptured;
    private DateTimeOffset _stepShownAt;
    private string? _pendingRoute;
    private BonboardingStepTarget _resolvedTarget = BonboardingStepTarget.None;

    /// <summary>Bumped on every transition so stale async work can detect it lost the race.</summary>
    private int _generation;

    public BonboardingTourSession(
        BonboardingTourDefinition tour,
        BonboardingOptions globalOptions,
        IBonboardingInterop interop,
        IBonboardingStore store,
        IBonboardingLocalizer localizer,
        NavigationManager? navigation,
        IServiceProvider services,
        ILogger logger,
        Func<BonboardingEvent, ValueTask> publish)
    {
        Tour = tour;
        _globalOptions = globalOptions;
        _interop = interop;
        _store = store;
        _localizer = localizer;
        _navigation = navigation;
        _services = services;
        _logger = logger;
        _publish = publish;

        CurrentOptions = ResolveOptions(null);
    }

    public string Id { get; } = $"bo-{Guid.NewGuid():N}";

    public BonboardingTourDefinition Tour { get; }

    public BonboardingTourStatus Status { get; private set; } = BonboardingTourStatus.Idle;

    public BonboardingStepDefinition? CurrentStep { get; private set; }

    public int CurrentIndex { get; private set; } = -1;

    public BonboardingEffectiveStepOptions? CurrentOptions { get; private set; }

    public BonboardingRect? TargetRect => _geometry is { Found: true } geometry && !geometry.Target.IsEmpty ? geometry.Target : null;

    public BonboardingPlacementResult? Placement { get; private set; }

    public bool HasTarget => TargetRect is not null;

    public bool IsWaiting => Status == BonboardingTourStatus.Waiting;

    public bool IsActive => Status is BonboardingTourStatus.Running or BonboardingTourStatus.Waiting;

    /// <summary>True while the tour has started and not yet ended, paused or not.</summary>
    internal bool IsLive => IsActive || Status == BonboardingTourStatus.Paused;

    public IReadOnlyList<string> History => _history;

    public IDictionary<string, object?> State => _state;

    public BonboardingTourEndReason? EndReason { get; private set; }

    public Exception? Error { get; private set; }

    /// <summary>True while the tour has stopped for good.</summary>
    public bool IsFinished => Status
        is BonboardingTourStatus.Completed or BonboardingTourStatus.Dismissed or BonboardingTourStatus.Skipped
        or BonboardingTourStatus.Cancelled or BonboardingTourStatus.Failed;

    public int DisplayCount => _plan.Count > 0 ? _plan.Count : (CurrentIndex >= 0 ? 1 : 0);

    public int DisplayPosition
    {
        get
        {
            if (CurrentIndex < 0) return 0;
            var position = _plan.IndexOf(CurrentIndex);
            return position >= 0 ? position + 1 : Math.Min(Math.Max(_history.Count, 1), DisplayCount);
        }
    }

    public bool IsFirstStep => CurrentStep?.ResolvePrevious is null && _history.Count <= 1;

    public bool IsLastStep
    {
        get
        {
            if (CurrentStep is null) return true;

            // A branching step cannot be known to be last without running user code, so the
            // forward button keeps saying "Next" and the hook decides at click time.
            if (CurrentStep.ResolveNext is not null) return false;

            var position = _plan.IndexOf(CurrentIndex);
            return position < 0 ? CurrentIndex >= Tour.Steps.Count - 1 : position >= _plan.Count - 1;
        }
    }

    public event EventHandler<BonboardingTourSessionChangedEventArgs>? Changed;

    // ---- Start -------------------------------------------------------------

    public Task StartAsync(CancellationToken cancellationToken = default)
        => StartAsync(new BonboardingStartOptions(), cancellationToken);

    /// <summary>Applies <see cref="BonboardingStartOptions"/> and shows the first eligible step.</summary>
    public Task StartAsync(BonboardingStartOptions options, CancellationToken cancellationToken = default)
        => RunGuardedAsync(async ct =>
        {
            if (Status != BonboardingTourStatus.Idle) return;

            if (options.State is not null)
            {
                foreach (var (key, value) in options.State) _state[key] = value;
            }

            _environment = await SafeInitializeAsync(ct);
            CurrentOptions = ResolveOptions(null);

            await SafeAttachAsync(ct);
            HookNavigation();

            var (startIndex, resumed) = await DetermineStartIndexAsync(options, ct);

            Status = BonboardingTourStatus.Running;

            if (Tour.OnStarted is { } onStarted)
            {
                await InvokeHookAsync(() => onStarted(new BonboardingTourContext(Tour, _services, this)));
            }

            // Started comes first so an analytics stream always opens with it; resumed qualifies it.
            await PublishAsync(BonboardingEventKind.TourStarted);
            if (resumed) await PublishAsync(BonboardingEventKind.TourResumed);

            await MoveToAsync(startIndex, BonboardingStepChangeReason.Start, +1, ct);
        }, cancellationToken);

    /// <summary>Picks the first step, and reports whether that means resuming saved progress.</summary>
    private async Task<(int Index, bool Resumed)> DetermineStartIndexAsync(BonboardingStartOptions options, CancellationToken cancellationToken)
    {
        if (options.StartAtStepId is { } stepId)
        {
            var explicitStep = Tour.IndexOf(stepId);
            if (explicitStep >= 0) return (explicitStep, false);
        }

        if (options.StartAtIndex is { } explicitIndex)
        {
            return (Math.Clamp(explicitIndex, 0, Math.Max(0, Tour.Steps.Count - 1)), false);
        }

        if (!options.Resume || !ShouldPersist) return (0, false);

        var record = await SafeGetRecordAsync(cancellationToken);
        if (record is null || record.IsClosed || record.Version != Tour.Version) return (0, false);

        // Prefer the id: indices shift as soon as a step is inserted.
        var resumeIndex = record.CurrentStepId is null ? -1 : Tour.IndexOf(record.CurrentStepId);
        if (resumeIndex < 0) resumeIndex = Math.Clamp(record.CurrentIndex, 0, Math.Max(0, Tour.Steps.Count - 1));

        if (resumeIndex > 0)
        {
            _history.Clear();
            // Keep only history entries that still exist, so Back cannot land on a deleted step.
            _history.AddRange(record.VisitedStepIds.Where(id => Tour.IndexOf(id) >= 0));
            if (_history.Count > 0 && _history[^1] == Tour.Steps[resumeIndex].Id)
            {
                _history.RemoveAt(_history.Count - 1);
            }
        }

        return (resumeIndex, resumeIndex > 0);
    }

    // ---- Public commands ---------------------------------------------------

    public Task NextAsync(CancellationToken cancellationToken = default)
        => RunGuardedAsync(NextCoreAsync, cancellationToken);

    public Task PreviousAsync(CancellationToken cancellationToken = default)
        => RunGuardedAsync(PreviousCoreAsync, cancellationToken);

    /// <remarks>
    /// An unknown id is a mistake in the calling code, so it is reported to that code rather than
    /// failing the user's tour. The id is checked again once the transition lock is held, because a
    /// declarative tour can drop the step in between; that rare case ends the tour as failed.
    /// </remarks>
    public Task GoToAsync(string stepId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stepId);
        if (Tour.IndexOf(stepId) < 0) throw StepNotFound(stepId);

        return RunGuardedAsync(ct => GoToCoreAsync(stepId, ct), cancellationToken);
    }

    /// <remarks>
    /// An out-of-range index is reported to the caller. One that only becomes out of range while the
    /// command waits for the transition lock is ignored.
    /// </remarks>
    public Task GoToAsync(int index, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, Tour.Steps.Count);

        return RunGuardedAsync(
            ct => index < Tour.Steps.Count
                ? MoveToAsync(index, BonboardingStepChangeReason.Jump, +1, ct)
                : Task.CompletedTask,
            cancellationToken);
    }

    public Task SkipAsync(CancellationToken cancellationToken = default)
        => RunGuardedAsync(ct => EndAsync(BonboardingTourEndReason.Skipped, ct), cancellationToken);

    public Task CompleteAsync(CancellationToken cancellationToken = default)
        => RunGuardedAsync(ct => EndAsync(BonboardingTourEndReason.Completed, ct), cancellationToken);

    public Task DismissAsync(CancellationToken cancellationToken = default)
        => RunGuardedAsync(ct => EndAsync(BonboardingTourEndReason.Dismissed, ct), cancellationToken);

    public Task CancelAsync(CancellationToken cancellationToken = default)
        => RunGuardedAsync(ct => EndAsync(BonboardingTourEndReason.Cancelled, ct), cancellationToken);

    public Task PauseAsync(CancellationToken cancellationToken = default)
        => RunGuardedAsync(async _ =>
        {
            if (Status is not (BonboardingTourStatus.Running or BonboardingTourStatus.Waiting)) return;

            Status = BonboardingTourStatus.Paused;
            await SafeDeactivateAsync();
            await PublishAsync(BonboardingEventKind.TourPaused);
            Notify(BonboardingTourSessionChange.Step);
        }, cancellationToken);

    public Task ResumeAsync(CancellationToken cancellationToken = default)
        => RunGuardedAsync(async ct =>
        {
            if (Status != BonboardingTourStatus.Paused || CurrentIndex < 0) return;

            Status = BonboardingTourStatus.Running;
            await MoveToAsync(CurrentIndex, BonboardingStepChangeReason.Resume, +1, ct);
        }, cancellationToken);

    public Task RefreshAsync(CancellationToken cancellationToken = default)
        => RunGuardedAsync(_ => RefreshCoreAsync(), cancellationToken);

    private async Task RefreshCoreAsync()
    {
        if (CurrentStep is null || Status != BonboardingTourStatus.Running) return;

        var geometry = await SafeMeasureAsync();
        if (geometry is null) return;

        ApplyGeometryIfNewer(geometry);
        Notify(BonboardingTourSessionChange.Geometry);
    }

    public Task InvokeActionAsync(BonboardingStepAction action, CancellationToken cancellationToken = default)
        => RunGuardedAsync(async ct =>
        {
            if (CurrentStep is null) return;

            var step = CurrentStep;

            if (action.OnClick is { } onClick)
            {
                await InvokeHookAsync(() => onClick(CreateContext(step)));
            }

            await PublishAsync(BonboardingEventKind.ActionInvoked, step, action);

            // The handler is allowed to end the tour or navigate itself.
            if (!IsActive || !ReferenceEquals(CurrentStep, step)) return;

            switch (action.Effect)
            {
                case BonboardingStepActionEffect.Next:
                    await NextCoreAsync(ct);
                    break;
                case BonboardingStepActionEffect.Previous:
                    await PreviousCoreAsync(ct);
                    break;
                case BonboardingStepActionEffect.GoTo when action.TargetStepId is { } targetId:
                    await GoToCoreAsync(targetId, ct);
                    break;
                case BonboardingStepActionEffect.Complete:
                    await EndAsync(BonboardingTourEndReason.Completed, ct);
                    break;
                case BonboardingStepActionEffect.Skip:
                    await EndAsync(BonboardingTourEndReason.Skipped, ct);
                    break;
                case BonboardingStepActionEffect.Dismiss:
                    await EndAsync(BonboardingTourEndReason.Dismissed, ct);
                    break;
            }
        }, cancellationToken);

    // ---- Command cores (transition lock already held) ----------------------

    private async Task NextCoreAsync(CancellationToken cancellationToken)
    {
        if (CurrentStep is null || IsFinished) return;

        var step = CurrentStep;
        var context = CreateContext(step);

        if (step.CanAdvance is { } canAdvance)
        {
            if (!await InvokeHookAsync(() => canAdvance(context))) return;

            // The hook is allowed to end the tour or navigate itself.
            if (IsFinished || !ReferenceEquals(CurrentStep, step)) return;
        }

        if (step.ResolveNext is { } resolveNext)
        {
            var nextId = await InvokeHookAsync(() => resolveNext(context));
            if (IsFinished || !ReferenceEquals(CurrentStep, step)) return;

            if (nextId == BonboardingTourDefinition.EndStepId)
            {
                await EndAsync(BonboardingTourEndReason.Completed, cancellationToken);
                return;
            }

            if (nextId is not null)
            {
                await GoToCoreAsync(nextId, cancellationToken);
                return;
            }
        }

        await MoveToAsync(null, BonboardingStepChangeReason.Next, +1, cancellationToken);
    }

    private async Task PreviousCoreAsync(CancellationToken cancellationToken)
    {
        if (CurrentStep is null || IsFinished) return;

        var step = CurrentStep;

        if (step.ResolvePrevious is { } resolvePrevious)
        {
            var previousId = await InvokeHookAsync(() => resolvePrevious(CreateContext(step)));
            if (IsFinished || !ReferenceEquals(CurrentStep, step)) return;

            if (previousId is not null && Tour.IndexOf(previousId) is var explicitIndex and >= 0)
            {
                await MoveToAsync(explicitIndex, BonboardingStepChangeReason.Previous, -1, cancellationToken);
                return;
            }
        }

        // The visit history is authoritative: with branching, "the step before this one" is
        // whatever the user actually saw, not the previous array element. Without one, fall back
        // to the previous eligible step. MoveToAsync unwinds the history once the move is certain.
        var visited = PreviousVisitedIndex();
        await MoveToAsync(visited >= 0 ? visited : null, BonboardingStepChangeReason.Previous, -1, cancellationToken);
    }

    /// <summary>Index of the step the user saw before the current one, or <c>-1</c>.</summary>
    private int PreviousVisitedIndex()
    {
        var end = _history.Count;
        if (end > 0 && CurrentStep is not null && _history[^1] == CurrentStep.Id) end--;

        return end > 0 ? Tour.IndexOf(_history[end - 1]) : -1;
    }

    private Task GoToCoreAsync(string stepId, CancellationToken cancellationToken)
    {
        var index = Tour.IndexOf(stepId);
        if (index < 0) throw StepNotFound(stepId);

        return MoveToAsync(index, BonboardingStepChangeReason.Jump, +1, cancellationToken);
    }

    private BonboardingException StepNotFound(string stepId)
        => new($"Step '{stepId}' was not found in tour '{Tour.Id}'.")
        {
            TourId = Tour.Id,
            StepId = stepId,
        };

    // ---- The transition itself --------------------------------------------

    /// <summary>
    /// Performs one transition. Pass <see langword="null"/> for <paramref name="requestedIndex"/>
    /// to mean "whichever step comes next in <paramref name="direction"/>": the destination is then
    /// resolved after the leave hooks have run, so a hook that changes the state a condition
    /// depends on is reflected in where the user actually lands.
    /// </summary>
    private async Task MoveToAsync(int? requestedIndex, BonboardingStepChangeReason reason, int direction, CancellationToken cancellationToken)
    {
        if (_disposed || IsFinished) return;

        var previousStep = CurrentStep;
        var previousShown = _stepShown;
        var isReentry = reason is BonboardingStepChangeReason.Resume or BonboardingStepChangeReason.Retarget;
        var leaving = previousStep is not null && !isReentry;

        // Going back with nowhere to go is a no-op. That is decided before any leave hook runs, so
        // the user is never left on a step whose hooks have already torn it down.
        if (leaving && direction < 0)
        {
            await RebuildPlanAsync();
            if (ResolveDestination(requestedIndex, direction) < 0) return;
        }

        if (leaving)
        {
            // Leave hooks pair with show hooks: a step still waiting for its route never ran
            // OnBeforeShow and has nothing to undo, while one that was skipped for a missing target
            // did and may have opened something to reveal it.
            if (_stepPrepared && previousStep!.OnBeforeLeave is { } beforeLeave)
            {
                await InvokeHookAsync(() => beforeLeave(CreateContext(previousStep)));

                // The hook is allowed to end the tour or navigate itself.
                if (IsFinished || !ReferenceEquals(CurrentStep, previousStep)) return;
            }

            // Only a step that was actually on screen can report how long it was there.
            if (previousShown)
            {
                await PublishAsync(BonboardingEventKind.StepLeft, previousStep, duration: DateTimeOffset.UtcNow - _stepShownAt);
            }
        }

        await RebuildPlanAsync();

        var index = ResolveDestination(requestedIndex, direction);

        if (index < 0)
        {
            if (direction > 0)
            {
                // Nothing eligible ahead: the tour is over.
                await EndAsync(BonboardingTourEndReason.Completed, cancellationToken);
            }
            else if (leaving)
            {
                // A leave hook changed what is eligible and took away the step to go back to.
                // Its leave hooks have run, so show the step again rather than leave it torn down.
                await MoveToAsync(CurrentIndex, BonboardingStepChangeReason.Retarget, +1, cancellationToken);
            }

            return;
        }

        var generation = ++_generation;
        var step = Tour.Steps[index];

        if (!isReentry)
        {
            // Back only ever lands on steps the user actually saw: moving back unwinds the current
            // step's entry, and a step that never made it on screen leaves none behind.
            if (previousStep is not null && (direction < 0 || !previousShown)
                && _history.Count > 0 && _history[^1] == previousStep.Id)
            {
                _history.RemoveAt(_history.Count - 1);
            }

            // Steps being backed past that are no longer eligible are not somewhere to return to.
            if (direction < 0)
            {
                while (_history.Count > 0 && _history[^1] != step.Id && !_plan.Contains(Tour.IndexOf(_history[^1])))
                {
                    _history.RemoveAt(_history.Count - 1);
                }
            }

            // A step that never made it on screen is not where the user came from, so the hooks
            // keep pointing at the last step they actually saw.
            if (previousShown || previousStep is null) _enteredFrom = previousStep;

            _enteredBy = reason;
            _stepPrepared = false;
        }
        else if (_stepShown)
        {
            // Re-entering the same step keeps where it was originally entered from. A step that
            // was never shown (it was still waiting for its route) reports the transition that led
            // to it; one that was already on screen reports the re-entry itself.
            _enteredBy = reason;
        }

        _stepShown = false;

        CancelStep();
        _stepCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var stepToken = _stepCts.Token;

        await SafeDeactivateAsync();

        CurrentStep = step;
        CurrentIndex = index;
        CurrentOptions = ResolveOptions(step);
        _geometry = null;
        _lastSequence = -1;
        Placement = null;

        if (_history.Count == 0 || _history[^1] != step.Id) _history.Add(step.Id);

        Notify(BonboardingTourSessionChange.Step);

        // Route handling comes first: the element cannot exist until the right page is rendered.
        if (!await EnsureRouteAsync(step)) return;

        var target = step.Target;
        _resolvedTarget = target.IsDynamic
            ? await InvokeHookAsync(() => target.ResolveAsync(CreateContext(step, stepToken)))
            : target;
        if (generation != _generation) return;

        _stepPrepared = true;

        if (step.OnBeforeShow is { } beforeShow)
        {
            await InvokeHookAsync(() => beforeShow(CreateContext(step, stepToken)));
            if (generation != _generation) return;
        }

        var options = CurrentOptions!;
        var wantsTarget = !_resolvedTarget.IsNone;
        var geometry = await ActivateInBrowserAsync(step, options, stepToken);

        if (generation != _generation) return;

        if (wantsTarget && geometry is { Found: false })
        {
            await PublishAsync(BonboardingEventKind.TargetMissing, step);

            // The command's token, not the step's: skipping starts a new transition, which cancels
            // this step's token as its first act.
            if (await HandleMissingTargetAsync(options.MissingTarget, step, direction, cancellationToken))
            {
                return;
            }
        }

        // Applied for a centred fallback too: the browser may already have reported this exact
        // frame, in which case it will not send it again and the card would never be placed. A
        // frame of this activation that overtook the reply is newer, and is kept.
        if (geometry is not null)
        {
            ApplyGeometryIfNewer(geometry);
            if (geometry.Found) await PublishAsync(BonboardingEventKind.TargetResolved, step);
        }

        Status = BonboardingTourStatus.Running;
        _stepShownAt = DateTimeOffset.UtcNow;

        Notify(BonboardingTourSessionChange.Step);

        await CompleteShowAsync(step, index, _enteredFrom, _enteredBy, stepToken);
    }

    /// <summary>The part of showing a step that runs once it is actually on screen.</summary>
    private async Task CompleteShowAsync(
        BonboardingStepDefinition step, int index, BonboardingStepDefinition? previousStep,
        BonboardingStepChangeReason reason, CancellationToken stepToken)
    {
        _stepShown = true;

        await PersistAsync(stepToken);
        await PublishAsync(BonboardingEventKind.StepShown, step);

        if (Tour.OnStepChanged is { } onStepChanged)
        {
            await InvokeHookAsync(() => onStepChanged(new BonboardingStepChangedEventArgs(this, step, index, previousStep, reason)));
        }

        // OnStepChanged is allowed to end the tour or navigate itself.
        if (step.OnShown is { } onShown && !IsFinished && ReferenceEquals(CurrentStep, step))
        {
            await InvokeHookAsync(() => onShown(CreateContext(step, stepToken)));
        }
    }

    /// <summary>
    /// Returns false when the step cannot proceed yet because the route does not match. The
    /// transition then resumes from <see cref="HandleLocationChangedAsync"/>.
    /// </summary>
    private async Task<bool> EnsureRouteAsync(BonboardingStepDefinition step)
    {
        _pendingRoute = null;

        if (step.Route is null || _navigation is null) return true;

        var current = _navigation.ToBaseRelativePath(_navigation.Uri);
        if (BonboardingRouteMatcher.Matches(step.Route, current)) return true;

        _pendingRoute = step.Route;
        Status = BonboardingTourStatus.Waiting;
        Notify(BonboardingTourSessionChange.Step);

        // A wildcard route is a constraint rather than a destination, so there is nowhere to go.
        if (_globalOptions.AutoNavigateToStepRoute && !step.Route.EndsWith('*'))
        {
            // Navigating runs LocationChanged handlers, the application's and this session's own,
            // and a router may do so before NavigateTo returns. Called like a hook, so a location
            // change raised from inside the call resumes this step straight away instead of
            // queueing behind the transition that caused it.
            var route = step.Route;
            await InvokeHookAsync(() =>
            {
                _navigation.NavigateTo(route);
                return ValueTask.CompletedTask;
            });
        }

        return false;
    }

    private async Task<BonboardingStepGeometry?> ActivateInBrowserAsync(
        BonboardingStepDefinition step, BonboardingEffectiveStepOptions options, CancellationToken cancellationToken)
    {
        if (!_interop.IsAvailable) return null;

        var activation = new BonboardingStepActivation
        {
            SessionId = Id,
            Target = BonboardingTargetDescriptor.From(_resolvedTarget),
            AdditionalTargets = BuildAdditionalDescriptors(step),
            WaitForTarget = options.MissingTarget == BonboardingMissingTargetBehavior.Wait,
            WaitTimeoutMs = (int)options.WaitTimeout.TotalMilliseconds,
            Scroll = options.Scroll switch
            {
                BonboardingScrollMode.None => "none",
                BonboardingScrollMode.Instant => "instant",
                BonboardingScrollMode.Auto => "auto",
                _ => "smooth",
            },
            ScrollPadding = options.ScrollPadding,
            Interaction = options.Interaction switch
            {
                BonboardingInteractionMode.Free => "free",
                BonboardingInteractionMode.TargetOnly => "target",
                _ => "blocked",
            },
            CloseOnEscape = options.CloseOnEscape,
            KeyboardNavigation = options.KeyboardNavigation,
            TrapFocus = options.TrapFocus,
            AdvanceOn = step.AdvanceOn is { } trigger
                ? new BonboardingAdvanceTriggerDescriptor
                {
                    EventName = trigger.EventName,
                    Selector = trigger.Selector,
                    MatchSelector = trigger.MatchSelector,
                    DelayMs = trigger.DelayMs,
                }
                : null,
        };

        try
        {
            return await _interop.ActivateStepAsync(activation, cancellationToken);
        }
        catch (Exception ex) when (BonboardingInteropFailure.IsTransient(ex))
        {
            // Circuit disconnects and prerender teardown must never take the tour down with them.
            _logger.Log(BonboardingInteropFailure.LevelFor(ex), ex, "BonboardingService step activation could not reach the browser.");
            return null;
        }
    }

    private IReadOnlyList<BonboardingTargetDescriptor>? BuildAdditionalDescriptors(BonboardingStepDefinition step)
    {
        if (step.AdditionalTargets is not { Count: > 0 } extra) return null;

        var list = new List<BonboardingTargetDescriptor>(extra.Count);
        foreach (var target in extra)
        {
            // Dynamic extra targets are not supported; they would need a second resolution pass
            // for a case that has not come up in practice.
            if (target.IsDynamic) continue;

            var descriptor = BonboardingTargetDescriptor.From(target);
            if (descriptor.Kind != BonboardingTargetKind.None) list.Add(descriptor);
        }

        return list.Count == 0 ? null : list;
    }

    /// <summary>Returns true when the step was handled elsewhere and this transition must stop.</summary>
    private async Task<bool> HandleMissingTargetAsync(
        BonboardingMissingTargetBehavior behavior, BonboardingStepDefinition step, int direction, CancellationToken cancellationToken)
    {
        switch (behavior)
        {
            case BonboardingMissingTargetBehavior.Skip:
            {
                await PublishAsync(BonboardingEventKind.StepSkipped, step);

                // Skipping backwards past the first eligible step leaves nowhere to go back to, so
                // carry on forwards rather than strand the user on a step that cannot be shown.
                var turnedAround = false;
                if (direction < 0)
                {
                    await RebuildPlanAsync();
                    if (PreviousPlanned(CurrentIndex) < 0)
                    {
                        direction = +1;
                        turnedAround = true;
                    }
                }

                // A step that never made it on screen passes on the reason the user set off for it,
                // so the step they land on reports how they really got there.
                var reason = _stepShown || turnedAround
                    || _enteredBy is BonboardingStepChangeReason.Resume or BonboardingStepChangeReason.Retarget
                        ? direction > 0 ? BonboardingStepChangeReason.Next : BonboardingStepChangeReason.Previous
                        : _enteredBy;

                await MoveToAsync(null, reason, direction, cancellationToken);
                return true;
            }

            case BonboardingMissingTargetBehavior.Fail:
                throw new BonboardingException($"Target for step '{step.Id}' of tour '{Tour.Id}' was not found.")
                {
                    TourId = Tour.Id,
                    StepId = step.Id,
                };

            case BonboardingMissingTargetBehavior.Wait:
                // The browser layer is watching for the element; show the waiting state until it
                // reports back or the wait times out.
                Status = BonboardingTourStatus.Waiting;
                Notify(BonboardingTourSessionChange.Step);
                return true;

            default:
                // Center: drop the target and fall through to render a modal card.
                _resolvedTarget = BonboardingStepTarget.None;
                return false;
        }
    }

    // ---- Step eligibility --------------------------------------------------

    /// <summary>
    /// Re-evaluates every step condition. Conditions are expected to be cheap and free of side
    /// effects, because progress display and lookahead both depend on asking them repeatedly.
    /// </summary>
    private async Task RebuildPlanAsync()
    {
        var plan = new List<int>(Tour.Steps.Count);

        for (var i = 0; i < Tour.Steps.Count; i++)
        {
            var step = Tour.Steps[i];
            var index = i;

            if (step.When is not { } when || await InvokeHookAsync(() => when(CreateContext(step, index: index))))
            {
                plan.Add(i);
            }
        }

        _plan = plan;
    }

    /// <summary>
    /// Where a transition lands: the requested step (snapped onto an eligible one), or the next
    /// eligible step in the direction of travel. <c>-1</c> when there is none.
    /// </summary>
    private int ResolveDestination(int? requestedIndex, int direction)
        => requestedIndex is { } explicitIndex
            ? ResolveEligible(explicitIndex, direction)
            : direction > 0 ? NextPlanned(CurrentIndex) : PreviousPlanned(CurrentIndex);

    /// <summary>Snaps a requested index onto the nearest planned step in the direction of travel.</summary>
    private int ResolveEligible(int requestedIndex, int direction)
    {
        if (_plan.Count == 0) return -1;
        if (_plan.Contains(requestedIndex)) return requestedIndex;

        if (direction >= 0)
        {
            foreach (var candidate in _plan)
            {
                if (candidate >= requestedIndex) return candidate;
            }

            return -1;
        }

        for (var i = _plan.Count - 1; i >= 0; i--)
        {
            if (_plan[i] <= requestedIndex) return _plan[i];
        }

        return -1;
    }

    private int NextPlanned(int fromIndex)
    {
        foreach (var index in _plan)
        {
            if (index > fromIndex) return index;
        }

        return -1;
    }

    private int PreviousPlanned(int fromIndex)
    {
        for (var i = _plan.Count - 1; i >= 0; i--)
        {
            if (_plan[i] < fromIndex) return _plan[i];
        }

        return -1;
    }

    // ---- Ending ------------------------------------------------------------

    private async Task EndAsync(BonboardingTourEndReason reason, CancellationToken cancellationToken)
    {
        if (IsFinished) return;

        var lastStep = CurrentStep;
        var lastIndex = CurrentIndex;

        // Only a step that was actually on screen can report how long it was there.
        if (lastStep is not null && _stepShown)
        {
            await PublishAsync(BonboardingEventKind.StepLeft, lastStep, duration: DateTimeOffset.UtcNow - _stepShownAt);
        }

        Status = reason switch
        {
            BonboardingTourEndReason.Completed => BonboardingTourStatus.Completed,
            BonboardingTourEndReason.Skipped => BonboardingTourStatus.Skipped,
            BonboardingTourEndReason.Failed => BonboardingTourStatus.Failed,
            BonboardingTourEndReason.Cancelled => BonboardingTourStatus.Cancelled,
            _ => BonboardingTourStatus.Dismissed,
        };

        EndReason = reason;
        _pendingRoute = null;
        CurrentStep = null;
        Placement = null;
        _geometry = null;

        _generation++;
        CancelStep();
        await SafeDeactivateAsync();
        await RestoreFocusAsync();
        UnhookNavigation();

        await PersistEndAsync(reason, lastStep, lastIndex);

        await PublishAsync(
            reason switch
            {
                BonboardingTourEndReason.Completed => BonboardingEventKind.TourCompleted,
                BonboardingTourEndReason.Skipped => BonboardingEventKind.TourSkipped,
                BonboardingTourEndReason.Failed => BonboardingEventKind.TourFailed,
                BonboardingTourEndReason.Cancelled => BonboardingEventKind.TourCancelled,
                _ => BonboardingEventKind.TourDismissed,
            },
            lastStep,
            reason: reason);

        if (Tour.OnEnded is { } onEnded)
        {
            await InvokeHookAsync(() => onEnded(new BonboardingTourEndedEventArgs(this, reason, lastStep, lastIndex, Error)));
        }

        if (_attached)
        {
            _attached = false;
            await SafeDetachAsync();
        }

        Notify(BonboardingTourSessionChange.Step | BonboardingTourSessionChange.Ended);
    }

    // ---- Interop callbacks -------------------------------------------------

    public ValueTask OnGeometryChangedAsync(BonboardingStepGeometry geometry)
    {
        if (_disposed || CurrentStep is null) return ValueTask.CompletedTask;

        // Frames can arrive out of order after a resize storm; keep only the newest.
        if (geometry.Sequence <= _lastSequence) return ValueTask.CompletedTask;

        var wasWaitingForTarget = Status == BonboardingTourStatus.Waiting && _pendingRoute is null;

        ApplyGeometry(geometry);

        if (wasWaitingForTarget && geometry.Found)
        {
            return new ValueTask(ShowResolvedTargetAsync(CurrentStep, _generation));
        }

        Notify(BonboardingTourSessionChange.Geometry);
        return ValueTask.CompletedTask;
    }

    /// <summary>Finishes showing a step whose target turned up while the step was waiting for it.</summary>
    private Task ShowResolvedTargetAsync(BonboardingStepDefinition step, int generation)
        => RunGuardedAsync(async _ =>
        {
            // Another transition, or an earlier frame for the same target, got here first.
            if (generation != _generation || !ReferenceEquals(CurrentStep, step)) return;
            if (Status != BonboardingTourStatus.Waiting || _pendingRoute is not null) return;

            Status = BonboardingTourStatus.Running;
            _stepShownAt = DateTimeOffset.UtcNow;
            Notify(BonboardingTourSessionChange.Step);

            var stepToken = _stepCts?.Token ?? CancellationToken.None;

            await PublishAsync(BonboardingEventKind.TargetResolved, step);

            // A target that was lost and came back: the step was already shown once.
            if (_stepShown) return;

            await CompleteShowAsync(step, CurrentIndex, _enteredFrom, _enteredBy, stepToken);
        }, CancellationToken.None);

    public async ValueTask OnTargetLostAsync()
    {
        if (_disposed || CurrentStep is null || Status != BonboardingTourStatus.Running) return;

        var step = CurrentStep;
        var generation = _generation;

        await RunGuardedAsync(async ct =>
        {
            // Another transition got here first; the lost target belonged to a step that is gone.
            if (generation != _generation || !ReferenceEquals(CurrentStep, step) || Status != BonboardingTourStatus.Running) return;

            var options = CurrentOptions!;
            var stepToken = _stepCts?.Token ?? CancellationToken.None;

            await PublishAsync(BonboardingEventKind.TargetMissing, step);

            // The old rectangle no longer points at anything; drawing the spotlight there would lie.
            _geometry = null;
            Placement = null;

            switch (options.MissingTarget)
            {
                case BonboardingMissingTargetBehavior.Center:
                {
                    var geometry = await SafeMeasureAsync();
                    if (generation != _generation) return;

                    if (geometry is not null) ApplyGeometryIfNewer(geometry);
                    Notify(BonboardingTourSessionChange.Step);
                    return;
                }

                case BonboardingMissingTargetBehavior.Wait:
                {
                    Status = BonboardingTourStatus.Waiting;
                    Notify(BonboardingTourSessionChange.Step);

                    // The browser stopped watching when it reported the loss, so hand the step back
                    // to it in waiting mode: it watches for the element and arms the wait timeout.
                    var geometry = await ActivateInBrowserAsync(step, options, stepToken);
                    if (generation != _generation || Status != BonboardingTourStatus.Waiting) return;

                    if (geometry is { Found: true })
                    {
                        ApplyGeometryIfNewer(geometry);
                        Status = BonboardingTourStatus.Running;
                        await PublishAsync(BonboardingEventKind.TargetResolved, step);
                        Notify(BonboardingTourSessionChange.Step);
                    }

                    return;
                }

                default:
                    // Skip and Fail behave exactly as they do when the target is missing on arrival:
                    // a vanished target is not the user choosing to advance, so CanAdvance and
                    // branching do not get a say.
                    await HandleMissingTargetAsync(options.MissingTarget, step, +1, ct);
                    return;
            }
        }, CancellationToken.None);
    }

    public async ValueTask OnKeyAsync(string key, bool shiftKey)
    {
        if (_disposed || CurrentOptions is null || !IsActive) return;

        var options = CurrentOptions;
        var rtl = options.RightToLeft;

        switch (key)
        {
            case "Escape" when options.CloseOnEscape:
                await DismissAsync();
                break;
            case "ArrowRight" when options.KeyboardNavigation:
                await (rtl ? PreviousAsync() : NextAsync());
                break;
            case "ArrowLeft" when options.KeyboardNavigation:
                await (rtl ? NextAsync() : PreviousAsync());
                break;
            case "Home" when options.KeyboardNavigation && _plan.Count > 0:
                await GoToAsync(_plan[0]);
                break;
            case "End" when options.KeyboardNavigation && _plan.Count > 0:
                await GoToAsync(_plan[^1]);
                break;
        }
    }

    public async ValueTask OnOverlayClickAsync()
    {
        if (_disposed || CurrentOptions is null || !IsActive) return;
        if (!CurrentOptions.CloseOnOverlayClick) return;

        await DismissAsync();
    }

    public async ValueTask OnAdvanceTriggeredAsync()
    {
        if (_disposed || Status != BonboardingTourStatus.Running) return;
        await NextAsync();
    }

    public async ValueTask OnWaitTimedOutAsync()
    {
        if (_disposed || CurrentStep is null || Status != BonboardingTourStatus.Waiting) return;

        var step = CurrentStep;
        var generation = _generation;
        var behavior = CurrentOptions!.WaitTimeoutBehavior;

        // Waiting again after a wait expired would spin forever.
        if (behavior == BonboardingMissingTargetBehavior.Wait) behavior = BonboardingMissingTargetBehavior.Skip;

        await RunGuardedAsync(async ct =>
        {
            if (generation != _generation || !ReferenceEquals(CurrentStep, step)) return;
            if (Status != BonboardingTourStatus.Waiting || _pendingRoute is not null) return;

            if (behavior == BonboardingMissingTargetBehavior.Center)
            {
                await ShowCenteredAfterTimeoutAsync(step, generation);
                return;
            }

            await HandleMissingTargetAsync(behavior, step, +1, ct);
        }, CancellationToken.None);
    }

    /// <summary>
    /// Gives up on the target and shows the step as a centred card. The browser stopped watching
    /// when the wait expired, so the step is activated again without a target: that measures the
    /// popover for placement and re-arms the observers that keep it placed.
    /// </summary>
    private async Task ShowCenteredAfterTimeoutAsync(BonboardingStepDefinition step, int generation)
    {
        var stepToken = _stepCts?.Token ?? CancellationToken.None;

        _resolvedTarget = BonboardingStepTarget.None;

        var geometry = await ActivateInBrowserAsync(step, CurrentOptions!, stepToken);
        if (generation != _generation) return;

        if (geometry is not null) ApplyGeometryIfNewer(geometry);

        Status = BonboardingTourStatus.Running;
        _stepShownAt = DateTimeOffset.UtcNow;
        Notify(BonboardingTourSessionChange.Step);

        if (_stepShown) return;

        await CompleteShowAsync(step, CurrentIndex, _enteredFrom, _enteredBy, stepToken);
    }

    // ---- Geometry and placement -------------------------------------------

    /// <summary>
    /// Applies the reply to an activation or measurement request, unless a frame the browser
    /// measured after it has already arrived and been applied.
    /// </summary>
    private void ApplyGeometryIfNewer(BonboardingStepGeometry geometry)
    {
        if (_geometry is not null && geometry.Sequence <= _lastSequence) return;

        ApplyGeometry(geometry);
    }

    private void ApplyGeometry(BonboardingStepGeometry geometry)
    {
        _geometry = geometry;
        _lastSequence = geometry.Sequence;

        var options = CurrentOptions;
        if (options is null) return;

        if (geometry.Popover.IsEmpty)
        {
            // The popover has not rendered yet, so there is nothing to place. The resize observer
            // calls back as soon as it has a size.
            Placement = null;
            return;
        }

        var hasTarget = geometry.Found && !geometry.Target.IsEmpty;
        var padded = hasTarget ? geometry.Target.Inflate(options.SpotlightPadding) : BonboardingRect.Empty;
        var viewport = geometry.Viewport.IsEmpty
            ? new BonboardingRect(0, 0, _environment.Viewport.Width, _environment.Viewport.Height)
            : geometry.Viewport;

        Placement = BonboardingPlacementEngine.Place(new BonboardingPlacementRequest
        {
            Target = padded,
            Popover = geometry.Popover,
            Viewport = viewport,
            Preferred = options.Placement,
            Fallbacks = options.PlacementFallbacks,
            Offset = options.Offset,
            ViewportPadding = options.ViewportPadding,
            ArrowSize = options.ArrowSize,
            // The radius the stylesheet actually gave the popover, so a theme that changes it
            // still keeps the arrow off the rounded corners.
            CornerRadius = geometry.PopoverRadius ?? BonboardingDefaults.PopoverRadius,
            RightToLeft = options.RightToLeft,
            HasTarget = hasTarget,
        });
    }

    /// <summary>Called by the host once the popover element exists, so it can be measured.</summary>
    public async ValueTask SetPopoverElementAsync(ElementReference? element)
    {
        if (_disposed || !_interop.IsAvailable) return;

        try
        {
            await _interop.SetPopoverAsync(Id, element, CurrentOptions?.TrapFocus ?? true);
        }
        catch (Exception ex) when (BonboardingInteropFailure.IsTransient(ex))
        {
            _logger.Log(BonboardingInteropFailure.LevelFor(ex), ex, "BonboardingService could not register the popover element.");
        }
    }

    /// <summary>Called by the host after the popover renders, to move focus into it.</summary>
    public async ValueTask FocusPopoverAsync()
    {
        if (_disposed || !_interop.IsAvailable || CurrentOptions is not { AutoFocus: true }) return;

        try
        {
            if (!_focusCaptured)
            {
                await _interop.CaptureFocusAsync(Id);
                _focusCaptured = true;
            }

            await _interop.FocusPopoverAsync(Id);
        }
        catch (Exception ex) when (BonboardingInteropFailure.IsTransient(ex))
        {
            _logger.Log(BonboardingInteropFailure.LevelFor(ex), ex, "BonboardingService could not move focus into the popover.");
        }
    }

    // ---- Routing -----------------------------------------------------------

    private void HookNavigation()
    {
        if (_navigation is null || _navigationHooked) return;

        _navigation.LocationChanged += OnLocationChanged;
        _navigationHooked = true;
    }

    private void UnhookNavigation()
    {
        if (_navigation is null || !_navigationHooked) return;

        _navigation.LocationChanged -= OnLocationChanged;
        _navigationHooked = false;
    }

    private void OnLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        if (_disposed || _navigation is null) return;

        _ = HandleLocationChangedAsync(_navigation.ToBaseRelativePath(e.Location));
    }

    private async Task HandleLocationChangedAsync(string relative)
    {
        try
        {
            // Under the transition lock like every other state change, so a navigation cannot
            // interleave with a step that is halfway through being shown.
            await RunGuardedAsync(ct => HandleLocationChangedCoreAsync(relative, ct), CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "BonboardingService failed to handle a location change.");
        }
    }

    private async Task HandleLocationChangedCoreAsync(string relative, CancellationToken cancellationToken)
    {
        // A paused tour belongs to the application until it resumes; ResumeAsync re-checks the
        // route, so there is nothing to track in the meantime.
        if (CurrentStep is null || !IsActive) return;

        if (_pendingRoute is { } pending)
        {
            if (!BonboardingRouteMatcher.Matches(pending, relative)) return;

            _pendingRoute = null;

            // Re-enter the same step now the page is right. Retarget skips the leave hooks so
            // the step is not counted as visited twice.
            await MoveToAsync(CurrentIndex, BonboardingStepChangeReason.Retarget, +1, cancellationToken);
            return;
        }

        if (CurrentStep.Route is { } route && !BonboardingRouteMatcher.Matches(route, relative))
        {
            // The user navigated away from the step's page. Hold rather than yanking them back:
            // fighting the user for control of the address bar is never the right call.
            _generation++;
            _pendingRoute = route;
            Status = BonboardingTourStatus.Waiting;
            _geometry = null;
            Placement = null;
            await SafeDeactivateAsync();
            Notify(BonboardingTourSessionChange.Step);
            return;
        }

        // Same page, new content: the target may have been re-rendered underneath us.
        if (Status == BonboardingTourStatus.Running)
        {
            await RefreshCoreAsync();
        }
    }

    // ---- Persistence -------------------------------------------------------

    private bool ShouldPersist => Tour.Persist ?? _globalOptions.Persist;

    private async Task PersistAsync(CancellationToken cancellationToken)
    {
        if (!ShouldPersist || CurrentStep is not { } step) return;

        var index = CurrentIndex;
        var visited = _history.ToArray();

        try
        {
            var existing = await _store.GetAsync(Tour.Id, cancellationToken);

            await _store.SetAsync(
                new BonboardingRecord
                {
                    TourId = Tour.Id,
                    Version = Tour.Version,
                    CurrentStepId = step.Id,
                    CurrentIndex = index,
                    VisitedStepIds = visited,
                    // Data belongs to the application: the library's own writes must carry it over.
                    Data = existing?.Data,
                },
                cancellationToken);
        }
        catch (Exception ex) when (BonboardingInteropFailure.IsTransient(ex))
        {
            _logger.Log(BonboardingInteropFailure.LevelFor(ex), ex, "BonboardingService progress could not be persisted.");
        }
    }

    private async Task PersistEndAsync(BonboardingTourEndReason reason, BonboardingStepDefinition? lastStep, int lastIndex)
    {
        if (!ShouldPersist) return;

        // A cancelled tour was interrupted rather than refused, so the record must stay open for
        // it to resume from. The position was already written when the step was shown.
        if (reason == BonboardingTourEndReason.Cancelled) return;

        var visited = _history.ToArray();

        try
        {
            var existing = await _store.GetAsync(Tour.Id, CancellationToken.None);

            await _store.SetAsync(
                new BonboardingRecord
                {
                    TourId = Tour.Id,
                    Version = Tour.Version,
                    Data = existing?.Data,
                    CurrentStepId = lastStep?.Id,
                    CurrentIndex = lastIndex,
                    Completed = reason == BonboardingTourEndReason.Completed,
                    Dismissed = reason is BonboardingTourEndReason.Dismissed or BonboardingTourEndReason.Skipped,
                    CompletedAt = reason == BonboardingTourEndReason.Completed ? DateTimeOffset.UtcNow : null,
                    VisitedStepIds = visited,
                },
                CancellationToken.None);
        }
        catch (Exception ex) when (BonboardingInteropFailure.IsTransient(ex))
        {
            _logger.Log(BonboardingInteropFailure.LevelFor(ex), ex, "BonboardingService completion could not be persisted.");
        }
    }

    private async Task<BonboardingRecord?> SafeGetRecordAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await _store.GetAsync(Tour.Id, cancellationToken);
        }
        catch (Exception ex) when (BonboardingInteropFailure.IsTransient(ex))
        {
            _logger.Log(BonboardingInteropFailure.LevelFor(ex), ex, "BonboardingService progress could not be read.");
            return null;
        }
    }

    // ---- Plumbing ----------------------------------------------------------

    private BonboardingEffectiveStepOptions ResolveOptions(BonboardingStepDefinition? step)
        => BonboardingOptionResolver.Resolve(
            step,
            Tour,
            _globalOptions,
            _localizer.GetLabels(Tour),
            _environment.ReducedMotion,
            _environment.RightToLeft ? BonboardingDirection.RightToLeft : BonboardingDirection.LeftToRight);

    private BonboardingStepContext CreateContext(BonboardingStepDefinition step, CancellationToken cancellationToken = default, int? index = null)
        => new(this, step, index ?? Tour.Steps.IndexOf(step), _services, cancellationToken);

    /// <summary>
    /// Runs a command with the transition lock held, turning an unhandled fault into a failed tour
    /// rather than an unobserved exception.
    /// </summary>
    private async Task RunGuardedAsync(Func<CancellationToken, Task> action, CancellationToken cancellationToken)
    {
        if (_disposed) return;

        // Issued by an application hook while it runs: part of the transition that called the
        // hook, which already holds the lock.
        if (_hookScope.Value is { IsClosed: false } scope && await TryRunInHookScopeAsync(scope, action, cancellationToken))
        {
            return;
        }

        try
        {
            await _gate.WaitAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is ObjectDisposedException or OperationCanceledException)
        {
            return;
        }

        // A flow that reaches here may still carry the closed scope of the hook that started it.
        // That scope means nothing to the transition this flow now owns.
        _hookScope.Value = null;

        try
        {
            await RunAsync(action, cancellationToken);
        }
        finally
        {
            try { _gate.Release(); } catch (ObjectDisposedException) { }
        }
    }

    /// <summary>
    /// Runs a command a hook issued, one at a time per hook. Returns false when the hook had already
    /// returned by the time it was this command's turn, so the caller queues for the lock instead.
    /// </summary>
    private async Task<bool> TryRunInHookScopeAsync(HookScope scope, Func<CancellationToken, Task> action, CancellationToken cancellationToken)
    {
        try
        {
            await scope.Gate.WaitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return true;
        }

        try
        {
            if (scope.IsClosed) return false;

            await RunAsync(action, cancellationToken);
            return true;
        }
        finally
        {
            scope.Gate.Release();
        }
    }

    /// <summary>
    /// One call into application code made while a transition holds the lock.
    /// </summary>
    /// <remarks>
    /// Commands the hook issues while it runs are part of that transition, so they run inline
    /// (serialised among themselves) instead of waiting for the lock their own caller holds. The
    /// scope is captured by the async flow, so work the hook starts in the background inherits it
    /// too: when the hook returns the scope closes, the transition waits for any such command that
    /// is already running, and any that only starts later queues for the lock like every other
    /// caller. Nothing a hook starts can therefore run alongside the transition that called it.
    /// </remarks>
    private sealed class HookScope
    {
        private volatile bool _closed;

        public SemaphoreSlim Gate { get; } = new(1, 1);

        public bool IsClosed => _closed;

        public void Close() => _closed = true;
    }

    private async Task InvokeHookAsync(Func<ValueTask> hook)
    {
        var scope = new HookScope();
        _hookScope.Value = scope;

        try
        {
            await hook();
        }
        finally
        {
            await CloseHookScopeAsync(scope);
        }
    }

    private async Task<T> InvokeHookAsync<T>(Func<ValueTask<T>> hook)
    {
        var scope = new HookScope();
        _hookScope.Value = scope;

        try
        {
            return await hook();
        }
        finally
        {
            await CloseHookScopeAsync(scope);
        }
    }

    /// <summary>Closes a hook's scope and waits out a command the hook left running.</summary>
    private static async Task CloseHookScopeAsync(HookScope scope)
    {
        scope.Close();

        await scope.Gate.WaitAsync();
        scope.Gate.Release();
    }

    /// <summary>Runs a command body, turning an unhandled fault into a failed tour.</summary>
    private async Task RunAsync(Func<CancellationToken, Task> action, CancellationToken cancellationToken)
    {
        try
        {
            await action(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // A cancelled transition is the normal outcome of ending a tour mid-flight.
        }
        catch (Exception ex)
        {
            Error = ex;
            _logger.LogError(ex, "BonboardingService tour {TourId} failed during a transition.", Tour.Id);

            try
            {
                await EndAsync(BonboardingTourEndReason.Failed, CancellationToken.None);
            }
            catch (Exception endFailure)
            {
                _logger.LogError(endFailure, "BonboardingService tour {TourId} failed while shutting down.", Tour.Id);
            }
        }
    }

    private async ValueTask PublishAsync(
        BonboardingEventKind kind,
        BonboardingStepDefinition? step = null,
        BonboardingStepAction? action = null,
        BonboardingTourEndReason? reason = null,
        TimeSpan? duration = null)
    {
        var target = step ?? CurrentStep;

        var onboardingEvent = new BonboardingEvent
        {
            Kind = kind,
            Session = this,
            Step = target,
            StepIndex = target is null ? -1 : Tour.Steps.IndexOf(target),
            Action = action,
            Reason = reason,
            Error = kind == BonboardingEventKind.TourFailed ? Error : null,
            Duration = duration,
        };

        // Observers and EventRaised handlers are application code like any other hook, and may
        // call back into the session.
        await InvokeHookAsync(() => _publish(onboardingEvent));
    }

    private void Notify(BonboardingTourSessionChange change)
        => Changed?.Invoke(this, new BonboardingTourSessionChangedEventArgs(change));

    private void CancelStep()
    {
        var cts = _stepCts;
        _stepCts = null;

        if (cts is null) return;

        try { cts.Cancel(); } catch (ObjectDisposedException) { }
        cts.Dispose();
    }

    private async Task<BonboardingBrowserEnvironment> SafeInitializeAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await _interop.InitializeAsync(cancellationToken);
        }
        catch (Exception ex) when (BonboardingInteropFailure.IsTransient(ex))
        {
            _logger.Log(BonboardingInteropFailure.LevelFor(ex), ex, "BonboardingService could not sample the browser environment.");
            return BonboardingBrowserEnvironment.Unknown;
        }
    }

    private async Task SafeAttachAsync(CancellationToken cancellationToken)
    {
        if (!_interop.IsAvailable) return;

        try
        {
            await _interop.AttachAsync(Id, this, cancellationToken);
            _attached = true;
        }
        catch (Exception ex) when (BonboardingInteropFailure.IsTransient(ex))
        {
            _logger.Log(BonboardingInteropFailure.LevelFor(ex), ex, "BonboardingService could not attach the session to the browser.");
        }
    }

    private async Task<BonboardingStepGeometry?> SafeMeasureAsync()
    {
        if (!_interop.IsAvailable) return null;

        try
        {
            return await _interop.MeasureAsync(Id);
        }
        catch (Exception ex) when (BonboardingInteropFailure.IsTransient(ex))
        {
            _logger.Log(BonboardingInteropFailure.LevelFor(ex), ex, "BonboardingService could not measure the current step.");
            return null;
        }
    }

    private async Task SafeDeactivateAsync()
    {
        if (!_interop.IsAvailable || !_attached) return;

        try
        {
            await _interop.DeactivateStepAsync(Id);
        }
        catch (Exception ex) when (BonboardingInteropFailure.IsTransient(ex))
        {
            _logger.Log(BonboardingInteropFailure.LevelFor(ex), ex, "BonboardingService could not deactivate the current step.");
        }
    }

    private async Task SafeDetachAsync()
    {
        if (!_interop.IsAvailable) return;

        try
        {
            await _interop.DetachAsync(Id);
        }
        catch (Exception ex) when (BonboardingInteropFailure.IsTransient(ex))
        {
            _logger.Log(BonboardingInteropFailure.LevelFor(ex), ex, "BonboardingService could not detach the session.");
        }
    }

    private async Task RestoreFocusAsync()
    {
        if (!_focusCaptured || !_interop.IsAvailable) return;
        _focusCaptured = false;

        if (CurrentOptions is { RestoreFocus: false }) return;

        try
        {
            await _interop.RestoreFocusAsync(Id);
        }
        catch (Exception ex) when (BonboardingInteropFailure.IsTransient(ex))
        {
            _logger.Log(BonboardingInteropFailure.LevelFor(ex), ex, "BonboardingService could not restore focus.");
        }
    }

    public async ValueTask DisposeAsync()
    {
        var wasAttached = ReleaseLocalResources();

        if (wasAttached)
        {
            await SafeDetachAsync();
        }
    }

    /// <summary>
    /// Synchronous teardown, for containers that dispose their scopes without awaiting. Everything
    /// local is released; the browser-side cleanup is skipped because it cannot be awaited, which
    /// is harmless since a scope going away synchronously means the page is going away too.
    /// </summary>
    public void Dispose() => ReleaseLocalResources();

    private bool ReleaseLocalResources()
    {
        if (_disposed) return false;
        _disposed = true;

        UnhookNavigation();
        CancelStep();

        var wasAttached = _attached;
        _attached = false;

        _gate.Dispose();
        Changed = null;

        return wasAttached;
    }
}
