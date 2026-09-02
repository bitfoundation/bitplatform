namespace Bit.Butil;

/// <summary>
/// The JS-shaped panner settings: the same members as <see cref="AudioPannerOptions"/>, with the two
/// models already the strings the specification uses.
/// </summary>
internal class AudioPannerJsOptions
{
    public string PanningModel { get; set; } = string.Empty;

    public string DistanceModel { get; set; } = string.Empty;

    public double PositionX { get; set; }

    public double PositionY { get; set; }

    public double PositionZ { get; set; }

    public double OrientationX { get; set; }

    public double OrientationY { get; set; }

    public double OrientationZ { get; set; }

    public double RefDistance { get; set; }

    public double MaxDistance { get; set; }

    public double RolloffFactor { get; set; }

    public double ConeInnerAngle { get; set; }

    public double ConeOuterAngle { get; set; }

    public double ConeOuterGain { get; set; }
}
