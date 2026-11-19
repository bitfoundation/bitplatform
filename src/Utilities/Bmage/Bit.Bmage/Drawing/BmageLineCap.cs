namespace Bit.Bmage;

/// <summary>How the ends of an open stroked path are drawn.</summary>
public enum BmageLineCap
{
    /// <summary>Ends exactly at the endpoint.</summary>
    Butt = 0,
    /// <summary>Extends by half the stroke width with a semicircle.</summary>
    Round,
    /// <summary>Extends by half the stroke width with a square.</summary>
    Square,
}
