namespace Bit.BlazorUI;

/// <summary>A single (x, y) or (x, y, r) data point used by scatter and bubble charts.</summary>
public readonly record struct BitChartDataPoint(double X, double Y, double? R = null);
