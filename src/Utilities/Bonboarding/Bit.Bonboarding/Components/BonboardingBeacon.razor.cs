using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;

namespace Bit.Bonboarding;

/// <summary>
/// A hotspot: a small pulsing dot on an element of the page that, when pressed, opens a hint about it, starts a
/// tour, or runs a handler of your own. It follows its element as the page scrolls and changes, hides while the
/// element is missing, hidden or scrolled out of sight, and steps aside while a tour runs.
/// </summary>
/// <remarks>
/// The browser layer positions it, with no call into .NET per frame, so a page of beacons costs nothing while
/// idle, also on Blazor Server. Give it an <see cref="Id"/> to make it go away for good once its hint (or the tour
/// it starts) has been read; without one it stays.
/// </remarks>
public sealed partial class BonboardingBeacon : ComponentBase, IAsyncDisposable
{
    private readonly string _beaconId = $"bit-bonboarding-beacon-{Guid.NewGuid():N}";

    private ElementReference? _element;
    private string? _attachedElementId;
    private BonboardingBeaconActivation? _attached;
    private BonboardingStepTarget _target = BonboardingStepTarget.None;
    private IBonboardingTourSession? _opened;

    private bool _shown;
    private bool _closed;
    private bool _focusTargetOnClose;
    private bool _disposed;

    [Inject] private IServiceProvider Services { get; set; } = default!;

    [Inject] private ILogger<BonboardingBeacon> Logger { get; set; } = default!;

    // Resolved rather than injected, so the beacon also works when the app never called AddBitBonboardingServices.
    // Null until first used: DisposeAsync leaves alone what was never resolved.
    private BonboardingRuntime? _runtime;
    private BonboardingRuntime Runtime => _runtime ??= BonboardingRuntime.Of(Services);
    private IBonboardingService BonboardingService => Runtime.Service;
    private BonboardingGuidanceRegistry Guidance => Runtime.Guidance;
    private IBonboardingInterop Interop => Runtime.Interop;
    private IBonboardingLocalizer Localizer => Runtime.Localizer;

    /// <summary>
    /// Makes the beacon remember that its hint was read: once the user closes the hint, the beacon does not come
    /// back (the id is the hint's persistence key, like a tour id). Without one, the beacon always shows.
    /// </summary>
    [Parameter] public string? Id { get; set; }

    // ---- Targeting ----------------------------------------------------------

    /// <summary>The element the beacon sits on. Accepts a CSS selector string or an <see cref="ElementReference"/>.</summary>
    [Parameter] public BonboardingStepTarget? Target { get; set; }

    /// <summary>Sits on the element carrying <c>data-bonboarding-anchor="..."</c>.</summary>
    [Parameter] public string? Anchor { get; set; }

    /// <summary>Sits on the first rendered element matching a CSS selector.</summary>
    [Parameter] public string? Selector { get; set; }

    /// <summary>Sits on a captured element reference.</summary>
    [Parameter] public ElementReference? Element { get; set; }

    /// <summary>Which corner, edge or the centre of the element. Default <see cref="BonboardingBeaconPosition.TopEnd"/>.</summary>
    [Parameter] public BonboardingBeaconPosition Position { get; set; } = BonboardingBeaconPosition.TopEnd;

    /// <summary>Pixels the beacon is moved towards the inline end (the right in left-to-right).</summary>
    [Parameter] public double OffsetX { get; set; }

    /// <summary>Pixels the beacon is moved down.</summary>
    [Parameter] public double OffsetY { get; set; }

    // ---- What pressing it does ------------------------------------------------

    /// <summary>Heading of the hint, and the beacon's accessible name.</summary>
    [Parameter] public string? Title { get; set; }

    /// <summary>Text of the hint.</summary>
    [Parameter] public string? Description { get; set; }

    /// <summary>Markup of the hint, replacing <see cref="Description"/>.</summary>
    [Parameter] public RenderFragment<BonboardingStepRenderContext>? ChildContent { get; set; }

    /// <summary>Where the hint goes relative to the element. Default <see cref="BonboardingPlacement.Auto"/>.</summary>
    [Parameter] public BonboardingPlacement? Placement { get; set; }

    /// <summary>Starts this tour instead of opening a hint. The beacon goes away once the tour is completed or dismissed.</summary>
    [Parameter] public BonboardingTourDefinition? Tour { get; set; }

    /// <summary>Starts this registered tour instead of opening a hint.</summary>
    [Parameter] public string? TourId { get; set; }

    /// <summary>Runs instead of opening a hint or starting a tour.</summary>
    [Parameter] public EventCallback OnClick { get; set; }

    /// <summary>Keeps showing the beacon after the tour it starts was completed or dismissed. Default <see langword="false"/>.</summary>
    [Parameter] public bool KeepAfterClosed { get; set; }

    // ---- Presentation -------------------------------------------------------

    /// <summary>The beacon's accessible name. Defaults to <see cref="Title"/>, then <see cref="BonboardingLabels.BeaconLabel"/>.</summary>
    [Parameter] public string? Label { get; set; }

