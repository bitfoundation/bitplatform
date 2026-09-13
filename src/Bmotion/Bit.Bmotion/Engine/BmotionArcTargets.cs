namespace Bit.Bmotion;

/// <summary>
/// The single owner of the rule "does this animation travel along an arc?".
/// <para>
/// Two places have to agree on the answer: the element state, which builds the coupled driver, and
/// the compositor offload, which must stand down when one applies (pre-sampling x and y
/// independently would sample the straight line between the endpoints and silently throw the bend
/// away). Keeping the predicate here means they can't drift into a state where the offload runs a
/// straight line for an animation the engine believed was curved.
/// </para>
/// </summary>
internal static class BmotionArcTargets
{
    /// <summary>
    /// Whether a configured arc applies to <paramref name="values"/>: a curve is defined by two
    /// endpoints, so it needs both <c>x</c> and <c>y</c> present as single finite numbers. A
    /// keyframe sequence on either axis already describes its own path and is left alone.
    /// </summary>
    public static bool Applies(Dictionary<string, object?> values)
        => values.TryGetValue("x", out var x) && values.TryGetValue("y", out var y)
           && IsSingleFiniteNumber(x) && IsSingleFiniteNumber(y);

    private static bool IsSingleFiniteNumber(object? value)
    {
        if (value is null) return false;
        // Sequences (keyframes) are not single values. Strings are excluded too: a string-valued
        // x/y is a CSS dimension, which the arc maths has no meaning for.
        if (value is string) return false;
        if (value is System.Collections.IEnumerable) return false;
        try
        {
            double result = Convert.ToDouble(value, System.Globalization.CultureInfo.InvariantCulture);
            return double.IsFinite(result);
        }
        catch (Exception e) when (e is FormatException or InvalidCastException or OverflowException)
        {
            return false;
        }
    }
}
