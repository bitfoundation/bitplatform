namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.Chart;

public partial class _BitChartPolarAreaDemo
{
    private const int INITAL_COUNT = 6;

    private BitChart _chart = default!;
    private BitChartPolarAreaConfig _config = default!;

    protected override void OnInitialized()
    {
        _config = new BitChartPolarAreaConfig
        {
            Options = new BitChartPolarAreaOptions
            {
                Responsive = true,
                Title = new BitChartOptionsTitle
                {
                    Display = true,
                    Text = "BitChart PolarArea Chart"
                },
                Scale = new BitChartLinearRadialAxis
                {
                    GridLines = new BitChartGridLines
                    {
                        Color = "gray"
                    }
                }
            }
        };

        var dataset = new BitChartPolarAreaDataset<int>(BitChartDemoUtils.RandomScalingFactor(INITAL_COUNT))
        {
            BackgroundColor = BitChartDemoColors.All.Take(INITAL_COUNT).Select(color => BitChartColorUtil.FromDrawingColor(color)).ToArray()
        };
        _config.Data.Labels.AddRange(BitChartDemoUtils.Months.Take(INITAL_COUNT));
        _config.Data.Datasets.Add(dataset);
    }

    private void RandomizePolarAreaData()
    {
        foreach (IDataset<int> dataset in _config.Data.Datasets)
        {
            int count = dataset.Count;
            dataset.Clear();
            for (int i = 0; i < count; i++)
            {
                if (BitChartDemoUtils._rng.NextDouble() < 0.2)
                {
                    dataset.Add(0);
                }
                else
                {
                    dataset.Add(BitChartDemoUtils.RandomScalingFactor());
                }
            }
        }

        _chart.Update();
    }

    private void AddPolarAreaDataset()
    {
        int count = _config.Data.Labels.Count;
        BitChartPolarAreaDataset<int> dataset = new BitChartPolarAreaDataset<int>(BitChartDemoUtils.RandomScalingFactor(count, -100, 100))
        {
            BackgroundColor = BitChartDemoColors.All.Take(count).Select(color => BitChartColorUtil.FromDrawingColor(color)).ToArray()
        };

        _config.Data.Datasets.Add(dataset);
        _chart.Update();
    }

    private void RemovePolarAreaDataset()
    {
        IList<IBitChartDataset> datasets = _config.Data.Datasets;
        if (datasets.Count == 0)
            return;

        datasets.RemoveAt(datasets.Count - 1);
        _chart.Update();
    }



    private readonly string razorCode = @"
<BitChart Config=""_config"" @ref=""_chart"" />

<BitButton OnClick=""RandomizePolarAreaData"">Randomize Data</BitButton>
<BitButton OnClick= ""AddPolarAreaDataset"" > Add Dataset</BitButton>
<BitButton OnClick= ""RemovePolarAreaDataset"" > Remove Dataset</BitButton>";
    private readonly string csharpCode = @"
private const int INITAL_COUNT = 5;

private BitChart _chart = default!;
private BitChartPolarAreaConfig _config = default!;

protected override void OnInitialized()
{
    _config = new BitChartPolarAreaConfig
    {
        Options = new BitChartPolarAreaOptions
        {
            Responsive = true,
            Title = new BitChartOptionsTitle
            {
                Display = true,
                Text = ""BitChart PolarArea Chart""
            },
            Scale = new BitChartLinearRadialAxis
            {
                GridLines = new BitChartGridLines
                {
                    Color = ""gray""
                }
            }
        }
    };

    var dataset = new BitChartPolarAreaDataset<int>(BitChartDemoUtils.RandomScalingFactor(INITAL_COUNT))
    {
        BackgroundColor = BitChartDemoColors.All.Take(INITAL_COUNT).Select(color => BitChartColorUtil.FromDrawingColor(color)).ToArray()
    };
    _config.Data.Labels.AddRange(BitChartDemoUtils.Months.Take(INITAL_COUNT));
    _config.Data.Datasets.Add(dataset);
}

private void RandomizePolarAreaData()
{
    foreach (IDataset<int> dataset in _config.Data.Datasets)
    {
        int count = dataset.Count;
        dataset.Clear();
        for (int i = 0; i < count; i++)
        {
            if (BitChartDemoUtils._rng.NextDouble() < 0.2)
            {
                dataset.Add(0);
            }
            else
            {
                dataset.Add(BitChartDemoUtils.RandomScalingFactor());
            }
        }
    }

    _chart.Update();
}

private void AddPolarAreaDataset()
{
    int count = _config.Data.Labels.Count;
    BitChartPolarAreaDataset<int> dataset = new BitChartPolarAreaDataset<int>(BitChartDemoUtils.RandomScalingFactor(count, -100, 100))
    {
        BackgroundColor = BitChartDemoColors.All.Take(count).Select(color => BitChartColorUtil.FromDrawingColor(color)).ToArray()
    };

    _config.Data.Datasets.Add(dataset);
    _chart.Update();
}

private void RemovePolarAreaDataset()
{
    IList<IBitChartDataset> datasets = _config.Data.Datasets;
    if (datasets.Count == 0)
        return;

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
