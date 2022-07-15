namespace Bit.BlazorUI;

/// <summary>
/// The angle lines sub-config of the linear-radial-axis-configuration (see <see cref="LinearRadialAxis"/>).
/// <para>As per documentation <a href="https://www.chartjs.org/docs/latest/axes/cartesian/linear.html">here (Chart.js)</a>.</para>
/// </summary>
public class AngleLines
{
    /// <summary>
    /// Gets or sets the value indicating whether the angle line is displayed or not.
    /// </summary>
    public bool? Display { get; set; }

    /// <summary>
    /// Gets or sets the color of the angled lines.
    /// <para>See <see cref="ColorUtil"/> for working with colors.</para>
    /// </summary>
    public string Color { get; set; }

    /// <summary>
    /// Gets or sets the width of the angled lines.
    /// </summary>
    public int? LineWidth { get; set; }

    /// <summary>
    /// Gets or sets the length and spacing of dashes of the angled lines.
    /// <para>As per documentation <a href="https://developer.mozilla.org/en-US/docs/Web/API/CanvasRenderingContext2D/setLineDash">here (MDN)</a>.</para>
    /// </summary>
    public int[] BorderDash { get; set; }

    /// <summary>
    /// Gets or sets the offset for line dashes.
    /// <para>As per documentation <a href="https://developer.mozilla.org/en-US/docs/Web/API/CanvasRenderingContext2D/lineDashOffset">here (MDN)</a>.</para>
    /// </summary>
    public double? BorderDashOffset { get; set; }
}
