using Boilerplate.Shared.Features.Dashboard;

namespace Boilerplate.Workbench.Components.Pages.AppPages.Dashboard.Data;

/// <summary>
/// The stand-in for the app's <see cref="IDashboardController"/>: the numbers of a sample product catalog, counted the way
/// the server counts them (DashboardController), each widget's in the view's data state.
/// </summary>
public sealed class WorkbenchDashboardController(WorkbenchScenarioState scenarioState)
    : WorkbenchStandIn(scenarioState, "dashboard"), IDashboardController
{
    /// <summary>The catalog's categories, with their chart colors and how many products each has.</summary>
    private static readonly (string Name, string Color, int ProductCount)[] categories =
    [
        ("Ford", "#FFCD56", 8),
        ("Nissan", "#FF6384", 8),
        ("Benz", "#4BC0C0", 19),
        ("BMW", "#FF9124", 8),
        ("Tesla", "#2B88D8", 7)
    ];

    /// <summary>How many of the products were added in the last 30 days.</summary>
    private const int Last30DaysProductCount = 12;

    public Task<OverallAnalyticsStatsDataResponseDto> GetOverallAnalyticsStatsData(CancellationToken cancellationToken)
        => Load("overall", () => new OverallAnalyticsStatsDataResponseDto
        {
            TotalCategories = categories.Length,
            CategoriesWithProductCount = categories.Count(category => category.ProductCount > 0),
            TotalProducts = categories.Sum(category => category.ProductCount),
            Last30DaysProductCount = Last30DaysProductCount
        }, () => new OverallAnalyticsStatsDataResponseDto(), cancellationToken);

    public Task<List<ProductsCountPerCategoryResponseDto>> GetProductsCountPerCategoryStats(CancellationToken cancellationToken)
        => Load("count-per-category", () => categories.Select(category => new ProductsCountPerCategoryResponseDto
        {
            CategoryName = category.Name,
            CategoryColor = category.Color,
            ProductCount = category.ProductCount
        }).ToList(), () => [], cancellationToken);

    public Task<List<ProductPercentagePerCategoryResponseDto>> GetProductsPercentagePerCategoryStats(CancellationToken cancellationToken)
        => Load("percentage-per-category", () =>
        {
            var total = (float)categories.Sum(category => category.ProductCount);

            return categories.Select(category => new ProductPercentagePerCategoryResponseDto
            {
                CategoryName = category.Name,
                CategoryColor = category.Color,
                ProductPercentage = category.ProductCount / total * 100
            }).ToList();
        }, () => [], cancellationToken);
}
