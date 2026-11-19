namespace Bit.Blow;

/// <summary>
/// Controls which handle pairings are allowed when connecting.
/// </summary>
public enum BlowConnectionMode
{
    /// <summary>Only source-to-target connections are valid (default).</summary>
    Strict,

    /// <summary>Any handle can connect to any other handle.</summary>
    Loose
}
