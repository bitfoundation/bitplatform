namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.Chart;

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



    private readonly string razorCode = @"
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
