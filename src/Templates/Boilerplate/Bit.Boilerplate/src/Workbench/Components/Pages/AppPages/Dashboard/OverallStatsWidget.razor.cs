namespace Boilerplate.Workbench.Components.Pages.AppPages.Dashboard;

/// <summary>The Workbench copy of the app's overall stats widget, counting <c>DashboardDemoData</c> through the scenario state.</summary>
public partial class OverallStatsWidget
{
    private bool isLoading = true;
    private bool isFailed;
    private int totalCategories;
    private int categoriesWithProductCount;
    private int totalProducts;
    private int last30DaysProductCount;

    protected override async Task OnInitializedAsync()
    {
        if (IsPrerendering)
            return;

        await GetData();
    }

    private async Task GetData()
    {
        isLoading = true;
        isFailed = false;
        StateHasChanged();

        try
        {
            var categories = await ScenarioState.Load("dashboard.overall", () => DashboardDemoData.Categories, () => [], CurrentCancellationToken);

            totalCategories = categories.Count;
            categoriesWithProductCount = categories.Count(c => c.ProductCount > 0);
            totalProducts = categories.Sum(c => c.ProductCount);
            last30DaysProductCount = categories.Count > 0 ? DashboardDemoData.Last30DaysProductCount : 0;
        }
        catch (WorkbenchSimulatedException)
        {
            isFailed = true;
        }
        catch (OperationCanceledException)
        {
            return;
        }

        isLoading = false;
        StateHasChanged();
    }
}
