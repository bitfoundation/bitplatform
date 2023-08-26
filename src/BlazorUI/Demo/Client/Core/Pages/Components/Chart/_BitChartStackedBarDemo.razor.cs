namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Chart;

public partial class _BitChartStackedBarDemo
{
    private const int INITAL_COUNT = 5;

    private BitChart _chart = default!;
    private BitChartBarConfig _config = default!;

    protected override void OnInitialized()
    {
        _config = new BitChartBarConfig
        {
            Options = new BitChartBarOptions()
            {
                Responsive = true,
                Title = new BitChartOptionsTitle
                {
                    Display = true,
                    Text = "BitChart stacked bar Chart"
                },
                Tooltips = new BitChartTooltips
                {
                    Mode = BitChartInteractionMode.Index,
                    Intersect = false
                },
                Scales = new BitChartBarScales
                {
                    XAxes = new List<BitChartCartesianAxis>
                    {
                        new BitChartBarCategoryAxis
                        {
                            Stacked = true
                        }
                    },
                    YAxes = new List<BitChartCartesianAxis>
                    {
                        new BitChartBarLinearCartesianAxis
                        {
                            Stacked = true
                        }
                    }
                }
            }
        };

        IDataset<int> dataset1 = new BitChartBarDataset<int>(BitChartDemoUtils.RandomScalingFactor(INITAL_COUNT))
        {
            Label = "Dataset 1",
            BackgroundColor = BitChartColorUtil.FromDrawingColor(BitChartDemoColors.Red)
        };

        IDataset<int> dataset2 = new BitChartBarDataset<int>(BitChartDemoUtils.RandomScalingFactor(INITAL_COUNT))
        {
            Label = "Dataset 2",
            BackgroundColor = BitChartColorUtil.FromDrawingColor(BitChartDemoColors.Blue)
        };

        IDataset<int> dataset3 = new BitChartBarDataset<int>(BitChartDemoUtils.RandomScalingFactor(INITAL_COUNT))
        {
            Label = "Dataset 3",
            BackgroundColor = BitChartColorUtil.FromDrawingColor(BitChartDemoColors.Green)
        };


        _config.Data.Datasets.Add(dataset1);
        _config.Data.Datasets.Add(dataset2);
        _config.Data.Datasets.Add(dataset3);
        _config.Data.Labels.AddRange(BitChartDemoUtils.Months.Take(INITAL_COUNT));
    }



    private readonly string htmlCode = @"
<BitChart Config=""_config"" @ref=""_chart"" />";
    private readonly string csharpCode = @"
private const int INITAL_COUNT = 5;

private BitChart _chart = default!;
private BitChartBarConfig _config = default!;

protected override void OnInitialized()
{
    _config = new BitChartBarConfig
    {
        Options = new BitChartBarOptions()
        {
            Responsive = true,
            Title = new BitChartOptionsTitle
            {
                Display = true,
                Text = ""BitChart stacked bar Chart""
            },
            Tooltips = new BitChartTooltips
            {
                Mode = BitChartInteractionMode.Index,
                Intersect = false
            },
            Scales = new BitChartBarScales
            {
                XAxes = new List<BitChartCartesianAxis>
                {
                    new BitChartBarCategoryAxis
                    {
                        Stacked = true
                    }
                },
                YAxes = new List<BitChartCartesianAxis>
                {
                    new BitChartBarLinearCartesianAxis
                    {
                        Stacked = true
                    }
                }
            }
        }
    };

    IDataset<int> dataset1 = new BitChartBarDataset<int>(BitChartDemoUtils.RandomScalingFactor(INITAL_COUNT))
    {
        Label = ""Dataset 1"",
        BackgroundColor = BitChartColorUtil.FromDrawingColor(BitChartDemoColors.Red)
    };

    IDataset<int> dataset2 = new BitChartBarDataset<int>(BitChartDemoUtils.RandomScalingFactor(INITAL_COUNT))
    {
        Label = ""Dataset 2"",
        BackgroundColor = BitChartColorUtil.FromDrawingColor(BitChartDemoColors.Blue)
    };

    IDataset<int> dataset3 = new BitChartBarDataset<int>(BitChartDemoUtils.RandomScalingFactor(INITAL_COUNT))
    {
        Label = ""Dataset 3"",
        BackgroundColor = BitChartColorUtil.FromDrawingColor(BitChartDemoColors.Green)
    };


    _config.Data.Datasets.Add(dataset1);
    _config.Data.Datasets.Add(dataset2);
    _config.Data.Datasets.Add(dataset3);
    _config.Data.Labels.AddRange(BitChartDemoUtils.Months.Take(INITAL_COUNT));
}";
}
