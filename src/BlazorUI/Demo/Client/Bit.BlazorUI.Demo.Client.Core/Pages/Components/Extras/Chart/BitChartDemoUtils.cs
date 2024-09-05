using System.Collections.ObjectModel;

namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.Chart;

public static class BitChartDemoUtils
{
    public static readonly Random _rng = new();

    public static IReadOnlyList<string> Months { get; } = new ReadOnlyCollection<string>(
    [
        "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December"
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
        var factors = new int[count];
        lock (_rng)
        {
            for (var i = 0; i < count; i++)
            {
                factors[i] = RandomScalingFactorThreadUnsafe(min, max);
            }
        }

        return factors;
    }

    public static IEnumerable<DateTimeOffset> GetNextDays(int count)
    {
        var now = DateTimeOffset.Now;
        var factors = new DateTimeOffset[count];

        for (var i = 0; i < factors.Length; i++)
        {
            factors[i] = now.AddDays(i);
        }

        return factors;
    }

    public static List<BitChartPoint> CreateRandomPoints(int count)
    {
        List<BitChartPoint> points = [];

        for (int i = 0; i < count; i++)
        {
            double x = RandomScalingFactor();
            double y = RandomScalingFactor();

            points.Add(new BitChartPoint(x, y));
        }

        return points;
    }

    public static List<BitChartBubblePoint> CreateRandomBubblePoints(int count)
    {
        List<BitChartBubblePoint> points = [];

        for (int i = 0; i < count; i++)
        {
            double x = RandomScalingFactor();
            double y = RandomScalingFactor();
            double radius = RandomScalingFactor() % 20 + 5;

            points.Add(new BitChartBubblePoint(x, y, radius));
        }

        return points;
    }
}
