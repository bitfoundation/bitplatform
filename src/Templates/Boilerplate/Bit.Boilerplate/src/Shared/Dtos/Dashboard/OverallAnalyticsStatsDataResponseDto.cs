namespace Boilerplate.Shared.Dtos.Dashboard;

public partial class OverallAnalyticsStatsDataResponseDto
{
    public int TotalCategories { get; set; }

    public int TotalProducts { get; set; }

    public int CategoriesWithProductCount { get; set; }

    public int Last30DaysProductCount { get; set; }
}
