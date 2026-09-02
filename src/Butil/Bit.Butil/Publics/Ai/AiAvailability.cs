namespace Bit.Butil;

/// <summary>
/// Whether one of the built-in AI APIs can serve a given option set right now - the answer from
/// every <c>Availability()</c> probe.
/// </summary>
public enum AiAvailability
{
    /// <summary>
    /// Not usable: the runtime has no such API, the device doesn't meet the model's requirements,
    /// or the options ask for something the model can't do (an unsupported language pair, say).
    /// </summary>
    Unavailable,

    /// <summary>
    /// Usable, but the model has to be downloaded first. Creating a session starts that download,
    /// which needs a user gesture and can take minutes - pass a progress handler.
    /// </summary>
    Downloadable,

    /// <summary>The model is downloading right now. Creating a session waits for it to finish.</summary>
    Downloading,

    /// <summary>Ready: a session can be created without waiting for anything.</summary>
    Available,
}
