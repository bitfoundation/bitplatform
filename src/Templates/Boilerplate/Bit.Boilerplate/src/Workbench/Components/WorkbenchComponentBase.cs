using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;

namespace Boilerplate.Workbench.Components;

/// <summary>
/// Base of every Workbench component: the services they all use, the scenario of the view it runs in,
/// and a cancellation token that is cancelled when the component goes away.
/// </summary>
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

    /// <summary>
    /// Cancelled when the component is disposed, and stays cancelled after it; pass it to loads and delays, and check it
    /// after an await to stop work a gone component started.
    /// </summary>
    protected CancellationToken CurrentCancellationToken => cancellationTokenSource?.Token ?? new CancellationToken(canceled: true);

    /// <summary>The settings of the view this component renders in: device, theme, data state, user and so on.</summary>
    protected WorkbenchScenario Scenario => ScenarioState.Scenario;

    protected bool IsPrerendering => RendererInfo.IsInteractive is false;

    /// <summary>Completes once the Blex stores hold what the browser saved for this view; at once while prerendering.</summary>
    protected Task WhenStateRestored() => IsPrerendering ? Task.CompletedTask : Persistor.WhenHydratedAsync();

    /// <summary>The id of the shell's pane this preview is shown in, or null outside a pane.</summary>
    protected string? PaneId => WorkbenchUrls.ReadQuery(NavigationManager.Uri, WorkbenchUrls.PaneQueryKey);

    /// <summary>True inside a /preview page, which runs in an iframe of the shell; false when the shell renders the page inline.</summary>
    protected bool IsInsidePreview => new Uri(NavigationManager.Uri).AbsolutePath.StartsWith("/preview/", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Hands a scenario the page changed by itself (a theme or language picked in the app's chrome) to the shell.
    /// <paramref name="isApplied"/> false asks for a reload with the new scenario instead of updating the address in place.
    /// </summary>
    protected async Task ReportScenario(WorkbenchScenario scenario, bool isApplied = true)
    {
        // Rendered inline by the shell (the phone layout): the shell's store is right here.
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
        // Let go of the source first, so nothing reads the token of a disposed source while it cancels.
        if (cancellationTokenSource is { } source)
        {
            cancellationTokenSource = null;
            await source.CancelAsync();
            source.Dispose();
        }

        Dispose();

        GC.SuppressFinalize(this);
    }
}
