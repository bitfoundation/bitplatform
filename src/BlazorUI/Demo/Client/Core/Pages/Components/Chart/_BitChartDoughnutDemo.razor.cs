namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Chart;

public partial class _BitChartDoughnutDemo
{
    private const int INITAL_COUNT = 5;

    private BitChart _chart = default!;
    private BitChartPieConfig _config = default!;

    protected override void OnInitialized()
    {
        _config = new BitChartPieConfig(useDoughnutType: true)
        {
            Options = new BitChartPieOptions
            {
                Responsive = true,
                Title = new BitChartOptionsTitle
                {
                    Display = true,
                    Text = "BitChart Doughnut Chart"
                }
            }
        };

        BitChartPieDataset<int> dataset = new BitChartPieDataset<int>(BitChartDemoUtils.RandomScalingFactor(INITAL_COUNT))
        {
            BackgroundColor = BitChartDemoColors.All.Take(INITAL_COUNT).Select(c => BitChartColorUtil.FromDrawingColor(System.Drawing.Color.FromArgb(220, c))).ToArray()
        };
        _config.Data.Labels.AddRange(BitChartDemoUtils.Months.Take(INITAL_COUNT));
        _config.Data.Datasets.Add(dataset);
    }



    private readonly string htmlCode = @"
<BitChart Config=""_config"" @ref=""_chart"" />";
    private readonly string csharpCode = @"
private const int INITAL_COUNT = 5;

private BitChart _chart = default!;
private BitChartPieConfig _config = default!;

protected override void OnInitialized()
{
    _config = new BitChartPieConfig(useDoughnutType: true)
    {
        Options = new BitChartPieOptions
        {
            Responsive = true,
            Title = new BitChartOptionsTitle
            {
                Display = true,
                Text = ""BitChart Doughnut Chart""
            }
        }
    };

    BitChartPieDataset<int> dataset = new BitChartPieDataset<int>(BitChartDemoUtils.RandomScalingFactor(INITAL_COUNT))
    {
        BackgroundColor = BitChartDemoColors.All.Take(INITAL_COUNT).Select(c => BitChartColorUtil.FromDrawingColor(System.Drawing.Color.FromArgb(220, c))).ToArray()
    };
    _config.Data.Labels.AddRange(BitChartDemoUtils.Months.Take(INITAL_COUNT));
    _config.Data.Datasets.Add(dataset);
}";
}
