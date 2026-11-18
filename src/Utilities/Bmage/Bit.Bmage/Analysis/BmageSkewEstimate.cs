namespace Bit.Bmage;

/// <summary>How far a document's lines are tilted, and how sure the estimate is.</summary>
/// <param name="Angle">
/// The clockwise rotation in degrees that levels the lines: pass it to <see cref="BmageImagePipeline.Straighten"/>
/// or <see cref="BmageImagePipeline.Rotate"/> as it is.
/// </param>
/// <param name="Confidence">
/// How clearly one angle stood out from the others, in [0,1]: about 0.9 for lines of text, and below 0.3 for scattered
/// marks with no lines among them. An estimate below about 0.3 is better left unapplied. Texture with a grain along the
/// pixel grid can score higher, but then the angle found is within a fraction of a degree of level.
/// </param>
public readonly record struct BmageSkewEstimate(double Angle, double Confidence);
