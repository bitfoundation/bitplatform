namespace Bit.Bmotion;

/// <summary>
/// Piecewise-linear mapping from an input range onto an output range - the maths behind both
/// <see cref="Bm.MapRange"/> and <see cref="BmValue{T}.Transform(double[], double[], bool)"/>, so
/// the one-shot and the reactive form can never drift apart.
/// </summary>
internal static class BmRangeMap
{
    /// <summary>
    /// Validates a range pair for use with <see cref="Map"/>, throwing the same diagnostics the
    /// reactive and one-shot entry points both need.
    /// </summary>
    public static void Validate(double[] inputRange, double[] outputRange)
    {
        if (inputRange.Length != outputRange.Length)
            throw new ArgumentException("inputRange and outputRange must have the same length.");
        if (inputRange.Length < 2)
            throw new ArgumentException("inputRange and outputRange must contain at least 2 points.");
        for (int i = 0; i < inputRange.Length - 1; i++)
            if (inputRange[i + 1] <= inputRange[i])
                throw new ArgumentException("inputRange must be strictly increasing (no repeated or decreasing points).");
    }

    /// <summary>
    /// Maps <paramref name="value"/> through the range pair. Outside the input range the result
    /// either clamps to the nearest output end or extrapolates along the outermost segment.
    /// </summary>
    public static double Map(double value, double[] inputRange, double[] outputRange, bool clamp)
    {
        Validate(inputRange, outputRange);

        int last = inputRange.Length - 1;

        if (value <= inputRange[0])
            return clamp ? outputRange[0] : Lerp(value, inputRange[0], inputRange[1], outputRange[0], outputRange[1]);

        if (value >= inputRange[last])
            return clamp
                ? outputRange[last]
                : Lerp(value, inputRange[last - 1], inputRange[last], outputRange[last - 1], outputRange[last]);

        for (int i = 0; i < last; i++)
            if (value <= inputRange[i + 1])
                return Lerp(value, inputRange[i], inputRange[i + 1], outputRange[i], outputRange[i + 1]);

        return outputRange[last];
    }

    private static double Lerp(double value, double inFrom, double inTo, double outFrom, double outTo)
    {
        double t = (value - inFrom) / (inTo - inFrom);
        return outFrom + t * (outTo - outFrom);
    }
}
