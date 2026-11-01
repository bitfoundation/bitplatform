namespace Boilerplate.Workbench.Components.Pages.Showcases.PosDashboard;

/// <summary>
/// Base of every dashboard widget: loads its data for the dashboard's filter and tracks whether it is loading,
/// ready, empty or failed. It loads once the page is interactive and again whenever the filter changes.
/// </summary>
public abstract partial class PosWidgetBase<TData> : WorkbenchComponentBase
{
    private PosDashboardFilter? loadedFilter;
    private CancellationTokenSource? loadCancellation;

    [AutoInject] protected PosDashboardDataService DataService = default!;

    /// <summary>The dashboard the widget sits on, for its filter, its edit mode and the texts of its period.</summary>
    [CascadingParameter] public PosDashboardPage Dashboard { get; set; } = default!;

    [Parameter] public PosWidgetLayout Layout { get; set; } = default!;

    [Parameter] public PosWidgetDefinition Definition { get; set; } = default!;

    protected TData? Data { get; private set; }

    protected PosWidgetStatus Status { get; private set; } = PosWidgetStatus.Loading;

    /// <summary>Loads the widget's data for the filter from the sample data service.</summary>
    protected abstract Task<TData> Fetch(PosDashboardFilter filter, CancellationToken cancellationToken);

    /// <summary>Whether the data has nothing to show, which turns the widget to its empty state.</summary>
    protected abstract bool IsEmpty(TData data);

    /// <summary>Prepares what the markup needs, like chart data, once data with something to show has loaded.</summary>
    protected virtual void OnDataLoaded()
    {
    }

    /// <summary>Loads when the filter differs from the last one loaded; skipped while prerendering, which shows the skeleton.</summary>
    protected override async Task OnParametersSetAsync()
    {
        if (IsPrerendering || loadedFilter == Dashboard.Filter)
            return;

        await Reload();
    }

    /// <summary>Loads the data again: for another filter, and from the error state's Retry button.</summary>
    protected async Task Reload()
    {
        var filter = loadedFilter = Dashboard.Filter;
        Status = PosWidgetStatus.Loading;
        StateHasChanged();

        // A load still running for an earlier filter is cancelled, so its late answer can't replace this one.
        loadCancellation?.Cancel();
        loadCancellation?.Dispose();
        var cancellation = loadCancellation = CancellationTokenSource.CreateLinkedTokenSource(CurrentCancellationToken);

        try
        {
            var data = await Fetch(filter, cancellation.Token);

            // A load for a newer filter started while this one was finishing.
            if (cancellation.IsCancellationRequested)
                return;

            Data = data;
            Status = IsEmpty(data) ? PosWidgetStatus.Empty : PosWidgetStatus.Ready;

            if (Status is PosWidgetStatus.Ready)
            {
                OnDataLoaded();
            }
        }
        catch (WorkbenchSimulatedException) when (cancellation.IsCancellationRequested is false)
        {
            Status = PosWidgetStatus.Failed;
        }
        catch (Exception exception) when (exception is OperationCanceledException or WorkbenchSimulatedException)
        {
            // The widget went away, or a load for a newer filter took over.
        }
    }

    protected override void Dispose(bool disposing)
    {
        loadCancellation?.Dispose();

        base.Dispose(disposing);
    }
}
