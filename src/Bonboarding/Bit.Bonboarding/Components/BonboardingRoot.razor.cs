using Microsoft.AspNetCore.Components;

namespace Bit.Bonboarding;

/// <summary>
/// Renders the active tour. Place one of these once, typically in the main layout, and it hosts
/// every tour the application starts.
/// </summary>
/// <remarks>
/// The component owns the popover box itself even when the visuals are supplied by a template.
/// That is deliberate: measurement, placement, focus management and dialog semantics all depend on
/// that element existing and being known to the engine, and reimplementing them correctly in every
/// application is not a reasonable ask.
/// </remarks>
public sealed partial class BonboardingRoot : ComponentBase, IAsyncDisposable
{
    private ElementReference? _popoverElement;
    private ElementReference? _registeredElement;

    private BonboardingTourSession? _session;
    private BonboardingStepDefinition? _step;
    private BonboardingEffectiveStepOptions? _options;
    private BonboardingStepRenderContext? _renderContext;

    private string _titleId = string.Empty;
    private string _descriptionId = string.Empty;
    private string? _announcement;
    /// <summary>Session and step last announced, so a new session re-announces even the same step id.</summary>
    private string? _announcedKey;
    private string? _focusedStepId;
    private string? _claimedSessionId;
    private bool _disposed;

    [Inject] private IBonboardingService BonboardingService { get; set; } = default!;

    [Inject] private IBonboardingLocalizer Localizer { get; set; } = default!;

    [Inject] private BonboardingHostRegistry Hosts { get; set; } = default!;

    /// <summary>
    /// Render this session instead of whichever one is active. Doing so also claims it, so the
    /// application's main host stops rendering it and the two cannot draw the same tour twice.
    /// </summary>
    [Parameter] public IBonboardingTourSession? Session { get; set; }

    /// <summary>Extra classes on the root element, for application-level theming.</summary>
    [Parameter] public string? Class { get; set; }

    /// <summary>
    /// Forces a colour scheme for everything this host renders, overriding the tour and global
    /// settings. Bind it to your own theme switch so tours follow the rest of the application.
    /// </summary>
    [Parameter] public BonboardingTheme? Theme { get; set; }

    /// <summary>Extra classes on the popover element.</summary>
    [Parameter] public string? PopoverClass { get; set; }

    /// <summary>
    /// Drops the built-in popover classes so the popover carries only your own. The element, its
    /// position and its dialog semantics are still managed for you.
    /// </summary>
    [Parameter] public bool Unstyled { get; set; }

    /// <summary>Replaces the contents of the popover for every step.</summary>
    [Parameter] public RenderFragment<BonboardingStepRenderContext>? ChildContent { get; set; }

    /// <summary>Replaces the overlay and spotlight visuals.</summary>
    [Parameter] public RenderFragment<BonboardingStepRenderContext>? OverlayTemplate { get; set; }

    /// <summary>Replaces the placeholder shown while the engine waits for a target or a route.</summary>
    [Parameter] public RenderFragment<BonboardingStepRenderContext>? WaitingTemplate { get; set; }

    protected override void OnInitialized()
    {
        BonboardingService.SessionsChanged += OnSessionsChanged;
        Hosts.Changed += OnSessionsChanged;
        Bind(ResolveSession());
    }

    protected override void OnParametersSet()
    {
        UpdateClaim();
        Bind(ResolveSession());
    }

    /// <summary>Claims the explicitly supplied session, releasing any previously claimed one.</summary>
    private void UpdateClaim()
    {
        var wanted = Session?.Id;
        if (wanted == _claimedSessionId) return;

        if (_claimedSessionId is not null) Hosts.Release(_claimedSessionId);

        _claimedSessionId = wanted;

        if (_claimedSessionId is not null) Hosts.Claim(_claimedSessionId);
    }

    private BonboardingTourSession? ResolveSession()
    {
        if (Session is not null) return Session as BonboardingTourSession;

        // An ambient host leaves claimed sessions to the host that claimed them.
        var active = BonboardingService.Active;
        return active is not null && !Hosts.IsClaimed(active.Id) ? active as BonboardingTourSession : null;
    }

    private void Bind(BonboardingTourSession? session)
    {
        if (ReferenceEquals(session, _session))
        {
            Snapshot();
            return;
        }

        if (_session is not null) _session.Changed -= OnSessionChanged;

        _session = session;

        if (_session is not null) _session.Changed += OnSessionChanged;

        // A new session means the previous popover element is gone as far as the engine knows.
        _registeredElement = null;
        _focusedStepId = null;
        Snapshot();
    }

