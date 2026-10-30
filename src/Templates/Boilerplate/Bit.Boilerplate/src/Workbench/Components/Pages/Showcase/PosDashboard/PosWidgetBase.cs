namespace Boilerplate.Workbench.Components.Pages.Showcase.PosDashboard;

public abstract partial class PosWidgetBase<TData> : WorkbenchComponentBase
{
    private PosDashboardFilter? loadedFilter;

    [AutoInject] protected PosDashboardDataService DataService = default!;

    [CascadingParameter] public PosDashboardPage Dashboard { get; set; } = default!;

    [Parameter] public PosWidgetLayout Layout { get; set; } = default!;

    [Parameter] public PosWidgetDefinition Definition { get; set; } = default!;

    protected TData? Data { get; private set; }

    protected PosWidgetStatus Status { get; private set; } = PosWidgetStatus.Loading;

    protected abstract Task<TData> Fetch(PosDashboardFilter filter, CancellationToken cancellationToken);

    protected abstract bool IsEmpty(TData data);

    protected virtual void OnDataLoaded()
    {
    }

    protected override async Task OnParametersSetAsync()
    {
        if (IsPrerendering || loadedFilter == Dashboard.Filter)
            return;

        await Reload();
    }

    protected async Task Reload()
    {
        loadedFilter = Dashboard.Filter;
        Status = PosWidgetStatus.Loading;
        StateHasChanged();

        try
        {
            var data = await Fetch(loadedFilter, CurrentCancellationToken);
            Data = data;
            Status = IsEmpty(data) ? PosWidgetStatus.Empty : PosWidgetStatus.Ready;

            if (Status is PosWidgetStatus.Ready)
            {
                OnDataLoaded();
            }
        }
        catch (WorkbenchSimulatedException)
        {
            Status = PosWidgetStatus.Failed;
        }
        catch (OperationCanceledException)
        {
        }
    }
}
