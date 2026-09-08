namespace Bit.Butil;

/// <summary>
/// What a key session is asking for, the <c>messageType</c> of
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/MediaKeyMessageEvent">MediaKeyMessageEvent</see>.
/// </summary>
/// <remarks>
/// All four go to the licence server the same way - post the bytes, feed the response back through
/// <see cref="MediaKeySessionHandle.Update"/> - but the type tells you which endpoint or which
/// server-side flow the request belongs to.
/// </remarks>
public enum MediaKeyMessageType
{
    /// <summary>The initial licence request, produced by <see cref="MediaKeySessionHandle.GenerateRequest(EncryptedMediaInitData)"/>.</summary>
    LicenseRequest,

    /// <summary>A renewal for a licence that is about to expire, raised by the key system on its own.</summary>
    LicenseRenewal,

    /// <summary>Proof that a persistent licence was released, produced by <see cref="MediaKeySessionHandle.Remove"/>.</summary>
    LicenseRelease,

    /// <summary>A provisioning request the key system needs to complete before it can be licensed at all.</summary>
    IndividualizationRequest,

    /// <summary>A message type this version of Butil doesn't know - forward the bytes unchanged.</summary>
    Unknown
}
