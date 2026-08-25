namespace Bit.BlazorUI;

/// <summary>
/// The availability of the person a BitPersona represents, shown as a dot on the coin.
/// </summary>
public enum BitPersonaPresence
{
    /// <summary>
    /// No presence is known or worth showing, so no dot is rendered at all.
    /// </summary>
    None,

    /// <summary>
    /// The person is signed out.
    /// </summary>
    Offline,

    /// <summary>
    /// The person is signed in and available.
    /// </summary>
    Online,

    /// <summary>
    /// The person is signed in but idle.
    /// </summary>
    Away,

    /// <summary>
    /// The person has asked not to be interrupted.
    /// </summary>
    Dnd,

    /// <summary>
    /// The person cannot be reached from here.
    /// </summary>
    Blocked,

    /// <summary>
    /// The person is signed in and occupied.
    /// </summary>
    Busy,

    /// <summary>
    /// The person is away from work for an extended period.
    /// </summary>
    OutOfOffice,

    /// <summary>
    /// The presence of the person could not be determined.
    /// </summary>
    Unknown
}
