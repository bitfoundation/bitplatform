namespace Bit.Butil;

/// <summary>
/// How a <see cref="MediaKeySystemConfiguration"/> feels about a capability the key system may or
/// may not need, mirroring
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Navigator/requestMediaKeySystemAccess#distinctiveidentifier">MediaKeysRequirement</see>.
/// </summary>
public enum MediaKeysRequirement
{
    /// <summary>The configuration is only acceptable with the capability. A key system that can't provide it is skipped.</summary>
    Required,

    /// <summary>Use it if it is available. The resolved configuration says which way it went.</summary>
    Optional,

    /// <summary>Refuse a key system that would use it - the choice that avoids a user-visible consent prompt on some systems.</summary>
    NotAllowed
}
