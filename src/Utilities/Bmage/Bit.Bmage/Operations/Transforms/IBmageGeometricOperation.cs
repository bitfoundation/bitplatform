using System.Drawing;
using System.Numerics;

namespace Bit.Bmage;

/// <summary>
/// An operation whose effect on coordinates can be described by an affine matrix. Used to keep annotations attached to
/// image content when geometric edits are inserted before them, and by the pipeline optimiser.
/// </summary>
public interface IBmageGeometricOperation : IBmageImageOperation
{
    /// <summary>
    /// Returns the matrix mapping input pixel coordinates to output pixel coordinates for an input of the given size,
    /// or null when the mapping is not affine (e.g. perspective warps).
    /// </summary>
    Matrix3x2? GetTransform(Size inputSize);
}
