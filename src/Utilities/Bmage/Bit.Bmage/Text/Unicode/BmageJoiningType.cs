namespace Bit.Bmage;

/// <summary>Joining_Type (the Arabic shaping property), in the order the generated table numbers it.</summary>
internal enum BmageJoiningType : byte
{
    /// <summary>Non joining: a space, a Latin letter, ZERO WIDTH NON-JOINER.</summary>
    U,
    /// <summary>Joins only to the letter before it, as alef and reh do.</summary>
    R,
    /// <summary>Joins only to the letter after it; no Arabic letter does.</summary>
    L,
    /// <summary>Joins on both sides, as beh and seen do.</summary>
    D,
    /// <summary>Join causing: makes its neighbours join to it, as tatweel and ZERO WIDTH JOINER do.</summary>
    C,
    /// <summary>Transparent: skipped when deciding how letters join, as the harakat are.</summary>
    T,
}
