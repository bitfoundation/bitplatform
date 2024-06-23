namespace Boilerplate.Shared.Dtos.Dashboard;

public class OverallAnalyticsStatsDataResponseDto
{
    public int Last30DaysProductCount { get; set; }

    public int Last30DaysCategoryCount { get; set; }

    public int TotalProducts { get; set; }

    public int TotalCategories { get; set; }
}
