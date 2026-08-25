namespace Bit.BlazorUI;

/// <summary>
/// The size ramp of the BitPersona, which decides the diameter of the coin and how many of the
/// detail texts are shown next to it.
/// </summary>
public enum BitPersonaSize
{
    /// <summary>
    /// A presence dot and the primary text only, with no coin at all.
    /// </summary>
    Size8,

    /// <summary>
    /// A 24px coin with the primary text.
    /// </summary>
    Size24,

    /// <summary>
    /// A 32px coin with the primary text.
    /// </summary>
    Size32,

    /// <summary>
    /// A 40px coin with the primary and secondary texts.
    /// </summary>
    Size40,

    /// <summary>
    /// A 48px coin with the primary and secondary texts.
    /// </summary>
    Size48,

    /// <summary>
    /// A 56px coin with the primary and secondary texts.
    /// </summary>
    Size56,

    /// <summary>
    /// A 72px coin with the primary, secondary and tertiary texts.
    /// </summary>
    Size72,

    /// <summary>
    /// A 100px coin with all four texts.
    /// </summary>
    Size100,

    /// <summary>
    /// A 120px coin with all four texts.
    /// </summary>
    Size120
}
