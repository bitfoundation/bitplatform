namespace Bit.Butil;

/// <summary>
/// What a session's licence is allowed to outlive, the argument of
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/MediaKeys/createSession">MediaKeys.createSession()</see>.
/// </summary>
public enum MediaKeySessionType
{
    /// <summary>
    /// The licence lives only as long as the session. Every playback fetches a fresh one - what
    /// streaming normally uses, and the only type every key system supports.
    /// </summary>
    Temporary,

    /// <summary>
    /// The licence is stored by the key system and can be reloaded later by its session id, which is
    /// what offline playback is built on. Requires
    /// <see cref="MediaKeySystemConfiguration.PersistentState"/> to be
    /// <see cref="MediaKeysRequirement.Required"/>, and is not available everywhere.
    /// </summary>
    PersistentLicense
}
