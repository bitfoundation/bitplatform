namespace Bit.Butil;

/// <summary>
/// What a codec can currently do, mirroring the <c>state</c> of a
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/VideoEncoder/state">WebCodecs encoder or decoder</see>.
/// </summary>
public enum CodecState
{
    /// <summary>Created but not configured - the state a <see cref="WebCodecsHandle.Reset"/> leaves it in. Work submitted now is rejected.</summary>
    Unconfigured,

    /// <summary>Configured and accepting work.</summary>
    Configured,

    /// <summary>Closed for good, by disposal or by an unrecoverable error. A closed codec cannot be reconfigured.</summary>
    Closed
}
