namespace Bit.BlazorUI;

/// <summary>How a line/area dataset fills relative to a baseline.</summary>
public enum BitChartFillMode
{
    None,
    Origin,
    Start,
    End,
    Stack,
    /// <summary>Fill to another dataset's line (see <see cref="BitChartDataset.FillTargetIndex"/>).</summary>
    Dataset,
    /// <summary>Fill to an absolute axis value (see <see cref="BitChartDataset.FillValue"/>).</summary>
    Value
}
