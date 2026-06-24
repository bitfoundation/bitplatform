namespace Bit.Butil;

/// <summary>
/// Mirrors <see href="https://developer.mozilla.org/en-US/docs/Web/API/PermissionStatus/state">PermissionStatus.state</see>.
/// </summary>
public enum PermissionState
{
    /// <summary>The user has granted the permission.</summary>
    Granted,

    /// <summary>The user has denied the permission.</summary>
    Denied,

    /// <summary>The user must be asked the next time the capability is used.</summary>
    Prompt,

    /// <summary>The runtime does not implement the Permissions API or doesn't know about the descriptor.</summary>
    Unknown
}