    /// <summary>
    /// Copies the pieces of session state the render tree depends on. Reading them once per render
    /// keeps the markup free of null checks and means a mid-render change cannot tear.
    /// </summary>
    private void Snapshot()
    {
        var session = _session;

        if (session is null || !session.IsActive || session.CurrentStep is null || session.CurrentOptions is null)
        {
            _step = null;
            _options = null;
            _renderContext = null;

            // Empty the live region between tours: a polite region only speaks when its text
            // changes, so a tour that starts again with the same first step would otherwise be silent.
            _announcement = null;
            _announcedKey = null;
            return;
        }

        _step = session.CurrentStep;
        _options = session.CurrentOptions;
        _titleId = $"bit-bonboarding-title-{session.Id}-{_step.Id}";
        _descriptionId = $"bit-bonboarding-desc-{session.Id}-{_step.Id}";

        _renderContext = new BonboardingStepRenderContext
        {
            Session = session,
            Step = _step,
            Options = _options,
            Title = Localizer.Localize(_step.Title),
            Description = Localizer.Localize(_step.Description),
        };

        var announcedKey = session.Id + "/" + _step.Id;

        if (_announcedKey != announcedKey && !session.IsWaiting)
        {
            _announcedKey = announcedKey;
            _announcement = BonboardingLabels.Format(
                _options.Labels.StepAnnouncementFormat,
                BonboardingLabels.DefaultStepAnnouncementFormat,
                session.DisplayPosition,
                session.DisplayCount,
                _renderContext.Title ?? _renderContext.Description ?? string.Empty).Trim();
        }
        else if (session.IsWaiting && _announcement != _options.Labels.WaitingAnnouncement)
        {
            _announcement = _options.Labels.WaitingAnnouncement;
            _announcedKey = null;
        }
    }

    private void OnSessionsChanged(object? sender, EventArgs e)
    {
        if (_disposed) return;

        _ = InvokeAsync(() =>
        {
            Bind(ResolveSession());
            StateHasChanged();
        });
    }

    private void OnSessionChanged(object? sender, BonboardingTourSessionChangedEventArgs e)
    {
        if (_disposed) return;

        _ = InvokeAsync(() =>
        {
            Snapshot();
            StateHasChanged();
        });
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_disposed || _session is null) return;

        // Register the popover as soon as it exists so the engine can measure it. The element is
        // stable across steps, so this normally happens once per tour.
        if (_popoverElement is { } element && _registeredElement?.Id != element.Id)
        {
            _registeredElement = element;
            await _session.SetPopoverElementAsync(element);
        }
        else if (_popoverElement is null && _registeredElement is not null)
        {
            _registeredElement = null;
            await _session.SetPopoverElementAsync(null);
        }

