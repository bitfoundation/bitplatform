namespace Bit.Butil;

/// <summary>
/// The primary actions an XR input source can report, from the
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/XRSession/select_event">select</see>
/// and squeeze event families.
/// </summary>
/// <remarks>
/// Select is "the main button" - a trigger, a pinch, a tap on a phone screen - and is the one action
/// every device has. Squeeze is grabbing, which many devices don't report at all.
/// </remarks>
public enum XrInputEventType
{
    /// <summary>The primary action started.</summary>
    SelectStart,

    /// <summary>The primary action completed. The one to act on for a click-like interaction.</summary>
    Select,

    /// <summary>The primary action ended, whether or not it completed.</summary>
    SelectEnd,

    /// <summary>A grab started.</summary>
    SqueezeStart,

    /// <summary>A grab completed.</summary>
    Squeeze,

    /// <summary>A grab ended.</summary>
    SqueezeEnd
}
