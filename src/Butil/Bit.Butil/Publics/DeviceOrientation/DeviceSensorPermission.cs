namespace Bit.Butil;

/// <summary>The outcome of <see cref="DeviceOrientation.RequestPermission"/>.</summary>
public enum DeviceSensorPermission
{
    /// <summary>
    /// Nothing was asked - the JS runtime wasn't available, i.e. this ran during prerender/SSR.
    /// </summary>
    Unknown,

    /// <summary>
    /// Access is available. Also the answer on every engine that doesn't gate these events at all.
    /// </summary>
    Granted,

    /// <summary>
    /// The user declined, or the request was made outside a user gesture and the engine rejected it.
    /// </summary>
    Denied,
}
