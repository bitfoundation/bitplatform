namespace Bit.Butil;

/// <summary>
/// Where a sound is, which way it faces, and how it fades - the settings of a
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/PannerNode">PannerNode</see>.
/// </summary>
/// <remarks>
/// Positions are in the same arbitrary units as <see cref="WebAudio.SetListener"/>, and only the
/// relationship between the two matters. The defaults put the source at the origin facing along the
/// positive x axis, which is where an unconfigured listener also is - so a panner left alone sounds
/// like no panner at all.
/// <br/>
/// Everything here can also be changed later: the positions and orientations are AudioParams
/// (<see cref="AudioNodeHandle.SetParam"/> with <c>"positionX"</c> and friends), the rest are plain
/// properties (<see cref="AudioNodeHandle.SetProperty(string, double)"/>).
/// </remarks>
public class AudioPannerOptions
{
    /// <summary>How the position is turned into what each ear hears.</summary>
    public AudioPanningModel PanningModel { get; set; } = AudioPanningModel.EqualPower;

    /// <summary>How loudness falls off with distance.</summary>
    public AudioDistanceModel DistanceModel { get; set; } = AudioDistanceModel.Inverse;

    /// <summary>Position along the x axis - positive is to the listener's right.</summary>
    public double PositionX { get; set; }

    /// <summary>Position along the y axis - positive is up.</summary>
    public double PositionY { get; set; }

    /// <summary>Position along the z axis - negative is in front of the listener.</summary>
    public double PositionZ { get; set; }

    /// <summary>Which way the source faces, x component. Only matters when the cone angles are set.</summary>
    public double OrientationX { get; set; } = 1;

    /// <summary>Which way the source faces, y component.</summary>
    public double OrientationY { get; set; }

    /// <summary>Which way the source faces, z component.</summary>
    public double OrientationZ { get; set; }

    /// <summary>The distance at which no attenuation is applied yet. Everything closer is at full volume.</summary>
    public double RefDistance { get; set; } = 1;

    /// <summary>The distance beyond which the sound no longer gets quieter.</summary>
    public double MaxDistance { get; set; } = 10000;

    /// <summary>How quickly the sound fades with distance. Higher fades faster.</summary>
    public double RolloffFactor { get; set; } = 1;

    /// <summary>The angle, in degrees, inside which the source is at full volume.</summary>
    public double ConeInnerAngle { get; set; } = 360;

    /// <summary>The angle, in degrees, outside which the source is at <see cref="ConeOuterGain"/>.</summary>
    public double ConeOuterAngle { get; set; } = 360;

    /// <summary>The gain applied outside the outer cone - 0 makes a source inaudible from behind.</summary>
    public double ConeOuterGain { get; set; }

    internal AudioPannerJsOptions ToJsObject() => new()
    {
        PanningModel = PanningModel == AudioPanningModel.Hrtf ? "HRTF" : "equalpower",
        DistanceModel = DistanceModel switch
        {
            AudioDistanceModel.Linear => "linear",
            AudioDistanceModel.Exponential => "exponential",
            _ => "inverse"
        },
        PositionX = PositionX,
        PositionY = PositionY,
        PositionZ = PositionZ,
        OrientationX = OrientationX,
        OrientationY = OrientationY,
        OrientationZ = OrientationZ,
        RefDistance = RefDistance,
        MaxDistance = MaxDistance,
        RolloffFactor = RolloffFactor,
        ConeInnerAngle = ConeInnerAngle,
        ConeOuterAngle = ConeOuterAngle,
        ConeOuterGain = ConeOuterGain
    };
}
