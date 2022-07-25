using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;

namespace Bit.BlazorUI.Playground.Web.Pages.Components.Chart;

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
