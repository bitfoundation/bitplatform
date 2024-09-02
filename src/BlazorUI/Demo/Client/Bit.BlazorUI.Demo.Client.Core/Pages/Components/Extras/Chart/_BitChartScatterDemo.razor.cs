namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.Chart;

public partial class _BitChartScatterDemo
{
    private const int INITAL_COUNT = 5;

    private BitChart _chart = default!;
    private BitChartScatterConfig _config = default!;

    protected override void OnInitialized()
    {
        _config = new BitChartScatterConfig() 
        { 
            Options = new BitChartLineOptions
            {
                Scales = new BitChartScales
                {
                    XAxes =
                    [
                        new BitChartLinearCartesianAxis
                        {
                            ScaleLabel = new BitChartScaleLabel
                            {
                                LabelString = "Value"
                            },
                            GridLines = new BitChartGridLines
                            {
                                Color = "gray"
                            }
                        }
                    ],
                    YAxes =
                    [
                        new BitChartLinearCartesianAxis
                        {
                            ScaleLabel = new BitChartScaleLabel
                            {
                                LabelString = "Value"
                            },
                            GridLines = new BitChartGridLines
                            {
                                Color = "gray"
                            }
                        }
                    ]
                }
            }
        };

        _config.Data.Labels.AddRange(BitChartDemoUtils.GetNextDays(INITAL_COUNT).Select(d => d.ToString("o")));

        var dataset1 = new BitChartScatterDataset(BitChartDemoUtils.CreateRandomPoints(10))
        {
            Label = "My first dataset",
            BackgroundColor = BitChartColorUtil.FromDrawingColor(BitChartDemoColors.Red),
            BorderColor = BitChartColorUtil.FromDrawingColor(BitChartDemoColors.Red),
        };

        var dataset2 = new BitChartScatterDataset(BitChartDemoUtils.CreateRandomPoints(10))
        {
            Label = "My second dataset",
            BackgroundColor = BitChartColorUtil.FromDrawingColor(BitChartDemoColors.Blue),
            BorderColor = BitChartColorUtil.FromDrawingColor(BitChartDemoColors.Blue),
        };

        var dataset3 = new BitChartScatterDataset(BitChartDemoUtils.CreateRandomPoints(10))
        {
            Label = "Dataset with point data",
            BackgroundColor = BitChartColorUtil.FromDrawingColor(BitChartDemoColors.Green),
            BorderColor = BitChartColorUtil.FromDrawingColor(BitChartDemoColors.Green),
        };

        _config.Data.Datasets.Add(dataset1);
        _config.Data.Datasets.Add(dataset2);
        _config.Data.Datasets.Add(dataset3);
    }

    private void RandomizeData()
    {
        foreach (IBitChartDataset dataset in _config.Data.Datasets)
        {
            if (dataset is BitChartScatterDataset scatterDataset)
            {
                int count = scatterDataset.Count;
                scatterDataset.Clear();
                scatterDataset.AddRange(BitChartDemoUtils.CreateRandomPoints(count));
            }
        }

        _chart.Update();
    }

    private void AddDataset()
    {
        string color = BitChartColorUtil.FromDrawingColor(BitChartDemoColors.All[_config.Data.Datasets.Count % BitChartDemoColors.All.Count]);
        var newDataset = new BitChartScatterDataset(BitChartDemoUtils.CreateRandomPoints(_config.Data.Labels.Count))
        {
            Label = $"Dataset {_config.Data.Datasets.Count + 1}",
            BackgroundColor = color,
            BorderColor = color,
        };

        _config.Data.Datasets.Add(newDataset);
        _chart.Update();
    }

    private void RemoveDataset()
    {
        IList<IBitChartDataset> datasets = _config.Data.Datasets;

        if (datasets.Count == 0) return;

        datasets.RemoveAt(datasets.Count - 1);
        _chart.Update();
    }



