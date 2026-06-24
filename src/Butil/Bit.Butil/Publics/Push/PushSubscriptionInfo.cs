namespace Bit.Butil;

/// <summary>
/// Mirrors <see href="https://developer.mozilla.org/en-US/docs/Web/API/PushSubscription">PushSubscription</see>.
/// </summary>
public class PushSubscriptionInfo
{
    /// <summary>True when a subscription was found / created.</summary>
    public bool IsActive { get; set; }

    /// <summary>The endpoint URL the push service expects POSTs at.</summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>Unix-epoch milliseconds expiration time, or null when the subscription doesn't expire.</summary>
    public long? ExpirationTime { get; set; }

    /// <summary>Base64URL-encoded P-256 ECDH public key for payload encryption.</summary>
    public string P256dh { get; set; } = string.Empty;

    /// <summary>Base64URL-encoded auth secret used by the Web Push protocol.</summary>
    public string Auth { get; set; } = string.Empty;
}
