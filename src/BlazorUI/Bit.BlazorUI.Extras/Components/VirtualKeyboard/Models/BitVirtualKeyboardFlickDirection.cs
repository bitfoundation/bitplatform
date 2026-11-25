using System.Text.Json.Serialization;

namespace Bit.BlazorUI;

/// <summary>
/// The direction of a flick on a key of a <see cref="BitVirtualKeyboard"/>. Reserved for flick gestures, which the
/// keyboard does not act on yet.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<BitVirtualKeyboardFlickDirection>))]
public enum BitVirtualKeyboardFlickDirection
{
    /// <summary>North.</summary>
    N,

    /// <summary>North-east.</summary>
    NE,

    /// <summary>East.</summary>
    E,

    /// <summary>South-east.</summary>
    SE,

    /// <summary>South.</summary>
    S,

    /// <summary>South-west.</summary>
    SW,

    /// <summary>West.</summary>
    W,

    /// <summary>North-west.</summary>
    NW
}