namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Chart;

public partial class _BitChartLineDemo
{
    private const int INITAL_COUNT = 5;

    private BitChart _chart = default!;
    private BitChartLineConfig _config = default!;

    protected override void OnInitialized()
    {
        _config = new BitChartLineConfig
        {
            Options = new BitChartLineOptions
            {
                Responsive = true,
                Title = new BitChartOptionsTitle
                {
                    Display = true,
                    Text = "BitChart Line Chart"
                },
                Tooltips = new BitChartTooltips
                {
                    Mode = BitChartInteractionMode.Nearest,
                    Intersect = true
                },
                Hover = new BitChartHover
                {
                    Mode = BitChartInteractionMode.Nearest,
                    Intersect = true
                },
                Scales = new BitChartScales
                {
                    XAxes = new List<BitChartCartesianAxis>
                    {
                        new BitChartCategoryAxis
                        {
                            ScaleLabel = new BitChartScaleLabel
                            {
                                LabelString = "Month"
                            }
                        }
                    },
                    YAxes = new List<BitChartCartesianAxis>
                    {
                        new BitChartLinearCartesianAxis
                        {
                            ScaleLabel = new BitChartScaleLabel
                            {
                                LabelString = "Value"
                            }
                        }
                    }
                }
            }
        };

        IDataset<int> dataset1 = new BitChartLineDataset<int>(BitChartDemoUtils.RandomScalingFactor(INITAL_COUNT))
        {
            Label = "My first dataset",
            BackgroundColor = BitChartColorUtil.FromDrawingColor(BitChartDemoColors.Red),
            BorderColor = BitChartColorUtil.FromDrawingColor(BitChartDemoColors.Red),
            Fill = BitChartFillingMode.Disabled
        };

        IDataset<int> dataset2 = new BitChartLineDataset<int>(BitChartDemoUtils.RandomScalingFactor(INITAL_COUNT))
        {
            Label = "My second dataset",
            BackgroundColor = BitChartColorUtil.FromDrawingColor(BitChartDemoColors.Blue),
            BorderColor = BitChartColorUtil.FromDrawingColor(BitChartDemoColors.Blue),
            Fill = BitChartFillingMode.Disabled
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
private BitChartLineConfig _config = default!;

protected override void OnInitialized()
{
    _config = new BitChartLineConfig
    {
        Options = new BitChartLineOptions
        {
            Responsive = true,
            Title = new BitChartOptionsTitle
            {
                Display = true,
                Text = ""BitChart Line Chart""
            },
            Tooltips = new BitChartTooltips
            {
                Mode = BitChartInteractionMode.Nearest,
                Intersect = true
            },
            Hover = new BitChartHover
            {
                Mode = BitChartInteractionMode.Nearest,
                Intersect = true
            },
            Scales = new BitChartScales
            {
                XAxes = new List<BitChartCartesianAxis>
                {
                    new BitChartCategoryAxis
                    {
                        ScaleLabel = new BitChartScaleLabel
                        {
                            LabelString = ""Month""
                        }
                    }
                },
                YAxes = new List<BitChartCartesianAxis>
                {
                    new BitChartLinearCartesianAxis
                    {
                        ScaleLabel = new BitChartScaleLabel
                        {
                            LabelString = ""Value""
                        }
                    }
                }
            }
        }
    };

    IDataset<int> dataset1 = new BitChartLineDataset<int>(BitChartDemoUtils.RandomScalingFactor(INITAL_COUNT))
    {
        Label = ""My first dataset"",
        BackgroundColor = BitChartColorUtil.FromDrawingColor(BitChartDemoColors.Red),
        BorderColor = BitChartColorUtil.FromDrawingColor(BitChartDemoColors.Red),
        Fill = BitChartFillingMode.Disabled
    };

    IDataset<int> dataset2 = new BitChartLineDataset<int>(BitChartDemoUtils.RandomScalingFactor(INITAL_COUNT))
    {
        Label = ""My second dataset"",
        BackgroundColor = BitChartColorUtil.FromDrawingColor(BitChartDemoColors.Blue),
        BorderColor = BitChartColorUtil.FromDrawingColor(BitChartDemoColors.Blue),
        Fill = BitChartFillingMode.Disabled
    };

    _config.Data.Labels.AddRange(BitChartDemoUtils.Months.Take(INITAL_COUNT));
    _config.Data.Datasets.Add(dataset1);
    _config.Data.Datasets.Add(dataset2);
}";
}
