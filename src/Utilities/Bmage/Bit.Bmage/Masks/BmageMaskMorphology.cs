namespace Bit.Bmage;

/// <summary>How a <see cref="BmageMorphedMask"/> changes the mask it wraps.</summary>
public enum BmageMaskMorphology
{
    /// <summary>Grows the covered area by the distance in every direction.</summary>
    Expand = 0,
    /// <summary>Shrinks the covered area by the distance from every edge.</summary>
    Contract,
    /// <summary>A band the distance wide, centred on the mask's edge: an outline of the selection.</summary>
    Border,
}