    private readonly string razorCode = @"
<BitChart Config=""_config"" IsDateAdapterRequired=""true"" @ref=""_chart"" />

<BitButton OnClick=""RandomizeData"">Randomize Data</BitButton>
<BitButton OnClick=""AddDataset"">Add Dataset</BitButton>
<BitButton OnClick=""RemoveDataset"">Remove Dataset</BitButton>";
    private readonly string csharpCode = @"
private const int INITAL_COUNT = 5;

private BitChart _chart = default!;
private BitChartScatterConfig _config = default!;

protected override void OnInitialized()
{
    _config = new BitChartScatterConfig() 
    { 
        Options = new BitChartLineOptions
        {
            Scales = new BitChartScales
            {
                XAxes =
                [
                    new BitChartLinearCartesianAxis
                    {
                        ScaleLabel = new BitChartScaleLabel
                        {
                            LabelString = ""Value""
                        },
                        GridLines = new BitChartGridLines
                        {
                            Color = ""gray""
                        }
                    }
                ],
                YAxes =
                [
                    new BitChartLinearCartesianAxis
                    {
                        ScaleLabel = new BitChartScaleLabel
                        {
                            LabelString = ""Value""
                        },
                        GridLines = new BitChartGridLines
                        {
                            Color = ""gray""
                        }
                    }
                ]
            }
        }
    };

    _config.Data.Labels.AddRange(BitChartDemoUtils.GetNextDays(INITAL_COUNT).Select(d => d.ToString(""o"")));

    var dataset1 = new BitChartScatterDataset(BitChartDemoUtils.CreateRandomPoints(10))
    {
        Label = ""My first dataset"",
        BackgroundColor = BitChartColorUtil.FromDrawingColor(BitChartDemoColors.Red),
        BorderColor = BitChartColorUtil.FromDrawingColor(BitChartDemoColors.Red),
    };

    var dataset2 = new BitChartScatterDataset(BitChartDemoUtils.CreateRandomPoints(10))
    {
        Label = ""My second dataset"",
        BackgroundColor = BitChartColorUtil.FromDrawingColor(BitChartDemoColors.Blue),
        BorderColor = BitChartColorUtil.FromDrawingColor(BitChartDemoColors.Blue),
    };

    var dataset3 = new BitChartScatterDataset(BitChartDemoUtils.CreateRandomPoints(10))
    {
        Label = ""Dataset with point data"",
        BackgroundColor = BitChartColorUtil.FromDrawingColor(BitChartDemoColors.Green),
        BorderColor = BitChartColorUtil.FromDrawingColor(BitChartDemoColors.Green),
    };

    _config.Data.Datasets.Add(dataset1);
    _config.Data.Datasets.Add(dataset2);
    _config.Data.Datasets.Add(dataset3);
}

private void RandomizeData()
{
    foreach (IBitChartDataset dataset in _config.Data.Datasets)
    {
        if (dataset is BitChartScatterDataset scatterDataset)
        {
            int count = scatterDataset.Count;
            scatterDataset.Clear();
            scatterDataset.AddRange(BitChartDemoUtils.CreateRandomPoints(count));
        }
    }

    _chart.Update();
}

private void AddDataset()
{
    string color = BitChartColorUtil.FromDrawingColor(BitChartDemoColors.All[_config.Data.Datasets.Count % BitChartDemoColors.All.Count]);
    var newDataset = new BitChartScatterDataset(BitChartDemoUtils.CreateRandomPoints(_config.Data.Labels.Count))
    {
        Label = $""Dataset {_config.Data.Datasets.Count + 1}"",
        BackgroundColor = color,
        BorderColor = color,
    };

    _config.Data.Datasets.Add(newDataset);
    _chart.Update();
}

private void RemoveDataset()
{
    IList<IBitChartDataset> datasets = _config.Data.Datasets;

    if (datasets.Count == 0) return;

    datasets.RemoveAt(datasets.Count - 1);
    _chart.Update();
}

public static class BitChartDemoColors
{
    private static readonly Lazy<IReadOnlyList<System.Drawing.Color>> _all = new(() =>
    [
        Red, Orange, Yellow, Green, Blue, Purple, Grey
    ]);

    public static IReadOnlyList<System.Drawing.Color> All => _all.Value;

    public static readonly System.Drawing.Color Red = System.Drawing.Color.FromArgb(255, 99, 132);
    public static readonly System.Drawing.Color Orange = System.Drawing.Color.FromArgb(255, 159, 64);
    public static readonly System.Drawing.Color Yellow = System.Drawing.Color.FromArgb(255, 205, 86);
    public static readonly System.Drawing.Color Green = System.Drawing.Color.FromArgb(75, 192, 192);
    public static readonly System.Drawing.Color Blue = System.Drawing.Color.FromArgb(54, 162, 235);
    public static readonly System.Drawing.Color Purple = System.Drawing.Color.FromArgb(153, 102, 255);
    public static readonly System.Drawing.Color Grey = System.Drawing.Color.FromArgb(201, 203, 207);
}

public static class BitChartDemoUtils
{
    public static readonly Random _rng = new();

    public static IReadOnlyList<string> Months { get; } = new ReadOnlyCollection<string>(
    [
        ""January"", ""February"", ""March"", ""April"", ""May"", ""June"", ""July"", ""August"", ""September"", ""October"", ""November"", ""December""
    ]);

    private static int RandomScalingFactorThreadUnsafe(int min, int max) => _rng.Next(min, max);

    public static int RandomScalingFactor()
    {
        lock (_rng)
        {
            return RandomScalingFactorThreadUnsafe(0, 100);
        }
    }

    public static IEnumerable<int> RandomScalingFactor(int count, int min = 0, int max = 100)
    {
        int[] factors = new int[count];
        lock (_rng)
        {
            for (int i = 0; i < count; i++)
            {
                factors[i] = RandomScalingFactorThreadUnsafe(min, max);
            }
        }

        return factors;
    }

    public static IEnumerable<DateTime> GetNextDays(int count)
    {
        DateTime now = DateTime.Now;
        DateTime[] factors = new DateTime[count];
        for (int i = 0; i < factors.Length; i++)
        {
            factors[i] = now.AddDays(i);
        }

        return factors;
    }

    public static List<BitChartPoint> CreateRandomPoints(int count)
    {
        List<BitChartPoint> points = new();

        for (int i = 0; i < count; i++)
        {
            double x = RandomScalingFactor();
            double y = RandomScalingFactor();

            points.Add(new BitChartPoint(x, y));
        }

        return points;
    }
}

public static class IListExtensions
{
    public static void AddRange<T>(this IList<T> list, IEnumerable<T> items)
    {
        if (list == null)
            throw new ArgumentNullException(nameof(list));

        if (items == null)
            throw new ArgumentNullException(nameof(items));

        if (list is List<T> asList)
        {
            asList.AddRange(items);
        }
        else
        {
            foreach (T item in items)
            {
                list.Add(item);
            }
        }
    }
}";
}
