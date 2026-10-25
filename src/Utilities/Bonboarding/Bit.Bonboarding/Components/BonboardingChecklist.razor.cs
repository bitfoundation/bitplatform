using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;

namespace Bit.Bonboarding;

/// <summary>
/// Renders a <see cref="BonboardingChecklistDefinition"/>: its tasks, which are done, and a progress bar. Pressing a
/// task starts its tour, follows its route or runs its handler; tasks tick themselves off as their tours are
/// completed, their conditions become true, or the application says so.
/// </summary>
/// <remarks>
/// Place it where it belongs in your layout - a dashboard card, a drawer, a help panel. It renders nothing until
/// its progress has been read, which never happens while prerendering.
/// </remarks>
public sealed partial class BonboardingChecklist : ComponentBase, IAsyncDisposable
{
    private readonly string _headingId = $"bit-bonboarding-checklist-{Guid.NewGuid():N}";

    private List<BonboardingChecklistItemState>? _items;
    private BonboardingChecklistDefinition? _registered;
    private string? _title;
    private string? _description;
    private bool _dismissed;
    private bool _wasComplete;
    private bool _disposed;
    private int _refreshVersion;

    [Inject] private IBonboardingService BonboardingService { get; set; } = default!;

    [Inject] private IBonboardingLocalizer Localizer { get; set; } = default!;

    [Inject] private NavigationManager Navigation { get; set; } = default!;

    [Inject] private IServiceProvider Services { get; set; } = default!;

    [Inject] private ILogger<BonboardingChecklist> Logger { get; set; } = default!;

    /// <summary>The checklist to render.</summary>
    [Parameter, EditorRequired] public required BonboardingChecklistDefinition Checklist { get; set; }

    /// <summary>Shows a button that hides the checklist for good. Default <see langword="false"/>.</summary>
    [Parameter] public bool Dismissible { get; set; }

    /// <summary>Renders nothing once every task is done. Default <see langword="false"/>.</summary>
    [Parameter] public bool HideWhenComplete { get; set; }

    /// <summary>The heading level of the title, 1-6, to fit the page's outline. Default <c>2</c>.</summary>
    [Parameter] public int HeadingLevel { get; set; } = 2;

    /// <summary>Colour scheme. Default <see cref="BonboardingTheme.System"/>.</summary>
    [Parameter] public BonboardingTheme? Theme { get; set; }

    /// <summary>Extra classes on the checklist.</summary>
    [Parameter] public string? Class { get; set; }

    /// <summary>Replaces the markup of each task; the checklist still provides the list and its semantics.</summary>
    [Parameter] public RenderFragment<BonboardingChecklistItemState>? ItemTemplate { get; set; }

    /// <summary>Raised when the last task gets done while the checklist is on screen.</summary>
    [Parameter] public EventCallback OnCompleted { get; set; }

    /// <summary>The tasks as they stand, or <see langword="null"/> before progress has been read.</summary>
    public IReadOnlyList<BonboardingChecklistItemState>? Items => _items;

    /// <summary>True once every listed task is done.</summary>
    public bool IsComplete => AllDone;

    private BonboardingLabels Labels => Localizer.GetLabels(null);

    private int DoneCount => _items?.Count(state => state.IsDone) ?? 0;

    private bool AllDone => _items is { Count: > 0 } items && items.All(state => state.IsDone);

    private string ProgressText => BonboardingLabels.Format(
        Labels.ChecklistProgressFormat, BonboardingLabels.DefaultChecklistProgressFormat, DoneCount, _items?.Count ?? 0);

    private string FillStyle
        => "width:" + BonboardingCss.Number(_items is { Count: > 0 } items ? DoneCount * 100d / items.Count : 0) + "%";

    private string ThemeAttribute => Theme switch
    {
        BonboardingTheme.Dark => "dark",
        BonboardingTheme.Light => "light",
        _ => "system",
    };

    protected override void OnInitialized()
    {
        BonboardingService.EventRaised += OnTourEvent;
        if (BonboardingService is BonboardingService service) service.ChecklistChanged += OnChecklistChanged;
    }

