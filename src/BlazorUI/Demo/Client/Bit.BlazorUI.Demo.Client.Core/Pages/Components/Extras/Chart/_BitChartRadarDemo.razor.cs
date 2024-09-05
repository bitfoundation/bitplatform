namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.Chart;

public partial class _BitChartRadarDemo
{
    private const int INITAL_COUNT = 8;

    private BitChart _chart = default!;
    private BitChartRadarConfig _config = default!;

    protected override void OnInitialized()
    {
        _config = new BitChartRadarConfig
        {
            Options = new BitChartRadarOptions()
            {
                Responsive = true,
                Title = new BitChartOptionsTitle
                {
                    Display = true,
                    Text = "BitChart Radar Chart"
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

        IDataset<int> dataset1 = new BitChartRadarDataset<int>(BitChartDemoUtils.RandomScalingFactor(INITAL_COUNT))
        {
            Label = "Dataset 1",
            BackgroundColor = BitChartColorUtil.FromDrawingColor(System.Drawing.Color.FromArgb(128, BitChartDemoColors.Red))
        };

        IDataset<int> dataset2 = new BitChartRadarDataset<int>(BitChartDemoUtils.RandomScalingFactor(INITAL_COUNT))
        {
            Label = "Dataset 2",
            BackgroundColor = BitChartColorUtil.FromDrawingColor(System.Drawing.Color.FromArgb(128, BitChartDemoColors.Blue))
        };

        IDataset<int> dataset3 = new BitChartRadarDataset<int>(BitChartDemoUtils.RandomScalingFactor(INITAL_COUNT))
        {
            Label = "Dataset 3",
            BackgroundColor = BitChartColorUtil.FromDrawingColor(System.Drawing.Color.FromArgb(128, BitChartDemoColors.Orange))
        };


        _config.Data.Datasets.Add(dataset1);
        _config.Data.Datasets.Add(dataset2);
        _config.Data.Datasets.Add(dataset3);
        _config.Data.Labels.AddRange(BitChartDemoUtils.Months.Take(INITAL_COUNT));
    }

    private void RandomizeRadarData()
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

    private void AddRadarDataset()
    {
        System.Drawing.Color color = BitChartDemoColors.All[_config.Data.Datasets.Count % BitChartDemoColors.All.Count];
        IDataset<int> dataset = new BitChartRadarDataset<int>(BitChartDemoUtils.RandomScalingFactor(_config.Data.Labels.Count))
        {
            Label = $"Dataset {_config.Data.Datasets.Count + 1}",
            BackgroundColor = BitChartColorUtil.FromDrawingColor(System.Drawing.Color.FromArgb(128, color)),
            BorderColor = BitChartColorUtil.FromDrawingColor(color),
            BorderWidth = 1
        };

        _config.Data.Datasets.Add(dataset);
        _chart.Update();
    }

    private void RemoveRadarDataset()
    {
        IList<IBitChartDataset> datasets = _config.Data.Datasets;
        if (datasets.Count == 0)
            return;

        datasets.RemoveAt(datasets.Count - 1);
        _chart.Update();
    }

    private void AddRadarData()
    {
        if (_config.Data.Datasets.Count == 0)
            return;

        string month = BitChartDemoUtils.Months[_config.Data.Labels.Count % BitChartDemoUtils.Months.Count];
        _config.Data.Labels.Add(month);

        foreach (IDataset<int> dataset in _config.Data.Datasets)
        {
            dataset.Add(BitChartDemoUtils.RandomScalingFactor());
        }

        _chart.Update();
    }

    private void RemoveRadarData()
    {
        if (_config.Data.Datasets.Count == 0 ||
            _config.Data.Labels.Count == 0)
        {
            return;
        }

        _config.Data.Labels.RemoveAt(_config.Data.Labels.Count - 1);

        foreach (IDataset<int> dataset in _config.Data.Datasets)
        {
            dataset.RemoveAt(dataset.Count - 1);
        }

        _chart.Update();
    }



    private readonly string razorCode = @"
<BitChart Config=""_config"" @ref=""_chart"" />

<BitButton OnClick=""RandomizeRadarData"">Randomize Data</BitButton>
<BitButton OnClick= ""AddRadarDataset"" > Add Dataset</BitButton>
<BitButton OnClick= ""RemoveRadarDataset"" > Remove Dataset</BitButton>
<BitButton OnClick= ""AddRadarData"" > Add Data</BitButton>
<BitButton OnClick= ""RemoveRadarData"" > Remove Data</BitButton>";
    private readonly string csharpCode = @"
private const int INITAL_COUNT = 8;

private BitChart _chart = default!;
private BitChartRadarConfig _config = default!;

protected override void OnInitialized()
{
    _config = new BitChartRadarConfig
    {
        Options = new BitChartRadarOptions()
        {
            Responsive = true,
            Title = new BitChartOptionsTitle
            {
                Display = true,
                Text = ""BitChart Radar Chart""
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

    IDataset<int> dataset1 = new BitChartRadarDataset<int>(BitChartDemoUtils.RandomScalingFactor(INITAL_COUNT))
    {
        Label = ""Dataset 1"",
        BackgroundColor = BitChartColorUtil.FromDrawingColor(System.Drawing.Color.FromArgb(128, BitChartDemoColors.Red))
    };

    IDataset<int> dataset2 = new BitChartRadarDataset<int>(BitChartDemoUtils.RandomScalingFactor(INITAL_COUNT))
    {
        Label = ""Dataset 2"",
        BackgroundColor = BitChartColorUtil.FromDrawingColor(System.Drawing.Color.FromArgb(128, BitChartDemoColors.Blue))
    };

    IDataset<int> dataset3 = new BitChartRadarDataset<int>(BitChartDemoUtils.RandomScalingFactor(INITAL_COUNT))
    {
        Label = ""Dataset 3"",
        BackgroundColor = BitChartColorUtil.FromDrawingColor(System.Drawing.Color.FromArgb(128, BitChartDemoColors.Orange))
    };


    _config.Data.Datasets.Add(dataset1);
    _config.Data.Datasets.Add(dataset2);
    _config.Data.Datasets.Add(dataset3);
}

private void RandomizeRadarData()
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

private void AddRadarDataset()
{
    System.Drawing.Color color = BitChartDemoColors.All[_config.Data.Datasets.Count % BitChartDemoColors.All.Count];
    IDataset<int> dataset = new BitChartRadarDataset<int>(BitChartDemoUtils.RandomScalingFactor(_config.Data.Labels.Count))
    {
        Label = $""Dataset {_config.Data.Datasets.Count + 1}"",
        BackgroundColor = BitChartColorUtil.FromDrawingColor(System.Drawing.Color.FromArgb(128, color)),
        BorderColor = BitChartColorUtil.FromDrawingColor(color),
        BorderWidth = 1
    };

    _config.Data.Datasets.Add(dataset);
    _chart.Update();
}

private void RemoveRadarDataset()
{
    IList<IBitChartDataset> datasets = _config.Data.Datasets;
    if (datasets.Count == 0)
        return;

    datasets.RemoveAt(datasets.Count - 1);
    _chart.Update();
}

private void AddRadarData()
{
    if (_config.Data.Datasets.Count == 0)
        return;

    string month = BitChartDemoUtils.Months[_config.Data.Labels.Count % BitChartDemoUtils.Months.Count];
    _config.Data.Labels.Add(month);

    foreach (IDataset<int> dataset in _config.Data.Datasets)
    {
        dataset.Add(BitChartDemoUtils.RandomScalingFactor());
    }

    _chart.Update();
}

private void RemoveRadarData()
{
    if (_config.Data.Datasets.Count == 0 ||
        _config.Data.Labels.Count == 0)
    {
        return;
    }

    _config.Data.Labels.RemoveAt(_config.Data.Labels.Count - 1);

    foreach (IDataset<int> dataset in _config.Data.Datasets)
    {
        dataset.RemoveAt(dataset.Count - 1);
    }

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
