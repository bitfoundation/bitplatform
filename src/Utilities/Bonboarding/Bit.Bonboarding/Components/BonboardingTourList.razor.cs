using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;

namespace Bit.Bonboarding;

/// <summary>
/// Lists tours with where the user stands with each - new, in progress, completed - and a button to start, resume
/// or replay it: the entry point of a help menu or a resource centre, so guidance can be found again on purpose
/// rather than only offered once.
/// </summary>
/// <remarks>
/// Lists every registered tour unless given <see cref="Tours"/>. Tours declared in markup are registered only while
/// their page is on screen; register the ones a help menu should always offer with
/// <see cref="IBonboardingService.Register"/> at startup.
/// </remarks>
public sealed partial class BonboardingTourList : ComponentBase, IDisposable
{
    private readonly string _headingId = $"bit-bonboarding-tours-{Guid.NewGuid():N}";

    private List<BonboardingTourListItem>? _items;
    private string? _title;
    private IEnumerable<BonboardingTourDefinition>? _listedTours;
    private bool _listedHideCompleted;
    private bool _refreshPending;
    private bool _disposed;
    private int _refreshVersion;

    [Inject] private IServiceProvider Services { get; set; } = default!;

    [Inject] private ILogger<BonboardingTourList> Logger { get; set; } = default!;

    // Resolved rather than injected, so the list also works when the app never called AddBitBonboardingServices. Null
    // until first used: Dispose leaves alone what was never resolved.
    private BonboardingRuntime? _runtime;
    private BonboardingRuntime Runtime => _runtime ??= BonboardingRuntime.Of(Services);
    private IBonboardingService BonboardingService => Runtime.Service;
    private BonboardingGuidanceRegistry Guidance => Runtime.Guidance;
    private IBonboardingLocalizer Localizer => Runtime.Localizer;

    /// <summary>The tours to list, in order. Defaults to every registered tour, by title.</summary>
    [Parameter] public IEnumerable<BonboardingTourDefinition>? Tours { get; set; }

    /// <summary>Heading of the list, e.g. "Guided tours".</summary>
    [Parameter] public string? Title { get; set; }

    /// <summary>The heading level of the title, 1-6, to fit the page's outline. Default <c>2</c>.</summary>
    [Parameter] public int HeadingLevel { get; set; } = 2;

    /// <summary>Leaves out tours the user has completed. Default <see langword="false"/>.</summary>
    [Parameter] public bool HideCompleted { get; set; }

    /// <summary>Colour scheme. Default <see cref="BonboardingTheme.System"/>.</summary>
    [Parameter] public BonboardingTheme? Theme { get; set; }

    /// <summary>Extra classes on the list.</summary>
    [Parameter] public string? Class { get; set; }

    /// <summary>Replaces the markup of each tour; the list still provides the list semantics.</summary>
    [Parameter] public RenderFragment<BonboardingTourListItem>? ItemTemplate { get; set; }

    /// <summary>Shown when there is no tour to list.</summary>
    [Parameter] public RenderFragment? EmptyContent { get; set; }

    /// <summary>The tours as listed, or <see langword="null"/> before their progress has been read.</summary>
    public IReadOnlyList<BonboardingTourListItem>? Items => _items;

    private BonboardingLabels Labels => Localizer.GetLabels(null);

    private string ThemeAttribute => BonboardingCss.Theme(Theme);

    protected override void OnInitialized()
    {
        BonboardingService.SessionsChanged += OnChanged;
        BonboardingService.EventRaised += OnTourEvent;
        Guidance.RegistrationsChanged += OnChanged;
        Guidance.RecordsChanged += OnChanged;
    }

    // A session is added before it starts, so its start (and a pause) is only seen in the event stream.
    private void OnTourEvent(object? sender, BonboardingEvent e)
    {
        if (e.Kind is BonboardingEventKind.TourStarted or BonboardingEventKind.TourPaused) OnChanged(sender, EventArgs.Empty);
    }

