namespace Bit.BlazorUI;

/// <summary>
/// The bar thickness used to customize all bar axes (extended cartesian axes in <see cref="ChartJs.Blazor.BarChart.Axes"/>).
/// <para>As per documentation <a href="https://www.chartjs.org/docs/latest/charts/bar.html#barthickness">here (Chart.js)</a>.</para>
/// </summary>
public class BarThickness : ObjectEnum
{
    /// <summary>
    /// Creates a new instance of the <see cref="BarThickness" /> class.
    /// </summary>
    /// <param name="stringValue">The <see cref="string" /> value to set.</param>
    private BarThickness(string stringValue) : base(stringValue) { }

    /// <summary>
    /// Creates a new instance of the <see cref="BarThickness" /> class.
    /// </summary>
    /// <param name="doubleValue">The <see cref="double" /> value to set.</param>
    private BarThickness(double doubleValue) : base(doubleValue) { }

    /// <summary>
    /// Gets a <see cref="BarThickness"/> from a <see cref="double"/> value (in pixels).
    /// </summary>
    /// <param name="thickness">The thickness value in pixels.</param>
    /// <returns>A <see cref="BarThickness"/> from a <see cref="double"/> value (in pixels).</returns>
    public static BarThickness Absolute(double thickness) => new BarThickness(thickness);

    /// <summary>
    /// Converts a <see cref="double"/> value to a <see cref="BarThickness"/> value.
    /// </summary>
    /// <param name="thickness">The thickness value in pixels.</param>
    public static implicit operator BarThickness(double thickness) => new BarThickness(thickness);

    /// <summary>
    /// If set to <see cref="Flex"/>, the base sample widths are calculated automatically
    /// based on the previous and following samples so that they take the full available widths without overlap.
    /// </summary>
    public static BarThickness Flex => new BarThickness("flex");
}