    /// <summary>Hides the beacon while any tour runs, so it does not compete with it. Default <see langword="true"/>.</summary>
    [Parameter] public bool HideWhileTourRuns { get; set; } = true;

    /// <summary>Colour scheme of the beacon and its hint. Default <see cref="BonboardingTheme.System"/>.</summary>
    [Parameter] public BonboardingTheme? Theme { get; set; }

    /// <summary>Extra classes on the beacon.</summary>
    [Parameter] public string? Class { get; set; }

    /// <summary>True while the hint or the tour this beacon opened is on screen.</summary>
    public bool IsOpen => _opened is { EndReason: null } opened && opened.Status != BonboardingTourStatus.Idle;

    /// <summary>True once what the beacon opened has ended, however it ended.</summary>
    private bool IsFinishedOpening => _opened is { EndReason: not null };

    private bool OpensDialog => !OnClick.HasDelegate;

    /// <summary>The record that says whether the beacon has done its job: its hint's, or its tour's.</summary>
    private string? PersistenceKey => Tour?.Id ?? TourId ?? Id;

    private string BeaconKey => Id ?? _beaconId;

    private string AccessibleName
        => Localizer.Localize(Label) ?? Localizer.Localize(Title) ?? Localizer.GetLabels(null).BeaconLabel;

    private string ThemeAttribute => BonboardingCss.Theme(Theme);

    protected override void OnInitialized()
    {
        BonboardingService.SessionsChanged += OnSessionsChanged;

        // A session is added before it starts (or while it is queued), so its start is only seen in the event stream.
        BonboardingService.EventRaised += OnTourEvent;

        // A reset, or another tab, can bring back a beacon whose hint was read or take away one whose tour was finished.
        Guidance.RecordsChanged += OnRecordsChanged;
    }

    private void OnRecordsChanged(object? sender, EventArgs e)
    {
        if (_disposed) return;

        _ = InvokeAsync(async () =>
        {
            if (!_disposed && await RefreshClosedAsync()) StateHasChanged();
        });
    }

    private void OnTourEvent(object? sender, BonboardingEvent e)
    {
        if (_disposed || e.Kind is not (BonboardingEventKind.TourStarted or BonboardingEventKind.TourPaused or BonboardingEventKind.StepShown)) return;

        _ = InvokeAsync(() =>
        {
            var shown = ComputeShown();
            if (shown == _shown || _disposed) return;

            _shown = shown;
            StateHasChanged();
        });
    }

    protected override void OnParametersSet()
    {
        _target = Target
            ?? (Element is { } element ? BonboardingStepTarget.Element(element) : null)
            ?? (Anchor is not null ? BonboardingStepTarget.Anchor(Anchor) : null)
            ?? (Selector is not null ? BonboardingStepTarget.Css(Selector) : null)
            ?? BonboardingStepTarget.None;

        if (_target.IsNone || _target.IsDynamic)
        {
            throw new InvalidOperationException(
                $"{nameof(BonboardingBeacon)} needs a concrete target: set {nameof(Anchor)}, {nameof(Selector)}, {nameof(Element)} or {nameof(Target)} (a dynamic target cannot be followed by the browser).");
        }

        _shown = ComputeShown();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_disposed) return;

        // Never runs while prerendering, so this is the first moment the store can be read.
        if (firstRender && await RefreshClosedAsync())
        {
            StateHasChanged();
            return;
        }