    protected override void OnParametersSet()
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(HeadingLevel, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(HeadingLevel, 6);

        _title = Localizer.Localize(Title);

        // What to list changed: a refresh still listing the old must not land, and the new is listed once rendered.
        if (!ReferenceEquals(_listedTours, Tours) || _listedHideCompleted != HideCompleted)
        {
            _listedTours = Tours;
            _listedHideCompleted = HideCompleted;
            _refreshVersion++;
            _refreshPending = true;
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        // Never runs while prerendering, where the store cannot be read.
        if (!firstRender && !_refreshPending) return;

        _refreshPending = false;
        await RefreshAsync();
    }

    /// <summary>Reads every tour's progress again and re-renders.</summary>
    public async Task RefreshAsync()
    {
        if (_disposed) return;

        var version = ++_refreshVersion;
        var tours = (Tours ?? BonboardingService.RegisteredTours.OrderBy(tour => tour.Title ?? tour.Id, StringComparer.CurrentCulture)).ToArray();
        var items = new List<BonboardingTourListItem>(tours.Length);

        try
        {
            foreach (var tour in tours)
            {
                // This definition decides which of its records are current, whether or not it is registered.
                var progress = ProgressOf(BonboardingRecordMigration.Apply(tour, await BonboardingService.GetRecordAsync(tour.Id), Logger));
                if (HideCompleted && progress == BonboardingTourProgress.Completed) continue;

                // A tour the user walked away from is not running as far as they can tell: it can be resumed from here.
                var running = BonboardingService.FindSession(tour.Id) is { } session && BonboardingTourSession.IsUnderway(session);

                items.Add(new BonboardingTourListItem(tour, progress, running) { StartAsync = () => StartAsync(tour, progress) });
            }
        }
        catch (Exception ex) when (BonboardingInteropFailure.IsTransient(ex))
        {
            Logger.Log(BonboardingInteropFailure.LevelFor(ex), ex, "BonboardingTourList could not read the tours' progress.");
        }

        if (version != _refreshVersion || _disposed) return;

        _items = items;
        StateHasChanged();
    }

    private static BonboardingTourProgress ProgressOf(BonboardingRecord? record) => record switch
    {
        { Completed: true } => BonboardingTourProgress.Completed,
        { Dismissed: false, CurrentStepId: not null } => BonboardingTourProgress.InProgress,
        _ => BonboardingTourProgress.New,
    };

    private async Task StartAsync(BonboardingTourDefinition tour, BonboardingTourProgress progress)
    {
        try
        {
            // The user picked it, so it takes over from anything running. A replay starts from the top; anything
            // else resumes where the user left off. CanStart still decides: a tour for administrators stays theirs.
            await BonboardingService.StartAsync(tour, new BonboardingStartOptions { Resume = progress != BonboardingTourProgress.Completed });
        }
        catch (BonboardingException ex)
        {
            Logger.LogWarning(ex, "BonboardingTourList could not start tour {TourId}.", tour.Id);
        }
    }

    private string StateLabel(BonboardingTourProgress progress) => progress switch
    {
        BonboardingTourProgress.Completed => Labels.TourCompleted,
        BonboardingTourProgress.InProgress => Labels.TourInProgress,
        _ => Labels.TourNew,
    };

    private string ActionLabel(BonboardingTourProgress progress) => progress switch
    {
        BonboardingTourProgress.Completed => Labels.TourReplay,
        BonboardingTourProgress.InProgress => Labels.TourResume,
        _ => Labels.TourStart,
    };

    private static string ProgressName(BonboardingTourProgress progress) => progress switch
    {
        BonboardingTourProgress.Completed => "completed",
        BonboardingTourProgress.InProgress => "in-progress",
        _ => "new",
    };

    private void OnChanged(object? sender, EventArgs e)
    {
        if (_disposed) return;
        _ = InvokeAsync(RefreshAsync);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        // Nothing was subscribed to when the services were never reached - resolving them failed, say.
        if (_runtime is null) return;

        BonboardingService.SessionsChanged -= OnChanged;
        BonboardingService.EventRaised -= OnTourEvent;
        Guidance.RegistrationsChanged -= OnChanged;
        Guidance.RecordsChanged -= OnChanged;
    }
}
