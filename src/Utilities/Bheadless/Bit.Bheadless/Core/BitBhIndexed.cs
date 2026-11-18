namespace Bit.Bheadless;

/// <summary>A value paired with its position in a list (used for virtualized option lists).</summary>
/// <typeparam name="T">The value type.</typeparam>
public readonly record struct BitBhIndexed<T>(int Index, T Value);
