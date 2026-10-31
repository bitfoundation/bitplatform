using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;

namespace Boilerplate.Workbench.Components;

public partial class WorkbenchComponentBase : BlexComponentBase, IAsyncDisposable
{
    [AutoInject] protected IStringLocalizer<WorkbenchStrings> Localizer = default!;
    [AutoInject] protected IStringLocalizer<AppStrings> AppLocalizer = default!;
    [AutoInject] protected NavigationManager NavigationManager = default!;
    [AutoInject] protected WorkbenchScenarioState ScenarioState = default!;
    [AutoInject] protected BlexStatePersistor Persistor = default!;
    [AutoInject] protected WorkbenchStore WorkbenchStore = default!;
    [AutoInject] protected IJSRuntime JSRuntime = default!;
    [AutoInject] protected SnackBarService SnackBarService = default!;
    [AutoInject] protected IAuthorizationService AuthorizationService = default!;

    private CancellationTokenSource? cancellationTokenSource = new();

    [CascadingParameter] protected BitDir? CurrentDir { get; set; }

    [CascadingParameter] protected Task<AuthenticationState> AuthenticationStateTask { get; set; } = default!;

    protected CancellationToken CurrentCancellationToken => cancellationTokenSource?.Token ?? CancellationToken.None;

    protected WorkbenchScenario Scenario => ScenarioState.Scenario;

    protected bool IsPrerendering => RendererInfo.IsInteractive is false;

    protected Task WhenStateRestored() => IsPrerendering ? Task.CompletedTask : Persistor.WhenHydratedAsync();

    protected string? PaneId => WorkbenchUrls.ReadQuery(NavigationManager.Uri, WorkbenchUrls.PaneQueryKey);

    protected bool IsInsidePreview => new Uri(NavigationManager.Uri).AbsolutePath.StartsWith("/preview/", StringComparison.OrdinalIgnoreCase);

    protected async Task ReportScenario(WorkbenchScenario scenario, bool isApplied = true)
    {
        if (IsInsidePreview is false)
        {
            WorkbenchStore.ChangeScenario(scenario);
            return;
        }

        ScenarioState.Set(scenario);

        var pageKey = new Uri(NavigationManager.Uri).Segments[^1].Trim('/');

        await JSRuntime.InvokeVoidAsync("workbench.reportScenario", WorkbenchUrls.Preview(pageKey, scenario, PaneId), scenario.ToQueryString(), isApplied);
    }

    public virtual async ValueTask DisposeAsync()
    {
        if (cancellationTokenSource is not null)
        {
            await cancellationTokenSource.CancelAsync();
            cancellationTokenSource.Dispose();
            cancellationTokenSource = null;
        }

        Dispose();

        GC.SuppressFinalize(this);
    }
}
