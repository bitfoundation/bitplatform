using System;
using System.Collections.Generic;

namespace Bit.BlazorUI.Playground.Web.Pages.Components.Chart;

public static class ChartUtils
{
    private static readonly Random _rng = new Random();
    private static int RandomScalingFactorThreadUnsafe() => _rng.Next(-100, 100);

    public static IEnumerable<int> RandomScalingFactor(int count)
    {
        int[] factors = new int[count];
        lock (_rng)
        {
            for (int i = 0; i < count; i++)
            {
                factors[i] = RandomScalingFactorThreadUnsafe();
            }
        }

        return factors;
    }
}
