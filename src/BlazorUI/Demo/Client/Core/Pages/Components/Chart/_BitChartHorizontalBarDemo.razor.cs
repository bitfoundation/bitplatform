namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Chart;

public partial class _BitChartHorizontalBarDemo
{
    private const int INITAL_COUNT = 5;

    private BitChart _chart = default!;
    private BitChartBarConfig _config = default!;

    protected override void OnInitialized()
    {
        _config = new BitChartBarConfig(horizontal: true)
        {
            Options = new BitChartBarOptions
            {
                Responsive = true,
                Legend = new BitChartLegend
                {
                    Position = BitChartPosition.Right
                },
                Title = new BitChartOptionsTitle
                {
                    Display = true,
                    Text = "BitChart Horizontal Bar Chart"
                }
            }
        };

        IDataset<int> dataset1 = new BitChartBarDataset<int>(BitChartDemoUtils.RandomScalingFactor(INITAL_COUNT, -100), horizontal: true)
        {
            Label = "My first dataset",
            BackgroundColor = BitChartColorUtil.FromDrawingColor(System.Drawing.Color.FromArgb(128, BitChartDemoColors.Red)),
            BorderColor = BitChartColorUtil.FromDrawingColor(BitChartDemoColors.Red),
            BorderWidth = 1
        };

        IDataset<int> dataset2 = new BitChartBarDataset<int>(BitChartDemoUtils.RandomScalingFactor(INITAL_COUNT, -100), horizontal: true)
        {
            Label = "My second dataset",
            BackgroundColor = BitChartColorUtil.FromDrawingColor(System.Drawing.Color.FromArgb(128, BitChartDemoColors.Blue)),
            BorderColor = BitChartColorUtil.FromDrawingColor(BitChartDemoColors.Blue),
            BorderWidth = 1
        };

        _config.Data.Labels.AddRange(BitChartDemoUtils.Months.Take(INITAL_COUNT));
        _config.Data.Datasets.Add(dataset1);
        _config.Data.Datasets.Add(dataset2);
    }



    private readonly string htmlCode = @"
<BitChart Config=""_config"" @ref=""_chart"" />";
    private readonly string csharpCode = @"
private const int INITAL_COUNT = 5;

private BitChart _chart = default!;
private BitChartBarConfig _config = default!;

protected override void OnInitialized()
{
    _config = new BitChartBarConfig(horizontal: true)
    {
        Options = new BitChartBarOptions
        {
            Responsive = true,
            Legend = new BitChartLegend
            {
                Position = BitChartPosition.Right
            },
            Title = new BitChartOptionsTitle
            {
                Display = true,
                Text = ""BitChart Horizontal Bar Chart""
            }
        }
    };

    IDataset<int> dataset1 = new BitChartBarDataset<int>(BitChartDemoUtils.RandomScalingFactor(INITAL_COUNT, -100), horizontal: true)
    {
        Label = ""My first dataset"",
        BackgroundColor = BitChartColorUtil.FromDrawingColor(System.Drawing.Color.FromArgb(128, BitChartDemoColors.Red)),
        BorderColor = BitChartColorUtil.FromDrawingColor(BitChartDemoColors.Red),
        BorderWidth = 1
    };

    IDataset<int> dataset2 = new BitChartBarDataset<int>(BitChartDemoUtils.RandomScalingFactor(INITAL_COUNT, -100), horizontal: true)
    {
        Label = ""My second dataset"",
        BackgroundColor = BitChartColorUtil.FromDrawingColor(System.Drawing.Color.FromArgb(128, BitChartDemoColors.Blue)),
        BorderColor = BitChartColorUtil.FromDrawingColor(BitChartDemoColors.Blue),
        BorderWidth = 1
    };

    _config.Data.Labels.AddRange(BitChartDemoUtils.Months.Take(INITAL_COUNT));
    _config.Data.Datasets.Add(dataset1);
    _config.Data.Datasets.Add(dataset2);
}";
}
