namespace Bit.Bmage;

/// <summary>The eight hue bands an <see cref="BmageHslAdjustment"/> adjusts separately, named for their centre.</summary>
public enum BmageHueBand
{
    /// <summary>Centred on 0°.</summary>
    Reds = 0,
    /// <summary>Centred on 30°.</summary>
    Oranges,
    /// <summary>Centred on 60°.</summary>
    Yellows,
    /// <summary>Centred on 120°.</summary>
    Greens,
    /// <summary>Centred on 180°.</summary>
    Aquas,
    /// <summary>Centred on 240°.</summary>
    Blues,
    /// <summary>Centred on 270°.</summary>
    Purples,
    /// <summary>Centred on 300°.</summary>
    Magentas,
}