        // Move focus once per step, after the content for that step has actually rendered.
        if (_step is not null && !_session.IsWaiting && _focusedStepId != _step.Id)
        {
            _focusedStepId = _step.Id;
            await _session.FocusPopoverAsync();
        }
    }

    private Task HandleOverlayClickAsync()
        => _session is null ? Task.CompletedTask : _session.OnOverlayClickAsync().AsTask();

    // ---- Computed presentation --------------------------------------------

    /// <summary>
    /// True when the popover may be shown. While the engine waits for a target or a route there is
    /// nothing to place against, so the waiting card is centred by <see cref="PopoverStyle"/> instead.
    /// </summary>
    private bool IsPlaced => _session?.Placement is not null || IsWaitingUnplaced;

    private bool IsWaitingUnplaced => _session is { IsWaiting: true, Placement: null };

    private BonboardingPlacementResult? Placement => _session?.Placement;

    private bool ShowSpotlight
        => _options is { ShowSpotlight: true, SpotlightShape: not BonboardingSpotlightShape.None } && SpotlightRect is not null;

    /// <summary>The padded target rectangle the spotlight and the blockers share.</summary>
    private BonboardingRect? SpotlightRect
    {
        get
        {
            if (_options is null || _session?.TargetRect is not { } target) return null;
            return target.Inflate(_options.SpotlightPadding);
        }
    }

    private bool ShowArrow => Placement is { ShowArrow: true } && _options is { ArrowSize: > 0 };

    private string SideAttribute => Placement?.SideName ?? "center";

    private string ThemeAttribute => (Theme ?? _options?.Theme) switch
    {
        BonboardingTheme.Dark => "dark",
        BonboardingTheme.Light => "light",
        _ => "system",
    };

    private string DirectionAttribute => _options?.Direction == BonboardingDirection.RightToLeft ? "rtl" : "ltr";

    private string RootStyle
    {
        get
        {
            if (_options is null) return string.Empty;

            var opacity = _options.ShowOverlay ? _options.OverlayOpacity : 0;

            return string.Concat(
                "--bit-bonboarding-z:", BonboardingCss.Int(_options.ZIndex),
                ";--bit-bonboarding-duration:", BonboardingCss.Ms(_options.AnimationDuration),
                ";--bit-bonboarding-overlay-opacity:", BonboardingCss.Number(opacity),
                ";--bit-bonboarding-arrow:", BonboardingCss.Px(_options.ArrowSize));
        }
    }

    private string SpotlightStyle
    {
        get
        {
            if (SpotlightRect is not { } rect || _options is null) return string.Empty;

            var radius = _options.SpotlightShape switch
            {
                BonboardingSpotlightShape.Circle => BonboardingCss.Px(Math.Max(rect.Width, rect.Height)),
                BonboardingSpotlightShape.Rectangle => "0",
                _ => BonboardingCss.Px(_options.SpotlightRadius),
            };

            return string.Concat(
                "left:", BonboardingCss.Px(rect.X),
                ";top:", BonboardingCss.Px(rect.Y),
                ";width:", BonboardingCss.Px(rect.Width),
                ";height:", BonboardingCss.Px(rect.Height),
                ";border-radius:", radius);
        }
    }

    private string PopoverStyle
    {
        get
        {
            if (IsWaitingUnplaced)
            {
                // Centred by the stylesheet itself: the waiting card needs no measurement.
                return "left:50%;top:50%;transform:translate3d(-50%,-50%,0)";
            }

            if (Placement is not { } placement)
            {
                // Off-screen but laid out, so it can be measured without ever being visible.
                return "transform:translate3d(0,0,0)";
            }

            return string.Concat(
                "transform:translate3d(",
                BonboardingCss.Px(placement.Popover.X), ",", BonboardingCss.Px(placement.Popover.Y), ",0)");
        }
    }

    private string ArrowStyle
    {
        get
        {
            if (Placement is not { } placement || _options is null) return string.Empty;

            var offset = BonboardingCss.Px(placement.ArrowOffset - (_options.ArrowSize / 2));

            return placement.Side switch
            {
                BonboardingPlacementSide.Top or BonboardingPlacementSide.Bottom => "left:" + offset,
                _ => "top:" + offset,
            };
        }
    }

    private enum BlockerSide { Top, Bottom, Left, Right }

    /// <summary>
    /// Positions one of the four rectangles that surround the spotlight hole, so pointer events
    /// reach the highlighted element and nothing else.
    /// </summary>
    private static string BlockerStyle(BlockerSide side, BonboardingRect hole) => side switch
    {
        BlockerSide.Top => string.Concat("left:0;right:0;top:0;height:", BonboardingCss.Px(Math.Max(0, hole.Top))),
        BlockerSide.Bottom => string.Concat("left:0;right:0;top:", BonboardingCss.Px(Math.Max(0, hole.Bottom)), ";bottom:0"),
        BlockerSide.Left => string.Concat(
            "left:0;width:", BonboardingCss.Px(Math.Max(0, hole.Left)),
            ";top:", BonboardingCss.Px(Math.Max(0, hole.Top)),
            ";height:", BonboardingCss.Px(Math.Max(0, hole.Height))),
        _ => string.Concat(
            "left:", BonboardingCss.Px(Math.Max(0, hole.Right)),
            ";right:0;top:", BonboardingCss.Px(Math.Max(0, hole.Top)),
            ";height:", BonboardingCss.Px(Math.Max(0, hole.Height))),
    };

    private string? AriaLabel
        => string.IsNullOrEmpty(_renderContext?.Title)
            ? string.IsNullOrEmpty(_session?.Tour.Title) ? _options?.Labels.DialogLabel : _session.Tour.Title
            : null;

    private string? AriaLabelledBy
        => string.IsNullOrEmpty(_renderContext?.Title) ? null : _titleId;

    private string? AriaDescribedBy
        => string.IsNullOrEmpty(_renderContext?.Description) ? null : _descriptionId;

    public ValueTask DisposeAsync()
    {
        if (_disposed) return ValueTask.CompletedTask;
        _disposed = true;

        BonboardingService.SessionsChanged -= OnSessionsChanged;
        Hosts.Changed -= OnSessionsChanged;

        if (_claimedSessionId is not null)
        {
            Hosts.Release(_claimedSessionId);
            _claimedSessionId = null;
        }

        if (_session is not null) _session.Changed -= OnSessionChanged;

        _session = null;
        return ValueTask.CompletedTask;
    }
}