        await SyncWithBrowserAsync();
    }

    private bool ComputeShown()
        => !_closed && !(HideWhileTourRuns && AnotherTourRuns());

    /// <summary>
    /// The tour the beacon opens: its hint's id, or the tour it starts. Known by id rather than by session, because a
    /// session's first events arrive before the start that created it has returned.
    /// </summary>
    private string OwnTourId => Tour?.Id ?? TourId ?? Id ?? _beaconId;

    /// <summary>A tour the user walked away from does not count: nothing of it is on screen to compete with.</summary>
    private bool AnotherTourRuns()
        => BonboardingService.Sessions.Any(session => session.Tour.Id != OwnTourId && BonboardingTourSession.IsUnderway(session));

    /// <summary>Reads whether the beacon's hint or tour is finished with. True when that changed what is shown.</summary>
    private async Task<bool> RefreshClosedAsync()
    {
        var closed = false;

        if (PersistenceKey is { } key && (Tour is null && TourId is null || !KeepAfterClosed))
        {
            try
            {
                var record = await BonboardingService.GetRecordAsync(key);

                // A tour in hand decides which of its records are current, registered or not.
                if (Tour is not null) record = BonboardingRecordMigration.Apply(Tour, record, Logger);

                closed = record?.IsClosed == true;
            }
            catch (Exception ex) when (BonboardingInteropFailure.IsTransient(ex))
            {
                Logger.Log(BonboardingInteropFailure.LevelFor(ex), ex, "BonboardingBeacon could not read whether {Key} was closed.", key);
            }
        }

        if (closed == _closed) return false;

        _closed = closed;
        var shown = ComputeShown();
        var changed = shown != _shown;
        _shown = shown;
        return changed;
    }

    /// <summary>Hands the beacon element to the browser layer, or takes it back.</summary>
    private async Task SyncWithBrowserAsync()
    {
        try
        {
            if (_shown && _element is { } element && !string.IsNullOrEmpty(element.Id))
            {
                var activation = new BonboardingBeaconActivation
                {
                    BeaconId = _beaconId,
                    Target = BonboardingTargetDescriptor.From(_target),
                    Position = PositionName(Position),
                    OffsetX = OffsetX,
                    OffsetY = OffsetY,
                };

                if (_attachedElementId != element.Id || _attached != activation)
                {
                    await Interop.AttachBeaconAsync(activation, element);
                    _attachedElementId = element.Id;
                    _attached = activation;
                }
            }
            else if (_attachedElementId is not null)
            {
                _attachedElementId = null;
                _attached = null;
                await Interop.DetachBeaconAsync(_beaconId);

                // The beacon the user just used has gone for good; its element is where they were looking.
                if (_focusTargetOnClose)
                {
                    _focusTargetOnClose = false;
                    await Interop.FocusTargetAsync(BonboardingTargetDescriptor.From(_target));
                }
            }
        }
        catch (Exception ex) when (BonboardingInteropFailure.IsTransient(ex))
        {
            Logger.Log(BonboardingInteropFailure.LevelFor(ex), ex, "BonboardingBeacon could not reach the browser.");
        }
    }

    private async Task OpenAsync()
    {
        if (OnClick.HasDelegate)
        {
            await OnClick.InvokeAsync();
            return;
        }

        try
        {
            // The user asked for this, so it takes over from anything running, like any start they ask for.
            _opened = Tour is not null ? await BonboardingService.StartAsync(Tour)
                : TourId is not null ? await BonboardingService.StartAsync(TourId)
                : await BonboardingService.StartAsync(CreateHint(), new BonboardingStartOptions { Force = true, Resume = false });
        }
        catch (BonboardingException ex)
        {
            Logger.LogWarning(ex, "BonboardingBeacon {Beacon} could not open.", BeaconKey);
        }
    }

    /// <summary>
    /// The hint: one non-modal step on the beacon's element, with no overlay and no focus trap, that the user can
    /// read and close while the page stays theirs. Focus moves into it and comes back to the beacon.
    /// </summary>
    private BonboardingTourDefinition CreateHint() => new()
    {
        Id = Id ?? _beaconId,
        Title = Title,
        // Without an id there is nothing to remember it by; with one, the application's own Persist setting decides.
        Persist = Id is null ? false : null,
        ShowOverlay = false,
        ShowSpotlight = false,
        Interaction = BonboardingInteractionMode.Free,
        TrapFocus = false,
        Progress = BonboardingProgressStyle.None,
        ShowSkip = false,
        ShowPrevious = false,
        Placement = Placement,
        Theme = Theme,
        MissingTarget = BonboardingMissingTargetBehavior.Center,
        Steps =
        {
            new BonboardingStepDefinition
            {
                Id = "hint",
                Target = _target,
                Title = Title,
                Description = Description,
                Body = ChildContent,
                DoneLabel = Localizer.GetLabels(null).HintDone,
            },
        },
    };

    private void OnSessionsChanged(object? sender, EventArgs e)
    {
        if (_disposed) return;

        _ = InvokeAsync(async () =>
        {
            if (_disposed) return;

            if (IsFinishedOpening)
            {
                _opened = null;
                await RefreshClosedAsync();
                _focusTargetOnClose = _closed;
            }

            _shown = ComputeShown();
            StateHasChanged();
        });
    }

    private static string PositionName(BonboardingBeaconPosition position) => position switch
    {
        BonboardingBeaconPosition.TopStart => "top-start",
        BonboardingBeaconPosition.Top => "top",
        BonboardingBeaconPosition.End => "end",
        BonboardingBeaconPosition.Center => "center",
        BonboardingBeaconPosition.Start => "start",
        BonboardingBeaconPosition.BottomEnd => "bottom-end",
        BonboardingBeaconPosition.BottomStart => "bottom-start",
        BonboardingBeaconPosition.Bottom => "bottom",
        _ => "top-end",
    };

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        // Nothing was subscribed to or attached when the services were never reached - resolving them failed, say.
        if (_runtime is null) return;

        BonboardingService.SessionsChanged -= OnSessionsChanged;
        BonboardingService.EventRaised -= OnTourEvent;
        Guidance.RecordsChanged -= OnRecordsChanged;

        // A hint is about this beacon's element, which is going with the page.
        if (_opened is { EndReason: null } opened && Tour is null && TourId is null)
        {
            await opened.CancelAsync();
        }

        if (_attachedElementId is not null)
        {
            try
            {
                await Interop.DetachBeaconAsync(_beaconId);
            }
            catch (Exception ex) when (BonboardingInteropFailure.IsTransient(ex))
            {
                Logger.Log(BonboardingInteropFailure.LevelFor(ex), ex, "BonboardingBeacon could not detach from the browser.");
            }
        }
    }
}