    protected override void OnParametersSet()
    {
        ArgumentNullException.ThrowIfNull(Checklist);
        ArgumentOutOfRangeException.ThrowIfLessThan(HeadingLevel, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(HeadingLevel, 6);

        _title = Localizer.Localize(Checklist.Title);
        _description = Localizer.Localize(Checklist.Description);

        // Signals tick items off only for checklists on screen, so this one tells the service it is.
        if (!ReferenceEquals(_registered, Checklist) && BonboardingService is BonboardingService service)
        {
            if (_registered is not null) service.UnregisterChecklist(_registered);
            service.RegisterChecklist(Checklist);
            _registered = Checklist;
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        // Never runs while prerendering, where the store cannot be read.
        if (firstRender) await RefreshAsync();
    }

    /// <summary>
    /// Reads the progress again and re-renders. The checklist does this itself when a tour ends and when its stored
    /// progress changes; call it after your own state changed something an <see cref="BonboardingChecklistItem.IsComplete"/>
    /// or <see cref="BonboardingChecklistItem.When"/> condition reads.
    /// </summary>
    public async Task RefreshAsync()
    {
        if (_disposed) return;

        // Refreshes can overlap (two tours ending, a signal); only the latest one's answer is shown.
        var version = ++_refreshVersion;
        var checklist = Checklist;

        try
        {
            var dismissed = await BonboardingService.IsChecklistDismissedAsync(checklist.Id);
            var ticked = new HashSet<string>(await BonboardingService.GetCompletedChecklistItemsAsync(checklist.Id), StringComparer.Ordinal);

            var states = new List<BonboardingChecklistItemState>(checklist.Items.Count);
            foreach (var item in checklist.Items)
            {
                var context = new BonboardingChecklistItemContext(checklist, item, Services);

                if (item.When is { } when && !await when(context)) continue;

                var done = ticked.Contains(item.Id)
                    || await IsTourCompletedAsync(item)
                    || (item.IsComplete is { } isComplete && await isComplete(context));

                states.Add(new BonboardingChecklistItemState(item, done, IsActionable(item))
                {
                    ActivateAsync = () => ActivateAsync(item),
                });
            }

            if (version != _refreshVersion || _disposed) return;

            var hadState = _items is not null;
            _items = states;
            _dismissed = dismissed;

            var complete = AllDone;
            var justCompleted = hadState && complete && !_wasComplete;
            _wasComplete = complete;

            StateHasChanged();

            if (justCompleted && OnCompleted.HasDelegate) await OnCompleted.InvokeAsync();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // An item's condition is application code; one that throws must not take the page down with it.
            Logger.LogWarning(ex, "BonboardingChecklist could not work out the progress of checklist {ChecklistId}.", checklist.Id);
        }
    }

    /// <summary>
    /// True when the item's tour was completed, in the version of the tour the item names: a definition in hand decides
    /// which of its records count, registered or not (a bumped version without a migration is a tour not yet taken).
    /// </summary>
    private async Task<bool> IsTourCompletedAsync(BonboardingChecklistItem item)
    {
        if (item.Tour is { } tour)
        {
            return BonboardingRecordMigration.Apply(tour, await BonboardingService.GetRecordAsync(tour.Id), Logger)?.Completed == true;
        }

        return item.TourId is { } tourId && await BonboardingService.IsCompletedAsync(tourId);
    }

    private static bool IsActionable(BonboardingChecklistItem item)
        => item.OnClick is not null || item.Tour is not null || item.TourId is not null || item.Route is not null;

    private async Task ActivateAsync(BonboardingChecklistItem item)
    {
        try
        {
            if (item.OnClick is { } onClick)
            {
                await onClick(new BonboardingChecklistItemContext(Checklist, item, Services));
            }
            else if (item.Tour is { } tour)
            {
                // The user asked for it, so it takes over from anything running, like any start they ask for.
                await BonboardingService.StartAsync(tour);
            }
            else if (item.TourId is { } tourId)
            {
                await BonboardingService.StartAsync(tourId);
            }
            else if (item.Route is { } route)
            {
                Navigation.NavigateTo(route);
            }
        }
        catch (BonboardingException ex)
        {
            Logger.LogWarning(ex, "BonboardingChecklist could not start the tour of item {ItemId}.", item.Id);
        }

        await RefreshAsync();
    }

    private async Task DismissAsync()
    {
        await BonboardingService.DismissChecklistAsync(Checklist.Id);
        _dismissed = true;
    }

    private void OnTourEvent(object? sender, BonboardingEvent e)
    {
        // A tour that ends may complete an item, and one that is replayed may not.
        if (_disposed || e.Kind is not (BonboardingEventKind.TourCompleted or BonboardingEventKind.TourSkipped
            or BonboardingEventKind.TourDismissed or BonboardingEventKind.TourCancelled)) return;

        _ = InvokeAsync(RefreshAsync);
    }

    private void OnChecklistChanged(object? sender, string checklistId)
    {
        if (_disposed || checklistId != Checklist.Id) return;

        _ = InvokeAsync(RefreshAsync);
    }

    public ValueTask DisposeAsync()
    {
        if (_disposed) return ValueTask.CompletedTask;
        _disposed = true;

        BonboardingService.EventRaised -= OnTourEvent;

        if (BonboardingService is BonboardingService service)
        {
            service.ChecklistChanged -= OnChecklistChanged;
            if (_registered is not null) service.UnregisterChecklist(_registered);
        }

        return ValueTask.CompletedTask;
    }
}
