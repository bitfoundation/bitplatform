using System.Numerics;

namespace Bit.Bmage;

/// <summary>A zoom gesture.</summary>
/// <param name="Factor">Multiplier to apply to the current zoom.</param>
/// <param name="Focus">The point in image coordinates that should stay under the pointer.</param>
public readonly record struct BmageZoomArgs(double Factor, Vector2 Focus);
