namespace Bit.BlazorUI;

/// <summary>How a line/area dataset fills relative to a baseline.</summary>
public enum BitChartFillMode
{
    /// <summary>No area fill.</summary>
    None,
    /// <summary>Fill to zero (clamped into the axis range).</summary>
    Origin,
    /// <summary>Fill to the low end of the axis.</summary>
    Start,
    /// <summary>Fill to the high end of the axis.</summary>
    End,
    /// <summary>Fill to the series stacked below this one. Needs a stacked value axis; without one it
    /// behaves like <see cref="Origin"/>.</summary>
    Stack,
    /// <summary>Fill to another dataset's line (see <see cref="BitChartDataset.FillTargetIndex"/>).</summary>
    Dataset,
    /// <summary>Fill to an absolute axis value (see <see cref="BitChartDataset.FillValue"/>).</summary>
    Value
}
