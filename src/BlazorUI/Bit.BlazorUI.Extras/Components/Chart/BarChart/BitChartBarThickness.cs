namespace Bit.BlazorUI;

/// <summary>
/// The bar thickness used to customize all bar axes.
/// <para>As per documentation <a href="https://www.chartjs.org/docs/latest/charts/bar.html#barthickness">here (Chart.js)</a>.</para>
/// </summary>
public class BitChartBarThickness : BitChartObjectEnum
{
    /// <summary>
    /// Creates a new instance of the <see cref="BitChartBarThickness" /> class.
    /// </summary>
    /// <param name="stringValue">The <see cref="string" /> value to set.</param>
    private BitChartBarThickness(string stringValue) : base(stringValue) { }

    /// <summary>
    /// Creates a new instance of the <see cref="BitChartBarThickness" /> class.
    /// </summary>
    /// <param name="doubleValue">The <see cref="double" /> value to set.</param>
    private BitChartBarThickness(double doubleValue) : base(doubleValue) { }

    /// <summary>
    /// Gets a <see cref="BitChartBarThickness"/> from a <see cref="double"/> value (in pixels).
    /// </summary>
    /// <param name="thickness">The thickness value in pixels.</param>
    /// <returns>A <see cref="BitChartBarThickness"/> from a <see cref="double"/> value (in pixels).</returns>
    public static BitChartBarThickness Absolute(double thickness) => new BitChartBarThickness(thickness);

    /// <summary>
    /// Converts a <see cref="double"/> value to a <see cref="BitChartBarThickness"/> value.
    /// </summary>
    /// <param name="thickness">The thickness value in pixels.</param>
    public static implicit operator BitChartBarThickness(double thickness) => new BitChartBarThickness(thickness);

    /// <summary>
    /// If set to <see cref="Flex"/>, the base sample widths are calculated automatically
    /// based on the previous and following samples so that they take the full available widths without overlap.
    /// </summary>
    public static BitChartBarThickness Flex => new BitChartBarThickness("flex");
}
