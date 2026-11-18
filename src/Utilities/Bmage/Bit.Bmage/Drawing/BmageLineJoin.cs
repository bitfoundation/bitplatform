namespace Bit.Bmage;

/// <summary>How corners between segments are drawn.</summary>
public enum BmageLineJoin
{
    /// <summary>Extends the outer edges until they meet, falling back to a bevel past the miter limit.</summary>
    Miter = 0,
    /// <summary>Rounds the corner with an arc.</summary>
    Round,
    /// <summary>Cuts the corner off with a straight edge.</summary>
    Bevel,
}
