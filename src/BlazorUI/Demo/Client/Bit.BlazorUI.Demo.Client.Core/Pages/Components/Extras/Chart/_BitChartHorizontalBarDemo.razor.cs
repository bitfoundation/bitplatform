namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.Chart;

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
                },
                Scales = new BitChartBarScales
                {
                    XAxes =
                    [
                        new BitChartLinearCartesianAxis
                        {
                            GridLines = new BitChartGridLines
                            {
                                Color = "gray"
                            }
                        }
                    ],
                    YAxes =
                    [
                        new BitChartBarCategoryAxis
                        {
                            GridLines = new BitChartGridLines
                            {
                                Color = "gray"
                            }
                        }
                    ]
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



    private readonly string razorCode = @"
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
            },
            Scales = new BitChartBarScales
            {
                XAxes =
                [
                    new BitChartLinearCartesianAxis
                    {
                        GridLines = new BitChartGridLines
                        {
                            Color = ""gray""
                        }
                    }
                ],
                YAxes =
                [
                    new BitChartBarCategoryAxis
                    {
                        GridLines = new BitChartGridLines
                        {
                            Color = ""gray""
                        }
                    }
                ]
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
