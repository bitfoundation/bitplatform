using System;

namespace Bit.Butil;

/// <summary>
/// Mirrors <see href="https://developer.mozilla.org/en-US/docs/Web/API/GeolocationPositionError">GeolocationPositionError.code</see>.
/// </summary>
public enum GeolocationErrorCode
{
    /// <summary>The user denied the request for geolocation.</summary>
    PermissionDenied = 1,

    /// <summary>The position cannot be determined.</summary>
    PositionUnavailable = 2,

    /// <summary>The request timed out.</summary>
    Timeout = 3,

    /// <summary>The error is not one of the above.</summary>
    Unknown = 0
}

/// <summary>
/// Wraps a <c>GeolocationPositionError</c> raised by the browser.
/// </summary>
public class GeolocationException : Exception
{
    public GeolocationErrorCode Code { get; }

    public GeolocationException(GeolocationErrorCode code, string message) : base(message)
    {
        Code = code;
    }
}
