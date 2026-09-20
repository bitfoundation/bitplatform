using System;

namespace Bit.Butil;

/// <summary>
/// Wraps a <c>GeolocationPositionError</c> raised by the browser.
/// </summary>
public class GeolocationException : Exception
{
    /// <summary>Which of the four failures this was.</summary>
    public GeolocationErrorCode Code { get; }

    /// <summary>Creates an exception for one <c>GeolocationPositionError</c>.</summary>
    public GeolocationException(GeolocationErrorCode code, string message) : base(message)
    {
        Code = code;
    }
}
