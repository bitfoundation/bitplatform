namespace Boilerplate.Workbench.Components;

public partial class WorkbenchComponentBase : ComponentBase, IAsyncDisposable
{
    [AutoInject] protected IStringLocalizer<WorkbenchStrings> Localizer = default!;
    [AutoInject] protected IStringLocalizer<AppStrings> AppLocalizer = default!;
    [AutoInject] protected NavigationManager NavigationManager = default!;
    [AutoInject] protected WorkbenchScenarioState ScenarioState = default!;
    [AutoInject] protected IJSRuntime JSRuntime = default!;

    private CancellationTokenSource? cancellationTokenSource = new();

    [CascadingParameter] protected BitDir? CurrentDir { get; set; }

    protected CancellationToken CurrentCancellationToken => cancellationTokenSource?.Token ?? CancellationToken.None;

    protected WorkbenchScenario Scenario => ScenarioState.Scenario;

    public virtual async ValueTask DisposeAsync()
    {
        if (cancellationTokenSource is not null)
        {
            await cancellationTokenSource.CancelAsync();
            cancellationTokenSource.Dispose();
            cancellationTokenSource = null;
        }

        GC.SuppressFinalize(this);
    }
}
