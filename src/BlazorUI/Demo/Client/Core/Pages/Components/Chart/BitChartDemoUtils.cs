using System.Collections.ObjectModel;

namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Chart;

public static class BitChartDemoUtils
{
    public static readonly Random _rng = new Random();

    public static IReadOnlyList<string> Months { get; } = new ReadOnlyCollection<string>(new[]
    {
            "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December"
    });

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
}
