using Bit.BlazorUI.Legacy;
namespace Bit.BlazorUI.Legacy.Demo.Chart;

public partial class _BitChartLegacyPieDemo
{
    private const int INITAL_COUNT = 5;

    private BitChartLegacy _chart = default!;
    private BitChartLegacyPieConfig _config = default!;

    protected override void OnInitialized()
    {
        _config = new BitChartLegacyPieConfig
        {
            Options = new BitChartLegacyPieOptions
            {
                Responsive = true,
                Title = new BitChartLegacyOptionsTitle
                {
                    Display = true,
                    Text = "BitChartLegacy Pie Chart"
                }
            }
        };

        var dataset = new BitChartLegacyPieDataset<int>(BitChartLegacyDemoUtils.RandomScalingFactor(INITAL_COUNT))
        {
            BackgroundColor = BitChartLegacyDemoColors.All.Take(INITAL_COUNT).Select(color => BitChartLegacyColorUtil.FromDrawingColor(color)).ToArray()
        };
        _config.Data.Labels.AddRange(BitChartLegacyDemoUtils.Months.Take(INITAL_COUNT));
        _config.Data.Datasets.Add(dataset);
    }

    private void RandomizePieData()
    {
        foreach (IDataset<int> dataset in _config.Data.Datasets)
        {
            int count = dataset.Count;
            dataset.Clear();
            for (int i = 0; i < count; i++)
            {
                if (BitChartLegacyDemoUtils._rng.NextDouble() < 0.2)
                {
                    dataset.Add(0);
                }
                else
                {
                    dataset.Add(BitChartLegacyDemoUtils.RandomScalingFactor());
                }
            }
        }

        _chart.Update();
    }

    private void AddPieDataset()
    {
        int count = _config.Data.Labels.Count;
        var dataset = new BitChartLegacyPieDataset<int>(BitChartLegacyDemoUtils.RandomScalingFactor(count, -100, 100))
        {
            BackgroundColor = BitChartLegacyDemoColors.All.Take(count).Select(color => BitChartLegacyColorUtil.FromDrawingColor(color)).ToArray()
        };

        _config.Data.Datasets.Add(dataset);
        _chart.Update();
    }

    private void RemovePieDataset()
    {
        IList<IBitChartLegacyDataset> datasets = _config.Data.Datasets;
        if (datasets.Count == 0)
            return;

        datasets.RemoveAt(0);
        _chart.Update();
    }



    private readonly string razorCode = @"
<BitChartLegacy Config=""_config"" @ref=""_chart"" />

<BitButton OnClick=""RandomizePieData"">Randomize Data</BitButton>
<BitButton OnClick=""AddPieDataset"">Add Dataset</BitButton>
<BitButton OnClick=""RemovePieDataset"">Remove Dataset</BitButton>";
    private readonly string csharpCode = @"
private const int INITAL_COUNT = 5;

private BitChartLegacy _chart = default!;
private BitChartLegacyPieConfig _config = default!;

protected override void OnInitialized()
{
    _config = new BitChartLegacyPieConfig
    {
        Options = new BitChartLegacyPieOptions
        {
            Responsive = true,
            Title = new BitChartLegacyOptionsTitle
            {
                Display = true,
                Text = ""BitChartLegacy Pie Chart""
            }
        }
    };

    
    var dataset = new BitChartLegacyPieDataset<int>(BitChartLegacyDemoUtils.RandomScalingFactor(INITAL_COUNT))
    {
        BackgroundColor = BitChartLegacyDemoColors.All.Take(INITAL_COUNT).Select(color => BitChartLegacyColorUtil.FromDrawingColor(color)).ToArray()
    };
    _config.Data.Labels.AddRange(BitChartLegacyDemoUtils.Months.Take(INITAL_COUNT));
    _config.Data.Datasets.Add(dataset);
}

private void RandomizePieData()
{
    foreach (IDataset<int> dataset in _config.Data.Datasets)
    {
        int count = dataset.Count;
        dataset.Clear();
        for (int i = 0; i < count; i++)
        {
            if (BitChartLegacyDemoUtils._rng.NextDouble() < 0.2)
            {
                dataset.Add(0);
            }
            else
            {
                dataset.Add(BitChartLegacyDemoUtils.RandomScalingFactor());
            }
        }
    }

    _chart.Update();
}

private void AddPieDataset()
{
    int count = _config.Data.Labels.Count;
    var dataset = new BitChartLegacyPieDataset<int>(BitChartLegacyDemoUtils.RandomScalingFactor(count, -100, 100))
    {
        BackgroundColor = BitChartLegacyDemoColors.All.Take(count).Select(color => BitChartLegacyColorUtil.FromDrawingColor(color)).ToArray()
    };

    _config.Data.Datasets.Add(dataset);
    _chart.Update();
}

private void RemovePieDataset()
{
    IList<IBitChartLegacyDataset> datasets = _config.Data.Datasets;
    if (datasets.Count == 0)
        return;

    datasets.RemoveAt(0);
    _chart.Update();
}

public static class BitChartLegacyDemoColors
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

public static class BitChartLegacyDemoUtils
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
