namespace Bit.BlazorUI;

/// <summary>Context passed to per-segment styling callbacks for a line.</summary>
public readonly record struct BitChartSegmentContext(
    int StartIndex,
    int EndIndex,
    double StartValue,
    double EndValue);
