using System.Globalization;

namespace Bit.BlazorUI;

/// <summary>A computed tick on an axis.</summary>
public readonly record struct BitChartAxisTick(double Value, string Label, double Pixel, bool Minor = false);
