﻿namespace BlazorWeb.Client.Pages.Dashboard;

public partial class ProductsCountPerCategotyWidget
{

    private bool isLoading;
    private BitChart? chart;
    private BitChartBarConfig config = default!;

    protected override async Task OnInitAsync()
    {
        config = new BitChartBarConfig
        {
            Options = new BitChartBarOptions
            {
                Responsive = true,
                Legend = new BitChartLegend()
                {
                    Display = false,
                },
            }
        };

        await GetData();
    }

    private async Task GetData()
    {
        try
        {
            isLoading = true;

            var data = await PrerenderStateService.GetValue($"{nameof(DashboardPage)}-{nameof(ProductsCountPerCategotyWidget)}",
                                async () => await HttpClient.GetFromJsonAsync($"Dashboard/GetProductsCountPerCategotyStats",
                                    AppJsonContext.Default.ListProductsCountPerCategoryDto)) ?? [];

            BitChartBarDataset<int> chartDataSet = [.. data.Select(d => d.ProductCount)];
            chartDataSet.BackgroundColor = data.Select(d => d.CategoryColor ?? string.Empty).ToArray();
            config.Data.Datasets.Add(chartDataSet);
            config.Data.Labels.AddRange(data.Select(d => d.CategoryName ?? string.Empty));
        }
        finally
        {
            isLoading = false;
        }
    }

}
